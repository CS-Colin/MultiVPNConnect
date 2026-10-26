using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
class SavedLogin {public string Username,Password;}
static class CredentialStore {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
 struct Credential {public uint Flags,Type;public string TargetName,Comment;public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;public uint CredentialBlobSize;public IntPtr CredentialBlob;public uint Persist,AttributeCount;public IntPtr Attributes;public string TargetAlias,UserName;}
 [DllImport("advapi32.dll",EntryPoint="CredWriteW",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Write(ref Credential credential,uint flags);
 [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Read(string target,uint type,uint flags,out IntPtr credential);
 [DllImport("advapi32.dll",EntryPoint="CredDeleteW",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Delete(string target,uint type,uint flags);
 [DllImport("advapi32.dll")]static extern void CredFree(IntPtr buffer);
 public static string Key(Profile profile){return "MultiVPNConnect/"+profile.Kind+"/"+VpnSession.Id(profile);}
 public static SavedLogin Load(string key){IntPtr buffer;if(!Read(key,1,0,out buffer)){int error=Marshal.GetLastWin32Error();if(error==1168)return null;throw new Win32Exception(error,"Windows could not read the saved VPN login.");}try{var data=(Credential)Marshal.PtrToStructure(buffer,typeof(Credential));if(data.CredentialBlobSize>2560||data.CredentialBlobSize%2!=0)throw new Exception("The saved login is invalid.");return new SavedLogin{Username=data.UserName??"",Password=Marshal.PtrToStringUni(data.CredentialBlob,(int)data.CredentialBlobSize/2)??""};}finally{CredFree(buffer);}}
 public static void Save(string key,string username,string password){if(username==null||username.Length>513||password==null||password.Length>1280)throw new ArgumentException("The username or password is too long for Windows credential storage.");byte[] bytes=Encoding.Unicode.GetBytes(password);IntPtr blob=Marshal.AllocHGlobal(Math.Max(1,bytes.Length));try{Marshal.Copy(bytes,0,blob,bytes.Length);var data=new Credential{Type=1,TargetName=key,Comment="MultiVPNConnect saved VPN login",UserName=username,CredentialBlobSize=(uint)bytes.Length,CredentialBlob=blob,Persist=2};if(!Write(ref data,0))throw new Win32Exception(Marshal.GetLastWin32Error(),"Windows could not save the VPN login.");}finally{for(int i=0;i<bytes.Length;i++)Marshal.WriteByte(blob,i,0);Array.Clear(bytes,0,bytes.Length);Marshal.FreeHGlobal(blob);}}
 public static void Forget(string key){if(!Delete(key,1,0)){int error=Marshal.GetLastWin32Error();if(error!=1168)throw new Win32Exception(error,"Windows could not remove the saved VPN login.");}}
}
