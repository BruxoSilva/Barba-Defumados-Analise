## Uso de skills

* Antes de cada módulo: use grill-me e brainstorming para tirar dúvidas, depois writing-plans e aguarde minha aprovação.
* Durante o desenvolvimento: use test-driven-development e react-best-practices.
* Na criação visual: use ui-ux-pro-max.
* Ao terminar cada módulo: revise com web-design-guidelines e security-best-practices, e teste com playwright.
* Antes de eu abrir uma conversa nova: use handoff para gerar o resumo.

# AGENTS.md — Regras permanentes do projeto
* 
* Este arquivo contém as regras fixas do projeto da loja de charcutaria e defumados.
* Leia este arquivo inteiro antes de qualquer tarefa. Em caso de conflito entre um pedido e estas regras, pergunte antes de agir.
* 
* \## 1. Modo de trabalho
* \- O projeto é desenvolvido em MÓDULOS. Trabalhe em um módulo por vez.
* \- Antes de cada módulo: use as skills grill-me e brainstorming para tirar dúvidas comigo. Depois use writing-plans e mostre o plano. NÃO escreva código antes de eu aprovar o plano.
* \- Durante o desenvolvimento: use test-driven-development (teste primeiro, código depois) e react-best-practices.
* \- Na criação visual: use ui-ux-pro-max para definir e seguir o sistema de design.
* \- Ao terminar cada módulo: revise com web-design-guidelines e security-best-practices, teste os fluxos com playwright e me entregue um resumo com o que foi feito, como testar e o que ficou pendente.
* \- Ao finalizar um módulo ou quando a conversa estiver longa: use handoff para gerar um resumo e atualize a seção "Estado do projeto" abaixo.
* \- Nunca avance para o próximo módulo sem minha aprovação explícita.
* \- Faça mudanças pequenas e focadas. Não reescreva arquivos inteiros sem necessidade. Não altere módulos já aprovados sem me avisar.
* \- Responda em português do Brasil.
* 
* \## 2. Ambiente 100% local (OBRIGATÓRIO)
* \- O sistema deve rodar localmente com `npm install`, `npm run setup` e `npm run dev`.
* \- Depois de instalado, o sistema deve funcionar SEM internet.
* \- PROIBIDO até minha aprovação final: APIs reais, serviços externos, CDNs, Google Fonts online, scripts externos, chaves de terceiros, links externos.
* \- Todo serviço externo deve ser SIMULADO (mock) atrás de um adaptador (interface), para ser trocado depois pelo serviço real apenas mudando configuração:
* &#x20; - Login social (Google, Facebook, Instagram, Apple/iCloud, telefone): tela de login simulada.
* &#x20; - Busca de CEP: base local de CEPs de teste.
* &#x20; - WhatsApp/SMS: "caixa de saída simulada" visível no painel administrativo, registrando cada mensagem que seria enviada.
* &#x20; - Pagamento (Pix, cartão, saldo de cashback): fluxo simulado com aprovação e recusa.
* &#x20; - Upload de imagens e vídeos: salvos em pasta local.
* \- Fontes, ícones e imagens devem ficar dentro do projeto.
* 
* \## 3. Tecnologias padrão
* \- Next.js (App Router) + TypeScript
* \- Tailwind CSS + shadcn/ui
* \- Prisma + SQLite (banco local)
* \- Auth.js (NextAuth) com provedores simulados
* \- Tarefas agendadas locais (ex.: node-cron) para expiração de carrinho, avisos e validade de cashback
* \- Vitest para testes unitários e Playwright para testes de ponta a ponta
* \- Se quiser trocar alguma tecnologia, explique o motivo e peça aprovação.
* 
* \## 4. Segurança e configuração
* \- Credenciais e configurações sensíveis ficam APENAS no arquivo `.env`, nunca no código.
* \- `.env` deve estar no `.gitignore`. Crie um `.env.example` sem valores reais.
* \- Senhas sempre com hash (bcrypt ou argon2).
* \- Validar todos os dados recebidos no servidor (ex.: zod).
* \- Proteger todas as rotas do painel administrativo no servidor, não só na interface.
* \- Toda regra de dinheiro (preço, desconto, frete, cashback, estoque) deve ser calculada no SERVIDOR, nunca confiando no navegador.
* \- Valores em dinheiro armazenados em centavos (inteiro).
* 
* \## 5. Regra de ouro: tudo editável pelo painel administrativo
* \- Nenhum texto, preço, taxa, prazo, porcentagem ou regra de negócio pode ficar fixo no código.
* \- Tudo deve ser configurável no painel administrativo e salvo no banco, com valores iniciais (seed).
* 
* \## 6. Padrão de código
* \- Código organizado por módulo (cliente, loja, admin, pedidos, cashback, estoque, notificações).
* \- Componentes reutilizáveis, nomes claros, sem código duplicado.
* \- Interface responsiva (celular primeiro) e acessível.
* \- Comentários em português apenas onde a lógica não for óbvia.
* 
* \## 7. Estado do projeto
* (Atualize esta seção ao fim de cada módulo: módulos concluídos, aprovados, pendências e decisões tomadas.)
* \- O usuário aprovou a execução integral sem pausas em 04/11/2026. Fundação, painel, cliente, loja, carrinho, checkout, pedidos, cashback, estoque e notificações locais foram implementados.
* \- Decisões: ViaCEP é a única API externa; endereço manual funciona sem conexão. Login é obrigatório ao clicar em produto. Peso e preço são fixos por embalagem; não há fracionamento.
* \- Verificações finais: build, 15 testes unitários, 11 testes Playwright e auditoria de dependências de produção passaram.
* \- Pendências conhecidas: substituir materiais provisórios; alguns rótulos da interface ainda estão nos componentes. Serviços e pagamentos reais dependem de aprovação futura.
* \- Consulte README.md para execução e HANDOFF.md para passagem de contexto.
* \- Atualização em 05/11/2026: todos os campos monetários visíveis e a exportação de vendas usam reais (R$), com vírgula decimal. O banco mantém centavos inteiros. Build, 22 testes unitários e 14 testes Playwright passaram.
* 


<!-- BEGIN:nextjs-agent-rules -->

## This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->
