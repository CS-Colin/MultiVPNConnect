using System;using System.Net.NetworkInformation;using System.Threading.Tasks;using System.Windows.Forms;using Microsoft.Win32;
class LoginReusePolicy {
 public bool TriedSaved {get;private set;}bool forcePrompt;
 public bool CanUse(Profile profile,bool eligible,bool hasSaved){if(!eligible||!hasSaved||profile==null||!profile.OneClickLogin||profile.RequiresOtp||TriedSaved||forcePrompt)return false;TriedSaved=true;return true;}
 public void Reject(){forcePrompt=true;}
 public void Prompted(){forcePrompt=true;}
}
class RecoveryPolicy {
 bool cancelled;public bool Armed {get;private set;}public bool Blocked {get;private set;}public int Attempts {get;private set;}public DateTime? Due {get;private set;}
 public void Start(){cancelled=false;Armed=false;Blocked=false;Attempts=0;Due=null;}
 public void Cancel(){cancelled=true;Armed=false;Blocked=true;Due=null;}
 public void RejectAuthentication(){Blocked=true;Due=null;}
 public void Observe(string state){if(state=="Connected"&&!cancelled){Blocked=false;Armed=true;Attempts=0;Due=null;}if(state.IndexOf("Authentication",StringComparison.OrdinalIgnoreCase)>=0&&state!="AUTH")RejectAuthentication();}
 public bool Schedule(DateTime now){if(!Armed||Blocked||Attempts>=5)return false;if(!Due.HasValue)Due=now.AddSeconds(Math.Min(60,5*Math.Pow(2,Attempts)));return true;}
 public bool Ready(DateTime now,bool networkAvailable){return networkAvailable&&Due.HasValue&&now>=Due.Value&&Armed&&!Blocked&&Attempts<5;}
 public void BeginAttempt(){Attempts++;Due=null;}
}
partial class MainWindow {
 readonly RecoveryPolicy recovery=new RecoveryPolicy();LoginReusePolicy loginReuse=new LoginReusePolicy();bool recovering;DateTime? lostAt;
 void InitializeRecovery(){NetworkChange.NetworkAvailabilityChanged+=NetworkAvailabilityChanged;SystemEvents.PowerModeChanged+=PowerModeChanged;}
 void RemoveRecoveryEvents(){NetworkChange.NetworkAvailabilityChanged-=NetworkAvailabilityChanged;SystemEvents.PowerModeChanged-=PowerModeChanged;}
 void NetworkAvailabilityChanged(object sender,NetworkAvailabilityEventArgs args){if(!IsHandleCreated||IsDisposed)return;if(args.IsAvailable)UI(()=>{nextIdentityRefresh=DateTime.MinValue;Log("Network available. Checking the managed connection.");});}
 void PowerModeChanged(object sender,PowerModeChangedEventArgs args){if(!IsHandleCreated||IsDisposed)return;if(args.Mode==PowerModes.Resume)UI(()=>{nextIdentityRefresh=DateTime.MinValue;if(session!=null&&recovery.Armed)lostAt=DateTime.UtcNow;Log("Windows resumed. Checking the managed connection.");});}
 void SessionChanged(VpnSession source,string message){UI(()=>{if(source!=session)return;if(message=="Authentication failed"){loginReuse.Reject();recovery.RejectAuthentication();}recovery.Observe(source.State);status.Text=source.State;RefreshDesktop();Log(message);});}
 async Task StartSession(Profile profile,bool automatic){loginReuse=new LoginReusePolicy();session=new VpnSession();var current=session;current.Credentials=Ask;current.Changed=message=>SessionChanged(current,message);Exception failure=null;
  try{Log((automatic?"Reconnecting ":"Starting ")+profile.Kind+" connection.");await current.Connect(profile,home);
   if(profile.Kind=="Sophos Connect"&&current.State=="Disconnected"&&loginReuse.TriedSaved){loginReuse.Reject();recovery.RejectAuthentication();Log("Saved login did not establish the connection. Please sign in again.");await current.Connect(profile,home);}
  }catch(Exception ex){failure=ex;}
  if(failure!=null){Log(failure.Message);if(!automatic&&!(failure is OperationCanceledException))MessageBox.Show(failure.Message,"Connection could not start");if(failure is OperationCanceledException)recovery.Cancel();bool cleaned=false;try{await current.Disconnect();cleaned=true;}catch(Exception cleanup){Log("Cleanup requires attention: "+cleanup.Message);}if(cleaned&&current==session&&!automatic)session=null;status.Text=session==null?"Disconnected":"Disconnect required";}
 } async Task MaybeRecover(VpnSession current,string state){if(current!=session||current.Profile==null||!current.Profile.AutoReconnect||recovering||busy||!recovery.Armed||recovery.Blocked)return;
  bool lost=state=="Disconnected"||state=="Connection to VPN engine lost"||state.IndexOf("stale",StringComparison.OrdinalIgnoreCase)>=0;
  // OpenVPN performs its own recovery while RECONNECTING; allow it time before restarting.
  if(state=="RECONNECTING"){if(!lostAt.HasValue)lostAt=DateTime.UtcNow;lost=(DateTime.UtcNow-lostAt.Value).TotalSeconds>=60;}
  else if(!lost)lostAt=null;
  if(!lost)return;if(!recovery.Schedule(DateTime.UtcNow)){if(recovery.Attempts>=5)guidance.Text="Reconnect stopped after 5 attempts. Disconnect, then connect again.";return;}guidance.Text="Connection lost. Automatic reconnect scheduled (Disconnect cancels).";
  if(!recovery.Ready(DateTime.UtcNow,NetworkInterface.GetIsNetworkAvailable()))return;
  recovering=true;busy=true;Buttons();var profile=current.Profile;try{recovery.BeginAttempt();Log("Automatic reconnect attempt "+recovery.Attempts+" of 5.");await current.Disconnect();if(session!=current)return;session=null;lostAt=null;await StartSession(profile,true);if(session==null)recovery.Cancel();}catch(Exception ex){Log("Reconnect paused: "+ex.Message);recovery.RejectAuthentication();guidance.Text="Reconnect needs attention. Disconnect, then connect again.";}finally{busy=false;recovering=false;Buttons();}
 }
}
