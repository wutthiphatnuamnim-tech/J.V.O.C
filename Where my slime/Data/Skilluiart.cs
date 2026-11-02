using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // ภาพการ์ด Skill ตอนต่อสู้ ขนาด 280x200 ต่อฮีโร่ (โฟลเดอร์ Ui_Skill)
    //   icon_char_Skill_<ชื่อ>.png  = กรอบ (วงกลมพอร์เทรต + ช่องสกิล + หลอด)
    //   icon_char_Skill_<ชื่อ>1.png = ไอคอนสกิลที่ซ้อนทับบนช่องสกิล
    public static class SkillUiArt
    {
        public const int Width = 280;
        public const int Height = 200;

        // พิกัดบนภาพ 280x200
        public static readonly Vector2 CircleCenter = new Vector2(82f, 115f);
        public const float CircleRadius = 57f;                              // รัศมีพื้นที่พอร์เทรต (เว้นขอบทองไว้)
        public static readonly Rectangle SkillSlot = new Rectangle(161, 61, 101, 101);
        public static readonly Rectangle Bar = new Rectangle(33, 175, 227, 13);

        private static readonly Dictionary<string, (Texture2D frame, Texture2D icon)> _art = new Dictionary<string, (Texture2D, Texture2D)>();

        public static void Set(string heroName, Texture2D frame, Texture2D icon)
        {
            if (string.IsNullOrEmpty(heroName) || frame == null) return;
            _art[heroName] = (frame, icon);
        }

        private static readonly Dictionary<Texture2D, Rectangle> _cropCache = new Dictionary<Texture2D, Rectangle>();

        // วาดไอคอนสกิลของฮีโร่ (ภาพ ...1.png) ครอบเฉพาะส่วนที่มีพิกเซลแล้วย่อให้พอดีกับกรอบ (ใช้ในแผง "Ultimate Skill")
        public static bool TryDrawIcon(SpriteBatch sb, string heroName, Rectangle dest, float alpha = 1f)
        {
            if (!TryGet(heroName, out _, out var icon) || icon == null) return false;
            if (!_cropCache.TryGetValue(icon, out var crop))
            {
                crop = new Rectangle(0, 0, icon.Width, icon.Height);
                try
                {
                    var data = new Color[icon.Width * icon.Height];
                    icon.GetData(data);
                    int minX = icon.Width, minY = icon.Height, maxX = -1, maxY = -1;
                    for (int y = 0; y < icon.Height; y++)
                        for (int x = 0; x < icon.Width; x++)
                            if (data[y * icon.Width + x].A > 20)
                            {
                                if (x < minX) minX = x; if (x > maxX) maxX = x;
                                if (y < minY) minY = y; if (y > maxY) maxY = y;
                            }
                    if (maxX >= 0)
                    {
                        int side = System.Math.Max(maxX - minX + 1, maxY - minY + 1) + 8;
                        int cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
                        crop = new Rectangle(cx - side / 2, cy - side / 2, side, side);
                        crop = Rectangle.Intersect(crop, new Rectangle(0, 0, icon.Width, icon.Height));
                    }
                }
                catch { }
                _cropCache[icon] = crop;
            }
            // รักษาสัดส่วน: วางกึ่งกลางกรอบปลายทาง
            float scale = System.Math.Min(dest.Width / (float)crop.Width, dest.Height / (float)crop.Height);
            int w = (int)(crop.Width * scale), h = (int)(crop.Height * scale);
            sb.Draw(icon, new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h), crop, Color.White * alpha);
            return true;
        }

        public static bool TryGet(string heroName, out Texture2D frame, out Texture2D icon)
        {
            frame = null; icon = null;
            if (string.IsNullOrEmpty(heroName)) return false;
            foreach (var kv in _art)
            {
                if (heroName.IndexOf(kv.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    frame = kv.Value.frame; icon = kv.Value.icon;
                    return true;
                }
            }
            return false;
        }
    }
}