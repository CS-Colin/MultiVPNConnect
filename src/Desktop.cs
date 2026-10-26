using System;
using System.Drawing;
using System.Windows.Forms;

partial class MainWindow {
 NotifyIcon tray;
 ContextMenuStrip trayMenu;
 ToolStripMenuItem trayStatus,trayConnect,trayDisconnect;
 System.Windows.Forms.Timer detailsTimer;
 Label durationValue,addressValue,receivedValue,sentValue;
 ToolTip detailTips;
 bool trayHintShown;
 void InitializeDesktop(TableLayoutPanel main){
  var card=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,16),Padding=new Padding(14,9,14,9)};
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=8,Margin=new Padding(0)};
  for(int i=0;i<4;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,22));grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,22));grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));
  for(int row=0;row<2;row++){grid.RowStyles.Add(new RowStyle(SizeType.Absolute,22));grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));}
  string[] names={"CONNECTED TIME","VPN ADDRESS","RECEIVED","SENT"};Label[] values=new Label[4];
  for(int i=0;i<4;i++){var caption=Label(names[i],8,FontStyle.Bold,Palette.Muted);caption.Dock=DockStyle.Fill;grid.Controls.Add(caption,i,0);values[i]=Label(i==0?"00:00:00":"—",i==1?10:13,FontStyle.Bold,Palette.Text);values[i].Dock=DockStyle.Fill;values[i].TextAlign=ContentAlignment.MiddleLeft;grid.Controls.Add(values[i],i,1);}
  durationValue=values[0];addressValue=values[1];receivedValue=values[2];sentValue=values[3];card.Controls.Add(grid);
  var activityPanel=main.GetControlFromPosition(0,2);main.Controls.Remove(activityPanel);main.RowStyles.RemoveAt(2);main.RowCount=2;main.RowStyles[1].SizeType=SizeType.Percent;main.RowStyles[1].Height=100;
  var tabs=new TableLayoutPanel{Dock=DockStyle.Fill,Margin=new Padding(0),ColumnCount=1,RowCount=2};tabs.RowStyles.Add(new RowStyle(SizeType.Absolute,44));tabs.RowStyles.Add(new RowStyle(SizeType.Percent,100));var bar=new FlowLayoutPanel{Dock=DockStyle.Fill,Margin=new Padding(0)};var pages=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0)};
  var detailsTab=new VpnButton{Text="Connection details",Primary=true,Size=new Size(180,36),Margin=new Padding(0,0,8,4)};var activityTab=new VpnButton{Text="Activity",Size=new Size(120,36),Margin=new Padding(0,0,0,4)};bar.Controls.Add(detailsTab);bar.Controls.Add(activityTab);card.Margin=new Padding(0);activityPanel.Margin=new Padding(0);pages.Controls.Add(activityPanel);pages.Controls.Add(card);activityPanel.Visible=false;card.BringToFront();
  detailsTab.Click+=(s,e)=>{activityPanel.Visible=false;card.Visible=true;card.BringToFront();detailsTab.Primary=true;activityTab.Primary=false;detailsTab.Invalidate();activityTab.Invalidate();};activityTab.Click+=(s,e)=>{card.Visible=false;activityPanel.Visible=true;activityPanel.BringToFront();detailsTab.Primary=false;activityTab.Primary=true;detailsTab.Invalidate();activityTab.Invalidate();};tabs.Controls.Add(bar,0,0);tabs.Controls.Add(pages,0,1);main.Controls.Add(tabs,0,1);
  detailTips=new ToolTip();detailTips.SetToolTip(addressValue,"The assigned VPN address when the engine exposes it. This is not your public Internet IP.");
  internet=new InternetIdentity();
  var publicCaption=Label("PUBLIC IP",8,FontStyle.Bold,Palette.Muted);publicCaption.Dock=DockStyle.Fill;grid.Controls.Add(publicCaption,0,2);
  publicValue=Label("Checking…",11,FontStyle.Bold,Palette.Text);publicValue.Dock=DockStyle.Fill;grid.Controls.Add(publicValue,0,3);grid.SetColumnSpan(publicValue,2);
  var hostCaption=Label("PC HOSTNAME",8,FontStyle.Bold,Palette.Muted);hostCaption.Dock=DockStyle.Fill;grid.Controls.Add(hostCaption,2,2);
  hostnameValue=Label(internet.Hostname,11,FontStyle.Bold,Palette.Text);hostnameValue.Dock=DockStyle.Fill;grid.Controls.Add(hostnameValue,2,3);grid.SetColumnSpan(hostnameValue,2);detailTips.SetToolTip(hostnameValue,"This computer's hostname: "+internet.Hostname+". The hostname is read locally and is not sent to the IP lookup service.");
  refreshIdentity=new LinkLabel{Text="Refresh IP",Dock=DockStyle.Fill,LinkColor=Palette.Muted,ActiveLinkColor=Palette.Accent,TextAlign=ContentAlignment.TopRight};refreshIdentity.Click+=(s,e)=>RefreshInternetIdentity();grid.Controls.Add(refreshIdentity,1,2);
  Shown+=(s,e)=>{CheckInternetIdentity();RefreshInternetIdentity();};
  InitializeNetworkFields(grid);
  trayMenu=new ContextMenuStrip();trayMenu.Items.Add("Open MultiVPNConnect",null,(s,e)=>RestoreWindow());trayStatus=new ToolStripMenuItem("Disconnected"){Enabled=false};trayMenu.Items.Add(trayStatus);trayMenu.Items.Add(new ToolStripSeparator());
  trayConnect=new ToolStripMenuItem("Connect selected",null,(s,e)=>{RestoreWindow();Connect(s,e);});trayDisconnect=new ToolStripMenuItem("Disconnect",null,Disconnect);trayMenu.Items.Add(trayConnect);trayMenu.Items.Add(trayDisconnect);trayMenu.Items.Add(new ToolStripSeparator());trayMenu.Items.Add("Exit",null,(s,e)=>{RestoreWindow();Close();});trayMenu.Opening+=(s,e)=>RefreshDesktop();
  tray=new NotifyIcon{Icon=Icon,Text="MultiVPNConnect — Disconnected",ContextMenuStrip=trayMenu,Visible=true};tray.DoubleClick+=(s,e)=>RestoreWindow();
  Resize+=(s,e)=>{if(WindowState==FormWindowState.Minimized)MinimizeToTray();};
  detailsTimer=new System.Windows.Forms.Timer{Interval=1000};detailsTimer.Tick+=(s,e)=>RefreshDesktop();detailsTimer.Start();RefreshDesktop();
 }
 void MinimizeToTray(){Hide();ShowInTaskbar=false;if(!trayHintShown){trayHintShown=true;tray.ShowBalloonTip(3000,"MultiVPNConnect","Running in the system tray. Double-click the shield to reopen.",ToolTipIcon.Info);}}
 void RestoreWindow(){ShowInTaskbar=true;Show();WindowState=FormWindowState.Normal;Activate();}
 void RefreshDesktop(){if(tray==null)return;RefreshNetworkFields();CheckInternetIdentity();string state=session==null?"Disconnected":status.Text;string title="MultiVPNConnect — "+state;tray.Text=title.Length>63?title.Substring(0,63):title;trayStatus.Text=state;trayConnect.Enabled=!busy&&session==null&&Selected!=null;trayDisconnect.Enabled=!busy&&session!=null;trayConnect.Text=Selected==null?"Connect selected":"Connect "+(Selected.Name.Length>28?Selected.Name.Substring(0,28)+"…":Selected.Name);
  var snapshot=session==null?new ConnectionSnapshot():session.Metrics.Snapshot();durationValue.Text=ConnectionMetrics.Elapsed(snapshot.Duration);addressValue.Text=snapshot.Address??"Unavailable";receivedValue.Text=ConnectionMetrics.Bytes(snapshot.Received);sentValue.Text=ConnectionMetrics.Bytes(snapshot.Sent);
 }
 protected override void Dispose(bool disposing){if(disposing){RemoveRecoveryEvents();if(internet!=null)internet.Dispose();timer.Stop();timer.Dispose();if(detailsTimer!=null){detailsTimer.Stop();detailsTimer.Dispose();}if(tray!=null){tray.Visible=false;tray.Dispose();}if(trayMenu!=null)trayMenu.Dispose();if(detailTips!=null)detailTips.Dispose();}base.Dispose(disposing);}
}
