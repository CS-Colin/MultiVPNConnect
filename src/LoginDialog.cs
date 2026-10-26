using System;using System.Drawing;using System.Windows.Forms;
class LoginDialog : Form {
 public readonly bool CanRemember;
 public TextBox UsernameBox,PasswordBox,OtpBox;
 public CheckBox Remember,ShowPassword;
 public LoginDialog(string title,bool username,bool allowRemember,SavedLogin saved,bool sophos){
  CanRemember=allowRemember;Text="VPN sign in";ClientSize=new Size(500,sophos?385:335);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Palette.Surface;ForeColor=Palette.Text;Font=new Font("Segoe UI",10);Icon=Branding.LoadIcon();
  Controls.Add(new Label{Text=title,Location=new Point(22,18),Size=new Size(456,48)});
  Controls.Add(new Label{Text="Username",Location=new Point(22,80),AutoSize=true});UsernameBox=new TextBox{Text=saved==null?"":saved.Username,Location=new Point(170,76),Width=300,Enabled=username};Controls.Add(UsernameBox);
  Controls.Add(new Label{Text=username?"Password":"Password / code",Location=new Point(22,126),AutoSize=true});PasswordBox=new TextBox{Text=saved==null?"":saved.Password,Location=new Point(170,122),Width=300,UseSystemPasswordChar=true};Controls.Add(PasswordBox);
  OtpBox=new TextBox{Location=new Point(170,168),Width=300,Visible=sophos,UseSystemPasswordChar=true};if(sophos){Controls.Add(new Label{Text="One-time code",Location=new Point(22,172),AutoSize=true});Controls.Add(OtpBox);}
  int row=sophos?213:167;ShowPassword=new CheckBox{Text="Show password",Location=new Point(170,row),AutoSize=true};ShowPassword.CheckedChanged+=(s,e)=>{PasswordBox.UseSystemPasswordChar=!ShowPassword.Checked;OtpBox.UseSystemPasswordChar=!ShowPassword.Checked;};Controls.Add(ShowPassword);
  Remember=new CheckBox{Text="Remember login on this Windows account",Location=new Point(22,row+32),Size=new Size(450,26),Visible=allowRemember,Checked=allowRemember&&saved!=null};Controls.Add(Remember);
  Controls.Add(new Label{Text=allowRemember?"Saved in Windows Credential Manager. Codes entered separately are not saved.":"This password or one-time code will not be saved.",Location=new Point(22,row+65),Size=new Size(456,36),ForeColor=Palette.Muted,Font=new Font("Segoe UI",9)});
  var ok=new VpnButton{Primary=true,Text="Continue",Location=new Point(245,row+116),Size=new Size(108,36)};ok.Click+=(s,e)=>{if(UsernameBox.Text.IndexOfAny(new[]{'\r','\n','\0'})>=0||PasswordBox.Text.IndexOfAny(new[]{'\r','\n','\0'})>=0||OtpBox.Text.IndexOfAny(new[]{'\r','\n','\0'})>=0){MessageBox.Show("Login fields cannot contain line breaks.");return;}DialogResult=DialogResult.OK;};
  var cancel=new VpnButton{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(363,row+116),Size=new Size(108,36)};Controls.Add(ok);Controls.Add(cancel);AcceptButton=ok;CancelButton=cancel;Shown+=(s,e)=>{if(saved!=null||!username)PasswordBox.Focus();else UsernameBox.Focus();};
 }
 public string[] Values(){return new[]{UsernameBox.Text,PasswordBox.Text+OtpBox.Text};}
 public void Persist(string key){if(!CanRemember)return;if(Remember.Checked)CredentialStore.Save(key,UsernameBox.Text,PasswordBox.Text);else CredentialStore.Forget(key);}
 protected override void Dispose(bool disposing){if(disposing){PasswordBox.Clear();OtpBox.Clear();}base.Dispose(disposing);}
}
partial class MainWindow {
 Button forgetLoginButton;
 void ForgetLogin(object sender,EventArgs e){if(Selected==null||session!=null)return;try{CredentialStore.Forget(CredentialStore.Key(Selected));MessageBox.Show("Saved username and password removed for this connection.","Forget saved login");}catch(Exception ex){MessageBox.Show(ex.Message,"Saved login could not be removed");}}
 string[] Ask(string title,bool username){if(InvokeRequired)return (string[])Invoke(new Func<string,bool,string[]>(Ask),title,username);var profile=session==null?Selected:session.Profile;bool allowRemember=profile!=null&&username&&(title=="VPN authentication: Auth"||title.StartsWith("Sophos Connect login",StringComparison.Ordinal));string key=allowRemember?CredentialStore.Key(profile):null;SavedLogin saved=null;
  if(allowRemember)try{saved=CredentialStore.Load(key);}catch(Exception ex){MessageBox.Show(ex.Message+" Enter your login manually.","Saved login unavailable");}
  if(loginReuse.CanUse(profile,allowRemember,saved!=null)){Log("Using the saved login for this profile.");return new[]{saved.Username,saved.Password};}
  loginReuse.Prompted();if(!Visible)RestoreWindow();
  using(var dialog=new LoginDialog(title,username,allowRemember,saved,title.StartsWith("Sophos Connect login",StringComparison.Ordinal))){if(saved!=null){saved.Password=null;saved=null;}if(dialog.ShowDialog(this)!=DialogResult.OK)return null;var values=dialog.Values();if(profile!=null&&dialog.OtpBox.Text.Length>0){profile.RequiresOtp=true;Save();}if(allowRemember)try{dialog.Persist(key);}catch(Exception ex){MessageBox.Show(ex.Message+" The connection will continue without changing the saved login.","Login was not saved");}return values;}
 }
}
