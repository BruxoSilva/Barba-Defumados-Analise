import { z } from 'zod';
import { db } from './db';
import { canUseReferral,orderTotals } from './commerce';
import { simulatedPayment } from './adapters';
const checkoutInput=z.object({deliveryMethod:z.enum(['DELIVERY','PICKUP']),deliverySlot:z.string().optional(),couponCode:z.string().trim().optional(),paymentMethod:z.enum(['PIX','CARD','CASHBACK']),paymentOutcome:z.enum(['approve','decline']).default('approve'),cashbackRequestedCents:z.number().int().min(0).default(0)});
export function promotionDiscount(subtotal:number,promo:{percentBps:number|null;amountCents:number|null}){return Math.min(subtotal,promo.amountCents??Math.floor(subtotal*(promo.percentBps??0)/10000))}
export function resolveShipping(neighborhood:string,cep:string,rates:{neighborhood:string|null;cepStart:string|null;cepEnd:string|null;priceCents:number}[],defaultCents:number){const local=rates.find(r=>r.neighborhood?.toLowerCase()===neighborhood.toLowerCase());if(local)return local.priceCents;const range=rates.find(r=>r.cepStart&&r.cepEnd&&cep>=r.cepStart&&cep<=r.cepEnd);return range?.priceCents??defaultCents}
export async function checkout(userId:string,raw:unknown){const input=checkoutInput.parse(raw);const now=new Date();const [user,cart,settings,rates,promotions]=await Promise.all([db.user.findUnique({where:{id:userId},include:{address:true}}),db.cart.findFirst({where:{userId,status:'ACTIVE',expiresAt:{gt:now}},include:{items:{include:{product:true,lot:true}}}}),db.setting.findMany(),db.deliveryRate.findMany({where:{active:true}}),db.promotion.findMany({where:{active:true}})]);
 if(!user?.profileComplete||!user.address)throw new Error('Complete seu cadastro antes de comprar');if(!cart?.items.length)throw new Error('Carrinho vazio ou reserva expirada');
 if(cart.items.some(i=>!i.product.active||(i.lot.expiresAt&&i.lot.expiresAt<=now)))throw new Error('Um item do carrinho não está mais disponível');
 const config=Object.fromEntries(settings.map(s=>[s.key,s.value]));
 const grouped=new Map<string,number>();for(const item of cart.items)grouped.set(item.productId,(grouped.get(item.productId)??0)+item.quantityUnits);
 const activePromotions=promotions.filter(p=>(!p.startsAt||p.startsAt<=now)&&(!p.endsAt||p.endsAt>=now));
 const unitPrices=new Map<string,number>();
 for(const item of cart.items){const p=item.product;const qty=grouped.get(item.productId)!;const base=p.wholesalePriceCents!=null&&p.wholesaleMinQty!=null&&qty>=p.wholesaleMinQty?p.wholesalePriceCents:p.priceCents;const reduction=Math.max(0,...activePromotions.filter(x=>(x.kind==='PRODUCT'&&x.productId===p.id)||(x.kind==='CATEGORY'&&x.categoryId===p.categoryId)).map(x=>promotionDiscount(base,x)));unitPrices.set(p.id,base-reduction)}
 const subtotalCents=cart.items.reduce((sum,item)=>sum+unitPrices.get(item.productId)!*item.quantityUnits,0);
 if(subtotalCents<Number(config['shipping.minimumOrderCents']))throw new Error('Pedido abaixo do valor mínimo');
 let shippingCents=0;if(input.deliveryMethod==='DELIVERY'){const slots=config['shipping.deliverySlots'].split('\n').map(s=>s.trim()).filter(Boolean);if(!input.deliverySlot||!slots.includes(input.deliverySlot))throw new Error('Escolha um horário de entrega');shippingCents=resolveShipping(user.address.neighborhood,user.address.cep,rates,Number(config['shipping.defaultCents']));const free=activePromotions.some(p=>p.kind==='FREE_SHIPPING'&&subtotalCents>=p.minOrderCents&&(!p.region||p.region.toLowerCase()===user.address?.neighborhood.toLowerCase()));if(free||subtotalCents>=Number(config['shipping.freeAboveCents']))shippingCents=0}else if(config['shipping.pickupEnabled']!=='true')throw new Error('Retirada indisponível');
 const coupon=(input.couponCode??'').toUpperCase();const referral=coupon?await db.referralCode.findFirst({where:{code:coupon,active:true},include:{user:{include:{address:true}}}}):null;
 const promotion=coupon&&!referral?promotions.find(p=>p.kind==='COUPON'&&p.code===coupon&&(!p.startsAt||p.startsAt<=now)&&(!p.endsAt||p.endsAt>=now)&&(!p.usageLimit||p.usedCount<p.usageLimit)&&subtotalCents>=p.minOrderCents):null;
 if(coupon&&!referral&&!promotion)throw new Error('Cupom inválido ou expirado');
 if(referral){const a=referral.user.address;const b=user.address;const sameAddress=Boolean(a&&b&&a.cep===b.cep&&a.number.toLowerCase()===b.number.toLowerCase());if(!canUseReferral({ownerId:referral.userId,buyerId:userId,samePhone:Boolean(referral.user.phone&&referral.user.phone===user.phone),sameAddress,blockPhone:config['referrals.blockSamePhone']==='true',blockAddress:config['referrals.blockSameAddress']==='true'}))throw new Error('Este convite não pode ser usado por esta conta')}
 const discountCents=promotion?promotionDiscount(subtotalCents,promotion):referral?Math.floor(subtotalCents*Number(config['referrals.buyerDiscountBps'])/10000):0;
 const credits=await db.cashbackEntry.findMany({where:{userId,status:'AVAILABLE',remainingCents:{gt:0},OR:[{expiresAt:null},{expiresAt:{gt:now}}]},orderBy:{expiresAt:'asc'}});
 const available=credits.reduce((sum,e)=>sum+e.remainingCents,0);
 const totals=orderTotals({subtotalCents,discountCents,shippingCents,cashbackRequestedCents:input.paymentMethod==='CASHBACK'?subtotalCents+shippingCents:input.cashbackRequestedCents,cashbackAvailableCents:available});
 if(input.paymentMethod==='CASHBACK'&&totals.totalCents>0)throw new Error('Saldo insuficiente para pagamento apenas com cashback');
 if(totals.totalCents>0){const payment=await simulatedPayment.charge(totals.totalCents,input.paymentOutcome);if(!payment.approved)throw new Error('Pagamento simulado recusado')}
 const campaign=promotions.find(p=>p.kind==='CASHBACK'&&(!p.startsAt||p.startsAt<=now)&&(!p.endsAt||p.endsAt>=now));
 const percentBps=referral?(config['referrals.percentDateBasis']==='creation'?referral.percentBps:campaign?.percentBps??Number(config['cashback.defaultPercentBps'])):0;
 return db.$transaction(async tx=>{
  const fresh=await tx.cart.findUnique({where:{id:cart.id}});if(fresh?.status!=='ACTIVE'||fresh.expiresAt<=new Date())throw new Error('Reserva expirada');
  let left=totals.cashbackUsedCents;for(const credit of credits){if(left<=0)break;const take=Math.min(left,credit.remainingCents);const changed=await tx.cashbackEntry.updateMany({where:{id:credit.id,remainingCents:{gte:take}},data:{remainingCents:{decrement:take}}});if(changed.count!==1)throw new Error('Saldo de cashback alterado');left-=take}if(left)throw new Error('Saldo de cashback insuficiente');
  const orderItems=cart.items.map(item=>{
   const p=item.product;const unitPriceCents=unitPrices.get(item.productId)!;
   return {productId:item.productId,productName:p.name,quantityUnits:item.quantityUnits,unitPriceCents,lotId:item.lotId,lotCode:item.lot.code};
  });
  const order=await tx.order.create({data:{userId,subtotalCents,discountCents,shippingCents,cashbackUsedCents:totals.cashbackUsedCents,totalCents:totals.totalCents,paymentMethod:input.paymentMethod,deliveryMethod:input.deliveryMethod,deliverySlot:input.deliverySlot,addressSnapshot:JSON.stringify(user.address),referralCodeId:referral?.id,items:{create:orderItems}}});
  if(totals.cashbackUsedCents)await tx.cashbackEntry.create({data:{userId,orderId:order.id,amountCents:-totals.cashbackUsedCents,status:'USED'}});
  if(referral&&percentBps>0)await tx.cashbackEntry.create({data:{userId:referral.userId,orderId:order.id,amountCents:Math.floor(subtotalCents*percentBps/10000),status:'PENDING'}});
  if(promotion){const changed=await tx.promotion.updateMany({where:{id:promotion.id,...(promotion.usageLimit?{usedCount:{lt:promotion.usageLimit}}:{})},data:{usedCount:{increment:1}}});if(changed.count!==1)throw new Error('Limite do cupom atingido')}
  await tx.cart.update({where:{id:cart.id},data:{status:'COMPLETED'}});
  if(user.phone)await tx.outboxMessage.create({data:{channel:'WHATSAPP',recipient:user.phone,body:config['messages.orderStatus'].replace('{status}','Recebido')}});
  return order;
 });
}
