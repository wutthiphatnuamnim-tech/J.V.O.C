using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Data;
using Where_My_Slime.Models;

namespace Where_My_Slime
{
    // หน้าจัดทีม (Formation): ลากฮีโร่จาก Hero Tray ลงช่อง Front/Back 5 ช่อง ก่อนเริ่มต่อสู้
    public class TeamSetupScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Texture2D _elmaIdleTex;
        private readonly Texture2D _lumiIdleTex;
        private readonly Texture2D _aliceIdleTex;
        private readonly Texture2D _veyraIdleTex;
        private readonly Texture2D _enosIdleTex;
        private readonly Texture2D _testCharacterTex;
        private readonly Dictionary<Texture2D, Rectangle> _spriteBoundsCache = new Dictionary<Texture2D, Rectangle>();
        private float _time;

        private readonly Action _goBack;
        private readonly Action _startBattle;
        private readonly Action _playUIClick;

        private bool _hoveredTeamStartButton;
        private bool _hoveredTeamBackButton;
        private int _hoveredSlotIndex = -1;
        private int _hoveredHeroTrayIndex = -1;
        private int _selectedHeroIndex = -1; // index ใน AllHeroes ของฮีโร่ที่กำลังถูกเลือกค้างไว้ รอให้กดเลือกช่องที่จะใส่

        private bool _showTeamWarning;
        private float _teamWarningTimer;

