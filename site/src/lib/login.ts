export type SimulatedProvider='google'|'facebook'|'instagram'|'apple'|'phone';
export interface IdentityAdapter { signIn(provider:SimulatedProvider,identifier:string):Promise<{provider:SimulatedProvider;identifier:string;simulated:true}> }
export const simulatedIdentity:IdentityAdapter={async signIn(provider,identifier){
 if (!['google','facebook','instagram','apple','phone'].includes(provider)) throw new Error('Provedor inválido');
 if (!identifier.trim()) throw new Error('Identificação obrigatória');
 return {provider,identifier:identifier.trim(),simulated:true};
}};
