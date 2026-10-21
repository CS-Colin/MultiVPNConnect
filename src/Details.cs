using System;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

class ConnectionSnapshot {
 public TimeSpan Duration;
 public string Address;
 public long? Received,Sent; public double? DownloadSpeed,UploadSpeed;
}
class ConnectionMetrics {
 readonly object gate=new object(); readonly Stopwatch clock=new Stopwatch();
 string address;long? received,sent;double? download,upload;long sampleTick;
 public void ObserveState(string state){lock(gate){if(state=="Connected")clock.Start();else clock.Stop();if(state!="Connected"&&state!="ASSIGN_IP"&&state!="ADD_ROUTES")address=null;}}
 public void Addresses(string local){IPAddress parsed;lock(gate){address=IPAddress.TryParse(local,out parsed)&&!parsed.Equals(IPAddress.Any)&&!parsed.Equals(IPAddress.IPv6Any)?parsed.ToString():null;}}
  public void Traffic(long? down,long? up){TrafficAt(down,up,Stopwatch.GetTimestamp());}
 public void TrafficAt(long? down,long? up,long tick){lock(gate){double seconds=(tick-sampleTick)/(double)Stopwatch.Frequency;if(sampleTick>0&&seconds>0&&down.HasValue&&up.HasValue&&received.HasValue&&sent.HasValue&&down>=received&&up>=sent){download=(down.Value-received.Value)/seconds;upload=(up.Value-sent.Value)/seconds;}else{download=null;upload=null;}received=down;sent=up;sampleTick=tick;}}
 public static string Speed(double? bytes){return bytes.HasValue?Bytes((long)Math.Max(0,bytes.Value))+"/s":"Unavailable";}
 public ConnectionSnapshot Snapshot(){lock(gate)return new ConnectionSnapshot{Duration=clock.Elapsed,Address=address,Received=received,Sent=sent,DownloadSpeed=clock.IsRunning?download:null,UploadSpeed=clock.IsRunning?upload:null};}
 public static string Bytes(long? bytes){if(!bytes.HasValue)return "Unavailable";double value=bytes.Value;string[] units={"B","KiB","MiB","GiB","TiB"};int unit=0;while(value>=1024&&unit<units.Length-1){value/=1024;unit++;}return value.ToString(unit==0?"0":"0.0")+" "+units[unit];}
 public static string Elapsed(TimeSpan elapsed){return ((long)elapsed.TotalHours).ToString("00")+":"+elapsed.Minutes.ToString("00")+":"+elapsed.Seconds.ToString("00");}
}
partial class VpnSession {
 public readonly ConnectionMetrics Metrics=new ConnectionMetrics();
 public void ObserveState(string value){Set(value);}
 public void ReadState(string line){var parts=(line.StartsWith(">STATE:")?line.Substring(7):line).Split(',');if(parts.Length<2)return;string state=parts[1]=="CONNECTED"?"Connected":parts[1]=="EXITING"?"Disconnected":parts[1];Set(state);if(state=="Connected"&&parts.Length>4){IPAddress remote;if(IPAddress.TryParse(parts[4],out remote))remoteAddress=remote.ToString();}if((state=="Connected"||state=="ASSIGN_IP")&&parts.Length>3)Metrics.Addresses(parts[3]);}
 public void ReadTraffic(string text){var parts=text.Split(',');long down,up;if(parts.Length==2&&long.TryParse(parts[0],out down)&&long.TryParse(parts[1],out up)&&down>=0&&up>=0)Metrics.Traffic(down,up);}
 public static string Field(string text,string name){foreach(var raw in text.Split('\n')){string line=raw.Trim();int colon=line.IndexOf(':');if(colon>0&&line.Substring(0,colon).Equals(name,StringComparison.OrdinalIgnoreCase))return line.Substring(colon+1).Trim();}return null;}
 public void ReadSophosDetails(string text){long down,up;Metrics.Traffic(long.TryParse(Field(text,"Bytes Received"),out down)&&down>=0?(long?)down:null,long.TryParse(Field(text,"Bytes Sent"),out up)&&up>=0?(long?)up:null);
  // SCCLI's Local IP can be the outer transport address, not the assigned VPN address.
  // Display an address only when the engine explicitly labels it as a virtual/tunnel IP.
  Metrics.Addresses(Field(text,"Virtual IP")??Field(text,"Tunnel IP"));
 }
 public async Task RefreshDetails(){
  if(Profile==null)return;
  if(Profile.Kind=="Sophos Connect"){string output=await Task.Run(()=>Run(Find("Sophos Connect"),"get -n "+Arg(SophosName)+" -t network"));ReadSophosDetails(output);}
  if(Profile.Kind=="WireGuard"){
   string wg=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Find("WireGuard")),"wg.exe");string output=await Task.Run(()=>Run(wg,"show "+TunnelName+" transfer"));long received=0,sent=0;bool found=false;
   foreach(var row in output.Split('\n')){var parts=row.Trim().Split('\t');long rx,tx;if(parts.Length==3&&long.TryParse(parts[1],out rx)&&long.TryParse(parts[2],out tx)&&rx>=0&&tx>=0){found=true;received=rx>long.MaxValue-received?long.MaxValue:received+rx;sent=tx>long.MaxValue-sent?long.MaxValue:sent+tx;}}
   Metrics.Traffic(found?(long?)received:null,found?(long?)sent:null);
   string ip=null;foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces().Where(a=>a.Name.Equals(TunnelName,StringComparison.OrdinalIgnoreCase))){ip=adapter.GetIPProperties().UnicastAddresses.Select(a=>a.Address).Where(a=>a.AddressFamily==AddressFamily.InterNetwork||a.AddressFamily==AddressFamily.InterNetworkV6&&!a.IsIPv6LinkLocal).Select(a=>a.ToString()).FirstOrDefault();}
   Metrics.Addresses(ip);
  }
  await RefreshNetworkFacts();
 }
}

