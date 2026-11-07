import { formatReais } from '@/lib/money-input';
import Link from 'next/link';import { db } from '@/lib/db';import { requireAdmin } from '@/lib/session';import { AdminNav } from '@/components/admin-nav';
export const dynamic='force-dynamic';
export default async function Products(){await requireAdmin();const products=await db.product.findMany({include:{category:true,lots:true},orderBy:{name:'asc'}});return <main id="conteudo" className="admin-wrap"><h1>Produtos</h1><AdminNav/><Link className="button" href="/admin/produtos/novo">Criar produto</Link><div className="admin-card">{products.map(p=><div className="message" key={p.id}><Link href={`/admin/produtos/${p.id}`}><strong>{p.name}</strong></Link><p>{p.category.name} · {formatReais(p.priceCents)} · {p.weightGrams} g · {p.active?'Ativo':'Inativo'} · {p.lots.reduce((n,l)=>n+l.quantityUnits,0)} unidades</p></div>)}</div></main>}

