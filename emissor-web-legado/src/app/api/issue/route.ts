import { prepareReceipt } from '@/lib/receipt';
import { printReceipt } from '@/lib/printer';

export const runtime = 'nodejs';
export async function POST(request: Request) {
  const host = request.headers.get('host') || '';
  const origin = request.headers.get('origin') || '';
  if (!/^127\.0\.0\.1:\d+$/.test(host) || (origin && origin !== `http://${host}`)) {
    return Response.json({ error: 'Acesso permitido apenas neste computador.' }, { status: 403 });
  }
  if (!request.headers.get('content-type')?.startsWith('application/json')) {
    return Response.json({ error: 'Formato inválido.' }, { status: 415 });
  }
  try {
    const text = await request.text();
    if (text.length > 20000) return Response.json({ error: 'Dados acima do limite.' }, { status: 413 });
    const receipt = prepareReceipt(JSON.parse(text), new Date());
    await printReceipt(receipt);
    return Response.json({ receipt });
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Não foi possível emitir o cupom.' }, { status: 400 });
  }
}
