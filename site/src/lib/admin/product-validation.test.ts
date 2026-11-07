import {expect,it} from 'vitest';
import {productInput} from './validation';

const product={name:'Linguiça Artesanal',slug:'',categoryId:'categoria',description:'Defumada',priceCents:2500,wholesalePriceCents:null,wholesaleMinQty:null,weightGrams:500,saleUnit:'UN' as const,lowStockThreshold:5,active:true};
it('gera endereço curto de produto a partir do nome',()=>{expect(productInput.parse(product).slug).toBe('linguica-artesanal')});
it('exige preço e mínimo de atacado juntos',()=>{expect(productInput.safeParse({...product,wholesalePriceCents:2000}).success).toBe(false)});
