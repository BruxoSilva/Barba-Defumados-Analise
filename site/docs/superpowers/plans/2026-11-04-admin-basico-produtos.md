# Painel básico e acesso aos produtos — Plano de implementação

> **Para implementação:** executar as tarefas em ordem, com teste que falha antes do código, verificação após cada tarefa e revisão final. Este plano depende de aprovação explícita do usuário.

**Objetivo:** Permitir ao administrador editar configurações, categorias, produtos, mídias e estoque por lote; exigir login simulado antes de abrir qualquer produto da vitrine.

**Arquitetura:** Usar formulários e ações no servidor com validação Zod, autorização administrativa no servidor e Prisma/SQLite para persistência. Cada produto representa uma embalagem de peso fixo, vendida em quantidades inteiras. O clique no produto leva a uma rota de detalhes protegida por sessão de cliente ou administrador; o login de clientes é simulado nesta fase.

**Tecnologias:** Next.js App Router, TypeScript, Tailwind, shadcn/ui, Prisma, SQLite, Auth.js, Zod, Vitest e Playwright.

**Referências:** `AGENTS.md`, pedido inicial anexado e decisão do usuário de 04/11/2026 sobre peso fixo e login ao clicar no produto.

## Restrições

- Não iniciar código antes da aprovação deste plano.
- Tudo permanece local, exceto ViaCEP já aprovado. Nenhum CDN, fonte ou script externo.
- Preços em centavos inteiros; peso por embalagem em gramas inteiros; quantidade de estoque em unidades inteiras. Não há venda fracionada por KG.
- Configurações, textos, preços, regras, peso e mídias ficam editáveis no painel e persistidos no SQLite.
- Rotas e mutações administrativas verificam a sessão e o papel `ADMIN` no servidor; entradas recebem validação Zod.
- Não modificar fluxos já aprovados além das ligações necessárias à edição e ao login antes de produtos.

## Pontos de atenção nos testes

- Preço ou peso inválido: impedir gravação e mostrar erro junto ao campo.
- Dois produtos com o mesmo identificador de URL: impedir colisão e preservar o produto existente.
- Baixa de estoque maior que o disponível: rejeitar sem alterar saldo ou histórico.
- Lote vencido ou inativo: mostrar no painel, mas não somar como disponível para venda.
- Acesso direto a `/produtos/[slug]` sem sessão e requisições administrativas forjadas: redirecionar ou negar no servidor.

## Estrutura prevista

- `prisma/schema.prisma`: dados de produto, mídia, lote, movimentação de estoque e autenticação simulada do cliente.
- `prisma/seed.ts`: novos dados iniciais e migração idempotente.
- `src/lib/admin-auth.ts`: guarda compartilhada para ações e páginas administrativas.
- `src/lib/admin/settings.ts`, `categories.ts`, `products.ts`, `inventory.ts`, `media.ts`: validação e regras de cada domínio.
- `src/app/admin/...`: páginas e formulários de edição.
- `src/app/login/...` e `src/app/produtos/[slug]/...`: login simulado e detalhes protegidos.
- `src/components/ui/...`: componentes shadcn/ui necessários aos formulários.
- `src/**/*.test.ts` e `tests/*.spec.ts`: testes unitários e de ponta a ponta.

## Tarefa 1 — Base de autorização e modelos

- [ ] Escrever testes que negam mutações sem `ADMIN`, validam preço/peso inteiros e verificam relações de lote e movimentação.
- [ ] Confirmar as falhas esperadas.
- [ ] Ampliar o schema e criar `requireAdmin()` em `src/lib/admin-auth.ts`; manter o seed repetível.
- [ ] Passar testes, `npm run setup` e `npx tsc --noEmit`.

## Tarefa 2 — Configurações editáveis

- [ ] Testar validação de tipos, atualização permitida e rejeição de chave desconhecida ou usuário não administrador.
- [ ] Confirmar as falhas.
- [ ] Criar `updateSetting(key: string, rawValue: unknown)` em `src/lib/admin/settings.ts` e tela `/admin/configuracoes`, organizada por grupos e com confirmação de salvamento.
- [ ] Passar testes e verificar que uma mudança de texto aparece na vitrine após recarregar.

## Tarefa 3 — Categorias

- [ ] Testar criação, edição, ordenação, ativação, desativação e colisões de nome/slug.
- [ ] Confirmar as falhas.
- [ ] Criar ações em `src/lib/admin/categories.ts` e interface `/admin/categorias`.
- [ ] Passar testes e conferir a ordem e visibilidade na vitrine.

## Tarefa 4 — Produtos e mídias locais

- [ ] Testar criação, edição, duplicação, desativação e validação de preço de varejo, atacado, mínimo, peso fixo, unidade e campos descritivos.
- [ ] Testar upload com tipo/tamanho permitidos, nome seguro e associação da foto principal, fotos adicionais e vídeos ao produto.
- [ ] Confirmar as falhas.
- [ ] Criar `src/lib/admin/products.ts` e `media.ts`, rotas de upload protegidas e telas `/admin/produtos` e `/admin/produtos/[id]`.
- [ ] Usar arquivos locais e ordenar mídias por posição; passar testes e conferir a vitrine atualizada.

## Tarefa 5 — Estoque por lote

- [ ] Testar entrada e saída em unidades inteiras, rejeição de saldo negativo, lote vencido, alerta de estoque baixo e registro de quem alterou.
- [ ] Confirmar as falhas.
- [ ] Criar `adjustStock(productId: string, lotId: string, deltaUnits: number, actorId: string)` em `src/lib/admin/inventory.ts`, com transação Prisma, e tela `/admin/estoque`.
- [ ] Passar testes e conferir saldos, lotes, validade e histórico no painel.

## Tarefa 6 — Login obrigatório ao clicar em produto

- [ ] Testar clique na vitrine sem sessão, login simulado e retorno ao produto; testar acesso direto à rota e sessão de outro usuário.
- [ ] Confirmar as falhas.
- [ ] Criar login simulado de cliente em Auth.js para os provedores previstos, preservando `returnTo` interno seguro; tornar os cards links para `/produtos/[slug]` e proteger a página de detalhes no servidor.
- [ ] Mostrar os dados disponíveis do produto, peso fixo, preço e mídias; deixar a ampliação visual da página para o módulo D.
- [ ] Passar testes unitários e Playwright em desktop e celular.

## Tarefa 7 — Revisão e entrega

- [ ] Revisar acessibilidade e responsividade pelas regras de `web-design-guidelines` e `ui-ux-pro-max`.
- [ ] Revisar autorização, upload, validação e dependências; executar `npm audit`.
- [ ] Executar `npm run setup`, `npm test`, `npm run test:e2e` e `npm run build`.
- [ ] Atualizar `README.md`, `HANDOFF.md` e a seção Estado do projeto em `AGENTS.md`; entregar resumo, passos de teste e pendências.
- [ ] Parar e aguardar aprovação do módulo antes de avançar.

## Fora deste módulo

Carrinho, reserva de estoque, pagamento, checkout, cadastro completo do cliente, cashback, avaliações, campanhas e relatórios avançados permanecem nos módulos aprovados para essas funções.
