using BarbaCupom;

internal static class BackupScenarios
{
    public static void Run()
    {
        var folder=Path.Combine(Path.GetTempPath(),$"barba-backup-test-{Guid.NewGuid():N}");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,"app.db");var destination=Path.Combine(folder,"copies");
        try
        {
            using(var db=new LocalStore(path))db.SaveCustomer("Antes","","");
            var copy=BackupManager.Create(path,destination,30);
            using(var db=new LocalStore(path))db.SaveCustomer("Depois","","");
            BackupManager.ScheduleRestore(copy,path);
            BackupManager.ApplyPendingRestore(path);
            using(var db=new LocalStore(path))if(db.FindCustomers("").Count!=1||db.FindCustomers("antes").Count!=1)throw new Exception("Restauração não recuperou o estado anterior");
            Console.WriteLine("Backup SQLite e restauração passaram.");
        }
        finally { Directory.Delete(folder,true); }
    }
}
