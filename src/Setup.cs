using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
class Setup {
 internal static bool IsInstalledCopy(string executable, string target) {
  return !String.IsNullOrEmpty(executable) && String.Equals(Path.GetFullPath(executable), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase);
 }
 static void CloseInstalledCopy(string target) {
  foreach (var process in Process.GetProcessesByName("MultiVPNConnect")) using (process) {
   try {
    if (process.HasExited || !IsInstalledCopy(process.MainModule.FileName, target)) continue;
    process.CloseMainWindow();
    if (process.WaitForExit(15000)) continue;
    var version = FileVersionInfo.GetVersionInfo(target);
    if (version.FileMajorPart > 0 || version.FileMinorPart >= 2) throw new Exception("Close MultiVPNConnect and disconnect its session before installing. Installation has not changed the app.");
    if (MessageBox.Show("The installed app is still running. Close it now to continue installing? This appears to be an older background copy.", "Close MultiVPNConnect", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
     throw new Exception("Installation cancelled. Close the installed app and try again.");
    process.Kill();
    if (!process.WaitForExit(5000)) throw new Exception("The installed app could not be closed. Close it in Task Manager and try again.");
   } catch (InvalidOperationException) { /* The process already exited. */ }
  }
 }
 [STAThread] static void Main() {
  Application.EnableVisualStyles();
  if (MessageBox.Show("Install MultiVPNConnect for your Windows account?", "MultiVPNConnect Setup", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
  try {
   string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MultiVPNConnect");
   Directory.CreateDirectory(dir);
   string target = Path.Combine(dir, "MultiVPNConnect.exe");
   CloseInstalledCopy(target);
   string staged = target + ".new";
   using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("app"))
   using (var output = File.Create(staged)) input.CopyTo(output);
   if (File.Exists(target)) File.Replace(staged, target, null); else File.Move(staged, target);
   Type shell = Type.GetTypeFromProgID("WScript.Shell");
   dynamic obj = Activator.CreateInstance(shell);
   dynamic link = obj.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "MultiVPNConnect.lnk"));
   link.TargetPath = target; link.WorkingDirectory = dir; link.Save();
   MessageBox.Show("Installed. Open MultiVPNConnect from the Start menu. To uninstall, delete the application folder and Start menu shortcut. Saved profiles are kept separately in Local AppData\\MultiVPNConnect.");
  } catch (Exception ex) { MessageBox.Show("Installation failed: " + ex.Message); Environment.ExitCode = 1; }
 }
}


