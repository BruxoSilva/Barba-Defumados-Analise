using BarbaCupom;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Data.Sqlite;
using System.Globalization;
using ZXing.Windows.Compatibility;

if(args.Contains("--ui-smoke"))
{
    var uiPath=Path.Combine(Path.GetTempPath(),$"barba-ui-{Guid.NewGuid():N}.db");
    Environment.SetEnvironmentVariable("BARBA_CUPOM_DB_PATH",uiPath);
    var thread=new Thread(()=>
    {
        ApplicationConfiguration.Initialize();
        using var stock=new InventoryStore(uiPath);
        using var main=new MainForm();main.Show();Application.DoEvents();
        using(var bitmap=new Bitmap(main.Width,main.Height)){main.DrawToBitmap(bitmap,main.ClientRectangle);bitmap.Save("Tests/main-estoque.png");}
        using var panel=new StockPanel(stock);panel.Show();Application.DoEvents();
        foreach(var tab in panel.Controls.OfType<TabControl>().Single().TabPages.Cast<TabPage>())
        {panel.Controls.OfType<TabControl>().Single().SelectedTab=tab;Application.DoEvents();}
        using(var bitmap=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bitmap,panel.ClientRectangle);bitmap.Save("Tests/painel-estoque.png");}
        panel.Close();main.Close();
    });
    thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
    Console.WriteLine("Interface: formulário principal e todas as abas abriram em banco isolado.");return;
}

InventoryScenarios.Run();
AuthScenarios.Run();
BackupScenarios.Run();
var cardReceipt = new Receipt("Ana", "", "", new[] { new OrderLine("Teste", 1, 1000) }, PaymentKind.Card, null, DateTime.Now, cardFeePercent: 5);
if (cardReceipt.BaseTotalCents != 1000 || cardReceipt.TotalCents != 1053 || cardReceipt.CardTotalCents != 1053) throw new Exception("Acréscimo de cartão incorreto");

var lines = new[] { new OrderLine("Costela BBQ", 2, 1050) };
var order = new Receipt("Ana", "34999999999", "Retirada", lines, PaymentKind.Cash, 3000, new DateTime(2026, 11, 6, 15, 32, 0));
if (order.TotalCents != 2100 || order.ChangeCents != 900) throw new Exception("Troco e total incorretos");
if (order.IssuedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) != "06/11/2026 15:32") throw new Exception("Data incorreta");
if (Money.Parse("1.234,56") != 123456 || Money.Format(123456) != "R$ 1.234,56") throw new Exception("Dinheiro incorreto");
try { _ = new Receipt("Ana", "34999999999", "", lines, PaymentKind.Cash, 2000, DateTime.Now); throw new Exception("Aceitou dinheiro insuficiente"); }
catch (ArgumentException) { }
try { _ = Money.Parse("1,999"); throw new Exception("Aceitou centavos inválidos"); }
catch (FormatException) { }
var accents = new Receipt("João Gonçalves", "34999999999", "Rua São José, 10", new[] { new OrderLine("Linguiça e pão de açúcar", 1, 1250) }, PaymentKind.Pix, null, DateTime.Now);
using (var bitmap = ReceiptRenderer.Render(accents, Path.Combine("assets", "logo.png")))
{
    if (bitmap.Width != 576 || bitmap.Height < 500) throw new Exception("Raster do cupom incorreto");
    bitmap.Save(Path.Combine("Tests", "cupom-acentos.png"));
}
if (args.Contains("--print-test")) ReceiptRenderer.Print(accents, Path.Combine("assets", "logo.png"));
Console.WriteLine("6 verificações passaram.");

var dbPath = Path.Combine(Path.GetTempPath(), $"barba-cupom-test-{Guid.NewGuid():N}.db");
try
{
    using (var store = new LocalStore(dbPath))
    {
    var customerId = store.SaveCustomer("João Gonçalves", "34999999999", "Rua São José, 10");
    if (store.FindCustomers("joao").Single().Id != customerId) throw new Exception("Busca de cliente falhou");
    store.UpdateCustomer(customerId, "João G.", "34988888888", "Rua B");
    if (store.GetCustomer(customerId)?.Phone != "34988888888") throw new Exception("Edição de cliente falhou");
    store.UpsertProducts(accents.Lines);
    if (store.FindProducts("linguica").Single().UnitPriceCents != 1250) throw new Exception("Busca de produto falhou");
    store.RecordReceipt(accents, customerId);
    if (store.CountReceipts(customerId) != 1) throw new Exception("Histórico do cliente falhou");
    }
    using (var reopened = new LocalStore(dbPath))
    {
        if (reopened.FindCustomers("goncalves").Count != 0) throw new Exception("Edição antiga persistiu incorretamente");
        if (reopened.FindCustomers("joao g.").Count != 1 || reopened.FindProducts("acucar").Count != 1)
            throw new Exception("Banco não persistiu após reabrir");
    }
}
finally
{
    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(dbPath)!, Path.GetFileName(dbPath) + "*")) File.Delete(path);
}
Console.WriteLine("Banco SQLite: cadastro, busca, edição, produtos, compra e persistência passaram.");

