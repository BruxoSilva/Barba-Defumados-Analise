import { expect, it } from 'vitest';
import { simulatedIdentity } from './login';
it('aceita identidades de teste sem chamar provedores externos',async()=>{
 const result=await simulatedIdentity.signIn('google','cliente1@teste.local');
 expect(result).toEqual({provider:'google',identifier:'cliente1@teste.local',simulated:true});
});
