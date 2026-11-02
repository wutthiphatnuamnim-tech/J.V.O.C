using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    public enum StatusType { Aura, Blessing, Lifesteal, Stun, Poison, Weakness, Aggro, Fire, Melt, Shield }

    // ไอคอนสถานะ (Buff/Debuff) 7 ช่อง ช่องละ 64x64 เรียงแนวนอนตามลำดับ StatusType
    // ถ้าอยากแก้รูป ให้วางไฟล์ Status/Status_Icons.png (Content/Status) แล้วโหลดผ่าน Load(device, texture)
    public static class StatusIcons
    {
        public static Texture2D Sheet;
        private static Texture2D _px;
        private const int Cell = 64;

        public static void Load(GraphicsDevice device, Texture2D fromFile)
        {
            _px = new Texture2D(device, 1, 1);
            _px.SetData(new[] { Color.White });
            if (fromFile != null) { Sheet = fromFile; return; }
            try
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(Base64)))
                    Sheet = Texture2D.FromStream(device, stream);
            }
            catch { Sheet = null; }
        }

        public static bool Draw(SpriteBatch sb, StatusType type, Rectangle dest, float alpha = 1f)
        {
            if (Sheet == null) return false;
            // ช่อง Fire / Melt ไม่อยู่ในชีตเดิม (ชีตเดิมมี 7 ช่อง) -> วาดไอคอนแบบพิกเซลแทน ถ้าวางชีตใหม่ที่มี 9 ช่องจะใช้รูปจากชีต
            if ((int)type * Cell + Cell > Sheet.Width)
                return DrawFallback(sb, type, dest, alpha);
            sb.Draw(Sheet, dest, new Rectangle((int)type * Cell, 0, Cell, Cell), Color.White * alpha);
            return true;
        }

        private static readonly string[] FireGlyph =
        {
            "...#....", "...##...", "..###.#.", ".######.", ".######.", ".#####..", "..####..", "...##..."
        };
        private static readonly string[] MeltGlyph =
        {
            "...#....", "...#....", "..###...", ".#####..", ".#####..", ".#####..", "..###...", "........"
        };

        private static readonly string[] ShieldGlyph =
        {
            ".######.", "########", "##.##.##", "########", ".######.", "..####..", "...##...", "........"
        };

        private static bool DrawFallback(SpriteBatch sb, StatusType type, Rectangle dest, float alpha)
        {
            if (_px == null) return false;
            bool fire = type == StatusType.Fire;
            bool shield = type == StatusType.Shield;
            Color bg = shield ? new Color(18, 36, 90) : (fire ? new Color(95, 28, 12) : new Color(18, 55, 70));
            Color fg = shield ? new Color(170, 215, 255) : (fire ? new Color(255, 160, 45) : new Color(120, 225, 235));
            Color edge = shield ? new Color(80, 140, 235) : (fire ? new Color(255, 90, 30) : new Color(60, 170, 200));
            sb.Draw(_px, dest, bg * alpha);
            // กรอบบางๆ
            sb.Draw(_px, new Rectangle(dest.X, dest.Y, dest.Width, 1), edge * alpha);
            sb.Draw(_px, new Rectangle(dest.X, dest.Bottom - 1, dest.Width, 1), edge * alpha);
            sb.Draw(_px, new Rectangle(dest.X, dest.Y, 1, dest.Height), edge * alpha);
            sb.Draw(_px, new Rectangle(dest.Right - 1, dest.Y, 1, dest.Height), edge * alpha);

            var glyph = shield ? ShieldGlyph : (fire ? FireGlyph : MeltGlyph);
            float cw = dest.Width / 8f, ch = dest.Height / 8f;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (glyph[y][x] == '#')
                        sb.Draw(_px, new Rectangle(dest.X + (int)(x * cw), dest.Y + (int)(y * ch), (int)Math.Ceiling(cw), (int)Math.Ceiling(ch)), fg * alpha);
            return true;
        }

        public static string Label(StatusType t) => t.ToString();

        // สถานะที่ติดอยู่: Turns = เทิร์นที่เหลือ (แสดงมุมซ้ายล่าง) / Stacks = จำนวน Stack (แสดงมุมขวาล่าง) 0 = ไม่แสดง
        public struct Entry
        {
            public StatusType Type;
            public int Turns;
            public int Stacks;
        }

        public static List<Entry> Active(Where_My_Slime.Models.Character c)
        {
            var l = new List<Entry>();
            if (c == null) return l;
            if (c.AuraTurns > 0) l.Add(new Entry { Type = StatusType.Aura, Turns = c.AuraTurns });
            else if (c.SwordmanBuffDuration > 0) l.Add(new Entry { Type = StatusType.Aura, Turns = c.SwordmanBuffDuration });
            // Blessing: Stack อยู่ขวา / เทิร์นที่เหลือก่อนลด 1 Stack อยู่ซ้าย
            if (c.BlessingStacks > 0) l.Add(new Entry { Type = StatusType.Blessing, Stacks = c.BlessingStacks, Turns = Math.Max(1, Where_My_Slime.Models.Character.BlessingDecayRounds - c.BlessingTimer) });
            if (c.LifestealTurns > 0) l.Add(new Entry { Type = StatusType.Lifesteal, Turns = c.LifestealTurns });
            if (c.AggroTurns > 0) l.Add(new Entry { Type = StatusType.Aggro, Turns = c.AggroTurns });
            if (c.ShieldStacks > 0) l.Add(new Entry { Type = StatusType.Shield, Stacks = c.ShieldStacks });
            if (c.StunTurns > 0) l.Add(new Entry { Type = StatusType.Stun, Turns = c.StunTurns });
            if (c.PoisonTurns > 0) l.Add(new Entry { Type = StatusType.Poison, Turns = c.PoisonTurns });
            if (c.WeaknessTurns > 0) l.Add(new Entry { Type = StatusType.Weakness, Turns = c.WeaknessTurns });
            if (c.FireStacks > 0) l.Add(new Entry { Type = StatusType.Fire, Turns = c.FireTurns, Stacks = c.FireStacks });
            if (c.MeltTurns > 0) l.Add(new Entry { Type = StatusType.Melt, Turns = c.MeltTurns });
            return l;
        }

        private const string Base64 = "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";
    }
}