import { expect, it } from 'vitest';
import { productInput, stockInput, categoryInput } from './validation';
it('aceita apenas produtos de embalagem fixa com valores inteiros',()=>{
 expect(productInput.parse({name:'Pancetta',slug:'pancetta',categoryId:'abc',description:'Boa',priceCents:2000,weightGrams:500,saleUnit:'UN'}).weightGrams).toBe(500);
 expect(()=>productInput.parse({name:'Pancetta',slug:'pancetta',categoryId:'abc',description:'Boa',priceCents:2000,weightGrams:0,saleUnit:'KG'})).toThrow();
});
it('recusa saldo fracionado e categoria inválida',()=>{
 expect(()=>stockInput.parse({lotId:'abc',deltaUnits:0.5,reason:'ajuste'})).toThrow();
 expect(()=>categoryInput.parse({name:'',slug:'a',position:0})).toThrow();
});
