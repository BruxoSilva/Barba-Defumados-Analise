import { db } from './db';
import { expireCarts,warnCarts } from './cart';
import { getSetting } from './config';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
export async function expireCashback(now=new Date()){await db.cashbackEntry.updateMany({where:{status:'AVAILABLE',expiresAt:{lte:now}},data:{status:'EXPIRED',remainingCents:0}})}
export async function warnCashback(now=new Date()){const days=Number(await getSetting('cashback.warningDays'));const deadline=new Date(now.getTime()+days*86400000);const template=await getSetting('messages.cashbackExpiry');const entries=await db.cashbackEntry.findMany({where:{status:'AVAILABLE',remainingCents:{gt:0},warnedAt:null,expiresAt:{gt:now,lte:deadline}},include:{user:true}});for(const entry of entries)await db.$transaction(async tx=>{const changed=await tx.cashbackEntry.updateMany({where:{id:entry.id,warnedAt:null},data:{warnedAt:now}});if(changed.count&&entry.user.phone)await tx.outboxMessage.create({data:{channel:'WHATSAPP',recipient:entry.user.phone,body:template}})})}
export async function backupDatabase(){const dir=join(process.cwd(),process.env.BACKUP_DIR??'backups');await mkdir(dir,{recursive:true});const file=join(dir,`barba-${new Date().toISOString().replace(/[:.]/g,'-')}.db`).replaceAll('\\','/');await db.$executeRawUnsafe(`VACUUM INTO '${file.replaceAll("'","''")}'`);return file}
export async function runMinuteJobs(){await expireCarts();await warnCarts();await expireCashback();await warnCashback()}
