namespace BarbaCupom;

public sealed class LoginForm : Form
{
    readonly AuthStore auth;
    readonly TextBox username=new();
    readonly TextBox password=new();
    readonly TextBox confirm=new();
    readonly Label message=new();
    int failures;
    DateTime blockedUntil;
    public LoginForm(AuthStore auth)
    {
        this.auth=auth;
        var first=!auth.IsConfigured;
        Text=first?"Criar acesso — Barba Defumados":"Entrar — Barba Defumados";
        Width=440;Height=first?370:310;MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(29,26,24);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        Controls.Add(new Label{Text=first?"PRIMEIRO ACESSO":"ACESSO AO SISTEMA",Left=28,Top=22,Width=360,Height=35,ForeColor=Color.FromArgb(215,158,123),Font=new Font("Segoe UI",17,FontStyle.Bold)});
        AddField("Usuário",username,74);AddField("Senha",password,135);password.UseSystemPasswordChar=true;
        if(first){AddField("Confirme a senha",confirm,196);confirm.UseSystemPasswordChar=true;}
        var submit=new Button{Text=first?"Criar acesso":"Entrar",Left=28,Top=first?264:205,Width=372,Height=43,BackColor=Color.FromArgb(215,158,123),ForeColor=Color.Black,FlatStyle=FlatStyle.Flat};
        submit.Click+=(_,_)=>Submit(first);Controls.Add(submit);AcceptButton=submit;
        message.SetBounds(28,first?314:254,372,32);message.ForeColor=Color.FromArgb(255,179,165);Controls.Add(message);
        BackColor=DesktopTheme.Background;DesktopTheme.Apply(this);
    }
    void AddField(string label,TextBox box,int top)
    { Controls.Add(new Label{Text=label,Left=28,Top=top,Width=350,Height=23});box.SetBounds(28,top+23,372,30);box.BackColor=Color.FromArgb(22,20,19);box.ForeColor=Color.White;Controls.Add(box); }
    void Submit(bool first)
    {
        if(DateTime.UtcNow<blockedUntil)return;
        try
        {
            if(first)
            { if(password.Text!=confirm.Text)throw new ArgumentException("As senhas não conferem.");auth.Create(username.Text,password.Text); }
            else if(!auth.Verify(username.Text,password.Text))throw new UnauthorizedAccessException("Usuário ou senha incorretos.");
            DialogResult=DialogResult.OK;Close();
        }
        catch(Exception ex) when(ex is ArgumentException or UnauthorizedAccessException or InvalidOperationException)
        { failures++;message.Text=ex.Message;if(failures>=5){blockedUntil=DateTime.UtcNow.AddSeconds(30);AcceptButton=null;var timer=new System.Windows.Forms.Timer{Interval=30_000};timer.Tick+=(_,_)=>{timer.Stop();timer.Dispose();failures=0;AcceptButton=Controls.OfType<Button>().Single();message.Text="Tente novamente.";};timer.Start();message.Text="Muitas tentativas. Aguarde 30 segundos.";} }
    }
}
