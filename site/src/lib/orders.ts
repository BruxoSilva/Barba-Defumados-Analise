import { db } from './db';
import { getSetting } from './config';
const allowed:Record<string,string[]>={RECEIVED:['PREPARING','CANCELLED'],PREPARING:['OUT_FOR_DELIVERY','CANCELLED'],OUT_FOR_DELIVERY:['DELIVERED','CANCELLED'],DELIVERED:[],CANCELLED:[]};
export function canTransition(from:string,to:string){return allowed[from]?.includes(to)??false}
const labels:Record<string,string>={RECEIVED:'Recebido',PREPARING:'Em preparo',OUT_FOR_DELIVERY:'Saiu para entrega',DELIVERED:'Entregue',CANCELLED:'Cancelado'};
export async function changeOrderStatus(actorId:string,orderId:string,status:string){
 const [statusTemplate,reviewTemplate,validDays]=await Promise.all([getSetting('messages.orderStatus'),getSetting('messages.reviewAvailable'),getSetting('cashback.validDays')]);
 return db.$transaction(async tx=>{const order=await tx.order.findUniqueOrThrow({where:{id:orderId},include:{items:true,user:true}});if(!canTransition(order.status,status))throw new Error('Transição de status inválida');
  const now=new Date();await tx.order.update({where:{id:orderId},data:{status,deliveredAt:status==='DELIVERED'?now:null}});
  if(status==='DELIVERED')await tx.cashbackEntry.updateMany({where:{orderId,status:'PENDING'},data:{status:'AVAILABLE',remainingCents:{set:0}}});
  if(status==='DELIVERED'){const entries=await tx.cashbackEntry.findMany({where:{orderId,status:'AVAILABLE',amountCents:{gt:0}}});for(const e of entries)await tx.cashbackEntry.update({where:{id:e.id},data:{remainingCents:e.amountCents,expiresAt:new Date(now.getTime()+Number(validDays)*86400000)}})}
  if(status==='CANCELLED'){
   for(const item of order.items)await tx.stockLot.update({where:{id:item.lotId},data:{quantityUnits:{increment:item.quantityUnits}}});
   await tx.cashbackEntry.updateMany({where:{orderId,status:'PENDING'},data:{status:'CANCELLED'}});
   if(order.cashbackUsedCents)await tx.cashbackEntry.create({data:{userId:order.userId,orderId,amountCents:order.cashbackUsedCents,remainingCents:order.cashbackUsedCents,status:'AVAILABLE',expiresAt:new Date(now.getTime()+Number(validDays)*86400000)}});
  }
  if(order.user.phone){await tx.outboxMessage.create({data:{channel:'WHATSAPP',recipient:order.user.phone,body:statusTemplate.replace('{status}',labels[status]??status)}});if(status==='DELIVERED')await tx.outboxMessage.create({data:{channel:'WHATSAPP',recipient:order.user.phone,body:reviewTemplate}})}
  await tx.auditLog.create({data:{actorId,action:'STATUS',entity:'Order',entityId:orderId,beforeJson:order.status,afterJson:status}});
 });
}
