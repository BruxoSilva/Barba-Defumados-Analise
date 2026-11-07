namespace BarbaCupom;

public sealed class ProductPanel : Form
{
    readonly LocalStore store;
    readonly Action<SavedProduct> useProduct;
    readonly TextBox search = new();
    readonly ListBox list = new();
    readonly TextBox name = new();
    readonly TextBox price = new();
    readonly ComboBox unit = new();
    long? selectedId;

    public ProductPanel(LocalStore store, Action<SavedProduct> useProduct)
    {
        this.store = store; this.useProduct = useProduct;
        Text = "Produtos cadastrados"; Width = 740; Height = 570; MinimumSize = new Size(650, 500);
        StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(29, 26, 24); ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "PRODUTOS CADASTRADOS", Left = 24, Top = 18, Width = 660, Height = 38,
            Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(215, 158, 123) });
        Controls.Add(new Label { Text = "Buscar por nome", Left = 24, Top = 64, Width = 300, Height = 24 });
        search.SetBounds(24, 90, 670, 31); search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        search.TextChanged += (_, _) => RefreshList(); Controls.Add(search);
        list.SetBounds(24, 136, 670, 210); list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        list.DisplayMember = "Display"; list.SelectedIndexChanged += (_, _) => LoadSelection();
        list.DoubleClick += (_, _) => UseSelected(); Controls.Add(list);
        AddField("Nome do produto", name, 24, 365, 320);
        AddField("Preço por unidade / kg (R$)", price, 365, 365, 190);
        Controls.Add(new Label { Text = "Unidade", Left = 575, Top = 365, Width = 115, Height = 24 });
        unit.SetBounds(575, 391, 115, 30); unit.DropDownStyle = ComboBoxStyle.DropDownList;
        unit.Items.AddRange(new object[] { "UN", "KG" }); unit.SelectedIndex = 0; Controls.Add(unit);
        var novo = MakeButton("Novo produto", 24, 453, 145);
        novo.Click += (_, _) => { list.ClearSelected(); selectedId = null; name.ReadOnly = false; name.Clear(); price.Clear(); unit.SelectedIndex = 0; name.Focus(); };
        var save = MakeButton("Salvar preço", 365, 453, 155); save.Click += (_, _) => Save();
        var use = MakeButton("Usar no cupom", 535, 453, 155); use.Click += (_, _) => UseSelected();
        Controls.Add(novo); Controls.Add(save); Controls.Add(use);
        RefreshList();
    }
    void AddField(string label, TextBox box, int x, int y, int width)
    {
        Controls.Add(new Label { Text = label, Left = x, Top = y, Width = width, Height = 24 });
        box.SetBounds(x, y + 26, width, 30); box.BackColor = Color.FromArgb(22, 20, 19); box.ForeColor = Color.White;
        Controls.Add(box);
    }
    static Button MakeButton(string text, int x, int y, int width) => new()
    {
        Text = text, Left = x, Top = y, Width = width, Height = 39,
        BackColor = Color.FromArgb(215, 158, 123), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat
    };
    void RefreshList()
    {
        var id = selectedId;
        list.BeginUpdate(); list.Items.Clear();
        foreach (var item in store.FindProducts(search.Text)) list.Items.Add(item);
        list.EndUpdate();
        var index = -1;
        if (id.HasValue)
            for (var i = 0; i < list.Items.Count; i++)
                if (list.Items[i] is SavedProduct product && product.Id == id.Value) { index = i; break; }
        if (index < 0 && list.Items.Count > 0) index = 0;
        list.SelectedIndex = index;
        if (index >= 0) LoadSelection();
    }
    void LoadSelection()
    {
        if (list.SelectedItem is not SavedProduct product) return;
        selectedId = product.Id; name.Text = product.Name; name.ReadOnly = true;
        price.Text = (product.UnitPriceCents / 100m).ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        unit.SelectedItem = product.Unit == SaleUnit.Kg ? "KG" : "UN";
    }
    void Save()
    {
        try
        {
            var cents = Money.Parse(price.Text);
            store.SaveProduct(name.Text, cents, unit.Text == "KG" ? SaleUnit.Kg : SaleUnit.Un);
            var key = LocalStore.SearchKey(name.Text);
            selectedId = store.FindProducts(name.Text).First(x => LocalStore.SearchKey(x.Name) == key).Id;
            RefreshList();
            MessageBox.Show(this, "Produto e preço salvos.", "Produtos", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Confira o produto", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    void UseSelected()
    {
        if (list.SelectedItem is not SavedProduct product) return;
        useProduct(product); DialogResult = DialogResult.OK; Close();
    }
}
