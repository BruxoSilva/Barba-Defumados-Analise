# Emissor local de cupom Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar uma tela local simples que calcula e imprime comprovantes não fiscais na Elgin i7.

**Architecture:** Um projeto Next.js independente mantém formulário e pré-visualização no navegador local. Uma ação de servidor valida os dados e usa um adaptador Windows de impressão; a composição do cupom é compartilhada entre pré-visualização e envio à impressora.

**Tech Stack:** Next.js, React, TypeScript, Tailwind CSS, Vitest, Playwright e API local de impressão do Windows.

**Spec:** `D:\Impressora\barba-cupom\docs\design.md`

## Global Constraints

- Pasta separada: `D:\Impressora\barba-cupom`.
- Depois da instalação, execução sem internet ou serviços externos.
- Impressora: `ELGIN i7(USB)` em `COM4:`; bobina 80 mm, área imprimível 72 mm.
- Comprovante explicitamente não fiscal.
- Não modificar `D:\Site barba defumados`.

## Review Focus

- Nome ou endereço longo deve quebrar linha sem cortar a lateral do cupom.
- Valores como `10,99` devem permanecer corretos em centavos.
- Quantidade inválida não pode produzir total ou impressão.
- Impressora desconectada deve gerar erro visível.
- Texto com acentos deve ficar legível na impressora.

---

### Task 1: Estrutura local e cálculo

**Files:** `package.json`, `src/lib/receipt.ts`, `src/lib/receipt.test.ts`, configuração do projeto.

- [ ] Escrever testes de validação, centavos, total, troco e data/hora emitidas pelo servidor; verificar falha.
- [ ] Implementar o modelo mínimo e executar os testes até passarem.
- [ ] Verificar instalação e inicialização local sem dependências remotas em tempo de execução.

### Task 2: Tela de preenchimento e pré-visualização

**Files:** `src/app/page.tsx`, `src/app/globals.css`, `src/components/receipt-form.tsx`, `src/components/receipt-preview.tsx`, assets locais.

- [ ] Executar pesquisas das skills `modern-web-guidance` e `ui-ux-pro-max` antes do HTML/CSS.
- [ ] Escrever testes para adição/remoção de produto, telefone, pagamento e total; verificar falha.
- [ ] Implementar interface responsiva semelhante à referência, com campos rotulados e navegação por teclado.
- [ ] Verificar que a pré-visualização reflete os dados digitados e não usa os valores fictícios da imagem.

### Task 3: Impressão na Elgin

**Files:** `src/lib/printer.ts`, `src/lib/printer.test.ts`, `src/app/actions.ts`, `scripts/print-receipt.ps1`.

- [ ] Escrever testes para payload, validação no servidor, largura de 72 mm e falha de impressora; verificar falha.
- [ ] Implementar adaptador Windows com chamada parametrizada e sem execução de texto do usuário como comando.
- [ ] Fazer impressão física de um cupom de teste e comparar as bordas e linhas com a referência.

### Task 4: Verificação e entrega

**Files:** `tests/receipt.spec.ts`, `README.md`.

- [ ] Testar preenchimento, pré-visualização, limpeza e mensagens de erro com Playwright.
- [ ] Revisar interface com `web-design-guidelines` e segurança do fluxo de impressão.
- [ ] Executar testes unitários e build; registrar como iniciar e como selecionar a impressora.
- [ ] Entregar resumo, evidência dos testes e ajustes necessários após conferir o cupom físico.
