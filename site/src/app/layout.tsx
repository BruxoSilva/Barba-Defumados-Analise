import type { Metadata } from 'next';
import './globals.css';
import { feedbackAction } from './feedback-action';
import { db } from '@/lib/db';
import type { CSSProperties } from 'react';
export const metadata:Metadata={title:'Barba Defumados',description:'Charcutaria em Geral'};
export default async function RootLayout({children}:{children:React.ReactNode}) {const settings=await db.setting.findMany({where:{key:{in:['store.feedbackLabel','store.feedbackPrompt','brand.copper','brand.forest','brand.cream','brand.paper']}}});const values=Object.fromEntries(settings.map(s=>[s.key,s.value]));const color=(key:string,fallback:string)=>/^#[0-9a-fA-F]{6}$/.test(values[key]??'')?values[key]:fallback;const style={'--copper':color('brand.copper','#ac643e'),'--forest':color('brand.forest','#263227'),'--cream':color('brand.cream','#f6f0e4'),'--paper':color('brand.paper','#fbf8f1')} as CSSProperties;return <html lang="pt-BR" data-scroll-behavior="smooth" style={style}><body><a className="skip-link" href="#conteudo">Pular para o conteúdo</a>{children}<details className="feedback"><summary>{values['store.feedbackLabel']}</summary><form action={feedbackAction}><label className="field">{values['store.feedbackPrompt']}<textarea name="text" minLength={5} maxLength={2000} required/></label><button className="button">Enviar</button></form></details></body></html>}
