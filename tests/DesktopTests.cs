using System;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
static class DesktopTests {
 static void Check(bool pass,string text){if(!pass)throw new Exception(text);Console.WriteLine("PASS: "+text);}
 public static void Metrics(){
  var session=new VpnSession();session.ReadState(">STATE:100,CONNECTED,SUCCESS,10.9.0.7,198.51.100.10");session.ReadTraffic("2048,4096");Thread.Sleep(30);var snapshot=session.Metrics.Snapshot();Check(snapshot.Address=="10.9.0.7","VPN address comes from the engine's assigned-address field");Check(snapshot.Received==2048&&snapshot.Sent==4096,"Traffic directions and totals parsed correctly");Check(snapshot.Duration.TotalMilliseconds>0,"Connected duration advances");session.ReadState(">STATE:101,RECONNECTING,network-unreachable,,");snapshot=session.Metrics.Snapshot();var frozen=snapshot.Duration;Thread.Sleep(30);Check(session.Metrics.Snapshot().Duration==frozen,"Connected duration pauses while reconnecting");Check(snapshot.Address==null,"Reconnecting clears a stale VPN address");session.ReadTraffic("-1,not-a-number");Check(session.Metrics.Snapshot().Received==2048,"Malformed traffic cannot overwrite valid counters");
  var sophos=new VpnSession();sophos.ReadSophosDetails("Network ----\n Local IP: 192.0.2.1\n Bytes Received: 8192\n Bytes Sent: 1024\n");snapshot=sophos.Metrics.Snapshot();Check(snapshot.Address==null&&snapshot.Received==8192,"Sophos outer transport address is not mislabeled as VPN IP");sophos.ReadSophosDetails("Virtual IP: 10.8.0.2\nBytes Received: 0\nBytes Sent: 0");Check(sophos.Metrics.Snapshot().Address=="10.8.0.2","Explicit Sophos virtual address is accepted");Check(ConnectionMetrics.Bytes(null)=="Unavailable"&&ConnectionMetrics.Bytes(0)=="0 B"&&ConnectionMetrics.Bytes(2048)=="2.0 KiB","Traffic labels distinguish unavailable values from zero");Check(ConnectionMetrics.Elapsed(TimeSpan.FromHours(25))=="25:00:00","Duration supports sessions longer than a day");
 }
 public static void Window(MainWindow form){var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  Check(!String.IsNullOrEmpty(((Label)typeof(MainWindow).GetField("hostnameValue",flags).GetValue(form)).Text),"Hostname appears in the details panel");
  var tray=(NotifyIcon)typeof(MainWindow).GetField("tray",flags).GetValue(form);Check(tray.Visible&&tray.Icon!=null,"Tray icon is registered with the app logo");
  var fake=new VpnSession();fake.ReadState(">STATE:100,CONNECTED,SUCCESS,10.9.0.7");fake.ReadTraffic("2048,4096");typeof(MainWindow).GetField("session",flags).SetValue(form,fake);((Label)typeof(MainWindow).GetField("status",flags).GetValue(form)).Text="Connected";typeof(MainWindow).GetMethod("RefreshDesktop",flags).Invoke(form,null);
  Check(((Label)typeof(MainWindow).GetField("receivedValue",flags).GetValue(form)).Text=="2.0 KiB","Connection details show engine traffic");
  typeof(MainWindow).GetField("trayHintShown",flags).SetValue(form,true);typeof(MainWindow).GetMethod("MinimizeToTray",flags).Invoke(form,null);Application.DoEvents();Check(!form.Visible&&!form.ShowInTaskbar&&Object.ReferenceEquals(typeof(MainWindow).GetField("session",flags).GetValue(form),fake),"Minimise hides the window while retaining the session");
  typeof(MainWindow).GetMethod("RestoreWindow",flags).Invoke(form,null);Application.DoEvents();Check(form.Visible&&form.ShowInTaskbar&&form.WindowState==FormWindowState.Normal,"Tray restore shows the normal app window");
  var connect=(ToolStripMenuItem)typeof(MainWindow).GetField("trayConnect",flags).GetValue(form);var disconnect=(ToolStripMenuItem)typeof(MainWindow).GetField("trayDisconnect",flags).GetValue(form);Check(!connect.Enabled&&disconnect.Enabled,"Tray actions reflect the current session");
  disconnect.PerformClick();Application.DoEvents();Check(typeof(MainWindow).GetField("session",flags).GetValue(form)==null,"Tray Disconnect invokes the shared disconnect path");
  var menu=(ContextMenuStrip)typeof(MainWindow).GetField("trayMenu",flags).GetValue(form);menu.Items[menu.Items.Count-1].PerformClick();Application.DoEvents();Check(form.IsDisposed&&!tray.Visible,"Tray Exit closes the app and removes its tray icon");
 }
}

