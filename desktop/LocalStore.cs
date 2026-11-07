using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text;

namespace BarbaCupom;

public sealed record SavedCustomer(long Id, string Name, string Phone, string Address)
{
    public override string ToString() => Name;
}
public sealed record SavedProduct(long Id, string Name, int UnitPriceCents, SaleUnit Unit)
{
    public string Display => $"{Name}  ·  {Money.Format(UnitPriceCents)}/{(Unit == SaleUnit.Kg ? "kg" : "un")}";
}

public sealed class LocalStore : IDisposable
{
    readonly SqliteConnection connection;
    public static string DefaultPath => Environment.GetEnvironmentVariable("BARBA_CUPOM_DB_PATH") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarbaDefumados", "Cupom", "cadastros.db");
    public LocalStore(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString());
        connection.Open();
        Execute("PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;");
        Execute("""
            CREATE TABLE IF NOT EXISTS customers(
              id INTEGER PRIMARY KEY, name TEXT NOT NULL, phone TEXT NOT NULL, address TEXT NOT NULL,
              created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE TABLE IF NOT EXISTS products(
              id INTEGER PRIMARY KEY, search_key TEXT NOT NULL UNIQUE, name TEXT NOT NULL,
              unit_price_cents INTEGER NOT NULL, unit TEXT NOT NULL DEFAULT 'UN', updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE TABLE IF NOT EXISTS receipts(
              id INTEGER PRIMARY KEY, customer_id INTEGER REFERENCES customers(id),
              customer_name TEXT NOT NULL, phone TEXT NOT NULL, address TEXT NOT NULL,
              issued_at TEXT NOT NULL, payment TEXT NOT NULL, received_cents INTEGER,
              total_cents INTEGER NOT NULL, pix_txid TEXT);
            CREATE TABLE IF NOT EXISTS pix_settings(
              id INTEGER PRIMARY KEY CHECK(id = 1), pix_key TEXT NOT NULL,
              receiver_name TEXT NOT NULL, city TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS receipt_lines(
              id INTEGER PRIMARY KEY, receipt_id INTEGER NOT NULL REFERENCES receipts(id),
              product_id INTEGER REFERENCES products(id), product_name TEXT NOT NULL,
              quantity INTEGER NOT NULL, unit_price_cents INTEGER NOT NULL,
              total_cents INTEGER NOT NULL, unit TEXT NOT NULL DEFAULT 'UN', quantity_milli INTEGER);
            """);
        EnsureColumn("products", "unit", "TEXT NOT NULL DEFAULT 'UN'");
        EnsureColumn("receipt_lines", "unit", "TEXT NOT NULL DEFAULT 'UN'");
        EnsureColumn("receipt_lines", "quantity_milli", "INTEGER");
        EnsureColumn("receipts", "pix_txid", "TEXT");
        EnsureColumn("receipts", "delivery_cents", "INTEGER NOT NULL DEFAULT 0");
    }
    public void Dispose() => connection.Dispose();

    public long SaveCustomer(string name, string phone, string address)
    {
        ValidateCustomer(name, phone, address);
        using var cmd = Command("INSERT INTO customers(name,phone,address) VALUES($n,$p,$a); SELECT last_insert_rowid();");
        cmd.Parameters.AddWithValue("$n", name.Trim()); cmd.Parameters.AddWithValue("$p", phone.Trim()); cmd.Parameters.AddWithValue("$a", address.Trim());
        return (long)cmd.ExecuteScalar()!;
    }
    public void UpdateCustomer(long id, string name, string phone, string address)
    {
        ValidateCustomer(name, phone, address);
        using var cmd = Command("UPDATE customers SET name=$n, phone=$p, address=$a WHERE id=$id;");
        cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$n", name.Trim());
        cmd.Parameters.AddWithValue("$p", phone.Trim()); cmd.Parameters.AddWithValue("$a", address.Trim());
        if (cmd.ExecuteNonQuery() != 1) throw new ArgumentException("Cliente não encontrado.");
    }
    public SavedCustomer? GetCustomer(long id)
    {
        using var cmd = Command("SELECT id,name,phone,address FROM customers WHERE id=$id"); cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader(); return reader.Read() ? ReadCustomer(reader) : null;
    }
    public IReadOnlyList<SavedCustomer> FindCustomers(string query)
    {
        using var cmd = Command("SELECT id,name,phone,address FROM customers ORDER BY name COLLATE NOCASE");
        using var reader = cmd.ExecuteReader(); var result = new List<SavedCustomer>();
        var key = SearchKey(query);
        while (reader.Read()) { var item = ReadCustomer(reader); if (SearchKey(item.Name).Contains(key) || item.Phone.Contains(query.Trim())) result.Add(item); }
        return result;
    }
    public IReadOnlyList<SavedProduct> FindProducts(string query)
    {
        using var cmd = Command("SELECT id,name,unit_price_cents,unit FROM products ORDER BY name COLLATE NOCASE");
        using var reader = cmd.ExecuteReader(); var result = new List<SavedProduct>();
        var key = SearchKey(query);
        while (reader.Read()) { var item = new SavedProduct(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3) == "KG" ? SaleUnit.Kg : SaleUnit.Un); if (SearchKey(item.Name).Contains(key)) result.Add(item); }
        return result;
    }
    public void UpsertProducts(IEnumerable<OrderLine> lines)
    {
        using var tx = connection.BeginTransaction();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Name) || line.Name.Length > 60 || line.UnitPriceCents < 0) throw new ArgumentException("Produto inválido.");
            using var cmd = Command("INSERT INTO products(search_key,name,unit_price_cents,unit) VALUES($k,$n,$p,$u) ON CONFLICT(search_key) DO UPDATE SET name=excluded.name,unit_price_cents=excluded.unit_price_cents,unit=excluded.unit,updated_at=CURRENT_TIMESTAMP;");
            cmd.Transaction = tx; cmd.Parameters.AddWithValue("$k", SearchKey(line.Name));
            cmd.Parameters.AddWithValue("$n", line.Name.Trim()); cmd.Parameters.AddWithValue("$p", line.UnitPriceCents);
            cmd.Parameters.AddWithValue("$u", line.Unit == SaleUnit.Kg ? "KG" : "UN");
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public void SaveProduct(string name, int priceCents, SaleUnit unit)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 60 || priceCents < 0 || priceCents > 100_000_000)
            throw new ArgumentException("Confira nome e preço do produto.");
        using var cmd = Command("INSERT INTO products(search_key,name,unit_price_cents,unit) VALUES($k,$n,$p,$u) ON CONFLICT(search_key) DO UPDATE SET name=excluded.name,unit_price_cents=excluded.unit_price_cents,unit=excluded.unit,updated_at=CURRENT_TIMESTAMP");
        cmd.Parameters.AddWithValue("$k", SearchKey(name)); cmd.Parameters.AddWithValue("$n", name.Trim());
        cmd.Parameters.AddWithValue("$p", priceCents); cmd.Parameters.AddWithValue("$u", unit == SaleUnit.Kg ? "KG" : "UN");
        cmd.ExecuteNonQuery();
    }
    public void RememberProducts(IEnumerable<OrderLine> lines)
    {
        using var tx = connection.BeginTransaction();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Name) || line.Name.Length > 60 || line.UnitPriceCents < 0)
                throw new ArgumentException("Produto inválido.");
            using var cmd = Command("INSERT INTO products(search_key,name,unit_price_cents,unit) VALUES($k,$n,$p,$u) ON CONFLICT(search_key) DO NOTHING");
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("$k", SearchKey(line.Name)); cmd.Parameters.AddWithValue("$n", line.Name.Trim());
            cmd.Parameters.AddWithValue("$p", line.UnitPriceCents); cmd.Parameters.AddWithValue("$u", line.Unit == SaleUnit.Kg ? "KG" : "UN");
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public long RecordReceipt(Receipt receipt, long? customerId, IReadOnlyList<long>? reservationIds = null)
    {
        using var tx = connection.BeginTransaction();
        using var cmd = Command("INSERT INTO receipts(customer_id,customer_name,phone,address,issued_at,payment,received_cents,total_cents,pix_txid) VALUES($id,$n,$p,$a,$t,$pay,$r,$sum,$txid); SELECT last_insert_rowid();");
        cmd.Transaction = tx;
        cmd.Parameters.AddWithValue("$id", (object?)customerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$n", receipt.CustomerName); cmd.Parameters.AddWithValue("$p", receipt.Phone);
        cmd.Parameters.AddWithValue("$a", receipt.Address); cmd.Parameters.AddWithValue("$t", receipt.IssuedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$pay", receipt.Payment.ToString()); cmd.Parameters.AddWithValue("$r", (object?)receipt.ReceivedCents ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sum", receipt.TotalCents);
        cmd.Parameters.AddWithValue("$txid", (object?)receipt.PixTxId ?? DBNull.Value);
        var receiptId = (long)cmd.ExecuteScalar()!;
        using(var delivery=Command("UPDATE receipts SET delivery_cents=$d WHERE id=$id"))
        { delivery.Transaction=tx;delivery.Parameters.AddWithValue("$d",receipt.DeliveryCents);delivery.Parameters.AddWithValue("$id",receiptId);delivery.ExecuteNonQuery(); }
        foreach (var line in receipt.Lines)
        {
            using var item = Command("INSERT INTO receipt_lines(receipt_id,product_id,product_name,quantity,unit_price_cents,total_cents,unit,quantity_milli) VALUES($rid,(SELECT id FROM products WHERE search_key=$key),$n,$q,$p,$sum,$u,$qm)");
            item.Transaction = tx; item.Parameters.AddWithValue("$rid", receiptId); item.Parameters.AddWithValue("$key", SearchKey(line.Name));
            item.Parameters.AddWithValue("$n", line.Name); item.Parameters.AddWithValue("$q", line.Quantity);
            item.Parameters.AddWithValue("$u", line.Unit == SaleUnit.Kg ? "KG" : "UN"); item.Parameters.AddWithValue("$qm", line.QuantityMilli);
            item.Parameters.AddWithValue("$p", line.UnitPriceCents); item.Parameters.AddWithValue("$sum", line.TotalCents);
            item.ExecuteNonQuery();
        }
        if (reservationIds is not null)
        {
            if (reservationIds.Count != receipt.Lines.Count || reservationIds.Distinct().Count() != reservationIds.Count)
                throw new ArgumentException("Reservas do cupom inválidas.");
            foreach (var reservationId in reservationIds)
            {
                using var link = Command("UPDATE reservations SET receipt_id=$rid WHERE id=$id AND status='active' AND receipt_id IS NULL AND expires_at>$now");
                link.Transaction = tx;
                link.Parameters.AddWithValue("$rid",receiptId);link.Parameters.AddWithValue("$id",reservationId);
                link.Parameters.AddWithValue("$now",DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                if(link.ExecuteNonQuery()!=1)throw new InvalidOperationException("Uma reserva expirou ou já está em outro cupom.");
            }
        }
        tx.Commit();
        return receiptId;
    }
    public PixSettings? GetPixSettings()
    {
        using var cmd = Command("SELECT pix_key,receiver_name,city FROM pix_settings WHERE id=1");
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? new PixSettings(reader.GetString(0), reader.GetString(1), reader.GetString(2)) : null;
    }
    public void SavePixSettings(PixSettings settings)
    {
        PixCode.Validate(settings);
        using var cmd = Command("INSERT INTO pix_settings(id,pix_key,receiver_name,city) VALUES(1,$k,$n,$c) ON CONFLICT(id) DO UPDATE SET pix_key=excluded.pix_key,receiver_name=excluded.receiver_name,city=excluded.city");
        cmd.Parameters.AddWithValue("$k", settings.Key);
        cmd.Parameters.AddWithValue("$n", settings.ReceiverName.Trim());
        cmd.Parameters.AddWithValue("$c", settings.City.Trim());
        cmd.ExecuteNonQuery();
    }
    public int CountReceipts(long customerId)
    {
        using var cmd = Command("SELECT COUNT(*) FROM receipts WHERE customer_id=$id"); cmd.Parameters.AddWithValue("$id", customerId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
    static void ValidateCustomer(string name, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
            (!string.IsNullOrWhiteSpace(phone) && new string(phone.Where(char.IsDigit).ToArray()).Length < 8) || address.Length > 120)
            throw new ArgumentException("Confira nome, telefone e endereço do cliente.");
    }
    static SavedCustomer ReadCustomer(SqliteDataReader reader) => new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    SqliteCommand Command(string sql) { var cmd = connection.CreateCommand(); cmd.CommandText = sql; return cmd; }
    void Execute(string sql) { using var cmd = Command(sql); cmd.ExecuteNonQuery(); }
    void EnsureColumn(string table, string column, string definition)
    {
        using var cmd = Command($"PRAGMA table_info({table})");
        using var reader = cmd.ExecuteReader();
        var exists = false;
        while (reader.Read()) if (reader.GetString(1) == column) exists = true;
        reader.Close();
        if (!exists) Execute($"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }
    public static string SearchKey(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder();
        foreach (var c in normalized) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) buffer.Append(char.ToLowerInvariant(c));
        return buffer.ToString().Normalize(NormalizationForm.FormC);
    }
}
