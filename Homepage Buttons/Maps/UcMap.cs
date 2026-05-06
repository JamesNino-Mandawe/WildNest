using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Project
{
    public partial class UcMap : UserControl
    {
        private const float MapRatio = 1262f / 710f;
        private bool _updatingSize = false;

        private Image? _mapImage;
        private List<ZoneInfo> _zones = new();
        private List<CabinInfo> _cabins = new();

        private bool _showZones = false;
        private bool _showCabins = false;

        private Panel? _sidePanel;

        // ── Toolbar constants ──
        private const int BTN_SIZE = 48;
        private const int BTN_X = 14;
        private const int BTN_Y1 = 14;
        private const int BTN_Y2 = BTN_Y1 + BTN_SIZE + 10;

        private bool _zoneHover = false;
        private bool _cabinHover = false;

        // ── Cached bitmap: map + pins drawn once, buttons layered on top ──
        // This eliminates the black-flash: we never clear the panel to black.
        private Bitmap? _mapCache = null;   // map image + pins
        private bool _cacheValid = false;

        public UcMap()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.AutoScroll = false;
            this.BackColor = Color.FromArgb(240, 237, 232);

            _mapImage = AppAssetLoader.LoadImage("Map", "Resources", "Map.png");

            pnlMapContainer.BackgroundImage = null;
            pnlMapContainer.BackColor = Color.FromArgb(240, 237, 232);
            pnlMapContainer.Dock = DockStyle.None;

            // Use SetStyle on the panel for flicker-free painting
            typeof(Panel).GetMethod("SetStyle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(pnlMapContainer, new object[]{
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint, true });

            pnlMapContainer.Paint += PnlMapContainer_Paint;
            pnlMapContainer.MouseClick += PnlMap_MouseClick;
            pnlMapContainer.MouseMove += PnlMap_MouseMove;
            pnlMapContainer.MouseLeave += PnlMap_MouseLeave;

            BuildZoneData();
            BuildCabinData();
            SetupStatusHeader();
            SetupFooter();
            SetupSidePanel();
        }

        // Invalidate the map+pins cache whenever pins toggle
        private void InvalidateCache() { _cacheValid = false; pnlMapContainer.Invalidate(); }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_updatingSize || _mapImage == null || pnlMapContainer == null) return;
            _updatingSize = true;
            int drawH = (int)(this.Width / MapRatio);
            pnlMapContainer.Location = new Point(0, 85);
            pnlMapContainer.Width = this.Width;
            pnlMapContainer.Height = drawH;
            int totalHeight = 85 + drawH + 36;
            if (this.Height != totalHeight) this.Height = totalHeight;
            RepositionSidePanel();
            _cacheValid = false;   // size changed → rebuild cache
            _updatingSize = false;
        }

        // ════════════════════════════════════════════════════════════
        //  MASTER PAINT  — flicker-free two-layer approach
        //  Layer 1 (cached bitmap): map image + zone/cabin pins
        //  Layer 2 (live):          toolbar buttons only
        // ════════════════════════════════════════════════════════════
        private void PnlMapContainer_Paint(object sender, PaintEventArgs e)
        {
            int W = pnlMapContainer.Width, H = pnlMapContainer.Height;
            if (W <= 0 || H <= 0) return;

            // ── Rebuild map+pins cache if needed ──
            if (!_cacheValid || _mapCache == null ||
                _mapCache.Width != W || _mapCache.Height != H)
            {
                _mapCache?.Dispose();
                _mapCache = new Bitmap(W, H, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (var cg = Graphics.FromImage(_mapCache))
                {
                    cg.SmoothingMode = SmoothingMode.AntiAlias;
                    cg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    cg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    if (_mapImage != null) cg.DrawImage(_mapImage, 0, 0, W, H);
                    if (_showCabins) DrawCabinPins(cg, W, H);
                    if (_showZones) DrawZonePins(cg, W, H);
                }
                _cacheValid = true;
            }

            // ── Draw cached layer (no flicker — no clear-to-black) ──
            e.Graphics.DrawImageUnscaled(_mapCache, 0, 0);

            // ── Draw toolbar buttons live on top (tiny region, instant) ──
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawToolbarButton(e.Graphics, BTN_X, BTN_Y1, _showZones, _zoneHover, isZone: true);
            DrawToolbarButton(e.Graphics, BTN_X, BTN_Y2, _showCabins, _cabinHover, isZone: false);
        }

        // ════════════════════════════════════════════════════════════
        //  DRAW ONE TOOLBAR BUTTON  — premium vector symbols
        // ════════════════════════════════════════════════════════════
        private void DrawToolbarButton(Graphics g, int x, int y, bool active, bool hovered, bool isZone)
        {
            int sz = BTN_SIZE;
            Rectangle r = new Rectangle(x, y, sz, sz);
            float cx = x + sz / 2f;
            float cy = y + sz / 2f;
            float s = sz / 48f;   // scale unit

            // ── Step 1: clip to circle — NOTHING escapes ──
            using (var cp = new GraphicsPath())
            {
                cp.AddEllipse(r);
                g.SetClip(cp);
            }

            // Outer soft glow (inside clip — still no bleed)
            if (active || hovered)
            {
                Color glowC = active ? Color.FromArgb(60, 120, 255, 80)
                                     : Color.FromArgb(35, 180, 255, 160);
                using (var gb = new SolidBrush(glowC))
                    g.FillEllipse(gb, x - 4, y - 4, sz + 8, sz + 8);
            }

            // Body gradient
            Color top = active ? Color.FromArgb(255, 55, 160, 55)
                         : hovered ? Color.FromArgb(255, 30, 95, 42)
                                   : Color.FromArgb(255, 14, 58, 24);
            Color bottom = active ? Color.FromArgb(255, 22, 95, 28)
                         : hovered ? Color.FromArgb(255, 16, 58, 24)
                                   : Color.FromArgb(255, 7, 34, 14);
            using (var grad = new LinearGradientBrush(
                new PointF(x, y), new PointF(x, y + sz), top, bottom))
                g.FillEllipse(grad, r);

            // Inner shadow at bottom
            using (var shadow = new LinearGradientBrush(
                new PointF(x, y + sz * 0.55f), new PointF(x, y + sz),
                Color.FromArgb(0, 0, 0, 0), Color.FromArgb(60, 0, 0, 0)))
                g.FillEllipse(shadow, r);

            // Top gloss
            using (var gloss = new LinearGradientBrush(
                new PointF(x, y), new PointF(x, y + sz * 0.48f),
                Color.FromArgb(active ? 55 : 70, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255)))
                g.FillEllipse(gloss, x + 4, y + 3, sz - 8, (int)(sz * 0.5f));

            // ── Step 2: draw vector symbol inside clip ──
            if (isZone)
                DrawZoneSymbol(g, cx, cy, s, active);
            else
                DrawCabinSymbol(g, cx, cy, s, active);

            // ── Step 3: reset clip, draw crisp outer ring ──
            g.ResetClip();

            // Outer ring
            Color ringOuter = active ? Color.FromArgb(220, 140, 255, 90)
                            : hovered ? Color.FromArgb(140, 160, 255, 180)
                                      : Color.FromArgb(70, 200, 255, 200);
            using (var pen = new Pen(ringOuter, active ? 2f : 1.5f))
                g.DrawEllipse(pen, r);

            // Active: second pulse ring
            if (active)
                using (var pen = new Pen(Color.FromArgb(50, 160, 255, 100), 3.5f))
                    g.DrawEllipse(pen, x - 3, y - 3, sz + 6, sz + 6);
        }

        // ── ZONE SYMBOL: Sleek location pin with inner diamond ──
        private void DrawZoneSymbol(Graphics g, float cx, float cy, float s, bool active)
        {
            Color ic = active ? Color.FromArgb(255, 240, 255, 200)
                              : Color.FromArgb(230, 210, 245, 160);
            Color shadow = Color.FromArgb(60, 0, 0, 0);

            float pr = 9f * s;                 // pin head radius
            float pTop = cy - pr - 7f * s;       // pin head top

            // Shadow offset
            using (var b = new SolidBrush(shadow))
            {
                g.FillEllipse(b, cx - pr + 1, pTop + 1, pr * 2, pr * 2);
                PointF[] ts = {
                    new PointF(cx - pr * 0.6f + 1, pTop + pr * 1.4f + 1),
                    new PointF(cx + pr * 0.6f + 1, pTop + pr * 1.4f + 1),
                    new PointF(cx + 1,              pTop + pr * 3.1f + 1)
                };
                g.FillPolygon(b, ts);
            }

            // Pin body (filled circle head)
            using (var b = new SolidBrush(ic))
                g.FillEllipse(b, cx - pr, pTop, pr * 2, pr * 2);

            // Pin tail
            PointF[] tail = {
                new PointF(cx - pr * 0.62f, pTop + pr * 1.4f),
                new PointF(cx + pr * 0.62f, pTop + pr * 1.4f),
                new PointF(cx,              pTop + pr * 3.1f)
            };
            using (var b = new SolidBrush(ic)) g.FillPolygon(b, tail);

            // Inner hole — creates the classic pin ring
            float hr = pr * 0.42f;
            Color hole = active ? Color.FromArgb(200, 55, 160, 55) : Color.FromArgb(200, 14, 58, 24);
            using (var b = new SolidBrush(hole))
                g.FillEllipse(b, cx - hr, pTop + pr - hr, hr * 2, hr * 2);

            // Specular highlight on pin head
            using (var b = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                g.FillEllipse(b, cx - pr * 0.55f, pTop + pr * 0.12f, pr * 0.7f, pr * 0.55f);
        }

        // ── CABIN SYMBOL: Modern architectural house with chimney ──
        private void DrawCabinSymbol(Graphics g, float cx, float cy, float s, bool active)
        {
            Color ic = active ? Color.FromArgb(255, 240, 255, 200)
                                  : Color.FromArgb(230, 210, 245, 160);
            Color shadow = Color.FromArgb(55, 0, 0, 0);
            Color hole = active ? Color.FromArgb(210, 55, 160, 55) : Color.FromArgb(210, 14, 58, 24);

            float hw = 11f * s;   // half base width
            float hh = 8f * s;   // half base height
            float roofH = 11f * s;   // roof peak height above base top
            float baseTop = cy - hh * 0.2f;

            // Shadow
            using (var b = new SolidBrush(shadow))
            {
                PointF[] rs = {
                    new PointF(cx + 1,               baseTop - roofH + 1),
                    new PointF(cx - hw - 3f*s + 1,   baseTop + 1),
                    new PointF(cx + hw + 3f*s + 1,   baseTop + 1)
                };
                g.FillPolygon(b, rs);
                g.FillRectangle(b, cx - hw + 1, baseTop, hw * 2f, hh * 2f + 1);
            }

            // Roof
            PointF[] roof = {
                new PointF(cx,                 baseTop - roofH),
                new PointF(cx - hw - 3f * s,   baseTop),
                new PointF(cx + hw + 3f * s,   baseTop)
            };
            using (var b = new SolidBrush(ic)) g.FillPolygon(b, roof);

            // Roof highlight streak
            using (var b = new SolidBrush(Color.FromArgb(55, 255, 255, 255)))
            {
                PointF[] hl = {
                    new PointF(cx,             baseTop - roofH + 1f),
                    new PointF(cx - hw * 0.3f, baseTop - 1f),
                    new PointF(cx + hw * 0.3f, baseTop - 1f)
                };
                g.FillPolygon(b, hl);
            }

            // Walls
            using (var b = new SolidBrush(ic))
                g.FillRectangle(b, cx - hw, baseTop, hw * 2f, hh * 2f);

            // Chimney
            float chW = 3f * s, chH = 5.5f * s;
            float chX = cx + hw * 0.45f;
            using (var b = new SolidBrush(ic))
                g.FillRectangle(b, chX - chW / 2f, baseTop - roofH * 0.55f - chH, chW, chH);

            // Window (small square cutout)
            float wSize = 4.5f * s;
            float wTop = baseTop + hh * 0.15f;
            // Left window
            using (var b = new SolidBrush(hole))
                g.FillRectangle(b, cx - hw * 0.62f - wSize / 2f, wTop, wSize, wSize);
            // Right window
            using (var b = new SolidBrush(hole))
                g.FillRectangle(b, cx + hw * 0.62f - wSize / 2f, wTop, wSize, wSize);

            // Door
            float dw = 4.5f * s, dh = 6.5f * s;
            using (var b = new SolidBrush(hole))
                g.FillRectangle(b, cx - dw / 2f, baseTop + hh * 2f - dh, dw, dh);

            // Door arch
            using (var b = new SolidBrush(hole))
                g.FillEllipse(b, cx - dw / 2f, baseTop + hh * 2f - dh - dw * 0.4f, dw, dw * 0.85f);
        }

        // ════════════════════════════════════════════════════════════
        //  MOUSE EVENTS — no full Invalidate on hover, only button rects
        // ════════════════════════════════════════════════════════════
        private Rectangle ZoneButtonRect => new Rectangle(BTN_X, BTN_Y1, BTN_SIZE, BTN_SIZE);
        private Rectangle CabinButtonRect => new Rectangle(BTN_X, BTN_Y2, BTN_SIZE, BTN_SIZE);

        // Inflate slightly for the active pulse ring
        private Rectangle ZoneButtonRectPadded => Rectangle.Inflate(ZoneButtonRect, 5, 5);
        private Rectangle CabinButtonRectPadded => Rectangle.Inflate(CabinButtonRect, 5, 5);

        private void PnlMap_MouseClick(object sender, MouseEventArgs e)
        {
            if (ZoneButtonRect.Contains(e.Location))
            {
                _showZones = !_showZones;
                InvalidateCache();
                return;
            }
            if (CabinButtonRect.Contains(e.Location))
            {
                _showCabins = !_showCabins;
                InvalidateCache();
                return;
            }

            int W = pnlMapContainer.Width, H = pnlMapContainer.Height;
            if (_showZones)
                for (int i = 0; i < _zones.Count; i++)
                {
                    int px = (int)(W * _zones[i].PosX), py = (int)(H * _zones[i].PosY);
                    if (Dist(e.X, e.Y, px, py) <= 34) { OpenZonePanel(_zones[i]); return; }
                }
            if (_showCabins)
                for (int i = 0; i < _cabins.Count; i++)
                {
                    int px = (int)(W * _cabins[i].PosX), py = (int)(H * _cabins[i].PosY);
                    if (Dist(e.X, e.Y, px, py) <= 24) { OpenCabinPanel(_cabins[i]); return; }
                }
        }

        private void PnlMap_MouseMove(object sender, MouseEventArgs e)
        {
            bool zh = ZoneButtonRect.Contains(e.Location);
            bool ch = CabinButtonRect.Contains(e.Location);
            if (zh == _zoneHover && ch == _cabinHover) return; // nothing changed

            _zoneHover = zh;
            _cabinHover = ch;
            pnlMapContainer.Cursor = (zh || ch) ? Cursors.Hand : Cursors.Default;

            // ── Only repaint the two small button rectangles — NOT the whole map ──
            pnlMapContainer.Invalidate(ZoneButtonRectPadded);
            pnlMapContainer.Invalidate(CabinButtonRectPadded);
        }

        private void PnlMap_MouseLeave(object sender, EventArgs e)
        {
            if (!_zoneHover && !_cabinHover) return;
            _zoneHover = _cabinHover = false;
            pnlMapContainer.Invalidate(ZoneButtonRectPadded);
            pnlMapContainer.Invalidate(CabinButtonRectPadded);
        }

        private static double Dist(int x1, int y1, int x2, int y2)
            => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        // ════════════════════════════════════════════════════════════
        //  ZONE DATA
        // ════════════════════════════════════════════════════════════
        private void BuildZoneData()
        {
            _zones = new List<ZoneInfo>
            {
                new ZoneInfo { Id="savanna", Name="Golden Savanna", Icon="🦁",
                    PinColor=Color.FromArgb(220,180,70,10), Status="open",
                    Desc="Sprawling open grasslands hosting Africa's most iconic megafauna. Home to luxury safari tents along the perimeter trail.",
                    Animals=new[]{"Lion","Giraffe","Zebra","Elephant","Wildebeest"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=20, SlotUsed=8, ExperienceName="Safari Drive",
                    Cabins=new[]{"Safari Tent Alpha — ₱2,800/night","Savanna Tent — ₱2,400/night","Safari Lodge Suite — ₱4,200/night"},
                    PosX=0.10f, PosY=0.13f },
                new ZoneInfo { Id="predator", Name="Predator Ridge", Icon="🐅",
                    PinColor=Color.FromArgb(220,170,30,30), Status="limited",
                    Desc="Fortified ridge habitat for apex predators. Strictly capacity-limited viewing windows for guest safety.",
                    Animals=new[]{"Bengal Tiger","Sun Bear","Leopard","Hyena","Crocodile"}, AnimalOk=new[]{true,true,true,false,true},
                    SlotCap=10, SlotUsed=8, ExperienceName="Predator Watch", Cabins=new string[]{},
                    PosX=0.32f, PosY=0.13f },
                new ZoneInfo { Id="aviary", Name="Aviary Dome", Icon="🦜",
                    PinColor=Color.FromArgb(220,10,100,170), Status="open",
                    Desc="Iconic glass geodesic dome housing 60+ free-flying species. Walk-through experience at canopy height.",
                    Animals=new[]{"Philippine Eagle","Hornbill","Cockatoo","Macaw","Pelican"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=25, SlotUsed=10, ExperienceName="Bird Walk", Cabins=new string[]{},
                    PosX=0.52f, PosY=0.11f },
                new ZoneInfo { Id="jungle", Name="Jungle Canopy", Icon="🌿",
                    PinColor=Color.FromArgb(220,20,130,45), Status="open",
                    Desc="Dense rainforest with elevated canopy walkways. Forest Cabins A & B nestled among the trees.",
                    Animals=new[]{"Spider Monkey","Macaque","Gibbon","Tarsier","Slow Loris"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=15, SlotUsed=6, ExperienceName="Canopy Walk",
                    Cabins=new[]{"Forest Cabin A — ₱3,500/night","Forest Cabin B — ₱3,500/night"},
                    PosX=0.50f, PosY=0.50f },
                new ZoneInfo { Id="aquatic", Name="Aquatic Zone", Icon="🦦",
                    PinColor=Color.FromArgb(220,10,70,175), Status="open",
                    Desc="Pristine lakefront waterways hosting aquatic megafauna. Sanctuary Villa & Lakeside Lodge sit at water's edge.",
                    Animals=new[]{"River Otter","Freshwater Croc","Pelican","Hippopotamus","Giant Catfish"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=12, SlotUsed=4, ExperienceName="Kayak Tour",
                    Cabins=new[]{"The Sanctuary Villa — ₱8,500/night","Lakeside Lodge — ₱6,500/night"},
                    PosX=0.80f, PosY=0.11f },
                new ZoneInfo { Id="nocturnal", Name="Nocturnal Trail", Icon="🦉",
                    PinColor=Color.FromArgb(220,30,30,100), Status="open",
                    Desc="Twilight habitat lit by bioluminescent pools. Accessible post-sunset. Treetop Treehouse overlooks the trail.",
                    Animals=new[]{"Philippine Owl","Tarsier","Giant Bat","Civet Cat","Flying Fox"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=15, SlotUsed=12, ExperienceName="Night Safari",
                    Cabins=new[]{"Treetop Treehouse — ₱4,500/night"},
                    PosX=0.77f, PosY=0.54f },
                new ZoneInfo { Id="reptile", Name="Reptile Zone", Icon="🐍",
                    PinColor=Color.FromArgb(220,30,120,50), Status="advisory",
                    Desc="Climate-controlled facility for Philippine and Asian reptile species. West wing currently under maintenance.",
                    Animals=new[]{"Philippine Croc","Burmese Python","Monitor Lizard","Giant Gecko","Sea Turtle"}, AnimalOk=new[]{true,false,true,true,true},
                    SlotCap=10, SlotUsed=6, ExperienceName="Reptile Tour", Cabins=new string[]{},
                    PosX=0.51f, PosY=0.70f },
                new ZoneInfo { Id="conservation", Name="Conservation Hub", Icon="🌱",
                    PinColor=Color.FromArgb(220,10,120,95), Status="open",
                    Desc="Flagship center for critically endangered Philippine species. Eco Bungalow located adjacent.",
                    Animals=new[]{"Philippine Eagle","Tamaraw","Visayan Warty Pig","Palawan Bearcat","Dugong"}, AnimalOk=new[]{true,true,true,true,true},
                    SlotCap=8, SlotUsed=2, ExperienceName="Keeper Experience",
                    Cabins=new[]{"Eco Bungalow — ₱2,500/night"},
                    PosX=0.46f, PosY=0.84f }
            };
        }

        // ════════════════════════════════════════════════════════════
        //  CABIN DATA
        // ════════════════════════════════════════════════════════════
        private class CabinInfo
        {
            public string Name, Icon, Price;
            public float PosX, PosY;
            public string ZoneId;
        }

        private void BuildCabinData()
        {
            _cabins = new List<CabinInfo>
            {
                new CabinInfo { Name="Safari Tent Alpha",   Icon="🏕", Price="₱2,800/night", PosX=0.09f, PosY=0.40f, ZoneId="savanna"      },
                new CabinInfo { Name="Safari Lodge Suite",  Icon="🏠", Price="₱4,200/night", PosX=0.22f, PosY=0.46f, ZoneId="savanna"      },
                new CabinInfo { Name="Savanna Tent",        Icon="🏕", Price="₱2,400/night", PosX=0.08f, PosY=0.53f, ZoneId="savanna"      },
                new CabinInfo { Name="Forest Cabin A",      Icon="🏡", Price="₱3,500/night", PosX=0.45f, PosY=0.41f, ZoneId="jungle"       },
                new CabinInfo { Name="Forest Cabin B",      Icon="🏡", Price="₱3,500/night", PosX=0.45f, PosY=0.47f, ZoneId="jungle"       },
                new CabinInfo { Name="The Sanctuary Villa", Icon="🏠", Price="₱8,500/night", PosX=0.83f, PosY=0.31f, ZoneId="aquatic"      },
                new CabinInfo { Name="Lakeside Lodge",      Icon="🏠", Price="₱6,500/night", PosX=0.83f, PosY=0.50f, ZoneId="aquatic"      },
                new CabinInfo { Name="Treetop Treehouse",   Icon="🌳", Price="₱4,500/night", PosX=0.83f, PosY=0.67f, ZoneId="nocturnal"    },
                new CabinInfo { Name="Eco Bungalow",        Icon="🌿", Price="₱2,500/night", PosX=0.85f, PosY=0.82f, ZoneId="conservation" },
                new CabinInfo { Name="Savanna Base Camp",   Icon="🏕", Price="₱2,200/night", PosX=0.11f, PosY=0.82f, ZoneId="savanna"      },
            };
        }

        // ════════════════════════════════════════════════════════════
        //  DRAW ZONE PINS
        // ════════════════════════════════════════════════════════════
        private void DrawZonePins(Graphics g, int W, int H)
        {
            const int R = 32, D = R * 2;
            const float LW = 130f, LH = 24f;

            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                int cx = (int)(W * z.PosX), cy = (int)(H * z.PosY);
                Color gc = z.PinColor;

                for (int layer = 4; layer >= 1; layer--)
                {
                    int lr = R + 6 + layer * 4;
                    using (var gb = new SolidBrush(Color.FromArgb(Math.Min(18 * layer, 200), gc.R, gc.G, gc.B)))
                        g.FillEllipse(gb, cx - lr, cy - lr, lr * 2, lr * 2);
                }
                using (var sb = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                    g.FillEllipse(sb, cx - R + 4, cy - R + 6, D, D);
                using (var b = new SolidBrush(gc))
                    g.FillEllipse(b, cx - R, cy - R, D, D);
                using (var p = new Pen(Color.FromArgb(230, 255, 255, 255), 2.5f))
                    g.DrawEllipse(p, cx - R, cy - R, D, D);
                using (var p2 = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                    g.DrawEllipse(p2, cx - R + 5, cy - R + 5, D - 10, D - 10);

                using (var clip = new GraphicsPath())
                {
                    clip.AddEllipse(cx - R + 1, cy - R + 1, D - 2, D - 2);
                    g.SetClip(clip);
                }
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var fnt = new Font("Segoe UI Emoji", 20f))
                    g.DrawString(z.Icon, fnt, Brushes.White, new RectangleF(cx - R, cy - R, D, D), sf);
                g.ResetClip();

                Color dotColor = z.Status == "open" ? Color.FromArgb(55, 230, 85)
                               : z.Status == "limited" ? Color.FromArgb(225, 175, 25)
                                                       : Color.FromArgb(230, 60, 60);
                int dotX = cx + R - 12, dotY = cy - R - 2;
                using (var db = new SolidBrush(dotColor)) g.FillEllipse(db, dotX, dotY, 11, 11);
                using (var dp = new Pen(Color.FromArgb(8, 28, 14), 2f)) g.DrawEllipse(dp, dotX, dotY, 11, 11);
                using (var db2 = new SolidBrush(Color.FromArgb(170, 255, 255, 255))) g.FillEllipse(db2, dotX + 2, dotY + 2, 4, 4);

                float lx = cx - LW / 2f, ly = cy + R + 6f;
                using (var lb = new SolidBrush(Color.FromArgb(215, 4, 16, 8)))
                using (var lbP = RoundedRectF(new RectangleF(lx, ly, LW, LH), 8))
                    g.FillPath(lb, lbP);
                using (var border = new Pen(Color.FromArgb(80, 212, 160, 23), 1f))
                using (var lbP2 = RoundedRectF(new RectangleF(lx, ly, LW, LH), 8))
                    g.DrawPath(border, lbP2);
                using (var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.None })
                using (var lf = new Font("Bahnschrift SemiLight", 9.5f, FontStyle.Bold))
                using (var lbr = new SolidBrush(Color.FromArgb(255, 248, 235)))
                    g.DrawString(z.Name, lf, lbr, new RectangleF(lx, ly, LW, LH), sf2);
            }
        }

        // ════════════════════════════════════════════════════════════
        //  DRAW CABIN PINS
        // ════════════════════════════════════════════════════════════
        private void DrawCabinPins(Graphics g, int W, int H)
        {
            const int SZ = 38;
            const float LW = 120f, LH = 20f;

            for (int i = 0; i < _cabins.Count; i++)
            {
                var c = _cabins[i];
                int cx = (int)(W * c.PosX), cy = (int)(H * c.PosY);
                float x = cx - SZ / 2f, y = cy - SZ / 2f;

                using (var sb = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                using (var sp = RoundedRectF(new RectangleF(x + 2, y + 3, SZ, SZ), 8))
                    g.FillPath(sb, sp);
                using (var bb = new SolidBrush(Color.FromArgb(205, 195, 140, 22)))
                using (var bp = RoundedRectF(new RectangleF(x, y, SZ, SZ), 8))
                    g.FillPath(bb, bp);
                using (var pen = new Pen(Color.FromArgb(180, 255, 225, 100), 1.5f))
                using (var bd = RoundedRectF(new RectangleF(x + 1, y + 1, SZ - 2, SZ - 2), 7))
                    g.DrawPath(pen, bd);

                using (var clip = new GraphicsPath())
                {
                    clip.AddRectangle(new RectangleF(x, y, SZ, SZ));
                    g.SetClip(clip);
                }
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var fnt = new Font("Segoe UI Emoji", 16f))
                    g.DrawString(c.Icon, fnt, Brushes.White, new RectangleF(x, y, SZ, SZ), sf);
                g.ResetClip();

                float lx = cx - LW / 2f, ly = cy + SZ / 2f + 3;
                using (var lb = new SolidBrush(Color.FromArgb(195, 4, 16, 8)))
                using (var lbP = RoundedRectF(new RectangleF(lx, ly, LW, LH), 5))
                    g.FillPath(lb, lbP);
                using (var border = new Pen(Color.FromArgb(70, 212, 160, 23), 1f))
                using (var lbP2 = RoundedRectF(new RectangleF(lx, ly, LW, LH), 5))
                    g.DrawPath(border, lbP2);
                using (var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.None })
                using (var lf = new Font("Bahnschrift SemiLight", 8f, FontStyle.Bold))
                using (var lbr = new SolidBrush(Color.FromArgb(255, 248, 235)))
                    g.DrawString(c.Name, lf, lbr, new RectangleF(lx, ly, LW, LH), sf2);
            }
        }

        // ════════════════════════════════════════════════════════════
        //  SIDE PANEL SETUP
        // ════════════════════════════════════════════════════════════
        private void SetupSidePanel()
        {
            _sidePanel = new Panel { Width = 300, BackColor = Color.FromArgb(252, 7, 26, 14), Visible = false, AutoScroll = true };
            this.Controls.Add(_sidePanel);
            _sidePanel.BringToFront();
        }

        private void RepositionSidePanel()
        {
            if (_sidePanel == null) return;
            _sidePanel.Location = new Point(this.Width - 300, 85);
            _sidePanel.Height = pnlMapContainer.Height;
        }

        // ════════════════════════════════════════════════════════════
        //  SIDE PANEL — ZONE
        // ════════════════════════════════════════════════════════════
        private void OpenZonePanel(ZoneInfo z)
        {
            _sidePanel.Controls.Clear();
            _sidePanel.Visible = true;
            RepositionSidePanel();
            _sidePanel.BringToFront();

            int W = _sidePanel.Width - (SystemInformation.VerticalScrollBarWidth + 4);
            int y = 0;

            Button btnX = MakeFlatButton("✕", 30, 30);
            btnX.Location = new Point(W - 36, 10);
            btnX.ForeColor = Color.FromArgb(200, 200, 200);
            btnX.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnX.Click += (s, e) => _sidePanel.Visible = false;
            _sidePanel.Controls.Add(btnX);

            Panel ph = new Panel { Location = new Point(0, 0), Width = W, Height = 168, BackColor = Color.FromArgb(10, 36, 18) };
            ph.Controls.Add(new Label { Text = "WILDLIFE ZONE  ·  WILDNEST RESORT", Font = new Font("Bahnschrift", 6.5f, FontStyle.Bold), ForeColor = Color.FromArgb(85, 185, 85), Location = new Point(14, 12), AutoSize = true, BackColor = Color.Transparent });
            ph.Controls.Add(new Label { Text = z.Icon, Font = new Font("Segoe UI Emoji", 28f), ForeColor = Color.White, Location = new Point(12, 32), Size = new Size(54, 48), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });
            ph.Controls.Add(new Label { Text = z.Name, Font = new Font("Bahnschrift SemiLight", 13f), ForeColor = Color.FromArgb(248, 244, 239), Location = new Point(74, 36), Size = new Size(W - 88, 26), BackColor = Color.Transparent });
            ph.Controls.Add(new Label { Text = z.Desc, Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(125, 185, 125), Location = new Point(14, 88), Size = new Size(W - 22, 52), BackColor = Color.Transparent });
            Color bgc = z.Status == "open" ? Color.FromArgb(22, 145, 55) : z.Status == "limited" ? Color.FromArgb(145, 90, 10) : Color.FromArgb(158, 35, 35);
            string statusText = z.Status == "open" ? "● Fully Open" : z.Status == "limited" ? "● Limited Capacity" : "⚠ Health Advisory";
            ph.Controls.Add(new Label { Text = statusText, Font = new Font("Bahnschrift", 7f, FontStyle.Bold), ForeColor = Color.White, BackColor = bgc, AutoSize = true, Padding = new Padding(7, 3, 7, 3), Location = new Point(14, 148) });
            _sidePanel.Controls.Add(ph);
            y = 176;

            AddDivider(ref y, W); AddSectionLabel("ZONE INFO", ref y);
            string[,] gr = { { "🐾", "ANIMALS", z.Animals.Length + " species" }, { "👥", "CAPACITY", z.SlotCap + " guests" }, { "🎟", "EXPERIENCES", "1 active" }, { "📍", "TRAM STOP", "Yes" } };
            int gx = 12, gy = y, cW = (W - 28) / 2, cH = 70;
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                Panel cell = new Panel { Location = new Point(gx + col * (cW + 4), gy + row * (cH + 6)), Size = new Size(cW, cH), BackColor = Color.FromArgb(15, 45, 22) };
                cell.Paint += (s, ev) => { ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(cell.BackColor)) using (var p2 = RoundedRect(new Rectangle(0, 0, cell.Width - 1, cell.Height - 1), 6)) ev.Graphics.FillPath(b, p2); };
                cell.Controls.Add(new Label { Text = gr[i, 0], Font = new Font("Segoe UI Emoji", 15f), ForeColor = Color.White, Location = new Point(8, 8), Size = new Size(30, 28), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });
                cell.Controls.Add(new Label { Text = gr[i, 1], Font = new Font("Bahnschrift", 6f, FontStyle.Bold), ForeColor = Color.FromArgb(85, 155, 85), Location = new Point(42, 10), Size = new Size(cW - 46, 14), BackColor = Color.Transparent });
                cell.Controls.Add(new Label { Text = gr[i, 2], Font = new Font("Bahnschrift SemiLight", 9f), ForeColor = Color.FromArgb(248, 244, 239), Location = new Point(42, 26), Size = new Size(cW - 46, 18), BackColor = Color.Transparent });
                _sidePanel.Controls.Add(cell);
            }
            y = gy + cH * 2 + 6 + 18;

            AddDivider(ref y, W); AddSectionLabel("RESIDENT ANIMALS", ref y);
            FlowLayoutPanel flow = new FlowLayoutPanel { Location = new Point(12, y), Size = new Size(W - 14, 80), BackColor = Color.Transparent, WrapContents = true, AutoScroll = false };
            for (int i = 0; i < z.Animals.Length; i++)
            {
                bool ok = z.AnimalOk[i];
                flow.Controls.Add(new Label { Text = (ok ? "✓ " : "⚠ ") + z.Animals[i], Font = new Font("Segoe UI", 7.5f), ForeColor = ok ? Color.FromArgb(160, 235, 160) : Color.FromArgb(235, 155, 50), BackColor = ok ? Color.FromArgb(18, 58, 28) : Color.FromArgb(58, 38, 8), AutoSize = true, Padding = new Padding(5, 3, 5, 3), Margin = new Padding(0, 0, 4, 4) });
            }
            _sidePanel.Controls.Add(flow);
            y += 88;

            AddDivider(ref y, W); AddSectionLabel("EXPERIENCE SLOTS", ref y);
            int pct = (int)((float)z.SlotUsed / z.SlotCap * 100);
            Color barC = pct >= 80 ? Color.FromArgb(200, 55, 55) : pct >= 50 ? Color.FromArgb(212, 160, 23) : Color.FromArgb(35, 170, 75);
            Panel expRow = new Panel { Location = new Point(12, y), Size = new Size(W - 24, 20), BackColor = Color.Transparent };
            expRow.Controls.Add(new Label { Text = z.ExperienceName, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(220, 220, 220), Location = new Point(0, 2), AutoSize = true, BackColor = Color.Transparent });
            expRow.Controls.Add(new Label { Text = (z.SlotCap - z.SlotUsed) + " / " + z.SlotCap + " slots", Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(150, 150, 150), Location = new Point(W - 100, 3), Size = new Size(88, 16), TextAlign = ContentAlignment.TopRight, BackColor = Color.Transparent });
            _sidePanel.Controls.Add(expRow);
            y += 22;
            AddProgressBar(ref y, W, pct, barC);

            if (z.Cabins.Length > 0)
            {
                AddDivider(ref y, W); AddSectionLabel("NEARBY CABINS", ref y);
                foreach (var cab in z.Cabins)
                {
                    _sidePanel.Controls.Add(new Label { Text = "🏠  " + cab, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(212, 160, 23), Location = new Point(14, y), Size = new Size(W - 20, 20), BackColor = Color.Transparent });
                    y += 24;
                }
            }
            y += 14;
            AddBookButton(ref y, W, z.Name);

            y += 4;
            Button btnClose = MakeFlatButton("Close Panel", W - 24, 34);
            btnClose.Location = new Point(12, y);
            btnClose.ForeColor = Color.FromArgb(150, 200, 150);
            btnClose.Font = new Font("Segoe UI", 8);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(40, 82, 45);
            btnClose.Click += (s, e) => _sidePanel.Visible = false;
            _sidePanel.Controls.Add(btnClose);
            _sidePanel.BringToFront();
        }

        // ════════════════════════════════════════════════════════════
        //  SIDE PANEL — CABIN
        // ════════════════════════════════════════════════════════════
        private void OpenCabinPanel(CabinInfo c)
        {
            _sidePanel.Controls.Clear();
            _sidePanel.Visible = true;
            RepositionSidePanel();
            _sidePanel.BringToFront();

            int W = _sidePanel.Width - (SystemInformation.VerticalScrollBarWidth + 4);
            int y = 0;

            Button btnX = MakeFlatButton("✕", 30, 30);
            btnX.Location = new Point(W - 36, 10);
            btnX.ForeColor = Color.FromArgb(200, 200, 200);
            btnX.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnX.Click += (s, e) => _sidePanel.Visible = false;
            _sidePanel.Controls.Add(btnX);

            Panel ph = new Panel { Location = new Point(0, 0), Width = W, Height = 152, BackColor = Color.FromArgb(10, 36, 18) };
            ph.Controls.Add(new Label { Text = "ACCOMMODATION  ·  WILDNEST RESORT", Font = new Font("Bahnschrift", 6.5f, FontStyle.Bold), ForeColor = Color.FromArgb(212, 160, 23), Location = new Point(14, 12), AutoSize = true, BackColor = Color.Transparent });
            ph.Controls.Add(new Label { Text = c.Icon, Font = new Font("Segoe UI Emoji", 26f), ForeColor = Color.White, Location = new Point(12, 32), Size = new Size(50, 44), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });
            ph.Controls.Add(new Label { Text = c.Name, Font = new Font("Bahnschrift SemiLight", 12f), ForeColor = Color.FromArgb(248, 244, 239), Location = new Point(68, 34), Size = new Size(W - 82, 24), BackColor = Color.Transparent });
            ph.Controls.Add(new Label { Text = c.Price, Font = new Font("Bahnschrift SemiLight", 11f), ForeColor = Color.FromArgb(212, 160, 23), Location = new Point(68, 60), AutoSize = true, BackColor = Color.Transparent });
            ph.Controls.Add(new Label { Text = "Fully Available  ●", Font = new Font("Bahnschrift", 7f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(22, 145, 55), AutoSize = true, Padding = new Padding(7, 3, 7, 3), Location = new Point(14, 128) });
            _sidePanel.Controls.Add(ph);
            y = 160;

            AddDivider(ref y, W); AddSectionLabel("CABIN INFO", ref y);
            string[,] gr = { { "🛏", "BEDROOMS", "1-2 beds" }, { "🚿", "BATHROOM", "Private ensuite" }, { "🌲", "SETTING", "Wilderness view" }, { "📶", "AMENITIES", "WiFi · AC · Minibar" } };
            int gx = 12, gy = y, cW = (W - 28) / 2, cH = 70;
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                Panel cell = new Panel { Location = new Point(gx + col * (cW + 4), gy + row * (cH + 6)), Size = new Size(cW, cH), BackColor = Color.FromArgb(15, 45, 22) };
                cell.Paint += (s, ev) => { ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(cell.BackColor)) using (var p2 = RoundedRect(new Rectangle(0, 0, cell.Width - 1, cell.Height - 1), 6)) ev.Graphics.FillPath(b, p2); };
                cell.Controls.Add(new Label { Text = gr[i, 0], Font = new Font("Segoe UI Emoji", 15f), ForeColor = Color.White, Location = new Point(8, 8), Size = new Size(30, 28), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });
                cell.Controls.Add(new Label { Text = gr[i, 1], Font = new Font("Bahnschrift", 6f, FontStyle.Bold), ForeColor = Color.FromArgb(85, 155, 85), Location = new Point(42, 10), Size = new Size(cW - 46, 14), BackColor = Color.Transparent });
                cell.Controls.Add(new Label { Text = gr[i, 2], Font = new Font("Bahnschrift SemiLight", 8.5f), ForeColor = Color.FromArgb(248, 244, 239), Location = new Point(42, 26), Size = new Size(cW - 46, 18), BackColor = Color.Transparent });
                _sidePanel.Controls.Add(cell);
            }
            y = gy + cH * 2 + 6 + 18;

            AddDivider(ref y, W); AddSectionLabel("NEARBY WILDLIFE ZONE", ref y);
            ZoneInfo? linkedZone = _zones.Find(z2 => z2.Id == c.ZoneId);
            if (linkedZone != null)
            {
                Panel zoneRef = new Panel { Location = new Point(12, y), Size = new Size(W - 24, 54), BackColor = Color.FromArgb(15, 45, 22) };
                zoneRef.Paint += (s, ev) => { ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(zoneRef.BackColor)) using (var p2 = RoundedRect(new Rectangle(0, 0, zoneRef.Width - 1, zoneRef.Height - 1), 8)) ev.Graphics.FillPath(b, p2); };
                zoneRef.Controls.Add(new Label { Text = linkedZone.Icon, Font = new Font("Segoe UI Emoji", 18f), ForeColor = Color.White, Location = new Point(8, 8), Size = new Size(34, 34), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });
                zoneRef.Controls.Add(new Label { Text = linkedZone.Name, Font = new Font("Bahnschrift SemiLight", 10f), ForeColor = Color.FromArgb(248, 244, 239), Location = new Point(48, 10), Size = new Size(W - 70, 18), BackColor = Color.Transparent });
                Color sc = linkedZone.Status == "open" ? Color.FromArgb(55, 230, 85) : linkedZone.Status == "limited" ? Color.FromArgb(225, 175, 25) : Color.FromArgb(230, 60, 60);
                string st = linkedZone.Status == "open" ? "● Open" : linkedZone.Status == "limited" ? "● Limited" : "⚠ Advisory";
                zoneRef.Controls.Add(new Label { Text = st, Font = new Font("Bahnschrift", 7f, FontStyle.Bold), ForeColor = sc, Location = new Point(48, 30), AutoSize = true, BackColor = Color.Transparent });
                _sidePanel.Controls.Add(zoneRef);
                y += 62;
            }

            AddDivider(ref y, W); AddSectionLabel("PRICING", ref y);
            string[,] pricing = { { "Per Night", c.Price }, { "Weekend Rate", c.Price.Split('/')[0] + "+10%/night" }, { "Weekly Rate", "7th night free" } };
            for (int i = 0; i < 3; i++)
            {
                Panel row2 = new Panel { Location = new Point(12, y), Size = new Size(W - 24, 24), BackColor = Color.Transparent };
                row2.Controls.Add(new Label { Text = pricing[i, 0], Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(140, 185, 140), Location = new Point(0, 4), AutoSize = true, BackColor = Color.Transparent });
                row2.Controls.Add(new Label { Text = pricing[i, 1], Font = new Font("Bahnschrift SemiLight", 9f), ForeColor = Color.FromArgb(212, 160, 23), Location = new Point(W - 130, 3), Size = new Size(110, 20), TextAlign = ContentAlignment.TopRight, BackColor = Color.Transparent });
                _sidePanel.Controls.Add(row2);
                y += 26;
            }
            y += 14;
            AddBookButton(ref y, W, c.Name);

            y += 4;
            Button btnClose = MakeFlatButton("Close Panel", W - 24, 34);
            btnClose.Location = new Point(12, y);
            btnClose.ForeColor = Color.FromArgb(150, 200, 150);
            btnClose.Font = new Font("Segoe UI", 8);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(40, 82, 45);
            btnClose.Click += (s, e) => _sidePanel.Visible = false;
            _sidePanel.Controls.Add(btnClose);
            _sidePanel.BringToFront();
        }

        // ════════════════════════════════════════════════════════════
        //  PANEL HELPERS
        // ════════════════════════════════════════════════════════════
        private void AddSectionLabel(string t, ref int y)
        {
            _sidePanel?.Controls.Add(new Label { Text = t, Font = new Font("Bahnschrift", 7f, FontStyle.Bold), ForeColor = Color.FromArgb(82, 150, 82), Location = new Point(12, y), AutoSize = true, BackColor = Color.Transparent });
            y += 20;
        }

        private void AddDivider(ref int y, int W)
        {
            _sidePanel?.Controls.Add(new Panel { Location = new Point(0, y), Size = new Size(W, 1), BackColor = Color.FromArgb(40, 212, 160, 23) });
            y += 10;
        }

        private void AddProgressBar(ref int y, int W, int pct, Color barColor)
        {
            Panel track = new Panel { Location = new Point(12, y), Size = new Size(W - 24, 9), BackColor = Color.FromArgb(25, 58, 30) };
            int fw = Math.Max(0, (int)((W - 24) * pct / 100.0));
            Panel fill = new Panel { Location = new Point(0, 0), Size = new Size(fw, 9), BackColor = barColor };
            track.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(track.BackColor)) using (var ph = RoundedRect(new Rectangle(0, 0, track.Width - 1, 8), 4)) e.Graphics.FillPath(b, ph); };
            fill.Paint += (s, e) => { if (fill.Width < 4) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var b = new SolidBrush(fill.BackColor)) using (var ph = RoundedRect(new Rectangle(0, 0, fill.Width - 1, 8), 4)) e.Graphics.FillPath(b, ph); };
            track.Controls.Add(fill);
            _sidePanel?.Controls.Add(track);
            y += 20;
        }

        private void AddBookButton(ref int y, int W, string subjectName)
        {
            Button btn = new Button { Text = "🎟  Book Now — " + subjectName, Size = new Size(W - 24, 46), Location = new Point(12, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(212, 160, 23), ForeColor = Color.FromArgb(7, 26, 14), Font = new Font("Bahnschrift SemiLight", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 130, 10);
            btn.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(btn.BackColor))
                using (var path = RoundedRect(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 10))
                    e.Graphics.FillPath(b, path);
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var br = new SolidBrush(btn.ForeColor))
                    e.Graphics.DrawString(btn.Text, btn.Font, br, new RectangleF(0, 0, btn.Width, btn.Height), sf);
            };
            btn.Click += (s, e) => NavigateToBookNowDayVisit();
            _sidePanel.Controls.Add(btn);
            y += 56;
        }

        private void NavigateToBookNowDayVisit()
        {
            Form? mainForm = this.FindForm();
            if (mainForm == null) return;
            Panel? pnlMain = mainForm.Controls.Find("pnlMain", true).FirstOrDefault() as Panel;
            if (pnlMain == null) return;
            pnlMain.Controls.Clear();
            BookNow bookNow = new BookNow();
            bookNow.Dock = DockStyle.Fill;
            pnlMain.Controls.Add(bookNow);
            bookNow.OpenDayVisit();
        }

        private Button MakeFlatButton(string text, int w, int h)
        {
            var btn = new Button { Text = text, Size = new Size(w, h), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static Label Lbl(string text, string font, float size, FontStyle style, Color fore, Point loc, bool autoSize, Size? fixedSize = null)
        {
            var l = new Label { Text = text, Font = new Font(font, size, style), ForeColor = fore, Location = loc, BackColor = Color.Transparent };
            if (autoSize) l.AutoSize = true; else if (fixedSize.HasValue) l.Size = fixedSize.Value;
            return l;
        }

        // ════════════════════════════════════════════════════════════
        //  HEADER & FOOTER
        // ════════════════════════════════════════════════════════════
        private void SetupStatusHeader()
        {
            Panel p = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = Color.FromArgb(230, 9, 24, 16) };
            p.Paint += (s, e) =>
            {
                using var lg = new LinearGradientBrush(
                    new Rectangle(0, 0, p.Width, p.Height),
                    Color.FromArgb(11, 36, 22),
                    Color.FromArgb(7, 26, 14),
                    90f);
                e.Graphics.FillRectangle(lg, p.ClientRectangle);
                using var glow = new LinearGradientBrush(
                    new Rectangle(0, 0, p.Width, p.Height),
                    Color.FromArgb(24, 212, 160, 23),
                    Color.Transparent,
                    0f);
                e.Graphics.FillRectangle(glow, p.ClientRectangle);
            };
            p.Controls.AddRange(new Control[]{
                new Label{Text="WILDNEST LIVE HABITAT MAP",Font=new Font("Georgia",16,FontStyle.Bold),ForeColor=Color.FromArgb(248,244,239),Location=new Point(36,14),AutoSize=true,BackColor=Color.Transparent},
                new Label{Text="Real-time view of cabins, wildlife zones, and guest wayfinding across Carmen, Cebu",Font=new Font("Segoe UI",9,FontStyle.Regular),ForeColor=Color.FromArgb(190,248,244,239),Location=new Point(38,44),AutoSize=true,BackColor=Color.Transparent},
                new Panel{Height=2,BackColor=Color.FromArgb(120,212,160,23),Dock=DockStyle.Bottom}
            });
            this.Controls.Add(p); p.BringToFront();
        }

        private void SetupFooter()
        {
            Panel p = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(230, 6, 18, 12) };
            p.Controls.AddRange(new Control[]{
                new Label{Text="— END OF WILDNEST GROUNDS —",Font=new Font("Bahnschrift SemiLight",9),ForeColor=Color.FromArgb(212,160,23),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter},
                new Panel{Height=2,Dock=DockStyle.Top,BackColor=Color.FromArgb(180,212,160,23)}
            });
            this.Controls.Add(p); p.BringToFront();
        }

        // ════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2; var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        private static GraphicsPath RoundedRectF(RectangleF r, float radius)
        {
            float d = radius * 2; var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }
    }
}
