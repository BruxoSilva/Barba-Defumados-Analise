import { notFound } from 'next/navigation';
import Link from 'next/link';
import { db } from '@/lib/db';
import { requireCustomer } from '@/lib/session';
import { addToCartAction } from '@/app/carrinho/actions';
import { waitlistAction } from './actions';
export const dynamic='force-dynamic';
const money=(cents:number)=>new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(cents/100);
export default async function ProductPage({params}:{params:Promise<{slug:string}>}){
 const {slug}=await params;await requireCustomer(`/produtos/${slug}`);
 const product=await db.product.findUnique({where:{slug},include:{kitParts:{include:{component:true}},category:true,media:{orderBy:{position:'asc'}},lots:true,reviews:{where:{status:'APPROVED'},select:{stars:true,comment:true,response:true}}}});
 if(!product||!product.active)notFound();
 const available=product.lots.filter(l=>!l.expiresAt||l.expiresAt>new Date()).reduce((n,l)=>n+l.quantityUnits,0);
 const photos=product.media.filter(m=>m.type==='IMAGE');const videos=product.media.filter(m=>m.type==='VIDEO');
 return <main id="conteudo" className="admin-wrap"><Link href="/">← Voltar à loja</Link><p className="small-label">{product.category.name}</p><h1>{product.name}</h1>{photos[0]?<img className="detail-image" src={`/api/media/${photos[0].id}`} width="720" height="500" alt={product.name}/>:<div className="product-image" role="img" aria-label={`Imagem provisória de ${product.name}`}/>}
 <p>{product.description}</p><p><strong>Varejo: {money(product.priceCents)}</strong>{product.wholesalePriceCents!=null&&product.wholesaleMinQty!=null&&<> | Atacado (a partir de {product.wholesaleMinQty} unidades): {money(product.wholesalePriceCents)}</>}</p><p>Peso da embalagem: {product.weightGrams} g · Venda por {product.saleUnit === 'KG' ? 'KG em embalagem fechada' : 'UN'}</p>{product.smokeType&&<p>Defumação: {product.smokeType}</p>}{product.notes&&<p>Observações: {product.notes}</p>}{product.pairing&&<p>Combina com: {product.pairing}</p>}{product.badges&&<p>{product.badges}</p>}{product.kitParts.length>0&&<section><h2>O que vem no kit</h2>{product.kitParts.map(part=><p key={part.id}>{part.quantityUnits} × {part.component.name}</p>)}</section>}{product.lots.filter(l=>l.quantityUnits>0&&(!l.expiresAt||l.expiresAt>new Date())).map(l=><p key={l.id}>Lote {l.code} · validade {l.expiresAt?.toLocaleDateString('pt-BR')??'não informada'}</p>)}<p>{available>0?`${available} unidades disponíveis`:'Produto esgotado'}</p>
 {available>0?<form action={addToCartAction} className="admin-card"><input type="hidden" name="productId" value={product.id}/><label className="field">Quantidade de embalagens<input name="quantityUnits" type="number" min="1" max={available} defaultValue="1" required/></label><button className="button">Adicionar ao carrinho</button></form>:<form action={waitlistAction} className="admin-card"><input type="hidden" name="slug" value={slug}/><label className="field">WhatsApp para aviso de reposição<input name="phone" type="tel" required/></label><button className="button">Avise-me quando chegar</button></form>}
 {photos.length>1&&<section><h2>Fotos</h2><div className="media-grid">{photos.map(m=><img key={m.id} src={`/api/media/${m.id}`} width="320" height="240" alt={product.name} loading="lazy"/>)}</div></section>}{videos.length>0&&<section><h2>Vídeos</h2><div className="media-grid">{videos.map(m=><video key={m.id} src={`/api/media/${m.id}`} controls preload="metadata" width="320" aria-label={`Vídeo de ${product.name}`}/>)}</div></section>}
 <h2>Avaliações</h2>{product.reviews.length?product.reviews.map((review,i)=><div className="admin-card" key={i}>{'★'.repeat(review.stars)}<p>{review.comment}</p>{review.response&&<p>Resposta da loja: {review.response}</p>}</div>):<p>Ainda não há avaliações.</p>}</main>;
}



