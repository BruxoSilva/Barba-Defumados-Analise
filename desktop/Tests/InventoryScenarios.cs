using BarbaCupom;
using Microsoft.Data.Sqlite;

internal static class InventoryScenarios
{
    public static void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"barba-estoque-{Guid.NewGuid():N}.db");
        try
        {
            var day=DateTime.Today;
            using var db = new InventoryStore(path);
            var supplier = db.AddSupplier("Fornecedor Teste", "34999999999");
            var category = db.ListCategories().Single(x => x.Name == "Embalagens");
            var bag = db.AddSupply("Saco 500 g", category.Id, "pacote", 100, "un", 10000, 10);
            var bagLot = db.ReceiveSupply(bag, supplier, 2, day.AddMonths(7), day);
            Assert(db.GetSupplyUnitCost(bag) == 110, "Custo da embalagem com perda de 10%");
            db.UpdateSupplyPrice(bag, 12000, DateTime.Now);
            Assert(db.ListSupplyPrices(bag).Count == 2, "Histórico de preço imutável");
            var meat = db.AddRawMaterial("Costela");
            var raw = db.ReceiveRaw(meat, supplier, 10000, 50000, day.AddDays(25), day);
            var product = db.AddStockProduct("Costela defumada", 50, SaleUnit.Kg);
            var recipe = db.AddRecipe(product, 10);
            db.AddRecipeItem(recipe, "raw", meat, 1000);
            db.SetRecipePackaging(recipe, 500, bag, 1);
            var manipulation = db.StartManipulation(recipe, new[] { (raw, 5000) }, day.AddDays(1), receivedGrams:4800);
            var finished = db.FinishProduction(manipulation, 4500, new[] { (500, 9) },
                new[] { (bagLot, 9) }, day.AddDays(15), day.AddDays(2));
            Assert(finished.Count == 1 && finished[0].FrozenCostCents > 0, "Custo congelado do lote");
            Assert(finished[0].FrozenCostCents == 3063, "Custo congelado inclui perda de embalagem e custo fixo");
            using(var connection=new SqliteConnection($"Data Source={path};Pooling=False"))
            {connection.Open();using var c=connection.CreateCommand();c.CommandText="SELECT transfer_loss_g,process_loss_g FROM manipulations WHERE id="+manipulation;using var r=c.ExecuteReader();if(!r.Read()||r.GetInt32(0)!=200||r.GetInt32(1)!=300)throw new Exception("Perdas das duas etapas não foram registradas");}
            db.RenameSupply(bag,"Saco 500 g",20);
            Assert(db.ListSupplyPrices(bag).Count==3 && db.GetSupplyUnitCost(bag)==144,"Nova perda cria versão de custo sem alterar lote pronto");
            Assert(db.ListOffers(day.AddDays(2)).Single().Available == 9, "Estoque pronto");
            var reservation = db.Reserve(product, 500, 2, day.AddDays(3));
            Assert(db.ListOffers(day.AddDays(3)).Single().Available == 7, "Reserva retira disponibilidade");
            db.MarkPaid(reservation, PaymentKind.Pix, day.AddDays(3).AddHours(10));
            db.Deliver(reservation, day.AddDays(4));
            Assert(db.ListOffers(day.AddDays(4)).Single().Available == 7, "Entrega dá baixa definitiva");
            var report=db.Report(day.AddDays(4),day.AddDays(4));
            Assert(report.Count==1 && report[0].WeightGrams==1000 && report[0].Payment=="Pix" && report[0].RevenueCents>report[0].CostCents,"Relatório de saída e lucro");
            var expiring = db.Reserve(product, 500, 1, day.AddDays(4));
            db.ReleaseExpired(day.AddDays(6));
            Assert(db.ListOffers(day.AddDays(6)).Single().Available == 7, "Reserva expirada retorna ao estoque");
            Assert(db.GetReservation(expiring).Status == "expired", "Estado da reserva expirada");
            try{db.RecordLoss(finished[0].Id,1,"Defeito","");throw new Exception("Aceitou perda sem descrição");}catch(ArgumentException){}
            Assert(db.ListOffers(day.AddDays(16)).Count == 0, "Lote vencido bloqueado");
            Console.WriteLine("Estoque: aquisição, custo, três estoques, reserva, entrega e validade passaram.");
        }
        finally
        {
            foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(path) + "*")) File.Delete(file);
        }
        Fefo();
    }
    static void Fefo()
    {
        var path=Path.Combine(Path.GetTempPath(),$"barba-fefo-{Guid.NewGuid():N}.db");
        try
        {
            using var db=new InventoryStore(path);var day=DateTime.Today;
            var supplier=db.AddSupplier("Fornecedor");var category=db.ListCategories().Single(x=>x.Name=="Embalagens");
            var bag=db.AddSupply("Saco",category.Id,"pacote",100,"un",10000,10);var bagLot=db.ReceiveSupply(bag,supplier,1,day.AddMonths(2),day);
            var material=db.AddRawMaterial("Carne");var raw=db.ReceiveRaw(material,supplier,3000,15000,day.AddMonths(2),day);
            var product=db.AddStockProduct("Produto",50,SaleUnit.Kg);var recipe=db.AddRecipe(product,0);db.SetRecipePackaging(recipe,500,bag,1);
            var first=db.StartManipulation(recipe,new[]{(raw,1000)},day);var early=db.FinishProduction(first,1000,new[]{(500,2)},new[]{(bagLot,2)},day.AddDays(8),day)[0];
            var second=db.StartManipulation(recipe,new[]{(raw,1000)},day.AddDays(1));var late=db.FinishProduction(second,1000,new[]{(500,2)},new[]{(bagLot,2)},day.AddDays(12),day.AddDays(1))[0];
            var reservation=db.Reserve(product,500,3,day.AddDays(2));
            Assert(db.ListReadyLots().Single(x=>x.Id==early.Id).Reserved==2 && db.ListReadyLots().Single(x=>x.Id==late.Id).Reserved==1,"FEFO não separou lotes corretamente");
            try{db.Reserve(product,500,2,day.AddDays(2));throw new Exception("Aceitou reserva acima do saldo");}catch(InvalidOperationException){}
            Assert(db.ListReadyLots().Sum(x=>x.Reserved)==3,"Reserva falha não fez rollback");
            db.CancelReservation(reservation,day.AddDays(2));Assert(db.ListReadyLots().Sum(x=>x.Available)==4,"Cancelamento não devolveu pacotes");
            db.RecordMaterialLoss("materia_prima",raw,100,"Defeito","Teste",day);
            Assert(db.ListRawLots(day).Single().AvailableGrams==900,"Perda de matéria-prima incorreta");
            db.RecordMaterialLoss("insumo",bagLot,2,"Danificado","Dois sacos",day);
            Assert(db.ListSupplyLots().Single().Available==94,"Perda de insumo incorreta");
            Assert(db.TraceReadyLot(early.Id).Count==2,"Rastreabilidade não inclui as duas origens");
            Console.WriteLine("FEFO, reserva entre lotes e rollback passaram.");
        }
        finally{foreach(var file in Directory.GetFiles(Path.GetTempPath(),Path.GetFileName(path)+"*"))File.Delete(file);}
    }
    static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); }
}
