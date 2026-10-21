using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

class VpnSession {
 public string State="Disconnected";
 public Action<string> Changed;
 public Func<string,bool,string[]> Credentials;
 public Profile Profile;
 Process daemon;
 TcpClient management;
 StreamWriter writer;
 string runtime;
 bool stopping; bool touched;
 public string TunnelName;
 public string SophosName;
 public static string Quote(string s) { if(s.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new Exception("Invalid line break in argument.");return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"")+"\""; }
 // Windows command-line quoting follows CommandLineToArgvW rules, independently of management quoting.
 public static string Arg(string s) {
  if(s.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new Exception("Invalid argument.");
  var b=new StringBuilder("\"");int slashes=0;
  foreach(char c in s){if(c=='\\'){slashes++;continue;}if(c=='\"'){b.Append('\\',slashes*2+1);b.Append(c);}else{b.Append('\\',slashes);b.Append(c);}slashes=0;}
  b.Append('\\',slashes*2);b.Append('"');return b.ToString();
 }
#if TEST_ENGINE
 public static string TestEngine;
#endif
 public static string Find(string kind) {
#if TEST_ENGINE
  if(TestEngine!=null)return TestEngine;
#endif
  var pf=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);var x86=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
  string[] paths=kind=="WireGuard"?new[]{Path.Combine(pf,"WireGuard","wireguard.exe")}:kind=="Sophos Connect"?new[]{Path.Combine(x86,"Sophos","Connect","sccli.exe"),Path.Combine(pf,"Sophos","Connect","sccli.exe")}:new[]{Path.Combine(pf,"OpenVPN","bin","openvpn.exe"),Path.Combine(x86,"OpenVPN","bin","openvpn.exe"),Path.Combine(x86,"Sophos","Connect","openvpn.exe")};
  return paths.FirstOrDefault(File.Exists);
 }
 public static string Run(string exe,string args,string input=null) {
  var info=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true,WorkingDirectory=Path.GetDirectoryName(exe)};
  using(var p=Process.Start(info)) {var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();if(input!=null)p.StandardInput.Write(input);p.StandardInput.Close();if(!p.WaitForExit(20000)){p.Kill();throw new Exception("The VPN engine did not respond within 20 seconds.");}Task.WaitAll(stdout,stderr);return stdout.Result+stderr.Result;}
 }
 void Set(string value){State=value;if(Changed!=null)Changed(value);}
 public static string Id(Profile profile){using(var hash=SHA256.Create())return "MVC"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes((String.IsNullOrEmpty(profile.EngineName)?Path.GetFullPath(profile.Path):profile.EngineName).ToLowerInvariant()))).Replace("-","").Substring(0,12);}
 public async Task Connect(Profile profile,string home) {
  Profile=profile;TunnelName=Id(profile);stopping=false;Set("Connecting");
  string exe=Find(profile.Kind);if(exe==null)throw new Exception("The "+profile.Kind+" engine is missing. Install its official Windows package first; connections will then be controlled here.");
  if(String.IsNullOrEmpty(profile.EngineName)&&!File.Exists(profile.Path))throw new Exception("Configuration file is missing. Import it again.");
  if(profile.Kind=="WireGuard") {
   string folder=Path.Combine(home,"tunnels");SecureFolder(folder);string config=Path.Combine(folder,TunnelName+".conf");File.Copy(profile.Path,config,true);
   touched=true;await Task.Run(()=>Run(exe,"/installtunnelservice "+Arg(config)));
   string result=await Status();if(result=="Disconnected")throw new Exception("WireGuard could not start its tunnel service. Check the configuration and administrator permissions.");Set(result);
  } else if(profile.Kind=="Sophos Connect") {
   SophosName=String.IsNullOrEmpty(profile.EngineName)?TunnelName:profile.EngineName;touched=true;if(await Status()=="Connected"){Set("Connected");return;}
   if(String.IsNullOrEmpty(profile.EngineName)) {string result=await Task.Run(()=>Run(exe,"add -f "+Arg(profile.Path)+" -n "+Arg(SophosName)));if(!await ExistsSophos(exe))throw new Exception("Sophos could not import the profile. "+result.Trim());}
   var credentials=Credentials("Sophos Connect login (append your OTP to the password if required by your VPN)",true);if(credentials==null)throw new OperationCanceledException("Login cancelled.");
   string login=credentials[0],pass=credentials[1];credentials[0]=credentials[1]=null;
   // Password goes to redirected input, never the process command line or disk.
   await Task.Run(()=>Run(exe,"enable -n "+Arg(SophosName)+" -u "+Arg(login),pass+Environment.NewLine));pass=null;
   Set(await Status());
  } else await StartOpenVpn(exe,home);
 }
 async Task<bool> ExistsSophos(string exe){string text=await Task.Run(()=>Run(exe,"list"));return text.Split('\n').Any(l=>l.Trim()==SophosName);}
 public static void SecureFolder(string path) {Directory.CreateDirectory(path);var acl=new DirectorySecurity();acl.SetAccessRuleProtection(true,false);foreach(var sid in new[]{WindowsIdentity.GetCurrent().User,new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null),new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid,null)})acl.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));Directory.SetAccessControl(path,acl);}
 async Task StartOpenVpn(string exe,string home) {
  runtime=Path.Combine(home,"sessions",Guid.NewGuid().ToString("N"));SecureFolder(runtime);
  string password;using(var rng=RandomNumberGenerator.Create()){var bytes=new byte[32];rng.GetBytes(bytes);password=Convert.ToBase64String(bytes);}string pwfile=Path.Combine(runtime,"management.key");File.WriteAllText(pwfile,password+"\n");
  int port;var listener=new TcpListener(IPAddress.Loopback,0);listener.Server.ExclusiveAddressUse=true;listener.Start();port=((IPEndPoint)listener.LocalEndpoint).Port;
  daemon=new Process{StartInfo=new ProcessStartInfo(exe,"--config "+Arg(Profile.Path)+" --management 127.0.0.1 "+port+" "+Arg(pwfile)+" --management-hold --management-query-passwords --management-client --auth-nocache --verb 3"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(Profile.Path)}};
  try {
   daemon.OutputDataReceived+=EngineOutput;daemon.ErrorDataReceived+=EngineOutput;daemon.Start();touched=true;daemon.BeginOutputReadLine();daemon.BeginErrorReadLine();
   var accept=listener.AcceptTcpClientAsync();
   while(!accept.IsCompleted){if(daemon.HasExited)throw new Exception("OpenVPN exited before starting. Check the connection log, profile certificates, and VPN adapter installation.");if(await Task.WhenAny(accept,Task.Delay(10000))!=accept)throw new Exception("The OpenVPN engine did not respond.");}
   management=await accept;
  } finally {listener.Stop();}  writer=new StreamWriter(management.GetStream(),new UTF8Encoding(false)){AutoFlush=true,NewLine="\n"};writer.WriteLine(password);
  var reader=new StreamReader(management.GetStream());bool authenticated=false;
  for(int i=0;i<10;i++){var pending=reader.ReadLineAsync();if(await Task.WhenAny(pending,Task.Delay(10000))!=pending)throw new Exception("OpenVPN management authentication timed out.");string response=await pending;if(response==null)break;if(response.Contains("SUCCESS: password is correct")){authenticated=true;break;}if(response.Contains("ERROR:"))break;}
  if(!authenticated)throw new Exception("OpenVPN management authentication failed.");
  foreach(string command in new[]{"bytecount 2","hold off","state on all"}){
   writer.WriteLine(command);bool acknowledged=false;
   for(int i=0;i<100;i++){var pending=reader.ReadLineAsync();if(await Task.WhenAny(pending,Task.Delay(10000))!=pending)throw new Exception("VPN engine command timed out.");string response=await pending;if(response==null||response.StartsWith("ERROR:"))throw new Exception("VPN engine rejected initialization.");if(response.StartsWith("SUCCESS:")){acknowledged=true;break;}}
   if(!acknowledged)throw new Exception("VPN engine did not acknowledge initialization.");
  }
  writer.WriteLine("hold release");
  Task ignored=ReadManagement(reader);
 }
 void EngineOutput(object sender,DataReceivedEventArgs e){string line=e.Data;if(line==null)return;if((line.Contains("Options error:")||line.Contains("ERROR:")||line.Contains("Exiting due to fatal"))&&!line.ToLowerInvariant().Contains("password")){if(Changed!=null)Changed(line);}}
 async Task ReadManagement(StreamReader reader){try{using(reader){string line;while((line=await reader.ReadLineAsync())!=null){if(stopping)break;

if(line.StartsWith(">STATE:")||Regex.IsMatch(line,@"^\d+,[A-Z_]+,")){var parts=(line.StartsWith(">STATE:")?line.Substring(7):line).Split(',');if(parts.Length>1)Set(parts[1]=="CONNECTED"?"Connected":parts[1]=="EXITING"?"Disconnected":parts[1]);}else if(line.StartsWith(">BYTECOUNT:")){if(Changed!=null)Changed("Traffic: "+line.Substring(11)+" bytes received/sent");}else if(line.StartsWith(">PASSWORD:Need ")){var match=Regex.Match(line,"'([^']+)'");if(!match.Success)continue;string kind=match.Groups[1].Value;bool user=line.Contains("username/password");var values=Credentials("VPN authentication: "+kind,user);if(values==null){writer.WriteLine("signal SIGTERM");Set("Authentication cancelled");continue;}if(user&&line.Contains(" SC:")){var challenge=Credentials("One-time code required by your VPN",false);if(challenge==null){writer.WriteLine("signal SIGTERM");Set("Authentication cancelled");continue;}values[1]="SCRV1:"+Convert.ToBase64String(Encoding.UTF8.GetBytes(values[1]))+":"+Convert.ToBase64String(Encoding.UTF8.GetBytes(challenge[1]));challenge[1]=null;}if(user)writer.WriteLine("username "+Quote(kind)+" "+Quote(values[0]));writer.WriteLine("password "+Quote(kind)+" "+Quote(values[1]));values[0]=values[1]=null;}else if(line.StartsWith(">PASSWORD:Verification Failed"))Set("Authentication failed");else if(line.StartsWith(">FATAL:"))Set("VPN engine reported a fatal error");}}}catch(Exception){if(!stopping)Set("Connection to VPN engine lost");}}
 public static string SophosState(string text,string name){bool block=false;foreach(string raw in text.Split('\n')){string line=raw.TrimEnd('\r');if(line.StartsWith("  ")&&!line.StartsWith("    "))block=line.Trim()==name;if(block&&line.TrimStart().StartsWith("VPN state:",StringComparison.OrdinalIgnoreCase)){string state=line.Substring(line.IndexOf(':')+1).Trim();return state.Equals("connected",StringComparison.OrdinalIgnoreCase)?"Connected":state.Equals("disconnected",StringComparison.OrdinalIgnoreCase)?"Disconnected":state;}}return "Status unavailable";}
 public async Task<string> Status(){if(Profile.Kind=="OpenVPN")return daemon==null||daemon.HasExited?"Disconnected":State;
  if(Profile.Kind=="Sophos Connect")return SophosState(await Task.Run(()=>Run(Find("Sophos Connect"),"list -d")),SophosName);
  string wg=Path.Combine(Path.GetDirectoryName(Find("WireGuard")),"wg.exe");string output=await Task.Run(()=>Run(wg,"show "+TunnelName+" latest-handshakes"));long stamp=0;foreach(var line in output.Split('\n')){var parts=line.Trim().Split('\t');long value;if(parts.Length==2&&long.TryParse(parts[1],out value))stamp=Math.Max(stamp,value);}if(stamp>0){double age=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds-stamp;return age<180?"Connected":"Tunnel active — handshake is stale";}
  using(var svc=new System.ServiceProcess.ServiceController("WireGuardTunnel$"+TunnelName)){try{return svc.Status==System.ServiceProcess.ServiceControllerStatus.Running?"Tunnel active — waiting for handshake":"Disconnected";}catch(InvalidOperationException){return "Disconnected";}}
 }
 public async Task Disconnect(){stopping=true;if(Profile==null)return;if(!touched){if(runtime!=null&&Directory.Exists(runtime))Directory.Delete(runtime,true);Set("Disconnected");return;}if(Find(Profile.Kind)==null){Set("Disconnected");return;}Set("Disconnecting");if(Profile.Kind=="WireGuard"){await Task.Run(()=>Run(Find("WireGuard"),"/uninstalltunnelservice "+Arg(TunnelName)));if(await Status()!="Disconnected")throw new Exception("WireGuard tunnel is still running.");}else if(Profile.Kind=="Sophos Connect"){await Task.Run(()=>Run(Find("Sophos Connect"),"disable -n "+Arg(SophosName)));if(await Status()!="Disconnected")throw new Exception("Sophos has not confirmed disconnection. Try Disconnect again.");}else {if(writer!=null)try{writer.WriteLine("signal SIGTERM");}catch(IOException){}catch(ObjectDisposedException){}if(daemon!=null&&!daemon.HasExited&&writer==null)daemon.Kill();if(daemon!=null&&!daemon.HasExited){bool exited=await Task.Run(()=>daemon.WaitForExit(10000));if(!exited)throw new Exception("OpenVPN is still shutting down. Try Disconnect again.");}if(management!=null)management.Close();if(runtime!=null&&Directory.Exists(runtime))Directory.Delete(runtime,true);}Set("Disconnected");}
}










