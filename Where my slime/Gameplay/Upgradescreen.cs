using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Data;
using Where_My_Slime.Models;

namespace Where_My_Slime
{
    // หน้าอัพเกรดตัวละคร: แจกแต้มสเตตัส (STR/INT/VIT/DEX/LUX/CRT) หรือใส่/ถอดไอเทม
    public class UpgradeScreen
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

        private readonly Action _onDone;
        private readonly Action _playUIClick;

        private int _upgradeSelectedIndex = -1;
        private int _upgradeSelectedTab = 0;
        private int _inventoryScrollY = 0;
        private int _previousScrollWheelValue;

        private bool _hoveredStatTab;
        private bool _hoveredEquipTab;
        private int _hoveredHeroTrayIndex = -1;
        private int _hoveredStatPlusRow = -1;
        private int _hoveredStatMinusRow = -1;
        private bool _hoveredConfirmButton;
        private bool _hoveredContinue;
        private int _hoveredRestItemRow = -1;
        private int _hoveredUnequipSlot = -1;

        // ตัวกรองคลังไอเท็ม: -1 = All, 0 = Weapon, 1 = Armor, 2 = Additional
        private int _invFilter = -1;
        private int _hoveredInvFilter = -1;
        private static readonly string[] FilterLabels = { "All", "Weapon", "Armor", "Additional" };
        private static readonly int[] FilterWidths = { 52, 84, 74, 104 };

        private static readonly GrowthStat[] GrowthStats =
            { GrowthStat.STR, GrowthStat.INT, GrowthStat.VIT, GrowthStat.DEX, GrowthStat.LUX, GrowthStat.CRT };
        private int[] _pendingAllocations = new int[GrowthStats.Length];

        private bool _isInputActive = false;
        private bool _inputFresh = false;
        private string _inputBuffer = "1";

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public UpgradeScreen(
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
            Action onDone,
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
            _onDone = onDone;
            _playUIClick = playUIClick;
        }

        // เรียกก่อนเข้าหน้า Upgrade ใหม่ทุกครั้ง เพื่อล้างการเลือกตัวละคร/แท็บ/แต้มที่กำลังจัดสรรค้างอยู่
        public void EnterFresh()
        {
            _upgradeSelectedIndex = -1;
            _upgradeSelectedTab = 0;
            ResetPendingAllocations();
            _layoutShiftX = GetTargetShift(); // เข้าหน้าใหม่ไม่ต้องสไลด์
        }

        // --- เลย์เอาต์เลื่อนแนวนอน ---
        // ยังไม่ได้เปิด Stat/Equipment: ชุด (แท็บ + การ์ด + INFO) อยู่กึ่งกลางหน้าจอ
        // กด Stat/Equipment: เลื่อนกลับไปตำแหน่งเดิมเพื่อเว้นที่ให้แผงด้านขวา
        private float _layoutShiftX;
        private const float LayoutShiftEase = 0.18f;
        private const float ShiftHeroClosed = 445f;  // ชุดแท็บ+การ์ด+INFO (x 120..910) อยู่กลางจอ
        private const float ShiftNoHero = 590f;      // เหลือการ์ดเปล่าใบเดียว (กึ่งกลาง x=370) อยู่กลางจอ
        private int Shift => (int)Math.Round(_layoutShiftX);

        private float GetTargetShift()
        {
            bool heroSelected = _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count;
            if (!heroSelected) return ShiftNoHero;
            return _upgradeSelectedTab >= 0 ? 0f : ShiftHeroClosed;
        }

        private Rectangle GetUpgradeStatTabRect() => new Rectangle(120 + Shift, 320, 70, 70);
        private Rectangle GetUpgradeEquipTabRect() => new Rectangle(120 + Shift, 410, 70, 70);
        private Rectangle GetUpgradeCharacterCardRect() => new Rectangle(230 + Shift, 220, 280, 520);
        private Rectangle GetUpgradeInfoPanelRect() => new Rectangle(530 + Shift, 220, 380, 520);
        private Rectangle GetUpgradeRightPanelRect() => new Rectangle(930 + Shift, 220, 520, 520);

        private Rectangle GetStatRowMinusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 150, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetStatRowPlusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 80, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetUpgradeConfirmButtonRect() => new Rectangle(GetUpgradeRightPanelRect().X + 30, GetUpgradeRightPanelRect().Bottom - 80, GetUpgradeRightPanelRect().Width - 60, 50);

        private Rectangle GetUpgradePtsBoxRect()
        {
            var rightRect = GetUpgradeRightPanelRect();
            return new Rectangle(rightRect.Right - 130, rightRect.Y + 12, 105, 36);
        }

        // ช่องสวมใส่ 3 ช่อง: 0 = Weapon, 1 = Armor, 2 = Additional เรียงในแนวนอน
        private static readonly string[] SlotLabels = { "WEAPON", "ARMOR", "ADDITIONAL" };

        private Rectangle GetEquipItemSlotRect(int slot)
        {
            var panel = GetUpgradeRightPanelRect();
            const int size = 100, gap = 40;
            int totalWidth = 3 * size + 2 * gap;
            int startX = panel.Center.X - totalWidth / 2;
            return new Rectangle(startX + slot * (size + gap), panel.Y + 90, size, size);
        }

        // คลังไอเท็มแสดงเป็นช่องไอคอนเรียงเป็นตาราง 5 คอลัมน์ (ชื่อ/คำอธิบายจะโชว์ตอนเอาเมาส์ไปชี้)
        private const int InvTile = 72;
        private const int InvPitch = 82;
        private const int InvCols = 5;

