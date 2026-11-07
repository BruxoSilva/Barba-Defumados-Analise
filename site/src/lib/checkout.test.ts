import { expect,it } from 'vitest';import { promotionDiscount,resolveShipping } from './checkout';
it('limita desconto promocional ao subtotal',()=>{expect(promotionDiscount(1000,{percentBps:5000,amountCents:null})).toBe(500);expect(promotionDiscount(1000,{percentBps:null,amountCents:2000})).toBe(1000)});
it('calcula frete por bairro antes da taxa padrão',()=>{expect(resolveShipping('Centro','01001000',[{neighborhood:'Centro',cepStart:null,cepEnd:null,priceCents:900}],1500)).toBe(900)});
