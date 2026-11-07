using System.Globalization;

namespace BarbaCupom;

public sealed class StockPanel : Form
{
    readonly InventoryStore stock;
    readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    readonly Dictionary<TabPage,Action> refreshers=new();
    static readonly Color Copper=Color.FromArgb(215,158,123);
    static readonly CultureInfo Br=CultureInfo.GetCultureInfo("pt-BR");
    sealed record Choice<T>(T Value,string Label){public override string ToString()=>Label;}
    public StockPanel(InventoryStore stock)
    {
        this.stock=stock;Text="Estoque · Barba Defumados BBQ";Width=1280;Height=800;MinimumSize=new Size(1000,650);StartPosition=FormStartPosition.CenterParent;
        BackColor=Color.FromArgb(22,20,19);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        Controls.Add(tabs);BuildOverview();BuildSuppliers();BuildCategories();BuildSupplies();BuildRaw();BuildRecipes();BuildProduction();BuildReservations();BuildLosses();BuildReports();BuildForecast();BuildSettings();
        DesktopTheme.StyleNavigation(tabs);DesktopTheme.Apply(this);
        DesktopTheme.AddSidebar(this,tabs);
        tabs.SelectedIndexChanged+=(_,_)=>RefreshCurrent();RefreshCurrent();
        var timer=new System.Windows.Forms.Timer{Interval=60_000};timer.Tick+=(_,_)=>{stock.ReleaseExpired();RefreshCurrent();};timer.Start();FormClosed+=(_,_)=>timer.Dispose();
    }
    (TabPage Page,ListBox List,FlowLayoutPanel Actions) AddTab(string title)
    {
        var page=new TabPage(title){BackColor=DesktopTheme.Background,ForeColor=DesktopTheme.Text,Padding=new Padding(24,20,24,20)};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,108));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.Controls.Add(layout);
        layout.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,ForeColor=DesktopTheme.Text,Font=new Font("Segoe UI",24,FontStyle.Bold)},0,0);
        var hint=title switch{"Estoque pronto"=>"Pacotes disponíveis, custos e alertas de validade.","Produção"=>"Da matéria-prima ao produto pronto: acompanhe cada etapa.","Reservas"=>"Confirme o pagamento e a entrega ou gerencie o prazo da reserva.","Receitas"=>"Produtos, ingredientes e embalagens organizados por receita.","Insumos"=>"Cadastre os materiais e registre suas compras e versões de preço.","Perdas"=>"Registre perdas e confira a contagem física dos lotes.","Config. e backup"=>"Custos, prazos e cópias de segurança do seu negócio.",_=>"Barba Defumados BBQ  /  Gestão de estoque"};
        layout.Controls.Add(new Label{Text=hint,Dock=DockStyle.Fill,ForeColor=DesktopTheme.Muted,Font=new Font("Segoe UI",10)},0,1);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,Padding=new Padding(0,4,0,8)};layout.Controls.Add(actions,0,2);
        var list=new StockListBox{Dock=DockStyle.Fill};layout.Controls.Add(list,0,3);tabs.TabPages.Add(page);return(page,list,actions);
    }
    void Button(FlowLayoutPanel panel,string label,Action action)
    { var b=new Button{Text=label,AutoSize=true,Height=42,Margin=new Padding(0,2,8,0),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(65,48,40),ForeColor=Color.White};b.Click+=(_,_)=>Safe(action);panel.Controls.Add(b); }
    void Safe(Action action){try{action();RefreshCurrent();}catch(Exception e){MessageBox.Show(this,e.Message,"Estoque",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    void RefreshCurrent(){try{if(tabs.SelectedTab is not null&&refreshers.TryGetValue(tabs.SelectedTab,out var action))action();}catch(Exception e){MessageBox.Show(this,e.Message,"Estoque",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    static void Fill<T>(ListBox list,IEnumerable<T> items,Func<T,string> label)
    {object? selected=list.SelectedItem is Choice<T> current?current.Value:null;list.BeginUpdate();list.Items.Clear();foreach(var item in items)list.Items.Add(new Choice<T>(item,label(item)));list.EndUpdate();if(selected is not null)for(var i=0;i<list.Items.Count;i++)if(Equals(((Choice<T>)list.Items[i]!).Value,selected)){list.SelectedIndex=i;break;}}
    static T Selected<T>(ListBox list)=>list.SelectedItem is Choice<T> c?c.Value:throw new ArgumentException("Selecione um item da lista.");
    static decimal Number(string value)=>decimal.Parse(value.Replace('.',','),NumberStyles.Number,Br);
    static int Integer(string value)=>int.Parse(value,NumberStyles.Integer,Br);
    static DateTime DateInput(string value)=>DateTime.ParseExact(value.Trim(),"dd/MM/yyyy",Br);
    static string DateText(DateTime date)=>date.ToString("dd/MM/yyyy",Br);
    static Dictionary<string,string>? Ask(IWin32Window owner,string title,params (string Label,string Value)[] fields)
    {
        using var dialog=new Form{Text=title,Width=550,Height=Math.Min(720,150+fields.Length*58),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(29,26,24),ForeColor=Color.White,Font=new Font("Segoe UI",10),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=fields.Length+1,Padding=new Padding(18)};dialog.Controls.Add(table);
        var boxes=new Dictionary<string,TextBox>();
        for(var i=0;i<fields.Length;i++){table.RowStyles.Add(new RowStyle(SizeType.Absolute,58));var field=new Panel{Dock=DockStyle.Fill};field.Controls.Add(new Label{Text=fields[i].Label,Dock=DockStyle.Top,Height=23});var box=new TextBox{Text=fields[i].Value,Dock=DockStyle.Bottom,Height=28,BackColor=Color.FromArgb(22,20,19),ForeColor=Color.White};field.Controls.Add(box);boxes[fields[i].Label]=box;table.Controls.Add(field,0,i);}
        table.RowStyles.Add(new RowStyle(SizeType.Absolute,58));var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new Button{Text="Confirmar",DialogResult=DialogResult.OK,Width=130,Height=38,BackColor=Copper,ForeColor=Color.Black};var cancel=new Button{Text="Cancelar",DialogResult=DialogResult.Cancel,Width=110,Height=38};buttons.Controls.Add(ok);buttons.Controls.Add(cancel);table.Controls.Add(buttons,0,fields.Length);dialog.AcceptButton=ok;dialog.CancelButton=cancel;
        DesktopTheme.Apply(dialog);
        return dialog.ShowDialog(owner)==DialogResult.OK?boxes.ToDictionary(x=>x.Key,x=>x.Value.Text.Trim()):null;
    }
    T Pick<T>(string title,IEnumerable<T> values,Func<T,string> label)
    {
        var items=values.ToArray();if(items.Length==0)throw new ArgumentException("Não há itens para selecionar.");
        using var dialog=new Form{Text=title,Width=620,Height=460,StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(29,26,24),ForeColor=Color.White};
        var list=new ListBox{Dock=DockStyle.Fill,BackColor=Color.FromArgb(22,20,19),ForeColor=Color.White,Font=new Font("Segoe UI",11)};foreach(var value in items)list.Items.Add(new Choice<T>(value,label(value)));list.SelectedIndex=0;
        var ok=new Button{Text="Selecionar",DialogResult=DialogResult.OK,Dock=DockStyle.Bottom,Height=45,BackColor=Copper,ForeColor=Color.Black};list.DoubleClick+=(_,_)=>{dialog.DialogResult=DialogResult.OK;dialog.Close();};dialog.Controls.Add(list);dialog.Controls.Add(ok);
        DesktopTheme.Apply(dialog);
        if(dialog.ShowDialog(this)!=DialogResult.OK)throw new OperationCanceledException("Seleção cancelada.");return ((Choice<T>)list.SelectedItem!).Value;
    }
    long Supplier(bool quick=true)
    { var suppliers=stock.ListSuppliers();var choices=suppliers.Select(x=>(x.Id,x.Name,x.Phone)).ToList();if(quick)choices.Insert(0,(0,"+ Cadastrar fornecedor agora",""));var selected=Pick("Fornecedor",choices,x=>$"{x.Id} · {x.Name} · {x.Phone}");if(selected.Id!=0)return selected.Id;var input=Ask(this,"Cadastro rápido de fornecedor",("Nome",""),("Telefone",""));if(input is null)throw new OperationCanceledException();return stock.AddSupplier(input["Nome"],input["Telefone"]); }
    void BuildOverview()
    {
        var (page,list,buttons)=AddTab("Estoque pronto");
        Button(buttons,"Atualizar",()=>{});Button(buttons,"Registrar perda",()=>tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().First(x=>x.Text=="Perdas"));
        Button(buttons,"Rastrear lote",()=>{var lot=Pick("Lote pronto",stock.ListReadyLots(),x=>$"#{x.Id} · produto #{x.ProductId}");MessageBox.Show(this,string.Join(Environment.NewLine,stock.TraceReadyLot(lot.Id)),"Origem do lote");});
        refreshers[page]=()=>
        {
            stock.ReleaseExpired();var products=stock.ListStockProducts();var lots=stock.ListReadyLots();list.Items.Clear();
            foreach(var product in products)
            {
                list.Items.Add($"{product.Name.ToUpperInvariant()}  · margem definida {product.Margin:0.##}%");
                foreach(var group in lots.Where(x=>x.ProductId==product.Id).GroupBy(x=>x.PackageWeightGrams).OrderBy(x=>x.Key))
                    list.Items.Add($"    {group.Sum(x=>x.Available)} disp. + {group.Sum(x=>x.Reserved)} reserv. × {group.Key} g · peso total {((group.Sum(x=>x.Available)+group.Sum(x=>x.Reserved))*group.Key/1000m):0.###} kg");
                foreach(var lot in lots.Where(x=>x.ProductId==product.Id))
                {
                    var price=product.Unit==SaleUnit.Kg?(int)Math.Round((decimal)lot.UnitPriceCents*lot.PackageWeightGrams/1000,0,MidpointRounding.AwayFromZero):lot.UnitPriceCents;
                    var profit=price-lot.FrozenCostCents;
                    var margin=price==0?0:100m*profit/price;
                    var markup=lot.FrozenCostCents==0?0:100m*profit/lot.FrozenCostCents;
                    var profitKg=(int)Math.Round((decimal)profit*1000/lot.PackageWeightGrams,0,MidpointRounding.AwayFromZero);
                    list.Items.Add($"        Lote #{lot.Id} · {lot.Available} disp. · {lot.Reserved} reserv. · entrada {DateText(lot.CreatedAt)} · validade {DateText(lot.ExpiresAt)}");
                    list.Items.Add($"        custo {Money.Format(lot.FrozenCostCents)}/un · venda {Money.Format(price)}/un · lucro {Money.Format(profit)}/un ou {Money.Format(profitKg)}/kg · margem {margin:0.##}% · markup {markup:0.##}%");
                }
            }
            foreach(var alert in stock.ListAlerts())list.Items.Add($"⚠ {alert.Level}: {alert.Product}, lote #{alert.ReadyLotId}, vence {DateText(alert.ExpiresAt)}. Registre perda se necessário.");
        };
    }
    void BuildSuppliers()
    {
        var (page,list,buttons)=AddTab("Fornecedores");
        Button(buttons,"Novo fornecedor",()=>{var a=Ask(this,"Fornecedor",("Nome",""),("Telefone",""));if(a!=null)stock.AddSupplier(a["Nome"],a["Telefone"]);});
        Button(buttons,"Editar selecionado",()=>{var s=Selected<(long Id,string Name,string Phone)>(list);var a=Ask(this,"Editar fornecedor",("Nome",s.Name),("Telefone",s.Phone));if(a!=null)stock.UpdateSupplier(s.Id,a["Nome"],a["Telefone"]);});
        refreshers[page]=()=>Fill(list,stock.ListSuppliers(),x=>$"#{x.Id}  {x.Name}  ·  {x.Phone}");
    }
    void BuildCategories()
    {
        var (page,list,buttons)=AddTab("Categorias");
        Button(buttons,"Nova categoria",()=>{var a=Ask(this,"Categoria",("Nome",""));if(a!=null)stock.AddCategory(a["Nome"]);});
        Button(buttons,"Renomear",()=>{var c=Selected<SupplyCategory>(list);var a=Ask(this,"Renomear",("Nome",c.Name));if(a!=null)stock.RenameCategory(c.Id,a["Nome"]);});
        Button(buttons,"Excluir",()=>{var c=Selected<SupplyCategory>(list);if(MessageBox.Show(this,$"Excluir {c.Name}?","Confirmar",MessageBoxButtons.YesNo)==DialogResult.Yes)stock.DeleteCategory(c.Id);});
        refreshers[page]=()=>Fill(list,stock.ListCategories(),x=>$"#{x.Id}  {x.Name}");
    }
    void BuildSupplies()
    {
        var (page,list,buttons)=AddTab("Insumos");
        Button(buttons,"Novo insumo",()=>{var cat=Pick("Categoria",stock.ListCategories(),x=>x.Name);var a=Ask(this,"Aquisição de insumo",("Nome",""),("Forma: caixa / pacote / kg / litro / unidade","pacote"),("Conteúdo por compra","1"),("Unidade de uso: g / ml / un","un"),("Preço pago R$","0,00"),("Perda %","0"));if(a!=null)stock.AddSupply(a["Nome"],cat.Id,a["Forma: caixa / pacote / kg / litro / unidade"].ToLowerInvariant(),Number(a["Conteúdo por compra"]),a["Unidade de uso: g / ml / un"].ToLowerInvariant(),Money.Parse(a["Preço pago R$"]),Number(a["Perda %"]));});
        Button(buttons,"Novo preço",()=>{var s=Selected<(long Id,string Name,string Unit,int UnitCostCents)>(list);var a=Ask(this,$"Novo preço — {s.Name}",("Preço da compra R$","0,00"));if(a!=null)stock.UpdateSupplyPrice(s.Id,Money.Parse(a["Preço da compra R$"]));});
        Button(buttons,"Editar nome/perda",()=>{var s=Selected<(long Id,string Name,string Unit,int UnitCostCents)>(list);var a=Ask(this,"Editar insumo",("Nome",s.Name),("Perda %","0"));if(a!=null)stock.RenameSupply(s.Id,a["Nome"],Number(a["Perda %"]));});
        Button(buttons,"Entrada de insumo",()=>{var s=Selected<(long Id,string Name,string Unit,int UnitCostCents)>(list);var supplier=Supplier();var a=Ask(this,"Entrada — "+s.Name,("Número de compras/pacotes","1"),("Validade (dd/MM/aaaa)",DateText(DateTime.Today.AddMonths(6))));if(a!=null)stock.ReceiveSupply(s.Id,supplier,Integer(a["Número de compras/pacotes"]),DateInput(a["Validade (dd/MM/aaaa)"]));});
        Button(buttons,"Ver lotes",()=>{var text=string.Join(Environment.NewLine,stock.ListSupplyLots().Select(x=>$"#{x.Id} {x.Supply}: {x.Available:0.###} {x.Unit}, vence {DateText(x.ExpiresAt)}"));MessageBox.Show(this,text.Length==0?"Sem lotes disponíveis.":text,"Lotes de insumo");});
        Button(buttons,"Histórico de preços",()=>{var s=Selected<(long Id,string Name,string Unit,int UnitCostCents)>(list);MessageBox.Show(this,string.Join(Environment.NewLine,stock.ListSupplyPrices(s.Id).Select(x=>$"{x.EffectiveAt:dd/MM/yyyy HH:mm} · compra {Money.Format(x.PurchasePriceCents)} · uso {Money.Format(x.UnitCostCents)}/{s.Unit}")),"Versões de preço");});
        refreshers[page]=()=>Fill(list,stock.ListSupplies(),x=>$"#{x.Id}  {x.Name} · {Money.Format(x.UnitCostCents)}/{x.Unit}");
    }
    void BuildRaw()
    {
        var (page,list,buttons)=AddTab("Matéria-prima");
        Button(buttons,"Novo material",()=>{var a=Ask(this,"Matéria-prima",("Nome",""));if(a!=null)stock.AddRawMaterial(a["Nome"]);});
        Button(buttons,"Entrada por lote",()=>{var m=Pick("Matéria-prima",stock.ListRawMaterials(),x=>x.Name);var supplier=Supplier();var a=Ask(this,"Entrada a granel",("Peso cheio (kg)","1"),("Custo pago R$","0,00"),("Validade",DateText(DateTime.Today.AddMonths(1))));if(a!=null)stock.ReceiveRaw(m.Id,supplier,checked((int)(Number(a["Peso cheio (kg)"])*1000)),Money.Parse(a["Custo pago R$"]),DateInput(a["Validade"]));});
        refreshers[page]=()=>{list.Items.Clear();foreach(var m in stock.ListRawMaterials())list.Items.Add($"{m.Name} (#{m.Id})");foreach(var l in stock.ListRawLots())list.Items.Add($"    Lote #{l.Id} · {l.Material} · {l.AvailableGrams/1000m:0.###} kg · vence {DateText(l.Expiry)}");};
    }
    void BuildRecipes()
    {
        var (page,list,buttons)=AddTab("Receitas");
        Button(buttons,"Novo produto",()=>{var a=Ask(this,"Produto para comercialização",("Nome",""),("Margem %","50"),("Preço por: KG / UN","KG"));if(a!=null)stock.AddStockProduct(a["Nome"],Number(a["Margem %"]),a["Preço por: KG / UN"].Equals("UN",StringComparison.OrdinalIgnoreCase)?SaleUnit.Un:SaleUnit.Kg);});
        Button(buttons,"Editar margem",()=>{var p=Pick("Produto",stock.ListStockProducts(),x=>x.Name);var a=Ask(this,"Margem para lotes futuros",("Margem %",p.Margin.ToString(Br)));if(a!=null)stock.AddStockProduct(p.Name,Number(a["Margem %"]),p.Unit);});
        Button(buttons,"Criar receita",()=>{var p=Pick("Produto",stock.ListStockProducts(),x=>x.Name);var a=Ask(this,"Receita",("Perda esperada %","10"));if(a!=null)stock.AddRecipe(p.Id,Number(a["Perda esperada %"]));});
        Button(buttons,"Adicionar ingrediente",()=>{var r=Selected<RecipeView>(list);var kind=Pick("Tipo de item",new[]{"raw","supply"},x=>x=="raw"?"Matéria-prima":"Insumo");long id;string unit;if(kind=="raw"){var m=Pick("Matéria-prima",stock.ListRawMaterials(),x=>x.Name);id=m.Id;unit="g/kg";}else{var s=Pick("Insumo",stock.ListSupplies(),x=>x.Name);id=s.Id;unit=s.Unit+"/kg";}var a=Ask(this,"Quantidade da receita",($"Quantidade usada ({unit})","1"));if(a!=null)stock.AddRecipeItem(r.Id,kind,id,Number(a[$"Quantidade usada ({unit})"]));});
        Button(buttons,"Embalagem por tamanho",()=>{var r=Selected<RecipeView>(list);var s=Pick("Embalagem",stock.ListSupplies(),x=>x.Name);var a=Ask(this,"Embalagem",("Peso do pacote (g)","500"),("Embalagens por pacote","1"));if(a!=null)stock.SetRecipePackaging(r.Id,Integer(a["Peso do pacote (g)"]),s.Id,Integer(a["Embalagens por pacote"]));});
        Button(buttons,"Ver composição",()=>{var r=Selected<RecipeView>(list);var items=stock.ListRecipeItems(r.Id).Select(x=>$"{x.Name}: {x.QuantityPerKg:0.###}/kg");var packs=stock.ListRecipePackages(r.Id).Select(x=>$"Pacote {x.WeightGrams} g: {x.Quantity} × {x.Supply}");MessageBox.Show(this,string.Join(Environment.NewLine,items.Concat(packs)),r.Product);});
        refreshers[page]=()=>Fill(list,stock.ListRecipes(),x=>$"Receita #{x.Id} · {x.Product} · perda {x.LossPercent:0.##}% · {(x.Active?"ativa":"inativa")}");
    }
    void BuildProduction()
    {
        var (page,list,buttons)=AddTab("Produção");
        Button(buttons,"Iniciar manipulação",()=>
        {
            var recipe=Pick("Receita",stock.ListRecipes().Where(x=>x.Active),x=>$"#{x.Id} {x.Product}");var inputs=new List<(long,int)>();
            do
            {
                var lot=Pick("Lote de matéria-prima",stock.ListRawLots(),x=>$"#{x.Id} {x.Material} {x.AvailableGrams/1000m:0.###} kg vence {DateText(x.Expiry)}");
                var a=Ask(this,"Retirada para manipulação",("Peso retirado (kg)","1"));if(a is null)return;inputs.Add((lot.Id,checked((int)(Number(a["Peso retirado (kg)"])*1000))));
            }while(MessageBox.Show(this,"Adicionar outro lote de matéria-prima?","Produção",MessageBoxButtons.YesNo)==DialogResult.Yes);
            var received=Ask(this,"Recebimento na manipulação",("Peso recebido (kg)",(inputs.Sum(x=>x.Item2)/1000m).ToString(Br)));
            if(received!=null)stock.StartManipulation(recipe.Id,inputs,receivedGrams:checked((int)(Number(received["Peso recebido (kg)"])*1000)));
        });
        Button(buttons,"Finalizar e embalar",()=>{var manipulation=Selected<ManipulationView>(list);if(manipulation.Status!="open")throw new InvalidOperationException("Manipulação já concluída.");var a=Ask(this,"Saída para comercialização",("Peso final (kg)","1"),("Pacotes: peso g × quantidade; separe por ;","500x2"),("Validade",DateText(DateTime.Today.AddDays(14))));if(a is null)return;var packs=a["Pacotes: peso g × quantidade; separe por ;"].Split(';',StringSplitOptions.RemoveEmptyEntries).Select(part=>{var bits=part.Trim().ToLowerInvariant().Split('x');if(bits.Length!=2)throw new FormatException("Use 500x2;1000x1.");return(PackageWeightGrams:Integer(bits[0]),Quantity:Integer(bits[1]));}).ToArray();stock.FinishProductionAuto(manipulation.Id,checked((int)(Number(a["Peso final (kg)"])*1000)),packs,DateInput(a["Validade"]));});
        refreshers[page]=()=>Fill(list,stock.ListManipulations(),x=>$"#{x.Id} · {x.Product} · entrada {x.InputGrams/1000m:0.###} kg · saída {(x.OutputGrams.HasValue?x.OutputGrams.Value/1000m:0):0.###} kg · {x.Status} · {DateText(x.StartedAt)}");
    }
    void BuildReservations()
    {
        var (page,list,buttons)=AddTab("Reservas");
        Button(buttons,"Confirmar pagamento",()=>{var r=Selected<StockReservation>(list);var kind=Pick("Forma usada",new[]{PaymentKind.Pix,PaymentKind.Cash,PaymentKind.Card},x=>x switch{PaymentKind.Pix=>"Pix",PaymentKind.Cash=>"Dinheiro",_=>"Cartão"});stock.MarkPaid(r.Id,kind);});
        Button(buttons,"Confirmar entrega",()=>stock.Deliver(Selected<StockReservation>(list).Id));
        Button(buttons,"Estender",()=>{var r=Selected<StockReservation>(list);var a=Ask(this,"Estender reserva",("Horas a partir de agora","24"));if(a!=null)stock.ExtendReservation(r.Id,Integer(a["Horas a partir de agora"]));});
        Button(buttons,"Cancelar",()=>{var r=Selected<StockReservation>(list);if(MessageBox.Show(this,"Cancelar e devolver ao estoque?","Confirmar",MessageBoxButtons.YesNo)==DialogResult.Yes)stock.CancelReservation(r.Id);});
        refreshers[page]=()=>{stock.ReleaseExpired();Fill(list,stock.ListReservations(),x=>$"#{x.Id} · produto #{x.ProductId} · {x.Quantity} × {x.PackageWeightGrams} g · {x.Status} · {(x.Status=="active"?$"restam {Math.Max(0,(int)(x.ExpiresAt-DateTime.Now).TotalMinutes)} min":"prazo encerrado")} · {(x.Payment??"sem pagamento")}");};
    }
    void BuildLosses()
    {
        var (page,list,buttons)=AddTab("Perdas");
        Button(buttons,"Contar matéria-prima",()=>{var lot=Pick("Lote",stock.ListRawLots(),x=>$"#{x.Id} {x.Material}");var a=Ask(this,"Inventário",("Contagem em g",lot.AvailableGrams.ToString()),("Justificativa",""));if(a!=null)stock.AdjustMaterialInventory("materia_prima",lot.Id,Number(a["Contagem em g"]),a["Justificativa"]);});
        Button(buttons,"Contar insumo",()=>{var lot=Pick("Lote",stock.ListSupplyLots(),x=>$"#{x.Id} {x.Supply}");var a=Ask(this,"Inventário",("Contagem em "+lot.Unit,lot.Available.ToString(Br)),("Justificativa",""));if(a!=null)stock.AdjustMaterialInventory("insumo",lot.Id,Number(a["Contagem em "+lot.Unit]),a["Justificativa"]);});
        Button(buttons,"Perda de matéria-prima",()=>{var lot=Pick("Lote de matéria-prima",stock.ListRawLots(),x=>$"#{x.Id} {x.Material} · {x.AvailableGrams} g");var reason=Pick("Motivo",new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"},x=>x);var a=Ask(this,"Perda de matéria-prima",("Peso (g)","1"),("Descrição",""));if(a!=null)stock.RecordMaterialLoss("materia_prima",lot.Id,Number(a["Peso (g)"]),reason,a["Descrição"]);});
        Button(buttons,"Perda de insumo",()=>{var lot=Pick("Lote de insumo",stock.ListSupplyLots(),x=>$"#{x.Id} {x.Supply} · {x.Available:0.###} {x.Unit}");var reason=Pick("Motivo",new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"},x=>x);var a=Ask(this,"Perda de insumo",($"Quantidade ({lot.Unit})","1"),("Descrição",""));if(a!=null)stock.RecordMaterialLoss("insumo",lot.Id,Number(a[$"Quantidade ({lot.Unit})"]),reason,a["Descrição"]);});
        Button(buttons,"Registrar perda",()=>{var lot=Pick("Lote pronto",stock.ListReadyLots().Where(x=>x.Available>0),x=>$"#{x.Id} produto #{x.ProductId} · {x.Available} × {x.PackageWeightGrams} g");var reason=Pick("Motivo",new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"},x=>x);var a=Ask(this,"Perda",("Quantidade de pacotes","1"),("Descrição obrigatória",""));if(a!=null)stock.RecordLoss(lot.Id,Integer(a["Quantidade de pacotes"]),reason,a["Descrição obrigatória"]);});
        Button(buttons,"Ajustar contagem",()=>{var lot=Pick("Lote",stock.ListReadyLots(),x=>$"#{x.Id} · {x.Available} disponíveis");var a=Ask(this,"Ajuste de inventário",("Contagem física disponível",lot.Available.ToString()),("Justificativa",""));if(a!=null)stock.AdjustReadyInventory(lot.Id,Integer(a["Contagem física disponível"]),a["Justificativa"]);});
        refreshers[page]=()=>{list.Items.Clear();foreach(var l in stock.ListLosses())list.Items.Add($"#{l.Id} · {DateText(l.Date)} · {l.Reason} · {l.Quantity} pacotes · {l.WeightGrams/1000m:0.###} kg · custo {Money.Format(l.CostCents)} · {l.Description}");};
    }
    void BuildReports()
    {
        var (page,list,buttons)=AddTab("Relatórios");DateTime from=new(DateTime.Today.Year,1,1),to=DateTime.Today;long? product=null,supplier=null;string? payment=null,reason=null;
        Button(buttons,"Histórico de movimentos",()=>{using var view=new Form{Text="Entradas, saídas, perdas e ajustes",Width=1050,Height=650,StartPosition=FormStartPosition.CenterParent};var box=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=false,Text=string.Join(Environment.NewLine,stock.MovementHistory(from,to))};view.Controls.Add(box);view.ShowDialog(this);});
        Button(buttons,"Período",()=>{var a=Ask(this,"Período de até um ano civil",("De",DateText(from)),("Até",DateText(to)));if(a!=null){from=DateInput(a["De"]);to=DateInput(a["Até"]);}});
        Button(buttons,"Produto",()=>{product=Pick("Produto",stock.ListStockProducts(),x=>x.Name).Id;});
        Button(buttons,"Fornecedor",()=>{supplier=Supplier(false);});
        Button(buttons,"Pagamento",()=>{payment=Pick("Forma",new[]{"Pix","Cash","Card"},x=>x);});
        Button(buttons,"Motivo de perda",()=>{reason=Pick("Motivo",new[]{"Defeito","Vencimento","Extravio","Danificado","Erro de produção"},x=>x);});
        Button(buttons,"Limpar filtros",()=>{product=null;supplier=null;payment=null;reason=null;from=new(DateTime.Today.Year,1,1);to=DateTime.Today;});
        refreshers[page]=()=>{list.Items.Clear();list.Items.Add($"PERÍODO {DateText(from)} a {DateText(to)} · {(product.HasValue?"produto filtrado":"todos os produtos")}");foreach(var r in stock.Report(from,to,product,supplier,payment,reason))list.Items.Add($"{(r.Date.HasValue?DateText(r.Date.Value):"Ano anterior")} · {r.Product} · {r.Packages} un · {r.WeightGrams/1000m:0.###} kg · custo {Money.Format(r.CostCents)} · venda {Money.Format(r.RevenueCents)} · {r.Payment} {r.Reason} · {r.Supplier}");};
    }
    void BuildForecast()
    {
        var (page,list,buttons)=AddTab("Previsão");Button(buttons,"Recalcular",()=>{});
        refreshers[page]=()=>{list.Items.Clear();foreach(var s in stock.Forecast())list.Items.Add($"{s.Product} · média {s.DailyAverage:0.##}/dia · melhor dia {s.BusiestDay} · {s.Trend} · {s.AvailablePackages} disp. + {s.ReservedPackages} reserv. · sugerir {s.SuggestedPackages} pacotes · {s.Note}");};
    }
    void BuildSettings()
    {
        var (page,list,buttons)=AddTab("Config. e backup");
        Button(buttons,"Editar custos/prazos",()=>{var s=stock.GetSettings();var a=Ask(this,"Valores para lotes futuros",("Mão de obra R$/kg",Money.Format(s.Labor)),("Energia, água e gás R$/kg",Money.Format(s.Utilities)),("Entrega R$/kg",Money.Format(s.Delivery)),("Taxa cartão %",s.CardFee.ToString(Br)),("Reserva (horas)",s.ReservationHours.ToString()),("Alerta 1 (dias)",s.Alert1.ToString()),("Alerta 2 (dias)",s.Alert2.ToString()));if(a!=null)stock.UpdateSettings(Money.Parse(a["Mão de obra R$/kg"]),Money.Parse(a["Energia, água e gás R$/kg"]),Money.Parse(a["Entrega R$/kg"]),Number(a["Taxa cartão %"]),Integer(a["Reserva (horas)"]),Integer(a["Alerta 1 (dias)"]),Integer(a["Alerta 2 (dias)"]),s.BackupPath);});
        Button(buttons,"Pasta de backup",()=>{using var dialog=new FolderBrowserDialog{Description="Escolha a pasta de backup automático",SelectedPath=stock.GetSettings().BackupPath??BackupManager.DefaultFolder};if(dialog.ShowDialog(this)!=DialogResult.OK)return;var s=stock.GetSettings();stock.UpdateSettings(s.Labor,s.Utilities,s.Delivery,s.CardFee,s.ReservationHours,s.Alert1,s.Alert2,dialog.SelectedPath);});
        Button(buttons,"Backup agora",()=>{var path=BackupManager.Create(LocalStore.DefaultPath,stock.GetSettings().BackupPath??BackupManager.DefaultFolder);MessageBox.Show(this,$"Cópia criada em:\n{path}","Backup");});
        Button(buttons,"Restaurar backup",()=>{using var dialog=new OpenFileDialog{Title="Escolha uma cópia do banco",Filter="Banco SQLite (*.db)|*.db"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(MessageBox.Show(this,"A restauração substituirá os dados atuais após reiniciar. Uma cópia dos dados atuais será criada. Continuar?","Restaurar",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;BackupManager.ScheduleRestore(dialog.FileName,LocalStore.DefaultPath);MessageBox.Show(this,"Feche e abra o aplicativo para concluir a restauração.","Restauração agendada");});
        refreshers[page]=()=>{var s=stock.GetSettings();list.Items.Clear();list.Items.Add($"Mão de obra: {Money.Format(s.Labor)}/kg");list.Items.Add($"Energia, água e gás: {Money.Format(s.Utilities)}/kg");list.Items.Add($"Entrega: {Money.Format(s.Delivery)}/kg, mínimo 1 kg");list.Items.Add($"Cartão: {s.CardFee:0.##}%");list.Items.Add($"Reserva: {s.ReservationHours} h · alertas {s.Alert1}/{s.Alert2} dias");list.Items.Add($"Backup: {s.BackupPath??BackupManager.DefaultFolder} · últimos 30 dias");};
    }
}