        private Rectangle GetEquipInventoryItemRect(int index, int scrollY)
        {
            var panel = GetUpgradeRightPanelRect();
            int gridWidth = InvCols * InvPitch - (InvPitch - InvTile);
            int startX = panel.X + (panel.Width - gridWidth) / 2;
            int col = index % InvCols;
            int row = index / InvCols;
            return new Rectangle(startX + col * InvPitch, panel.Y + 280 + row * InvPitch - scrollY, InvTile, InvTile);
        }

        // ปุ่มกรองคลัง: 0 = All, 1 = Weapon, 2 = Armor, 3 = Additional เรียงชิดขวาของแผงบนแถวหัวข้อ Inventory
        private Rectangle GetInvFilterRect(int i)
        {
            var panel = GetUpgradeRightPanelRect();
            const int gap = 6;
            int total = gap * 3;
            foreach (var w in FilterWidths) total += w;
            int x = panel.Right - 25 - total;
            for (int k = 0; k < i; k++) x += FilterWidths[k] + gap;
            return new Rectangle(x, panel.Y + 232, FilterWidths[i], 30);
        }

        // ดัชนีจริงในคลังของไอเท็มที่ผ่านตัวกรองปัจจุบัน (เรียงตามลำดับในคลัง)
        private System.Collections.Generic.List<int> GetFilteredInventory()
        {
            var list = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _session.Inventory.Count; i++)
            {
                if (_invFilter < 0 || (int)_session.Inventory[i].Slot == _invFilter) list.Add(i);
            }
            return list;
        }

        private Rectangle GetEquipUnequipButtonRect(int slot)
        {
            var s = GetEquipItemSlotRect(slot);
            return new Rectangle(s.X, s.Bottom + 8, s.Width, 32);
        }

        private Rectangle GetUpgradeHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _session.Party.Count * w + (_session.Party.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private Rectangle GetContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard, KeyboardState previousKeyboard)
        {
            float shiftTarget = GetTargetShift();
            _layoutShiftX = MathHelper.Lerp(_layoutShiftX, shiftTarget, LayoutShiftEase);
            if (Math.Abs(_layoutShiftX - shiftTarget) < 0.5f) _layoutShiftX = shiftTarget;

            _hoveredStatTab = GetUpgradeStatTabRect().Contains(mouse.Position);
            _hoveredEquipTab = GetUpgradeEquipTabRect().Contains(mouse.Position);

            if (leftClicked)
            {
                var ptsBoxRect = GetUpgradePtsBoxRect();
                if (ptsBoxRect.Contains(mouse.Position))
                {
                    if (!_isInputActive) _inputFresh = true; // first digit replaces the old number
                    _isInputActive = true;
                }
                else
                {
                    _isInputActive = false;
                }
            }

            if (_isInputActive && _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selected = _session.Party[_upgradeSelectedIndex];
                int pendingUsed = SumPendingAllocations();
                int maxAvailable = selected.UpgradePoints - pendingUsed;

                Keys[] pressedKeys = keyboard.GetPressedKeys();
                foreach (var key in pressedKeys)
                {
                    if (!previousKeyboard.IsKeyDown(key))
                    {
                        int digitVal = -1;
                        if (key >= Keys.D0 && key <= Keys.D9) digitVal = key - Keys.D0;
                        else if (key >= Keys.NumPad0 && key <= Keys.NumPad9) digitVal = key - Keys.NumPad0;

                        if (digitVal >= 0)
                        {
                            if (_inputFresh) { _inputBuffer = ""; _inputFresh = false; }
                            if (_inputBuffer.Length < 6) _inputBuffer += digitVal.ToString();
                            _inputBuffer = _inputBuffer.TrimStart('0');
                        }
                        else if (key == Keys.Back || key == Keys.Delete)
                        {
                            _inputFresh = false;
                            if (_inputBuffer.Length > 0) _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                        }
                    }
                }

                // Empty box is allowed while typing (treated as 1); never exceed the points left
                if (_inputBuffer != "" && int.TryParse(_inputBuffer, out int val))
                {
                    if (val > maxAvailable) _inputBuffer = Math.Max(maxAvailable, 0).ToString().TrimStart('0');
                }
                else if (_inputBuffer != "")
                {
                    _inputBuffer = "";
                }
            }
            if (leftClicked && GetUpgradeCharacterCardRect().Contains(mouse.Position))
            {
                if (_upgradeSelectedIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    _upgradeSelectedIndex = -1;
                    _upgradeSelectedTab = 0;
                    ResetPendingAllocations();
                }
            }

            if (leftClicked && _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                if (_hoveredStatTab)
                {
                    _playUIClick?.Invoke();
                    if (_upgradeSelectedTab == 0) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 0; ResetPendingAllocations(); }
                }
                else if (_hoveredEquipTab)
                {
                    _playUIClick?.Invoke();
                    if (_upgradeSelectedTab == 1) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 1; ResetPendingAllocations(); }
                }
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _session.Party.Count; i++)
            {
                if (GetUpgradeHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            if (leftClicked && _hoveredHeroTrayIndex >= 0 && _hoveredHeroTrayIndex != _upgradeSelectedIndex)
            {
                _playUIClick?.Invoke();
                // เลือกตัวละครครั้งแรก: เริ่มแบบจัดกลางจอ (ยังไม่เปิด Stat/Equipment) ถ้าเปลี่ยนตัวระหว่างเปิดแท็บอยู่ ให้คงแท็บเดิม
                if (_upgradeSelectedIndex < 0) _upgradeSelectedTab = -1;
                _upgradeSelectedIndex = _hoveredHeroTrayIndex;
                ResetPendingAllocations();
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count && _upgradeSelectedTab >= 0)
            {
                var selected = _session.Party[_upgradeSelectedIndex];

                if (_upgradeSelectedTab == 0)
                {
                    int pendingUsed = SumPendingAllocations();
                    int remaining = selected.UpgradePoints - pendingUsed;

                    _hoveredStatPlusRow = -1;
                    _hoveredStatMinusRow = -1;
                    _hoveredConfirmButton = false;

                    for (int row = 0; row < GrowthStats.Length; row++)
                    {
                        if (GetStatRowPlusButtonRect(row).Contains(mouse.Position)) _hoveredStatPlusRow = row;
                        if (GetStatRowMinusButtonRect(row).Contains(mouse.Position)) _hoveredStatMinusRow = row;
                    }
                    _hoveredConfirmButton = pendingUsed > 0 && GetUpgradeConfirmButtonRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredStatPlusRow >= 0 && remaining > 0)
                        {
                            _playUIClick?.Invoke();
                            int addAmount = 1;
                            if (int.TryParse(_inputBuffer, out int parsedInput) && parsedInput > 0)
                            {
                                addAmount = Math.Min(parsedInput, remaining);
                            }
                            _pendingAllocations[_hoveredStatPlusRow] += addAmount;
                        }
                        else if (_hoveredStatMinusRow >= 0 && _pendingAllocations[_hoveredStatMinusRow] > 0)
                        {
                            _playUIClick?.Invoke();
                            int subAmount = 1;
                            if (int.TryParse(_inputBuffer, out int parsedInput) && parsedInput > 0)
                            {
                                subAmount = Math.Min(parsedInput, _pendingAllocations[_hoveredStatMinusRow]);
                            }
                            _pendingAllocations[_hoveredStatMinusRow] -= subAmount;
                        }
                        else if (_hoveredConfirmButton)
                        {
                            _playUIClick?.Invoke();
                            for (int row = 0; row < GrowthStats.Length; row++)
                            {
                                for (int n = 0; n < _pendingAllocations[row]; n++) selected.ApplyGrowthPoint(GrowthStats[row]);
                            }
                            selected.UpgradePoints -= pendingUsed;
                            ResetPendingAllocations();
                        }
                    }
                }
                else if (_upgradeSelectedTab == 1)
                {
                    var rightRect = GetUpgradeRightPanelRect();

                    // สโครลด้วยล้อเมาส์เฉพาะตอนที่เมาส์อยู่เหนือกรอบ Equipment เท่านั้น กันไม่ให้
                    // สโครลไอเทมไปโดยไม่ตั้งใจตอนเลื่อนเมาส์ผ่านส่วนอื่นของหน้าจอ
                    int currentScrollWheel = mouse.ScrollWheelValue;
                    if (rightRect.Contains(mouse.Position))
                    {
                        if (currentScrollWheel > _previousScrollWheelValue) _inventoryScrollY -= 50;
                        else if (currentScrollWheel < _previousScrollWheelValue) _inventoryScrollY += 50;
                    }
                    _previousScrollWheelValue = currentScrollWheel;

                    int visibleHeight = GetUpgradeRightPanelRect().Height - 280;
                    int totalHeight = ((GetFilteredInventory().Count + InvCols - 1) / InvCols) * InvPitch;
                    int maxScroll = Math.Max(0, totalHeight - visibleHeight + 20);
                    if (_inventoryScrollY < 0) _inventoryScrollY = 0;
                    if (_inventoryScrollY > maxScroll) _inventoryScrollY = maxScroll;

                    _hoveredRestItemRow = -1;
                    _hoveredUnequipSlot = -1;

                    int clipTop = rightRect.Y + 270;
                    int clipBottom = rightRect.Bottom - 10;

                    var filteredInv = GetFilteredInventory();
                    for (int pos = 0; pos < filteredInv.Count; pos++)
                    {
                        var itemRow = GetEquipInventoryItemRect(pos, _inventoryScrollY);

                        if (itemRow.Top >= clipTop && itemRow.Bottom <= clipBottom)
                        {
                            if (itemRow.Contains(mouse.Position)) _hoveredRestItemRow = filteredInv[pos];
                        }
                    }

                    _hoveredInvFilter = -1;
                    for (int f = 0; f < 4; f++)
                    {
                        if (GetInvFilterRect(f).Contains(mouse.Position)) _hoveredInvFilter = f;
                    }

                    for (int sl = 0; sl < 3; sl++)
                    {
                        if (selected.GetEquipped((ItemSlot)sl) != null && GetEquipUnequipButtonRect(sl).Contains(mouse.Position))
                            _hoveredUnequipSlot = sl;
                    }

                    if (leftClicked)
                    {
                        if (_hoveredInvFilter >= 0)
                        {
                            _playUIClick?.Invoke();
                            _invFilter = _hoveredInvFilter - 1;
                            _inventoryScrollY = 0;
                        }
                        else if (_hoveredRestItemRow >= 0)
                        {
                            _playUIClick?.Invoke();
                            var newItem = _session.Inventory[_hoveredRestItemRow];
                            // สวมลงช่องตามชนิดของไอเท็ม (Weapon/Armor/Additional) ของเดิมในช่องนั้นจะกลับเข้าคลัง
                            var previousItem = selected.EquipItem(newItem);
                            _session.Inventory.RemoveAt(_hoveredRestItemRow);
                            if (previousItem != null) _session.Inventory.Add(previousItem);
                        }
                        else if (_hoveredUnequipSlot >= 0)
                        {
                            _playUIClick?.Invoke();
                            var removedItem = selected.UnequipItem((ItemSlot)_hoveredUnequipSlot);
                            if (removedItem != null) _session.Inventory.Add(removedItem);
                        }
                    }
                }
            }

            _hoveredContinue = GetContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredContinue)
            {
                _playUIClick?.Invoke();
                _upgradeSelectedIndex = -1;
                _upgradeSelectedTab = 0;
                ResetPendingAllocations();
                _onDone?.Invoke();
            }
        }

        private int SumPendingAllocations()
        {
            int sum = 0;
            for (int i = 0; i < _pendingAllocations.Length; i++) sum += _pendingAllocations[i];
            return sum;
        }

        private void ResetPendingAllocations()
        {
            for (int i = 0; i < _pendingAllocations.Length; i++) _pendingAllocations[i] = 0;
            _inventoryScrollY = 0;
            _inputBuffer = "1";
            _isInputActive = false;
        }

        private static int GetGrowthValue(Character c, GrowthStat stat)
        {
            switch (stat)
            {
                case GrowthStat.STR: return c.STR;
                case GrowthStat.INT: return c.INT;
                case GrowthStat.VIT: return c.VIT;
                case GrowthStat.DEX: return c.DEX;
                case GrowthStat.LUX: return c.LUX;
                case GrowthStat.CRT: return c.CRT;
                default: return 0;
            }
        }

        public void Draw()
        {
            DrawBackdrop();
            DrawTextCentered("Upgrade", new Vector2(ScreenWidth / 2f, 80), Color.Gold, 2.6f);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedCharForBadges = _session.Party[_upgradeSelectedIndex];

                // วงกลมแดงแจ้งเตือน: ไอคอน STAT เตือนเมื่อตัวละครยังมีแต้มเหลือให้อัพ,
                // ไอคอน Equip เตือนเมื่อมีไอเทมในคลังให้ใส่ และตัวละครยังไม่ได้ใส่ไอเทมอยู่
                bool hasPointsToSpend = (selectedCharForBadges.UpgradePoints - SumPendingAllocations()) > 0;
                bool hasItemToEquip = false;
                foreach (var invIt in _session.Inventory)
                {
                    if (selectedCharForBadges.GetEquipped(invIt.Slot) == null) { hasItemToEquip = true; break; }
                }

                var statTabRect = GetUpgradeStatTabRect();
                DrawButton(statTabRect, _upgradeSelectedTab == 0 ? new Color(220, 170, 30) : new Color(50, 50, 75), _hoveredStatTab);

                DrawRect(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Red);
                DrawRectBorder(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.LimeGreen);
                DrawRectBorder(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.DeepSkyBlue);
                DrawRectBorder(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.Black, 1);

                if (hasPointsToSpend) DrawNotificationDot(new Vector2(statTabRect.Right - 4, statTabRect.Top - 4), 10f);

                var equipTabRect = GetUpgradeEquipTabRect();
                DrawButton(equipTabRect, _upgradeSelectedTab == 1 ? new Color(220, 170, 30) : new Color(50, 50, 75), _hoveredEquipTab);

                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.White);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.Black, 1);
                DrawRect(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.White, 1);
                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.White, 1);

                if (hasItemToEquip) DrawNotificationDot(new Vector2(equipTabRect.Right - 4, equipTabRect.Top - 4), 10f);
            }

            var cardRect = GetUpgradeCharacterCardRect();
            Color cardAccent = (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
                ? GetHeroColor(_session.Party[_upgradeSelectedIndex].Name) : new Color(235, 205, 95);
            DrawPanel(cardRect, Color.Lerp(new Color(24, 24, 40), cardAccent, 0.38f), new Color(14, 14, 24), cardAccent, 3);
            // แท่นยืนใต้เท้าตัวละครเหมือนหน้า Team Setup
            var cardStage = new Point(cardRect.Center.X, cardRect.Bottom - 40);
            FillEllipse(cardStage, 112, 26, cardAccent * 0.18f);
            FillEllipse(cardStage, 98, 20, new Color(18, 18, 34, 235));
            DrawEllipseRing(cardStage, 98, 20, 3, cardAccent * 0.9f);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                DrawTextCentered(selectedChar.Name, new Vector2(cardRect.Center.X, cardRect.Bottom + 35), Color.Gold, 1.3f);

                Texture2D heroTex = GetHeroTexture(selectedChar, out Rectangle heroFrame);
                if (heroTex != null)
                {
                    Rectangle src = GetSpriteBounds(heroTex, heroFrame);
                    float scale = Math.Min((cardRect.Width - 50) / (float)src.Width, (cardRect.Height - 90) / (float)src.Height);
                    float w = src.Width * scale;
                    float h = src.Height * scale;
                    float bob = (float)Math.Sin(UiClock * 2.2f) * 3f;
                    var dest = new Rectangle((int)(cardRect.Center.X - w / 2f), (int)(cardStage.Y - 6 - h + bob), (int)w, (int)h);
                    _spriteBatch.Draw(heroTex, dest, src, Color.White);
                }
            }
            else
            {
                DrawTextCentered("+", new Vector2(cardRect.Center.X, cardRect.Center.Y), Color.LightGray, 3f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                var infoRect = GetUpgradeInfoPanelRect();
                DrawPanel(infoRect, new Color(34, 34, 52), new Color(15, 15, 25), Color.Gold, 2);

                DrawText("INFO", new Vector2(infoRect.X + 15, infoRect.Y + 12), Color.Gold, 0.9f);
                DrawText($"{selectedChar.Name}", new Vector2(infoRect.X + 25, infoRect.Y + 45), Color.White, 1.2f);
                DrawLine(new Vector2(infoRect.X + 20, infoRect.Y + 82), new Vector2(infoRect.Right - 20, infoRect.Y + 82), Color.Gold * 0.5f, 2);

                int pendingStr = _pendingAllocations[0];
                int pendingInt = _pendingAllocations[1];
                int pendingVit = _pendingAllocations[2];
                int pendingDex = _pendingAllocations[3];
                int pendingLux = _pendingAllocations[4];
                int pendingCrt = _pendingAllocations[5];

                int bonusHp = pendingVit * 50;
                int bonusPAtk = pendingStr * 12;
                int bonusPDef = pendingVit * 7;
                int bonusMAtk = pendingInt * 12;
                int bonusMDef = (pendingInt * 9);

                float luxCritRate = pendingLux * 4f;
                float crtCritRate = pendingCrt * 0.25f;
                float totalBonusCritRate = luxCritRate + crtCritRate;
                string critRateBonusStr = totalBonusCritRate > 0 ? $" (+{totalBonusCritRate:0.##}%)" : "";

                int bonusCritDmg = pendingCrt * 5;
                string critDmgBonusStr = bonusCritDmg > 0 ? $" (+{bonusCritDmg}%)" : "";

                int bonusSpeed = pendingDex * 2;
                int bonusAccuracy = pendingDex * 1;

                int statStartY = infoRect.Y + 95;
                int rowGap = 38;
                int col2X = infoRect.X + 210;

                DrawStatWithBonus($"HP: {selectedChar.MaxHp}", bonusHp > 0 ? $" (+{bonusHp})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 0), 0.9f);
                DrawStatWithBonus($"P. DEF: {selectedChar.PDef}", bonusPDef > 0 ? $" (+{bonusPDef})" : "", new Vector2(col2X, statStartY + rowGap * 0), 0.9f);

                DrawStatWithBonus($"P. ATK: {selectedChar.PAtk}", bonusPAtk > 0 ? $" (+{bonusPAtk})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 1), 0.9f);
                DrawStatWithBonus($"M. ATK: {selectedChar.MAtk}", bonusMAtk > 0 ? $" (+{bonusMAtk})" : "", new Vector2(col2X, statStartY + rowGap * 1), 0.9f);

                DrawStatWithBonus($"M. DEF: {selectedChar.MDef}", bonusMDef > 0 ? $" (+{bonusMDef})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 2), 0.9f);
                DrawStatWithBonus($"CRIT RATE: {(int)selectedChar.CritRate}%", critRateBonusStr, new Vector2(col2X, statStartY + rowGap * 2), 0.9f);

                DrawStatWithBonus($"CRIT DMG: {(int)selectedChar.CritDamage}%", critDmgBonusStr, new Vector2(infoRect.X + 25, statStartY + rowGap * 3), 0.9f);
                DrawStatWithBonus($"SPEED: {selectedChar.Speed}", bonusSpeed > 0 ? $" (+{bonusSpeed})" : "", new Vector2(col2X, statStartY + rowGap * 3), 0.9f);

                DrawStatWithBonus($"ACC: {(int)selectedChar.Accuracy}%", bonusAccuracy > 0 ? $" (+{bonusAccuracy}%)" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 4), 0.9f);

                int ultimateY = statStartY + rowGap * 5 + 20;
                DrawUltimateIcon(new Rectangle(infoRect.X + 25, ultimateY - 6, 40, 40), selectedChar.Name);
                DrawText("Ultimate Skill", new Vector2(infoRect.X + 78, ultimateY + 2), Color.Gold, 0.95f);
                DrawWrappedText(GetUltimateSkillDescription(selectedChar.Name), new Vector2(infoRect.X + 25, ultimateY + 52), infoRect.Width - 50, Color.White, 0.8f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count && _upgradeSelectedTab >= 0)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                var rightRect = GetUpgradeRightPanelRect();
                DrawPanel(rightRect, new Color(34, 34, 52), new Color(15, 15, 25), Color.Gold, 2);

                if (_upgradeSelectedTab == 0)
                {
                    DrawText("STAT", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);

                    int pendingUsed = SumPendingAllocations();
                    int remainingPts = selectedChar.UpgradePoints - pendingUsed;
                    int typedAmt = int.TryParse(_inputBuffer, out int ta) && ta > 0 ? ta : 1;

                    DrawText($"Point : {remainingPts}", new Vector2(rightRect.X + 150, rightRect.Y + 15), Color.LimeGreen, 1.1f);

                    var ptsBox = GetUpgradePtsBoxRect();
                    DrawRect(ptsBox, _isInputActive ? new Color(60, 60, 85) : new Color(40, 40, 55));
                    DrawRectBorder(ptsBox, _isInputActive ? Color.Gold : Color.LimeGreen, _isInputActive ? 3 : 2);
                    DrawTextCentered(_inputBuffer == "" ? "1" : _inputBuffer, new Vector2(ptsBox.Center.X, ptsBox.Center.Y), _inputBuffer == "" ? Color.Gray : Color.LimeGreen, 1f);

                    for (int row = 0; row < GrowthStats.Length; row++)
                    {
                        var statName = GrowthStats[row].ToString();
                        int baseVal = GetGrowthValue(selectedChar, GrowthStats[row]);
                        int pending = _pendingAllocations[row];

                        var minusRect = GetStatRowMinusButtonRect(row);
                        var plusRect = GetStatRowPlusButtonRect(row);

                        string rowText = pending > 0 ? $"{statName} : {baseVal} (+{pending})" : $"{statName} : {baseVal}";
                        DrawRect(new Rectangle(rightRect.X + 16, minusRect.Y - 4, rightRect.Width - 32, minusRect.Height + 8), pending > 0 ? Color.Gold * 0.10f : (row % 2 == 0 ? Color.White * 0.04f : Color.Transparent));
                        DrawText(rowText, new Vector2(rightRect.X + 30, minusRect.Y + 8), pending > 0 ? Color.Gold : Color.White, 1f);

                        DrawButton(minusRect, pending > 0 ? new Color(170, 55, 55) : new Color(70, 70, 76), pending > 0 && _hoveredStatMinusRow == row);
                        DrawTextCentered(pending > 0 ? $"-{Math.Min(typedAmt, pending)}" : "-", new Vector2(minusRect.Center.X, minusRect.Center.Y), Color.White, pending > 0 ? 0.85f : 1.2f);

                        DrawButton(plusRect, remainingPts > 0 ? new Color(55, 160, 55) : new Color(70, 70, 76), remainingPts > 0 && _hoveredStatPlusRow == row);
                        DrawTextCentered(remainingPts > 0 ? $"+{Math.Min(typedAmt, remainingPts)}" : "+", new Vector2(plusRect.Center.X, plusRect.Center.Y), Color.White, remainingPts > 0 ? 0.85f : 1.2f);
                    }

                    var confirmRect = GetUpgradeConfirmButtonRect();
                    DrawButton(confirmRect, pendingUsed > 0 ? new Color(55, 135, 55) : new Color(62, 62, 68), pendingUsed > 0 && _hoveredConfirmButton);
                    DrawTextCentered(pendingUsed > 0 ? $"Confirm ({pendingUsed} pt)" : "Confirm", new Vector2(confirmRect.Center.X, confirmRect.Center.Y), Color.White, 1.1f);
                }
                else if (_upgradeSelectedTab == 1)
                {
                    DrawText("Equipment", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);

                    var mousePos = VirtualMouse.GetState().Position;
                    Item tipItem = null;

                    for (int sl = 0; sl < 3; sl++)
                    {
                        var equipSlot = GetEquipItemSlotRect(sl);
                        var equippedHere = selectedChar.GetEquipped((ItemSlot)sl);

                        DrawTextCentered(SlotLabels[sl], new Vector2(equipSlot.Center.X, equipSlot.Y - 16), Color.LightGray, 0.8f);
                        DrawPanel(equipSlot, new Color(52, 52, 78), new Color(24, 24, 38), equippedHere != null ? Color.LimeGreen : Color.Gray, 2);

                        if (equippedHere != null)
                        {
                            var icon = new Rectangle(equipSlot.X + 10, equipSlot.Y + 10, equipSlot.Width - 20, equipSlot.Height - 20);
                            if (!ItemIcons.Draw(_spriteBatch, equippedHere, icon))
                            {
                                DrawTextCentered(equippedHere.Name, new Vector2(equipSlot.Center.X, equipSlot.Center.Y - 10), Color.LimeGreen, 0.6f);
                                DrawTextCentered($"[{equippedHere.Rarity}]", new Vector2(equipSlot.Center.X, equipSlot.Center.Y + 15), Color.White, 0.6f);
                            }

                            var unequipBtn = GetEquipUnequipButtonRect(sl);
                            DrawButton(unequipBtn, new Color(170, 55, 55), _hoveredUnequipSlot == sl, 1);
                            DrawTextCentered("Unequip", new Vector2(unequipBtn.Center.X, unequipBtn.Center.Y), Color.White, 0.8f);

                            if (equipSlot.Contains(mousePos)) tipItem = equippedHere;
                        }
                        else
                        {
                            DrawTextCentered("+", new Vector2(equipSlot.Center.X, equipSlot.Center.Y), Color.Gray, 2f);
                        }
                    }

                    DrawText("Inventory", new Vector2(rightRect.X + 30, rightRect.Y + 236), Color.White, 0.9f);

                    for (int f = 0; f < 4; f++)
                    {
                        var fr = GetInvFilterRect(f);
                        bool active = (f - 1) == _invFilter;
                        DrawButton(fr, active ? new Color(220, 170, 30) : new Color(50, 50, 75), _hoveredInvFilter == f, 1);
                        DrawTextCentered(FilterLabels[f], new Vector2(fr.Center.X, fr.Center.Y), active ? Color.Black : Color.White, 0.75f);
                    }

                    var filteredDraw = GetFilteredInventory();
                    if (filteredDraw.Count == 0)
                    {
                        DrawText(_session.Inventory.Count == 0 ? "No items in inventory" : "No items in this category", new Vector2(rightRect.X + 30, rightRect.Y + 285), Color.Gray, 0.9f);
                    }

                    int clipTop = rightRect.Y + 275;
                    int clipBottom = rightRect.Bottom - 10;

                    for (int pos = 0; pos < filteredDraw.Count; pos++)
                    {
                        int row = filteredDraw[pos];
                        var itemRow = GetEquipInventoryItemRect(pos, _inventoryScrollY);

                        if (itemRow.Top < clipTop || itemRow.Bottom > clipBottom) continue;

                        bool hoveredRow = _hoveredRestItemRow == row;
                        DrawButton(itemRow, new Color(40, 40, 60), hoveredRow, 2);

                        var invItem = _session.Inventory[row];
                        var invIcon = new Rectangle(itemRow.X + 6, itemRow.Y + 6, itemRow.Width - 12, itemRow.Height - 12);
                        if (!ItemIcons.Draw(_spriteBatch, invItem, invIcon))
                            DrawTextCentered(invItem.Name, new Vector2(itemRow.Center.X, itemRow.Center.Y), Color.Gold, 0.5f);
                    }

                    // Tooltip: ชี้ที่ช่อง Equip หรือไอเท็มในคลัง แล้วโชว์ชื่อ + ความสามารถ
                    if (_hoveredRestItemRow >= 0 && _hoveredRestItemRow < _session.Inventory.Count)
                        tipItem = _session.Inventory[_hoveredRestItemRow];
                    if (tipItem != null) DrawItemTooltip(tipItem, mousePos);
                }
            }

            int trayLineY = ScreenHeight - 200;
            DrawLine(new Vector2(100, trayLineY), new Vector2(ScreenWidth - 100, trayLineY), Color.Gold, 3);
            DrawText("Hero Tray", new Vector2(120, trayLineY - 35), Color.Gold, 1.2f);

            for (int i = 0; i < _session.Party.Count; i++)
            {
                var hero = _session.Party[i];
                var trayRect = GetUpgradeHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;
                bool selected = _upgradeSelectedIndex == i;

                Color heroColor = GetHeroColor(hero.Name);
                var r = trayRect;
                if (hovered || selected) r.Y -= 8;

                if (selected) DrawRect(new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12), Color.Gold * (0.12f + 0.06f * (float)Math.Sin(UiClock * 4f)));
                DrawRect(r, new Color(14, 14, 26));
                int portraitH = r.Height - 36;
                var portraitBox = new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, portraitH);
                DrawGradientRect(portraitBox, heroColor * 0.75f, heroColor * 0.25f);
                if (!DrawHeroPortrait(hero, portraitBox, Color.White))
                    DrawTextCentered(hero.Name.Substring(0, 1), new Vector2(r.Center.X, r.Y + portraitH / 2f + 4), Color.White * 0.9f, 3.2f);

                var bar = new Rectangle(r.X + 3, r.Bottom - 36, r.Width - 6, 33);
                DrawRect(bar, new Color(10, 10, 18, 235));
                DrawTextCentered(hero.Name, new Vector2(bar.Center.X, bar.Y + 10), Color.White, 0.85f);
                DrawTextCentered($"Pts: {hero.UpgradePoints}", new Vector2(bar.Center.X, bar.Y + 25), Color.LightGreen, 0.6f);

                DrawRectBorder(r, selected || hovered ? Color.Gold : heroColor, selected ? 4 : (hovered ? 3 : 2));

                // วงกลมแดงแจ้งเตือน: ตัวละครยังมีแต้มเหลือให้อัพ หรือมีไอเท็มในคลังที่ช่องสวมใส่ยังว่างอยู่
                int pointsLeft = hero.UpgradePoints - (selected ? SumPendingAllocations() : 0);
                bool canEquip = false;
                foreach (var invIt in _session.Inventory)
                {
                    if (hero.GetEquipped(invIt.Slot) == null) { canEquip = true; break; }
                }
                if (pointsLeft > 0 || canEquip)
                    DrawNotificationDot(new Vector2(r.Right - 4, r.Top - 4), 10f);
            }

            var contRect = GetContinueButtonRect();
            DrawButton(contRect, new Color(55, 135, 55), _hoveredContinue, 3);
            DrawTextCentered("Done ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private readonly System.Collections.Generic.Dictionary<Texture2D, Rectangle> _spriteBoundsCache = new System.Collections.Generic.Dictionary<Texture2D, Rectangle>();

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

        private Texture2D GetHeroTexture(Character c, out Rectangle frame)
        {
            frame = Rectangle.Empty;
            if (c.Name == "Elma" && _elmaIdleTex != null) { frame = new Rectangle(0, 0, 160, 320); return _elmaIdleTex; }
            if (c.Name == "Lumi" && _lumiIdleTex != null) { frame = new Rectangle(0, 0, 240, 320); return _lumiIdleTex; }
            if ((c.Name == "Alice" || c.Name == "Swordman") && _aliceIdleTex != null) { frame = new Rectangle(0, 0, 480, 480); return _aliceIdleTex; }
            if (c.Name == "Enos" && _enosIdleTex != null) { frame = SpriteFrames.EnosFrame(_enosIdleTex, 0); return _enosIdleTex; }
            if (c.Name == "Veyra" && _veyraIdleTex != null) { frame = SpriteFrames.VeyraFrame(_veyraIdleTex, 0); return _veyraIdleTex; }
            if (_testCharacterTex != null) { frame = new Rectangle(0, 0, _testCharacterTex.Width, _testCharacterTex.Height); return _testCharacterTex; }
            return null;
        }

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
                    for (int x = 0; x < frame.Width; x++)
                        if (data[y * frame.Width + x].A > 20)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                result = maxX >= 0 ? new Rectangle(frame.X + minX, frame.Y + minY, maxX - minX + 1, maxY - minY + 1) : frame;
            }
            catch { }
            _spriteBoundsCache[tex] = result;
            return result;
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

        private void FillEllipse(Point c, int rx, int ry, Color color)
        {
            for (int dy = -ry; dy <= ry; dy++)
            {
                float t = dy / (float)ry;
                int half = (int)(rx * Math.Sqrt(Math.Max(0, 1 - t * t)));
                if (half > 0) _spriteBatch.Draw(_pixel, new Rectangle(c.X - half, c.Y + dy, half * 2, 1), color);
            }
        }

        private void DrawEllipseRing(Point c, int rx, int ry, int thickness, Color color)
        {
            int irx = rx - thickness, iry = ry - thickness;
            for (int dy = -ry; dy <= ry; dy++)
            {
                float t = dy / (float)ry;
                int outer = (int)(rx * Math.Sqrt(Math.Max(0, 1 - t * t)));
                int inner = 0;
                if (iry > 0 && Math.Abs(dy) < iry)
                {
                    float ti = dy / (float)iry;
                    inner = (int)(irx * Math.Sqrt(Math.Max(0, 1 - ti * ti)));
                }
                if (outer <= 0) continue;
                if (inner <= 0) _spriteBatch.Draw(_pixel, new Rectangle(c.X - outer, c.Y + dy, outer * 2, 1), color);
                else
                {
                    _spriteBatch.Draw(_pixel, new Rectangle(c.X - outer, c.Y + dy, outer - inner, 1), color);
                    _spriteBatch.Draw(_pixel, new Rectangle(c.X + inner, c.Y + dy, outer - inner, 1), color);
                }
            }
        }

        // ตัดขอบโปร่งใสของเฟรมออก เพื่อให้ตัวละครขยายเต็มช่องการ์ด
        private static Rectangle TrimToContent(Texture2D tex, Rectangle src)
        {
            try
            {
                var data = new Color[src.Width * src.Height];
                tex.GetData(0, src, data, 0, data.Length);
                int minX = src.Width, minY = src.Height, maxX = -1, maxY = -1;
                for (int y = 0; y < src.Height; y++)
                    for (int x = 0; x < src.Width; x++)
                        if (data[y * src.Width + x].A > 16)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                if (maxX < minX || maxY < minY) return src;
                return new Rectangle(src.X + minX, src.Y + minY, maxX - minX + 1, maxY - minY + 1);
            }
            catch { return src; }
        }

        private void DrawStatWithBonus(string mainText, string bonusText, Vector2 position, float scale)
        {
            if (_font == null) return;
            DrawText(mainText, position, Color.White, scale);
            if (!string.IsNullOrEmpty(bonusText))
            {
                float mainWidth = _font.MeasureString(mainText).X * scale;
                DrawText(bonusText, new Vector2(position.X + mainWidth, position.Y), Color.Gold, scale);
            }
        }

        // วาดวงกลมแดงแจ้งเตือนเล็กๆ ไว้มุมบนขวาของไอคอน (มีขอบดำบางๆ กันกลืนกับพื้นหลัง)
        private void DrawNotificationDot(Vector2 center, float radius)
        {
            DrawFilledCircle(center, radius + 2f, Color.Black);
            DrawFilledCircle(center, radius, Color.Red);
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            int r = (int)Math.Ceiling(radius);
            for (int y = -r; y <= r; y++)
            {
                float remaining = radius * radius - y * y;
                if (remaining < 0) continue;
                int dx = (int)Math.Sqrt(remaining);
                if (dx <= 0) continue;
                var rowRect = new Rectangle((int)(center.X - dx), (int)(center.Y + y), dx * 2, 1);
                _spriteBatch.Draw(_pixel, rowRect, color);
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
                return "Channels healing light that restores 25% of Veyra's own Max HP, plus 2% of each ally's own Max HP, to every ally at once.";
            if (characterName.Contains("Lumi"))
                return "Unleashes a wave of magic that deals 110% Magic ATK damage to every enemy on the battlefield.";
            if (characterName.Contains("Alice"))
                return "Enters a focused battle stance (self only, no target needed). The gauge stays at 100% and drops 20% with every attack she makes, so the buff lasts 5 attacks: Physical ATK, Defense, and Speed +30%, Crit Rate +30%, Crit Damage +120%.";

            return "";
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

        private void DrawItemTooltip(Item item, Point mouse)
        {
            const int w = 320;
            const int h = 120;
            int x = mouse.X + 20;
            int y = mouse.Y + 20;
            if (x + w > ScreenWidth - 10) x = mouse.X - w - 20;
            if (y + h > ScreenHeight - 10) y = mouse.Y - h - 20;
            var box = new Rectangle(x, y, w, h);
            Color rc = GetItemRarityColor(item.Rarity);

            DrawRect(box, new Color(15, 15, 25, 245));
            DrawRectBorder(box, rc, 2);
            DrawText(item.Name, new Vector2(box.X + 14, box.Y + 10), rc, 1.0f);
            DrawText($"[{item.Rarity}]  {item.Slot}", new Vector2(box.X + 14, box.Y + 40), Color.LightGray, 0.8f);
            DrawWrappedText(item.Description, new Vector2(box.X + 14, box.Y + 68), w - 28, Color.White, 0.8f, 24f);
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