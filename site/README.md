# Barba Defumados

Loja local de charcutaria e defumados, com produtos de peso fixo e venda por unidade de embalagem. Os dados de demonstração são provisórios. O ViaCEP é a integração externa aprovada; há preenchimento manual se a consulta falhar. Login social, mensagens e pagamentos são simulados.

## Iniciar

1. Execute `npm install`.
2. Copie `.env.example` para `.env`. Informe `ADMIN_EMAIL`, `ADMIN_PASSWORD` e um `NEXTAUTH_SECRET` longo e aleatório.
3. Execute `npm run setup`.
4. Execute `npm run dev` e abra `http://localhost:3000`.

O setup cria `prisma/shop.db` e os dados de exemplo. Pode ser repetido. Após a instalação, a loja funciona sem internet, exceto a consulta opcional ao ViaCEP. Os demais serviços externos usam simulações locais.

## Roteiro de teste

1. Na vitrine `/`, clique em um produto: a página solicita login antes de abrir os detalhes. Use o login simulado; o código aparece na tela e é registrado na caixa de saída do administrador.
2. Em `/minha-conta`, complete o perfil e informe o endereço. Teste o CEP `01001000` com internet e o preenchimento manual sem conexão.
3. Adicione um produto ao carrinho, confira o tempo de reserva, vá ao checkout e escolha um resultado de pagamento simulado. Veja o pedido e o histórico na conta.
4. Entre em `/admin/login` com as credenciais do `.env`. Teste categorias, produtos, imagens, lotes de estoque, kits, campanhas, frete, configurações, pedidos, avaliações, relatórios, exportação CSV, caixa de saída e backup.
5. Teste convite e cashback na página `/minha-conta/convites`, com uma segunda conta simulada.

O preço, o peso da embalagem e a unidade de venda são cadastrados por produto no painel. Não há venda fracionada. Valores monetários são guardados em centavos e calculados no servidor.
Nos formulários, informe valores em reais, por exemplo `25,90`. O painel e a exportação de vendas também exibem reais.

## Verificações automáticas

- `npm test`: testes unitários.
- `npm run test:e2e`: fluxos Playwright em banco e pastas de mídia separados do uso normal. Se necessário, instale o Chromium com `npx playwright install chromium`.
- `npm run build`: compilação de produção.

Os arquivos enviados ficam em `uploads/` e os backups em `backups/`. Esses diretórios e os bancos locais não entram no Git. As tarefas locais verificam expiração de carrinho e cashback; o backup agendado é diário enquanto o servidor estiver em execução.

## Limites atuais

- A loja é uma demonstração local. Nenhum pagamento ou mensagem real é enviado.
- A consulta ao ViaCEP precisa de internet; o endereço pode ser informado manualmente.
- Alguns rótulos de interface ainda são definidos nos componentes. Conteúdo comercial, preços, taxas, prazos e regras principais podem ser alterados pelo painel.
