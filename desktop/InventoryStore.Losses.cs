namespace BarbaCupom;

public sealed partial class InventoryStore
{
    public void AdjustMaterialInventory(string kind,long lotId,decimal counted,string justification,DateTime? at=null)
    {
        if(kind is not ("materia_prima" or "insumo")||counted<0||string.IsNullOrWhiteSpace(justification))throw new ArgumentException("Informe tipo, contagem e justificativa.");
        var raw=kind=="materia_prima";var table=raw?"raw_lots":"supply_lots";var balance=raw?"available_g":"available_milli";var initial=raw?"weight_g":"quantity_milli";var value=checked((int)(counted*(raw?1:1000)));var now=at??DateTime.Now;
        using var tx=db.BeginTransaction();int old,cost;
        using(var c=Cmd($"SELECT {balance},CAST(ROUND(frozen_cost_cents*1.0/{initial}*($q-{balance})) AS INTEGER) FROM {table} WHERE id=$id",tx))
        {c.Parameters.AddWithValue("$id",lotId);c.Parameters.AddWithValue("$q",value);using var r=c.ExecuteReader();if(!r.Read())throw new ArgumentException("Lote não encontrado.");old=r.GetInt32(0);cost=r.GetInt32(1);}
        using(var c=Cmd($"UPDATE {table} SET {balance}=$q WHERE id=$id",tx)){c.Parameters.AddWithValue("$id",lotId);c.Parameters.AddWithValue("$q",value);c.ExecuteNonQuery();}
        Event("ajuste",kind,lotId,raw?checked((value-old)*1000):value-old,cost,justification,now,tx);tx.Commit();
    }
    public IReadOnlyList<string> MovementHistory(DateTime from,DateTime to)
    {
        if(from.Year!=to.Year||to<from)throw new ArgumentException("Selecione um único ano civil.");
        using var c=Cmd("SELECT event_at,kind,lot_kind,lot_id,quantity_milli,cost_cents,details FROM stock_events WHERE event_at>=$f AND event_at<$t ORDER BY event_at,id");c.Parameters.AddWithValue("$f",Stamp(from.Date));c.Parameters.AddWithValue("$t",Stamp(to.Date.AddDays(1)));using var r=c.ExecuteReader();var list=new List<string>();
        while(r.Read())list.Add($"{Date(r.GetString(0)):dd/MM/yyyy HH:mm} · {r.GetString(1)} · {r.GetString(2)} #{r.GetInt64(3)} · {r.GetInt64(4)/1000m:0.###} · {Money.Format(r.GetInt32(5))} · {r.GetString(6)}");return list;
    }
    public void RecordMaterialLoss(string kind,long lotId,decimal amount,string reason,string description,DateTime? at=null)
    {
        if(kind is not ("materia_prima" or "insumo"))throw new ArgumentException("Tipo de lote inválido.");
        if(amount<=0||amount*1000!=decimal.Truncate(amount*1000))throw new ArgumentException("Quantidade inválida.");
        if(string.IsNullOrWhiteSpace(description)||!new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"}.Contains(reason))throw new ArgumentException("Informe motivo e descrição da perda.");
        var now=at??DateTime.Now;var raw=kind=="materia_prima";
        var table=raw?"raw_lots":"supply_lots";var balance=raw?"available_g":"available_milli";var initial=raw?"weight_g":"quantity_milli";
        var used=checked((int)(amount*(raw?1:1000)));if(used<=0)throw new ArgumentException("Quantidade abaixo da precisão permitida.");
        using var tx=db.BeginTransaction();int cost;
        using(var c=Cmd($"SELECT {balance},{initial},frozen_cost_cents FROM {table} WHERE id=$id",tx))
        {c.Parameters.AddWithValue("$id",lotId);using var r=c.ExecuteReader();if(!r.Read()||r.GetInt32(0)<used)throw new InvalidOperationException("Saldo disponível insuficiente.");cost=checked((int)Math.Round((decimal)r.GetInt32(2)*used/r.GetInt32(1),0,MidpointRounding.AwayFromZero));}
        using(var c=Cmd($"UPDATE {table} SET {balance}={balance}-$q WHERE id=$id AND {balance}>=$q",tx))
        {c.Parameters.AddWithValue("$id",lotId);c.Parameters.AddWithValue("$q",used);if(c.ExecuteNonQuery()!=1)throw new InvalidOperationException("Saldo alterado.");}
        using(var c=Cmd("INSERT INTO stock_losses(lot_kind,lot_id,reason,description,weight_g,quantity,cost_cents,created_at) VALUES($k,$id,$r,$d,$w,$q,$c,$t)",tx))
        {c.Parameters.AddWithValue("$k",kind);c.Parameters.AddWithValue("$id",lotId);c.Parameters.AddWithValue("$r",reason);c.Parameters.AddWithValue("$d",description.Trim());c.Parameters.AddWithValue("$w",raw?used:0);c.Parameters.AddWithValue("$q",used);c.Parameters.AddWithValue("$c",cost);c.Parameters.AddWithValue("$t",Stamp(now));c.ExecuteNonQuery();}
        Event("perda",kind,lotId,raw?checked(used*1000):used,cost,reason+": "+description,now,tx);tx.Commit();
    }
    public IReadOnlyList<string> TraceReadyLot(long lotId)
    {
        var lines=new List<string>();
        using var c=Cmd("SELECT 'Matéria-prima',mr.raw_lot_id,m.name,mr.used_g,mr.cost_cents,s.name FROM ready_lots ready JOIN manipulation_raw mr ON mr.manipulation_id=ready.manipulation_id JOIN raw_lots l ON l.id=mr.raw_lot_id JOIN raw_materials m ON m.id=l.material_id JOIN suppliers s ON s.id=l.supplier_id WHERE ready.id=$id UNION ALL SELECT 'Insumo',ms.supply_lot_id,m.name,ms.used_milli/1000.0,ms.cost_cents,s.name FROM ready_lots ready JOIN manipulation_supply ms ON ms.manipulation_id=ready.manipulation_id JOIN supply_lots l ON l.id=ms.supply_lot_id JOIN supplies m ON m.id=l.supply_id JOIN suppliers s ON s.id=l.supplier_id WHERE ready.id=$id");
        c.Parameters.AddWithValue("$id",lotId);using var r=c.ExecuteReader();while(r.Read())lines.Add($"{r.GetString(0)} · lote #{r.GetInt64(1)} · {r.GetString(2)} · usado {r.GetDecimal(3):0.###} · custo {Money.Format(r.GetInt32(4))} · fornecedor {r.GetString(5)}");return lines;
    }
}
