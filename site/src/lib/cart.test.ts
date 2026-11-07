import { expect, it } from 'vitest';
import { allocateLots } from './cart';
it('reserva unidades exatas dos lotes válidos por validade',()=>{const now=new Date('2026-01-01T00:00:00Z');expect(allocateLots([{id:'b',quantityUnits:3,expiresAt:new Date('2026-04-01')},{id:'a',quantityUnits:2,expiresAt:new Date('2026-02-01')},{id:'x',quantityUnits:10,expiresAt:new Date('2025-01-01')}],4,now)).toEqual([{lotId:'a',quantityUnits:2},{lotId:'b',quantityUnits:2}]);expect(()=>allocateLots([],1,now)).toThrow('Estoque insuficiente')});
