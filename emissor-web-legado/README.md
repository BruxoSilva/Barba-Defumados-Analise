# Barba Defumados — emissor local de cupons

Sistema independente para Windows. Após a instalação, funciona sem internet. Usa apenas a fila local `ELGIN i7(USB)`.

## Instalação

1. Nesta pasta, execute `npm install` e `npm run build` (a instalação das dependências pode precisar de internet uma vez).
2. Abra `iniciar.cmd` e acesse `http://127.0.0.1:3000` no navegador do próprio computador.
3. Mantenha a janela do servidor aberta enquanto usa o emissor.

Preencha cliente, telefone, endereço opcional, produtos, preços e forma de pagamento. Em dinheiro, informe o valor recebido para calcular o troco. Clique em **Emitir cupom não fiscal**. O servidor pega data e hora do computador nesse instante e envia o trabalho à impressora. A pré-visualização mostra data e hora apenas depois da emissão.

O formulário aceita até 12 produtos por cupom. Para finalizar o servidor, pressione `Ctrl+C` na janela do `iniciar.cmd`.

O papel é uma bobina de 80 mm; o driver informa 72 mm imprimíveis. O comprovante é para controle interno e não tem valor fiscal. Pix e dinheiro são apenas formas registradas no cupom; este programa não processa pagamentos.

## Verificação

- `npm test` — regras de cálculo, validação e troco.
- `npm run test:e2e` — formulário e pré-visualização no navegador.
- `npm run build` — compilação.

O projeto não acessa nem modifica o site `D:\Site barba defumados`.
