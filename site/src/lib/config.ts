import { db } from './db';
export function parseConfigValue(value: string, type: string): string | number | boolean {
  if (type === 'boolean') { if (value !== 'true' && value !== 'false') throw new Error('Configuração booleana inválida'); return value === 'true'; }
  if (type === 'integer') { const n = Number(value); if (!Number.isSafeInteger(n)) throw new Error('Configuração inteira inválida'); return n; }
  return value;
}
export async function getSetting(key: string): Promise<string> {
  const setting = await db.setting.findUnique({ where: { key } });
  if (!setting) throw new Error(`Configuração ausente: ${key}`);
  return setting.value;
}
