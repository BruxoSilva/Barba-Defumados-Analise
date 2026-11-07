using Microsoft.Data.Sqlite;

namespace BarbaCupom;

public static class BackupManager
{
    public static string DefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BarbaDefumados","Backups");
    public static string Create(string databasePath,string folder,int retain=30)
    {
        if(!File.Exists(databasePath))throw new FileNotFoundException("Banco de dados não encontrado.",databasePath);
        if(retain<1)throw new ArgumentException("Retenção inválida.");Directory.CreateDirectory(folder);
        var destination=Path.Combine(folder,$"barba-cupom-{DateTime.Now:yyyyMMdd-HHmmss-fff}.db");
        using(var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=databasePath,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))
        using(var target=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=destination,Pooling=false}.ToString()))
        { source.Open();target.Open();source.BackupDatabase(target); }
        Validate(destination);
        foreach(var file in Directory.GetFiles(folder,"barba-cupom-*.db").OrderByDescending(File.GetCreationTimeUtc).Skip(retain))File.Delete(file);
        return destination;
    }
    public static string? RunDaily(string databasePath,string folder)
    {
        Directory.CreateDirectory(folder);
        if(Directory.GetFiles(folder,$"barba-cupom-{DateTime.Now:yyyyMMdd}-*.db").Length>0)return null;
        return Create(databasePath,folder);
    }
    public static void ScheduleRestore(string backupPath,string databasePath)
    {
        Validate(backupPath);
        if(Path.GetFullPath(backupPath).Equals(Path.GetFullPath(databasePath),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Selecione uma cópia de segurança diferente do banco atual.");
        File.WriteAllText(databasePath+".restore-pending",Path.GetFullPath(backupPath));
    }
    public static void ApplyPendingRestore(string databasePath)
    {
        var marker=databasePath+".restore-pending";if(!File.Exists(marker))return;
        var backupPath=File.ReadAllText(marker).Trim();Validate(backupPath);
        if(File.Exists(databasePath))Create(databasePath,Path.Combine(Path.GetDirectoryName(databasePath)!,"antes-restauracao"),30);
        using(var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=backupPath,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))
        using(var target=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=databasePath,Pooling=false}.ToString()))
        { source.Open();target.Open();source.BackupDatabase(target); }
        Validate(databasePath);File.Delete(marker);
    }
    static void Validate(string path)
    {
        if(!File.Exists(path))throw new FileNotFoundException("Cópia de segurança não encontrada.",path);
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());db.Open();using var c=db.CreateCommand();c.CommandText="PRAGMA quick_check";
        if(!string.Equals(Convert.ToString(c.ExecuteScalar()),"ok",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Banco SQLite inválido.");
    }
}
