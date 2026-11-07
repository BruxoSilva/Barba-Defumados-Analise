# Barba Defumados — emissor de cupom para Windows

Aplicativo nativo, sem navegador e sem conexão com a internet. O atalho **Barba Defumados - Cupom** na área de trabalho abre `app\BarbaCupom.exe`.

Preencha o nome do cliente, os produtos, quantidades e preços. Telefone e endereço podem ficar em branco; ao pré-visualizar ou emitir sem algum desses dados, o programa pede confirmação. Para venda por peso, selecione **KG** ou digite diretamente um peso decimal, com ponto ou vírgula e até três casas (ex.: `0.5`, `0,5`, `1.2`, `1,2`); o total é calculado pelo preço por quilo e arredondado para centavos. Para venda por unidade, mantenha **UN** e informe a quantidade inteira e o preço por unidade. Escolha Pix ou dinheiro; para dinheiro, informe o valor recebido. **Pré-visualizar** mostra o cupom sem registrar data e hora. **Emitir cupom** registra a data e a hora atuais do computador e envia o bitmap à fila `ELGIN i7(USB)`.

Quando o nome é novo, o programa pergunta **antes de imprimir** se deseja cadastrar o cliente. Se escolher **Não**, imprime sem cadastrar; **Cancelar** volta ao formulário. Ao selecionar um cliente salvo na lista do campo Nome, os dados são preenchidos e a pergunta não aparece. O botão **Clientes cadastrados** abre a pesquisa por nome ou telefone, mostra o número de compras e permite editar nome, telefone e endereço.

Ao pré-visualizar ou emitir, os produtos novos e seus preços ficam salvos. O botão **Produtos cadastrados** permite criar produtos e editar seu preço e unidade posteriormente. Ao digitar um nome salvo na grade ou usar **Buscar produto**, o preço definido no cadastro é preenchido automaticamente. Alterar o preço apenas em uma linha do cupom não modifica o cadastro. Cada compra é registrada no banco com os itens, inclusive o peso exato, e associada ao cliente selecionado, quando houver.

## Pix no cupom

Abra **Configurar Pix** no aplicativo e informe a chave Pix da sua conta Nubank, o nome do recebedor e a cidade. Esses dados ficam apenas no banco local deste computador. Em vendas Pix, o programa gera um QR Code BR Code com o valor exato e um identificador único do pedido. A pré-visualização e a emissão do mesmo pedido usam o mesmo identificador. O QR Code é gerado localmente; a internet não é necessária para essa etapa.

O cupom mostra **Pix aguardando confirmação**. O aplicativo **não consulta o Nubank nem confirma recebimentos automaticamente**. Confira o nome do recebedor na tela do banco pagador e o valor recebido no aplicativo do Nubank antes de considerar o pedido pago. Se o Pix não estiver configurado, o aplicativo oferece configurar a chave ou continuar sem QR Code.

O cupom usa bobina de 80 mm e área imprimível de 72 mm. O texto é renderizado em bitmap com a fonte Arial antes do envio ao driver, para manter `ã`, `ç`, `é` e outros caracteres no formato visto na pré-visualização. O logo do aplicativo é uma recriação local baseada nas imagens fornecidas.

## Arquivos

- `app\BarbaCupom.exe`: executável.
- `app\assets\logo.png` e `logo.ico`: imagem e ícone locais.
- `Tests\cupom-acentos.png`: amostra do cupom com acentos.
- `%LOCALAPPDATA%\BarbaDefumados\Cupom\cadastros.db`: banco SQLite de uso. Para fazer cópia de segurança, feche o aplicativo antes de copiar esse arquivo.

## Compilar novamente

Com o .NET 10 instalado, execute:

```powershell
dotnet run --project Tests\ReceiptTests.csproj
dotnet publish BarbaCupom.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o app
```

O executável publicado usa o .NET Desktop Runtime 10 já instalado neste computador. O aplicativo não acessa o site `D:\Site barba defumados`.