var weighted = new OrderLine("Costela por kg", 0.350m, 4990, SaleUnit.Kg);
if (weighted.TotalCents != 1747 || weighted.QuantityLabel != "0,350 KG") throw new Exception("Cálculo por peso ou arredondamento incorreto");
if (QuantityInput.Parse("0,350", SaleUnit.Kg) != 0.350m) throw new Exception("Leitura de peso incorreta");
foreach (var input in new[] { "0.5", "0,5", "1.2", "1,2" })
    if (QuantityInput.Parse(input, SaleUnit.Kg) != (input[0] == '0' ? 0.5m : 1.2m)) throw new Exception($"Peso {input} incorreto");
try { _ = QuantityInput.Parse("0,3500", SaleUnit.Kg); throw new Exception("Aceitou peso com quatro casas"); }
catch (FormatException) { }
var weightReceipt = new Receipt("Ana", "34999999999", "Retirada", new[] { weighted }, PaymentKind.Pix, null, DateTime.Now);
var weightDb = Path.Combine(Path.GetTempPath(), $"barba-peso-test-{Guid.NewGuid():N}.db");
try
{
    using (var store = new LocalStore(weightDb))
    {
        store.UpsertProducts(weightReceipt.Lines);
        if (store.FindProducts("costela").Single().Unit != SaleUnit.Kg) throw new Exception("Unidade do produto não salva");
        store.RecordReceipt(weightReceipt, null);
    }
    using (var conn = new SqliteConnection($"Data Source={weightDb};Pooling=False"))
    {
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT unit, quantity_milli, total_cents FROM receipt_lines LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read() || reader.GetString(0) != "KG" || reader.GetInt32(1) != 350 || reader.GetInt32(2) != 1747)
            throw new Exception("Peso do cupom não persistiu corretamente");
    }
}
finally
{
    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(weightDb)!, Path.GetFileName(weightDb) + "*")) File.Delete(path);
}
Console.WriteLine("Venda por peso, arredondamento e persistência passaram.");

