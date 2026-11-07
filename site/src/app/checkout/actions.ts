'use server';
import { redirect } from 'next/navigation';
import { requireCustomer } from '@/lib/session';
import { checkout } from '@/lib/checkout';
import { parseReais } from '@/lib/money-input';
export async function checkoutAction(form:FormData){const userId=await requireCustomer('/checkout');let orderId:string|undefined;let error:string|undefined;try{const order=await checkout(userId,{deliveryMethod:form.get('deliveryMethod'),deliverySlot:form.get('deliverySlot'),couponCode:form.get('couponCode'),paymentMethod:form.get('paymentMethod'),paymentOutcome:form.get('paymentOutcome'),cashbackRequestedCents:parseReais(String(form.get('cashbackRequestedReais')??'0'))});orderId=order.id}catch(e){error=e instanceof Error?e.message:'Não foi possível concluir'}if(error)redirect(`/checkout?erro=${encodeURIComponent(error)}`);redirect(`/pedidos/${orderId}`)}
