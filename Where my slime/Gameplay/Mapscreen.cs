using System;
using Where_My_Slime.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าแผนที่ระหว่างด่าน (Challenge Mode): เลือกไปด่านธรรมดา/ด่านยาก หรือเข้าร้านค้า (ถ้ามี)
    public class MapScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action<bool> _chooseStage; // isHard
        private readonly Action<int> _chooseShop;    // currentScrollWheelValue
        private readonly Action _chooseBoss;
        private readonly Action _playUIClick;

        private readonly Random _rng = new Random();

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public MapScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action<bool> chooseStage,
            Action<int> chooseShop,
            Action chooseBoss,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _chooseStage = chooseStage;
            _chooseShop = chooseShop;
            _chooseBoss = chooseBoss;
            _playUIClick = playUIClick;
        }

        // ทุกๆ 5 ชั้น (5, 10, 15, ...) จะมีให้เลือกแค่เส้นทางเดียวคือ Boss
        private bool IsNextFloorBoss()
        {
            int nextFloorNumber = _session.StoryStageIndex + 2;
            return nextFloorNumber % 5 == 0;
        }

        private Rectangle GetMapButtonRect(int i, int totalButtons)
        {
            int width = 340;
            int height = 420;
            int spacing = 70;
            int totalWidth = totalButtons * width + (totalButtons - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            return new Rectangle(startX + i * (width + spacing), (ScreenHeight - height) / 2 + 20, width, height);
        }

        // สุ่มกล่องของด่านถัดไป (ครั้งเดียวต่อชั้น เพื่อให้ตอนดูบนแผนที่กับตอนได้รางวัลเป็นกล่องเดียวกัน)
        private void EnsureChests()
        {
            if (_session.ChestRolledForStage == _session.StoryStageIndex) return;
            _session.ChestRolledForStage = _session.StoryStageIndex;
            int nextFloor = _session.StoryStageIndex + 2;
            _session.NormalChest = _rng.NextDouble() < ChestSystem.ChestDropChance ? ChestSystem.RollChestTier(false, false, nextFloor, _rng) : (ItemRarity?)null;
            _session.HardChest = _rng.NextDouble() < ChestSystem.ChestDropChance ? ChestSystem.RollChestTier(true, false, nextFloor, _rng) : (ItemRarity?)null;
            _session.BossChest = ChestSystem.RollChestTier(true, true, nextFloor, _rng);
        }

        public void Update(MouseState mouse, bool leftClicked)
        {
            EnsureChests();
            if (IsNextFloorBoss())
            {
                var bossRect = GetMapButtonRect(0, 1);
                bool hoveredBoss = bossRect.Contains(mouse.Position);
                if (leftClicked && hoveredBoss)
                {
                    _playUIClick?.Invoke();
                    _session.PendingChest = _session.BossChest;
                    _chooseBoss?.Invoke();
                }
                return;
            }

            int totalButtons = _session.IsShopOnMap ? 3 : 2;
            int hoveredMapChoice = -1;

            for (int i = 0; i < totalButtons; i++)
            {
                if (GetMapButtonRect(i, totalButtons).Contains(mouse.Position)) hoveredMapChoice = i;
            }

            if (leftClicked && hoveredMapChoice >= 0)
            {
                _playUIClick?.Invoke();
                if (hoveredMapChoice == 0 || hoveredMapChoice == 1)
                {
                    _session.PendingChest = hoveredMapChoice == 1 ? _session.HardChest : _session.NormalChest;
                    _chooseStage?.Invoke(hoveredMapChoice == 1);
                }
                else if (hoveredMapChoice == 2 && _session.IsShopOnMap)
                {
                    _chooseShop?.Invoke(mouse.ScrollWheelValue);
                }
            }
        }

        // ไอคอนพิกเซลหัวกระโหลก (ป้าย Boss)
        private static readonly string[] SkullGlyph =
        {
            "..XXXXXXX..",
            ".XXXXXXXXX.",
            "XXXXXXXXXXX",
            "XX..XXX..XX",
            "XX..XXX..XX",
            "XXXXXXXXXXX",
            ".XXXX.XXXX.",
            "..XXXXXXX..",
            "..X.X.X.X..",
            "..X.X.X.X.."
        };

        private void DrawGlyph(string[] glyph, Rectangle box, Color color)
        {
            int rows = glyph.Length, cols = glyph[0].Length;
            int cell = Math.Max(1, Math.Min(box.Width / cols, box.Height / rows));
            int ox = box.Center.X - cols * cell / 2;
            int oy = box.Center.Y - rows * cell / 2;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    if (glyph[y][x] == 'X')
                        _spriteBatch.Draw(_pixel, new Rectangle(ox + x * cell, oy + y * cell, cell, cell), color);
        }

        private void DrawSword(Vector2 c, float len, float angle, Color blade, Color hilt)
        {
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 nrm = new Vector2(-dir.Y, dir.X);
            Vector2 tip = c + dir * len;
            Vector2 guard = c - dir * len * 0.2f;
            Vector2 pommel = c - dir * len * 0.55f;
            DrawLine(c - dir * len * 0.15f - nrm * 3, tip - nrm * 3, blade, 7);
            DrawLine(guard - nrm * 16, guard + nrm * 16, hilt, 6);
            DrawLine(guard, pommel, hilt, 7);
            FillCircle(pommel, 6f, hilt);
        }

        // ไอคอนประจำการ์ด: 0 = ดาบเดี่ยว, 1 = ดาบไขว้, 2 = เหรียญ, 3 = กระโหลก (Boss)
        private void DrawCardIcon(int kind, Vector2 c, bool hovered)
        {
            float bob = hovered ? (float)Math.Sin(UiClock * 6f) * 3f : 0f;
            c.Y += bob;
            switch (kind)
            {
                case 0:
                    DrawSword(c + new Vector2(8, 4), 62f, -MathHelper.PiOver4, new Color(215, 225, 240), new Color(200, 160, 60));
                    break;
                case 1:
                    DrawSword(c + new Vector2(-6, 6), 58f, -MathHelper.PiOver4, new Color(240, 200, 200), new Color(200, 70, 70));
                    DrawSword(c + new Vector2(6, 6), 58f, -MathHelper.Pi + MathHelper.PiOver4, new Color(240, 200, 200), new Color(200, 70, 70));
                    break;
                case 2:
                    DrawCoin(c, 38f);
                    break;
                default:
                    DrawGlyph(SkullGlyph, new Rectangle((int)c.X - 44, (int)c.Y - 44, 88, 88), new Color(255, 110, 110));
                    break;
            }
        }

        // วาดช่องตัวเลือกหนึ่งช่อง สไตล์ดันเจี้ยน: กรอบหินสีตามประเภทด่าน + ไอคอนใหญ่ตรงกลาง ไม่มีข้อความ
        // frame: Normal = ฟ้า, Hard = แดง, Shop = ม่วง | icon: 0 = กล่อง (หรือช่องว่างถ้าไม่มี), 2 = เหรียญ
        private void DrawTile(Rectangle baseRect, bool hovered, Color frame, int icon, ItemRarity? chest,
                              string title, string desc, string line1, string line2)
        {
            var rect = hovered ? new Rectangle(baseRect.X, baseRect.Y - 6, baseRect.Width, baseRect.Height) : baseRect;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(UiClock * 5f);

            DrawRect(new Rectangle(rect.X + 6, rect.Y + 8, rect.Width, rect.Height), Color.Black * 0.45f);
            if (hovered) DrawRect(new Rectangle(rect.X - 10, rect.Y - 10, rect.Width + 20, rect.Height + 20), frame * (0.12f + 0.08f * pulse));

            Color stone = Color.Lerp(new Color(70, 78, 104), frame, hovered ? 0.85f : 0.6f);
            DrawRect(rect, new Color(14, 15, 22));
            DrawRectBorder(rect, stone, 8);
            DrawRectBorder(new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16), Color.Lerp(new Color(30, 33, 48), frame, 0.25f), 3);
            DrawRectBorder(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), Color.White * 0.18f, 1);
            DrawRectBorder(new Rectangle(rect.X + 11, rect.Y + 11, rect.Width - 22, rect.Height - 22), Color.Black * 0.6f, 1);

            var inner = new Rectangle(rect.X + 12, rect.Y + 12, rect.Width - 24, rect.Height - 24);
            DrawGradientRect(inner, new Color(30, 32, 46), new Color(14, 15, 22));
            DrawRect(new Rectangle(inner.X, inner.Y, inner.Width, inner.Height / 2), frame * (hovered ? 0.10f : 0.05f));

            int st = 14;
            foreach (var p in new[] { new Point(rect.X - 3, rect.Y - 3), new Point(rect.Right - st + 3, rect.Y - 3),
                                      new Point(rect.X - 3, rect.Bottom - st + 3), new Point(rect.Right - st + 3, rect.Bottom - st + 3) })
            {
                DrawRect(new Rectangle(p.X, p.Y, st, st), new Color(52, 58, 80));
                DrawRectBorder(new Rectangle(p.X, p.Y, st, st), Color.Lerp(new Color(150, 160, 190), frame, 0.4f), 2);
                DrawRect(new Rectangle(p.X + 5, p.Y + 5, 4, 4), new Color(210, 215, 235));
            }

            // ไอคอนกลาง (กล่อง / เหรียญ) ด้านบน
            float bob = hovered ? (float)Math.Sin(UiClock * 6f) * 4f : 0f;
            Vector2 c = new Vector2(rect.Center.X, rect.Y + 112 + bob);
            if (icon == 2) DrawCoin(c, 48f);
            else if (chest.HasValue)
            {
                int size = 150;
                ChestIcons.Draw(_spriteBatch, chest.Value, new Rectangle((int)c.X - size / 2, (int)c.Y - size / 2, size, size));
            }
            else DrawTextCentered("No Chest", c, Color.Gray * 0.8f, 1f);

            // ป้ายชื่อ
            string safeTitle = title.ToUpperInvariant();
            float tScale = 1.15f;
            Vector2 tSize = _font != null ? _font.MeasureString(GetSafeText(safeTitle)) * tScale : new Vector2(120, 20);
            var plate = new Rectangle((int)(rect.Center.X - tSize.X / 2f - 18), rect.Y + 206, (int)tSize.X + 36, (int)tSize.Y + 12);
            DrawRect(plate, new Color(22, 18, 16));
            DrawRectBorder(plate, Color.Lerp(new Color(120, 90, 50), frame, 0.5f), 2);
            DrawTextCentered(safeTitle, new Vector2(plate.Center.X, plate.Center.Y), new Color(255, 205, 130), tScale);

            // รายละเอียด
            DrawTextCentered(desc, new Vector2(rect.Center.X, rect.Y + 268), Color.LightGray * 0.9f, 0.9f);
            if (!string.IsNullOrEmpty(line1)) DrawTextCentered(line1, new Vector2(rect.Center.X, rect.Y + 304), Color.LimeGreen, 0.95f);
            if (!string.IsNullOrEmpty(line2)) DrawTextCentered(line2, new Vector2(rect.Center.X, rect.Y + 334), Color.Gold, 0.95f);

            // แถบบอกระดับกล่อง (ร้านค้าไม่มี)
            if (icon == 2) return;
            var bar = new Rectangle(rect.X + 40, rect.Bottom - 56, rect.Width - 80, 34);
            Color cc = chest.HasValue ? ChestColor(chest.Value) : Color.Gray;
            DrawRect(bar, new Color(14, 14, 20));
            DrawRectBorder(bar, cc * 0.8f, 2);
            DrawTextCentered(chest.HasValue ? "Chest: " + chest.Value.ToString().ToUpper() : "Chest: NONE", new Vector2(bar.Center.X, bar.Center.Y), cc, 0.95f);
        }

        private static Color ChestColor(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Common: return Color.White;
                case ItemRarity.Uncommon: return Color.LimeGreen;
                case ItemRarity.Rare: return Color.DeepSkyBlue;
                case ItemRarity.Epic: return Color.MediumPurple;
                case ItemRarity.Legendary: return Color.Gold;
                case ItemRarity.Mythic: return Color.Red;
                case ItemRarity.Secret: return new Color(150, 150, 175);   // ดำ: ใช้เทาอ่อนเพื่อให้อ่านออกบนพื้นมืด
                default: return RarityColors.Rainbow();
            }
        }

        public void Draw()
        {
            EnsureChests();
            DrawBackdrop();
            DrawTitleBanner("Choose Your Path", 110, 2.8f);

            var mousePos = VirtualMouse.GetState().Position;

            if (IsNextFloorBoss())
            {
                DrawTextCentered("A powerful Boss blocks the way - only one path forward", new Vector2(ScreenWidth / 2f, 170), Color.LightGray, 1.1f);

                var bossRect = GetMapButtonRect(0, 1);
                bool hoveredBoss = bossRect.Contains(mousePos);
                DrawTile(bossRect, hoveredBoss, new Color(225, 65, 65), 0, _session.BossChest,
                    "Boss", "A single, much stronger enemy", "Reward: +10 Points", "Reward Coins: 125 G");
                return;
            }

            DrawTextCentered("Select your next battle stage", new Vector2(ScreenWidth / 2f, 170), Color.LightGray, 1.1f);

            int totalButtons = _session.IsShopOnMap ? 3 : 2;

            for (int i = 0; i < totalButtons; i++)
            {
                var rect = GetMapButtonRect(i, totalButtons);
                bool hovered = rect.Contains(mousePos);

                if (i == 0) DrawTile(rect, hovered, new Color(80, 150, 240), 0, _session.NormalChest,
                    "Normal Stage", "Standard enemy difficulty", "Reward: +3 Points", "Reward Coins: 10 G");
                else if (i == 1) DrawTile(rect, hovered, new Color(225, 70, 70), 0, _session.HardChest,
                    "Hard Stage", "Strong enemy difficulty", "Reward: +5 Points", "Reward Coins: 20 G");
                else if (i == 2 && _session.IsShopOnMap) DrawTile(rect, hovered, new Color(170, 130, 235), 2, null,
                    "Merchant Shop", "Buy powerful items", "", "Need: Coins");
            }
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

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

        private void DrawRectBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
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