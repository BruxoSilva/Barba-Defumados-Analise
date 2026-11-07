export function assertMoney(value:number){if(!Number.isSafeInteger(value)||value<0)throw new Error('Valor monetário inválido')}
export function lineTotal(input:{qty:number;retailCents:number;wholesaleCents?:number|null;wholesaleMin?:number|null}){
 const {qty,retailCents,wholesaleCents,wholesaleMin}=input;
 if(!Number.isSafeInteger(qty)||qty<1)throw new Error('Quantidade inválida');
 assertMoney(retailCents);if(wholesaleCents!=null)assertMoney(wholesaleCents);
 const unit=wholesaleCents!=null&&wholesaleMin!=null&&qty>=wholesaleMin?wholesaleCents:retailCents;
 const total=unit*qty;if(!Number.isSafeInteger(total)||total>2147483647)throw new Error('Valor total acima do limite');return total;
}
export function orderTotals(input:{subtotalCents:number;discountCents:number;shippingCents:number;cashbackRequestedCents:number;cashbackAvailableCents:number}){
 Object.values(input).forEach(assertMoney);
 const discountCents=Math.min(input.discountCents,input.subtotalCents);
 const due=input.subtotalCents-discountCents+input.shippingCents;if(!Number.isSafeInteger(due)||due>2147483647)throw new Error('Valor total acima do limite');
 const cashbackUsedCents=Math.min(input.cashbackRequestedCents,input.cashbackAvailableCents,due);
 return {subtotalCents:input.subtotalCents,discountCents,shippingCents:input.shippingCents,cashbackUsedCents,totalCents:due-cashbackUsedCents};
}
export function canUseReferral(input:{ownerId:string;buyerId:string;samePhone:boolean;sameAddress:boolean;blockPhone:boolean;blockAddress:boolean}){
 return input.ownerId!==input.buyerId&&!(input.blockPhone&&input.samePhone)&&!(input.blockAddress&&input.sameAddress);
}
export function reservationExpiresAt(now:Date,minutes:number){if(!Number.isSafeInteger(minutes)||minutes<1)throw new Error('Prazo inválido');return new Date(now.getTime()+minutes*60000)}
