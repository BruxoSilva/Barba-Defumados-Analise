# Passagem de contexto — loja local implementada

- Projeto: `D:\Site barba defumados`, Barba Defumados — Charcutaria em Geral.
- Decisão do usuário: aprovou a execução completa sem pausas. Produtos exigem login ao clique. Peso e preço são fixos por embalagem; não há fracionamento.
- Stack: Next.js 16, TypeScript, React 19, Tailwind 4, shadcn/ui, Prisma 6/SQLite, Auth.js, Vitest 5, Playwright e node-cron.
- Execução: `npm install`, preencher `.env` a partir de `.env.example`, `npm run setup`, `npm run dev`.
- Banco de uso: `prisma/shop.db`. Testes E2E usam `prisma/test.db`, `uploads-test/` e `backups-test/` separados. Restos anteriores em `prisma/dev.db`, `uploads/` e `backups/` não são usados pelo banco atual.
- Funcionalidades: vitrine, login simulado, perfil e endereço, CEP ViaCEP/manual, carrinho e reserva, checkout e pagamento simulados, pedidos, convites/cashback, avaliações, lista de espera, feedback; painel de configurações, produtos/mídia, categorias, estoque, kits, campanhas, frete, pedidos, moderação, relatórios/CSV, mensagens simuladas, auditoria e backup.
- Segurança: painel protegido no servidor, senhas bcrypt, validação Zod, dinheiro em centavos, valores calculados no servidor, `.env` ignorado pelo Git, exclusão de conta com anonimização.
- Verificação final: `npm run setup` e `npm run build` passaram; 17 testes unitários passaram. Após correção do cadastro de categorias, os 2 fluxos Playwright de categoria passaram; antes dela, a suíte completa tinha 11 testes aprovados.
- Correção posterior: o identificador da categoria agora é gerado a partir do nome quando vazio, aceita acentos/espaços e mostra aviso para endereço repetido, sem erro 500.
- Correção de produto: identificador automático com acentos/espaços; campos de atacado exigem preço e quantidade mínima juntos e exibem orientação em vez de erro 500. Verificados 19 testes unitários, build e 2 fluxos Playwright focados.
- Conversão de interface: entradas monetárias de produto, campanhas, frete, configurações e checkout agora aceitam reais; CSV de vendas exporta reais. O banco e as regras internas seguem em centavos. Testes unitários e Playwright cobrem conversão e telas.
- Limites: simulação local de serviços e pagamentos; ViaCEP requer internet; alguns rótulos de interface ainda estão no código. O banco atual contém apenas dados iniciais.
