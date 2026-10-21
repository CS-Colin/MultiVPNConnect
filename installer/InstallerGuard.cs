using System;
using System.IO;
class InstallerGuard {
 static int Main(string[] args) {
  if(args.Length!=1)return 3;
  try {
   string target=Path.Combine(Path.GetFullPath(args[0]),"MultiVPNConnect.exe");
   if(!File.Exists(target))return 0;
   // Opening an executable for exclusive writing detects a loaded image as well as other file locks.
   using(var stream=new FileStream(target,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){}
   return 0;
  }catch(IOException){return 2;}catch(UnauthorizedAccessException){return 2;}catch{return 3;}
 }
}
