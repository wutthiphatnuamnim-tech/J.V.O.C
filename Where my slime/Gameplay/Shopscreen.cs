using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Data;
using Where_My_Slime.Manager;

namespace Where_My_Slime
{
    // หน้าร้านพ่อค้าเร่: ซื้อไอเท็มด้วยเหรียญที่สะสมมา
    public class ShopScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _onDone;
        private readonly Action _playUIClick;

        private int _shopScrollY = 0;
        private int _previousScrollWheelValue;
        private bool _sellMode = false; // false = แท็บ Buy, true = แท็บ Sell

        private const float SellPriceRatio = 0.4f; // ขายได้ 40% ของราคาเดิม

        private static int GetSellPrice(Item item) => Math.Max(1, (int)Math.Round(item.Price * SellPriceRatio));

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public ShopScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action onDone,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _onDone = onDone;
            _playUIClick = playUIClick;
        }

        // เรียกตอนเข้าหน้า Shop ใหม่จาก Map เพื่อสุ่มสินค้าและรีเซ็ตสกอร์ล
        public void EnterFresh(Random rng, int currentScrollWheelValue)
        {
            _session.ShopItems = ItemPool.GetRandomItems(9, rng);
            _shopScrollY = 0;
            _sellMode = false;
            _previousScrollWheelValue = currentScrollWheelValue;
        }

        private Rectangle GetBuyTabRect() => new Rectangle(120, 50, 170, 56);
        private Rectangle GetSellTabRect() => new Rectangle(310, 50, 170, 56);

        private Rectangle GetShopItemRect(int index, int scrollY)
        {
            int col = index % 3;
            int row = index / 3;
            int width = 380;
            int height = 450;
            int spacingX = 50;
            int spacingY = 50;

            int totalWidth = 3 * width + 2 * spacingX;
            int startX = (ScreenWidth - totalWidth) / 2;
            int startY = 160;

            return new Rectangle(startX + col * (width + spacingX), startY + row * (height + spacingY) - scrollY, width, height);
        }

        private Rectangle GetShopNextButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 60);

        public void Update(MouseState mouse, bool leftClicked)
        {
            int currentScrollWheel = mouse.ScrollWheelValue;
            if (currentScrollWheel > _previousScrollWheelValue) _shopScrollY -= 50;
            else if (currentScrollWheel < _previousScrollWheelValue) _shopScrollY += 50;
            _previousScrollWheelValue = currentScrollWheel;

            // สลับแท็บ Buy / Sell
            if (leftClicked)
            {
                if (GetBuyTabRect().Contains(mouse.Position) && _sellMode)
                {
                    _playUIClick?.Invoke();
                    _sellMode = false;
                    _shopScrollY = 0;
                    return;
                }
                if (GetSellTabRect().Contains(mouse.Position) && !_sellMode)
                {
                    _playUIClick?.Invoke();
                    _sellMode = true;
                    _shopScrollY = 0;
                    return;
                }
            }

            var list = _sellMode ? _session.Inventory : _session.ShopItems;

            int maxScroll = Math.Max(0, (int)(Math.Ceiling(list.Count / 3.0) * 500) - 800);
            if (_shopScrollY < 0) _shopScrollY = 0;
            if (_shopScrollY > maxScroll) _shopScrollY = maxScroll;

            for (int i = 0; i < list.Count; i++)
            {
                var rect = GetShopItemRect(i, _shopScrollY);
                // ไม่รับคลิกบนพื้นที่หัวหน้าจอ (การ์ดที่เลื่อนไปซ่อนอยู่ใต้แถบหัว)
                if (rect.Bottom > 0 && rect.Top < ScreenHeight && mouse.Y >= 140 && rect.Contains(mouse.Position) && leftClicked)
                {
                    if (_sellMode)
                    {
                        AudioManager.PlaySfx("Audio/Selling");
                        _session.Coins += GetSellPrice(list[i]);
                        list.RemoveAt(i);
                        break;
                    }
                    else if (_session.Coins >= list[i].Price)
                    {
                        AudioManager.PlaySfx("Audio/Buying");
                        _session.Coins -= list[i].Price;
                        _session.Inventory.Add(list[i]);
                        list.RemoveAt(i);
                        break;
                    }
                }
            }

            var nextRect = GetShopNextButtonRect();
            if (nextRect.Contains(mouse.Position) && leftClicked)
            {
                _playUIClick?.Invoke();
                _onDone?.Invoke();
            }
        }

        public void Draw()
        {
            DrawBackdrop();
            var list = _sellMode ? _session.Inventory : _session.ShopItems;
            var mousePos = VirtualMouse.GetState().Position;

            for (int i = 0; i < list.Count; i++)
            {
                var rect = GetShopItemRect(i, _shopScrollY);
                if (rect.Bottom < 0 || rect.Top > ScreenHeight) continue;

                var item = list[i];
                bool hovered = rect.Contains(mousePos) && mousePos.Y >= 140;
                bool affordable = _sellMode || _session.Coins >= item.Price;
                Color rarityColor = GetRarityColor(item.Rarity);

                var card = hovered && affordable ? new Rectangle(rect.X, rect.Y - 6, rect.Width, rect.Height) : rect;
                Color borderCol = !affordable ? new Color(90, 90, 90) : (hovered ? Color.Gold : rarityColor);
                DrawPanel(card, hovered && affordable ? new Color(62, 62, 98) : new Color(40, 40, 62), new Color(18, 18, 30), borderCol, hovered && affordable ? 4 : 3);

                // แถบสีตามระดับความหายากด้านบนการ์ด
                DrawGradientRect(new Rectangle(card.X + 8, card.Y + 8, card.Width - 16, 14), rarityColor * (affordable ? 0.55f : 0.2f), Color.Transparent);

                // กรอบรูปไอเท็ม
                var plate = new Rectangle(card.Center.X - 76, card.Y + 30, 152, 152);
                DrawRect(plate, new Color(12, 12, 20));
                DrawRectBorder(plate, rarityColor * (affordable ? 0.9f : 0.35f), 2);
                ItemIcons.Draw(_spriteBatch, item, new Rectangle(plate.X + 16, plate.Y + 16, 120, 120), affordable ? 1f : 0.45f);

                DrawTextCentered(item.Name, new Vector2(card.Center.X, card.Y + 210), !affordable ? Color.Gray : rarityColor, 1.3f);

                // ป้ายความหายาก
                string rarityText = item.Rarity.ToString().ToUpper();
                var badge = new Rectangle(card.Center.X - 70, card.Y + 236, 140, 28);
                DrawRect(badge, rarityColor * 0.18f);
                DrawRectBorder(badge, rarityColor * (affordable ? 0.8f : 0.3f), 1);
                DrawTextCentered(rarityText, new Vector2(badge.Center.X, badge.Center.Y), affordable ? rarityColor : Color.Gray, 0.8f);

                DrawWrappedCentered(item.Description, new Vector2(card.Center.X, card.Y + 296), card.Width - 50, affordable ? Color.White : Color.LightGray, 0.85f, 26f);

                // ป้ายราคา
                var priceBox = new Rectangle(card.X + 40, card.Bottom - 72, card.Width - 80, 46);
                DrawRect(priceBox, new Color(10, 10, 16));
                DrawRectBorder(priceBox, _sellMode ? Color.Orange * 0.8f : (affordable ? Color.LimeGreen * 0.8f : Color.Red * 0.7f), 2);
                int amount = _sellMode ? GetSellPrice(item) : item.Price;
                Color priceCol = _sellMode ? Color.Orange : (affordable ? Color.LimeGreen : Color.Red);
                DrawCoin(new Vector2(priceBox.X + 30, priceBox.Center.Y), 13f);
                DrawTextCentered(_sellMode ? $"Sell: {amount} G" : $"{amount} G", new Vector2(priceBox.Center.X + 14, priceBox.Center.Y), priceCol, 1.2f);
            }

            if (_sellMode && list.Count == 0)
            {
                DrawTextCentered("Your inventory is empty - nothing to sell", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.LightGray, 1.4f);
            }
            else if (!_sellMode && list.Count == 0)
            {
                DrawTextCentered("Sold out", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.LightGray, 1.4f);
            }

            // แถบหัว (ทับการ์ดที่เลื่อนขึ้นมา)
            DrawRect(new Rectangle(0, 0, ScreenWidth, 140), new Color(14, 14, 24));
            DrawGradientRect(new Rectangle(0, 0, ScreenWidth, 140), new Color(44, 44, 70), new Color(20, 20, 34));
            DrawRect(new Rectangle(0, 138, ScreenWidth, 3), Color.Gold);
            DrawRect(new Rectangle(0, 141, ScreenWidth, 5), Color.Black * 0.3f);
            DrawTextShadow("Wandering Merchant", new Vector2(ScreenWidth / 2f, 55), Color.Gold, 2.5f);

            // ช่องแสดงเหรียญ
            var coinBox = new Rectangle(ScreenWidth / 2 - 110, 92, 220, 38);
            DrawRect(coinBox, new Color(10, 10, 16));
            DrawRectBorder(coinBox, Color.Gold * 0.7f, 2);
            DrawCoin(new Vector2(coinBox.X + 26, coinBox.Center.Y), 12f);
            DrawTextCentered($"{_session.Coins} G", new Vector2(coinBox.Center.X + 14, coinBox.Center.Y), Color.Gold, 1.2f);

            var buyRect = GetBuyTabRect();
            var sellRect = GetSellTabRect();

            DrawButton(buyRect, !_sellMode ? new Color(220, 170, 30) : new Color(50, 50, 78), buyRect.Contains(mousePos));
            DrawTextCentered("Buy", new Vector2(buyRect.Center.X, buyRect.Center.Y), !_sellMode ? Color.Black : Color.White, 1.3f);

            DrawButton(sellRect, _sellMode ? new Color(220, 170, 30) : new Color(50, 50, 78), sellRect.Contains(mousePos));
            DrawTextCentered("Sell", new Vector2(sellRect.Center.X, sellRect.Center.Y), _sellMode ? Color.Black : Color.White, 1.3f);

            if (_sellMode)
                DrawTextCentered("Items sell for 40% of their original price", new Vector2(ScreenWidth - 420, 110), Color.LightGray, 0.9f);

            var nextRect = GetShopNextButtonRect();
            DrawButton(nextRect, new Color(55, 135, 55), nextRect.Contains(mousePos), 3);
            DrawTextCentered("Next ->", new Vector2(nextRect.Center.X, nextRect.Center.Y), Color.White, 1.2f);
        }

        private Color GetRarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common => Color.White,
            ItemRarity.Uncommon => Color.LimeGreen,
            ItemRarity.Rare => Color.DeepSkyBlue,
            ItemRarity.Epic => Color.MediumPurple,
            ItemRarity.Legendary => Color.Gold,
            ItemRarity.Mythic => Color.Red,
            ItemRarity.Secret => Color.Black,
            ItemRarity.Unique => Where_My_Slime.Data.RarityColors.Rainbow(),
            _ => Color.White
        };

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
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