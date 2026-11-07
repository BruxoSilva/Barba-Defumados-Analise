import { NextResponse } from 'next/server';
import { cepProvider } from '@/lib/cep';
export async function GET(_request:Request,{params}:{params:Promise<{cep:string}>}) {
 try {const {cep}=await params;return NextResponse.json(await cepProvider.lookup(cep));}
 catch(error){const message=error instanceof Error?error.message:'Erro ao consultar CEP';return NextResponse.json({error:message},{status:message==='CEP inválido'?400:message==='CEP não encontrado'?404:503});}
}
