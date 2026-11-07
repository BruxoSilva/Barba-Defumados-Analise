using System.Drawing.Drawing2D;

namespace BarbaCupom;

public static class DesktopTheme
{
    public static readonly Color Background=Color.FromArgb(20,22,24);
    public static readonly Color Surface=Color.FromArgb(29,31,34);
    public static readonly Color Raised=Color.FromArgb(39,41,44);
    public static readonly Color Copper=Color.FromArgb(223,167,128);
    public static readonly Color Text=Color.FromArgb(245,240,235);
    public static readonly Color Muted=Color.FromArgb(184,181,178);
    public static readonly Color Border=Color.FromArgb(73,71,69);
    public static void Button(Button b,bool primary=false)
    {
        b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=primary?0:1;b.FlatAppearance.BorderColor=Border;
        b.BackColor=primary?Copper:Raised;b.ForeColor=primary?Color.FromArgb(25,23,21):Text;
        b.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(240,188,148):Color.FromArgb(56,51,47);
        b.FlatAppearance.MouseDownBackColor=Color.FromArgb(169,119,85);
        b.Font=new Font("Segoe UI",10,FontStyle.Bold);b.Cursor=Cursors.Hand;
        b.Padding=new Padding(12,4,12,4);b.MinimumSize=new Size(0,42);
        b.AccessibleName=b.Text;
    }
    public static void Apply(Control root)
    {
        foreach(Control c in root.Controls)
        {
            if(c is Button button)Button(button,button.DialogResult==DialogResult.OK||button.Text.StartsWith("Emitir")||button.Text.StartsWith("+ Reservar"));
            else if(c is TextBox box){box.BackColor=Background;box.ForeColor=Text;box.BorderStyle=BorderStyle.FixedSingle;}
            else if(c is ComboBox combo){combo.BackColor=Background;combo.ForeColor=Text;combo.FlatStyle=FlatStyle.Flat;}
            else if(c is GroupBox group){group.BackColor=Surface;group.ForeColor=Copper;group.Font=new Font("Segoe UI",10,FontStyle.Bold);}
            Apply(c);
        }
    }
    public static void StyleNavigation(TabControl tabs)
    {
        tabs.Alignment=TabAlignment.Left;tabs.Multiline=true;tabs.SizeMode=TabSizeMode.Fixed;
        tabs.ItemSize=new Size(46,185);tabs.DrawMode=TabDrawMode.OwnerDrawFixed;
        tabs.Padding=new Point(16,8);
        tabs.DrawItem+=(_,e)=>
        {
            var selected=e.Index==tabs.SelectedIndex;var rect=e.Bounds;
            using var bg=new SolidBrush(selected?Color.FromArgb(62,48,39):Background);e.Graphics.FillRectangle(bg,rect);
            if(selected){using var accent=new SolidBrush(Copper);e.Graphics.FillRectangle(accent,rect.Left,rect.Top+6,4,rect.Height-12);}
            TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,new Font("Segoe UI",10,selected?FontStyle.Bold:FontStyle.Regular),new Rectangle(rect.Left+18,rect.Top,rect.Width-22,rect.Height),selected?Copper:Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        };
    }
    public static void AddSidebar(Form form,TabControl tabs)
    {
        tabs.Alignment=TabAlignment.Top;tabs.Appearance=TabAppearance.FlatButtons;tabs.ItemSize=new Size(1,1);tabs.SizeMode=TabSizeMode.Fixed;tabs.Multiline=false;
        var side=new Panel{Dock=DockStyle.Left,Width=198,BackColor=Background,Padding=new Padding(12)};
        var brand=new Label{Text="BARBA\nDEFUMADOS",Dock=DockStyle.Top,Height=82,ForeColor=Copper,Font=new Font("Georgia",15,FontStyle.Bold),Padding=new Padding(8,14,0,0)};
        var menu=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(0,4,0,0)};
        side.Controls.Add(menu);side.Controls.Add(brand);var buttons=new List<Button>();
        foreach(TabPage page in tabs.TabPages)
        {
            var b=new Button{Text=page.Text,Width=168,Height=40,TextAlign=ContentAlignment.MiddleLeft,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,0,4),Padding=new Padding(10,0,0,0),Cursor=Cursors.Hand,AccessibleName=page.Text};
            b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>tabs.SelectedTab=page;buttons.Add(b);menu.Controls.Add(b);
        }
        void Refresh(){for(var i=0;i<buttons.Count;i++){var active=i==tabs.SelectedIndex;buttons[i].BackColor=active?Color.FromArgb(63,47,37):Background;buttons[i].ForeColor=active?Copper:Muted;buttons[i].Font=new Font("Segoe UI",10,active?FontStyle.Bold:FontStyle.Regular);}}
        tabs.SelectedIndexChanged+=(_,_)=>Refresh();Refresh();form.Controls.Add(side);side.SendToBack();form.PerformLayout();
    }
}

public sealed class StockListBox : ListBox
{
    public StockListBox()
    {
        DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=66;BorderStyle=BorderStyle.None;HorizontalScrollbar=false;
        BackColor=DesktopTheme.Background;ForeColor=DesktopTheme.Text;Font=new Font("Segoe UI",10);
        IntegralHeight=false;
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if(e.Index<0)return;
        var selected=(e.State&DrawItemState.Selected)!=0;
        var box=new Rectangle(e.Bounds.Left+2,e.Bounds.Top+3,e.Bounds.Width-6,e.Bounds.Height-6);
        using var bg=new SolidBrush(selected?Color.FromArgb(69,51,39):e.Index%2==0?DesktopTheme.Surface:DesktopTheme.Raised);
        e.Graphics.FillRectangle(bg,box);
        if(selected){using var p=new Pen(DesktopTheme.Copper,1);e.Graphics.DrawRectangle(p,box);}
        TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,new Rectangle(box.Left+16,box.Top+10,box.Width-30,box.Height-14),DesktopTheme.Text,TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if((e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(e.Graphics,box,DesktopTheme.Copper,DesktopTheme.Surface);
    }
}
