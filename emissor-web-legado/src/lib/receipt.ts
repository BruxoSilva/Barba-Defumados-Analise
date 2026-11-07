import { z } from 'zod';

const itemSchema = z.object({
  name: z.string().trim().min(1).max(60),
  quantity: z.number().int().min(1).max(999),
  unitPrice: z.string(),
});

const inputSchema = z.object({
  customerName: z.string().trim().min(1).max(100),
  phone: z.string().trim().min(8).max(30),
  address: z.string().trim().max(120),
  paymentMethod: z.enum(['pix', 'cash']),
  received: z.string(),
  items: z.array(itemSchema).min(1).max(12),
});

export type ReceiptInput = z.input<typeof inputSchema>;
export type Receipt = {
  customerName: string; phone: string; address: string;
  paymentMethod: 'pix' | 'cash'; receivedCents: number | null;
  changeCents: number | null; totalCents: number;
  date: string; time: string;
  items: { name: string; quantity: number; unitPriceCents: number; totalCents: number }[];
};

export function parseCents(value: string): number {
  const cleaned = value.trim().replace(/^R\$\s*/, '').replace(/\s/g, '');
  if (!/^(?:\d{1,3}(?:\.\d{3})+|\d+)(?:,\d{1,2})?$/.test(cleaned)) throw new Error('Informe um valor em reais válido.');
  const [whole, fraction = ''] = cleaned.replace(/\./g, '').split(',');
  const cents = Number(whole) * 100 + Number(fraction.padEnd(2, '0'));
  if (!Number.isSafeInteger(cents) || cents > 100_000_000) throw new Error('Valor acima do limite permitido.');
  return cents;
}

export function formatCents(cents: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(cents / 100).replace(/\u00a0/g, ' ');
}

export function prepareReceipt(raw: unknown, now: Date): Receipt {
  const input = inputSchema.parse(raw);
  if (input.phone.replace(/\D/g, '').length < 8) throw new Error('Informe um telefone válido.');
  const items = input.items.map(item => {
    const unitPriceCents = parseCents(item.unitPrice);
    return { name: item.name, quantity: item.quantity, unitPriceCents, totalCents: unitPriceCents * item.quantity };
  });
  const totalCents = items.reduce((sum, item) => sum + item.totalCents, 0);
  if (totalCents <= 0 || totalCents > 100_000_000) throw new Error('O total precisa ser maior que zero e dentro do limite.');
  const receivedCents = input.paymentMethod === 'cash' ? parseCents(input.received) : null;
  if (receivedCents !== null && receivedCents < totalCents) throw new Error('Valor recebido insuficiente para o pagamento em dinheiro.');
  return {
    customerName: input.customerName, phone: input.phone, address: input.address,
    paymentMethod: input.paymentMethod, receivedCents,
    changeCents: receivedCents === null ? null : receivedCents - totalCents,
    totalCents, items,
    date: new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(now),
    time: new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', hour12: false }).format(now),
  };
}
