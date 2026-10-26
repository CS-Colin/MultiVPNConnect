using System;using System.Windows.Forms;
static class CredentialTests {
 static void Check(bool pass,string message){if(!pass)throw new Exception(message);Console.WriteLine("PASS: "+message);}
 public static void Run(){string key="MultiVPNConnect/Tests/"+Guid.NewGuid().ToString("N");string other=key+"-other";
  try{
   Check(CredentialStore.Load(key)==null,"Missing saved login returns no credentials");CredentialStore.Save(key,"test-user-ü","dummy-secret-λ");var saved=CredentialStore.Load(key);Check(saved.Username=="test-user-ü"&&saved.Password=="dummy-secret-λ","Windows Credential Manager stores and retrieves Unicode credentials");Check(CredentialStore.Load(other)==null,"Saved credentials are isolated by target");
   using(var dialog=new LoginDialog("Sophos Connect login",true,true,saved,true)){Check(dialog.UsernameBox.Text==saved.Username&&dialog.PasswordBox.Text==saved.Password&&dialog.Remember.Checked,"Saved username and password prefill the sign-in dialog");Check(dialog.PasswordBox.UseSystemPasswordChar,"Saved password is masked");dialog.OtpBox.Text="123456";dialog.PasswordBox.Text="new-base-password";Check(dialog.Values()[1]=="new-base-password123456","OTP is appended only to the submitted login");dialog.Persist(key);saved=CredentialStore.Load(key);Check(saved.Password=="new-base-password","Remember stores the base password without the OTP even after the dialog hides");dialog.Remember.Checked=false;dialog.Persist(key);Check(CredentialStore.Load(key)==null,"Unchecking Remember forgets an earlier saved login");}
   using(var dialog=new LoginDialog("One-time code",false,false,null,false)){dialog.PasswordBox.Text="987654";dialog.Remember.Checked=true;dialog.Persist(key);Check(CredentialStore.Load(key)==null,"One-time-code dialogs cannot persist credentials");}
   using(var dialog=new LoginDialog("VPN authentication: Auth",true,true,null,false)){dialog.UsernameBox.Text="cancelled";dialog.PasswordBox.Text="not-saved";dialog.Remember.Checked=true;}
   Check(CredentialStore.Load(key)==null,"Closing or cancelling a login dialog does not save credentials");
   CredentialStore.Save(key,"updated","replacement");CredentialStore.Forget(key);CredentialStore.Forget(key);Check(CredentialStore.Load(key)==null,"Forgetting a login removes it and is safe to repeat");
   var profile=new Profile{Kind="OpenVPN",Path="C:\\credential-test.ovpn"};var second=new Profile{Kind="OpenVPN",Path="C:\\different-test.ovpn"};Check(CredentialStore.Key(profile)==CredentialStore.Key(profile)&&CredentialStore.Key(profile)!=CredentialStore.Key(second),"Profile credential keys are stable and separate");
  }finally{CredentialStore.Forget(key);CredentialStore.Forget(other);}
 }
}
