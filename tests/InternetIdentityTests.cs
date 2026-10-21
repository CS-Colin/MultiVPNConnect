using System;using System.Threading;using System.Threading.Tasks;
static class InternetIdentityTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception(message);Console.WriteLine("PASS: "+message);}
 public static async Task Run(){
  using(var identity=new InternetIdentity(token=>Task.FromResult("203.0.113.7\n"))){await identity.Refresh();Check(identity.PublicAddress=="203.0.113.7"&&identity.CheckedUtc.HasValue&&!identity.Loading,"Public IP lookup validates its address and records freshness");Check(!String.IsNullOrEmpty(identity.Hostname),"Hostname is available locally");}
  using(var identity=new InternetIdentity(token=>Task.FromResult("<html>failed</html>"))){await identity.Refresh();Check(identity.PublicAddress==null&&!identity.Loading,"Invalid lookup responses are unavailable");}
  using(var identity=new InternetIdentity(token=>{throw new System.Net.Http.HttpRequestException("offline");})){await identity.Refresh();Check(identity.PublicAddress==null&&!identity.Loading,"Offline lookup completes without blocking the app");}
  var response=new TaskCompletionSource<string>();using(var identity=new InternetIdentity(token=>response.Task)){var pending=identity.Refresh();identity.Invalidate();response.SetResult("203.0.113.8");await pending;Check(identity.PublicAddress==null&&!identity.CheckedUtc.HasValue,"A route change discards the previous lookup result");}
  Check(InternetIdentity.ParseAddress("127.0.0.1")==null&&InternetIdentity.ParseAddress("0.0.0.0")==null,"Invalid egress-address placeholders are rejected");
#if TEST_ENGINE
  InternetIdentity.TestFetch=token=>Task.FromResult("203.0.113.7");
#endif
 }
}
