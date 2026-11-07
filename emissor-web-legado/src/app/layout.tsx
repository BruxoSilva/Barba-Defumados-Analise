import './globals.css';
export const metadata = { title: 'Barba Defumados · Emissor de cupons', description: 'Comprovante de pedido local' };
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return <html lang="pt-BR"><body><a className="skip-link" href="#conteudo">Ir para o conteúdo</a>{children}</body></html>;
}
