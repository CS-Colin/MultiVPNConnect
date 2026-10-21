using System;using System.IO;using System.Linq;using System.Net;using System.Net.NetworkInformation;using System.Net.Sockets;using System.Runtime.InteropServices;using System.Threading.Tasks;using System.Diagnostics;
class NetworkFacts {
 public string ServerHost,ServerAddress,Dns,Routing,Latency;
 public DateTime CheckedUtc;
 public NetworkFacts Copy(){return (NetworkFacts)MemberwiseClone();}
}
class AdapterRoute {public uint Index;public string Prefix;public int Length;public int Family;}
static class RouteReader {
 [DllImport("iphlpapi.dll")]static extern uint GetIpForwardTable2(ushort family,out IntPtr table);
 [DllImport("iphlpapi.dll")]static extern void FreeMibTable(IntPtr table);
 public static AdapterRoute[] Read(){IntPtr table;if(GetIpForwardTable2(0,out table)!=0)return new AdapterRoute[0];try{int count=Marshal.ReadInt32(table);if(count<0||count>100000)throw new Exception("Invalid route table");var rows=new AdapterRoute[count];for(int i=0;i<count;i++){var row=IntPtr.Add(table,8+i*104);int family=Marshal.ReadInt16(row,12);var bytes=new byte[family==2?4:16];Marshal.Copy(IntPtr.Add(row,family==2?16:20),bytes,0,bytes.Length);rows[i]=new AdapterRoute{Index=(uint)Marshal.ReadInt32(row,8),Family=family,Prefix=new IPAddress(bytes).ToString(),Length=Marshal.ReadByte(row,40)};}return rows;}finally{FreeMibTable(table);}}
 public static string Mode(AdapterRoute[] routes,uint index,int family){var rows=routes.Where(r=>r.Index==index&&r.Family==family).ToArray();if(rows.Length==0)return "None";bool full=rows.Any(r=>r.Length==0)||rows.Any(r=>r.Length==1&&r.Prefix==(family==2?"0.0.0.0":"::"))&&rows.Any(r=>r.Length==1&&r.Prefix==(family==2?"128.0.0.0":"8000::"));return full?"Full":"Split";}
}
partial class VpnSession {
 readonly object factsGate=new object();NetworkFacts facts=new NetworkFacts();DateTime nextFacts=DateTime.MinValue;string remoteAddress;
 public NetworkFacts Facts(){lock(factsGate)return facts.Copy();}
 void ResetNetworkFacts(){lock(factsGate){facts=new NetworkFacts();nextFacts=DateTime.MinValue;remoteAddress=null;}}
 public static string ConfiguredServer(string path,string kind){if(!File.Exists(path))return null;foreach(string raw in File.ReadLines(path)){var line=raw.Trim();if(line.StartsWith("#")||line.StartsWith(";"))continue;if(kind=="OpenVPN"&&line.StartsWith("remote ",StringComparison.OrdinalIgnoreCase)){var match=System.Text.RegularExpressions.Regex.Match(line,@"^remote\s+(?:""([^""]+)""|(\S+))",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(match.Success)return match.Groups[1].Success?match.Groups[1].Value:match.Groups[2].Value;}if(kind=="WireGuard"&&line.StartsWith("Endpoint",StringComparison.OrdinalIgnoreCase)){int equal=line.IndexOf('=');if(equal>=0)return EndpointHost(line.Substring(equal+1).Trim());}}return null;}
 public static string EndpointHost(string value){if(String.IsNullOrWhiteSpace(value))return null;value=value.Trim();if(value.StartsWith("[")){int end=value.IndexOf(']');return end>0?value.Substring(1,end-1):null;}int last=value.LastIndexOf(':');return last>=0&&value.IndexOf(':')==last?value.Substring(0,last):value;}
 async Task RefreshNetworkFacts(){if(DateTime.UtcNow<nextFacts)return;nextFacts=DateTime.UtcNow.AddSeconds(10);var next=new NetworkFacts{CheckedUtc=DateTime.UtcNow};string assigned=Metrics.Snapshot().Address;
  next.ServerHost=String.IsNullOrEmpty(Profile.EngineName)?ConfiguredServer(Profile.Path,Profile.Kind):null;next.ServerAddress=remoteAddress;
  if(Profile.Kind=="Sophos Connect"){string list=await Task.Run(()=>Run(Find("Sophos Connect"),"list -d"));bool block=false;foreach(var raw in list.Split('\n')){if(raw.StartsWith("  ")&&!raw.StartsWith("    "))block=raw.Trim()==SophosName;if(block&&raw.TrimStart().StartsWith("Gateway:"))next.ServerHost=Field(raw,"Gateway");}string network=await Task.Run(()=>Run(Find("Sophos Connect"),"get -n "+Arg(SophosName)+" -t network"));next.ServerAddress=Field(network,"Remote IP");}
  if(Profile.Kind=="WireGuard"){string wg=Path.Combine(Path.GetDirectoryName(Find("WireGuard")),"wg.exe");string output=await Task.Run(()=>Run(wg,"show "+TunnelName+" endpoints"));next.ServerAddress=output.Split('\n').Select(l=>l.Trim().Split('\t')).Where(p=>p.Length==2&&p[1]!="(none)").Select(p=>EndpointHost(p[1])).FirstOrDefault();}
  IPAddress ip;if(next.ServerAddress==null&&IPAddress.TryParse(next.ServerHost,out ip))next.ServerAddress=ip.ToString();
  foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces()){
   var properties=adapter.GetIPProperties();bool match=Profile.Kind=="WireGuard"&&adapter.Name.Equals(TunnelName,StringComparison.OrdinalIgnoreCase)||assigned!=null&&properties.UnicastAddresses.Any(a=>a.Address.ToString()==assigned);if(!match)continue;
   next.Dns=String.Join(", ",properties.DnsAddresses.Select(a=>a.ToString()));var routes=RouteReader.Read();string v4="None",v6="None";try{v4=RouteReader.Mode(routes,(uint)properties.GetIPv4Properties().Index,2);}catch(NetworkInformationException){}try{v6=RouteReader.Mode(routes,(uint)properties.GetIPv6Properties().Index,23);}catch(NetworkInformationException){}next.Routing="IPv4: "+v4+" / IPv6: "+v6;break;
  }
#if !TEST_ENGINE
  if(IPAddress.TryParse(next.ServerAddress,out ip)){try{using(var ping=new Ping()){var reply=await ping.SendPingAsync(ip,1200);next.Latency=reply.Status==IPStatus.Success?reply.RoundtripTime+" ms":"No ICMP reply";}}catch{next.Latency="Unavailable";}}
#endif
  lock(factsGate)facts=next;
 }
}

