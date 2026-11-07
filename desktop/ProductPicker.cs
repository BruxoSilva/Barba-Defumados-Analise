namespace BarbaCupom;

public sealed class ProductPicker : Form
{
    readonly LocalStore store;
    readonly TextBox search = new();
    readonly ListBox list = new();
    public SavedProduct? Selected { get; private set; }
    public ProductPicker(LocalStore store)
    {
        this.store = store;
        Text = "Buscar produto"; Width = 620; Height = 440; StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(29, 26, 24); ForeColor = Color.White; Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "Digite letras do nome do produto", Left = 24, Top = 22, Width = 400, Height = 25 });
        search.SetBounds(24, 52, 540, 32); search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        search.TextChanged += (_, _) => RefreshList(); Controls.Add(search);
        list.SetBounds(24, 100, 540, 245); list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.DisplayMember = "Display"; list.DoubleClick += (_, _) => Choose(); Controls.Add(list);
        var choose = new Button { Text = "Usar produto", Left = 405, Top = 355, Width = 160, Height = 39, BackColor = Color.FromArgb(215, 158, 123), ForeColor = Color.Black };
        choose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; choose.Click += (_, _) => Choose(); Controls.Add(choose);
        RefreshList();
    }
    void RefreshList()
    {
        list.BeginUpdate(); list.Items.Clear();
        foreach (var item in store.FindProducts(search.Text)) list.Items.Add(item);
        list.EndUpdate();
        if (list.Items.Count > 0) list.SelectedIndex = 0;
    }
    void Choose()
    {
        if (list.SelectedItem is not SavedProduct product) return;
        Selected = product; DialogResult = DialogResult.OK; Close();
    }
}
