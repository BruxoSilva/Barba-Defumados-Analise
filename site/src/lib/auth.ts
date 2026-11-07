import { compare } from 'bcryptjs';
import NextAuth, { type NextAuthOptions } from 'next-auth';
import CredentialsProvider from 'next-auth/providers/credentials';
import { db } from './db';

const admin=CredentialsProvider({id:'admin',name:'Administrador',credentials:{email:{label:'E-mail',type:'email'},password:{label:'Senha',type:'password'}},async authorize(credentials){
 if(!credentials?.email||!credentials.password)return null;
 const user=await db.user.findUnique({where:{email:credentials.email.trim().toLowerCase()}});
 if(!user||user.role!=='ADMIN'||!user.passwordHash||!await compare(credentials.password,user.passwordHash))return null;
 return {id:user.id,email:user.email,name:user.name,role:'ADMIN'};
}});
const customer=CredentialsProvider({id:'simulated',name:'Login simulado',credentials:{provider:{label:'Provedor',type:'text'},identifier:{label:'Identificador',type:'text'},code:{label:'Código',type:'text'}},async authorize(credentials){
 const provider=credentials?.provider;
 const identifier=credentials?.identifier?.trim().toLowerCase();
 if(!provider||!identifier||!['google','facebook','instagram','apple','phone'].includes(provider))return null;
 if(provider==='phone'){
  if(!/^\d{10,11}$/.test(identifier)||!credentials?.code)return null;
  const latest=await db.loginCode.findFirst({where:{phone:identifier,usedAt:null,expiresAt:{gt:new Date()}},orderBy:{createdAt:'desc'}});
  if(!latest||!await compare(credentials.code,latest.codeHash))return null;
  await db.loginCode.update({where:{id:latest.id},data:{usedAt:new Date()}});
 }else if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(identifier))return null;
 const email=provider==='phone'?`${identifier}@telefone.local`:identifier;
 const existing=await db.user.findUnique({where:{email}});
 if(existing?.role==='ADMIN')return null;
 const user=existing??await db.user.create({data:{email,name:email.split('@')[0],phone:provider==='phone'?identifier:null,provider,providerId:identifier}});
 return {id:user.id,email:user.email,name:user.name,role:'CUSTOMER'};
}});
export const authOptions:NextAuthOptions={
 providers:[admin,customer],session:{strategy:'jwt'},pages:{signIn:'/login'},callbacks:{jwt({token,user}){if(user){token.role=(user as {role?:string}).role;token.userId=user.id}return token},session({session,token}){(session as typeof session & {role?:string;userId?:string}).role=token.role as string;(session as typeof session & {userId?:string}).userId=token.userId as string;return session}},secret:process.env.NEXTAUTH_SECRET
};
export const authHandler=NextAuth(authOptions);
