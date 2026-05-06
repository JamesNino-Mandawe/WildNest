using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Project
{
    internal enum HeroSurfaceVariant
    {
        Home,
        Cabins,
        Experiences,
        Animals,
        Visit,
        About
    }

    internal static class HeroSurfacePainter
    {
        public static void Paint(Graphics g, Rectangle rect, HeroSurfaceVariant variant)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            GetPalette(variant, out Color c1, out Color c2, out Color accentA, out Color accentB);

            using (var bg = new LinearGradientBrush(rect, c1, c2, 135f))
                g.FillRectangle(bg, rect);

            using (var topWash = new LinearGradientBrush(
                new Rectangle(rect.X, rect.Y, rect.Width, Math.Max(1, rect.Height / 2)),
                Color.FromArgb(54, Color.FromArgb(31, 84, 54)),
                Color.Transparent, 90f))
            {
                g.FillRectangle(topWash, rect.X, rect.Y, rect.Width, Math.Max(1, rect.Height / 2));
            }

            DrawAccentGlow(g, rect, accentA, accentB, variant);
            DrawDiagonalTexture(g, rect);
            DrawFrameOrnaments(g, rect);
            DrawDepthBands(g, rect);
            DrawGoldStructure(g, rect, variant);
            DrawBottomRhythm(g, rect);
        }

        private static void GetPalette(HeroSurfaceVariant variant, out Color c1, out Color c2, out Color accentA, out Color accentB)
        {
            c1 = Color.FromArgb(11, 39, 22);
            c2 = Color.FromArgb(7, 26, 14);
            accentA = Color.FromArgb(42, 212, 160, 23);
            accentB = Color.FromArgb(22, 248, 215, 120);

            switch (variant)
            {
                case HeroSurfaceVariant.Cabins:
                    c1 = Color.FromArgb(13, 44, 25);
                    c2 = Color.FromArgb(8, 28, 16);
                    accentA = Color.FromArgb(44, 212, 160, 23);
                    accentB = Color.FromArgb(26, 234, 193, 92);
                    break;
                case HeroSurfaceVariant.Experiences:
                    c1 = Color.FromArgb(12, 40, 23);
                    c2 = Color.FromArgb(8, 25, 16);
                    accentA = Color.FromArgb(34, 173, 117, 255);
                    accentB = Color.FromArgb(30, 212, 160, 23);
                    break;
                case HeroSurfaceVariant.Animals:
                    c1 = Color.FromArgb(10, 36, 20);
                    c2 = Color.FromArgb(6, 22, 12);
                    accentA = Color.FromArgb(42, 212, 160, 23);
                    accentB = Color.FromArgb(16, 120, 206, 99);
                    break;
                case HeroSurfaceVariant.Visit:
                    c1 = Color.FromArgb(12, 40, 23);
                    c2 = Color.FromArgb(7, 26, 14);
                    accentA = Color.FromArgb(44, 212, 160, 23);
                    accentB = Color.FromArgb(22, 95, 196, 245);
                    break;
                case HeroSurfaceVariant.About:
                    c1 = Color.FromArgb(12, 38, 22);
                    c2 = Color.FromArgb(7, 26, 14);
                    accentA = Color.FromArgb(38, 212, 160, 23);
                    accentB = Color.FromArgb(20, 248, 244, 239);
                    break;
            }
        }

        private static void DrawAccentGlow(Graphics g, Rectangle rect, Color accentA, Color accentB, HeroSurfaceVariant variant)
        {
            int heroW = rect.Width;
            int heroH = rect.Height;

            using (var gp = new GraphicsPath())
            {
                int w = (int)(heroW * 0.42f);
                int h = Math.Max(180, (int)(heroH * 0.75f));
                int x = variant == HeroSurfaceVariant.Home || variant == HeroSurfaceVariant.Visit
                    ? (int)(heroW * 0.62f)
                    : (int)(heroW * 0.54f);
                gp.AddEllipse(new Rectangle(x, -heroH / 5, w, h));
                using var pgb = new PathGradientBrush(gp);
                pgb.CenterColor = accentA;
                pgb.SurroundColors = new[] { Color.Transparent };
                g.FillPath(pgb, gp);
            }

            using (var gp = new GraphicsPath())
            {
                int w = (int)(heroW * 0.30f);
                int h = (int)(heroH * 0.48f);
                int x = (int)(heroW * 0.18f);
                int y = (int)(heroH * 0.32f);
                gp.AddEllipse(new Rectangle(x - w / 2, y - h / 2, w, h));
                using var pgb = new PathGradientBrush(gp);
                pgb.CenterColor = accentB;
                pgb.SurroundColors = new[] { Color.Transparent };
                g.FillPath(pgb, gp);
            }
        }

        private static void DrawDepthBands(Graphics g, Rectangle rect)
        {
            using var pen = new Pen(Color.FromArgb(16, 212, 160, 23), 1f);
            int bands = Math.Clamp(rect.Height / 180, 1, 2);
            for (int i = 0; i < bands; i++)
            {
                int y = rect.Height - 84 - (i * 44);
                g.DrawBezier(pen,
                    new Point(-40, y),
                    new Point(rect.Width / 4, y - 6),
                    new Point((int)(rect.Width * 0.68f), y + 10),
                    new Point(rect.Width + 60, y - 4));
            }
        }

        private static void DrawDiagonalTexture(Graphics g, Rectangle rect)
        {
            using var texturePen = new Pen(Color.FromArgb(4, 255, 255, 255), 1f);
            for (int x = -rect.Height; x < rect.Width + rect.Height; x += 44)
                g.DrawLine(texturePen, x, 0, x + rect.Height, rect.Height);
        }

        private static void DrawFrameOrnaments(Graphics g, Rectangle rect)
        {
            using var softPen = new Pen(Color.FromArgb(98, 212, 160, 23), 1.35f);
            softPen.StartCap = LineCap.Round;
            softPen.EndCap = LineCap.Round;

            int inset = 18;
            int len = 52;

            g.DrawLine(softPen, inset, inset, inset + len, inset);
            g.DrawLine(softPen, inset, inset, inset, inset + len / 2);
            g.DrawLine(softPen, rect.Width - inset, inset, rect.Width - inset - len, inset);
            g.DrawLine(softPen, rect.Width - inset, inset, rect.Width - inset, inset + len / 2);

            using var microPen = new Pen(Color.FromArgb(34, 248, 215, 120), 1f);
            g.DrawLine(microPen, inset + 66, inset, inset + 112, inset);
            g.DrawLine(microPen, rect.Width - inset - 66, inset, rect.Width - inset - 112, inset);
        }

        private static void DrawGoldStructure(Graphics g, Rectangle rect, HeroSurfaceVariant variant)
        {
            using var soft = new Pen(Color.FromArgb(26, 212, 160, 23), 1f);
            using var crisp = new Pen(Color.FromArgb(68, 212, 160, 23), 1.1f);
            int center = rect.Width / 2;
            int topY = variant switch
            {
                HeroSurfaceVariant.Home => 98,
                HeroSurfaceVariant.Cabins => 100,
                HeroSurfaceVariant.Experiences => 100,
                HeroSurfaceVariant.Animals => 110,
                HeroSurfaceVariant.Visit => 126,
                HeroSurfaceVariant.About => 126,
                _ => 104
            };
            int bottomY = rect.Height - 30;

            g.DrawLine(soft, center - 160, topY, center - 34, topY);
            g.DrawLine(soft, center + 34, topY, center + 160, topY);

            g.DrawLine(crisp, center - 112, bottomY, center - 28, bottomY);
            g.DrawLine(crisp, center + 28, bottomY, center + 112, bottomY);
            DrawDiamond(g, center - 16, bottomY, Color.FromArgb(160, 212, 160, 23));
            DrawDiamond(g, center + 16, bottomY, Color.FromArgb(160, 212, 160, 23));
        }

        private static void DrawBottomRhythm(Graphics g, Rectangle rect)
        {
            int y = rect.Height - 1;
            using var linePen = new Pen(Color.FromArgb(170, 212, 160, 23), 1.5f);
            g.DrawLine(linePen, 0, y, rect.Width, y);

            using var segPen = new Pen(Color.FromArgb(56, 248, 215, 120), 1f);
            int segment = 86;

            for (int x = 28; x < rect.Width - 28; x += segment)
                g.DrawLine(segPen, x, y - 10, x + 30, y - 10);
        }

        private static void DrawDiamond(Graphics g, int cx, int cy, Color c)
        {
            using var b = new SolidBrush(c);
            g.FillPolygon(b, new[]
            {
                new PointF(cx, cy - 4),
                new PointF(cx + 4, cy),
                new PointF(cx, cy + 4),
                new PointF(cx - 4, cy)
            });
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
