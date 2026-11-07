'use server';
import { redirect } from 'next/navigation';
import { requireCustomer } from '@/lib/session';
import { reserveProduct,removeCartItem } from '@/lib/cart';
export async function addToCartAction(form:FormData){const productId=String(form.get('productId')??'');const userId=await requireCustomer('/carrinho');await reserveProduct(userId,productId,Number(form.get('quantityUnits')));redirect('/carrinho')}
export async function removeFromCartAction(form:FormData){const userId=await requireCustomer('/carrinho');await removeCartItem(userId,String(form.get('itemId')??''));redirect('/carrinho')}
