using System.Globalization;

namespace BarbaCupom;

public sealed class MainForm : Form
{
    static readonly Color Background = DesktopTheme.Background;
    static readonly Color PanelColor = DesktopTheme.Surface;
    static readonly Color Copper = Color.FromArgb(215, 158, 123);
    static readonly Color Muted = Color.FromArgb(211, 193, 181);
    readonly ComboBox name = new();
    readonly TextBox phone = new();
    readonly TextBox address = new();
    readonly TextBox received = new();
    readonly RadioButton pix = new() { Text = "Pix", Checked = true };
    readonly RadioButton cash = new() { Text = "Dinheiro" };
    readonly RadioButton card = new() { Text = "Cartão" };
    readonly CheckBox delivery = new() { Text = "Entrega", AutoSize = true, ForeColor = Color.White };
    readonly Label total = new();
    readonly Label change = new();
    readonly Label status = new();
    readonly DataGridView grid = new();
    readonly PictureBox preview = new();
    readonly string logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
    readonly LocalStore store = new();
    readonly InventoryStore inventory = new();
    sealed record ReservedLine(long ReservationId, StockOffer Offer, int Packages);
    long? selectedCustomerId;
    bool selectingCustomer;
    string currentPixTxId = PixCode.NewTxId();
    Receipt? lastRecordedReceipt;
    string? lastRecordedPix;

    public MainForm()
    {
        Text = "Barba Defumados BBQ — Cupom não fiscal";
        Width = Math.Min(1340, Screen.PrimaryScreen?.WorkingArea.Width ?? 1340);
        Height = Math.Min(860, (Screen.PrimaryScreen?.WorkingArea.Height ?? 900) - 16);
        MinimumSize = new Size(920, 690);
        StartPosition = FormStartPosition.CenterScreen; BackColor = Background; ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        var icon = Path.Combine(AppContext.BaseDirectory, "assets", "logo.ico");
        if (File.Exists(icon)) Icon = new Icon(icon);
        BuildLayout();
        DesktopTheme.Apply(this);
        UpdateTotal();
        RefreshCustomers();
        var maintenance = new System.Windows.Forms.Timer { Interval = 60_000 };
        maintenance.Tick += (_, _) =>
        {
            try
            {
                inventory.ReleaseExpired();
                BackupManager.RunDaily(LocalStore.DefaultPath, inventory.GetSettings().BackupPath ?? BackupManager.DefaultFolder);
            }
            catch (Exception ex) { status.Text = "Backup/manutenção pendente: " + ex.Message; }
        };
        maintenance.Start();
        FormClosed += (_, _) => maintenance.Dispose();
        FormClosed += (_, _) => { store.Dispose(); inventory.Dispose(); };
    }