        private MouseState _previousMouseState;
        private Character _infoPanelCharacter; // ตัวละครที่กำลังโชว์ข้อมูล INFO ค้างไว้ (คลิกขวาเพื่อเปิด/ปิด)

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public TeamSetupScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Texture2D elmaIdleTex,
            Texture2D lumiIdleTex,
            Texture2D aliceIdleTex,
            Texture2D veyraIdleTex,
            Texture2D enosIdleTex,
            Texture2D testCharacterTex,
            Action goBack,
            Action startBattle,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _elmaIdleTex = elmaIdleTex;
            _lumiIdleTex = lumiIdleTex;
            _aliceIdleTex = aliceIdleTex;
            _veyraIdleTex = veyraIdleTex;
            _enosIdleTex = enosIdleTex;
            _testCharacterTex = testCharacterTex;
            _goBack = goBack;
            _startBattle = startBattle;
            _playUIClick = playUIClick;
        }

        // --- แท็บในแผงด้านขวา: 0 = Info, 1 = Equipment (สวม/ถอดไอเท็มตอนจัดทีม) ---
        private int _panelTab;
        private int _hoveredPanelTab = -1;
        private int _hoveredInvItem = -1;
        private int _hoveredInvFilter = -1;
        private int _hoveredUnequipSlot = -1;
        private int _invFilter = -1;          // -1 = All, 0 = Weapon, 1 = Armor, 2 = Additional
        private int _invScrollY;
        private int _prevScrollWheel;
        private Point _mousePos;
        private static readonly string[] SlotLabels = { "WEAPON", "ARMOR", "ADDITIONAL" };
        private static readonly string[] FilterLabels = { "All", "Weapon", "Armor", "Additional" };
        private static readonly int[] FilterWidths = { 46, 76, 62, 96 };
        private const int InvTile = 58;
        private const int InvPitch = 66;
        private const int InvCols = 5;

        private bool PanelInteractive => _infoPanelCharacter != null && _infoAnim > 0.95f;
        private Rectangle GetPanelTabRect(int i) { var r = GetInfoPanelRect(); return new Rectangle(r.X - 62, r.Y + i * 62, 54, 54); }
        private Rectangle GetEquipSlotRect(int slot)
        {
            var r = GetInfoPanelRect();
            const int size = 86, gap = 22;
            int startX = r.X + (r.Width - (3 * size + 2 * gap)) / 2;
            return new Rectangle(startX + slot * (size + gap), r.Y + 95, size, size);
        }
        private Rectangle GetUnequipRect(int slot) { var s = GetEquipSlotRect(slot); return new Rectangle(s.X, s.Bottom + 6, s.Width, 28); }
        private Rectangle GetInvFilterRect(int i)
        {
            var r = GetInfoPanelRect();
            const int gap = 6;
            int total = gap * 3; foreach (var w in FilterWidths) total += w;
            int x = r.Right - 20 - total;
            for (int k = 0; k < i; k++) x += FilterWidths[k] + gap;
            return new Rectangle(x, r.Y + 262, FilterWidths[i], 28);
        }
        private Rectangle GetInvItemRect(int index)
        {
            var r = GetInfoPanelRect();
            int gridW = InvCols * InvPitch - (InvPitch - InvTile);
            int startX = r.X + (r.Width - gridW) / 2;
            return new Rectangle(startX + (index % InvCols) * InvPitch, r.Y + 305 + (index / InvCols) * InvPitch - _invScrollY, InvTile, InvTile);
        }
        private List<int> GetFilteredInventory()
        {
            var list = new List<int>();
            for (int i = 0; i < _session.Inventory.Count; i++)
                if (_invFilter < 0 || (int)_session.Inventory[i].Slot == _invFilter) list.Add(i);
            return list;
        }

        private const int InfoPanelWidth = 380;
        private const int InfoPanelRightMargin = 60;
        private const int InfoPanelGap = 40;

        // ตอนแผง INFO เปิดอยู่ ดันแค่ช่อง Formation ไปทางซ้ายเล็กน้อย ให้มีระยะห่างจากแผงพอดีๆ ไม่ชิดไม่ห่างเกินไป
        // ค่าเหล่านี้ไล่ค่าอย่างนุ่มนวลตาม _infoAnim (0 = ปิด, 1 = เปิด) แทนการกระโดดทันที
        private float _infoAnim;                 // ความคืบหน้าการเปิด/ปิดแผง INFO
        private Character _infoDisplayChar;      // ตัวละครที่แผงกำลังโชว์ (คงไว้ระหว่างสไลด์ออก)
        private const float InfoAnimSpeed = 11f; // ยิ่งมากยิ่งเร็ว
        private float InfoEase => _infoAnim * _infoAnim * (3f - 2f * _infoAnim); // smoothstep
        private int ContentOffsetX => (int)Math.Round(MathHelper.Lerp(0f, -330f, InfoEase));
        private int StageSpacing => (int)Math.Round(MathHelper.Lerp(310f, 230f, InfoEase));
        private int StageSlotWidth => (int)Math.Round(MathHelper.Lerp(200f, 170f, InfoEase));

        // Front 3 ช่องอยู่แถวหน้า (ล่าง/ใหญ่) ส่วน Back 2 ช่องอยู่แถวหลัง (สูงกว่า/เล็กกว่า) ยืนคั่นกลางระหว่างตัวหน้า
        private static float DepthScale(int slot) => slot < 3 ? 1f : 0.85f;

        private Point GetPlatformCenter(int slot)
        {
            int cx = ScreenWidth / 2 + ContentOffsetX;
            int half = StageSpacing / 2;
            switch (slot)
            {
                case 0: return new Point(cx, 700);
                case 1: return new Point(cx + StageSpacing, 700);
                case 2: return new Point(cx - StageSpacing, 700);
                case 3: return new Point(cx - half, 545);
                default: return new Point(cx + half, 545);
            }
        }

        private Rectangle GetTeamSetupStartButtonRect() => new Rectangle(ScreenWidth / 2 - 140, ScreenHeight - 258, 280, 58);
        private Rectangle GetTeamSetupBackButtonRect() => new Rectangle(40, 34, 170, 54);

        private Rectangle GetFormationSlotRect(int i)
        {
            var p = GetPlatformCenter(i);
            int w = (int)(StageSlotWidth * DepthScale(i));
            int h = (int)(320 * DepthScale(i));
            return new Rectangle(p.X - w / 2, p.Y - h, w, h + 45);
        }

        private Rectangle GetHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _session.AllHeroes.Count * w + (_session.AllHeroes.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        public void Update(GameTime gameTime, MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += delta;

            // แผง INFO: สไลด์เข้า/ออก และฟอร์เมชันค่อยๆ เลื่อนตาม
            if (_infoPanelCharacter != null) _infoDisplayChar = _infoPanelCharacter;
            float infoTarget = _infoPanelCharacter != null ? 1f : 0f;
            _infoAnim += (infoTarget - _infoAnim) * (1f - (float)Math.Exp(-InfoAnimSpeed * delta));
            if (Math.Abs(_infoAnim - infoTarget) < 0.002f) _infoAnim = infoTarget;
            if (_infoAnim <= 0f && _infoPanelCharacter == null) _infoDisplayChar = null;

            if (_showTeamWarning)
            {
                _teamWarningTimer -= delta;
                if (_teamWarningTimer <= 0f) _showTeamWarning = false;
            }

            if (keyboard.IsKeyDown(Keys.Escape)) { _playUIClick?.Invoke(); _selectedHeroIndex = -1; _infoPanelCharacter = null; _infoAnim = 0f; _infoDisplayChar = null; _goBack?.Invoke(); return; }

            _hoveredTeamStartButton = GetTeamSetupStartButtonRect().Contains(mouse.Position);
            _hoveredTeamBackButton = GetTeamSetupBackButtonRect().Contains(mouse.Position);

            _hoveredSlotIndex = -1;
            // วนแถวหลังก่อน แถวหน้าทีหลัง: ถ้าพื้นที่ซ้อนกัน ตัวแถวหน้าจะถูกเลือกก่อน
            foreach (int i in new[] { 3, 4, 0, 1, 2 })
            {
                if (GetFormationSlotRect(i).Contains(mouse.Position)) _hoveredSlotIndex = i;
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _session.AllHeroes.Count; i++)
            {
                if (GetHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            // คลิกขวาที่ตัวละคร (ใน Hero Tray หรือในช่อง Formation) เพื่อเปิด/ปิดแผง INFO เหมือนหน้า Upgrade
            bool rightClicked = mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released;
            if (rightClicked)
            {
                Character rightClickedChar = null;
                if (_hoveredHeroTrayIndex >= 0) rightClickedChar = _session.AllHeroes[_hoveredHeroTrayIndex];
                else if (_hoveredSlotIndex >= 0 && _session.FormationSlots[_hoveredSlotIndex] != null) rightClickedChar = _session.FormationSlots[_hoveredSlotIndex];

                _infoPanelCharacter = (rightClickedChar != null && _infoPanelCharacter == rightClickedChar) ? null : rightClickedChar;
            }
            _previousMouseState = mouse;
            _mousePos = mouse.Position;

            // --- ปุ่มแท็บ Info/Equipment + เนื้อหาแท็บ Equipment ---
            bool panelClick = false;
            _hoveredPanelTab = -1; _hoveredInvItem = -1; _hoveredInvFilter = -1; _hoveredUnequipSlot = -1;
            if (PanelInteractive)
            {
                var pr = GetInfoPanelRect();
                for (int t = 0; t < 2; t++) if (GetPanelTabRect(t).Contains(mouse.Position)) _hoveredPanelTab = t;
                panelClick = leftClicked && (pr.Contains(mouse.Position) || _hoveredPanelTab >= 0);

                if (_hoveredPanelTab >= 0 && leftClicked) { _playUIClick?.Invoke(); _panelTab = _hoveredPanelTab; }
                else if (_panelTab == 1)
                {
                    var c = _infoPanelCharacter;
                    var filtered = GetFilteredInventory();
                    int visibleH = pr.Bottom - 40 - (pr.Y + 305);
                    int totalH = ((filtered.Count + InvCols - 1) / InvCols) * InvPitch;
                    int maxScroll = Math.Max(0, totalH - visibleH);
                    if (pr.Contains(mouse.Position))
                    {
                        if (mouse.ScrollWheelValue > _prevScrollWheel) _invScrollY -= 50;
                        else if (mouse.ScrollWheelValue < _prevScrollWheel) _invScrollY += 50;
                    }
                    _invScrollY = Math.Max(0, Math.Min(_invScrollY, maxScroll));

                    int clipTop = pr.Y + 300, clipBottom = pr.Bottom - 40;
                    for (int pos = 0; pos < filtered.Count; pos++)
                    {
                        var ir = GetInvItemRect(pos);
                        if (ir.Top >= clipTop && ir.Bottom <= clipBottom && ir.Contains(mouse.Position)) _hoveredInvItem = filtered[pos];
                    }
                    for (int f = 0; f < 4; f++) if (GetInvFilterRect(f).Contains(mouse.Position)) _hoveredInvFilter = f;
                    for (int sl = 0; sl < 3; sl++)
                        if (c.GetEquipped((ItemSlot)sl) != null && GetUnequipRect(sl).Contains(mouse.Position)) _hoveredUnequipSlot = sl;

                    if (leftClicked)
                    {
                        if (_hoveredInvFilter >= 0) { _playUIClick?.Invoke(); _invFilter = _hoveredInvFilter - 1; _invScrollY = 0; }
                        else if (_hoveredInvItem >= 0)
                        {
                            _playUIClick?.Invoke();
                            var newItem = _session.Inventory[_hoveredInvItem];
                            var prev = c.EquipItem(newItem);
                            _session.Inventory.RemoveAt(_hoveredInvItem);
                            if (prev != null) _session.Inventory.Add(prev);
                        }
                        else if (_hoveredUnequipSlot >= 0)
                        {
                            _playUIClick?.Invoke();
                            var removed = c.UnequipItem((ItemSlot)_hoveredUnequipSlot);
                            if (removed != null) _session.Inventory.Add(removed);
                        }
                    }
                }
            }
            _prevScrollWheel = mouse.ScrollWheelValue;

            if (leftClicked && !panelClick)
            {
                if (_hoveredTeamBackButton) { _playUIClick?.Invoke(); _selectedHeroIndex = -1; _infoPanelCharacter = null; _infoAnim = 0f; _infoDisplayChar = null; _goBack?.Invoke(); }
                else if (_hoveredTeamStartButton)
                {
                    bool hasCharacter = false;
                    foreach (var c in _session.FormationSlots)
                    {
                        if (c != null) { hasCharacter = true; break; }
                    }

                    if (hasCharacter)
                    {
                        _playUIClick?.Invoke();
                        _selectedHeroIndex = -1;
                        _startBattle?.Invoke();
                    }
                    else
                    {
                        _playUIClick?.Invoke();
                        _showTeamWarning = true;
                        _teamWarningTimer = 2.0f;
                    }
                }
                else if (_hoveredSlotIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    if (!(_session.IsStoryMode && _session.StoryStageIndex == 0))
                    {
                        if (_selectedHeroIndex >= 0)
                        {
                            // มีฮีโร่ที่เลือกค้างไว้จาก Hero Tray -> เอาไปใส่ในช่องที่เพิ่งกด
                            var heroToPlace = _session.AllHeroes[_selectedHeroIndex];

                            // ถ้าฮีโร่ตัวนี้อยู่ในช่องอื่นอยู่แล้ว ให้เอาออกจากช่องเดิมก่อน กันไม่ให้ซ้ำสองช่อง
                            for (int i = 0; i < 5; i++)
                            {
                                if (_session.FormationSlots[i] == heroToPlace) _session.FormationSlots[i] = null;
                            }

                            _session.FormationSlots[_hoveredSlotIndex] = heroToPlace;
                            _selectedHeroIndex = -1;
                        }
                        else
                        {
                            // ไม่มีฮีโร่ที่เลือกค้างไว้ -> กดช่องเพื่อเอาฮีโร่ในช่องนั้นออก (พฤติกรรมเดิม)
                            _session.FormationSlots[_hoveredSlotIndex] = null;
                        }
                    }
                }
                else if (_hoveredHeroTrayIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    if (!(_session.IsStoryMode && _session.StoryStageIndex == 0))
                    {
                        if (_selectedHeroIndex == _hoveredHeroTrayIndex)
                        {
                            // กดตัวที่เลือกอยู่ซ้ำ -> ยกเลิกการเลือก
                            _selectedHeroIndex = -1;
                        }
                        else
                        {
                            // เลือกฮีโร่ตัวนี้ค้างไว้ก่อน รอให้กดเลือกช่องที่จะใส่ในขั้นต่อไป
                            _selectedHeroIndex = _hoveredHeroTrayIndex;
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            string modeTitle = _session.IsStoryMode
                ? $"Story Mode (1-{_session.StoryStageIndex + 1})"
                : (_session.IsNextBattleBoss ? "Challenge Mode - BOSS" : "Challenge Mode");
            DrawTextCentered($"Team Setup - {modeTitle}", new Vector2(ScreenWidth / 2f, 50), Color.Gold, 2.2f);

            DrawStageBackground();

            // วาดจากหลังไปหน้า (แถวที่อยู่สูงกว่าก่อน) ให้ซ้อนกันถูกต้อง
            foreach (int i in new[] { 3, 4, 2, 1, 0 })
            {
                DrawStageSlot(i);
            }

            int lineY = ScreenHeight - 200;
            DrawHeroTrayPanel(lineY);

            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _session.AllHeroes.Count)
            {
                DrawTextCentered($"{_session.AllHeroes[_selectedHeroIndex].Name} selected - tap a platform to place", new Vector2(ScreenWidth / 2f, 110), Color.Gold, 1.1f);
            }

            for (int i = 0; i < _session.AllHeroes.Count; i++)
            {
                DrawHeroCard(i);
            }

            var startRect = GetTeamSetupStartButtonRect();
            DrawStoneButton(startRect, new Color(70, 190, 80), _hoveredTeamStartButton);
            DrawTextCentered("START BATTLE", new Vector2(startRect.Center.X + 2, startRect.Center.Y + 3), Color.Black * 0.6f, 1.25f);
            DrawTextCentered("START BATTLE", new Vector2(startRect.Center.X, startRect.Center.Y), _hoveredTeamStartButton ? Color.Gold : Color.White, 1.25f);

            if (_showTeamWarning)
            {
                DrawTextCentered("Please choose a character to play", new Vector2(startRect.Center.X, startRect.Y - 30), Color.Red, 1.3f);
            }

            var backRect = GetTeamSetupBackButtonRect();
            UiKit.BackButton(_spriteBatch, _pixel, _font, backRect, _hoveredTeamBackButton);

            DrawInfoPanel();
        }

        private void DrawStoneButton(Rectangle r, Color accent, bool hovered) => UiKit.StoneButton(_spriteBatch, _pixel, r, accent, hovered);

        // พื้นหลังถาดฮีโร่: แผ่นมืดไล่สี เส้นทองด้านบน ป้ายชื่อ และคำใบ้
        private void DrawHeroTrayPanel(int lineY)
        {
            var panel = new Rectangle(60, lineY - 14, ScreenWidth - 120, ScreenHeight - (lineY - 14) + 20);
            for (int y = 0; y < panel.Height; y += 4)
            {
                float k = (float)y / panel.Height;
                DrawRect(new Rectangle(panel.X, panel.Y + y, panel.Width, 4), Color.Lerp(new Color(26, 26, 44), new Color(8, 8, 16), k));
            }
            DrawRect(new Rectangle(panel.X, panel.Y, panel.Width, 3), Color.Gold);
            DrawRect(new Rectangle(panel.X, panel.Y + 5, panel.Width, 1), Color.Gold * 0.35f);
            DrawRect(new Rectangle(panel.X, panel.Y, 3, panel.Height), Color.Gold * 0.6f);
            DrawRect(new Rectangle(panel.Right - 3, panel.Y, 3, panel.Height), Color.Gold * 0.6f);

            var plate = new Rectangle(panel.X + 24, panel.Y - 18, 190, 34);
            DrawRect(plate, new Color(22, 18, 16));
            DrawRectBorder(plate, Color.Gold, 2);
            DrawTextCentered("HERO TRAY", new Vector2(plate.Center.X, plate.Center.Y), new Color(255, 205, 130), 1.05f);

            DrawText("Click: select   |   Right-click: info", new Vector2(panel.Right - 360, panel.Y + 12), Color.Gray, 0.7f);
        }

        private Rectangle GetInfoPanelRect()
        {
            int baseX = ScreenWidth - InfoPanelRightMargin - InfoPanelWidth - 300;
            int slide = (int)Math.Round((1f - InfoEase) * (ScreenWidth - baseX)); // เริ่มนอกจอด้านขวาแล้วสไลด์เข้า
            return new Rectangle(baseX + slide, 160, InfoPanelWidth, 600);
        }

        // แผง INFO แบบเดียวกับหน้า Upgrade เปิด/ปิดด้วยการคลิกขวาที่ตัวละคร
        private Color GetHeroColor(string name)
        {
            switch (name)
            {
                case "Alice": return new Color(220, 70, 80);
                case "Elma": return new Color(90, 190, 100);
                case "Lumi": return new Color(170, 100, 230);
                case "Veyra": return new Color(235, 200, 70);
                case "Enos": return new Color(90, 150, 230);
                default: return new Color(150, 150, 170);
            }
        }

        private void DrawStageBackground()
        {
            // พื้นหลังเวที: ไล่สีม่วงเข้มจากบนลงล่าง
            int top = 130, bottom = 845;
            int bands = 28;
            for (int b = 0; b < bands; b++)
            {
                float t = b / (float)(bands - 1);
                Color c = Color.Lerp(new Color(22, 20, 40), new Color(62, 42, 96), t);
                int y0 = top + (bottom - top) * b / bands;
                int y1 = top + (bottom - top) * (b + 1) / bands;
                DrawRect(new Rectangle(0, y0, ScreenWidth, y1 - y0 + 1), c);
            }

            // วงแหวนจางๆ กลางเวที
            var center = new Point(ScreenWidth / 2 + ContentOffsetX, 625);
            DrawEllipseRing(center, 640, 190, 3, new Color(150, 120, 220, 40));
            DrawEllipseRing(center, 520, 150, 2, new Color(150, 120, 220, 30));
        }

        private void FillEllipse(Point c, int rx, int ry, Color color)
        {
            for (int dy = -ry; dy <= ry; dy++)
            {
                float t = dy / (float)ry;
                int half = (int)(rx * Math.Sqrt(Math.Max(0, 1 - t * t)));
                if (half <= 0) continue;
                _spriteBatch.Draw(_pixel, new Rectangle(c.X - half, c.Y + dy, half * 2, 1), color);
            }
        }

        private void DrawEllipseRing(Point c, int rx, int ry, int thickness, Color color)
        {
            int irx = rx - thickness, iry = ry - thickness;
            for (int dy = -ry; dy <= ry; dy++)
            {
                float t = dy / (float)ry;
                int outer = (int)(rx * Math.Sqrt(Math.Max(0, 1 - t * t)));
                if (outer <= 0) continue;

                int inner = 0;
                if (iry > 0 && Math.Abs(dy) < iry)
                {
                    float ti = dy / (float)iry;
                    inner = (int)(irx * Math.Sqrt(Math.Max(0, 1 - ti * ti)));
                }

                if (inner <= 0)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle(c.X - outer, c.Y + dy, outer * 2, 1), color);
                }
                else
                {
                    _spriteBatch.Draw(_pixel, new Rectangle(c.X - outer, c.Y + dy, outer - inner, 1), color);
                    _spriteBatch.Draw(_pixel, new Rectangle(c.X + inner, c.Y + dy, outer - inner, 1), color);
                }
            }
        }

        // หาขอบเขตพิกเซลที่ไม่โปร่งใสของเฟรมแรก เพื่อให้ตัวละครทุกตัวสูงเท่ากันและยืนบนแท่นพอดี
        private Rectangle GetSpriteBounds(Texture2D tex, Rectangle frame)
        {
            if (_spriteBoundsCache.TryGetValue(tex, out var cached)) return cached;

            Rectangle result = frame;
            try
            {
                frame = Rectangle.Intersect(frame, new Rectangle(0, 0, tex.Width, tex.Height));
                var data = new Color[frame.Width * frame.Height];
                tex.GetData(0, frame, data, 0, data.Length);

                int minX = frame.Width, minY = frame.Height, maxX = -1, maxY = -1;
                for (int y = 0; y < frame.Height; y++)
                {
                    for (int x = 0; x < frame.Width; x++)
                    {
                        if (data[y * frame.Width + x].A > 20)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                result = maxX >= 0
                    ? new Rectangle(frame.X + minX, frame.Y + minY, maxX - minX + 1, maxY - minY + 1)
                    : frame;
            }
            catch { }

            _spriteBoundsCache[tex] = result;
            return result;
        }

        private Texture2D GetHeroTexture(Character c, out Rectangle frame)
        {
            frame = Rectangle.Empty;
            if (c.Name == "Elma" && _elmaIdleTex != null) { frame = new Rectangle(0, 0, 160, 320); return _elmaIdleTex; }
            if (c.Name == "Lumi" && _lumiIdleTex != null) { frame = new Rectangle(0, 0, 240, 320); return _lumiIdleTex; }
            if ((c.Name == "Alice" || c.Name == "Swordman") && _aliceIdleTex != null) { frame = new Rectangle(0, 0, 480, 480); return _aliceIdleTex; }
            if (c.Name == "Enos" && _enosIdleTex != null) { frame = Where_My_Slime.Data.SpriteFrames.EnosFrame(_enosIdleTex, 0); return _enosIdleTex; }
            if (c.Name == "Veyra" && _veyraIdleTex != null) { frame = Where_My_Slime.Data.SpriteFrames.VeyraFrame(_veyraIdleTex, 0); return _veyraIdleTex; }
            if (_testCharacterTex != null) { frame = new Rectangle(0, 0, _testCharacterTex.Width, _testCharacterTex.Height); return _testCharacterTex; }
            return null;
        }

        // วาดภาพครึ่งบน (หัว/ลำตัว) ของตัวละครลงในกรอบพอร์เทรตบนการ์ด
        private bool DrawHeroPortrait(Character c, Rectangle box, Color tint)
        {
            if (Where_My_Slime.Data.HeroProfiles.Draw(_spriteBatch, c.Name, box, tint)) return true;
            Texture2D tex = GetHeroTexture(c, out Rectangle frame);
            if (tex == null) return false;

            Rectangle b = GetSpriteBounds(tex, frame);
            float aspect = box.Height / (float)box.Width;
            int srcH = Math.Min(b.Height, (int)(b.Width * aspect));
            if (srcH <= 0) return false;

            var src = new Rectangle(b.X, b.Y, b.Width, srcH);
            int destH = (int)(box.Width * srcH / (float)b.Width);
            var dest = new Rectangle(box.X, box.Y, box.Width, Math.Min(destH, box.Height));
            if (destH > box.Height) src.Height = (int)(b.Width * aspect);
            _spriteBatch.Draw(tex, dest, src, tint);
            return true;
        }

        private void DrawHeroSprite(Character c, Point platform, float alpha, int slot)
        {
            Texture2D tex = GetHeroTexture(c, out Rectangle frame);
            if (tex == null) return;

            Rectangle src = GetSpriteBounds(tex, frame);
            float targetHeight = 300f * DepthScale(slot);
            float scale = targetHeight / src.Height;
            float maxW = (StageSlotWidth - 10) * DepthScale(slot);
            if (src.Width * scale > maxW) scale = maxW / src.Width;

            float w = src.Width * scale;
            float h = src.Height * scale;
            float bob = (float)Math.Sin(_time * 2.2f + platform.X * 0.01f) * 3f;
            var dest = new Rectangle((int)(platform.X - w / 2f), (int)(platform.Y - 10 - h + bob), (int)w, (int)h);
            _spriteBatch.Draw(tex, dest, src, Color.White * alpha);
        }

        private void DrawStageSlot(int i)
        {
            var p = GetPlatformCenter(i);
            bool hovered = _hoveredSlotIndex == i;
            bool awaiting = _selectedHeroIndex >= 0;
            Character c = _session.FormationSlots[i];
            bool isFront = i < 3;

            float pulse = 0.5f + 0.5f * (float)Math.Sin(_time * 3.5f);
            Color glow = hovered ? Color.Gold : (awaiting ? new Color(235, 200, 90) : (c != null ? GetHeroColor(c.Name) : new Color(120, 130, 200)));

            float ds = DepthScale(i);
            int rx = (int)((StageSlotWidth / 2 + 10) * ds);
            int ry = (int)(34 * ds);

            // แสงเรืองรอบแท่น
            FillEllipse(p, rx + 18, ry + 10, glow * (0.10f + (awaiting || hovered ? 0.12f * pulse : 0f)));
            FillEllipse(p, rx + 8, ry + 5, glow * 0.16f);
            // แท่น
            FillEllipse(p, rx, ry, new Color(18, 18, 34, 235));
            DrawEllipseRing(p, rx, ry, 4, glow * (hovered || awaiting ? (0.7f + 0.3f * pulse) : 0.85f));
            DrawEllipseRing(p, rx - 22, ry - 9, 2, glow * 0.45f);

            if (c != null)
            {
                DrawHeroSprite(c, p, 1f, i);
                DrawTextCentered(c.Name, new Vector2(p.X, p.Y + ry + 22), Color.White, 1.05f);
                DrawTextCentered($"HP:{c.MaxHp}", new Vector2(p.X, p.Y + ry + 48), Color.LightGreen, 0.8f);
            }
            else
            {
                DrawTextCentered("+", new Vector2(p.X, p.Y - 70 * ds), glow * (0.6f + 0.3f * pulse), 3f);
            }

            DrawTextCentered(isFront ? "FRONT" : "BACK", new Vector2(p.X, p.Y + ry + (c != null ? 72 : 30)), isFront ? new Color(255, 170, 120) : new Color(140, 190, 255), 0.7f);
        }

        private void DrawHeroCard(int i)
        {
            var hero = _session.AllHeroes[i];
            var r = GetHeroTrayRect(i);
            bool hovered = _hoveredHeroTrayIndex == i;
            bool selected = _selectedHeroIndex == i;
            bool inTeam = false;
            for (int s = 0; s < 5; s++) { if (_session.FormationSlots[s] == hero) { inTeam = true; break; } }

            Color heroColor = GetHeroColor(hero.Name);
            float dim = inTeam ? 0.4f : 1f;
            int lift = (hovered || selected) ? -8 : 0;
            r.Y += lift;

            DrawRect(r, new Color(14, 14, 26));
            // ส่วนภาพบนการ์ด: ไล่สีตามสีประจำตัวละคร + ตัวอักษรย่อใหญ่
            int portraitH = r.Height - 36;
            for (int b = 0; b < 6; b++)
            {
                float t = b / 5f;
                Color band = Color.Lerp(heroColor * 0.75f, heroColor * 0.25f, t) * dim;
                int y0 = r.Y + 3 + portraitH * b / 6;
                int y1 = r.Y + 3 + portraitH * (b + 1) / 6;
                DrawRect(new Rectangle(r.X + 3, y0, r.Width - 6, y1 - y0), new Color(band.R, band.G, band.B, (byte)255));
            }
            var portraitBox = new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, portraitH);
            if (!DrawHeroPortrait(hero, portraitBox, Color.White * (inTeam ? 0.4f : 1f)))
                DrawTextCentered(hero.Name.Substring(0, 1), new Vector2(r.Center.X, r.Y + portraitH / 2f + 4), Color.White * (inTeam ? 0.45f : 0.9f), 3.2f);

            // แถบชื่อ + HP ด้านล่าง
            var bar = new Rectangle(r.X + 3, r.Bottom - 36, r.Width - 6, 33);
            DrawRect(bar, new Color(10, 10, 18, 235));
            DrawTextCentered(hero.Name, new Vector2(bar.Center.X, bar.Y + 10), inTeam ? Color.Gray : Color.White, 0.85f);
            DrawTextCentered(inTeam ? "IN TEAM" : $"HP:{hero.MaxHp}", new Vector2(bar.Center.X, bar.Y + 25), inTeam ? Color.Gray : Color.LightGreen, 0.6f);

            Color border = selected ? Color.Gold : (hovered ? Color.Gold : (inTeam ? Color.Gray : heroColor));
            if (selected || hovered)
                DrawRect(new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), Color.Gold * (selected ? 0.22f : 0.14f));
            DrawRectBorder(r, border, selected ? 4 : (hovered ? 3 : 2));
            DrawRectBorder(new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), Color.Black * 0.55f, 2);
            int st = 6;
            foreach (var p in new[] { new Point(r.X - 3, r.Y - 3), new Point(r.Right - st + 3, r.Y - 3), new Point(r.X - 3, r.Bottom - st + 3), new Point(r.Right - st + 3, r.Bottom - st + 3) })
                DrawRect(new Rectangle(p.X, p.Y, st, st), inTeam ? new Color(90, 90, 100) : Color.Lerp(heroColor, Color.White, 0.5f));
        }

        private void DrawInfoPanel()
        {
            if (_infoDisplayChar == null || _infoAnim <= 0f) return;
            var c = _infoDisplayChar;

            var infoRect = GetInfoPanelRect();
            DrawPanelTabs();
            DrawRect(infoRect, new Color(20, 20, 30, 240));
            DrawRectBorder(infoRect, Color.Gold, 2);

            if (_panelTab == 1) { DrawEquipmentPanel(c, infoRect); return; }

            DrawText("INFO", new Vector2(infoRect.X + 15, infoRect.Y + 12), Color.Gold, 0.9f);
            DrawText(c.Name, new Vector2(infoRect.X + 25, infoRect.Y + 45), Color.White, 1.2f);

            int statStartY = infoRect.Y + 95;
            int rowGap = 38;
            int col2X = infoRect.X + 210;

            DrawText($"HP: {c.MaxHp}", new Vector2(infoRect.X + 25, statStartY + rowGap * 0), Color.White, 0.9f);
            DrawText($"P. DEF: {c.PDef}", new Vector2(col2X, statStartY + rowGap * 0), Color.White, 0.9f);

            DrawText($"P. ATK: {c.PAtk}", new Vector2(infoRect.X + 25, statStartY + rowGap * 1), Color.White, 0.9f);
            DrawText($"M. ATK: {c.MAtk}", new Vector2(col2X, statStartY + rowGap * 1), Color.White, 0.9f);

            DrawText($"M. DEF: {c.MDef}", new Vector2(infoRect.X + 25, statStartY + rowGap * 2), Color.White, 0.9f);
            DrawText($"CRIT RATE: {(int)c.CritRate}%", new Vector2(col2X, statStartY + rowGap * 2), Color.White, 0.9f);

            DrawText($"CRIT DMG: {(int)c.CritDamage}%", new Vector2(infoRect.X + 25, statStartY + rowGap * 3), Color.White, 0.9f);
            DrawText($"SPEED: {c.Speed}", new Vector2(col2X, statStartY + rowGap * 3), Color.White, 0.9f);

            DrawText($"ACC: {(int)c.Accuracy}%", new Vector2(infoRect.X + 25, statStartY + rowGap * 4), Color.White, 0.9f);

            int ultimateY = statStartY + rowGap * 5 + 20;
            DrawUltimateIcon(new Rectangle(infoRect.X + 25, ultimateY - 6, 40, 40), c.Name);
            DrawText("Ultimate Skill", new Vector2(infoRect.X + 78, ultimateY + 2), Color.Gold, 0.95f);
            DrawWrappedText(GetUltimateSkillDescription(c.Name), new Vector2(infoRect.X + 25, ultimateY + 52), infoRect.Width - 50, Color.White, 0.8f);

            DrawText("(Right-click again to close)", new Vector2(infoRect.X + 15, infoRect.Bottom - 30), Color.Gray, 0.75f);
        }

        // ปุ่มไอคอน Info (i) และ Equipment (ดาบ) วางซ้ายของแผง
        private void DrawPanelTabs()
        {
            for (int t = 0; t < 2; t++)
            {
                var r = GetPanelTabRect(t);
                bool active = _panelTab == t;
                UiKit.StoneButton(_spriteBatch, _pixel, r, active ? new Color(220, 170, 30) : new Color(50, 50, 75), _hoveredPanelTab == t);
                if (t == 0)
                {
                    DrawRect(new Rectangle(r.X + 24, r.Y + 10, 7, 7), Color.White);
                    DrawRect(new Rectangle(r.X + 24, r.Y + 21, 7, 24), Color.White);
                    DrawRectBorder(new Rectangle(r.X + 24, r.Y + 21, 7, 24), Color.Black, 1);
                }
                else
                {
                    DrawRect(new Rectangle(r.X + 24, r.Y + 8, 6, 26), Color.White);
                    DrawRectBorder(new Rectangle(r.X + 24, r.Y + 8, 6, 26), Color.Black, 1);
                    DrawRect(new Rectangle(r.X + 14, r.Y + 32, 26, 6), Color.Black);
                    DrawRectBorder(new Rectangle(r.X + 14, r.Y + 32, 26, 6), Color.White, 1);
                    DrawRect(new Rectangle(r.X + 24, r.Y + 38, 6, 9), Color.Black);
                    DrawRectBorder(new Rectangle(r.X + 24, r.Y + 38, 6, 9), Color.White, 1);
                }
            }
        }

        private Color GetItemRarityColor(ItemRarity rarity) => rarity switch
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

        private void DrawEquipmentPanel(Character c, Rectangle pr)
        {
            DrawText("Equipment", new Vector2(pr.X + 15, pr.Y + 12), Color.Gold, 0.9f);
            DrawText(c.Name, new Vector2(pr.X + 25, pr.Y + 40), Color.White, 1.2f);

            Item tip = null;
            for (int sl = 0; sl < 3; sl++)
            {
                var slot = GetEquipSlotRect(sl);
                var eq = c.GetEquipped((ItemSlot)sl);
                DrawTextCentered(SlotLabels[sl], new Vector2(slot.Center.X, slot.Y - 12), Color.LightGray, 0.65f);
                DrawRect(slot, new Color(40, 40, 62));
                DrawRectBorder(slot, eq != null ? Color.LimeGreen : Color.Gray, 2);
                if (eq != null)
                {
                    var icon = new Rectangle(slot.X + 8, slot.Y + 8, slot.Width - 16, slot.Height - 16);
                    if (!ItemIcons.Draw(_spriteBatch, eq, icon))
                        DrawTextCentered(eq.Name, new Vector2(slot.Center.X, slot.Center.Y), Color.LimeGreen, 0.5f);
                    var ub = GetUnequipRect(sl);
                    UiKit.StoneButton(_spriteBatch, _pixel, ub, new Color(170, 55, 55), _hoveredUnequipSlot == sl, 1);
                    DrawTextCentered("Unequip", new Vector2(ub.Center.X, ub.Center.Y), Color.White, 0.65f);
                    if (slot.Contains(_mousePos)) tip = eq;
                }
                else DrawTextCentered("+", new Vector2(slot.Center.X, slot.Center.Y), Color.Gray, 1.8f);
            }

            DrawText("Inventory", new Vector2(pr.X + 20, pr.Y + 266), Color.White, 0.8f);
            for (int f = 0; f < 4; f++)
            {
                var fr = GetInvFilterRect(f);
                bool active = (f - 1) == _invFilter;
                UiKit.StoneButton(_spriteBatch, _pixel, fr, active ? new Color(220, 170, 30) : new Color(50, 50, 75), _hoveredInvFilter == f, 1);
                DrawTextCentered(FilterLabels[f], new Vector2(fr.Center.X, fr.Center.Y), active ? Color.Black : Color.White, 0.6f);
            }

            var filtered = GetFilteredInventory();
            if (filtered.Count == 0)
                DrawText(_session.Inventory.Count == 0 ? "No items in inventory" : "No items in this category", new Vector2(pr.X + 25, pr.Y + 315), Color.Gray, 0.8f);

            int clipTop = pr.Y + 300, clipBottom = pr.Bottom - 40;
            for (int pos = 0; pos < filtered.Count; pos++)
            {
                var ir = GetInvItemRect(pos);
                if (ir.Top < clipTop || ir.Bottom > clipBottom) continue;
                int row = filtered[pos];
                UiKit.StoneButton(_spriteBatch, _pixel, ir, new Color(40, 40, 60), _hoveredInvItem == row, 2);
                var it = _session.Inventory[row];
                var icon = new Rectangle(ir.X + 5, ir.Y + 5, ir.Width - 10, ir.Height - 10);
                if (!ItemIcons.Draw(_spriteBatch, it, icon))
                    DrawTextCentered(it.Name, new Vector2(ir.Center.X, ir.Center.Y), Color.Gold, 0.4f);
            }
            if (_hoveredInvItem >= 0 && _hoveredInvItem < _session.Inventory.Count) tip = _session.Inventory[_hoveredInvItem];

            DrawText("(Right-click again to close)", new Vector2(pr.X + 15, pr.Bottom - 30), Color.Gray, 0.75f);

            if (tip != null)
            {
                const int w = 320, h = 120;
                int x = _mousePos.X + 20, y = _mousePos.Y + 20;
                if (x + w > ScreenWidth - 10) x = _mousePos.X - w - 20;
                if (y + h > ScreenHeight - 10) y = _mousePos.Y - h - 20;
                var box = new Rectangle(x, y, w, h);
                Color rc = GetItemRarityColor(tip.Rarity);
                DrawRect(box, new Color(15, 15, 25, 245));
                DrawRectBorder(box, rc, 2);
                DrawText(tip.Name, new Vector2(box.X + 14, box.Y + 10), rc, 1.0f);
                DrawText($"[{tip.Rarity}]  {tip.Slot}", new Vector2(box.X + 14, box.Y + 40), Color.LightGray, 0.8f);
                DrawWrappedText(tip.Description, new Vector2(box.X + 14, box.Y + 68), w - 28, Color.White, 0.8f, 24f);
            }
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

        private void DrawText(string text, Vector2 position, Color color, float scale = 1f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            try { _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); }
            catch (ArgumentException) { _spriteBatch.DrawString(_font, GetSafeText(text), position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); }
        }

        private void DrawTextCentered(string text, Vector2 center, Color color, float scale = 1f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            Vector2 size;
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeText(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }

        // ไอคอนสี่เหลี่ยมของ Ultimate Skill วาดจากพิกเซล 8x8 ด้วยโค้ด (ไม่ต้องพึ่งไฟล์รูป) แต่ละตัวละครมีลายของตัวเอง
        private static readonly string[] IconShield =
        {
            ".######.", ".######.", ".######.", ".######.", "..####..", "..####..", "...##...", "........"
        };
        private static readonly string[] IconArrow =
        {
            "......##", ".....###", "....#.##", "...#....", "..#.....", ".#......", "#.......", "........"
        };
        private static readonly string[] IconCross =
        {
            "...##...", "...##...", ".######.", ".######.", "...##...", "...##...", "...##...", "........"
        };
        private static readonly string[] IconStar =
        {
            "...##...", "...##...", "########", ".######.", "..####..", ".##..##.", "##....##", "........"
        };
        private static readonly string[] IconSword =
        {
            "......##", ".....##.", "....##..", "#..##...", ".###....", "..##....", ".#.#....", "#...#..."
        };

        private void DrawUltimateIcon(Rectangle rect, string characterName)
        {
            string[] glyph = IconStar;
            Color glyphColor = Color.Gold;
            string n = characterName ?? "";

            if (n.Contains("Enos")) { glyph = IconShield; glyphColor = new Color(120, 180, 255); }
            else if (n.Contains("Elma")) { glyph = IconArrow; glyphColor = new Color(140, 230, 120); }
            else if (n.Contains("Veyra")) { glyph = IconCross; glyphColor = new Color(255, 120, 150); }
            else if (n.Contains("Lumi")) { glyph = IconStar; glyphColor = new Color(190, 140, 255); }
            else if (n.Contains("Alice")) { glyph = IconSword; glyphColor = new Color(255, 170, 90); }

            DrawRect(rect, new Color(50, 50, 50, 230));
            DrawRectBorder(rect, Color.Gold, 3);

            // ใช้ไอคอนสกิลจากโฟลเดอร์ Ui_Skill ถ้ามี (ไม่มีก็วาดพิกเซลเดิม)
            if (SkillUiArt.TryDrawIcon(_spriteBatch, characterName, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8))) return;

            int inner = rect.Width - 10;
            int cell = Math.Max(1, inner / 8);
            int offset = (rect.Width - cell * 8) / 2;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (glyph[y][x] == '#')
                        DrawRect(new Rectangle(rect.X + offset + x * cell, rect.Y + offset + y * cell, cell, cell), glyphColor);
                }
            }
        }

        private string GetUltimateSkillDescription(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return "";

            if (characterName.Contains("Enos"))
                return "Draws the enemies' attention (self only, no target needed). Enos gains Aggro for 2 turns: every enemy must attack Enos.";
            if (characterName.Contains("Elma"))
                return "Fires a powerful, precise shot that deals 250% Physical ATK damage to a single enemy.";
            if (characterName.Contains("Veyra"))
                return "Channels healing light that restores 25% of Veyra's own Max HP, plus 2% of each ally's own Max HP, to every ally at once, and removes 1 debuff from the chosen target.";
            if (characterName.Contains("Lumi"))
                return "Unleashes a wave of magic that deals 110% Magic ATK damage to every enemy on the battlefield.";
            if (characterName.Contains("Alice"))
                return "Enters a focused battle stance (self only, no target needed). The gauge stays at 100% and drops 20% with every attack she makes, so the buff lasts 5 attacks: Physical ATK +10%, Physical Defense +20%, Speed +30%, Crit Rate +25%, Crit Damage +50%.";

            return "";
        }

        private void DrawWrappedText(string text, Vector2 position, float maxWidth, Color color, float scale = 1f, float lineHeight = 26f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;

            string[] words = text.Split(' ');
            string line = "";
            float y = position.Y;

            foreach (var word in words)
            {
                string testLine = line.Length == 0 ? word : line + " " + word;
                float testWidth;
                try { testWidth = _font.MeasureString(GetSafeText(testLine)).X * scale; } catch (ArgumentException) { testWidth = 0; }

                if (testWidth > maxWidth && line.Length > 0)
                {
                    DrawText(line, new Vector2(position.X, y), color, scale);
                    line = word;
                    y += lineHeight;
                }
                else
                {
                    line = testLine;
                }
            }

            if (line.Length > 0)
                DrawText(line, new Vector2(position.X, y), color, scale);
        }
    }
}