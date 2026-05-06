using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Project.Homepage_Buttons
{
    public partial class UcAllResidents : UserControl
    {
        // ── SHARED MODEL ─────────────────────────────────────────
        public class AnimalInfo
        {
            public string Name, Species, Zone, Category, Conservation, PhotoFile, Emoji;
            public Color ConsColor;
        }

        private List<AnimalInfo> _allAnimals;
        private List<AnimalInfo> _filtered;
        private string _activeCategory = "All";

        private static readonly Color C_BG = Color.FromArgb(230, 226, 220);
        private static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        private static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        private static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        private static readonly Color C_TEXT = Color.FromArgb(22, 22, 22);
        private static readonly Color C_SUBTEXT = Color.FromArgb(110, 110, 110);
        private static readonly Color C_CARD_BG = Color.FromArgb(255, 255, 255);

        public UcAllResidents(List<AnimalInfo> animals)
        {
            InitializeComponent();
            _allAnimals = animals;
            _filtered = animals;
            this.DoubleBuffered = true;
            StyleAll();
            BuildCards();
        }

        public UcAllResidents()
        {
            InitializeComponent();
            _allAnimals = new List<AnimalInfo>();
            _filtered = _allAnimals;
            this.DoubleBuffered = true;
            StyleAll();
        }

        // ════════════════════════════════════════════════════════════
        //  HELPER: load image from embedded Resources by filename
        // ════════════════════════════════════════════════════════════
        private static Image LoadPhoto(string photoFile)
        {
            try
            {
                return Project.AppAssetLoader.LoadAnimalPhoto(photoFile);
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════
        //  STYLE
        // ════════════════════════════════════════════════════════════
        private void StyleAll()
        {
            this.BackColor = C_BG;
            pnlContentArea.BackColor = C_BG;
            pnlContentArea.Paint += PnlContentArea_Paint;

            // ── Header ──────────────────────────────────────────
            lblAllResidents.Text = "ALL RESIDENTS";
            lblAllResidents.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblAllResidents.ForeColor = C_GOLD;
            lblAllResidents.BackColor = Color.Transparent;
            lblAllResidents.Location = new Point(28, 22);
            lblAllResidents.AutoSize = true;

            lblTitle.Text = "Every Animal in the Sanctuary";
            lblTitle.Font = new Font("Georgia", 26f, FontStyle.Bold);
            lblTitle.ForeColor = C_TEXT;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.AutoSize = false;
            lblTitle.Size = new Size(900, 40);
            lblTitle.Location = new Point(28, 42);

            lblSubtitle.Text = "35 species across 8 wildlife zones — browse, filter, and explore every resident.";
            lblSubtitle.Font = new Font("Segoe UI", 10.5f);
            lblSubtitle.ForeColor = C_SUBTEXT;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.AutoSize = false;
            lblSubtitle.Size = new Size(800, 24);
            lblSubtitle.Location = new Point(28, 86);

            lblFilterBy.Text = "FILTER BY CATEGORY";
            lblFilterBy.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblFilterBy.ForeColor = Color.FromArgb(140, 140, 140);
            lblFilterBy.BackColor = Color.Transparent;
            lblFilterBy.Location = new Point(28, 122);
            lblFilterBy.AutoSize = true;

            lblAnimalCount.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblAnimalCount.ForeColor = C_SUBTEXT;
            lblAnimalCount.BackColor = Color.Transparent;
            lblAnimalCount.AutoSize = true;
            lblAnimalCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // ── Filter Buttons ───────────────────────────────────
            var btnDefs = new[]
            {
                (btnAll,       "All",       "🌿  All"),
                (btnMammals,   "Mammal",    "🦁  Mammals"),
                (btnBirds,     "Bird",      "🦜  Birds"),
                (btnReptiles,  "Reptile",   "🐍  Reptiles"),
                (btnAquatic,   "Aquatic",   "🦦  Aquatic"),
                (btnNocturnal, "Nocturnal", "🦉  Nocturnal"),
                (btnPrimates,  "Primate",   "🐒  Primates"),
            };

            int bx = 28;
            foreach (var (btn, cat, label) in btnDefs)
            {
                btn.Text = label;
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
                btn.Size = new Size(120, 34);
                btn.Location = new Point(bx, 142);
                btn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                bx += 128;

                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.Transparent;
                btn.Tag = (cat == "All");

                string capturedCat = cat;
                btn.Paint -= FilterBtn_Paint;
                btn.Paint += FilterBtn_Paint;
                btn.Click += (s, e) => ApplyFilter(capturedCat);
            }

            // ── Animals Grid ─────────────────────────────────────
            flpAnimals.BackColor = Color.Transparent;
            flpAnimals.Location = new Point(22, 190);
            flpAnimals.Size = new Size(pnlContentArea.Width - 44, pnlContentArea.Height - 198);
            flpAnimals.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            flpAnimals.AutoScroll = true;
            flpAnimals.WrapContents = true;
            flpAnimals.Padding = new Padding(0);

            pnlContentArea.Resize += (s, e) =>
            {
                lblAnimalCount.Location = new Point(pnlContentArea.Width - lblAnimalCount.PreferredWidth - 28, 126);
            };

            lblAnimalCount.Text = "35 animals shown";
        }

        private void PnlContentArea_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlContentArea.ClientRectangle;
            g.Clear(C_BG);

            // Subtle top highlight
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, r.Width, 4),
                Color.FromArgb(30, 212, 160, 23), Color.Transparent, LinearGradientMode.Vertical))
                g.FillRectangle(br, 0, 0, r.Width, 4);

            // Divider under filter bar
            using (var pen = new Pen(Color.FromArgb(50, 212, 160, 23), 1))
                g.DrawLine(pen, 22, 186, r.Width - 22, 186);
        }

        private void FilterBtn_Paint(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            bool active = btn.Tag is bool b && b;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = btn.ClientRectangle;
            r.Inflate(-1, -1);
            using (var path = MakeRoundedPath(r, 17))
            {
                if (active)
                {
                    using (var fill = new SolidBrush(Color.FromArgb(255, 248, 232)))
                        g.FillPath(fill, path);
                    using (var pen = new Pen(C_GOLD, 1.5f))
                        g.DrawPath(pen, path);
                }
                else
                {
                    using (var fill = new SolidBrush(Color.White))
                        g.FillPath(fill, path);
                    using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                        g.DrawPath(pen, path);
                }
            }
            TextRenderer.DrawText(g, btn.Text, btn.Font, btn.ClientRectangle,
                active ? C_GOLD : Color.FromArgb(80, 80, 80),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void SetFilterBtnStyle(Button btn, bool active)
        {
            btn.Tag = active;
            btn.Invalidate();
        }

        // ════════════════════════════════════════════════════════════
        //  FILTER
        // ════════════════════════════════════════════════════════════
        private void ApplyFilter(string category)
        {
            _activeCategory = category;
            _filtered = category == "All"
                ? _allAnimals
                : _allAnimals.Where(a => a.Category == category).ToList();

            lblAnimalCount.Text = $"{_filtered.Count} animal{(_filtered.Count != 1 ? "s" : "")} shown";
            lblAnimalCount.Location = new Point(pnlContentArea.Width - lblAnimalCount.PreferredWidth - 28, 126);

            var allBtns = new[] { (btnAll, "All"), (btnMammals, "Mammal"), (btnBirds, "Bird"),
                                   (btnReptiles, "Reptile"), (btnAquatic, "Aquatic"),
                                   (btnNocturnal, "Nocturnal"), (btnPrimates, "Primate") };
            foreach (var (b, c) in allBtns) SetFilterBtnStyle(b, c == category);

            BuildCards();
        }

        // ════════════════════════════════════════════════════════════
        //  BUILD CARDS
        // ════════════════════════════════════════════════════════════
        private void BuildCards()
        {
            flpAnimals.SuspendLayout();
            flpAnimals.Controls.Clear();
            lblAnimalCount.Text = $"{_filtered?.Count ?? 0} animals shown";

            if (_filtered == null || _filtered.Count == 0)
            {
                flpAnimals.ResumeLayout();
                return;
            }

            foreach (var a in _filtered)
                flpAnimals.Controls.Add(BuildCard(a));

            flpAnimals.ResumeLayout();
        }

        private Panel BuildCard(AnimalInfo a)
        {
            const int CARD_W = 220;
            const int CARD_H = 300;
            const int PHOTO_H = 155;

            var card = new Panel
            {
                Size = new Size(CARD_W, CARD_H),
                BackColor = C_CARD_BG,
                Margin = new Padding(7),
                Cursor = Cursors.Hand
            };

            // Rounded card clip
            card.HandleCreated += (s, e) =>
            {
                var path = MakeRoundedPath(card.ClientRectangle, 14);
                card.Region = new Region(path);
            };

            card.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var cr = card.ClientRectangle;
                cr.Inflate(-1, -1);
                using (var path = MakeRoundedPath(cr, 14))
                {
                    using (var fill = new SolidBrush(C_CARD_BG))
                        g.FillPath(fill, path);
                    using (var pen = new Pen(Color.FromArgb(14, 0, 0, 0), 1))
                        g.DrawPath(pen, path);
                }
                // Bottom shadow strip
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, cr.Height - 5, cr.Width, 5),
                    Color.FromArgb(12, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical))
                    g.FillRectangle(br, 0, cr.Height - 5, cr.Width, 5);
            };

            // ── Photo area using a SINGLE PictureBox + Paint overlay ──
            // Key fix: do NOT place a transparent Panel on top of a PictureBox.
            // Instead use the PictureBox's own Paint event to draw the overlay gradient.
            var pic = new PictureBox
            {
                Size = new Size(CARD_W, PHOTO_H),
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(12, 36, 20)
            };

            // Load photo from embedded resources
            try { pic.Image = LoadPhoto(a.PhotoFile); } catch { }

            // Draw gradient overlay + emoji + cons badge INSIDE the PictureBox Paint
            string shortCons = GetShortCons(a.Conservation);
            Color consColor = a.ConsColor;
            string emoji = a.Emoji;

            pic.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                var pr = pic.ClientRectangle;

                // Draw photo manually for full control
                if (pic.Image != null)
                    g.DrawImage(pic.Image, pr);

                // Bottom gradient vignette
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, pr.Height / 2, pr.Width, pr.Height / 2),
                    Color.Transparent, Color.FromArgb(190, 5, 20, 10), LinearGradientMode.Vertical))
                    g.FillRectangle(br, 0, pr.Height / 2, pr.Width, pr.Height / 2);

                // Top gradient vignette
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, 0, pr.Width, 30),
                    Color.FromArgb(60, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical))
                    g.FillRectangle(br, 0, 0, pr.Width, 30);

                // Emoji bottom-left
                TextRenderer.DrawText(g, emoji,
                    new Font("Segoe UI", 20f),
                    new Rectangle(6, pr.Height - 38, 40, 36),
                    Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // Conservation badge top-right
                var badgeRect = new Rectangle(pr.Width - 42, 7, 36, 18);
                using (var path = MakeRoundedPath(badgeRect, 9))
                {
                    using (var fill = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                        g.FillPath(fill, path);
                    using (var pen = new Pen(consColor, 1.2f))
                        g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, shortCons,
                    new Font("Segoe UI", 7f, FontStyle.Bold),
                    badgeRect, consColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // ── Card body labels ──────────────────────────────────
            var lblZone = new Label
            {
                Text = a.Zone.ToUpper(),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = C_GOLD,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(196, 16),
                Location = new Point(12, PHOTO_H + 8)
            };

            var lblName = new Label
            {
                Text = a.Name,
                Font = new Font("Georgia", 12.5f, FontStyle.Bold),
                ForeColor = C_TEXT,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(196, 26),
                Location = new Point(12, PHOTO_H + 24)
            };

            var lblSp = new Label
            {
                Text = a.Species,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = C_SUBTEXT,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(196, 16),
                Location = new Point(12, PHOTO_H + 50)
            };

            var sep = new Panel
            {
                Size = new Size(192, 1),
                Location = new Point(12, PHOTO_H + 70),
                BackColor = Color.FromArgb(18, 0, 0, 0)
            };

            var lblCategory = new Label
            {
                Text = $"{a.Emoji}  {a.Category}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(196, 22),
                Location = new Point(12, PHOTO_H + 76)
            };

            var lblConsStatus = new Label
            {
                Text = a.Conservation,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = a.ConsColor,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(196, 16),
                Location = new Point(12, PHOTO_H + 104)
            };

            card.Controls.Add(pic);
            card.Controls.Add(lblZone);
            card.Controls.Add(lblName);
            card.Controls.Add(lblSp);
            card.Controls.Add(sep);
            card.Controls.Add(lblCategory);
            card.Controls.Add(lblConsStatus);

            // Hover effect — lift card slightly with border
            card.MouseEnter += (s, e) =>
            {
                card.BackColor = Color.FromArgb(255, 253, 248);
                card.Invalidate();
            };
            card.MouseLeave += (s, e) =>
            {
                card.BackColor = C_CARD_BG;
                card.Invalidate();
            };

            return card;
        }

        private string GetShortCons(string c) => c switch
        {
            "Least Concern" => "LC",
            "Near Threatened" => "NT",
            "Vulnerable" => "VU",
            "Endangered" => "EN",
            "Critically Endangered" => "CR",
            _ => "LC"
        };

        private static GraphicsPath MakeRoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
