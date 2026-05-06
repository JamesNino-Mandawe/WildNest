using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Project
{
    internal static class AppAssetLoader
    {
        internal static Image? LoadImage(string resourceName, params string[] relativeFallbackParts)
        {
            try
            {
                object? embedded = Properties.Resources.ResourceManager.GetObject(resourceName);
                if (embedded is Image embeddedImage)
                    return (Image)embeddedImage.Clone();
            }
            catch
            {
                // Fall through to disk-based fallback.
            }

            if (relativeFallbackParts == null || relativeFallbackParts.Length == 0)
                return null;

            string? fallbackPath = ResolveExistingPath(relativeFallbackParts);
            if (string.IsNullOrWhiteSpace(fallbackPath))
                return null;

            try
            {
                using var stream = File.OpenRead(fallbackPath);
                using var raw = Image.FromStream(stream);
                return (Image)raw.Clone();
            }
            catch
            {
                return null;
            }
        }

        internal static Image? LoadAnimalPhoto(string photoFile)
        {
            string key = Path.GetFileNameWithoutExtension(photoFile);
            return LoadImage(key, "Resources", "Animals", photoFile);
        }

        internal static void DrawCoverImage(Graphics g, Image image, Rectangle bounds, float opacity = 1f)
        {
            if (g == null || image == null || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            float safeOpacity = Math.Max(0f, Math.Min(1f, opacity));
            Rectangle dest = GetCoverRectangle(image.Size, bounds);

            using var attrs = new ImageAttributes();
            var matrix = new ColorMatrix
            {
                Matrix33 = safeOpacity
            };
            attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(image, dest, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
        }

        private static Rectangle GetCoverRectangle(Size imageSize, Rectangle bounds)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0)
                return bounds;

            float scale = Math.Max(
                bounds.Width / (float)imageSize.Width,
                bounds.Height / (float)imageSize.Height);

            int drawW = (int)Math.Ceiling(imageSize.Width * scale);
            int drawH = (int)Math.Ceiling(imageSize.Height * scale);
            int drawX = bounds.X + (bounds.Width - drawW) / 2;
            int drawY = bounds.Y + (bounds.Height - drawH) / 2;
            return new Rectangle(drawX, drawY, drawW, drawH);
        }

        private static string? ResolveExistingPath(params string[] relativeFallbackParts)
        {
            string[] roots =
            {
                Application.StartupPath,
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.CurrentDirectory
            };

            foreach (string root in roots.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string? found = SearchUpwardForPath(root, relativeFallbackParts);
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }

            return null;
        }

        private static string? SearchUpwardForPath(string startDir, params string[] relativeFallbackParts)
        {
            try
            {
                DirectoryInfo? dir = new DirectoryInfo(startDir);
                for (int i = 0; dir != null && i < 8; i++, dir = dir.Parent)
                {
                    string candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeFallbackParts).ToArray());
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            catch
            {
                // Ignore bad path states and keep trying other roots.
            }

            return null;
        }
    }
}
