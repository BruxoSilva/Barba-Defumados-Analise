import { NextResponse } from 'next/server';
import { db } from '@/lib/db';
import { readFile } from 'node:fs/promises';
import { resolve,sep } from 'node:path';
import { uploadRoot } from '@/lib/adapters';
export async function GET(_request:Request,{params}:{params:Promise<{id:string}>}){const {id}=await params;const media=await db.productMedia.findUnique({where:{id}});if(!media)return new Response('Não encontrado',{status:404});const base=uploadRoot();const file=resolve(media.path);if(!file.startsWith(base+sep))return new Response('Acesso negado',{status:403});try{const bytes=await readFile(file);const ext=file.toLowerCase();const mime=ext.endsWith('.png')?'image/png':ext.endsWith('.webp')?'image/webp':ext.endsWith('.mp4')?'video/mp4':ext.endsWith('.webm')?'video/webm':'image/jpeg';return new NextResponse(bytes,{headers:{'Content-Type':mime,'Cache-Control':'private, max-age=3600','X-Content-Type-Options':'nosniff'}})}catch{return new Response('Não encontrado',{status:404})}}
