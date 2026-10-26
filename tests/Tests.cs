using System;using System.IO;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;
class Tests {
 static void Check(bool value,string text){if(!value)throw new Exception(text);Console.WriteLine("PASS: "+text);}
 [STAThread] static int Main(string[] args){try{Run(args).GetAwaiter().GetResult();return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 static async Task Run(string[] args){
  string fake=Path.GetFullPath(args[0]);string home=Path.Combine(Path.GetFullPath(args[1]),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(home);
  foreach(var text in new[]{"simple","C:\\folder with spaces\\","quote\"value","ending space ",""})Check(VpnSession.Run(fake,"--echo "+VpnSession.Arg(text)).TrimEnd('\r','\n')==text,"Windows argument round trip");
  bool rejected=false;try{VpnSession.Arg("bad\nargument");}catch{rejected=true;}Check(rejected,"Argument injection rejected");
  string status="Connections:\n  One\n    VPN state: disconnected\n  Two\n    VPN state: connected\n";
  Check(VpnSession.SophosState(status,"One")=="Disconnected","Disconnected is not confused with connected");Check(VpnSession.SophosState(status,"Two")=="Connected","Sophos state matched by connection name");Check(VpnSession.SophosState(status,"Missing")=="Status unavailable","Missing Sophos status is not reported connected");
  await InternetIdentityTests.Run();
  UsabilityTests.Policies();
  NetworkFactsTests.Run();
  DesktopTests.Metrics();
  VpnSession.TestEngine=fake;var connected=new TaskCompletionSource<bool>();var session=new VpnSession();session.Changed=s=>{if(s=="Connected")connected.TrySetResult(true);};session.Credentials=(title,user)=>new[]{"test user","dummy\\\"password"};string config=Path.Combine(home,"test.ovpn");File.WriteAllText(config,"client");await session.Connect(new Profile{Name="Test",Kind="OpenVPN",Path=config},home);Check(await Task.WhenAny(connected.Task,Task.Delay(5000))==connected.Task,"OpenVPN management authentication and connected state");Check(await session.Status()=="Connected","Status follows engine acknowledgement");await session.Disconnect();Check(await session.Status()=="Disconnected","Graceful OpenVPN disconnect confirmed by process exit");Check(Directory.GetDirectories(Path.Combine(home,"sessions")).Length==0,"Temporary management key removed");
  var cancelled=new VpnSession();cancelled.Credentials=(title,user)=>null;await cancelled.Connect(new Profile{Name="Cancel",Kind="OpenVPN",Path=config},home);await Task.Delay(500);await cancelled.Disconnect();Check(await cancelled.Status()=="Disconnected","Cancelled authentication cleans up engine");
  if(args.Length>2){
   VpnSession.TestEngine=Path.GetFullPath(args[2]);
   File.WriteAllText(config,"dev null\nproto udp\nremote 127.0.0.1 9\nifconfig-noexec\nroute-noexec\n");
   var real=new VpnSession();var started=new TaskCompletionSource<bool>();real.Changed=value=>{Console.WriteLine("Engine: "+value);if(value=="CONNECTING"||value=="ASSIGN_IP"||value=="Connected")started.TrySetResult(true);};
   Exception failed=null;try{await real.Connect(new Profile{Name="Loopback engine test",Kind="OpenVPN",Path=config},home);Check(await Task.WhenAny(started.Task,Task.Delay(5000))==started.Task,"Installed OpenVPN engine management authentication and state subscription (localhost only)");}catch(Exception ex){failed=ex;}await real.Disconnect();if(failed!=null)throw failed;
   Check(await real.Status()=="Disconnected","Installed OpenVPN engine graceful shutdown");
  }
  // Instantiate the actual UI on an STA thread without activating any tunnel.
  Exception uiFailure=null;var thread=new Thread(()=>{try{CredentialTests.Run();using(var form=new MainWindow()){form.Show();Application.DoEvents();Check(form.Controls.Count>0,"Desktop layout initializes");Check(form.Text=="MultiVPNConnect","Desktop window title");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(MainWindow).GetField("profiles",flags).SetValue(form,new System.Collections.Generic.List<Profile>{new Profile{Name="Office",Kind="OpenVPN",Path="C:\\preview.ovpn"},new Profile{Name="Home",Kind="WireGuard",Path="C:\\preview.conf"}});
typeof(MainWindow).GetMethod("RefreshList",flags).Invoke(form,null);
var search=(TextBox)typeof(MainWindow).GetField("search",flags).GetValue(form);
var list=(ListBox)typeof(MainWindow).GetField("list",flags).GetValue(form);
search.Text="wireguard";Application.DoEvents();Check(list.Items.Count==1&&((Profile)list.Items[0]).Name=="Home","Search filters by VPN type");
search.Text="no matching profile";Application.DoEvents();Check(list.Items.Count==0&&!((Button)typeof(MainWindow).GetField("connect",flags).GetValue(form)).Enabled,"Empty search disables Connect");
search.Text="";Application.DoEvents();Check(list.Items.Count==2,"Clearing search restores connections");
((Label)typeof(MainWindow).GetField("status",flags).GetValue(form)).Text="Connected";Application.DoEvents();Check(((Label)typeof(MainWindow).GetField("statusTitle",flags).GetValue(form)).Text=="Connected","Status card follows session state");
ReconnectIntegration.Run(form);UsabilityTests.Window(form);DesktopTests.Window(form);form.Close();}}catch(Exception ex){uiFailure=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(uiFailure!=null)throw uiFailure;
 }
}
