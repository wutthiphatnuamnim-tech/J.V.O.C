using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // รูปโปรไฟล์ฮีโร่ (Content/Profile/*_Profile.png) ใช้แสดงในการ์ดหน้า Team Setup / Upgrade
    public static class HeroProfiles
    {
        private static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();

        public static void Set(string heroKey, Texture2D tex)
        {
            if (tex != null) _tex[heroKey] = tex;
        }

        public static Texture2D Get(string heroName)
        {
            if (string.IsNullOrEmpty(heroName)) return null;
            string key = heroName == "Swordman" ? "Alice" : heroName;
            return _tex.TryGetValue(key, out var t) ? t : null;
        }

        // วาดรูปโปรไฟล์ลงกรอบ: เต็มความกว้าง ตัดจากด้านบนตามสัดส่วนกรอบ
        public static bool Draw(SpriteBatch sb, string heroName, Rectangle box, Color tint)
        {
            var tex = Get(heroName);
            if (tex == null || tex.Width <= 0 || tex.Height <= 0) return false;

            float aspect = box.Height / (float)box.Width;
            int srcH = System.Math.Min(tex.Height, (int)(tex.Width * aspect));
            if (srcH <= 0) return false;
            sb.Draw(tex, box, new Rectangle(0, 0, tex.Width, srcH), tint);
            return true;
        }
    }
}