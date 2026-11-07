import {describe,it,expect} from 'vitest';
import {categoryInput} from './validation';

describe('identificador de categoria',()=>{
 it('gera identificador a partir do nome com acentos',()=>{
  expect(categoryInput.parse({name:'Linguiças Artesanais',slug:'',position:0,active:true}).slug).toBe('linguicas-artesanais');
 });
 it('normaliza identificador informado com espaços',()=>{
  expect(categoryInput.parse({name:'Teste',slug:'  Peças Defumadas  ',position:0,active:true}).slug).toBe('pecas-defumadas');
 });
});
