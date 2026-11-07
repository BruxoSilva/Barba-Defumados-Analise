import { describe, expect, it } from 'vitest';
import { lineTotal, orderTotals, canUseReferral, reservationExpiresAt } from './commerce';
describe('regras de compra',()=>{
 it('aplica atacado por quantidade inteira',()=>{expect(lineTotal({qty:3,retailCents:2000,wholesaleCents:1500,wholesaleMin:3})).toBe(4500);expect(lineTotal({qty:2,retailCents:2000,wholesaleCents:1500,wholesaleMin:3})).toBe(4000)});
 it('calcula total no servidor e limita cashback usado',()=>{expect(orderTotals({subtotalCents:10000,discountCents:1000,shippingCents:1500,cashbackRequestedCents:20000,cashbackAvailableCents:3000})).toEqual({subtotalCents:10000,discountCents:1000,shippingCents:1500,cashbackUsedCents:3000,totalCents:7500})});
 it('bloqueia autoindicação por conta, telefone e endereço',()=>{expect(canUseReferral({ownerId:'a',buyerId:'a',samePhone:false,sameAddress:false,blockPhone:true,blockAddress:true})).toBe(false);expect(canUseReferral({ownerId:'a',buyerId:'b',samePhone:true,sameAddress:false,blockPhone:true,blockAddress:true})).toBe(false)});
 it('calcula expiração por minutos configurados',()=>{expect(reservationExpiresAt(new Date('2026-01-01T00:00:00Z'),360).toISOString()).toBe('2026-01-01T06:00:00.000Z')});
});
