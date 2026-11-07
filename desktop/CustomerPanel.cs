namespace BarbaCupom;

public sealed class CustomerPanel : Form
{
    readonly LocalStore store;
    readonly Action<SavedCustomer> useCustomer;
    readonly TextBox search = new();
    readonly DataGridView list = new();
    readonly TextBox name = new();
    readonly TextBox phone = new();
    readonly TextBox address = new();
    readonly Label purchases = new();
    long? selectedId;

    public CustomerPanel(LocalStore store, Action<SavedCustomer> useCustomer)
    {
        this.store = store; this.useCustomer = useCustomer;
        Text = "Clientes cadastrados"; Width = 900; Height = 620; MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(29, 26, 24); ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        var title = new Label { Text = "CLIENTES CADASTRADOS", Left = 24, Top = 18, Width = 800, Height = 35, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(215, 158, 123) };
        Controls.Add(title);
        Controls.Add(new Label { Text = "Buscar por nome ou telefone", Left = 24, Top = 61, Width = 350, Height = 24 });
        search.SetBounds(24, 87, 520, 32); search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        search.TextChanged += (_, _) => RefreshList(); Controls.Add(search);
        list.SetBounds(24, 135, 830, 235); list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        list.ReadOnly = true; list.AllowUserToAddRows = false; list.AllowUserToDeleteRows = false;
        list.RowHeadersVisible = false; list.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        list.MultiSelect = false; list.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        list.Columns.Add("customer", "Cliente"); list.Columns.Add("phone", "Telefone"); list.Columns.Add("address", "Endereço"); list.Columns.Add("purchases", "Compras");
        list.Columns[0].FillWeight = 150; list.Columns[1].FillWeight = 100; list.Columns[2].FillWeight = 190; list.Columns[3].FillWeight = 65;
        list.SelectionChanged += (_, _) => LoadSelection(); list.CellDoubleClick += (_, _) => UseSelected();
        Controls.Add(list);
        AddField("Nome", name, 24, 390, 290);
        AddField("Telefone", phone, 330, 390, 220);
        AddField("Endereço", address, 24, 457, 530);
        purchases.SetBounds(570, 405, 270, 55); purchases.ForeColor = Color.FromArgb(215, 158, 123); Controls.Add(purchases);
        var save = MakeButton("Salvar alterações", 570, 483, 150); save.Click += (_, _) => Save(); Controls.Add(save);
        var use = MakeButton("Usar no cupom", 730, 483, 130); use.Click += (_, _) => UseSelected(); Controls.Add(use);
        RefreshList();
    }
    void AddField(string label, TextBox box, int x, int y, int width)
    {
        Controls.Add(new Label { Text = label, Left = x, Top = y, Width = width, Height = 23 });
        box.SetBounds(x, y + 25, width, 30); box.BackColor = Color.FromArgb(22, 20, 19); box.ForeColor = Color.White;
        Controls.Add(box);
    }
    static Button MakeButton(string label, int x, int y, int width) => new() { Text = label, Left = x, Top = y, Width = width, Height = 39, BackColor = Color.FromArgb(215, 158, 123), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
    void RefreshList()
    {
        var oldId = selectedId;
        list.Rows.Clear();
        foreach (var customer in store.FindCustomers(search.Text))
        {
            var index = list.Rows.Add(customer.Name, customer.Phone, customer.Address, store.CountReceipts(customer.Id));
            list.Rows[index].Tag = customer.Id;
            if (customer.Id == oldId) list.Rows[index].Selected = true;
        }
        if (list.Rows.Count > 0 && list.SelectedRows.Count == 0) list.Rows[0].Selected = true;
        LoadSelection();
    }
    void LoadSelection()
    {
        if (list.SelectedRows.Count == 0 || list.SelectedRows[0].Tag is not long id) { selectedId = null; return; }
        var customer = store.GetCustomer(id); if (customer is null) return;
        selectedId = id; name.Text = customer.Name; phone.Text = customer.Phone; address.Text = customer.Address;
        purchases.Text = $"Compras registradas: {store.CountReceipts(id)}";
    }
    void Save()
    {
        try
        {
            if (selectedId is null) throw new ArgumentException("Selecione um cliente.");
            store.UpdateCustomer(selectedId.Value, name.Text, phone.Text, address.Text);
            RefreshList(); MessageBox.Show(this, "Cadastro atualizado.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Confira o cadastro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    void UseSelected()
    {
        if (selectedId is null) return;
        var customer = store.GetCustomer(selectedId.Value);
        if (customer is null) return;
        useCustomer(customer); DialogResult = DialogResult.OK; Close();
    }
}
