using Microsoft.Data.Sqlite;

namespace BarbaCupom;

public sealed record SupplyLotView(long Id,string Supply,string Unit,decimal Available,DateTime ExpiresAt);
public sealed record RecipeView(long Id,long ProductId,string Product,decimal LossPercent,bool Active);
public sealed record ManipulationView(long Id,string Product,int InputGrams,int? OutputGrams,string Status,DateTime StartedAt);
public sealed record StockAlert(long ReadyLotId,string Product,int Available,int Reserved,DateTime ExpiresAt,string Level);
public sealed record StockReportRow(DateTime? Date,string Product,int WeightGrams,int Packages,int CostCents,int RevenueCents,string Supplier,string Payment,string Reason);
public sealed record ProductionSuggestion(string Product,decimal DailyAverage,string BusiestDay,string Trend,int AvailablePackages,int ReservedPackages,int SuggestedPackages,string Note);

public sealed partial class InventoryStore
{
    public IReadOnlyList<SupplyLotView> ListSupplyLots(DateTime? at=null)
    { using var c=Cmd("SELECT l.id,s.name,s.use_unit,l.available_milli,l.expires_at FROM supply_lots l JOIN supplies s ON s.id=l.supply_id WHERE l.available_milli>0 ORDER BY l.expires_at,l.received_at,l.id");using var r=c.ExecuteReader();var list=new List<SupplyLotView>();while(r.Read())list.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetInt32(3)/1000m,Date(r.GetString(4))));return list; }
    public IReadOnlyList<RecipeView> ListRecipes()
    { using var c=Cmd("SELECT r.id,r.product_id,p.name,r.loss_pct,r.active FROM recipes r JOIN products p ON p.id=r.product_id ORDER BY p.name,r.id DESC");using var r=c.ExecuteReader();var list=new List<RecipeView>();while(r.Read())list.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetString(2),r.GetDecimal(3),r.GetInt32(4)==1));return list; }
    public void DeactivateRecipe(long id)
    { using var c=Cmd("UPDATE recipes SET active=0 WHERE id=$id");c.Parameters.AddWithValue("$id",id);if(c.ExecuteNonQuery()!=1)throw new ArgumentException("Receita não encontrada."); }
    public IReadOnlyList<(string Kind,long ReferenceId,string Name,decimal QuantityPerKg)> ListRecipeItems(long recipeId)
    { using var c=Cmd("SELECT i.kind,i.reference_id,CASE i.kind WHEN 'raw' THEN (SELECT name FROM raw_materials WHERE id=i.reference_id) ELSE (SELECT name FROM supplies WHERE id=i.reference_id) END,i.quantity_per_kg_milli FROM recipe_items i WHERE recipe_id=$r ORDER BY i.id");c.Parameters.AddWithValue("$r",recipeId);using var r=c.ExecuteReader();var list=new List<(string,long,string,decimal)>();while(r.Read())list.Add((r.GetString(0),r.GetInt64(1),r.GetString(2),r.GetInt32(3)/1000m));return list; }
    public IReadOnlyList<(int WeightGrams,long SupplyId,string Supply,int Quantity)> ListRecipePackages(long recipeId)
    { using var c=Cmd("SELECT p.weight_g,p.supply_id,s.name,p.quantity_per_package FROM recipe_packages p JOIN supplies s ON s.id=p.supply_id WHERE p.recipe_id=$r ORDER BY p.weight_g");c.Parameters.AddWithValue("$r",recipeId);using var r=c.ExecuteReader();var list=new List<(int,long,string,int)>();while(r.Read())list.Add((r.GetInt32(0),r.GetInt64(1),r.GetString(2),r.GetInt32(3)));return list; }
    public IReadOnlyList<ManipulationView> ListManipulations()
    { using var c=Cmd("SELECT m.id,p.name,m.input_g,m.output_g,m.status,m.started_at FROM manipulations m JOIN recipes r ON r.id=m.recipe_id JOIN products p ON p.id=r.product_id ORDER BY m.id DESC");using var r=c.ExecuteReader();var list=new List<ManipulationView>();while(r.Read())list.Add(new(r.GetInt64(0),r.GetString(1),r.GetInt32(2),r.IsDBNull(3)?null:r.GetInt32(3),r.GetString(4),Date(r.GetString(5))));return list; }
    public IReadOnlyList<(long Id,string Reason,string Description,int WeightGrams,int Quantity,int CostCents,DateTime Date)> ListLosses()
    { using var c=Cmd("SELECT id,reason,description,weight_g,quantity,cost_cents,created_at FROM stock_losses ORDER BY id DESC");using var r=c.ExecuteReader();var list=new List<(long,string,string,int,int,int,DateTime)>();while(r.Read())list.Add((r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetInt32(4),r.GetInt32(5),Date(r.GetString(6))));return list; }
    public IReadOnlyList<StockAlert> ListAlerts(DateTime? at=null)
    {
        var now=at??DateTime.Now;var settings=GetSettings(now);using var c=Cmd("SELECT r.id,p.name,r.available,r.reserved,r.expires_at FROM ready_lots r JOIN products p ON p.id=r.product_id WHERE r.available+r.reserved>0 ORDER BY r.expires_at");using var r=c.ExecuteReader();var list=new List<StockAlert>();
        while(r.Read()){var expiry=Date(r.GetString(4));var days=(expiry.Date-now.Date).Days;var level=days<0?"Vencido":days<=settings.Alert2?$"Até {settings.Alert2} dias":days<=settings.Alert1?$"Até {settings.Alert1} dias":"";if(level.Length>0)list.Add(new(r.GetInt64(0),r.GetString(1),r.GetInt32(2),r.GetInt32(3),expiry,level));}return list;
    }
    public IReadOnlyList<ReadyLot> FinishProductionAuto(long manipulationId,int outputGrams,IEnumerable<(int PackageWeightGrams,int Quantity)> packages,DateTime expiry,DateTime? at=null)
    {
        var pack=packages.ToArray();var now=at??DateTime.Now;long recipe;
        using(var c=Cmd("SELECT recipe_id FROM manipulations WHERE id=$id AND status='open'")){c.Parameters.AddWithValue("$id",manipulationId);recipe=Convert.ToInt64(c.ExecuteScalar()??throw new ArgumentException("Manipulação não encontrada."));}
        var needed=new Dictionary<long,int>();
        foreach(var (weight,qty) in pack)
            foreach(var p in ListRecipePackages(recipe).Where(x=>x.WeightGrams==weight))needed[p.SupplyId]=needed.GetValueOrDefault(p.SupplyId)+p.Quantity*qty*1000;
        foreach(var item in ListRecipeItems(recipe).Where(x=>x.Kind=="supply"))needed[item.ReferenceId]=needed.GetValueOrDefault(item.ReferenceId)+checked((int)Math.Ceiling(item.QuantityPerKg*outputGrams));
        var selected=new List<(long SupplyLotId,decimal Units)>();
        foreach(var (supplyId,amount) in needed)
        {
            var remaining=amount;using var c=Cmd("SELECT id,available_milli FROM supply_lots WHERE supply_id=$s AND available_milli>0 AND expires_at>=$today ORDER BY expires_at,received_at,id");c.Parameters.AddWithValue("$s",supplyId);c.Parameters.AddWithValue("$today",Stamp(now.Date));using var r=c.ExecuteReader();
            while(r.Read()&&remaining>0){var take=Math.Min(remaining,r.GetInt32(1));selected.Add((r.GetInt64(0),take/1000m));remaining-=take;}
            if(remaining>0)throw new InvalidOperationException("Insumos ou embalagens insuficientes.");
        }
        return FinishProductionDecimal(manipulationId,outputGrams,pack,selected,expiry,now);
    }
    public IReadOnlyList<StockReportRow> Report(DateTime from,DateTime to,long? productId=null,long? supplierId=null,string? payment=null,string? lossReason=null)
    {
        if(to.Date<from.Date||from.Year!=to.Year||to.Date>from.Date.AddYears(1))throw new ArgumentException("Selecione um período dentro de um único ano civil.");
        if(to.Year<DateTime.Today.Year)return ArchivedSummary(from.Year,productId);
        var rows=new List<StockReportRow>();
        using(var c=Cmd("""
            SELECT r.delivered_at,p.name,l.package_weight_g*a.quantity,a.quantity,a.unit_cost_cents*a.quantity,
                   CAST(ROUND(a.unit_price_cents*a.quantity*CASE s.sale_unit WHEN 'KG' THEN l.package_weight_g/1000.0 ELSE 1 END) AS INTEGER),
                   COALESCE((SELECT group_concat(DISTINCT su.name) FROM manipulation_raw mr JOIN raw_lots raw ON raw.id=mr.raw_lot_id JOIN suppliers su ON su.id=raw.supplier_id WHERE mr.manipulation_id=l.manipulation_id),''),COALESCE(r.payment,'')
            FROM reservations r JOIN reservation_allocations a ON a.reservation_id=r.id JOIN ready_lots l ON l.id=a.ready_lot_id
            JOIN products p ON p.id=l.product_id JOIN stock_products s ON s.product_id=p.id
            WHERE r.status='delivered' AND r.delivered_at>=$from AND r.delivered_at<$to
              AND ($product IS NULL OR p.id=$product) AND ($payment IS NULL OR r.payment=$payment)
              AND ($supplier IS NULL OR EXISTS(SELECT 1 FROM manipulation_raw mr JOIN raw_lots raw ON raw.id=mr.raw_lot_id WHERE mr.manipulation_id=l.manipulation_id AND raw.supplier_id=$supplier))
            ORDER BY r.delivered_at
            """))
        { c.Parameters.AddWithValue("$from",Stamp(from.Date));c.Parameters.AddWithValue("$to",Stamp(to.Date.AddDays(1)));c.Parameters.AddWithValue("$product",(object?)productId??DBNull.Value);c.Parameters.AddWithValue("$payment",(object?)payment??DBNull.Value);c.Parameters.AddWithValue("$supplier",(object?)supplierId??DBNull.Value);using var r=c.ExecuteReader();while(r.Read())rows.Add(new(Date(r.GetString(0)),r.GetString(1),r.GetInt32(2),r.GetInt32(3),r.GetInt32(4),r.GetInt32(5),r.GetString(6),r.GetString(7),""));}
        if(payment is null)
        using(var c=Cmd("SELECT l.created_at,p.name,l.weight_g,l.quantity,l.cost_cents,l.reason FROM stock_losses l LEFT JOIN ready_lots r ON l.lot_kind='comercializacao' AND r.id=l.lot_id LEFT JOIN products p ON p.id=r.product_id WHERE l.created_at>=$from AND l.created_at<$to AND ($reason IS NULL OR l.reason=$reason) AND ($product IS NULL OR p.id=$product) ORDER BY l.created_at"))
        { c.Parameters.AddWithValue("$from",Stamp(from.Date));c.Parameters.AddWithValue("$to",Stamp(to.Date.AddDays(1)));c.Parameters.AddWithValue("$reason",(object?)lossReason??DBNull.Value);c.Parameters.AddWithValue("$product",(object?)productId??DBNull.Value);using var r=c.ExecuteReader();while(r.Read())rows.Add(new(Date(r.GetString(0)),r.IsDBNull(1)?"":r.GetString(1),r.GetInt32(2),r.GetInt32(3),r.GetInt32(4),0,"","",r.GetString(5)));}
        return rows.OrderBy(x=>x.Date).ToArray();
    }
    IReadOnlyList<StockReportRow> ArchivedSummary(int year,long? productId)
    { using var c=Cmd("SELECT p.name,SUM(l.package_weight_g*a.quantity),SUM(a.quantity),SUM(a.unit_cost_cents*a.quantity),SUM(CAST(ROUND(a.unit_price_cents*a.quantity*CASE s.sale_unit WHEN 'KG' THEN l.package_weight_g/1000.0 ELSE 1 END) AS INTEGER)) FROM reservations r JOIN reservation_allocations a ON a.reservation_id=r.id JOIN ready_lots l ON l.id=a.ready_lot_id JOIN products p ON p.id=l.product_id JOIN stock_products s ON s.product_id=p.id WHERE r.status='delivered' AND substr(r.delivered_at,1,4)=$year AND ($product IS NULL OR p.id=$product) GROUP BY p.id ORDER BY p.name");c.Parameters.AddWithValue("$year",year.ToString());c.Parameters.AddWithValue("$product",(object?)productId??DBNull.Value);using var r=c.ExecuteReader();var list=new List<StockReportRow>();while(r.Read())list.Add(new(null,r.GetString(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3),r.GetInt32(4),"","","Ano anterior"));return list; }
    public IReadOnlyList<ProductionSuggestion> Forecast(DateTime? at=null)
    {
        var now=at??DateTime.Now;var products=ListStockProducts();var result=new List<ProductionSuggestion>();
        foreach(var p in products)
        {
            using var c=Cmd("SELECT substr(r.delivered_at,1,10),SUM(a.quantity) FROM reservations r JOIN reservation_allocations a ON a.reservation_id=r.id JOIN ready_lots l ON l.id=a.ready_lot_id WHERE r.status='delivered' AND l.product_id=$p AND r.delivered_at>=$from AND r.delivered_at<=$to GROUP BY substr(r.delivered_at,1,10)");c.Parameters.AddWithValue("$p",p.Id);c.Parameters.AddWithValue("$from",Stamp(now.Date.AddDays(-56)));c.Parameters.AddWithValue("$to",Stamp(now));using var rr=c.ExecuteReader();var days=new Dictionary<DateTime,int>();while(rr.Read())days[Date(rr.GetString(0)).Date]=rr.GetInt32(1);
            var first=days.Count==0?now.Date:days.Keys.Min();var observed=Math.Max(1,Math.Min(56,(now.Date-first).Days+1));var total=days.Values.Sum();var average=(decimal)total/observed;
            var weekday=Enumerable.Range(0,7).Select(i=>(Day:(DayOfWeek)i,Avg:Enumerable.Range(0,observed).Select(offset=>now.Date.AddDays(-offset)).Where(d=>d.DayOfWeek==(DayOfWeek)i).Select(d=>days.GetValueOrDefault(d)).DefaultIfEmpty().Average())).OrderByDescending(x=>x.Avg).First().Day;
            var recent=days.Where(x=>x.Key>=now.Date.AddDays(-13)).Sum(x=>x.Value);var prior=days.Where(x=>x.Key>=now.Date.AddDays(-27)&&x.Key<now.Date.AddDays(-13)).Sum(x=>x.Value);var trend=recent>prior*1.2?"Alta":recent<prior*0.8?"Queda":"Estável";
            var lots=ListReadyLots().Where(x=>x.ProductId==p.Id&&x.ExpiresAt.Date>=now.Date).ToArray();var available=lots.Sum(x=>x.Available);var reserved=lots.Sum(x=>x.Reserved);var shelf=lots.Length==0?14:Math.Max(1,(int)Math.Floor(lots.Average(x=>(x.ExpiresAt.Date-x.CreatedAt.Date).TotalDays)));
            var target=(int)Math.Ceiling(average*Math.Min(7,shelf));var suggested=Math.Max(0,target-available);
            result.Add(new(p.Name,Math.Round(average,2),System.Globalization.CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetDayName(weekday),trend,available,reserved,suggested,observed<21?"Previsão preliminar: use algumas semanas de histórico.":"Considere a validade antes de produzir."));
        }
        return result;
    }
}
