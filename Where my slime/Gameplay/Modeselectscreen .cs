using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าเลือกโหมด: Story Mode / Challenge Mode
    public class ModeSelectScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private readonly Action _goToMainMenu;
        private readonly Action _goToStoryModeSelect;
        private readonly Action _startNewRunForChallenge;
        private readonly Action _playUIClick;

        private bool _hoveredModeStory;
        private bool _hoveredModeChallenge;
        private bool _hoveredModeBack;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        // ซ่อนการ์ด Story Mode ไว้ก่อน (true = แสดงการ์ด Locked ข้าง Challenge เหมือนเดิม)
        private const bool ShowStoryMode = false;

        public ModeSelectScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Action goToMainMenu,
            Action goToStoryModeSelect,
            Action startNewRunForChallenge,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _goToMainMenu = goToMainMenu;
            _goToStoryModeSelect = goToStoryModeSelect;
            _startNewRunForChallenge = startNewRunForChallenge;
            _playUIClick = playUIClick;
        }

        private Rectangle GetModeSelectStoryCardRect() => new Rectangle(ScreenWidth / 2 - 440, ScreenHeight / 2 - 250, 400, 500);
        // ซ่อน Story Mode อยู่ -> Challenge Mode อยู่กึ่งกลางหน้าจอ
        private Rectangle GetModeSelectChallengeCardRect() => ShowStoryMode
            ? new Rectangle(ScreenWidth / 2 + 40, ScreenHeight / 2 - 250, 400, 500)
            : new Rectangle(ScreenWidth / 2 - 200, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectBackRect() => new Rectangle(40, 40, 150, 50);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick?.Invoke();
                _goToMainMenu?.Invoke();
                return;
            }

            // Story Mode ปิดไว้ชั่วคราว กดไม่ได้ จึงไม่ต้องเช็ค hover/คลิกของการ์ดนี้เลย
            _hoveredModeStory = false;
            _hoveredModeChallenge = GetModeSelectChallengeCardRect().Contains(mouse.Position);
            _hoveredModeBack = GetModeSelectBackRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredModeBack) { _playUIClick?.Invoke(); _goToMainMenu?.Invoke(); }
                else if (_hoveredModeChallenge) { _playUIClick?.Invoke(); _startNewRunForChallenge?.Invoke(); }
            }
        }

        // ดาบ (ใช้ตกแต่งการ์ด Challenge)
        private void DrawSword(Vector2 c, float len, float angle, Color blade, Color hilt)
        {
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 nrm = new Vector2(-dir.Y, dir.X);
            Vector2 tip = c + dir * len;
            Vector2 guard = c - dir * len * 0.2f;
            Vector2 pommel = c - dir * len * 0.55f;
            DrawLine(c - dir * len * 0.15f - nrm * 3, tip - nrm * 3, blade, 8);
            DrawLine(guard - nrm * 18, guard + nrm * 18, hilt, 7);
            DrawLine(guard, pommel, hilt, 8);
            FillCircle(pommel, 7f, hilt);
        }

        // แม่กุญแจ (Story Mode ที่ยังล็อก)
        private void DrawPadlock(Vector2 c, Color body, Color shackle)
        {
            DrawRectBorder(new Rectangle((int)c.X - 20, (int)c.Y - 52, 40, 40), shackle, 7);
            DrawRect(new Rectangle((int)c.X - 36, (int)c.Y - 22, 72, 58), body);
            DrawRectBorder(new Rectangle((int)c.X - 36, (int)c.Y - 22, 72, 58), shackle, 3);
            FillCircle(new Vector2(c.X, c.Y + 2), 8f, new Color(20, 20, 26));
            DrawRect(new Rectangle((int)c.X - 3, (int)c.Y + 4, 6, 18), new Color(20, 20, 26));
        }

        private void DrawModeCard(Rectangle baseRect, bool hovered, bool locked, Color accent, string title, string desc, string footer)
        {
            var rect = hovered ? new Rectangle(baseRect.X, baseRect.Y - 10, baseRect.Width, baseRect.Height) : baseRect;
            if (hovered) DrawRect(new Rectangle(rect.X - 10, rect.Y - 10, rect.Width + 20, rect.Height + 20), accent * (0.10f + 0.06f * (float)Math.Sin(UiClock * 5f)));

            Color top = locked ? new Color(36, 36, 44) : Color.Lerp(new Color(40, 40, 62), accent, hovered ? 0.30f : 0.18f);
            Color bottom = locked ? new Color(16, 16, 22) : new Color(16, 16, 26);
            DrawPanel(rect, top, bottom, hovered ? Color.Gold : accent, hovered ? 5 : 3);

            var banner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, 190);
            DrawGradientRect(banner, accent * (locked ? 0.25f : 0.45f), accent * 0.05f);
            DrawLine(new Vector2(banner.X, banner.Bottom), new Vector2(banner.Right, banner.Bottom), accent * 0.8f, 2);

            var ic = new Vector2(rect.Center.X, banner.Center.Y + (hovered ? (float)Math.Sin(UiClock * 6f) * 3f : 0f));
            if (locked)
                DrawPadlock(ic + new Vector2(0, 6), new Color(110, 110, 124), new Color(150, 150, 164));
            else
            {
                DrawSword(ic + new Vector2(-10, 14), 74f, -MathHelper.PiOver4 * 0.95f - 0.15f, new Color(240, 215, 200), new Color(215, 80, 70));
                DrawSword(ic + new Vector2(10, 14), 74f, -MathHelper.Pi + MathHelper.PiOver4 * 0.95f + 0.15f, new Color(240, 215, 200), new Color(215, 80, 70));
            }

            DrawTextShadow(title, new Vector2(rect.Center.X, rect.Y + 238), locked ? Color.Gray : Color.White, 1.9f);
            DrawLine(new Vector2(rect.X + 50, rect.Y + 282), new Vector2(rect.Right - 50, rect.Y + 282), accent * 0.8f, 2);
            DrawWrappedCentered(desc, new Vector2(rect.Center.X, rect.Y + 322), rect.Width - 80, locked ? Color.DimGray : Color.LightGray, 1f, 32f);

            if (!string.IsNullOrEmpty(footer))
            {
                var pill = new Rectangle(rect.Center.X - 110, rect.Bottom - 70, 220, 42);
                DrawRect(pill, new Color(10, 10, 16));
                DrawRectBorder(pill, locked ? Color.DimGray : Color.Gold * 0.9f, 2);
                DrawTextCentered(footer, new Vector2(pill.Center.X, pill.Center.Y), locked ? Color.Gray : Color.Gold, 1.05f);
            }
        }

        public void Draw()
        {
            DrawBackdrop();
            DrawTitleBanner("Select Game Mode", 120, 2.8f);

            if (ShowStoryMode)
                DrawModeCard(GetModeSelectStoryCardRect(), false, true, new Color(120, 120, 140),
                    "Story Mode", "Follow the tale of the slimes. Coming soon!", "Locked");

            DrawModeCard(GetModeSelectChallengeCardRect(), _hoveredModeChallenge, false, new Color(235, 110, 90),
                "Challenge Mode", "Climb endless floors, grab items and defeat a Boss every 5 floors.",
                _hoveredModeChallenge ? "Click to Start" : "Play");

            var backRect = GetModeSelectBackRect();
            UiKit.BackButton(_spriteBatch, _pixel, _font, backRect, _hoveredModeBack);
        }

        // ---------- ตัวช่วยตกแต่ง UI (พื้นหลัง/พาเนล/ปุ่ม) ----------
        private static float UiClock => (Environment.TickCount64 % 100000) / 1000f;

        private void DrawGradientRect(Rectangle r, Color top, Color bottom)
        {
            const int step = 4;
            for (int y = 0; y < r.Height; y += step)
            {
                float t = r.Height <= 1 ? 0f : (float)y / (r.Height - 1);
                _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y + y, r.Width, Math.Min(step, r.Height - y)), Color.Lerp(top, bottom, t));
            }
        }

        private void DrawBackdrop()
        {
            DrawGradientRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(30, 30, 50), new Color(12, 12, 22));
            // ลายเส้นเฉียงจางๆ ให้พื้นหลังไม่เรียบเกินไป
            for (int x = -ScreenHeight; x < ScreenWidth; x += 120)
                DrawLine(new Vector2(x, 0), new Vector2(x + ScreenHeight, ScreenHeight), Color.White * 0.025f, 2);
            // ขอบมืดรอบจอ
            DrawRect(new Rectangle(0, 0, ScreenWidth, 8), Color.Black * 0.35f);
            DrawRect(new Rectangle(0, ScreenHeight - 8, ScreenWidth, 8), Color.Black * 0.35f);
        }

        private void DrawPanel(Rectangle r, Color top, Color bottom, Color border, int thickness)
        {
            DrawRect(new Rectangle(r.X + 6, r.Y + 8, r.Width, r.Height), Color.Black * 0.35f);
            DrawGradientRect(r, top, bottom);
            DrawRectBorder(r, border, thickness);
            int pad = thickness + 4;
            if (r.Width > pad * 2 + 4 && r.Height > pad * 2 + 4)
                DrawRectBorder(new Rectangle(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2), border * 0.25f, 1);
            int s = 8;
            DrawRect(new Rectangle(r.X - 2, r.Y - 2, s, s), border);
            DrawRect(new Rectangle(r.Right - s + 2, r.Y - 2, s, s), border);
            DrawRect(new Rectangle(r.X - 2, r.Bottom - s + 2, s, s), border);
            DrawRect(new Rectangle(r.Right - s + 2, r.Bottom - s + 2, s, s), border);
        }

        private void DrawButton(Rectangle r, Color baseColor, bool hovered, int border = 2)
        {
            UiKit.StoneButton(_spriteBatch, _pixel, r, baseColor, hovered, border);
        }

        private void DrawTextShadow(string text, Vector2 center, Color color, float scale)
        {
            DrawTextCentered(text, center + new Vector2(2, 3), Color.Black * 0.65f, scale);
            DrawTextCentered(text, center, color, scale);
        }

        private void FillCircle(Vector2 c, float radius, Color color)
        {
            int r = (int)Math.Ceiling(radius);
            for (int dy = -r; dy <= r; dy++)
            {
                float w = (float)Math.Sqrt(Math.Max(0f, radius * radius - dy * dy));
                if (w < 0.5f) continue;
                _spriteBatch.Draw(_pixel, new Rectangle((int)Math.Round(c.X - w), (int)Math.Round(c.Y + dy), (int)Math.Round(w * 2), 1), color);
            }
        }

        // เหรียญทอง (ใช้แสดงราคา/จำนวนเงิน)
        private void DrawCoin(Vector2 c, float radius)
        {
            FillCircle(c, radius, new Color(150, 105, 10));
            FillCircle(c, radius - 2, new Color(255, 205, 50));
            FillCircle(c, radius - 5, new Color(235, 170, 25));
            DrawRect(new Rectangle((int)(c.X - 2), (int)(c.Y - radius * 0.45f), 4, (int)(radius * 0.9f)), new Color(255, 235, 140));
        }

        // หัวเรื่องมีเส้นตกแต่งสองข้าง
        private void DrawTitleBanner(string title, float centerY, float scale)
        {
            DrawTextShadow(title, new Vector2(ScreenWidth / 2f, centerY), Color.Gold, scale);
            Vector2 size = _font != null ? _font.MeasureString(GetSafeText(title)) * scale : Vector2.Zero;
            float half = size.X / 2f + 30f;
            float cx = ScreenWidth / 2f;
            DrawLine(new Vector2(cx - half - 240, centerY), new Vector2(cx - half, centerY), Color.Gold * 0.7f, 3);
            DrawLine(new Vector2(cx + half, centerY), new Vector2(cx + half + 240, centerY), Color.Gold * 0.7f, 3);
            DrawRect(new Rectangle((int)(cx - half - 8), (int)centerY - 4, 10, 10), Color.Gold);
            DrawRect(new Rectangle((int)(cx + half - 2), (int)centerY - 4, 10, 10), Color.Gold);
        }

        private void DrawWrappedCentered(string text, Vector2 center, float maxWidth, Color color, float scale, float lineHeight)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            var words = GetSafeText(text).Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            string line = "";
            foreach (var w in words)
            {
                string test = line.Length == 0 ? w : line + " " + w;
                if (line.Length > 0 && _font.MeasureString(test).X * scale > maxWidth) { lines.Add(line); line = w; }
                else line = test;
            }
            if (line.Length > 0) lines.Add(line);
            for (int i = 0; i < lines.Count; i++)
                DrawTextCentered(lines[i], new Vector2(center.X, center.Y + i * lineHeight), color, scale);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
        }

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

        private void DrawRectBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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

        private void DrawTextCentered(string text, Vector2 center, Color color, float scale = 1f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            Vector2 size;
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeText(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}