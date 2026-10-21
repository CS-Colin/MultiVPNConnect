using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
class IconTests {
 [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 static void Check(bool pass,string message){if(!pass)throw new Exception(message);Console.WriteLine("PASS: "+message);}
 [STAThread] static int Main(string[] args){try{
  var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
  using(var data=assembly.GetManifestResourceStream("MultiVPNConnect.AppIcon"))using(var reader=new BinaryReader(data)){Check(reader.ReadUInt16()==0&&reader.ReadUInt16()==1,"Embedded resource is a Windows icon");Check(reader.ReadUInt16()==9,"Nine icon sizes are embedded for taskbar and display scaling");}
  using(var form=(Form)Activator.CreateInstance(assembly.GetType("MainWindow"),true)){
   Check(form.ShowIcon&&form.ShowInTaskbar,"Window enables its taskbar icon");
   foreach(int kind in new[]{0,1}){
    IntPtr handle=SendMessage(form.Handle,0x7F,new IntPtr(kind),IntPtr.Zero);
    Check(handle!=IntPtr.Zero,kind==0?"Windows small icon is set":"Windows large icon is set");
    using(var bitmap=Icon.FromHandle(handle).ToBitmap()){
     int teal=0;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){var pixel=bitmap.GetPixel(x,y);if(pixel.G>120&&pixel.B>80&&pixel.G>pixel.R*1.3)teal++;}
     Check(teal>5,"Native window icon contains the branded teal shield");
    }
   }
  }
  return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
