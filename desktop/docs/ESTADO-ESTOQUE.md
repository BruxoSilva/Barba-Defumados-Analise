# Estado do desenvolvimento do estoque

Implementação em desenvolvimento. Não substituir o executável de uso antes de concluir os itens abaixo.

## Verificado
- Testes de produção, custo congelado, FEFO, reservas, entrega e vencimento.
- Login local, hash persistente, backup e restauração.
- Regressões de clientes, produtos, peso decimal e QR Pix.

## Pendências de conclusão
- Concluído: cupom e vínculos de reservas gravados em transação; reimpressão da última emissão da sessão após falha de impressão.
- Relatórios completos de entradas, saídas, ajustes e perdas em todos os estoques, com filtros combinados consistentes.
- Concluído: perdas e inventário para matéria-prima e insumos, além dos pacotes prontos; precisa ampliar a apresentação de unidades nos relatórios.
- Tela de composição da receita com cálculo antecipado por kg, perdas previstas e versões preservadas.
- Concluído: múltiplos lotes de matéria-prima selecionáveis na mesma manipulação pela interface.
- Concluído: taxa de entrega aplicada ao pedido e persistida no cupom.
- Rastreabilidade consultável dos lotes e versões de preço de venda independentes do custo congelado.
- Previsão por peso/tamanho e prazo de validade real do produto; dias da semana em português.
- Revisar auditoria de todas as edições e ajustes de custo das perdas.
- Concluído: teste automatizado de abertura do formulário principal e de todas as abas em banco isolado; imagens em Tests/main-estoque.png e Tests/painel-estoque.png. Revisão de acessibilidade e fluxos completos ainda pendente.
- Publicar versão final na pasta app, conferir atalho e preservar banco real.

Pedido do usuário: desligar o computador somente após concluir todas as tarefas. Essa condição ainda não foi alcançada.

## Publicação de desenvolvimento
`staging-estoque/BarbaCupom.exe` foi publicado com sucesso. O executável de uso em `app` ainda não foi substituído, pois as pendências acima afetam o escopo solicitado. Última execução dos testes de regressão e de abertura da interface: passou.
