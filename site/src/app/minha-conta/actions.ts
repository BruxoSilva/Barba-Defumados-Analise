'use server';
import { redirect } from 'next/navigation';
import { revalidatePath } from 'next/cache';
import { db } from '@/lib/db';
import { requireCustomer } from '@/lib/session';
import { addressInput } from '@/lib/admin/validation';
import { getSetting } from '@/lib/config';
import { z } from 'zod';
const val=(f:FormData,k:string)=>String(f.get(k)??'').trim();
export async function saveProfileAction(form:FormData){const userId=await requireCustomer('/minha-conta');const user=await db.user.findUniqueOrThrow({where:{id:userId}});const phone=z.string().regex(/^\d{10,11}$/).parse(val(form,'phone').replace(/\D/g,''));const address=addressInput.parse({cep:val(form,'cep').replace(/\D/g,''),street:val(form,'street'),number:val(form,'number'),complement:val(form,'complement'),neighborhood:val(form,'neighborhood'),city:val(form,'city'),state:val(form,'state').toUpperCase()});const name=user.profileComplete?user.name:z.string().trim().min(2).max(120).parse(val(form,'name'));if(!user.profileComplete&&!form.has('terms'))throw new Error('Aceite os termos');const ageRequired=await getSetting('customer.ageConfirmationEnabled')==='true';if(ageRequired&&!form.has('adult'))throw new Error('Confirme a maioridade');await db.$transaction([db.user.update({where:{id:userId},data:{name,phone,profileComplete:true,acceptedTermsAt:user.acceptedTermsAt??new Date(),ageConfirmedAt:ageRequired?new Date():user.ageConfirmedAt}}),db.address.upsert({where:{userId},update:address,create:{userId,...address}})]);revalidatePath('/minha-conta');redirect('/minha-conta?salvo=1')}
export async function deleteAccountAction(form:FormData){
 const userId=await requireCustomer('/minha-conta');if(val(form,'confirmation')!=='EXCLUIR')throw new Error('Confirmação inválida');
 await db.$transaction(async tx=>{
  const user=await tx.user.findUniqueOrThrow({where:{id:userId}});
  const carts=await tx.cart.findMany({where:{userId,status:'ACTIVE'},include:{items:true}});
  for(const cart of carts){for(const item of cart.items)await tx.stockLot.update({where:{id:item.lotId},data:{quantityUnits:{increment:item.quantityUnits}}});await tx.cart.update({where:{id:cart.id},data:{status:'CANCELLED'}})}
  await tx.address.deleteMany({where:{userId}});
  await tx.order.updateMany({where:{userId},data:{addressSnapshot:null}});
  await tx.referralCode.updateMany({where:{userId},data:{active:false}});
  await tx.review.deleteMany({where:{userId}});
  await tx.waitlistEntry.deleteMany({where:{userId}});
  await tx.feedback.deleteMany({where:{userId}});
  if(user.phone){await tx.outboxMessage.deleteMany({where:{recipient:user.phone}});await tx.loginCode.deleteMany({where:{phone:user.phone}})}
  await tx.user.update({where:{id:userId},data:{email:`excluido-${userId}@local.invalid`,name:'Conta excluída',phone:null,provider:null,providerId:null,profileComplete:false,deletedAt:new Date()}});
 });
 redirect('/conta-excluida');
}
