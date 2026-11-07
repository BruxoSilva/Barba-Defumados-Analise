import { Prisma } from '@prisma/client';
import { db } from './db';
import { getSetting } from './config';
import { reservationExpiresAt } from './commerce';
type Lot={id:string;quantityUnits:number;expiresAt:Date|null};
export function allocateLots(lots:Lot[],quantityUnits:number,now=new Date()){
 if(!Number.isSafeInteger(quantityUnits)||quantityUnits<1)throw new Error('Quantidade inválida');
 let left=quantityUnits;const result:{lotId:string;quantityUnits:number}[]=[];
 for(const lot of lots.filter(l=>l.quantityUnits>0&&(!l.expiresAt||l.expiresAt>now)).sort((a,b)=>(a.expiresAt?.getTime()??Infinity)-(b.expiresAt?.getTime()??Infinity))){const take=Math.min(left,lot.quantityUnits);result.push({lotId:lot.id,quantityUnits:take});left-=take;if(left===0)break}
 if(left>0)throw new Error('Estoque insuficiente');return result;
}
async function releaseInTransaction(tx:Prisma.TransactionClient,cartId:string,status:'EXPIRED'|'CANCELLED'){
 const items=await tx.cartItem.findMany({where:{cartId}});
 for(const item of items)await tx.stockLot.update({where:{id:item.lotId},data:{quantityUnits:{increment:item.quantityUnits}}});
 await tx.cart.update({where:{id:cartId},data:{status}});
}
export async function reserveProduct(userId:string,productId:string,quantityUnits:number){
 const minutes=Number(await getSetting('cart.reservationMinutes'));const now=new Date();
 return db.$transaction(async tx=>{
  const product=await tx.product.findUnique({where:{id:productId},include:{lots:true}});if(!product?.active)throw new Error('Produto indisponível');
  let cart=await tx.cart.findFirst({where:{userId,status:'ACTIVE'},orderBy:{createdAt:'desc'}});
  if(cart&&cart.expiresAt<=now){await releaseInTransaction(tx,cart.id,'EXPIRED');cart=null}
  if(!cart)cart=await tx.cart.create({data:{userId,expiresAt:reservationExpiresAt(now,minutes)}});
  const allocation=allocateLots(product.lots,quantityUnits,now);
  for(const part of allocation){const changed=await tx.stockLot.updateMany({where:{id:part.lotId,quantityUnits:{gte:part.quantityUnits}},data:{quantityUnits:{decrement:part.quantityUnits}}});if(changed.count!==1)throw new Error('Estoque insuficiente');await tx.cartItem.upsert({where:{cartId_lotId:{cartId:cart.id,lotId:part.lotId}},update:{quantityUnits:{increment:part.quantityUnits}},create:{cartId:cart.id,productId,lotId:part.lotId,quantityUnits:part.quantityUnits}})}
  return cart;
 });
}
export async function removeCartItem(userId:string,itemId:string){await db.$transaction(async tx=>{const item=await tx.cartItem.findUnique({where:{id:itemId},include:{cart:true}});if(!item||item.cart.userId!==userId||item.cart.status!=='ACTIVE')throw new Error('Item não encontrado');await tx.stockLot.update({where:{id:item.lotId},data:{quantityUnits:{increment:item.quantityUnits}}});await tx.cartItem.delete({where:{id:itemId}})})}
export async function expireCarts(now=new Date()){
 const carts=await db.cart.findMany({where:{status:'ACTIVE',expiresAt:{lte:now}}});
 for(const cart of carts)await db.$transaction(async tx=>{const current=await tx.cart.findUnique({where:{id:cart.id}});if(current?.status==='ACTIVE')await releaseInTransaction(tx,cart.id,'EXPIRED')});
 return carts.length;
}
export async function warnCarts(now=new Date()){
 const minutes=Number(await getSetting('cart.warningMinutes'));const deadline=new Date(now.getTime()+minutes*60000);
 const carts=await db.cart.findMany({where:{status:'ACTIVE',warnedAt:null,expiresAt:{gt:now,lte:deadline}},include:{user:true}});
 const template=await getSetting('messages.cartWarning');
 for(const cart of carts){await db.$transaction(async tx=>{const changed=await tx.cart.updateMany({where:{id:cart.id,warnedAt:null},data:{warnedAt:now}});if(changed.count&&cart.user.phone)await tx.outboxMessage.create({data:{channel:'WHATSAPP',recipient:cart.user.phone,body:template}})})}
 return carts.length;
}
export async function reserveBatch(userId:string,products:{productId:string;quantityUnits:number}[]){
 if(!products.length)throw new Error('Pedido vazio');const grouped=new Map<string,number>();for(const p of products){if(!Number.isSafeInteger(p.quantityUnits)||p.quantityUnits<1)throw new Error('Quantidade inválida');grouped.set(p.productId,(grouped.get(p.productId)??0)+p.quantityUnits)}
 const minutes=Number(await getSetting('cart.reservationMinutes'));const now=new Date();
 return db.$transaction(async tx=>{let cart=await tx.cart.findFirst({where:{userId,status:'ACTIVE'},orderBy:{createdAt:'desc'}});if(cart&&cart.expiresAt<=now){await releaseInTransaction(tx,cart.id,'EXPIRED');cart=null}if(!cart)cart=await tx.cart.create({data:{userId,expiresAt:reservationExpiresAt(now,minutes)}});
  for(const [productId,qty] of grouped){const product=await tx.product.findUnique({where:{id:productId},include:{lots:true}});if(!product?.active)throw new Error('Produto indisponível');for(const part of allocateLots(product.lots,qty,now)){const changed=await tx.stockLot.updateMany({where:{id:part.lotId,quantityUnits:{gte:part.quantityUnits}},data:{quantityUnits:{decrement:part.quantityUnits}}});if(changed.count!==1)throw new Error('Estoque insuficiente');await tx.cartItem.upsert({where:{cartId_lotId:{cartId:cart.id,lotId:part.lotId}},update:{quantityUnits:{increment:part.quantityUnits}},create:{cartId:cart.id,productId,lotId:part.lotId,quantityUnits:part.quantityUnits}})}}return cart;
 });
}
