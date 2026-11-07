using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace BarbaCupom;

public sealed class AuthStore : IDisposable
{
    const int Iterations = 310_000;
    readonly SqliteConnection db;
    public AuthStore(string? path=null)
    {
        path??=LocalStore.DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        db=new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=path,Pooling=false }.ToString());
        db.Open();using var c=db.CreateCommand();c.CommandText="PRAGMA busy_timeout=5000;CREATE TABLE IF NOT EXISTS app_login(id INTEGER PRIMARY KEY CHECK(id=1),username TEXT NOT NULL,salt BLOB NOT NULL,password_hash BLOB NOT NULL,iterations INTEGER NOT NULL,created_at TEXT NOT NULL)";c.ExecuteNonQuery();
    }
    public bool IsConfigured
    { get { using var c=db.CreateCommand();c.CommandText="SELECT COUNT(*) FROM app_login";return Convert.ToInt32(c.ExecuteScalar())==1; } }
    public void Create(string username,string password)
    {
        if(string.IsNullOrWhiteSpace(username)||username.Trim().Length>60||password.Length<10)throw new ArgumentException("Informe usuário e senha com pelo menos 10 caracteres.");
        var salt=RandomNumberGenerator.GetBytes(32);var hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,Iterations,HashAlgorithmName.SHA256,32);
        using var c=db.CreateCommand();c.CommandText="INSERT INTO app_login(id,username,salt,password_hash,iterations,created_at) VALUES(1,$u,$s,$h,$i,$d)";
        c.Parameters.AddWithValue("$u",username.Trim());c.Parameters.AddWithValue("$s",salt);c.Parameters.AddWithValue("$h",hash);c.Parameters.AddWithValue("$i",Iterations);c.Parameters.AddWithValue("$d",DateTime.Now.ToString("O"));
        try{c.ExecuteNonQuery();}catch(SqliteException e) when(e.SqliteErrorCode==19){throw new InvalidOperationException("O login único já foi criado.",e);}
    }
    public bool Verify(string username,string password)
    {
        using var c=db.CreateCommand();c.CommandText="SELECT username,salt,password_hash,iterations FROM app_login WHERE id=1";using var r=c.ExecuteReader();if(!r.Read())return false;
        var given=Rfc2898DeriveBytes.Pbkdf2(password,r.GetFieldValue<byte[]>(1),r.GetInt32(3),HashAlgorithmName.SHA256,32);
        return string.Equals(username.Trim(),r.GetString(0),StringComparison.Ordinal) && CryptographicOperations.FixedTimeEquals(given,r.GetFieldValue<byte[]>(2));
    }
    public void Dispose()=>db.Dispose();
}
