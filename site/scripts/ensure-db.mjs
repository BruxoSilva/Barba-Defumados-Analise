import 'dotenv/config';
import { mkdir,open } from 'node:fs/promises';
import { join } from 'node:path';
const match=/^file:\.\/([A-Za-z0-9_.-]+\.db)$/.exec(process.env.DATABASE_URL??'');
if(!match)throw new Error('DATABASE_URL deve apontar para um arquivo .db local em prisma/');
const dir=join(process.cwd(),'prisma');await mkdir(dir,{recursive:true});
const handle=await open(join(dir,match[1]),'a');await handle.close();
