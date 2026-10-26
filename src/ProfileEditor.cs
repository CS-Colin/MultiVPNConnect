using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
partial class Profile {
 public string Group {get;set;} public bool Favourite {get;set;} public bool AutoReconnect {get;set;} public bool OneClickLogin {get;set;} public bool RequiresOtp {get;set;}
 public Profile(){AutoReconnect=true;OneClickLogin=true;}
}
class ProfileEditor : Form {
 public TextBox NameBox,GroupBox;public CheckBox FavouriteBox,ReconnectBox,OneClickBox,OtpBox;
 public ProfileEditor(Profile profile){Text="Edit VPN profile";ClientSize=new Size(510,390);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Palette.Surface;ForeColor=Palette.Text;Font=new Font("Segoe UI",10);Icon=Branding.LoadIcon();
  Controls.Add(new Label{Text="Connection name",Location=new Point(24,24),AutoSize=true});NameBox=new TextBox{Text=profile.Name,Location=new Point(180,20),Width=300,MaxLength=100};Controls.Add(NameBox);
  Controls.Add(new Label{Text="Group",Location=new Point(24,70),AutoSize=true});GroupBox=new TextBox{Text=profile.Group??"",Location=new Point(180,66),Width=300,MaxLength=60};Controls.Add(GroupBox);
  FavouriteBox=Option("Favourite — show at the top",profile.Favourite,116);ReconnectBox=Option("Auto-reconnect after a connection is lost",profile.AutoReconnect,158);OneClickBox=Option("Use saved login without asking again",profile.OneClickLogin,200);OtpBox=Option("Always ask for a one-time code / login",profile.RequiresOtp,242);
  Controls.Add(new Label{Text="The configuration file and saved-login identity stay the same.",Location=new Point(24,288),Size=new Size(460,36),ForeColor=Palette.Muted});
  var save=new VpnButton{Text="Save",Primary=true,Location=new Point(250,336),Size=new Size(110,36)};save.Click+=(s,e)=>{if(String.IsNullOrWhiteSpace(NameBox.Text)){MessageBox.Show("Enter a connection name.");return;}DialogResult=DialogResult.OK;};var cancel=new VpnButton{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(370,336),Size=new Size(110,36)};Controls.Add(save);Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;
 }
 CheckBox Option(string text,bool value,int y){var box=new CheckBox{Text=text,Checked=value,Location=new Point(24,y),Size=new Size(462,30)};Controls.Add(box);return box;}
 public void Apply(Profile profile){profile.Name=NameBox.Text.Trim();profile.Group=GroupBox.Text.Trim();profile.Favourite=FavouriteBox.Checked;profile.AutoReconnect=ReconnectBox.Checked;profile.OneClickLogin=OneClickBox.Checked;profile.RequiresOtp=OtpBox.Checked;}
}
partial class MainWindow {
 Button editProfileButton;
 void EditProfile(object sender,EventArgs e){if(Selected==null||session!=null||busy)return;using(var editor=new ProfileEditor(Selected)){if(editor.ShowDialog(this)==DialogResult.OK){editor.Apply(Selected);Save();}}}
 public static System.Collections.Generic.List<Profile> FilterProfiles(System.Collections.Generic.IEnumerable<Profile> source,string query){return source.Where(p=>query.Length==0||(p.Name+" "+p.Kind+" "+p.Group).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).OrderByDescending(p=>p.Favourite).ThenBy(p=>p.Group??"",StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).ToList();}
}
