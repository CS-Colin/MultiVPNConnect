using System;
using System.Drawing;
using System.Reflection;
static class Branding {
 public static Icon LoadIcon() {
  // Read our own resource, not the Windows shell's cached executable icon.
  using(var stream=typeof(Branding).Assembly.GetManifestResourceStream("MultiVPNConnect.AppIcon")) {
   if(stream==null)throw new InvalidOperationException("The application icon resource is missing.");
   using(var icon=new Icon(stream,64,64))return (Icon)icon.Clone();
  }
 }
}
