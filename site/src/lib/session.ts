import { getServerSession } from 'next-auth';
import { redirect } from 'next/navigation';
import { authOptions } from './auth';
import { db } from './db';
export async function currentSession(){return await getServerSession(authOptions) as ({role?:string;userId?:string;user?:{email?:string|null}}|null)}
export async function requireAdmin(){const session=await currentSession();if(session?.role!=='ADMIN'||!session.userId)redirect('/admin/login');return session.userId}
export async function requireCustomer(returnTo='/'){const session=await currentSession();if(!session?.userId)redirect(`/login?returnTo=${encodeURIComponent(returnTo)}`);const user=await db.user.findUnique({where:{id:session.userId},select:{deletedAt:true}});if(!user||user.deletedAt)redirect(`/login?returnTo=${encodeURIComponent(returnTo)}`);return session.userId}
