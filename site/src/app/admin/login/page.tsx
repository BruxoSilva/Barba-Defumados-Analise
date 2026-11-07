'use client';
import { signIn } from 'next-auth/react';
import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
export default function Login(){const [error,setError]=useState('');const [pending,setPending]=useState(false);return <main id="conteudo" className="admin-wrap"><Link href="/">← Ver como cliente</Link><h1>Modo administrador</h1><p>Acesse a caixa de saída simulada.</p><form className="admin-card" onSubmit={async e=>{e.preventDefault();setPending(true);setError('');const data=new FormData(e.currentTarget);const result=await signIn('admin',{email:data.get('email'),password:data.get('password'),redirect:false});setPending(false);if(result?.ok)location.href='/admin';else setError('E-mail ou senha inválidos.')}}><label className="field">E-mail<input name="email" type="email" autoComplete="username" spellCheck={false} required /></label><label className="field">Senha<input name="password" type="password" autoComplete="current-password" required /></label>{error&&<p role="alert">{error}</p>}<Button className="button" disabled={pending}>{pending?'Entrando…':'Entrar'}</Button></form></main>}

