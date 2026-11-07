'use client';import { useEffect } from 'react';import { signOut } from 'next-auth/react';
export default function DeletedAccount(){useEffect(()=>{void signOut({callbackUrl:'/'})},[]);return <main id="conteudo" className="admin-wrap"><h1>Conta excluída</h1><p>Seus dados pessoais foram removidos. Encerrando a sessão…</p></main>}
