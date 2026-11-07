import Link from 'next/link';
import { db } from '@/lib/db';
import { requireCustomer } from '@/lib/session';
import { lineTotal } from '@/lib/commerce';
import { removeFromCartAction } from './actions';
import { Countdown } from './countdown';
export const dynamic='force-dynamic';
const money=(n:number)=>new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(n/100);
export default async function CartPage(){const userId=await requireCustomer('/carrinho');const cart=await db.cart.findFirst({where:{userId,status:'ACTIVE',expiresAt:{gt:new Date()}},include:{items:{include:{product:true}}},orderBy:{createdAt:'desc'}});const grouped=new Map<string,{name:string;qty:number;retail:number;wholesale:number|null;min:number|null}>();for(const item of cart?.items??[]){const old=grouped.get(item.productId);grouped.set(item.productId,{name:item.product.name,qty:(old?.qty??0)+item.quantityUnits,retail:item.product.priceCents,wholesale:item.product.wholesalePriceCents,min:item.product.wholesaleMinQty})}const subtotal=[...grouped.values()].reduce((n,p)=>n+lineTotal({qty:p.qty,retailCents:p.retail,wholesaleCents:p.wholesale,wholesaleMin:p.min}),0);return <main id="conteudo" className="admin-wrap"><Link href="/">← Continuar navegando</Link><h1>Meu carrinho</h1>{cart?.items.length?<><p><Countdown expiresAt={cart.expiresAt.toISOString()}/></p>{cart.items.map(item=><div className="admin-card" key={item.id}><h2>{item.product.name}</h2><p>{item.quantityUnits} unidade(s)</p><form action={removeFromCartAction}><input name="itemId" type="hidden" value={item.id}/><button>Remover</button></form></div>)}<p><strong>Subtotal: {money(subtotal)}</strong></p><Link className="button" href="/checkout">Finalizar compra</Link></>:<p>Seu carrinho está vazio ou a reserva expirou.</p>}</main>}
