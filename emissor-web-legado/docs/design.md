# Emissor local de comprovante — desenho do módulo

## Objetivo

Aplicativo independente, instalado em `D:\Impressora\barba-cupom`, para preencher e imprimir comprovantes de pedido não fiscais na `ELGIN i7(USB)` da porta `COM4:`. Após a instalação, não usa internet.

## Interface

Uma tela inspirada na primeira imagem: fundo escuro, detalhes cobre, nome Barba Defumados BBQ, dados do cliente (nome, telefone, endereço), grade de produtos editáveis, total, pré-visualização e botões Imprimir e Limpar. Os dados de exemplo das imagens não serão gravados como pedidos. Data e hora não são campos editáveis: o sistema lê o relógio do computador no clique em Emitir cupom.

## Cupom

Composição inspirada na segunda imagem: marca no topo, títulos `CUPOM NÃO FISCAL` e `COMPROVANTE DE PEDIDO`, cliente, telefone, data, hora, endereço, produtos com quantidade, unidade, nome e total por item, total geral, forma de pagamento, troco quando em dinheiro e aviso de ausência de valor fiscal. Bobina física de 80 mm; conteúdo restrito à área imprimível de 72 mm. Logomarca monocromática local, simplificada para conservar legibilidade na cabeça térmica de 203 dpi. Pré-visualização e impressão devem usar o mesmo modelo de dados e a mesma composição.

## Regras

- Valores em centavos inteiros; total por item = preço unitário × quantidade; total geral = soma dos itens.
- Validar nome, telefone, pelo menos um produto, quantidade positiva e preço não negativo. Endereço pode ser informado ou deixado em branco para retirada.
- Data e hora obtidas do computador ao emitir; não confiar em data/hora do navegador.
- Pagamento informado como Pix ou dinheiro. Em dinheiro, exigir valor recebido igual ou maior que o total e exibir troco. O aplicativo só registra a forma informada, sem processar transações.
- Nenhum pagamento, emissão fiscal, serviço externo, cadastro de produtos ou histórico neste primeiro módulo.
- Erro de impressora deve aparecer na tela e não ser apresentado como impressão concluída.

## Plataforma

Projeto local separado em Next.js, React e TypeScript, com recursos visuais empacotados no próprio projeto. Serviço local no Windows envia o cupom para a fila `ELGIN i7(USB)` sem navegador externo nem serviço de terceiros. O aplicativo não altera o projeto da loja existente.
