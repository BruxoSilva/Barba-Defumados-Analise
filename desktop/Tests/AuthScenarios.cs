using BarbaCupom;

internal static class AuthScenarios
{
    public static void Run()
    {
        var path=Path.Combine(Path.GetTempPath(),$"barba-login-{Guid.NewGuid():N}.db");
        try
        {
            using(var auth=new AuthStore(path))
            {
                if(auth.IsConfigured)throw new Exception("Login configurado antes do primeiro acesso");
                auth.Create("operador","Senha-forte-123");
                if(!auth.IsConfigured||!auth.Verify("operador","Senha-forte-123")||auth.Verify("operador","errada"))throw new Exception("Autenticação falhou");
                try { auth.Create("outro","OutraSenha-123"); throw new Exception("Permitiu segundo usuário"); } catch(InvalidOperationException) { }
            }
            using(var auth=new AuthStore(path)) if(!auth.Verify("operador","Senha-forte-123"))throw new Exception("Hash não persistiu");
            var bytes=File.ReadAllText(path);if(bytes.Contains("Senha-forte-123"))throw new Exception("Senha gravada em texto puro");
            Console.WriteLine("Login único e hash persistente passaram.");
        }
        finally { foreach(var file in Directory.GetFiles(Path.GetTempPath(),Path.GetFileName(path)+"*"))File.Delete(file); }
    }
}
