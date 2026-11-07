# Estoque do emissor desktop — plano de implementação

## Objetivo
Integrar ao emissor Windows um estoque local rastreável. Somente pacotes prontos, válidos e disponíveis podem entrar em cupons.

## Estrutura
- SQLite no banco atual, com migração aditiva e transações para movimentos, reserva, pagamento e entrega.
- Lotes de matéria-prima, manipulação e produto pronto; versões imutáveis de custos e preços.
- Painel Estoque no aplicativo com fornecedores, insumos, receitas, produção, perdas, reservas, relatórios, previsão e backup.
- Login local único com hash de senha e primeiro acesso.

## Etapas de verificação
1. Testes para conversão de aquisição, custo, perda e preços.
2. Testes para fluxo dos três estoques, rastreabilidade, FEFO, vencimento e reserva.
3. Testes para pagamento, entrega, cancelamento e relatório.
4. Testes para autenticação e cópia/restauração do SQLite.
5. Compilar, executar todos os testes e publicar o executável; validar uma sessão de interface.

## Decisões
- Pacotes prontos são indivisíveis no cupom; pesos flexíveis são definidos na produção.
- Pagamento é confirmado explicitamente. Emitir cupom não equivale a confirmar Pix.
- Dados antigos de produtos e cupons permanecem no banco como histórico; a seleção de novas vendas usa apenas o estoque pronto.
- Sem integração bancária ou dashboard financeiro nesta etapa.