    void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Background };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        var header = new Panel { Dock = DockStyle.Fill, BackColor = PanelColor, Padding = new Padding(28, 12, 12, 12) };
        root.Controls.Add(header, 0, 0);
        var brand = new PictureBox { Left = 28, Top = 10, Width = 100, Height = 100, SizeMode = PictureBoxSizeMode.Zoom };
        if (File.Exists(logoPath)) brand.Image = Image.FromFile(logoPath);
        header.Controls.Add(brand);
        header.Controls.Add(new Label { Text = "BARBA DEFUMADOS  B B Q", Left = 145, Top = 22, Width = 710, Height = 44, ForeColor = Copper, Font = new Font("Georgia", 23, FontStyle.Bold) });
        header.Controls.Add(new Label { Text = "Novo pedido  /  Emissão de cupom", Left = 148, Top = 70, Width = 650, Height = 28, ForeColor = Muted });
        var customersButton = Button("Clientes cadastrados", false);
        customersButton.Width = 205; customersButton.Left = 1090; customersButton.Top = 44;
        customersButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        customersButton.Click += (_, _) => { using var panel = new CustomerPanel(store, SelectCustomer); panel.ShowDialog(this); RefreshCustomers(); };
        header.Controls.Add(customersButton);
        var pixSettingsButton = Button("Configurar Pix", false);
        pixSettingsButton.Width = 150; pixSettingsButton.Left = 925; pixSettingsButton.Top = 60;
        pixSettingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pixSettingsButton.Click += (_, _) => { using var panel = new PixSettingsPanel(store); panel.ShowDialog(this); };
        header.Controls.Add(pixSettingsButton);
        var inventoryButton = Button("ESTOQUE", false);
        inventoryButton.Width = 150; inventoryButton.Left = 925; inventoryButton.Top = 10;
        inventoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        inventoryButton.Click += (_, _) => { using var panel = new StockPanel(inventory); panel.ShowDialog(this); };
        header.Controls.Add(inventoryButton);

        var split = new SplitContainer { Dock = DockStyle.Fill, Width = 1300, Height = 750, SplitterDistance = 880, BackColor = Background, Padding = new Padding(20), Panel1MinSize = 590, Panel2MinSize = 250 };
        root.Controls.Add(split, 0, 1);
        split.Panel1.BackColor = Background; split.Panel2.BackColor = Background;
        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Background, Padding = new Padding(0, 8, 16, 0) };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        split.Panel1.Controls.Add(editor);

        var client = Group("DADOS DO CLIENTE"); editor.Controls.Add(client, 0, 0);
        var clientGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10, 4, 10, 8) };
        clientGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); clientGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        clientGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        clientGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        client.Controls.Add(clientGrid);
        name.DropDownStyle = ComboBoxStyle.DropDown;
        name.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        name.AutoCompleteSource = AutoCompleteSource.ListItems;
        name.SelectedIndexChanged += (_, _) => { if (name.SelectedItem is SavedCustomer customer) SelectCustomer(customer); };
        name.TextChanged += (_, _) => { if (!selectingCustomer && (name.SelectedItem is not SavedCustomer customer || name.Text != customer.Name)) selectedCustomerId = null; };
        clientGrid.Controls.Add(Field("Nome do cliente", name), 0, 0); clientGrid.Controls.Add(Field("Telefone", phone), 1, 0);
        clientGrid.Controls.Add(Field("Endereço de entrega / retirada", address), 0, 1); clientGrid.SetColumnSpan(clientGrid.GetControlFromPosition(0, 1)!, 2);

        var products = Group("PRODUTOS"); editor.Controls.Add(products, 0, 1);
        var productLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10, 8, 10, 8) };
        productLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); productLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        products.Controls.Add(productLayout); SetupGrid(); productLayout.Controls.Add(grid, 0, 0);
        var rowButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var add = Button("+ Reservar pacote", false); add.Click += (_, _) => PickProduct();
        var remove = Button("Cancelar reserva", false); remove.Click += (_, _) => { if (grid.CurrentRow?.Tag is ReservedLine line) { inventory.CancelReservation(line.ReservationId); grid.Rows.Remove(grid.CurrentRow); UpdateTotal(); } };
        var findProduct = Button("Buscar no estoque", false); findProduct.Click += (_, _) => PickProduct();
        var manageProducts = Button("Gerenciar estoque", false);
        manageProducts.Click += (_, _) => { using var panel = new StockPanel(inventory); panel.ShowDialog(this); };
        rowButtons.Controls.Add(add); rowButtons.Controls.Add(remove); rowButtons.Controls.Add(findProduct); rowButtons.Controls.Add(manageProducts); productLayout.Controls.Add(rowButtons, 0, 1);

        var payment = Group("PAGAMENTO"); editor.Controls.Add(payment, 0, 2);
        var payLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 0, 0), WrapContents = false };
        payment.Controls.Add(payLayout);
        pix.AutoSize = true; cash.AutoSize = true; card.AutoSize = true; pix.ForeColor = Color.White; cash.ForeColor = Color.White; card.ForeColor = Color.White;
        pix.Margin = new Padding(0, 8, 22, 0); cash.Margin = new Padding(0, 8, 22, 0); card.Margin = new Padding(0, 8, 22, 0);
        payLayout.Controls.Add(pix); payLayout.Controls.Add(cash); payLayout.Controls.Add(card);
        payLayout.Controls.Add(delivery);delivery.CheckedChanged+=(_,_)=>UpdateTotal();
        var receivedField = Field("Valor recebido (R$)", received); receivedField.Width = 180; payLayout.Controls.Add(receivedField);
        change.Text = "Troco: —"; change.ForeColor = Copper; change.AutoSize = true; change.Font = new Font("Segoe UI", 11, FontStyle.Bold); change.Margin = new Padding(22, 33, 0, 0); payLayout.Controls.Add(change);
        cash.CheckedChanged += (_, _) => { receivedField.Visible = cash.Checked; change.Visible = cash.Checked; UpdateTotal(); };
        card.CheckedChanged += (_, _) => UpdateTotal();
        received.TextChanged += (_, _) => UpdateTotal(); receivedField.Visible = false; change.Visible = false;

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 10, 0, 0) };
        editor.Controls.Add(bottom, 0, 3);
        var issue = Button("Emitir cupom", true); issue.Click += (_, _) => Issue(); bottom.Controls.Add(issue);
        var view = Button("Pré-visualizar", false); view.Click += (_, _) => Preview(); bottom.Controls.Add(view);
        var clear = Button("Limpar tela", false); clear.Click += (_, _) => Clear(); bottom.Controls.Add(clear);
        var reprint=Button("Reimprimir último",false);reprint.Click+=(_,_)=>{try{if(lastRecordedReceipt is null)throw new InvalidOperationException("Nenhum cupom registrado nesta sessão.");ReceiptRenderer.Print(lastRecordedReceipt,logoPath,pixPayload:lastRecordedPix);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Reimpressão");}};bottom.Controls.Add(reprint);
        status.ForeColor = Copper; status.AutoSize = true; status.Margin = new Padding(16, 16, 0, 0); bottom.Controls.Add(status);

        var summary = Group("RESUMO E CUPOM"); summary.Dock = DockStyle.Fill; split.Panel2.Controls.Add(summary);
        var summaryLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(14) };
        summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75)); summaryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); summaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); summary.Controls.Add(summaryLayout);
        total.ForeColor = Copper; total.Font = new Font("Segoe UI", 23, FontStyle.Bold); total.Dock = DockStyle.Fill; summaryLayout.Controls.Add(total, 0, 0);
        summaryLayout.Controls.Add(new Label { Text = "Bobina 80 mm · área imprimível 72 mm", ForeColor = Muted, Dock = DockStyle.Fill }, 0, 1);
        preview.Dock = DockStyle.Fill; preview.BackColor = Color.White; preview.SizeMode = PictureBoxSizeMode.Zoom; summaryLayout.Controls.Add(preview, 0, 2);
    }

    void SetupGrid()
    {
        grid.Dock = DockStyle.Fill; grid.AllowUserToAddRows = false; grid.RowHeadersVisible = false; grid.ReadOnly = true;
        grid.BackgroundColor = PanelColor; grid.BorderStyle = BorderStyle.None; grid.GridColor = Copper;
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(77, 55, 43);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; grid.ColumnHeadersHeight = 34;
        grid.DefaultCellStyle.BackColor = PanelColor; grid.DefaultCellStyle.ForeColor = Color.White;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(100, 67, 50); grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowTemplate.Height = 34; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add("product", "Nome do produto");
        var unitColumn = new DataGridViewComboBoxColumn { Name = "unit", HeaderText = "Unid.", FlatStyle = FlatStyle.Flat };
        unitColumn.Items.AddRange("UN", "KG"); grid.Columns.Add(unitColumn);
        grid.Columns.Add("quantity", "Qtd. / Peso (kg)"); grid.Columns.Add("price", "Preço por UN/KG (R$)");
        grid.Columns.Add("lineTotal", "Total (R$)"); grid.Columns[0].FillWeight = 220; grid.Columns[1].FillWeight = 50;
        grid.Columns[2].FillWeight = 115; grid.Columns[3].FillWeight = 125; grid.Columns[4].FillWeight = 100; grid.Columns[4].ReadOnly = true;
        grid.CellEndEdit += (_, e) =>
        {
            if (e.ColumnIndex == 0) TryFillSavedProduct(e.RowIndex);
            if (e.ColumnIndex == 2 && e.RowIndex >= 0)
            {
                var entered = Convert.ToString(grid.Rows[e.RowIndex].Cells[2].Value) ?? "";
                if (entered.Contains('.') || entered.Contains(',')) grid.Rows[e.RowIndex].Cells[1].Value = "KG";
            }
            UpdateTotal();
        };
        grid.CellValueChanged += (_, e) => { if (e.ColumnIndex is >= 0 and < 4) UpdateTotal(); };
        grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty && grid.CurrentCell?.ColumnIndex == 1) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.RowsAdded += (_, _) => UpdateTotal(); grid.RowsRemoved += (_, _) => UpdateTotal();
    }

    static GroupBox Group(string title) => new() { Text = title, Dock = DockStyle.Fill, ForeColor = Copper, BackColor = PanelColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(8) };
    static Panel Field(string label, TextBox box)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4, 2, 10, 2) };
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 24, ForeColor = Color.White, Font = new Font("Segoe UI", 9) });
        box.Dock = DockStyle.Bottom; box.Height = 30; box.BackColor = Color.FromArgb(22, 20, 19); box.ForeColor = Color.White;
        box.BorderStyle = BorderStyle.FixedSingle; panel.Controls.Add(box); return panel;
    }
    static Panel Field(string label, ComboBox box)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4, 2, 10, 2) };
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 24, ForeColor = Color.White, Font = new Font("Segoe UI", 9) });
        box.Dock = DockStyle.Bottom; box.Height = 30; box.BackColor = Color.FromArgb(22, 20, 19); box.ForeColor = Color.White;
        box.FlatStyle = FlatStyle.Flat; panel.Controls.Add(box); return panel;
    }
    static Button Button(string text, bool primary) => new() { Text = text, AutoSize = true, Height = 42, BackColor = primary ? Copper : Color.FromArgb(65, 48, 40), ForeColor = primary ? Color.Black : Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

    void UpdateTotal()
    {
        if (grid.Columns.Count == 0) return;
        int sum = 0;
        foreach (DataGridViewRow row in grid.Rows)
        {
            try
            {
                var entered = Convert.ToString(row.Cells[2].Value) ?? "";
                if (ReadUnit(row) == SaleUnit.Un && (entered.Contains('.') || entered.Contains(','))) row.Cells[1].Value = "KG";
                var saleUnit = ReadUnit(row);
                var qty = QuantityInput.Parse(entered, saleUnit);
                var price = Money.Parse(Convert.ToString(row.Cells[3].Value) ?? "");
                var value = new OrderLine("Produto", qty, price, saleUnit).TotalCents;
                row.Cells[4].Value = Money.Format(value); sum = checked(sum + value);
            }
            catch { row.Cells[4].Value = "—"; }
        }
        if(delivery.Checked)sum=checked(sum+inventory.DeliveryPrice(CurrentWeight()));
        total.Text = Money.Format(card.Checked ? inventory.CardPrice(sum) : sum);
        try { change.Text = "Troco: " + Money.Format(Math.Max(0, Money.Parse(received.Text) - sum)); }
        catch { change.Text = "Troco: —"; }
    }

    Receipt ReadReceipt(bool issuing)
    {
        grid.EndEdit();
        UpdateTotal();
        var lines = new List<OrderLine>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is not ReservedLine selected) throw new InvalidOperationException("Selecione apenas produtos do estoque.");
            var reservation = inventory.GetReservation(selected.ReservationId);
            if (reservation.Status != "active" || reservation.ExpiresAt <= DateTime.Now) throw new InvalidOperationException("Uma reserva expirou. Selecione novamente o pacote.");
            if (selected.Offer.ExpiresAt.Date < DateTime.Today) throw new InvalidOperationException("Um pacote está vencido.");
            var qty = selected.Offer.SaleUnit == SaleUnit.Kg ? selected.Packages * selected.Offer.PackageWeightGrams / 1000m : selected.Packages;
            lines.Add(new OrderLine(selected.Offer.ProductName, qty, selected.Offer.UnitPriceCents, selected.Offer.SaleUnit));
        }
        var receivedCents = cash.Checked ? Money.Parse(received.Text) : (int?)null;
        return new Receipt(name.Text, phone.Text, address.Text, lines, cash.Checked ? PaymentKind.Cash : card.Checked ? PaymentKind.Card : PaymentKind.Pix,
            receivedCents, issuing ? DateTime.Now : DateTime.MinValue, pix.Checked ? currentPixTxId : null, inventory.GetSettings().CardFee,delivery.Checked?inventory.DeliveryPrice(CurrentWeight()):0);
    }
    void ShowPreview(Receipt receipt, string? pixPayload)
    {
        var previous = preview.Image;
        preview.Image = ReceiptRenderer.Render(receipt, logoPath, pixPayload); previous?.Dispose();
    }
    int CurrentWeight()=>grid.Rows.Cast<DataGridViewRow>().Where(x=>x.Tag is ReservedLine).Sum(x=>((ReservedLine)x.Tag!).Packages*((ReservedLine)x.Tag!).Offer.PackageWeightGrams);
    void Preview()
    {
        try
        {
            var receipt = ReadReceipt(false);
            if (!ConfirmMissingCustomerDetails(receipt, false)) return;
            if (!TryGetPixPayload(receipt, out var pixPayload)) return;
            ShowPreview(receipt, pixPayload); status.Text = "Pré-visualização pronta.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Confira o pedido", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    void Issue()
    {
        try
        {
            var receipt = ReadReceipt(true);
            if (!ConfirmMissingCustomerDetails(receipt, true)) return;
            if (!TryGetPixPayload(receipt, out var pixPayload)) return;
            long? customerId = selectedCustomerId;
            if (customerId is null)
            {
                var answer = MessageBox.Show(this,
                    $"Deseja salvar o cadastro de {receipt.CustomerName} antes de imprimir?\n\nSim: cadastrar e imprimir.\nNão: imprimir sem cadastrar.\nCancelar: voltar ao formulário.",
                    "Cadastrar cliente?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) return;
                if (answer == DialogResult.Yes)
                {
                    customerId = store.SaveCustomer(receipt.CustomerName, receipt.Phone, receipt.Address);
                    selectedCustomerId = customerId;
                    RefreshCustomers();
                }
            }
            ShowPreview(receipt, pixPayload);
            try
            {
                var reservationIds = grid.Rows.Cast<DataGridViewRow>().Select(row => ((ReservedLine)row.Tag!).ReservationId).ToArray();
                var receiptId = store.RecordReceipt(receipt, customerId, reservationIds);
                foreach (DataGridViewRow row in grid.Rows)
                    row.Tag = null;
                lastRecordedReceipt=receipt;lastRecordedPix=pixPayload;
                currentPixTxId = PixCode.NewTxId();
                try { ReceiptRenderer.Print(receipt, logoPath, pixPayload: pixPayload); }
                catch(Exception printError)
                {MessageBox.Show(this,"Cupom salvo. A impressão falhou: "+printError.Message+"\nUse Reimprimir último para tentar novamente sem duplicar o registro.","Impressão pendente");return;}
                status.Text = $"Enviado à Elgin às {receipt.IssuedAt:HH:mm}. Confirme o pagamento e a entrega em Estoque.";
            }
            catch (Exception saveError)
            {
                MessageBox.Show(this, $"O registro do cupom falhou: {saveError.Message}\nA impressão não foi iniciada.",
                    "Falha ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Não foi possível imprimir", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    void Clear()
    {
        selectingCustomer = true; name.SelectedIndex = -1; name.Text = ""; selectingCustomer = false; selectedCustomerId = null;
        phone.Clear(); address.Clear(); received.Clear(); pix.Checked = true;
        currentPixTxId = PixCode.NewTxId();
        foreach (DataGridViewRow row in grid.Rows)
            if (row.Tag is ReservedLine reserved && inventory.GetReservation(reserved.ReservationId).Status == "active") inventory.CancelReservation(reserved.ReservationId);
        grid.Rows.Clear();
        preview.Image?.Dispose(); preview.Image = null; status.Text = ""; UpdateTotal();
    }
    void RefreshCustomers()
    {
        var currentText = name.Text;
        selectingCustomer = true; name.BeginUpdate(); name.Items.Clear();
        foreach (var customer in store.FindCustomers("")) name.Items.Add(customer);
        name.Text = currentText; name.EndUpdate(); selectingCustomer = false;
    }
    void SelectCustomer(SavedCustomer customer)
    {
        selectingCustomer = true;
        name.Text = customer.Name; phone.Text = customer.Phone; address.Text = customer.Address;
        selectedCustomerId = customer.Id; selectingCustomer = false;
        status.Text = $"Cliente cadastrado selecionado: {customer.Name}.";
    }
    void PickProduct()
    {
        if (grid.Rows.Count >= 12) { MessageBox.Show(this,"O cupom aceita até 12 linhas."); return; }
        using var picker = new StockPicker(inventory);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected is null) return;
        try
        {
            var offer = picker.Selected;
            var reservationId = inventory.Reserve(offer.ProductId, offer.PackageWeightGrams, picker.Quantity, unitPriceCents: offer.UnitPriceCents);
            var quantity = offer.SaleUnit == SaleUnit.Kg ? picker.Quantity * offer.PackageWeightGrams / 1000m : picker.Quantity;
            var row = grid.Rows[grid.Rows.Add(offer.ProductName, offer.SaleUnit == SaleUnit.Kg ? "KG" : "UN", quantity.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR")), (offer.UnitPriceCents / 100m).ToString("N2", CultureInfo.GetCultureInfo("pt-BR")), "")];
            row.Tag = new ReservedLine(reservationId, offer, picker.Quantity);
            UpdateTotal(); status.Text = $"{picker.Quantity} pacote(s) reservado(s) por 24 h. Reserva #{reservationId}.";
        }
        catch (Exception ex) { MessageBox.Show(this,ex.Message,"Estoque",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    }
    void UseProduct(SavedProduct product)
    {
        if (grid.CurrentRow is null) grid.Rows.Add("", "UN", "1", "", "R$ 0,00");
        var row = grid.CurrentRow ?? grid.Rows[0];
        var existingUnit = ReadUnit(row);
        var entered = Convert.ToString(row.Cells[2].Value) ?? "";
        row.Cells[0].Value = product.Name;
        row.Cells[1].Value = product.Unit == SaleUnit.Kg ? "KG" : "UN";
        var keepQuantity = existingUnit == product.Unit &&
            decimal.TryParse(entered.Replace('.', ','), System.Globalization.NumberStyles.Number,
                CultureInfo.GetCultureInfo("pt-BR"), out var quantity) && quantity > 0;
        row.Cells[2].Value = keepQuantity ? entered : product.Unit == SaleUnit.Kg ? "0,000" : "1";
        row.Cells[3].Value = (product.UnitPriceCents / 100m).ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
        UpdateTotal();
    }
    bool ConfirmMissingCustomerDetails(Receipt receipt, bool printing)
    {
        if (!string.IsNullOrWhiteSpace(receipt.Phone) && !string.IsNullOrWhiteSpace(receipt.Address)) return true;
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(receipt.Phone)) missing.Add("telefone");
        if (string.IsNullOrWhiteSpace(receipt.Address)) missing.Add("endereço");
        var action = printing ? "imprimir" : "pré-visualizar";
        return MessageBox.Show(this, $"Faltam dados do cliente: {string.Join(" e ", missing)}. Deseja {action} o cupom assim mesmo?\n\nSim: continuar. Não: voltar ao formulário.",
            "Dados do cliente incompletos", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
    bool TryGetPixPayload(Receipt receipt, out string? payload)
    {
        payload = null;
        if (receipt.Payment != PaymentKind.Pix) return true;
        var settings = store.GetPixSettings();
        if (settings is null)
        {
            var choice = MessageBox.Show(this,
                "O Pix ainda não foi configurado.\n\nSim: configurar sua chave Pix agora.\nNão: continuar sem QR Code.\nCancelar: voltar ao formulário.",
                "Configurar Pix", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel) return false;
            if (choice == DialogResult.No) return true;
            using var panel = new PixSettingsPanel(store);
            if (panel.ShowDialog(this) != DialogResult.OK) return false;
            settings = store.GetPixSettings();
            if (settings is null) return false;
        }
        payload = PixCode.Build(settings, receipt.TotalCents, receipt.PixTxId!);
        return true;
    }
    static SaleUnit ReadUnit(DataGridViewRow row) => Convert.ToString(row.Cells[1].Value) == "KG" ? SaleUnit.Kg : SaleUnit.Un;
    void TryFillSavedProduct(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
        var row = grid.Rows[rowIndex];
        var productName = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
        var saved = store.FindProducts(productName).FirstOrDefault(x => LocalStore.SearchKey(x.Name) == LocalStore.SearchKey(productName));
        if (saved is null) return;
        var existingUnit = ReadUnit(row);
        var entered = Convert.ToString(row.Cells[2].Value) ?? "";
        row.Cells[1].Value = saved.Unit == SaleUnit.Kg ? "KG" : "UN";
        row.Cells[2].Value = existingUnit == saved.Unit &&
            decimal.TryParse(entered.Replace('.', ','), System.Globalization.NumberStyles.Number,
                CultureInfo.GetCultureInfo("pt-BR"), out var quantity) && quantity > 0
            ? entered : saved.Unit == SaleUnit.Kg ? "0,000" : "1";
        row.Cells[3].Value = (saved.UnitPriceCents / 100m).ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
    }
}
