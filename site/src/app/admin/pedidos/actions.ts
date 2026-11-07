'use server';import { redirect } from 'next/navigation';import { requireAdmin } from '@/lib/session';import { changeOrderStatus } from '@/lib/orders';
export async function statusAction(form:FormData){const actor=await requireAdmin();await changeOrderStatus(actor,String(form.get('orderId')),String(form.get('status')));redirect('/admin/pedidos?salvo=1')}
