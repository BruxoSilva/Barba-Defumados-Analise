namespace BarbaCupom;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        BackupManager.ApplyPendingRestore(LocalStore.DefaultPath);
        using var auth = new AuthStore();
        using (var login = new LoginForm(auth))
            if (login.ShowDialog() != DialogResult.OK) return;
        using (var stock = new InventoryStore())
        {
            try { BackupManager.RunDaily(LocalStore.DefaultPath, stock.GetSettings().BackupPath ?? BackupManager.DefaultFolder); }
            catch (Exception ex) { MessageBox.Show("Não foi possível executar o backup diário: " + ex.Message, "Backup pendente", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        Application.Run(new MainForm());
    }
}
