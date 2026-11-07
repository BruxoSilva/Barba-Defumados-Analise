export function parseReais(raw:string):number{
 const value=raw.trim();
 if(!/^\d+(?:[,.]\d{1,2})?$/.test(value))throw new Error('Informe um valor em reais, com até duas casas decimais');
 const [whole,fraction='']=value.replace(',','.').split('.');
 const cents=Number(whole)*100+Number(fraction.padEnd(2,'0'));
 if(!Number.isSafeInteger(cents)||cents>2147483647)throw new Error('Valor em reais fora do limite permitido');
 return cents;
}
export function formatReaisInput(cents:number):string{return (cents/100).toFixed(2).replace('.',',')}
export function formatReais(cents:number):string{return new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(cents/100)}
