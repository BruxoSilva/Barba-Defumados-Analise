# Barba Defumados — código para análise

Cópia preparada do backup local, sem bancos reais, credenciais, uploads de clientes, executáveis ou dependências instaladas.

## Componentes
- `desktop`: emissor Windows em C#/.NET 10 e SQLite; módulo de estoque em desenvolvimento.
- `emissor-web-legado`: versão web anterior do emissor.
- `site`: loja local Next.js e Prisma/SQLite.

## Para a IA revisora
Leia `CONTEXTO-PARA-ANALISE.md` antes de propor mudanças. Analise os arquivos e confirme os requisitos; não assuma que os recursos descritos na documentação estão completos. Os projetos originais e o backup integral não foram modificados.

## Verificação do desktop
No Windows com .NET 10: `dotnet run --project desktop/Tests/ReceiptTests.csproj`. O aplicativo recria um banco vazio; nenhum banco real acompanha esta cópia.

Os arquivos AGENTS e demais instruções em subpastas são documentos históricos dos projetos e devem ser interpretados em conjunto com a solicitação atual do proprietário.
