import { expect, it } from 'vitest';
import { parseConfigValue } from './config';
it('converte valores configuráveis com tipos seguros', () => {
  expect(parseConfigValue('true','boolean')).toBe(true);
  expect(parseConfigValue('360','integer')).toBe(360);
  expect(() => parseConfigValue('3.5','integer')).toThrow();
});
