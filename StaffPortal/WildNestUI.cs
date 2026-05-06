using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Project
{
    public static class WildNestUI
    {
        public static readonly Color Forest = Color.FromArgb(7, 26, 14);
        public static readonly Color ForestM = Color.FromArgb(13, 40, 24);
        public static readonly Color ForestL = Color.FromArgb(18, 52, 32);
        public static readonly Color Gold = Color.FromArgb(212, 160, 23);
        public static readonly Color GoldSoft = Color.FromArgb(28, 212, 160, 23);
        public static readonly Color Cream = Color.FromArgb(248, 244, 239);
        public static readonly Color DimCream = Color.FromArgb(128, 248, 244, 239);
        public static readonly Color Sand = Color.FromArgb(240, 237, 232);
        public static readonly Color Sand2 = Color.FromArgb(233, 229, 221);
        public static readonly Color White = Color.White;
        public static readonly Color Border = Color.FromArgb(221, 221, 221);
        public static readonly Color TextDark = Color.FromArgb(26, 26, 26);
        public static readonly Color Muted = Color.FromArgb(110, 105, 100);
        public static readonly Color Green = Color.FromArgb(22, 101, 52);
        public static readonly Color Amber = Color.FromArgb(133, 79, 11);
        public static readonly Color Blue = Color.FromArgb(24, 95, 165);
        public static readonly Color Red = Color.FromArgb(185, 28, 28);
        public static readonly Color AlertBg = Color.FromArgb(252, 235, 235);
        public static readonly Color AlertBdr = Color.FromArgb(240, 149, 149);
        public static readonly Color OkBg = Color.FromArgb(225, 245, 238);
        public static readonly Color OkBdr = Color.FromArgb(93, 202, 165);

        public static Font FontTitle(float sz = 16f) => new Font("Georgia", sz, FontStyle.Bold);
        public static Font FontSub(float sz = 9.5f) => new Font("Segoe UI", sz, FontStyle.Regular);
        public static Font FontLabel(float sz = 7.5f) => new Font("Segoe UI", sz, FontStyle.Bold);
        public static Font FontBody(float sz = 9f) => new Font("Segoe UI", sz, FontStyle.Regular);
        public static Font FontBold(float sz = 9f) => new Font("Segoe UI", sz, FontStyle.Bold);

        public static GraphicsPath RoundRect(Rectangle r, int rad = 8)
        {
            var p = new GraphicsPath();

            if (r.Width <= 1 || r.Height <= 1)
            {
                p.AddRectangle(new Rectangle(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height)));
                p.CloseFigure();
                return p;
            }

            int safeRadius = Math.Max(1, Math.Min(rad, Math.Min(r.Width, r.Height) / 2));
            int diameter = safeRadius * 2;

            if (diameter <= 2)
            {
                p.AddRectangle(r);
                p.CloseFigure();
                return p;
            }

            p.AddArc(r.X, r.Y, diameter, diameter, 180, 90);
            p.AddArc(r.Right - diameter, r.Y, diameter, diameter, 270, 90);
            p.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
            p.AddArc(r.X, r.Bottom - diameter, diameter, diameter, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void PaintSoftShadow(Graphics g, Rectangle rect, int radius = 12, int layers = 4)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = layers; i >= 1; i--)
            {
                int inflate = i * 2;
                var shadowRect = Rectangle.Inflate(rect, inflate, inflate);
                using var path = RoundRect(shadowRect, radius + inflate / 2);
                using var brush = new SolidBrush(Color.FromArgb(Math.Max(2, 10 - i), 0, 0, 0));
                g.FillPath(brush, path);
            }
        }

        public static Panel Card(int w, int h, int marginBottom = 12)
        {
            var c = new Panel
            {
                Size = new Size(w, h),
                BackColor = White,
                Margin = new Padding(0, 0, 0, marginBottom)
            };
            c.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                PaintSoftShadow(e.Graphics, new Rectangle(2, 4, c.Width - 6, c.Height - 8), 10, 3);
                using var path = RoundRect(new Rectangle(0, 0, c.Width - 1, c.Height - 1), 10);
                using var fill = new SolidBrush(White);
                using var border = new Pen(Border, 0.8f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            return c;
        }

        public static Panel CardWithHeader(int w, int h, string title, int headerH = 38, int marginBottom = 12)
        {
            var card = Card(w, h, marginBottom);
            var lbl = new Label
            {
                Text = title,
                Font = FontBold(11f),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(14, (headerH - 18) / 2),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lbl);
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(238, 238, 238), 0.8f);
                e.Graphics.DrawLine(pen, 0, headerH, card.Width, headerH);
            };
            return card;
        }

        public static Panel StatCard(string number, string label, Color numColor, int w = 0)
        {
            var card = new Panel { BackColor = White, Size = new Size(w > 0 ? w : 160, 84) };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                PaintSoftShadow(e.Graphics, new Rectangle(2, 4, card.Width - 6, card.Height - 8), 10, 3);
                using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                using var fill = new SolidBrush(White);
                using var border = new Pen(Border, 0.8f);
                using var accent = new SolidBrush(Color.FromArgb(22, numColor));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                e.Graphics.FillRectangle(accent, new Rectangle(0, 0, 6, card.Height));
            };
            card.Controls.Add(new Label
            {
                Text = number,
                Font = FontTitle(20f),
                ForeColor = numColor,
                AutoSize = true,
                Location = new Point(16, 12),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Text = label,
                Font = FontSub(8.5f),
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(16, 50),
                BackColor = Color.Transparent
            });
            return card;
        }

        public static Panel AlertBanner(string text, bool isAlert = true)
        {
            Color bg = isAlert ? AlertBg : OkBg;
            Color bdr = isAlert ? AlertBdr : OkBdr;
            Color fg = isAlert ? Color.FromArgb(80, 19, 19) : Color.FromArgb(4, 52, 44);
            string prefix = isAlert ? "Alert" : "Done";

            var pnl = new Panel { BackColor = bg, Size = new Size(800, 40), Margin = new Padding(0, 0, 0, 10) };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
                using var fill = new SolidBrush(bg);
                using var pen = new Pen(bdr, 1f);
                using var accent = new SolidBrush(Color.FromArgb(50, bdr));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
                e.Graphics.FillRectangle(accent, new Rectangle(0, 0, 6, pnl.Height));
            };
            pnl.Controls.Add(new Label
            {
                Text = prefix + "  " + text,
                Font = FontBody(9f),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(16, 11),
                BackColor = Color.Transparent
            });
            return pnl;
        }

        public static Panel PageHeader(string title, string sub, int w)
        {
            var pnl = new Panel { Size = new Size(w, 68), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 18) };
            pnl.Controls.Add(new Label
            {
                Text = title,
                Font = FontTitle(18f),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            });
            pnl.Controls.Add(new Label
            {
                Text = sub,
                Font = FontSub(9f),
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(1, 34),
                BackColor = Color.Transparent
            });
            return pnl;
        }

        public static Label SectionDivider(string text, int y, int x = 0)
        {
            return new Label
            {
                Text = text,
                Font = FontBold(9.5f),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        public static Label Badge(string text, BadgeStyle style)
        {
            Color fg;
            Color bg;
            switch (style)
            {
                case BadgeStyle.Green: fg = Color.FromArgb(8, 80, 65); bg = Color.FromArgb(225, 245, 238); break;
                case BadgeStyle.Amber: fg = Color.FromArgb(99, 56, 6); bg = Color.FromArgb(250, 238, 218); break;
                case BadgeStyle.Blue: fg = Color.FromArgb(12, 68, 124); bg = Color.FromArgb(230, 241, 251); break;
                case BadgeStyle.Red: fg = Color.FromArgb(80, 19, 19); bg = Color.FromArgb(252, 235, 235); break;
                case BadgeStyle.Gray: fg = Color.FromArgb(68, 68, 65); bg = Color.FromArgb(241, 239, 232); break;
                default: fg = TextDark; bg = Color.FromArgb(240, 240, 240); break;
            }

            var lbl = new Label
            {
                Text = text,
                Font = FontLabel(8f),
                ForeColor = fg,
                BackColor = bg,
                AutoSize = false,
                Size = new Size(TextRenderer.MeasureText(text, FontLabel(8f)).Width + 16, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lbl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), 10);
                using var fill = new SolidBrush(bg);
                using var pen = new Pen(Color.FromArgb(32, fg), 0.8f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    FontLabel(8f),
                    lbl.ClientRectangle,
                    fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            return lbl;
        }

        public static Button BtnPrimary(string text, int w = 0, int h = 30)
        {
            var b = new Button
            {
                Text = text,
                Font = FontBold(9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Forest,
                ForeColor = Gold,
                Cursor = Cursors.Hand,
                Size = new Size(w > 0 ? w : 110, h)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ForestL;
            b.FlatAppearance.MouseDownBackColor = ForestM;
            return b;
        }

        public static Button BtnOutline(string text, int w = 0, int h = 30)
        {
            var b = new Button
            {
                Text = text,
                Font = FontBody(9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = TextDark,
                Cursor = Cursors.Hand,
                Size = new Size(w > 0 ? w : 80, h)
            };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = GoldSoft;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, Gold);
            return b;
        }

        public static Button BtnAction(string text, ActionStyle style)
        {
            Color fg;
            Color bdr;
            switch (style)
            {
                case ActionStyle.Edit: fg = Green; bdr = Green; break;
                case ActionStyle.Delete: fg = Red; bdr = Red; break;
                case ActionStyle.View: fg = Blue; bdr = Blue; break;
                default: fg = TextDark; bdr = Border; break;
            }

            var b = new Button
            {
                Text = text,
                Font = FontBody(8f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = fg,
                Cursor = Cursors.Hand,
                Size = new Size(54, 24)
            };
            b.FlatAppearance.BorderColor = bdr;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, fg);
            return b;
        }

        public static TextBox SearchBox(string placeholder, int w = 300)
        {
            return new TextBox
            {
                PlaceholderText = placeholder,
                Size = new Size(w, 28),
                Font = FontBody(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = White
            };
        }

        public static int AddTableHeader(Panel parent, string[] cols, int[] widths, int yTop, int xLeft = 14)
        {
            int x = xLeft;
            for (int i = 0; i < cols.Length; i++)
            {
                parent.Controls.Add(new Label
                {
                    Text = cols[i].ToUpper(),
                    Font = FontLabel(7.5f),
                    ForeColor = Muted,
                    Location = new Point(x, yTop + 5),
                    Size = new Size(widths[i], 18),
                    BackColor = Color.FromArgb(248, 248, 248)
                });
                x += widths[i];
            }

            parent.Controls.Add(new Panel
            {
                Location = new Point(0, yTop + 26),
                Size = new Size(parent.Width, 1),
                BackColor = Color.FromArgb(238, 238, 238)
            });
            return yTop + 30;
        }

        public static void AddRowSeparator(Panel parent, int y)
        {
            parent.Controls.Add(new Panel
            {
                Location = new Point(0, y),
                Size = new Size(parent.Width, 1),
                BackColor = Color.FromArgb(245, 245, 245)
            });
        }

        public static void StyleSidebar(
            Panel sidebar,
            Panel contentArea,
            Label lblTitle,
            Label lblRole,
            Label lblUser,
            Button[] navBtns,
            Button signOutBtn,
            ref Button? activeBtn)
        {
            sidebar.BackColor = Forest;
            sidebar.Padding = new Padding(0);
            sidebar.Width = 220;
            sidebar.Dock = DockStyle.Left;

            sidebar.Paint += (s, e) =>
            {
                using var fill = new LinearGradientBrush(
                    new Rectangle(0, 0, sidebar.Width, sidebar.Height),
                    Forest,
                    ForestM,
                    90f);
                using var line = new Pen(Color.FromArgb(28, 212, 160, 23), 1f);
                e.Graphics.FillRectangle(fill, sidebar.ClientRectangle);
                e.Graphics.DrawLine(line, 14, 80, sidebar.Width - 14, 80);
                e.Graphics.DrawLine(line, 14, sidebar.Height - 66, sidebar.Width - 14, sidebar.Height - 66);
            };

            lblTitle.ForeColor = Cream;
            lblTitle.Font = new Font("Georgia", 11f, FontStyle.Bold);
            lblTitle.BackColor = Color.Transparent;

            lblRole.ForeColor = Gold;
            lblRole.Font = FontBold(7.5f);
            lblRole.BackColor = Color.Transparent;
            lblRole.AutoSize = true;

            lblUser.ForeColor = Color.FromArgb(90, 248, 244, 239);
            lblUser.Font = FontSub(8f);
            lblUser.BackColor = Color.Transparent;
            lblUser.AutoSize = true;

            void LayoutSidebarButtons()
            {
                lblTitle.Location = new Point(14, 16);
                lblRole.Location = new Point(14, 40);
                lblUser.Location = new Point(14, 58);

                int yPos = 112;
                int btnW = sidebar.Width - 28;
                foreach (var nav in navBtns)
                {
                    nav.Size = new Size(btnW, 44);
                    nav.Location = new Point(14, yPos);
                    yPos += 50;
                }

                signOutBtn.Size = new Size(btnW, 42);
                signOutBtn.Location = new Point(14, sidebar.Height - signOutBtn.Height - 18);
            }

            foreach (var btn in navBtns)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;
                btn.ForeColor = DimCream;
                btn.Font = FontBody(9.25f);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(12, 0, 0, 0);
                btn.Cursor = Cursors.Hand;

                var capturedBtn = btn;
                btn.MouseEnter += (s, e) =>
                {
                    if (!Equals(capturedBtn.Tag, "active"))
                    {
                        capturedBtn.BackColor = Color.FromArgb(18, 212, 160, 23);
                    }
                };
                btn.MouseLeave += (s, e) =>
                {
                    if (!Equals(capturedBtn.Tag, "active"))
                    {
                        capturedBtn.BackColor = Color.Transparent;
                    }
                };
                btn.Paint += (s, pe) =>
                {
                    if (!Equals(capturedBtn.Tag, "active"))
                    {
                        return;
                    }

                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundRect(new Rectangle(0, 0, capturedBtn.Width - 1, capturedBtn.Height - 1), 12);
                    using var fill = new SolidBrush(Color.FromArgb(36, Gold));
                    using var pen = new Pen(Color.FromArgb(70, Gold), 1f);
                    pe.Graphics.FillPath(fill, path);
                    pe.Graphics.DrawPath(pen, path);
                    using var accent = new Pen(Gold, 2f);
                    pe.Graphics.DrawLine(accent, 1, 6, 1, capturedBtn.Height - 7);
                };
            }

            signOutBtn.FlatStyle = FlatStyle.Flat;
            signOutBtn.FlatAppearance.BorderSize = 1;
            signOutBtn.FlatAppearance.BorderColor = Color.FromArgb(40, 226, 75, 74);
            signOutBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 226, 75, 74);
            signOutBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 226, 75, 74);
            signOutBtn.BackColor = Color.FromArgb(12, 226, 75, 74);
            signOutBtn.ForeColor = Color.FromArgb(226, 75, 74);
            signOutBtn.Font = FontBold(9f);
            signOutBtn.TextAlign = ContentAlignment.MiddleLeft;
            signOutBtn.Padding = new Padding(14, 0, 0, 0);
            signOutBtn.Cursor = Cursors.Hand;

            contentArea.BackColor = Sand;
            LayoutSidebarButtons();
            sidebar.Resize += (s, e) => LayoutSidebarButtons();
        }

        public static void SetActive(ref Button? activeBtn, Button newBtn)
        {
            if (activeBtn != null)
            {
                activeBtn.Tag = null;
                activeBtn.BackColor = Color.Transparent;
                activeBtn.ForeColor = DimCream;
                activeBtn.Invalidate();
            }

            activeBtn = newBtn;
            activeBtn.Tag = "active";
            activeBtn.BackColor = Color.FromArgb(36, 212, 160, 23);
            activeBtn.ForeColor = Gold;
            activeBtn.Invalidate();
        }

        public static Panel ScrollWrapper()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Sand,
                Padding = new Padding(32, 28, 32, 34)
            };
        }

        public static FlowLayoutPanel FlowColumn(int w)
        {
            return new FlowLayoutPanel
            {
                Width = w,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 56)
            };
        }
    }

    public enum BadgeStyle { Green, Amber, Blue, Red, Gray }
    public enum ActionStyle { Edit, Delete, View, Generic }
}
