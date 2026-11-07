import { expect,it } from 'vitest';import { canTransition } from './orders';
it('aceita fluxo de pedido e impede voltar ou cancelar entregue',()=>{expect(canTransition('RECEIVED','PREPARING')).toBe(true);expect(canTransition('PREPARING','DELIVERED')).toBe(false);expect(canTransition('DELIVERED','CANCELLED')).toBe(false)});
