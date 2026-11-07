export type Address = { cep:string; rua:string; bairro:string; cidade:string; estado:string };
export interface CepProvider { lookup(cep:string):Promise<Address> }
export async function consultarCep(rawCep:string, fetcher:typeof fetch = fetch):Promise<Address> {
 const cep = rawCep.replace(/\D/g, '');
 if (!/^\d{8}$/.test(cep)) throw new Error('CEP inválido');
 const response = await fetcher(`https://viacep.com.br/ws/${cep}/json/`, { signal: AbortSignal.timeout(5000), cache:'no-store' });
 if (!response.ok) throw new Error('Serviço de CEP indisponível');
 const data:unknown = await response.json();
 if (!data || typeof data !== 'object' || 'erro' in data) throw new Error('CEP não encontrado');
 const d = data as Record<string,unknown>;
 if (typeof d.localidade !== 'string' || typeof d.uf !== 'string') throw new Error('Resposta de CEP inválida');
 return { cep, rua:String(d.logradouro ?? ''), bairro:String(d.bairro ?? ''), cidade:d.localidade, estado:d.uf };
}
export const cepProvider:CepProvider = { lookup:consultarCep };
