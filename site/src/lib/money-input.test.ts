import {expect,it} from 'vitest';
import {parseReais,formatReaisInput} from './money-input';

it('converte reais com vírgula para centavos exatos',()=>{expect(parseReais('12,34')).toBe(1234);expect(parseReais('12.34')).toBe(1234);expect(parseReais('0')).toBe(0)});
it('rejeita valores monetários ambíguos ou negativos',()=>{expect(()=>parseReais('12,345')).toThrow();expect(()=>parseReais('-1')).toThrow();expect(()=>parseReais('abc')).toThrow()});
it('formata centavos como valor de entrada em reais',()=>{expect(formatReaisInput(1234)).toBe('12,34')});
