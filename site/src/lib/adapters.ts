import { db } from './db';
import { resolve } from 'node:path';
export interface NotificationAdapter { send(recipient:string, body:string, channel:'WHATSAPP'|'SMS'):Promise<void> }
export const simulatedNotifications:NotificationAdapter = { async send(recipient,body,channel) { await db.outboxMessage.create({data:{recipient,body,channel}}); } };
export interface PaymentAdapter { charge(amountCents:number, outcome:'approve'|'decline'):Promise<{approved:boolean}> }
export const simulatedPayment:PaymentAdapter = { async charge(amountCents,outcome) { if (!Number.isSafeInteger(amountCents) || amountCents < 0) throw new Error('Valor inválido'); return {approved:outcome==='approve'}; } };
export interface UploadAdapter { save(name:string, bytes:Uint8Array):Promise<string> }
export function uploadRoot(){return resolve(process.cwd(),process.env.UPLOAD_DIR??'uploads')}
export const localUpload:UploadAdapter = { async save(name,bytes) { const {mkdir,writeFile}=await import('node:fs/promises'); const {join,basename}=await import('node:path'); const safe=basename(name).replace(/[^a-zA-Z0-9._-]/g,'_'); if (!safe || safe.startsWith('.')) throw new Error('Arquivo inválido'); const dir=uploadRoot(); await mkdir(dir,{recursive:true}); const target=join(dir,`${crypto.randomUUID()}-${safe}`); await writeFile(target,bytes); return target; } };
export async function removeLocalUpload(path:string){const {resolve,sep}=await import('node:path');const {unlink}=await import('node:fs/promises');const base=uploadRoot();const target=resolve(path);if(!target.startsWith(base+sep))throw new Error('Arquivo fora da pasta local');await unlink(target).catch(error=>{if((error as NodeJS.ErrnoException).code!=='ENOENT')throw error})}
