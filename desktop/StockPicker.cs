namespace BarbaCupom;

public sealed class StockPicker : Form
{
    readonly InventoryStore inventory;
    readonly TextBox search=new();
    readonly ListBox list=new();
    readonly NumericUpDown quantity=new();
    public StockOffer? Selected { get; private set; }
    public int Quantity => (int)quantity.Value;
    public StockPicker(InventoryStore inventory)
    {
        this.inventory=inventory;Text="Selecionar pacote do estoque";Width=760;Height=510;StartPosition=FormStartPosition.CenterParent;
        BackColor=Color.FromArgb(29,26,24);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        Controls.Add(new Label{Text="PACOTES DISPONÍVEIS",Left=22,Top=20,Width=600,Height=36,ForeColor=Color.FromArgb(215,158,123),Font=new Font("Segoe UI",18,FontStyle.Bold)});
        Controls.Add(new Label{Text="Busque por letras do produto",Left=22,Top=65,Width=400,Height=24});
        search.SetBounds(22,91,700,32);search.TextChanged+=(_,_)=>RefreshList();Controls.Add(search);
        list.SetBounds(22,140,700,235);list.DisplayMember="Display";list.SelectedIndexChanged+=(_,_)=>UpdateLimit();list.DoubleClick+=(_,_)=>Choose();Controls.Add(list);
        Controls.Add(new Label{Text="Pacotes",Left=22,Top=393,Width=100,Height=26});
        quantity.SetBounds(120,390,100,32);quantity.Minimum=1;quantity.Maximum=1;Controls.Add(quantity);
        var choose=new Button{Text="Reservar para cupom",Left=490,Top=388,Width=232,Height=43,BackColor=Color.FromArgb(215,158,123),ForeColor=Color.Black,FlatStyle=FlatStyle.Flat};choose.Click+=(_,_)=>Choose();Controls.Add(choose);
        DesktopTheme.Apply(this);RefreshList();
    }
    void RefreshList()
    {
        var key=LocalStore.SearchKey(search.Text);list.BeginUpdate();list.Items.Clear();
        foreach(var offer in inventory.ListOffers().Where(x=>LocalStore.SearchKey(x.ProductName).Contains(key)))list.Items.Add(offer);
        list.EndUpdate();if(list.Items.Count>0)list.SelectedIndex=0;UpdateLimit();
    }
    void UpdateLimit(){quantity.Maximum=Math.Max(1,(list.SelectedItem as StockOffer)?.Available??1);quantity.Value=Math.Min(quantity.Value,quantity.Maximum);}
    void Choose(){if(list.SelectedItem is not StockOffer offer)return;Selected=offer;DialogResult=DialogResult.OK;Close();}
}
