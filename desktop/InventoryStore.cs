using Microsoft.Data.Sqlite;
using System.Globalization;

namespace BarbaCupom;

public sealed record SupplyCategory(long Id, string Name);
public sealed record SupplyPrice(long Id, DateTime EffectiveAt, int PurchasePriceCents, int UnitCostCents);
public sealed record StockOffer(long ProductId, string ProductName, int PackageWeightGrams, int Available,
    DateTime ExpiresAt, int UnitPriceCents, int FrozenCostCents, SaleUnit SaleUnit)
{
    public string Display => $"{ProductName} · {(PackageWeightGrams / 1000m).ToString("0.###", CultureInfo.GetCultureInfo("pt-BR"))} kg · {Available} disp. · vence {ExpiresAt:dd/MM/yyyy} · {Money.Format(UnitPriceCents)}";
}
public sealed record ReadyLot(long Id, long ProductId, int PackageWeightGrams, int Quantity,
    int Available, int Reserved, DateTime ExpiresAt, int FrozenCostCents, int UnitPriceCents, DateTime CreatedAt = default);
public sealed record StockReservation(long Id, long ProductId, int PackageWeightGrams, int Quantity,
    DateTime ExpiresAt, string Status, string? Payment);

public sealed partial class InventoryStore : IDisposable
{
    readonly SqliteConnection db;
    public InventoryStore(string? path = null)
    {
        path ??= LocalStore.DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using (var legacy = new LocalStore(path)) { }
        db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        db.Open();
        Run("PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");
        Run("""
            CREATE TABLE IF NOT EXISTS suppliers(id INTEGER PRIMARY KEY,name TEXT NOT NULL,phone TEXT NOT NULL DEFAULT '',created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS supply_categories(id INTEGER PRIMARY KEY,name TEXT NOT NULL UNIQUE,active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS supplies(id INTEGER PRIMARY KEY,name TEXT NOT NULL,category_id INTEGER NOT NULL REFERENCES supply_categories(id),purchase_form TEXT NOT NULL,content_milli INTEGER NOT NULL,use_unit TEXT NOT NULL,loss_pct REAL NOT NULL DEFAULT 0,active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS supply_prices(id INTEGER PRIMARY KEY,supply_id INTEGER NOT NULL REFERENCES supplies(id),effective_at TEXT NOT NULL,purchase_price_cents INTEGER NOT NULL,unit_cost_milli_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS supply_lots(id INTEGER PRIMARY KEY,supply_id INTEGER NOT NULL REFERENCES supplies(id),supplier_id INTEGER NOT NULL REFERENCES suppliers(id),received_at TEXT NOT NULL,expires_at TEXT NOT NULL,quantity_milli INTEGER NOT NULL,available_milli INTEGER NOT NULL,frozen_cost_cents INTEGER NOT NULL,price_version_id INTEGER NOT NULL REFERENCES supply_prices(id));
            CREATE TABLE IF NOT EXISTS raw_materials(id INTEGER PRIMARY KEY,name TEXT NOT NULL UNIQUE,active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS raw_lots(id INTEGER PRIMARY KEY,material_id INTEGER NOT NULL REFERENCES raw_materials(id),supplier_id INTEGER NOT NULL REFERENCES suppliers(id),received_at TEXT NOT NULL,expires_at TEXT NOT NULL,weight_g INTEGER NOT NULL,available_g INTEGER NOT NULL,frozen_cost_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS stock_products(id INTEGER PRIMARY KEY,product_id INTEGER NOT NULL UNIQUE REFERENCES products(id),margin_pct REAL NOT NULL,sale_unit TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS recipes(id INTEGER PRIMARY KEY,product_id INTEGER NOT NULL REFERENCES stock_products(product_id),loss_pct REAL NOT NULL,created_at TEXT NOT NULL,active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS recipe_items(id INTEGER PRIMARY KEY,recipe_id INTEGER NOT NULL REFERENCES recipes(id),kind TEXT NOT NULL,reference_id INTEGER NOT NULL,quantity_per_kg_milli INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS recipe_packages(id INTEGER PRIMARY KEY,recipe_id INTEGER NOT NULL REFERENCES recipes(id),weight_g INTEGER NOT NULL,supply_id INTEGER NOT NULL REFERENCES supplies(id),quantity_per_package INTEGER NOT NULL,UNIQUE(recipe_id,weight_g,supply_id));
            CREATE TABLE IF NOT EXISTS manipulations(id INTEGER PRIMARY KEY,recipe_id INTEGER NOT NULL REFERENCES recipes(id),started_at TEXT NOT NULL,finished_at TEXT,input_g INTEGER NOT NULL,output_g INTEGER,raw_cost_cents INTEGER NOT NULL,supply_cost_cents INTEGER,transfer_loss_g INTEGER NOT NULL DEFAULT 0,process_loss_g INTEGER,fixed_cost_cents INTEGER,total_cost_cents INTEGER,status TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS manipulation_raw(id INTEGER PRIMARY KEY,manipulation_id INTEGER NOT NULL REFERENCES manipulations(id),raw_lot_id INTEGER NOT NULL REFERENCES raw_lots(id),used_g INTEGER NOT NULL,cost_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS manipulation_supply(id INTEGER PRIMARY KEY,manipulation_id INTEGER NOT NULL REFERENCES manipulations(id),supply_lot_id INTEGER NOT NULL REFERENCES supply_lots(id),used_milli INTEGER NOT NULL,cost_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS ready_lots(id INTEGER PRIMARY KEY,manipulation_id INTEGER NOT NULL REFERENCES manipulations(id),product_id INTEGER NOT NULL REFERENCES stock_products(product_id),created_at TEXT NOT NULL,expires_at TEXT NOT NULL,package_weight_g INTEGER NOT NULL,quantity INTEGER NOT NULL,available INTEGER NOT NULL,reserved INTEGER NOT NULL DEFAULT 0,sold INTEGER NOT NULL DEFAULT 0,lost INTEGER NOT NULL DEFAULT 0,frozen_cost_cents INTEGER NOT NULL,unit_price_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS reservations(id INTEGER PRIMARY KEY,product_id INTEGER NOT NULL REFERENCES stock_products(product_id),package_weight_g INTEGER NOT NULL,quantity INTEGER NOT NULL,created_at TEXT NOT NULL,expires_at TEXT NOT NULL,status TEXT NOT NULL,payment TEXT,paid_at TEXT,delivered_at TEXT,receipt_id INTEGER);
            CREATE TABLE IF NOT EXISTS reservation_allocations(id INTEGER PRIMARY KEY,reservation_id INTEGER NOT NULL REFERENCES reservations(id),ready_lot_id INTEGER NOT NULL REFERENCES ready_lots(id),quantity INTEGER NOT NULL,unit_cost_cents INTEGER NOT NULL,unit_price_cents INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS stock_losses(id INTEGER PRIMARY KEY,lot_kind TEXT NOT NULL,lot_id INTEGER NOT NULL,reason TEXT NOT NULL,description TEXT NOT NULL,weight_g INTEGER NOT NULL,quantity INTEGER NOT NULL,cost_cents INTEGER NOT NULL,created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS stock_events(id INTEGER PRIMARY KEY,event_at TEXT NOT NULL,kind TEXT NOT NULL,lot_kind TEXT,lot_id INTEGER,quantity_milli INTEGER,cost_cents INTEGER,details TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS stock_settings_versions(id INTEGER PRIMARY KEY,effective_at TEXT NOT NULL,labor_cents_per_kg INTEGER NOT NULL,utilities_cents_per_kg INTEGER NOT NULL,delivery_cents_per_kg INTEGER NOT NULL,card_fee_pct REAL NOT NULL,reservation_hours INTEGER NOT NULL,alert_days_1 INTEGER NOT NULL,alert_days_2 INTEGER NOT NULL,backup_path TEXT);
            """);
        EnsureColumn("manipulations","transfer_loss_g","INTEGER NOT NULL DEFAULT 0");
        foreach (var category in new[] { "Embalagens", "Tempero sólido", "Tempero líquido", "Envasamento" })
        {
            using var c = Cmd("INSERT OR IGNORE INTO supply_categories(name) VALUES($n)"); c.Parameters.AddWithValue("$n", category); c.ExecuteNonQuery();
        }
        if (Long("SELECT COUNT(*) FROM stock_settings_versions") == 0)
            Run("INSERT INTO stock_settings_versions(effective_at,labor_cents_per_kg,utilities_cents_per_kg,delivery_cents_per_kg,card_fee_pct,reservation_hours,alert_days_1,alert_days_2) VALUES('2000-01-01T00:00:00',200,150,100,5,24,7,3)");
    }
    public void Dispose() => db.Dispose();
    SqliteCommand Cmd(string sql, SqliteTransaction? tx = null) { var c = db.CreateCommand(); c.CommandText = sql; c.Transaction = tx; return c; }
    void Run(string sql) { using var c = Cmd(sql); c.ExecuteNonQuery(); }
    void EnsureColumn(string table,string name,string definition)
    { using var c=Cmd($"PRAGMA table_info({table})");using var r=c.ExecuteReader();var exists=false;while(r.Read())if(r.GetString(1)==name)exists=true;r.Close();if(!exists)Run($"ALTER TABLE {table} ADD COLUMN {name} {definition}"); }
    long Long(string sql, SqliteTransaction? tx = null, params (string key, object value)[] args)
    { using var c = Cmd(sql, tx); foreach (var (k,v) in args) c.Parameters.AddWithValue(k,v); return Convert.ToInt64(c.ExecuteScalar()); }
    static string Stamp(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture);
    static void Positive(int value, string label) { if (value <= 0) throw new ArgumentException($"{label} deve ser maior que zero."); }
    static void Name(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Informe um nome."); }
    void Event(string kind, string lotKind, long lotId, int quantityMilli, int costCents, string details, DateTime at, SqliteTransaction tx)
    { using var c = Cmd("INSERT INTO stock_events(event_at,kind,lot_kind,lot_id,quantity_milli,cost_cents,details) VALUES($d,$k,$lk,$id,$q,$c,$t)", tx); c.Parameters.AddWithValue("$d",Stamp(at)); c.Parameters.AddWithValue("$k",kind); c.Parameters.AddWithValue("$lk",lotKind); c.Parameters.AddWithValue("$id",lotId); c.Parameters.AddWithValue("$q",quantityMilli); c.Parameters.AddWithValue("$c",costCents); c.Parameters.AddWithValue("$t",details); c.ExecuteNonQuery(); }

    public long AddSupplier(string name, string phone = "")
    { Name(name); using var c=Cmd("INSERT INTO suppliers(name,phone,created_at) VALUES($n,$p,$d);SELECT last_insert_rowid()"); c.Parameters.AddWithValue("$n",name.Trim()); c.Parameters.AddWithValue("$p",phone.Trim()); c.Parameters.AddWithValue("$d",Stamp(DateTime.Now)); return (long)c.ExecuteScalar()!; }
    public IReadOnlyList<(long Id,string Name,string Phone)> ListSuppliers()
    { using var c=Cmd("SELECT id,name,phone FROM suppliers ORDER BY name COLLATE NOCASE"); using var r=c.ExecuteReader(); var list=new List<(long,string,string)>(); while(r.Read()) list.Add((r.GetInt64(0),r.GetString(1),r.GetString(2))); return list; }
    public void UpdateSupplier(long id,string name,string phone)
    { Name(name); using var c=Cmd("UPDATE suppliers SET name=$n,phone=$p WHERE id=$id"); c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$n",name.Trim());c.Parameters.AddWithValue("$p",phone.Trim()); if(c.ExecuteNonQuery()!=1) throw new ArgumentException("Fornecedor não encontrado."); }
    public IReadOnlyList<SupplyCategory> ListCategories()
    { using var c=Cmd("SELECT id,name FROM supply_categories WHERE active=1 ORDER BY name COLLATE NOCASE"); using var r=c.ExecuteReader(); var list=new List<SupplyCategory>(); while(r.Read()) list.Add(new(r.GetInt64(0),r.GetString(1))); return list; }
    public long AddCategory(string name)
    { Name(name); using var c=Cmd("INSERT INTO supply_categories(name) VALUES($n);SELECT last_insert_rowid()");c.Parameters.AddWithValue("$n",name.Trim());return (long)c.ExecuteScalar()!; }
    public void RenameCategory(long id,string name)
    { Name(name); using var c=Cmd("UPDATE supply_categories SET name=$n WHERE id=$id AND active=1");c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$n",name.Trim());if(c.ExecuteNonQuery()!=1) throw new ArgumentException("Categoria não encontrada."); }
    public void DeleteCategory(long id)
    { if(Long("SELECT COUNT(*) FROM supplies WHERE category_id=$id",null,("$id",id))>0) throw new InvalidOperationException("A categoria possui insumos."); using var c=Cmd("UPDATE supply_categories SET active=0 WHERE id=$id");c.Parameters.AddWithValue("$id",id);c.ExecuteNonQuery(); }

    public long AddSupply(string name,long categoryId,string purchaseForm,decimal content,string useUnit,int purchasePriceCents,decimal lossPercent=0)
    {
        Name(name); if(!new[]{"caixa","pacote","kg","litro","unidade"}.Contains(purchaseForm)) throw new ArgumentException("Forma de aquisição inválida.");
        if(!new[]{"g","ml","un"}.Contains(useUnit)) throw new ArgumentException("Unidade de uso inválida.");
        if(content<=0 || content>1_000_000 || purchasePriceCents<0 || lossPercent<0 || lossPercent>=100) throw new ArgumentException("Confira quantidade, preço e perda.");
        if(purchaseForm=="kg" && useUnit!="g" || purchaseForm=="litro" && useUnit!="ml" || purchaseForm=="unidade" && useUnit!="un") throw new ArgumentException("Aquisição e unidade de uso incompatíveis.");
        var contentMilli=checked((int)(content*(purchaseForm is "kg" or "litro" ? 1_000_000m : 1000m)));
        using var tx=db.BeginTransaction(); using var c=Cmd("INSERT INTO supplies(name,category_id,purchase_form,content_milli,use_unit,loss_pct) VALUES($n,$cat,$f,$q,$u,$l);SELECT last_insert_rowid()",tx);
        c.Parameters.AddWithValue("$n",name.Trim());c.Parameters.AddWithValue("$cat",categoryId);c.Parameters.AddWithValue("$f",purchaseForm);c.Parameters.AddWithValue("$q",contentMilli);c.Parameters.AddWithValue("$u",useUnit);c.Parameters.AddWithValue("$l",lossPercent);
        var id=(long)c.ExecuteScalar()!; InsertSupplyPrice(id,purchasePriceCents,DateTime.Now,tx); tx.Commit();return id;
    }
    void InsertSupplyPrice(long id,int price,DateTime at,SqliteTransaction tx)
    {
        Positive(price,"Preço"); using var c=Cmd("INSERT INTO supply_prices(supply_id,effective_at,purchase_price_cents,unit_cost_milli_cents) SELECT id,$d,$p,CAST(ROUND($p*1000000.0/content_milli*(1+loss_pct/100.0)) AS INTEGER) FROM supplies WHERE id=$id",tx);
        c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$d",Stamp(at));c.Parameters.AddWithValue("$p",price);if(c.ExecuteNonQuery()!=1) throw new ArgumentException("Insumo não encontrado.");
    }
    public void UpdateSupplyPrice(long id,int price,DateTime? at=null)
    { using var tx=db.BeginTransaction();InsertSupplyPrice(id,price,at??DateTime.Now,tx);tx.Commit(); }
    public int GetSupplyUnitCost(long id)
    { return checked((int)Math.Round(Long("SELECT unit_cost_milli_cents FROM supply_prices WHERE supply_id=$id ORDER BY effective_at DESC,id DESC LIMIT 1",null,("$id",id))/1000m,0,MidpointRounding.AwayFromZero)); }
    public IReadOnlyList<SupplyPrice> ListSupplyPrices(long id)
    { using var c=Cmd("SELECT id,effective_at,purchase_price_cents,unit_cost_milli_cents FROM supply_prices WHERE supply_id=$id ORDER BY effective_at,id");c.Parameters.AddWithValue("$id",id);using var r=c.ExecuteReader();var list=new List<SupplyPrice>();while(r.Read())list.Add(new(r.GetInt64(0),Date(r.GetString(1)),r.GetInt32(2),(int)Math.Round(r.GetInt64(3)/1000m)));return list; }
    public IReadOnlyList<(long Id,string Name,string Unit,int UnitCostCents)> ListSupplies()
    { using var c=Cmd("SELECT s.id,s.name,s.use_unit,(SELECT unit_cost_milli_cents FROM supply_prices WHERE supply_id=s.id ORDER BY effective_at DESC,id DESC LIMIT 1) FROM supplies s WHERE active=1 ORDER BY s.name");using var r=c.ExecuteReader();var list=new List<(long,string,string,int)>();while(r.Read())list.Add((r.GetInt64(0),r.GetString(1),r.GetString(2),(int)Math.Round(r.GetInt64(3)/1000m)));return list; }
    public void RenameSupply(long id,string name,decimal lossPercent)
    {
        Name(name);if(lossPercent<0||lossPercent>=100)throw new ArgumentException("Perda inválida.");
        using var tx=db.BeginTransaction();decimal oldLoss;int price;
        using(var current=Cmd("SELECT s.loss_pct,p.purchase_price_cents FROM supplies s JOIN supply_prices p ON p.id=(SELECT id FROM supply_prices WHERE supply_id=s.id ORDER BY effective_at DESC,id DESC LIMIT 1) WHERE s.id=$id",tx))
        {current.Parameters.AddWithValue("$id",id);using var r=current.ExecuteReader();if(!r.Read())throw new ArgumentException("Insumo não encontrado.");oldLoss=r.GetDecimal(0);price=r.GetInt32(1);}
        using(var c=Cmd("UPDATE supplies SET name=$n,loss_pct=$l WHERE id=$id",tx)){c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$n",name.Trim());c.Parameters.AddWithValue("$l",lossPercent);c.ExecuteNonQuery();}
        if(oldLoss!=lossPercent)InsertSupplyPrice(id,price,DateTime.Now,tx);
        tx.Commit();
    }
    public long ReceiveSupply(long supplyId,long supplierId,int purchases,DateTime expiry,DateTime? at=null)
    {
        Positive(purchases,"Quantidade");var now=at??DateTime.Now;if(expiry.Date<now.Date)throw new ArgumentException("Validade vencida.");
        using var tx=db.BeginTransaction();using var c=Cmd("INSERT INTO supply_lots(supply_id,supplier_id,received_at,expires_at,quantity_milli,available_milli,frozen_cost_cents,price_version_id) SELECT s.id,$supplier,$date,$expiry,s.content_milli*$count,s.content_milli*$count,CAST(ROUND(p.purchase_price_cents*$count*(1+s.loss_pct/100.0)) AS INTEGER),p.id FROM supplies s JOIN supply_prices p ON p.id=COALESCE((SELECT id FROM supply_prices WHERE supply_id=s.id AND date(effective_at)<=date($date) ORDER BY effective_at DESC,id DESC LIMIT 1),(SELECT id FROM supply_prices WHERE supply_id=s.id ORDER BY effective_at,id LIMIT 1)) WHERE s.id=$id RETURNING id",tx);
        c.Parameters.AddWithValue("$id",supplyId);c.Parameters.AddWithValue("$supplier",supplierId);c.Parameters.AddWithValue("$date",Stamp(now));c.Parameters.AddWithValue("$expiry",Stamp(expiry));c.Parameters.AddWithValue("$count",purchases);
        var value=c.ExecuteScalar();if(value is null)throw new ArgumentException("Insumo sem preço vigente.");var id=(long)value;Event("entrada","insumo",id,purchases*1000,0,"Compra de insumo",now,tx);tx.Commit();return id;
    }
    public long AddRawMaterial(string name)
    { Name(name);using var c=Cmd("INSERT INTO raw_materials(name) VALUES($n);SELECT last_insert_rowid()");c.Parameters.AddWithValue("$n",name.Trim());return (long)c.ExecuteScalar()!; }
    public IReadOnlyList<(long Id,string Name)> ListRawMaterials()
    { using var c=Cmd("SELECT id,name FROM raw_materials WHERE active=1 ORDER BY name");using var r=c.ExecuteReader();var list=new List<(long,string)>();while(r.Read())list.Add((r.GetInt64(0),r.GetString(1)));return list; }
    public long ReceiveRaw(long materialId,long supplierId,int grams,int costCents,DateTime expiry,DateTime? at=null)
    {
        Positive(grams,"Peso");Positive(costCents,"Custo");var now=at??DateTime.Now;if(expiry.Date<now.Date)throw new ArgumentException("Validade vencida.");
        using var tx=db.BeginTransaction();using var c=Cmd("INSERT INTO raw_lots(material_id,supplier_id,received_at,expires_at,weight_g,available_g,frozen_cost_cents) VALUES($m,$s,$d,$e,$w,$w,$c);SELECT last_insert_rowid()",tx);
        c.Parameters.AddWithValue("$m",materialId);c.Parameters.AddWithValue("$s",supplierId);c.Parameters.AddWithValue("$d",Stamp(now));c.Parameters.AddWithValue("$e",Stamp(expiry));c.Parameters.AddWithValue("$w",grams);c.Parameters.AddWithValue("$c",costCents);var id=(long)c.ExecuteScalar()!;
        Event("entrada","materia_prima",id,grams*1000,costCents,"Entrada a granel",now,tx);tx.Commit();return id;
    }
    public IReadOnlyList<(long Id,string Material,int AvailableGrams,DateTime Expiry)> ListRawLots(DateTime? at=null)
    { using var c=Cmd("SELECT l.id,m.name,l.available_g,l.expires_at FROM raw_lots l JOIN raw_materials m ON m.id=l.material_id WHERE l.available_g>0 AND l.expires_at>=$today ORDER BY l.expires_at,l.received_at,l.id");c.Parameters.AddWithValue("$today",Stamp((at??DateTime.Now).Date));using var r=c.ExecuteReader();var list=new List<(long,string,int,DateTime)>();while(r.Read())list.Add((r.GetInt64(0),r.GetString(1),r.GetInt32(2),Date(r.GetString(3))));return list; }
    public long AddStockProduct(string name,decimal marginPercent,SaleUnit unit)
    {
        Name(name);if(marginPercent<0||marginPercent>=100)throw new ArgumentException("Margem deve ser menor que 100%.");
        using var tx=db.BeginTransaction();using var c=Cmd("INSERT INTO products(search_key,name,unit_price_cents,unit) VALUES($key,$n,0,$u) ON CONFLICT(search_key) DO UPDATE SET name=excluded.name;SELECT id FROM products WHERE search_key=$key",tx);
        c.Parameters.AddWithValue("$key",LocalStore.SearchKey(name));c.Parameters.AddWithValue("$n",name.Trim());c.Parameters.AddWithValue("$u",unit==SaleUnit.Kg?"KG":"UN");var id=(long)c.ExecuteScalar()!;
        using var s=Cmd("INSERT INTO stock_products(product_id,margin_pct,sale_unit) VALUES($id,$m,$u) ON CONFLICT(product_id) DO UPDATE SET margin_pct=$m,sale_unit=$u",tx);s.Parameters.AddWithValue("$id",id);s.Parameters.AddWithValue("$m",marginPercent);s.Parameters.AddWithValue("$u",unit==SaleUnit.Kg?"KG":"UN");s.ExecuteNonQuery();tx.Commit();return id;
    }
    public IReadOnlyList<(long Id,string Name,decimal Margin,SaleUnit Unit)> ListStockProducts()
    { using var c=Cmd("SELECT p.id,p.name,s.margin_pct,s.sale_unit FROM stock_products s JOIN products p ON p.id=s.product_id ORDER BY p.name");using var r=c.ExecuteReader();var list=new List<(long,string,decimal,SaleUnit)>();while(r.Read())list.Add((r.GetInt64(0),r.GetString(1),r.GetDecimal(2),r.GetString(3)=="KG"?SaleUnit.Kg:SaleUnit.Un));return list; }
    public long AddRecipe(long productId,decimal lossPercent)
    { if(lossPercent<0||lossPercent>=100)throw new ArgumentException("Perda inválida.");using var c=Cmd("INSERT INTO recipes(product_id,loss_pct,created_at) VALUES($p,$l,$d);SELECT last_insert_rowid()");c.Parameters.AddWithValue("$p",productId);c.Parameters.AddWithValue("$l",lossPercent);c.Parameters.AddWithValue("$d",Stamp(DateTime.Now));return (long)c.ExecuteScalar()!; }
    public void AddRecipeItem(long recipeId,string kind,long referenceId,decimal quantityPerKg)
    { if(kind is not ("raw" or "supply")||quantityPerKg<=0)throw new ArgumentException("Item de receita inválido.");using var c=Cmd("INSERT INTO recipe_items(recipe_id,kind,reference_id,quantity_per_kg_milli) VALUES($r,$k,$id,$q)");c.Parameters.AddWithValue("$r",recipeId);c.Parameters.AddWithValue("$k",kind);c.Parameters.AddWithValue("$id",referenceId);c.Parameters.AddWithValue("$q",checked((int)(quantityPerKg*1000)));c.ExecuteNonQuery(); }
    public void SetRecipePackaging(long recipeId,int packageWeightGrams,long supplyId,int quantityPerPackage)
    { Positive(packageWeightGrams,"Peso do pacote");Positive(quantityPerPackage,"Embalagens");using var c=Cmd("INSERT INTO recipe_packages(recipe_id,weight_g,supply_id,quantity_per_package) VALUES($r,$w,$s,$q) ON CONFLICT(recipe_id,weight_g,supply_id) DO UPDATE SET quantity_per_package=$q");c.Parameters.AddWithValue("$r",recipeId);c.Parameters.AddWithValue("$w",packageWeightGrams);c.Parameters.AddWithValue("$s",supplyId);c.Parameters.AddWithValue("$q",quantityPerPackage);c.ExecuteNonQuery(); }
    public long StartManipulation(long recipeId,IEnumerable<(long RawLotId,int Grams)> rawInputs,DateTime? at=null,int? receivedGrams=null)
    {
        var inputs=rawInputs.GroupBy(x=>x.RawLotId).Select(g=>(RawLotId:g.Key,Grams:checked(g.Sum(x=>x.Grams)))).ToArray();if(inputs.Length==0||inputs.Any(x=>x.Grams<=0))throw new ArgumentException("Informe lotes e pesos de matéria-prima.");var now=at??DateTime.Now;
        using var tx=db.BeginTransaction();int total=0,cost=0;
        foreach(var (id,grams) in inputs)
        { using var c=Cmd("SELECT weight_g,available_g,frozen_cost_cents,expires_at FROM raw_lots WHERE id=$id",tx);c.Parameters.AddWithValue("$id",id);using var r=c.ExecuteReader();if(!r.Read())throw new ArgumentException("Lote de matéria-prima inexistente.");if(r.GetInt32(1)<grams||Date(r.GetString(3)).Date<now.Date)throw new InvalidOperationException("Lote insuficiente ou vencido.");total=checked(total+grams);cost=checked(cost+(int)Math.Round((decimal)r.GetInt32(2)*grams/r.GetInt32(0),0,MidpointRounding.AwayFromZero)); }
        var received=receivedGrams??total;if(received<=0||received>total)throw new ArgumentException("Peso recebido na manipulação inválido.");
        using var insert=Cmd("INSERT INTO manipulations(recipe_id,started_at,input_g,transfer_loss_g,raw_cost_cents,status) VALUES($r,$d,$w,$loss,$c,'open');SELECT last_insert_rowid()",tx);insert.Parameters.AddWithValue("$r",recipeId);insert.Parameters.AddWithValue("$d",Stamp(now));insert.Parameters.AddWithValue("$w",received);insert.Parameters.AddWithValue("$loss",total-received);insert.Parameters.AddWithValue("$c",cost);var manipulation=(long)insert.ExecuteScalar()!;
        foreach(var (id,grams) in inputs)
        { var lotCost=Long("SELECT CAST(ROUND(frozen_cost_cents*1.0*$w/weight_g) AS INTEGER) FROM raw_lots WHERE id=$id",tx,("$w",grams),("$id",id));using var u=Cmd("UPDATE raw_lots SET available_g=available_g-$w WHERE id=$id AND available_g>=$w",tx);u.Parameters.AddWithValue("$id",id);u.Parameters.AddWithValue("$w",grams);if(u.ExecuteNonQuery()!=1)throw new InvalidOperationException("Saldo de matéria-prima alterado.");using var link=Cmd("INSERT INTO manipulation_raw(manipulation_id,raw_lot_id,used_g,cost_cents) VALUES($m,$id,$w,$c)",tx);link.Parameters.AddWithValue("$m",manipulation);link.Parameters.AddWithValue("$id",id);link.Parameters.AddWithValue("$w",grams);link.Parameters.AddWithValue("$c",lotCost);link.ExecuteNonQuery();Event("saida_para_manipulacao","materia_prima",id,grams*1000,(int)lotCost,"Baixa para manipulação",now,tx); }
        if(total>received)Event("perda_transferencia","manipulacao",manipulation,(total-received)*1000,(int)Math.Round((decimal)cost*(total-received)/total),"Diferença no recebimento da manipulação",now,tx);
        Event("entrada","manipulacao",manipulation,received*1000,cost,"Entrada na manipulação",now,tx);tx.Commit();return manipulation;
    }
    public IReadOnlyList<ReadyLot> FinishProduction(long manipulationId,int outputGrams,IEnumerable<(int PackageWeightGrams,int Quantity)> packages,IEnumerable<(long SupplyLotId,int Units)> supplies,DateTime expiry,DateTime? at=null)
        => FinishProductionDecimal(manipulationId,outputGrams,packages,supplies.Select(x=>(x.SupplyLotId,(decimal)x.Units)),expiry,at);
    public IReadOnlyList<ReadyLot> FinishProductionDecimal(long manipulationId,int outputGrams,IEnumerable<(int PackageWeightGrams,int Quantity)> packages,IEnumerable<(long SupplyLotId,decimal Units)> supplies,DateTime expiry,DateTime? at=null)
    {
        Positive(outputGrams,"Peso final");var pack=packages.ToArray();var supplyInputs=supplies.ToArray();var now=at??DateTime.Now;
        if(pack.Length==0||pack.Any(x=>x.PackageWeightGrams<=0||x.Quantity<=0)||pack.Sum(x=>x.PackageWeightGrams*x.Quantity)!=outputGrams)throw new ArgumentException("Pacotes devem somar o peso final.");
        if(expiry.Date<now.Date)throw new ArgumentException("Validade vencida.");
        using var tx=db.BeginTransaction();long recipe,product;int input,rawCost;decimal margin;string saleUnit;
        using(var c=Cmd("SELECT m.recipe_id,r.product_id,m.input_g,m.raw_cost_cents,p.margin_pct,p.sale_unit FROM manipulations m JOIN recipes r ON r.id=m.recipe_id JOIN stock_products p ON p.product_id=r.product_id WHERE m.id=$id AND m.status='open'",tx))
        { c.Parameters.AddWithValue("$id",manipulationId);using var r=c.ExecuteReader();if(!r.Read())throw new ArgumentException("Manipulação não encontrada ou concluída.");recipe=r.GetInt64(0);product=r.GetInt64(1);input=r.GetInt32(2);rawCost=r.GetInt32(3);margin=r.GetDecimal(4);saleUnit=r.GetString(5); }
        if(outputGrams>input)throw new ArgumentException("Peso final supera peso de entrada.");
        foreach(var (weight,quantity) in pack) if(Long("SELECT COUNT(*) FROM recipe_packages WHERE recipe_id=$r AND weight_g=$w",tx,("$r",recipe),("$w",weight))==0)throw new ArgumentException("Tamanho de pacote sem embalagem na receita.");
        var needed=new Dictionary<long,int>();
        foreach(var (weight,quantity) in pack)
        { using var c=Cmd("SELECT supply_id,quantity_per_package FROM recipe_packages WHERE recipe_id=$r AND weight_g=$w",tx);c.Parameters.AddWithValue("$r",recipe);c.Parameters.AddWithValue("$w",weight);using var r=c.ExecuteReader();while(r.Read())needed[r.GetInt64(0)]=needed.GetValueOrDefault(r.GetInt64(0))+r.GetInt32(1)*quantity*1000; }
        using(var requirements=Cmd("SELECT kind,reference_id,quantity_per_kg_milli FROM recipe_items WHERE recipe_id=$r",tx))
        {
            requirements.Parameters.AddWithValue("$r",recipe);using var rr=requirements.ExecuteReader();
            while(rr.Read())
            {
                var required=(int)Math.Ceiling((decimal)rr.GetInt32(2)*outputGrams/1_000_000m);
                var reference=rr.GetInt64(1);
                if(rr.GetString(0)=="supply") needed[reference]=needed.GetValueOrDefault(reference)+checked((int)Math.Ceiling((decimal)rr.GetInt32(2)*outputGrams/1000m));
                else
                {
                    var used=Long("SELECT COALESCE(SUM(mr.used_g),0) FROM manipulation_raw mr JOIN raw_lots l ON l.id=mr.raw_lot_id WHERE mr.manipulation_id=$m AND l.material_id=$id",tx,("$m",manipulationId),("$id",reference));
                    if(used<required)throw new InvalidOperationException("Matéria-prima insuficiente para a receita.");
                }
            }
        }
        int supplyCost=0;
        foreach(var (lotId,units) in supplyInputs)
        {
            if(units<=0||units*1000!=decimal.Truncate(units*1000))throw new ArgumentException("Quantidade de insumo inválida.");long supplyId,initial;int available,lotCost;DateTime valid;
            using(var c=Cmd("SELECT supply_id,quantity_milli,available_milli,frozen_cost_cents,expires_at FROM supply_lots WHERE id=$id",tx))
            { c.Parameters.AddWithValue("$id",lotId);using var r=c.ExecuteReader();if(!r.Read())throw new ArgumentException("Lote de insumo inexistente.");supplyId=r.GetInt64(0);initial=r.GetInt64(1);available=r.GetInt32(2);lotCost=r.GetInt32(3);valid=Date(r.GetString(4)); }
            var used=checked((int)(units*1000));if(available<used||valid.Date<now.Date)throw new InvalidOperationException("Insumo insuficiente ou vencido.");
            needed[supplyId]=needed.GetValueOrDefault(supplyId)-used;
            var partial=checked((int)Math.Round((decimal)lotCost*used/initial,0,MidpointRounding.AwayFromZero));supplyCost=checked(supplyCost+partial);
            using var update=Cmd("UPDATE supply_lots SET available_milli=available_milli-$q WHERE id=$id AND available_milli>=$q",tx);update.Parameters.AddWithValue("$id",lotId);update.Parameters.AddWithValue("$q",used);if(update.ExecuteNonQuery()!=1)throw new InvalidOperationException("Saldo de insumo alterado.");
            using var link=Cmd("INSERT INTO manipulation_supply(manipulation_id,supply_lot_id,used_milli,cost_cents) VALUES($m,$id,$q,$c)",tx);link.Parameters.AddWithValue("$m",manipulationId);link.Parameters.AddWithValue("$id",lotId);link.Parameters.AddWithValue("$q",used);link.Parameters.AddWithValue("$c",partial);link.ExecuteNonQuery();Event("consumo","insumo",lotId,used,partial,"Uso na produção",now,tx);
        }
        if(needed.Any(x=>x.Value>0))throw new InvalidOperationException("Embalagens insuficientes para os pacotes.");
        var settings=SettingsAt(now,tx);var fixedCost=checked((int)Math.Round((decimal)outputGrams*(settings.Labor+settings.Utilities)/1000,0,MidpointRounding.AwayFromZero));var totalCost=checked(rawCost+supplyCost+fixedCost);
        using(var u=Cmd("UPDATE manipulations SET finished_at=$d,output_g=$o,process_loss_g=$loss,supply_cost_cents=$s,fixed_cost_cents=$f,total_cost_cents=$t,status='finished' WHERE id=$id",tx))
        { u.Parameters.AddWithValue("$d",Stamp(now));u.Parameters.AddWithValue("$o",outputGrams);u.Parameters.AddWithValue("$loss",input-outputGrams);u.Parameters.AddWithValue("$s",supplyCost);u.Parameters.AddWithValue("$f",fixedCost);u.Parameters.AddWithValue("$t",totalCost);u.Parameters.AddWithValue("$id",manipulationId);u.ExecuteNonQuery(); }
        Event("perda_processo","manipulacao",manipulationId,(input-outputGrams)*1000,(int)Math.Round((decimal)rawCost*(input-outputGrams)/input),"Diferença de peso na produção",now,tx);
        var result=new List<ReadyLot>();
        foreach(var (weight,quantity) in pack)
        {
            var costPerPackage=checked((int)Math.Round((decimal)totalCost*weight/outputGrams,0,MidpointRounding.AwayFromZero));
            var pricePerKg=checked((int)Math.Ceiling((decimal)totalCost*1000/outputGrams/(1-margin/100)));
            var salePrice=saleUnit=="KG"?pricePerKg:checked((int)Math.Ceiling((decimal)pricePerKg*weight/1000));
            using var c=Cmd("INSERT INTO ready_lots(manipulation_id,product_id,created_at,expires_at,package_weight_g,quantity,available,frozen_cost_cents,unit_price_cents) VALUES($m,$p,$d,$e,$w,$q,$q,$c,$price);SELECT last_insert_rowid()",tx);
            c.Parameters.AddWithValue("$m",manipulationId);c.Parameters.AddWithValue("$p",product);c.Parameters.AddWithValue("$d",Stamp(now));c.Parameters.AddWithValue("$e",Stamp(expiry));c.Parameters.AddWithValue("$w",weight);c.Parameters.AddWithValue("$q",quantity);c.Parameters.AddWithValue("$c",costPerPackage);c.Parameters.AddWithValue("$price",salePrice);var id=(long)c.ExecuteScalar()!;
            result.Add(new(id,product,weight,quantity,quantity,0,expiry,costPerPackage,salePrice,now));Event("entrada","comercializacao",id,weight*quantity*1000,totalCost,"Pacotes prontos",now,tx);
        }
        tx.Commit();return result;
    }
    public IReadOnlyList<StockOffer> ListOffers(DateTime? at=null)
    {
        var now=at??DateTime.Now;ReleaseExpired(now);using var c=Cmd("SELECT r.product_id,p.name,r.package_weight_g,SUM(r.available),MIN(r.expires_at),r.unit_price_cents,r.frozen_cost_cents,s.sale_unit FROM ready_lots r JOIN products p ON p.id=r.product_id JOIN stock_products s ON s.product_id=r.product_id WHERE r.available>0 AND r.expires_at>=$today GROUP BY r.product_id,r.package_weight_g,r.unit_price_cents,r.frozen_cost_cents,s.sale_unit ORDER BY p.name,r.package_weight_g,MIN(r.expires_at)");c.Parameters.AddWithValue("$today",Stamp(now.Date));using var rd=c.ExecuteReader();var list=new List<StockOffer>();while(rd.Read())list.Add(new(rd.GetInt64(0),rd.GetString(1),rd.GetInt32(2),rd.GetInt32(3),Date(rd.GetString(4)),rd.GetInt32(5),rd.GetInt32(6),rd.GetString(7)=="KG"?SaleUnit.Kg:SaleUnit.Un));return list;
    }
    public long Reserve(long productId,int packageWeightGrams,int quantity,DateTime? at=null,int? unitPriceCents=null)
    {
        Positive(quantity,"Quantidade");var now=at??DateTime.Now;ReleaseExpired(now);using var tx=db.BeginTransaction();var settings=SettingsAt(now,tx);
        using var create=Cmd("INSERT INTO reservations(product_id,package_weight_g,quantity,created_at,expires_at,status) VALUES($p,$w,$q,$d,$e,'active');SELECT last_insert_rowid()",tx);
        create.Parameters.AddWithValue("$p",productId);create.Parameters.AddWithValue("$w",packageWeightGrams);create.Parameters.AddWithValue("$q",quantity);create.Parameters.AddWithValue("$d",Stamp(now));create.Parameters.AddWithValue("$e",Stamp(now.AddHours(settings.ReservationHours)));var id=(long)create.ExecuteScalar()!;
        using var lots=Cmd("SELECT id,available,frozen_cost_cents,unit_price_cents FROM ready_lots WHERE product_id=$p AND package_weight_g=$w AND available>0 AND expires_at>=$today AND ($price IS NULL OR unit_price_cents=$price) ORDER BY expires_at,created_at,id",tx);lots.Parameters.AddWithValue("$p",productId);lots.Parameters.AddWithValue("$w",packageWeightGrams);lots.Parameters.AddWithValue("$today",Stamp(now.Date));lots.Parameters.AddWithValue("$price",(object?)unitPriceCents??DBNull.Value);
        var candidates=new List<(long Id,int Available,int Cost,int Price)>();using(var r=lots.ExecuteReader())while(r.Read())candidates.Add((r.GetInt64(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3)));
        var remaining=quantity;foreach(var lot in candidates)
        { if(remaining==0)break;var take=Math.Min(remaining,lot.Available);using var u=Cmd("UPDATE ready_lots SET available=available-$q,reserved=reserved+$q WHERE id=$id AND available>=$q",tx);u.Parameters.AddWithValue("$q",take);u.Parameters.AddWithValue("$id",lot.Id);if(u.ExecuteNonQuery()!=1)throw new InvalidOperationException("Saldo do lote mudou.");using var a=Cmd("INSERT INTO reservation_allocations(reservation_id,ready_lot_id,quantity,unit_cost_cents,unit_price_cents) VALUES($r,$l,$q,$c,$p)",tx);a.Parameters.AddWithValue("$r",id);a.Parameters.AddWithValue("$l",lot.Id);a.Parameters.AddWithValue("$q",take);a.Parameters.AddWithValue("$c",lot.Cost);a.Parameters.AddWithValue("$p",lot.Price);a.ExecuteNonQuery();Event("reserva","comercializacao",lot.Id,take*packageWeightGrams*1000,take*lot.Cost,$"Reserva {id}",now,tx);remaining-=take; }
        if(remaining>0)throw new InvalidOperationException("Não há pacotes disponíveis em estoque.");tx.Commit();return id;
    }
    public StockReservation GetReservation(long id)
    { using var c=Cmd("SELECT id,product_id,package_weight_g,quantity,expires_at,status,payment FROM reservations WHERE id=$id");c.Parameters.AddWithValue("$id",id);using var r=c.ExecuteReader();if(!r.Read())throw new ArgumentException("Reserva não encontrada.");return new(r.GetInt64(0),r.GetInt64(1),r.GetInt32(2),r.GetInt32(3),Date(r.GetString(4)),r.GetString(5),r.IsDBNull(6)?null:r.GetString(6)); }
    public IReadOnlyList<StockReservation> ListReservations()
    { using var c=Cmd("SELECT id,product_id,package_weight_g,quantity,expires_at,status,payment FROM reservations ORDER BY id DESC");using var r=c.ExecuteReader();var list=new List<StockReservation>();while(r.Read())list.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt32(2),r.GetInt32(3),Date(r.GetString(4)),r.GetString(5),r.IsDBNull(6)?null:r.GetString(6)));return list; }
    public void ExtendReservation(long id,int hours,DateTime? at=null)
    { Positive(hours,"Horas");using var c=Cmd("UPDATE reservations SET expires_at=$e WHERE id=$id AND status='active'");c.Parameters.AddWithValue("$e",Stamp((at??DateTime.Now).AddHours(hours)));c.Parameters.AddWithValue("$id",id);if(c.ExecuteNonQuery()!=1)throw new InvalidOperationException("Reserva não ativa."); }
    public void ReleaseExpired(DateTime? at=null)
    { var now=at??DateTime.Now;using var c=Cmd("SELECT id FROM reservations WHERE status='active' AND expires_at<=$d");c.Parameters.AddWithValue("$d",Stamp(now));using var r=c.ExecuteReader();var ids=new List<long>();while(r.Read())ids.Add(r.GetInt64(0));foreach(var id in ids)CancelReservation(id,now,"expired"); }
    public void CancelReservation(long id,DateTime? at=null,string status="cancelled")
    {
        if(status is not ("cancelled" or "expired"))throw new ArgumentException("Estado inválido.");var now=at??DateTime.Now;using var tx=db.BeginTransaction();
        if(Long("SELECT COUNT(*) FROM reservations WHERE id=$id AND status IN ('active','paid')",tx,("$id",id))!=1)throw new InvalidOperationException("Reserva não pode ser cancelada.");
        using var c=Cmd("SELECT ready_lot_id,quantity FROM reservation_allocations WHERE reservation_id=$id",tx);c.Parameters.AddWithValue("$id",id);var allocations=new List<(long,int)>();using(var r=c.ExecuteReader())while(r.Read())allocations.Add((r.GetInt64(0),r.GetInt32(1)));
        foreach(var (lot,qty) in allocations){using var u=Cmd("UPDATE ready_lots SET reserved=reserved-$q,available=available+$q WHERE id=$id",tx);u.Parameters.AddWithValue("$q",qty);u.Parameters.AddWithValue("$id",lot);u.ExecuteNonQuery();Event(status=="expired"?"expiracao_reserva":"cancelamento_reserva","comercializacao",lot,qty*1000,0,$"Reserva {id}",now,tx);}
        using var update=Cmd("UPDATE reservations SET status=$s WHERE id=$id",tx);update.Parameters.AddWithValue("$s",status);update.Parameters.AddWithValue("$id",id);update.ExecuteNonQuery();tx.Commit();
    }
    public void MarkPaid(long id,PaymentKind payment,DateTime? at=null,long? receiptId=null)
    { var now=at??DateTime.Now;using var tx=db.BeginTransaction();using var c=Cmd("UPDATE reservations SET status='paid',payment=$p,paid_at=$d,receipt_id=COALESCE($rid,receipt_id) WHERE id=$id AND status='active' AND expires_at>$d",tx);c.Parameters.AddWithValue("$p",payment.ToString());c.Parameters.AddWithValue("$d",Stamp(now));c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$rid",(object?)receiptId??DBNull.Value);if(c.ExecuteNonQuery()!=1)throw new InvalidOperationException("Reserva expirada ou não ativa.");Event("pagamento","reserva",id,0,0,payment.ToString(),now,tx);tx.Commit(); }
    public void Deliver(long id,DateTime? at=null)
    {
        var now=at??DateTime.Now;using var tx=db.BeginTransaction();if(Long("SELECT COUNT(*) FROM reservations WHERE id=$id AND status='paid'",tx,("$id",id))!=1)throw new InvalidOperationException("Confirme o pagamento antes da entrega.");
        using var c=Cmd("SELECT ready_lot_id,quantity,unit_cost_cents,unit_price_cents FROM reservation_allocations WHERE reservation_id=$id",tx);c.Parameters.AddWithValue("$id",id);var allocations=new List<(long Id,int Qty,int Cost,int Price)>();using(var r=c.ExecuteReader())while(r.Read())allocations.Add((r.GetInt64(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3)));
        foreach(var x in allocations){if(Long("SELECT COUNT(*) FROM ready_lots WHERE id=$id AND expires_at>=$day",tx,("$id",x.Id),("$day",Stamp(now.Date)))==0)throw new InvalidOperationException("Um lote reservado venceu antes da entrega.");using var u=Cmd("UPDATE ready_lots SET reserved=reserved-$q,sold=sold+$q WHERE id=$id",tx);u.Parameters.AddWithValue("$id",x.Id);u.Parameters.AddWithValue("$q",x.Qty);u.ExecuteNonQuery();Event("entrega","comercializacao",x.Id,x.Qty*1000,x.Qty*x.Cost,$"Reserva {id}; venda {x.Qty*x.Price} centavos",now,tx);}
        using var done=Cmd("UPDATE reservations SET status='delivered',delivered_at=$d WHERE id=$id",tx);done.Parameters.AddWithValue("$id",id);done.Parameters.AddWithValue("$d",Stamp(now));done.ExecuteNonQuery();tx.Commit();
    }
    public void LinkReceipt(long reservationId,long receiptId)
    { using var c=Cmd("UPDATE reservations SET receipt_id=$rid WHERE id=$id AND status='active' AND receipt_id IS NULL");c.Parameters.AddWithValue("$id",reservationId);c.Parameters.AddWithValue("$rid",receiptId);if(c.ExecuteNonQuery()!=1)throw new InvalidOperationException("Reserva não pode ser vinculada ao cupom."); }
    public IReadOnlyList<ReadyLot> ListReadyLots()
    { using var c=Cmd("SELECT id,product_id,package_weight_g,quantity,available,reserved,expires_at,frozen_cost_cents,unit_price_cents,created_at FROM ready_lots ORDER BY expires_at,created_at,id");using var r=c.ExecuteReader();var list=new List<ReadyLot>();while(r.Read())list.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt32(2),r.GetInt32(3),r.GetInt32(4),r.GetInt32(5),Date(r.GetString(6)),r.GetInt32(7),r.GetInt32(8),Date(r.GetString(9))));return list; }
    public void RecordLoss(long readyLotId,int quantity,string reason,string description,DateTime? at=null)
    {
        if(string.IsNullOrWhiteSpace(description))throw new ArgumentException("Informe a descrição da perda.");
        if(!new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"}.Contains(reason))throw new ArgumentException("Motivo obrigatório e válido.");Positive(quantity,"Quantidade");var now=at??DateTime.Now;
        using var tx=db.BeginTransaction();using var c=Cmd("SELECT package_weight_g,frozen_cost_cents FROM ready_lots WHERE id=$id AND available>=$q",tx);c.Parameters.AddWithValue("$id",readyLotId);c.Parameters.AddWithValue("$q",quantity);using var r=c.ExecuteReader();if(!r.Read())throw new InvalidOperationException("Saldo disponível insuficiente.");var weight=r.GetInt32(0)*quantity;var cost=r.GetInt32(1)*quantity;r.Close();
        using var u=Cmd("UPDATE ready_lots SET available=available-$q,lost=lost+$q WHERE id=$id",tx);u.Parameters.AddWithValue("$id",readyLotId);u.Parameters.AddWithValue("$q",quantity);u.ExecuteNonQuery();using var loss=Cmd("INSERT INTO stock_losses(lot_kind,lot_id,reason,description,weight_g,quantity,cost_cents,created_at) VALUES('comercializacao',$id,$r,$d,$w,$q,$c,$t)",tx);loss.Parameters.AddWithValue("$id",readyLotId);loss.Parameters.AddWithValue("$r",reason);loss.Parameters.AddWithValue("$d",description);loss.Parameters.AddWithValue("$w",weight);loss.Parameters.AddWithValue("$q",quantity);loss.Parameters.AddWithValue("$c",cost);loss.Parameters.AddWithValue("$t",Stamp(now));loss.ExecuteNonQuery();Event("perda","comercializacao",readyLotId,weight*1000,cost,reason+": "+description,now,tx);tx.Commit();
    }
    public void AdjustReadyInventory(long readyLotId,int countedAvailable,string justification,DateTime? at=null)
    { if(countedAvailable<0||string.IsNullOrWhiteSpace(justification))throw new ArgumentException("Informe contagem e justificativa.");var now=at??DateTime.Now;using var tx=db.BeginTransaction();var old=Long("SELECT available FROM ready_lots WHERE id=$id",tx,("$id",readyLotId));using var c=Cmd("UPDATE ready_lots SET available=$n,quantity=quantity+($n-available) WHERE id=$id",tx);c.Parameters.AddWithValue("$id",readyLotId);c.Parameters.AddWithValue("$n",countedAvailable);if(c.ExecuteNonQuery()!=1)throw new ArgumentException("Lote não encontrado.");Event("ajuste","comercializacao",readyLotId,(countedAvailable-(int)old)*1000,0,justification,now,tx);tx.Commit(); }
    public (int Labor,int Utilities,int Delivery,decimal CardFee,int ReservationHours,int Alert1,int Alert2,string? BackupPath) GetSettings(DateTime? at=null)=>SettingsAt(at??DateTime.Now,null);
    (int Labor,int Utilities,int Delivery,decimal CardFee,int ReservationHours,int Alert1,int Alert2,string? BackupPath) SettingsAt(DateTime at,SqliteTransaction? tx)
    { using var c=Cmd("SELECT labor_cents_per_kg,utilities_cents_per_kg,delivery_cents_per_kg,card_fee_pct,reservation_hours,alert_days_1,alert_days_2,backup_path FROM stock_settings_versions WHERE effective_at<=$d ORDER BY effective_at DESC,id DESC LIMIT 1",tx);c.Parameters.AddWithValue("$d",Stamp(at));using var r=c.ExecuteReader();if(!r.Read())throw new InvalidOperationException("Configuração não encontrada.");return(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetDecimal(3),r.GetInt32(4),r.GetInt32(5),r.GetInt32(6),r.IsDBNull(7)?null:r.GetString(7)); }
    public void UpdateSettings(int labor,int utilities,int delivery,decimal cardFee,int reservationHours,int alert1,int alert2,string? backupPath,DateTime? at=null)
    { if(labor<0||utilities<0||delivery<0||cardFee<0||cardFee>=100||reservationHours<=0||alert1<0||alert2<0)throw new ArgumentException("Configuração inválida.");using var c=Cmd("INSERT INTO stock_settings_versions(effective_at,labor_cents_per_kg,utilities_cents_per_kg,delivery_cents_per_kg,card_fee_pct,reservation_hours,alert_days_1,alert_days_2,backup_path) VALUES($d,$l,$u,$e,$card,$h,$a1,$a2,$path)");c.Parameters.AddWithValue("$d",Stamp(at??DateTime.Now));c.Parameters.AddWithValue("$l",labor);c.Parameters.AddWithValue("$u",utilities);c.Parameters.AddWithValue("$e",delivery);c.Parameters.AddWithValue("$card",cardFee);c.Parameters.AddWithValue("$h",reservationHours);c.Parameters.AddWithValue("$a1",alert1);c.Parameters.AddWithValue("$a2",alert2);c.Parameters.AddWithValue("$path",(object?)backupPath??DBNull.Value);c.ExecuteNonQuery(); }
    public int CardPrice(int cashPriceCents)
    { var fee=GetSettings().CardFee;return checked((int)Math.Ceiling(cashPriceCents/(1-fee/100))); }
    public int DeliveryPrice(int totalWeightGrams)
    { var rate=GetSettings().Delivery;return checked((int)Math.Ceiling((decimal)Math.Max(1000,totalWeightGrams)*rate/1000)); }
}
