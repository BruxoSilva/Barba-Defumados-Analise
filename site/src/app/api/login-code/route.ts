import { NextResponse } from 'next/server';
import { hash } from 'bcryptjs';
import { randomInt } from 'node:crypto';
import { z } from 'zod';
import { db } from '@/lib/db';
import { simulatedNotifications } from '@/lib/adapters';
export async function POST(request:Request){
 const data=z.object({phone:z.string().regex(/^\d{10,11}$/)}).safeParse(await request.json().catch(()=>null));
 if(!data.success)return NextResponse.json({error:'Telefone inválido'},{status:400});
 const recent=await db.loginCode.count({where:{phone:data.data.phone,createdAt:{gt:new Date(Date.now()-60000)}}});
 if(recent>=3)return NextResponse.json({error:'Aguarde antes de pedir outro código'},{status:429});
 const code=String(randomInt(100000,1000000));
 await db.loginCode.create({data:{phone:data.data.phone,codeHash:await hash(code,10),expiresAt:new Date(Date.now()+10*60000)}});
 await simulatedNotifications.send(data.data.phone,`Seu código de acesso é ${code}.`,'SMS');
 return NextResponse.json({code,simulated:true});
}