var oldDb = Path.Combine(Path.GetTempPath(), $"barba-migration-test-{Guid.NewGuid():N}.db");
try
{
    using (var conn = new SqliteConnection($"Data Source={oldDb};Pooling=False"))
    {
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE products(id INTEGER PRIMARY KEY, search_key TEXT UNIQUE, name TEXT, unit_price_cents INTEGER, updated_at TEXT);
            INSERT INTO products(search_key,name,unit_price_cents,updated_at) VALUES('linguica','Linguiça',2500,CURRENT_TIMESTAMP);
            CREATE TABLE receipt_lines(id INTEGER PRIMARY KEY, receipt_id INTEGER, product_id INTEGER, product_name TEXT, quantity INTEGER, unit_price_cents INTEGER, total_cents INTEGER);
            """;
        cmd.ExecuteNonQuery();
    }
    using (var store = new LocalStore(oldDb))
    {
        if (store.FindProducts("linguica").Single().Unit != SaleUnit.Un) throw new Exception("Migração alterou produto antigo");
        store.UpsertProducts(new[] { weighted });
        if (store.FindProducts("costela").Single().Unit != SaleUnit.Kg) throw new Exception("Migração não habilitou peso");
    }
}
finally
{
    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(oldDb)!, Path.GetFileName(oldDb) + "*")) File.Delete(path);
}
Console.WriteLine("Migração de banco antigo passou.");

var nameOnly = new Receipt("Cliente sem contato", "", "", new[] { weighted }, PaymentKind.Pix, null, DateTime.Now);
if (nameOnly.Phone != "" || nameOnly.Address != "") throw new Exception("Cupom sem contato falhou");
using (var bitmap = ReceiptRenderer.Render(nameOnly))
    if (bitmap.Height < 200) throw new Exception("Prévia sem contato falhou");
var catalogDb = Path.Combine(Path.GetTempPath(), $"barba-catalog-test-{Guid.NewGuid():N}.db");
try
{
    using (var store = new LocalStore(catalogDb))
    {
        store.SaveProduct("Picanha", 5990, SaleUnit.Kg);
        store.SaveProduct("Picanha", 6490, SaleUnit.Kg);
        store.RememberProducts(new[] { new OrderLine("Picanha", 0.5m, 5990, SaleUnit.Kg) });
        if (store.FindProducts("picanha").Single().UnitPriceCents != 6490) throw new Exception("Preço editado sobrescrito pela venda");
        store.RememberProducts(new[] { new OrderLine("Costela", 0.5m, 3990, SaleUnit.Kg) });
        if (store.FindProducts("costela").Single().UnitPriceCents != 3990) throw new Exception("Produto novo não salvo");
        store.SaveCustomer("Cliente sem contato", "", "");
    }
}
finally
{
    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(catalogDb)!, Path.GetFileName(catalogDb) + "*")) File.Delete(path);
}
Console.WriteLine("Nome obrigatório, contato opcional e catálogo editável passaram.");

var pixSample = new PixSettings("123e4567-e12b-12d1-a456-426655440000", "Fulano de Tal", "Brasilia");
var sampleCode = PixCode.Build(pixSample, null, "***");
if (sampleCode != "00020126580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-4266554400005204000053039865802BR5913Fulano de Tal6008BRASILIA62070503***63041D3D")
    throw new Exception("BR Code não corresponde ao exemplo oficial do Banco Central");
var orderTxId = PixCode.NewTxId();
var amountCode = PixCode.Build(pixSample, 2495, orderTxId);
if (!amountCode.Contains("540524.95") || !amountCode.Contains($"05{orderTxId.Length:00}{orderTxId}"))
    throw new Exception("Valor ou identificador do Pix incorreto");
using (var noQr = ReceiptRenderer.Render(nameOnly))
using (var withQr = ReceiptRenderer.Render(nameOnly, Path.Combine("assets", "logo.png"), amountCode))
{
    withQr.Save(Path.Combine("Tests", "pix-test.png"));
    if (withQr.Height < noQr.Height + 250) throw new Exception("QR não foi incluído no cupom");
    var qrReader = new BarcodeReader { AutoRotate = true };
    qrReader.Options.TryHarder = true;
    qrReader.Options.TryInverted = true;
    qrReader.Options.PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE };
    var decoded = qrReader.Decode(withQr);
    if (decoded?.Text != amountCode)
    {
        using var bottom = withQr.Clone(new System.Drawing.Rectangle(75, Math.Max(0, withQr.Height - 520), 426, Math.Min(520, withQr.Height)), withQr.PixelFormat);
        decoded = qrReader.Decode(bottom);
    }
    if (decoded?.Text != amountCode) throw new Exception("QR do cupom não é legível por decodificador");
}
try { _ = PixCode.Build(new PixSettings("chave com espaco", "Fulano", "Brasilia"), 2495, orderTxId); throw new Exception("Aceitou chave Pix com espaço"); }
catch (ArgumentException) { }
var pixDb = Path.Combine(Path.GetTempPath(), $"barba-pix-test-{Guid.NewGuid():N}.db");
try
{
    using (var store = new LocalStore(pixDb))
    {
        if (store.GetPixSettings() is not null) throw new Exception("Pix deveria começar sem configuração");
        store.SavePixSettings(pixSample);
        store.RecordReceipt(new Receipt("Pix", "", "", new[] { weighted }, PaymentKind.Pix, null, DateTime.Now, orderTxId), null);
    }
    using (var store = new LocalStore(pixDb))
        if (store.GetPixSettings() != pixSample) throw new Exception("Configuração Pix não persistiu");
    using (var conn = new SqliteConnection($"Data Source={pixDb};Pooling=False"))
    {
        conn.Open(); using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pix_txid FROM receipts LIMIT 1";
        if ((string?)cmd.ExecuteScalar() != orderTxId) throw new Exception("Identificador Pix do cupom não persistiu");
    }
}
finally
{
    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(pixDb)!, Path.GetFileName(pixDb) + "*")) File.Delete(path);
}
Console.WriteLine("BR Code Pix, QR e configuração local passaram.");
