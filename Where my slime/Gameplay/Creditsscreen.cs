using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime
{
    // หน้า Credit: ข้อความเลื่อนขึ้นแล้วเล็กลงไปไกลๆ แบบ Star Wars บนพื้นหลังดาว
    public class CreditsScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private const float Horizon = 260f;       // เส้นขอบฟ้า (ข้อความหายไปที่นี่)
        private const float Depth = 900f;         // ระยะที่ข้อความเล็กลงครึ่งหนึ่ง (ยิ่งน้อยยิ่งเล็กเร็ว)
        private const float Speed = 150f;         // ความเร็วเลื่อน (หน่วยความลึก/วินาที)
        private const float StartDepth = -0.45f * Depth; // เริ่มต้นใต้จอเล็กน้อย
        private const float BaseScale = 3.2f;
        private const float LineGap = 130f;       // ระยะห่างระหว่างบรรทัด (หน่วยความลึก)

        private static readonly string[] Lines = { "Made By J.V.O.C" };

        private float _time;
        private readonly Vector2[] _stars = new Vector2[160];
        private readonly float[] _starPhase = new float[160];

        public CreditsScreen(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;

            var rng = new Random(7);
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = new Vector2(rng.Next(ScreenWidth), rng.Next(ScreenHeight));
                _starPhase[i] = (float)(rng.NextDouble() * Math.PI * 2);
            }
        }

        public void Reset() { _time = 0f; }

        public void Update(GameTime gameTime)
        {
            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // เลื่อนจบแล้ว วนเริ่มใหม่หลังเว้นช่วงสั้นๆ
            float endDepth = StartDepth + _time * Speed - (Lines.Length - 1) * LineGap;
            if (endDepth > Depth * 12f) _time = 0f;
        }

        public void Draw()
        {
            // พื้นหลังอวกาศ
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(3, 4, 14));

            for (int i = 0; i < _stars.Length; i++)
            {
                float tw = 0.5f + 0.5f * (float)Math.Sin(_time * 2f + _starPhase[i]);
                int size = (i % 7 == 0) ? 3 : 2;
                _spriteBatch.Draw(_pixel, new Rectangle((int)_stars[i].X, (int)_stars[i].Y, size, size), Color.White * (0.25f + 0.6f * tw));
            }

            // ข้อความเลื่อนขึ้น: ยิ่งไกลยิ่งเล็กและจางลง
            for (int i = 0; i < Lines.Length; i++)
            {
                float w = StartDepth + _time * Speed - i * LineGap; // ความลึกของบรรทัดนี้
                float s = 1f / (1f + w / Depth);
                if (s <= 0.02f || s > 3f) continue;

                float y = Horizon + (ScreenHeight - Horizon) * s;
                float fade = MathHelper.Clamp((s - 0.04f) / 0.3f, 0f, 1f);   // จางหายตอนใกล้ขอบฟ้า
                float scale = BaseScale * s;
                DrawTextCentered(Lines[i], new Vector2(ScreenWidth / 2f, y), new Color(255, 215, 70) * fade, scale);
            }

            DrawTextCentered("Click or press Esc to go back", new Vector2(ScreenWidth / 2f, ScreenHeight - 40), Color.Gray * 0.7f, 0.8f);
        }

        private string GetSafeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] > 126 || chars[i] < 32)
                {
                    if (chars[i] != '\n' && chars[i] != '\r' && chars[i] != '\t') chars[i] = '?';
                }
            }
            return new string(chars);
        }

        private void DrawTextCentered(string text, Vector2 center, Color color, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            Vector2 size;
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeText(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}