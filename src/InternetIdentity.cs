using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

sealed class InternetIdentity : IDisposable {
#if TEST_ENGINE
 public static Func<CancellationToken,Task<string>> TestFetch;
#endif
 readonly Func<CancellationToken,Task<string>> fetch;
 CancellationTokenSource pending;
 int generation;
 public string PublicAddress {get;private set;}
 public bool Loading {get;private set;}
 public DateTime? CheckedUtc {get;private set;}
 public string Hostname {get;private set;}
 public InternetIdentity(Func<CancellationToken,Task<string>> provider=null){
#if TEST_ENGINE
 fetch=provider??TestFetch??Fetch;
#else
 fetch=provider??Fetch;
#endif
try{Hostname=Dns.GetHostName();}catch{Hostname=Environment.MachineName;}}
 public static string ParseAddress(string text){IPAddress ip;if(text==null||!IPAddress.TryParse(text.Trim(),out ip)||IPAddress.IsLoopback(ip)||ip.Equals(IPAddress.Any)||ip.Equals(IPAddress.IPv6Any))return null;return ip.ToString();}
 static async Task<string> Fetch(CancellationToken token){
  ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
  using(var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false},true)){client.Timeout=TimeSpan.FromSeconds(8);client.MaxResponseContentBufferSize=128;client.DefaultRequestHeaders.UserAgent.ParseAdd("MultiVPNConnect/0.6.1");using(var response=await client.GetAsync("https://api64.ipify.org",token)){response.EnsureSuccessStatusCode();return await response.Content.ReadAsStringAsync();}}
 }
 public void Invalidate(){generation++;if(pending!=null){pending.Cancel();pending.Dispose();pending=null;}PublicAddress=null;CheckedUtc=null;Loading=false;}
 public async Task Refresh(){Invalidate();int request=generation;var cancellation=new CancellationTokenSource();pending=cancellation;Loading=true;
  string result=null;try{result=ParseAddress(await fetch(cancellation.Token));}catch(Exception){result=null;}
  if(request==generation){PublicAddress=result;CheckedUtc=result==null?(DateTime?)null:DateTime.UtcNow;Loading=false;pending=null;cancellation.Dispose();}
 }
 public void Dispose(){Invalidate();}
}
partial class MainWindow {
 InternetIdentity internet;

 string identityRoute;
 DateTime nextIdentityRefresh=DateTime.MinValue;
 System.Windows.Forms.Label publicValue,hostnameValue;
 System.Windows.Forms.LinkLabel refreshIdentity;
 async void RefreshInternetIdentity(){if(internet==null||internet.Loading)return;nextIdentityRefresh=DateTime.UtcNow.AddSeconds(60);publicValue.Text="Checking…";refreshIdentity.Enabled=false;
  try{await internet.Refresh();}catch(Exception){if(!IsDisposed)publicValue.Text="Unavailable";}
  if(IsDisposed||Disposing)return;publicValue.Text=internet.PublicAddress??"Unavailable";refreshIdentity.Enabled=true;detailTips.SetToolTip(publicValue,"Internet egress address reported by ipify over HTTPS. With split tunneling or a proxy it may differ from your VPN server's public address."+(internet.CheckedUtc.HasValue?" Checked "+internet.CheckedUtc.Value.ToLocalTime().ToString("HH:mm:ss")+".":" Lookup unavailable."));
 }
 void CheckInternetIdentity(){if(internet==null)return;string route=(session==null?"none":session.TunnelName??"session")+"|"+status.Text;
  if(route!=identityRoute){identityRoute=route;internet.Invalidate();publicValue.Text="Checking…";nextIdentityRefresh=DateTime.UtcNow.AddSeconds(2);}
  if(DateTime.UtcNow>=nextIdentityRefresh&&!internet.Loading)RefreshInternetIdentity();
 }
}
