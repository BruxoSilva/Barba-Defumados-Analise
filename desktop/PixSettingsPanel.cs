namespace BarbaCupom;

public sealed class PixSettingsPanel : Form
{
    readonly LocalStore store;
    readonly TextBox key = new();
    readonly TextBox receiver = new();
    readonly TextBox city = new();

    public PixSettingsPanel(LocalStore store)
    {
        this.store = store;
        Text = "Configurar Pix"; Width = 620; Height = 390; MinimumSize = new Size(540, 350);
        StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(29, 26, 24);
        ForeColor = Color.White; Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "PIX PARA RECEBER", Left = 24, Top = 18, Width = 540, Height = 38,
            Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(215, 158, 123) });
        Controls.Add(new Label { Text = "Dados da chave da sua conta pessoal Nubank. Salvos somente neste computador.",
            Left = 24, Top = 58, Width = 550, Height = 42 });
        AddField("Chave Pix", key, 24, 103, 550);
        AddField("Nome do recebedor (como aparece no Pix)", receiver, 24, 164, 340);
        AddField("Cidade", city, 385, 164, 190);
        Controls.Add(new Label { Text = "O QR informa o valor do pedido. Confira o recebimento no app do Nubank; este programa não recebe confirmação automática.",
            Left = 24, Top = 230, Width = 550, Height = 48, ForeColor = Color.FromArgb(215, 158, 123) });
        var save = new Button { Text = "Salvar configuração", Left = 367, Top = 290, Width = 208, Height = 40,
            BackColor = Color.FromArgb(215, 158, 123), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
        save.Click += (_, _) => Save(); Controls.Add(save);
        var settings = store.GetPixSettings();
        if (settings is not null) { key.Text = settings.Key; receiver.Text = settings.ReceiverName; city.Text = settings.City; }
    }
    void AddField(string label, TextBox box, int x, int y, int width)
    {
        Controls.Add(new Label { Text = label, Left = x, Top = y, Width = width, Height = 22 });
        box.SetBounds(x, y + 24, width, 30); box.BackColor = Color.FromArgb(22, 20, 19); box.ForeColor = Color.White;
        Controls.Add(box);
    }
    void Save()
    {
        try
        {
            store.SavePixSettings(new PixSettings(key.Text.Trim(), receiver.Text.Trim(), city.Text.Trim()));
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Confira os dados Pix", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}
