import { describe, expect, it, vi } from 'vitest';
import { consultarCep } from './cep';
describe('consulta de CEP', () => {
  it('recusa formato inválido antes da rede', async () => {
    const fetcher = vi.fn();
    await expect(consultarCep('abc', fetcher)).rejects.toThrow('CEP inválido');
    expect(fetcher).not.toHaveBeenCalled();
  });
  it('normaliza o endereço do ViaCEP', async () => {
    const fetcher = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ cep:'01001-000', logradouro:'Praça da Sé', bairro:'Sé', localidade:'São Paulo', uf:'SP' }) });
    await expect(consultarCep('01001-000', fetcher)).resolves.toEqual({ cep:'01001000', rua:'Praça da Sé', bairro:'Sé', cidade:'São Paulo', estado:'SP' });
  });
  it('informa quando o CEP não existe', async () => {
    const fetcher = vi.fn().mockResolvedValue({ ok:true, json: async () => ({ erro:true }) });
    await expect(consultarCep('99999999', fetcher)).rejects.toThrow('CEP não encontrado');
  });
});
