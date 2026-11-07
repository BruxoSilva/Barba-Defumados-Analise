import { describe, expect, it } from 'vitest';
import { prepareReceipt, parseCents, formatCents } from './receipt';

const base = {
  customerName: 'Ana Silva', phone: '(34) 99999-9999', address: 'Rua A, 10',
  paymentMethod: 'cash', received: '30,00',
  items: [{ name: 'Costela BBQ', quantity: 2, unitPrice: '10,50' }],
};

describe('comprovante', () => {
  it('usa centavos inteiros e calcula o troco', () => {
    const receipt = prepareReceipt(base, new Date('2026-11-06T15:32:00-03:00'));
    expect(receipt.totalCents).toBe(2100);
    expect(receipt.changeCents).toBe(900);
    expect(formatCents(receipt.totalCents)).toBe('R$ 21,00');
    expect(receipt.date).toBe('06/11/2026');
    expect(receipt.time).toBe('15:32');
  });
  it('aceita Pix sem troco', () => {
    const receipt = prepareReceipt({ ...base, paymentMethod: 'pix', received: '' }, new Date());
    expect(receipt.changeCents).toBeNull();
  });
  it('recusa dinheiro insuficiente', () => {
    expect(() => prepareReceipt({ ...base, received: '20,99' }, new Date())).toThrow(/insuficiente/i);
  });
  it('recusa quantidade e preço inválidos', () => {
    expect(() => prepareReceipt({ ...base, items: [{ ...base.items[0], quantity: 0 }] }, new Date())).toThrow();
    expect(() => parseCents('1,999')).toThrow();
  });
  it('recusa cupom sem telefone e sem produto', () => {
    expect(() => prepareReceipt({ ...base, phone: '' }, new Date())).toThrow();
    expect(() => prepareReceipt({ ...base, items: [] }, new Date())).toThrow();
  });
});
