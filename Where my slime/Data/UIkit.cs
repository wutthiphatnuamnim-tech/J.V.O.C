using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime
{
    // ปุ่มสไตล์กรอบหินกลางที่ทุกหน้าใช้ร่วมกัน (ให้หน้าตาปุ่ม Back/Start/Continue ฯลฯ เหมือนกันทั้งเกม)
    public static class UiKit
    {
        public static readonly Color Stone = new Color(120, 130, 165);
        public static readonly Color StoneDark = new Color(52, 58, 80);

        // ปุ่มทั่วไป: ไล่เฉดสีตาม accent, กรอบหินเทาน้ำเงิน (ทองตอนชี้), หมุดมุม, แสงเรืองตอนชี้
        public static void StoneButton(SpriteBatch sb, Texture2D px, Rectangle r, Color accent, bool hovered, int border = 3)
        {
            if (border < 3) border = 3;
            sb.Draw(px, new Rectangle(r.X + 3, r.Y + 5, r.Width, r.Height), Color.Black * 0.4f);
            if (hovered) sb.Draw(px, new Rectangle(r.X - 5, r.Y - 5, r.Width + 10, r.Height + 10), accent * 0.22f);

            Color top = Color.Lerp(accent, Color.White, hovered ? 0.30f : 0.10f);
            Color bottom = Color.Lerp(accent, Color.Black, 0.50f);
            for (int y = 0; y < r.Height; y += 2)
            {
                float k = (float)y / Math.Max(1, r.Height - 1);
                sb.Draw(px, new Rectangle(r.X, r.Y + y, r.Width, Math.Min(2, r.Height - y)), Color.Lerp(top, bottom, k));
            }
            sb.Draw(px, new Rectangle(r.X + border, r.Y + border, r.Width - border * 2, 2), Color.White * 0.25f);

            Color frame = hovered ? Color.Gold : Stone;
            Border(sb, px, r, frame, border);
            Border(sb, px, new Rectangle(r.X + border, r.Y + border, r.Width - border * 2, r.Height - border * 2), Color.Black * 0.4f, 1);

            int s = Math.Min(8, Math.Max(4, r.Height / 5));
            foreach (var p in new[] { new Point(r.X - 2, r.Y - 2), new Point(r.Right - s + 2, r.Y - 2), new Point(r.X - 2, r.Bottom - s + 2), new Point(r.Right - s + 2, r.Bottom - s + 2) })
            {
                sb.Draw(px, new Rectangle(p.X, p.Y, s, s), StoneDark);
                Border(sb, px, new Rectangle(p.X, p.Y, s, s), hovered ? Color.Gold : new Color(190, 200, 225), 1);
            }
        }

        // ปุ่ม Back มาตรฐาน: สีแดง + ลูกศรซ้าย + ข้อความ BACK
        public static void BackButton(SpriteBatch sb, Texture2D px, SpriteFont font, Rectangle r, bool hovered)
        {
            StoneButton(sb, px, r, new Color(200, 70, 70), hovered);
            Color c = hovered ? Color.Gold : Color.White;
            int ax = r.X + 32, ay = r.Center.Y;
            Line(sb, px, new Vector2(ax + 9, ay - 11), new Vector2(ax, ay), c, 4);
            Line(sb, px, new Vector2(ax, ay), new Vector2(ax + 9, ay + 11), c, 4);
            if (font == null) return;
            float scale = 1.15f;
            Vector2 size = font.MeasureString("BACK");
            Vector2 pos = new Vector2(r.Center.X + 12, r.Center.Y);
            sb.DrawString(font, "BACK", pos + new Vector2(2, 3), Color.Black * 0.6f, 0f, size / 2f, scale, SpriteEffects.None, 0f);
            sb.DrawString(font, "BACK", pos, c, 0f, size / 2f, scale, SpriteEffects.None, 0f);
        }

        private static void Border(SpriteBatch sb, Texture2D px, Rectangle r, Color c, int t)
        {
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        private static void Line(SpriteBatch sb, Texture2D px, Vector2 a, Vector2 b, Color c, int t)
        {
            Vector2 e = b - a; float len = e.Length();
            if (len < 0.01f) return;
            sb.Draw(px, new Rectangle((int)a.X, (int)a.Y, (int)len, t), null, c, (float)Math.Atan2(e.Y, e.X), Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}