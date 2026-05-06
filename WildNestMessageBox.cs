using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project
{
    public static class WildNestMessageBox
    {
        public static DialogResult Show(string text)
            => Show(null, text, "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static DialogResult Show(string text, string caption)
            => Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
            => Show(null, text, caption, buttons, MessageBoxIcon.Information);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
            => Show(null, text, caption, buttons, icon);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
            => Show(null, text, caption, buttons, icon, defaultButton);

        public static DialogResult Show(IWin32Window? owner, string text)
            => Show(owner, text, "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static DialogResult Show(IWin32Window? owner, string text, string caption)
            => Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons)
            => Show(owner, text, caption, buttons, MessageBoxIcon.Information);

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using var dialog = new WildNestDialogForm(text, caption, buttons, icon);
            return owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        }

        public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            using var dialog = new WildNestDialogForm(text, caption, buttons, icon, defaultButton);
            return owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        }

        private sealed class WildNestDialogForm : Form
        {
            private readonly FlowLayoutPanel _buttonRow = new()
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(20, 14, 20, 16),
                BackColor = WildNestUI.Cream
            };

            public WildNestDialogForm(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
            {
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                BackColor = WildNestUI.Cream;
                AutoScaleMode = AutoScaleMode.None;
                Font = WildNestUI.FontBody(9f);
                Text = string.IsNullOrWhiteSpace(caption) ? "WildNest" : caption;

                int dialogWidth = 560;
                int contentWidth = dialogWidth - 120;
                int measuredBodyHeight = TextRenderer.MeasureText(
                    string.IsNullOrWhiteSpace(message) ? "WildNest notification." : message,
                    WildNestUI.FontBody(10f),
                    new Size(contentWidth, 0),
                    TextFormatFlags.WordBreak | TextFormatFlags.Left).Height;

                int bodyHeight = Math.Max(92, measuredBodyHeight + 14);
                int contentHeight = Math.Max(142, bodyHeight + 50);
                ClientSize = new Size(dialogWidth, 70 + contentHeight + _buttonRow.Height);

                var shell = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = WildNestUI.Cream,
                    Padding = new Padding(0)
                };

                var header = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 70,
                    BackColor = WildNestUI.Forest
                };

                var accent = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 3,
                    BackColor = WildNestUI.Gold
                };

                var title = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(22, 0, 22, 0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = WildNestUI.FontTitle(18f),
                    ForeColor = WildNestUI.Gold,
                    Text = string.IsNullOrWhiteSpace(caption) ? "WildNest Notice" : caption,
                    BackColor = Color.Transparent
                };

                header.Controls.Add(title);
                header.Controls.Add(accent);

                var content = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = WildNestUI.Cream,
                    Padding = new Padding(22, 20, 22, 14)
                };

                var iconBadge = BuildIconBadge(icon);
                iconBadge.Location = new Point(0, 8);

                var status = new Label
                {
                    AutoSize = true,
                    Location = new Point(78, 8),
                    Font = WildNestUI.FontLabel(9f),
                    ForeColor = ResolveAccent(icon),
                    Text = ResolveStatusText(icon),
                    BackColor = Color.Transparent
                };

                var body = new Label
                {
                    AutoSize = false,
                    Location = new Point(78, 34),
                    MaximumSize = new Size(contentWidth, 0),
                    Size = new Size(contentWidth, bodyHeight),
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    Text = string.IsNullOrWhiteSpace(message) ? "WildNest notification." : message,
                    BackColor = Color.Transparent
                };

                content.Controls.Add(iconBadge);
                content.Controls.Add(status);
                content.Controls.Add(body);

                Controls.Add(_buttonRow);
                Controls.Add(content);
                Controls.Add(header);
                BuildButtons(buttons, defaultButton);
            }

            private Panel BuildIconBadge(MessageBoxIcon icon)
            {
                var badge = new Panel
                {
                    Size = new Size(62, 62),
                    BackColor = ResolveBadgeBackground(icon)
                };

                badge.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var path = WildNestUI.RoundRect(new Rectangle(0, 0, badge.Width - 1, badge.Height - 1), 16);
                    using var fill = new SolidBrush(ResolveBadgeBackground(icon));
                    using var border = new Pen(Color.FromArgb(50, ResolveAccent(icon)), 1f);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);

                    var glyphBounds = new Rectangle(0, 2, badge.Width - 1, badge.Height - 5);
                    TextRenderer.DrawText(
                        e.Graphics,
                        ResolveGlyph(icon),
                        ResolveGlyphFont(icon),
                        glyphBounds,
                        ResolveAccent(icon),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                };

                return badge;
            }

            private void BuildButtons(MessageBoxButtons buttons, MessageBoxDefaultButton defaultButton)
            {
                var configs = ResolveButtons(buttons);
                int defaultIndex = defaultButton switch
                {
                    MessageBoxDefaultButton.Button2 => 1,
                    MessageBoxDefaultButton.Button3 => 2,
                    _ => 0
                };

                for (int i = 0; i < configs.Length; i++)
                {
                    var (label, result, primary) = configs[i];
                    var button = primary
                        ? WildNestUI.BtnPrimary(label, 128, 36)
                        : WildNestUI.BtnOutline(label, 110, 36);

                    if (primary)
                    {
                        button.ForeColor = WildNestUI.Cream;
                        button.BackColor = ResolveAccentButtonBackColor();
                        button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(ResolveAccentButtonBackColor(), .08f);
                        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(ResolveAccentButtonBackColor(), .16f);
                    }

                    button.DialogResult = result;
                    button.Margin = new Padding(12, 0, 0, 0);
                    _buttonRow.Controls.Add(button);

                    if (i == defaultIndex)
                    {
                        AcceptButton = button;
                    }
                }

                AcceptButton ??= _buttonRow.Controls.Count > 0 ? _buttonRow.Controls[0] as Button : null;
                CancelButton = FindCancelButton();
            }

            private Button? FindCancelButton()
            {
                foreach (Control control in _buttonRow.Controls)
                {
                    if (control is Button button && (button.DialogResult == DialogResult.Cancel || button.DialogResult == DialogResult.No))
                    {
                        return button;
                    }
                }

                return null;
            }

            private (string label, DialogResult result, bool primary)[] ResolveButtons(MessageBoxButtons buttons)
            {
                return buttons switch
                {
                    MessageBoxButtons.YesNo => new[]
                    {
                        ("Yes, Continue", DialogResult.Yes, true),
                        ("No", DialogResult.No, false)
                    },
                    _ => new[]
                    {
                        ("OK", DialogResult.OK, true)
                    }
                };
            }

            private string ResolveGlyph(MessageBoxIcon icon)
            {
                return icon switch
                {
                    MessageBoxIcon.Error or MessageBoxIcon.Stop => "!",
                    MessageBoxIcon.Warning => "!",
                    MessageBoxIcon.Question => "?",
                    _ => "i"
                };
            }

            private Font ResolveGlyphFont(MessageBoxIcon icon)
            {
                return icon switch
                {
                    MessageBoxIcon.Warning => new Font("Georgia", 26f, FontStyle.Bold),
                    MessageBoxIcon.Error or MessageBoxIcon.Stop => new Font("Georgia", 24f, FontStyle.Bold),
                    MessageBoxIcon.Question => new Font("Georgia", 24f, FontStyle.Bold),
                    _ => new Font("Georgia", 22f, FontStyle.Bold)
                };
            }

            private string ResolveStatusText(MessageBoxIcon icon)
            {
                return icon switch
                {
                    MessageBoxIcon.Error or MessageBoxIcon.Stop => "CRITICAL NOTICE",
                    MessageBoxIcon.Warning => "ACTION NEEDED",
                    MessageBoxIcon.Question => "CONFIRM DECISION",
                    _ => "WILDNEST UPDATE"
                };
            }

            private Color ResolveAccent(MessageBoxIcon icon)
            {
                return icon switch
                {
                    MessageBoxIcon.Error or MessageBoxIcon.Stop => WildNestUI.Red,
                    MessageBoxIcon.Warning => WildNestUI.Amber,
                    MessageBoxIcon.Question => WildNestUI.Blue,
                    _ => WildNestUI.Green
                };
            }

            private Color ResolveBadgeBackground(MessageBoxIcon icon)
            {
                return icon switch
                {
                    MessageBoxIcon.Error or MessageBoxIcon.Stop => WildNestUI.AlertBg,
                    MessageBoxIcon.Warning => Color.FromArgb(255, 246, 229),
                    MessageBoxIcon.Question => Color.FromArgb(232, 241, 255),
                    _ => WildNestUI.OkBg
                };
            }

            private Color ResolveAccentButtonBackColor()
            {
                return WildNestUI.Forest;
            }
        }
    }
}
