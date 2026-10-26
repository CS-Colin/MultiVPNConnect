using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Palette {
 public static readonly Color Background=Color.FromArgb(12,18,30),Surface=Color.FromArgb(19,28,44),Raised=Color.FromArgb(26,38,57),Border=Color.FromArgb(39,54,75),Text=Color.FromArgb(238,244,252),Muted=Color.FromArgb(144,162,185),Accent=Color.FromArgb(74,215,189);
 public static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
class Card : Panel {
 public Card(){DoubleBuffered=true;BackColor=Palette.Surface;Padding=new Padding(24);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Palette.Round(new Rectangle(0,0,Width-1,Height-1),16))using(var pen=new Pen(Palette.Border))e.Graphics.DrawPath(pen,p);}
}
class VpnButton : Button {
 public bool Primary;
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);Invalidate();}
 public VpnButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Height=44;Font=new Font("Segoe UI",10,FontStyle.Bold);SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent==null?Palette.Surface:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color fill=Primary?Palette.Accent:Palette.Raised;if(!Enabled)fill=Palette.Raised;else if(ClientRectangle.Contains(PointToClient(Cursor.Position)))fill=Primary?Color.FromArgb(104,234,211):Color.FromArgb(36,52,74);using(var path=Palette.Round(new Rectangle(0,0,Width-1,Height-1),10))using(var brush=new SolidBrush(fill))e.Graphics.FillPath(brush,path);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,!Enabled?Color.FromArgb(91,111,137):Primary?Palette.Background:Palette.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(Focused){var r=ClientRectangle;r.Inflate(-5,-5);ControlPaint.DrawFocusRectangle(e.Graphics,r);}}
}
class ProfileList : ListBox {
 public ProfileList(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=72;BorderStyle=BorderStyle.None;BackColor=Palette.Surface;ForeColor=Palette.Text;IntegralHeight=false;}
 protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0||e.Index>=Items.Count)return;var profile=Items[e.Index] as Profile;if(profile==null)return;bool selected=(e.State&DrawItemState.Selected)!=0;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var background=new SolidBrush(BackColor))e.Graphics.FillRectangle(background,e.Bounds);var rect=new Rectangle(e.Bounds.X+2,e.Bounds.Y+3,e.Bounds.Width-6,e.Bounds.Height-7);using(var path=Palette.Round(rect,10))using(var brush=new SolidBrush(selected?Palette.Raised:Palette.Surface))e.Graphics.FillPath(brush,path);Color tint=profile.Kind=="WireGuard"?Color.FromArgb(188,156,255):profile.Kind=="OpenVPN"?Color.FromArgb(255,183,105):Color.FromArgb(102,186,255);using(var b=new SolidBrush(tint))e.Graphics.FillEllipse(b,rect.Left+16,rect.Top+22,8,8);using(var bold=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,(profile.Favourite?"★ ":"")+profile.Name,bold,new Rectangle(rect.Left+36,rect.Top+10,rect.Width-47,25),Palette.Text,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);TextRenderer.DrawText(e.Graphics,profile.Kind+(String.IsNullOrWhiteSpace(profile.Group)?"":" · "+profile.Group),Font,new Rectangle(rect.Left+36,rect.Top+34,rect.Width-47,20),Palette.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);if(selected)using(var pen=new Pen(Palette.Accent,2))e.Graphics.DrawLine(pen,rect.Left+1,rect.Top+14,rect.Left+1,rect.Bottom-14);if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();}
}
class ConnectionOrb : Control {
 public Color Tint=Palette.Muted; public bool Active;
 public ConnectionOrb(){DoubleBuffered=true;Size=new Size(112,112);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int size=Math.Min(Width,Height)-6;var outer=new Rectangle((Width-size)/2,(Height-size)/2,size,size);using(var b=new SolidBrush(Color.FromArgb(24,Tint)))e.Graphics.FillEllipse(b,outer);outer.Inflate(-10,-10);using(var p=new Pen(Color.FromArgb(90,Tint),1.5f))e.Graphics.DrawEllipse(p,outer);float cx=Width/2f,cy=Height/2f;var pts=new[]{new PointF(cx,cy-24),new PointF(cx+20,cy-16),new PointF(cx+17,cy+10),new PointF(cx,cy+25),new PointF(cx-17,cy+10),new PointF(cx-20,cy-16)};using(var p=new Pen(Tint,2.5f)){p.LineJoin=LineJoin.Round;e.Graphics.DrawPolygon(p,pts);if(Active){e.Graphics.DrawLine(p,cx-8,cy,cx-1,cy+7);e.Graphics.DrawLine(p,cx-1,cy+7,cx+10,cy-7);}}}
}

class SearchBox : TextBox {
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
 static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,string text);
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SendMessage(Handle,0x1501,new IntPtr(1),"Search connections…");}
}
