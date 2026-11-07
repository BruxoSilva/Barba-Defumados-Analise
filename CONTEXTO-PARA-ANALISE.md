# Contexto e decisões mais recentes

O aplicativo principal deve continuar sendo um executável Windows com logo, fundo escuro e identidade em cobre. O cupom usa Elgin i7 USB, bobina 80 mm e área imprimível 72 mm, com texto rasterizado para manter os acentos.

## Correções solicitadas e ainda não executadas
1. Entrega: incorporar R$ 1,00/kg ao custo de fabricação de todos os produtos. Não somar frete no pedido, não exibir linha de entrega no cupom. O código atual ainda contém o comportamento anterior; esta decisão tem precedência sobre documentos antigos.
2. Cartão: manter cálculo preço / (1 - taxa), taxa padrão 5%, para receber o total líquido. Não entra no custo de fabricação.
3. Categorias: cada item deve usar o mesmo estilo compartilhado dos botões Nova categoria/Renomear/Excluir, com largura total e texto à esquerda; preservar ids, nomes, ordem, espaçamento e estados de foco/hover/seleção. Não foi implementada essa alteração específica.

## Estoque
Três etapas: matéria-prima, manipulação e comercialização. Reservas ao adicionar pacotes ao cupom; pagamento mantém reserva; entrega faz baixa; cancelamento/expiração devolve disponibilidade. Custos por lote congelados e FEFO. A implementação precisa de auditoria completa antes de produção; ver `desktop/docs/ESTADO-ESTOQUE.md`.

## Próximas etapas excluídas da implementação atual
Dashboard financeiro e agente de IA financeiro ainda não foram implementados.

## Dados
Backup completo preservado no computador. Foram detectados alertas de integridade no banco real de cadastros; nenhum banco real está neste repositório. Revisar migrações, transações e restauração com bancos sintéticos.
