using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Project.Booking
{
    internal static class BookingFlowTheme
    {
        internal static readonly Color Gold = Color.FromArgb(212, 160, 23);
        internal static readonly Color GoldSoft = Color.FromArgb(18, 212, 160, 23);
        internal static readonly Color GoldLine = Color.FromArgb(70, 212, 160, 23);
        internal static readonly Color Dark = Color.FromArgb(7, 26, 14);
        internal static readonly Color Dark2 = Color.FromArgb(13, 36, 22);
        internal static readonly Color Dark3 = Color.FromArgb(18, 43, 26);
        internal static readonly Color Cream = Color.FromArgb(248, 244, 239);
        internal static readonly Color Cream2 = Color.FromArgb(240, 235, 227);
        internal static readonly Color Page = Color.FromArgb(240, 237, 232);
        internal static readonly Color PageAlt = Color.FromArgb(234, 229, 221);
        internal static readonly Color Text = Color.FromArgb(26, 26, 26);
        internal static readonly Color TextMuted = Color.FromArgb(107, 101, 96);
        internal static readonly Color TextDim = Color.FromArgb(155, 148, 144);
        internal static readonly Color Success = Color.FromArgb(39, 174, 96);
        internal static readonly Color Warning = Color.FromArgb(230, 126, 34);
        internal static readonly Color Danger = Color.FromArgb(192, 57, 43);
        internal static readonly Color Border = Color.FromArgb(223, 218, 212);

        internal static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static void ApplyRoundedRegion(Control control, int radius)
        {
            control.Resize += (s, e) =>
            {
                if (control.Width <= 0 || control.Height <= 0)
                {
                    return;
                }

                using var path = RoundedRect(new Rectangle(0, 0, control.Width, control.Height), radius);
                control.Region = new Region(path);
            };
        }

        internal static Panel CreateScrollPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Page
            };
        }

        internal static Panel CreateSection(string icon, string title, string subtitle, int width)
        {
            var section = new Panel
            {
                Width = width,
                Height = 220,
                BackColor = Color.Transparent
            };
            section.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, section.Width - 1, section.Height - 1), 12);
                using var fill = new SolidBrush(Color.White);
                using var border = new Pen(Color.FromArgb(25, 0, 0, 0), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = Color.Transparent
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(16, 0, 0, 0), 1f);
                e.Graphics.DrawLine(pen, 24, header.Height - 1, header.Width - 24, header.Height - 1);
            };

            header.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 18f),
                AutoSize = true,
                Location = new Point(24, 19),
                BackColor = Color.Transparent
            });

            header.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                ForeColor = Dark,
                AutoSize = true,
                Location = new Point(62, 14),
                BackColor = Color.Transparent
            });

            header.Controls.Add(new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextDim,
                AutoSize = false,
                Size = new Size(width - 86, 18),
                Location = new Point(62, 36),
                BackColor = Color.Transparent
            });

            var body = new Panel
            {
                Location = new Point(24, 76),
                Size = new Size(width - 48, 128),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            section.Controls.Add(body);
            section.Controls.Add(header);
            section.Tag = body;
            return section;
        }

        internal static Panel GetBody(Panel section) => (Panel)section.Tag;

        internal static int MeasureSectionHeight(Panel body) => body.Bottom + 24;

        internal static Label CreateFieldLabel(string text, int x, int y, int width = 220)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(width, 18),
                Location = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        internal static TextBox CreateTextBox(string placeholder = "")
        {
            return new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Cream,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = placeholder
            };
        }

        internal static ComboBox CreateCombo(string[] items)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Cream,
                Font = new Font("Segoe UI", 10f)
            };
            combo.Items.AddRange(items);
            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        internal static DateTimePicker CreateDatePicker()
        {
            return new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10f),
                CalendarForeColor = Text,
                CalendarMonthBackground = Cream,
                CalendarTitleBackColor = Dark,
                CalendarTitleForeColor = Gold,
                MinDate = DateTime.Today,
                Value = DateTime.Today,
                BackColor = Cream
            };
        }

        internal static Button CreatePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 208,
                Height = 42,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Gold,
                ForeColor = Dark,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 184, 75);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(190, 145, 20);
            return btn;
        }

        internal static Button CreateSecondaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 126,
                Height = 40,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 242, 237),
                ForeColor = TextMuted,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, 212, 160, 23);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = GoldSoft;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(32, Gold);
            return btn;
        }

        internal static Panel CreateNavBar(int y, int width)
        {
            return new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, 58),
                BackColor = Color.Transparent
            };
        }

        internal static void PaintStepBar(Graphics g, Rectangle bounds, string[] labels, int currentStep)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fill = new LinearGradientBrush(bounds, Dark, Dark2, 0f))
            {
                g.FillRectangle(fill, bounds);
            }

            using (var glow = new LinearGradientBrush(
                new Rectangle(bounds.X, bounds.Y, bounds.Width, Math.Max(1, bounds.Height / 2)),
                Color.FromArgb(36, Gold),
                Color.Transparent,
                90f))
            {
                g.FillRectangle(glow, bounds.X, bounds.Y, bounds.Width, Math.Max(1, bounds.Height / 2));
            }

            using var line = new Pen(GoldLine, 1f);
            g.DrawLine(line, 0, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

            int count = labels.Length;
            int colWidth = bounds.Width / count;
            const int circle = 34;
            int centerY = 34;

            for (int i = 0; i < count; i++)
            {
                int stepIndex = i + 1;
                int centerX = (i * colWidth) + (colWidth / 2);
                if (i > 0)
                {
                    int prevX = ((i - 1) * colWidth) + (colWidth / 2);
                    using var connector = new Pen(
                        stepIndex <= currentStep ? Gold : Color.FromArgb(90, 248, 244, 239),
                        2f);
                    g.DrawLine(connector, prevX + (circle / 2) + 10, centerY, centerX - (circle / 2) - 10, centerY);
                }

                var rect = new Rectangle(centerX - (circle / 2), centerY - (circle / 2), circle, circle);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                if (stepIndex < currentStep)
                {
                    using var doneFill = new SolidBrush(Dark);
                    g.FillEllipse(doneFill, rect);
                    g.DrawString("✓", new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(Gold), rect, sf);
                }
                else if (stepIndex == currentStep)
                {
                    using var activeFill = new SolidBrush(Gold);
                    g.FillEllipse(activeFill, rect);
                    g.DrawString(stepIndex.ToString(), new Font("Segoe UI", 11f, FontStyle.Bold), new SolidBrush(Dark), rect, sf);
                }
                else
                {
                    using var pendingFill = new SolidBrush(Color.FromArgb(28, 248, 244, 239));
                    using var pendingBorder = new Pen(Color.FromArgb(70, 248, 244, 239), 1.5f);
                    g.FillEllipse(pendingFill, rect);
                    g.DrawEllipse(pendingBorder, rect);
                    g.DrawString(stepIndex.ToString(), new Font("Segoe UI", 10f), new SolidBrush(Color.FromArgb(185, 248, 244, 239)), rect, sf);
                }

                bool isActive = stepIndex == currentStep;
                using var font = new Font("Segoe UI", 8.8f, isActive ? FontStyle.Bold : FontStyle.Regular);
                using var textBrush = new SolidBrush(isActive ? Cream : Color.FromArgb(160, 248, 244, 239));
                g.DrawString(labels[i], font, textBrush, new RectangleF(centerX - 70, centerY + 24, 140, 18), sf);
            }
        }

        internal static void PaintRoundedCard(Control control, PaintEventArgs e, bool selected = false, Color? fillColor = null)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, control.Width - 1, control.Height - 1), 10);
            using var fill = new SolidBrush(fillColor ?? Cream);
            using var border = new Pen(selected ? Gold : Border, selected ? 1.8f : 1f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            if (selected)
            {
                using var glow = new Pen(Color.FromArgb(48, Gold), 4f);
                e.Graphics.DrawPath(glow, path);
            }
        }

        internal static void PaintSectionShell(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var cardBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using var path = RoundedRect(cardBounds, 16);
            using var fill = new SolidBrush(Color.White);
            using var border = new Pen(Color.FromArgb(34, 0, 0, 0), 1f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);

            var headerHeight = Math.Min(74, Math.Max(58, bounds.Height / 3));
            var headerRect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, headerHeight);
            using var headerPath = RoundedRect(headerRect, 16);
            using (var headerFill = new LinearGradientBrush(headerRect, Dark, Dark2, 0f))
            {
                g.FillPath(headerFill, headerPath);
            }

            using var clip = new Region(path);
            var oldClip = g.Clip;
            g.Clip = clip;
            using (var glow = new LinearGradientBrush(
                new Rectangle(bounds.X, bounds.Y, bounds.Width, headerHeight + 24),
                Color.FromArgb(42, Gold),
                Color.Transparent,
                90f))
            {
                g.FillRectangle(glow, bounds.X, bounds.Y, bounds.Width, headerHeight + 24);
            }

            g.Clip = oldClip;
            using var divider = new Pen(GoldLine, 1f);
            g.DrawLine(divider, 24, headerHeight, bounds.Width - 24, headerHeight);
        }

        internal static void PaintNavBar(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(bounds.X, bounds.Y + 6, bounds.Width - 1, bounds.Height - 12);
            using var path = RoundedRect(rect, 12);
            using var fill = new SolidBrush(Color.FromArgb(235, 247, 243, 236));
            using var border = new Pen(Color.FromArgb(50, 212, 160, 23), 1f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);

            using var topLine = new Pen(Color.FromArgb(85, 212, 160, 23), 1f);
            g.DrawLine(topLine, rect.X + 18, rect.Y, rect.Right - 18, rect.Y);
        }

        internal static void StyleTextInput(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Cream;
            textBox.ForeColor = Text;
            textBox.Font = new Font("Segoe UI", 10f);
            textBox.Margin = new Padding(0);
        }

        internal static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = Cream;
            comboBox.ForeColor = Text;
            comboBox.Font = new Font("Segoe UI", 10f);
        }

        internal static void StyleSmallButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.White;
            btn.ForeColor = Text;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderColor = Color.FromArgb(42, 0, 0, 0);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = GoldSoft;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, Gold);
        }
    }
}
