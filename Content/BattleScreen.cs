using How.Battle;
using How.Data;
using How.Manager;
using How.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace How
{
    public class BattleScreen
    {
        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;
        private Texture2D _battleBackground;

        private Texture2D _elmaIdleTex;
        private Texture2D _elmaAttackTex;
        private Texture2D _testCharacterTex;
        private Texture2D _deadEffectTex;

        private Texture2D _testEnemyTex;
        private Texture2D _orcWarriorTex;
        private Texture2D _goblinArcherTex;
        private Texture2D _darkMageTex;
        private Texture2D _ghostHealerTex;
        private Texture2D _stoneGiantTex;

        private BattleManager _battle;
        private List<Character> _party;
        private Character[] _formationSlots;
        private List<Item> _inventory;
        private List<Item> _currentItemChoices;
        private int _coins;
        private bool _isStoryMode;
        private int _storyStageIndex;
        private bool _currentStageIsHard;
        private bool _cameraShakeEnabled; // ตัวแปรเก็บสถานะเปิด/ปิด Camera Shake
        private readonly Random _rng;

        private float _elmaAnimTimer;
        private int _elmaFrameIndex;
        private bool _elmaIsAnimatingAttack;
        private float _elmaAttackHoldTimer;
        private Character _elmaStoredTarget;
        private int _elmaCustomFrameIndex;
        private float _elmaCustomAnimTimer;
        private bool _elmaHasAnimatedThisTurn;

        // ตัวแปรสำหรับ Camera Shake ($\pm30$ X)
        private float _shakeTimer;
        private float _shakeOffsetX;

        private readonly Dictionary<Character, float> _deathFadeAlphas = new Dictionary<Character, float>();
        private readonly Dictionary<Character, float> _deathTimers = new Dictionary<Character, float>();
        private readonly HashSet<Character> _playedDeathSound = new HashSet<Character>();

        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private const float FloatingTextDuration = 0.8f;
        private const float FloatingTextRiseSpeed = 45f;
        private const float DamageTextScale = 2.2f;
        private const float CritTextScale = 2.8f;
        private const float MissTextScale = 1.8f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private Character _hoveredEnemyTarget;
        private bool _hoveredAutoToggle;
        private int _hoveredItemCard = -1;
        private bool _hoveredItemSkip;
        private bool _hoveredStoryWinContinue;
        private bool _hoveredStoryWinMenu;
        private bool _hoveredLoseRestart;
        private bool _hoveredLoseMenu;

        private readonly Action<AppState> _changeState;
        private readonly Action _playUIClick;
        private readonly Action _goToUpgrade;
        private readonly Action _startNewRunForChallenge;

        public BattleScreen(
            GraphicsDevice graphicsDevice,
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Texture2D battleBg,
            Dictionary<string, Texture2D> textures,
            Random rng,
            Action<AppState> changeState,
            Action playUIClick,
            Action goToUpgrade,
            Action startNewRunForChallenge)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _battleBackground = battleBg;
            _rng = rng;
            _changeState = changeState;
            _playUIClick = playUIClick;
            _goToUpgrade = goToUpgrade;
            _startNewRunForChallenge = startNewRunForChallenge;

            _elmaIdleTex = textures.GetValueOrDefault("ElmaIdle");
            _elmaAttackTex = textures.GetValueOrDefault("ElmaAttack");
            _testCharacterTex = textures.GetValueOrDefault("TestChar");
            _deadEffectTex = textures.GetValueOrDefault("DeadEffect");
            _testEnemyTex = textures.GetValueOrDefault("TestEnemy");
            _orcWarriorTex = textures.GetValueOrDefault("Orc");
            _goblinArcherTex = textures.GetValueOrDefault("Goblin");
            _darkMageTex = textures.GetValueOrDefault("DarkMage");
            _ghostHealerTex = textures.GetValueOrDefault("Ghost");
            _stoneGiantTex = textures.GetValueOrDefault("StoneGiant");
        }

        public void StartNewBattle(List<Character> party, Character[] formationSlots, bool isHard, bool isStoryMode, int storyStageIndex, ref int coins, List<Item> inventory, bool manualModePreference, bool cameraShakeEnabled)
        {
            _party = party;
            _formationSlots = formationSlots;
            _isStoryMode = isStoryMode;
            _storyStageIndex = storyStageIndex;
            _currentStageIsHard = isHard;
            _cameraShakeEnabled = cameraShakeEnabled;

            if (_battle != null)
                _battle.OnAttackResolved -= HandleAttackResolved;

            _battle = new BattleManager(_party, isHard, onShakeCamera: () => {
                if (_cameraShakeEnabled)
                {
                    _shakeTimer = 0.25f;
                }
            });
            _currentItemChoices = null;
            _deathFadeAlphas.Clear();
            _deathTimers.Clear();
            _playedDeathSound.Clear();

            if (_isStoryMode && _storyStageIndex == 0)
            {
                if (_battle.Enemies != null && _battle.Enemies.Count > 1)
                {
                    _battle.Enemies.RemoveRange(1, _battle.Enemies.Count - 1);
                }
            }

            _battle.SetManualMode(manualModePreference);
            _battle.OnAttackResolved += HandleAttackResolved;
            _floatingTexts.Clear();

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        public Vector2 GetShakeOffset()
        {
            if (_shakeTimer > 0f)
            {
                _shakeOffsetX = (float)(_rng.NextDouble() * 60.0 - 30.0);
                return new Vector2(_shakeOffsetX, 0f);
            }
            return Vector2.Zero;
        }

        private void HandleAttackResolved(Character target, AttackResult result)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);

            if (result.IsMiss)
            {
                _floatingTexts.Add(new FloatingText { Position = pos, Text = "MISS", Timer = FloatingTextDuration, Color = Color.LightGray, Scale = MissTextScale });
                return;
            }

            string text = result.IsCrit ? $"-{result.Damage}!" : $"-{result.Damage}";
            Color color = result.IsCrit ? Color.Red : Color.OrangeRed;
            float scale = result.IsCrit ? CritTextScale : DamageTextScale;
            _floatingTexts.Add(new FloatingText { Position = pos, Text = text, Timer = FloatingTextDuration, Color = color, Scale = scale });

            if ((!target.IsAlive || target.Hp <= 0) && !_playedDeathSound.Contains(target))
            {
                _playedDeathSound.Add(target);
                AudioManager.PlaySfx("Audio/Deadsound");
            }
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool leftClicked, ref bool manualModePreference, ref int coins, List<Item> inventory)
        {
            _coins = coins;
            _inventory = inventory;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= delta;
            }

            _elmaAnimTimer += delta;
            if (_elmaAnimTimer >= 0.12f)
            {
                _elmaAnimTimer = 0f;
                _elmaFrameIndex++;
            }

            if (_battle != null)
            {
                UpdateDeathFades(delta);

                bool isElmaAttacking = (_battle.ActiveAttacker != null && _battle.ActiveAttacker.Name == "Elma");

                if (isElmaAttacking)
                {
                    if (!_elmaHasAnimatedThisTurn)
                    {
                        _elmaIsAnimatingAttack = true;
                        _elmaAttackHoldTimer = 1.0f;
                        _elmaStoredTarget = _battle.ActiveTarget;
                        _elmaCustomFrameIndex = 0;
                        _elmaCustomAnimTimer = 0f;
                        _elmaHasAnimatedThisTurn = true;
                    }
                }
                else
                {
                    _elmaHasAnimatedThisTurn = false;
                }

                if (_elmaIsAnimatingAttack)
                {
                    _elmaAttackHoldTimer -= delta;
                    if (_elmaAttackTex != null)
                    {
                        int frameWidth = 320;
                        int totalFrames = Math.Max(1, _elmaAttackTex.Width / frameWidth);
                        float timePerFrame = 1.0f / totalFrames;
                        _elmaCustomAnimTimer += delta;
                        if (_elmaCustomAnimTimer >= timePerFrame)
                        {
                            _elmaCustomAnimTimer = 0f;
                            if (_elmaCustomFrameIndex < totalFrames - 1)
                            {
                                _elmaCustomFrameIndex++;
                            }
                        }
                    }

                    if (_elmaAttackHoldTimer <= 0f)
                    {
                        _elmaIsAnimatingAttack = false;
                        _elmaStoredTarget = null;
                    }
                }
                else
                {
                    _battle.Update(delta);
                }
            }

            UpdateFloatingTexts(delta);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick();
                _changeState(AppState.MainMenu);
                return;
            }

            if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                if (_currentItemChoices == null || _currentItemChoices.Count == 0)
                {
                    foreach (var c in _party) c.UpgradePoints += _battle.RewardPoints;
                    coins += _battle.RewardCoins;
                    _currentItemChoices = ItemPool.GetRandomItems(3, _rng);
                }

                _hoveredItemCard = -1;
                for (int i = 0; i < _currentItemChoices.Count; i++)
                {
                    if (GetItemCardRect(i).Contains(mouse.Position)) _hoveredItemCard = i;
                }

                if (leftClicked && _hoveredItemCard >= 0)
                {
                    _playUIClick();
                    var item = _currentItemChoices[_hoveredItemCard];
                    if (coins >= item.Price)
                    {
                        coins -= item.Price;
                        _inventory.Add(item);
                        _currentItemChoices = null;
                        _goToUpgrade();
                    }
                }

                _hoveredItemSkip = GetItemSkipButtonRect().Contains(mouse.Position);
                if (leftClicked && _hoveredItemSkip)
                {
                    _playUIClick();
                    _currentItemChoices = null;
                    _goToUpgrade();
                }
                return;
            }

            _hoveredAutoToggle = GetAutoToggleButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredAutoToggle)
            {
                _playUIClick();
                _battle.SetManualMode(!_battle.ManualMode);
                manualModePreference = _battle.ManualMode;
            }

            _hoveredEnemyTarget = null;
            if (_battle.PendingAttacker != null)
            {
                foreach (var enemy in _battle.Enemies)
                {
                    if (!enemy.IsAlive) continue;
                    if (GetAvatarHomeRect(enemy).Contains(mouse.Position)) { _hoveredEnemyTarget = enemy; break; }
                }

                if (leftClicked && _hoveredEnemyTarget != null)
                {
                    _battle.TrySelectTarget(_hoveredEnemyTarget);
                    _hoveredEnemyTarget = null;
                }
            }

            if (_battle.State == BattleState.AllyWin)
            {
                if (_isStoryMode)
                {
                    _hoveredStoryWinContinue = GetStoryWinContinueRect().Contains(mouse.Position);
                    _hoveredStoryWinMenu = GetStoryWinMenuRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredStoryWinContinue) { _playUIClick(); _changeState(AppState.StoryModeSelect); }
                        else if (_hoveredStoryWinMenu) { _playUIClick(); _changeState(AppState.MainMenu); }
                    }
                }
            }
            else if (_battle.State == BattleState.EnemyWin)
            {
                _hoveredLoseRestart = GetLoseRestartRect().Contains(mouse.Position);
                _hoveredLoseMenu = GetLoseMenuRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    if (_hoveredLoseRestart)
                    {
                        _playUIClick();
                        if (_isStoryMode)
                        {
                            StartNewBattle(_party, _formationSlots, _currentStageIsHard, _isStoryMode, _storyStageIndex, ref coins, inventory, manualModePreference, _cameraShakeEnabled);
                        }
                        else
                        {
                            _startNewRunForChallenge();
                        }
                    }
                    else if (_hoveredLoseMenu)
                    {
                        _playUIClick();
                        _changeState(AppState.MainMenu);
                    }
                }
            }
        }

        private void UpdateDeathFades(float delta)
        {
            if (_battle == null) return;

            var allUnits = new List<Character>();
            if (_battle.Allies != null) allUnits.AddRange(_battle.Allies);
            if (_battle.Enemies != null) allUnits.AddRange(_battle.Enemies);

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || unit.Hp <= 0)
                {
                    if (!_deathFadeAlphas.ContainsKey(unit))
                    {
                        _deathFadeAlphas[unit] = 1.0f;
                        _deathTimers[unit] = 0f;
                    }
                    else
                    {
                        _deathTimers[unit] += delta;
                    }

                    if (_deathFadeAlphas[unit] > 0f)
                    {
                        _deathFadeAlphas[unit] -= delta * 1.6f;
                        if (_deathFadeAlphas[unit] < 0f) _deathFadeAlphas[unit] = 0f;
                    }
                }
                else
                {
                    _deathFadeAlphas[unit] = 1.0f;
                    _deathTimers[unit] = 0f;
                }
            }
        }

        private void UpdateFloatingTexts(float delta)
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Timer -= delta;
                ft.Position.Y -= FloatingTextRiseSpeed * delta;
                if (ft.Timer <= 0f) _floatingTexts.RemoveAt(i);
            }
        }

        public void Draw()
        {
            DrawBattleBackground();
            DrawTeam(_battle.Allies, isEnemyRow: false);
            DrawTeam(_battle.Enemies, isEnemyRow: true);
            DrawFloatingTexts();
            DrawTurnArrows();
            DrawResultBanner();
            DrawAutoToggleButton();
        }

        private void DrawBattleBackground()
        {
            if (_battleBackground != null)
                _spriteBatch.Draw(_battleBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
        }

        private const int AvatarSize = 110;
        private const int RowSpacing = 165;
        private const int FormationTop = 330;
        private const int FrontRowCount = 3;
        private const int HpBarGap = 6;
        private const int HpBarHeight = 12;
        private const int AllyFrontX = 650;
        private const int AllyBackX = 430;
        private static int EnemyFrontX => ScreenWidth - AllyFrontX - AvatarSize;
        private static int EnemyBackX => ScreenWidth - AllyBackX - AvatarSize;

        private Rectangle GetAvatarHomeRect(Character unit)
        {
            var team = unit.IsEnemy ? _battle.Enemies : _battle.Allies;
            int index;

            if (unit.IsEnemy)
            {
                index = team.IndexOf(unit);
            }
            else
            {
                index = Array.IndexOf(_formationSlots, unit);
                if (index < 0) index = team.IndexOf(unit);
            }

            bool isFront = index < FrontRowCount;
            int rowSlot = isFront ? index : index - FrontRowCount;
            int y = isFront ? FormationTop + rowSlot * RowSpacing : FormationTop + RowSpacing / 2 + rowSlot * RowSpacing;
            int x = unit.IsEnemy ? (isFront ? EnemyFrontX : EnemyBackX) : (isFront ? AllyFrontX : AllyBackX);
            return new Rectangle(x, y, AvatarSize, AvatarSize);
        }

        private Rectangle GetAvatarDisplayRect(Character unit)
        {
            var home = GetAvatarHomeRect(unit);

            if (unit.Name == "Elma" && _elmaIsAnimatingAttack && _elmaStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }
            else if (_battle.ActiveAttacker == unit && _battle.ActiveTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_battle.ActiveTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }

            return home;
        }

        private Texture2D GetEnemyTexture(Character unit)
        {
            string name = unit.Name?.ToLower() ?? "";
            Texture2D tex = null;
            if (name.Contains("orc") || name.Contains("orc_warrior")) tex = _orcWarriorTex;
            else if (name.Contains("goblin") || name.Contains("goblin_archer")) tex = _goblinArcherTex;
            else if (name.Contains("dark") || name.Contains("dark_mage")) tex = _darkMageTex;
            else if (name.Contains("ghost") || name.Contains("ghost_healer")) tex = _ghostHealerTex;
            else if (name.Contains("stone") || name.Contains("stone_giant")) tex = _stoneGiantTex;

            return tex ?? _testEnemyTex;
        }

        private void DrawTeam(List<Character> team, bool isEnemyRow)
        {
            foreach (var unit in team)
            {
                bool isActive = _battle.ActiveAttacker == unit || _battle.ActiveTarget == unit;
                bool isHoveredTarget = _hoveredEnemyTarget == unit;
                Color themeColor = isEnemyRow ? new Color(180, 60, 60) : new Color(60, 120, 200);

                float alpha = 1.0f;
                if (_deathFadeAlphas.ContainsKey(unit))
                {
                    alpha = _deathFadeAlphas[unit];
                }
                else if (!unit.IsAlive || unit.Hp <= 0)
                {
                    alpha = 0f;
                }

                var avatarRect = GetAvatarDisplayRect(unit);

                if (unit.Name == "Elma")
                {
                    float baseScale = ((float)avatarRect.Height / 320f) * 1.8f;

                    if (_elmaIsAnimatingAttack)
                    {
                        if (_elmaAttackTex != null)
                        {
                            int frameWidth = 320;
                            int frameHeight = 320;
                            int totalFrames = Math.Max(1, _elmaAttackTex.Width / frameWidth);
                            int currentFrame = Math.Min(_elmaCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(_elmaAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                        else
                        {
                            DrawRect(avatarRect, themeColor * alpha);
                            DrawRectBorder(avatarRect, Color.White * alpha, 3);
                        }
                    }
                    else if (_elmaIdleTex != null)
                    {
                        int frameWidth = 160;
                        int frameHeight = 320;
                        int totalFrames = Math.Max(1, _elmaIdleTex.Width / frameWidth);
                        int currentFrame = _elmaFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_elmaIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        DrawRect(avatarRect, themeColor * alpha);
                        DrawRectBorder(avatarRect, Color.White * alpha, 3);
                    }
                }
                else if (!unit.IsEnemy)
                {
                    if (_testCharacterTex != null)
                    {
                        float baseScale = ((float)avatarRect.Height / (float)_testCharacterTex.Height) * 1.4f;
                        float drawW = _testCharacterTex.Width * baseScale;
                        float drawH = _testCharacterTex.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_testCharacterTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        DrawRect(avatarRect, (alpha <= 0f ? new Color(50, 50, 50) : themeColor) * alpha);
                        DrawRectBorder(avatarRect, (alpha <= 0f ? Color.DarkGray : (isHoveredTarget ? Color.Orange : (isActive ? Color.Gold : themeColor))) * alpha, isHoveredTarget ? 4 : 3);
                    }
                }
                else
                {
                    Texture2D enemyTex = GetEnemyTexture(unit);
                    if (enemyTex != null)
                    {
                        float baseScale = ((float)avatarRect.Height / (float)enemyTex.Height) * 1.4f;
                        float drawW = enemyTex.Width * baseScale;
                        float drawH = enemyTex.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(enemyTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.FlipHorizontally, 0f);
                    }
                    else
                    {
                        DrawRect(avatarRect, (alpha <= 0f ? new Color(50, 50, 50) : themeColor) * alpha);
                        DrawRectBorder(avatarRect, (alpha <= 0f ? Color.DarkGray : (isHoveredTarget ? Color.Orange : (isActive ? Color.Gold : themeColor))) * alpha, isHoveredTarget ? 4 : 3);
                    }
                }

                if ((!unit.IsAlive || unit.Hp <= 0) && _deadEffectTex != null)
                {
                    int effFrameWidth = 160;
                    int effFrameHeight = 320;
                    int totalEffFrames = 4;
                    float deathTime = _deathTimers.ContainsKey(unit) ? _deathTimers[unit] : 0f;
                    int currentEffFrame = Math.Min(totalEffFrames - 1, (int)(deathTime / 0.15f));

                    Rectangle effSourceRect = new Rectangle(currentEffFrame * effFrameWidth, 0, effFrameWidth, effFrameHeight);
                    float effScale = ((float)avatarRect.Height / effFrameHeight) * 1.8f;
                    float effW = effFrameWidth * effScale;
                    float effH = effFrameHeight * effScale;
                    Vector2 effPos = new Vector2(
                        avatarRect.X + (avatarRect.Width - effW) / 2f,
                        avatarRect.Bottom - effH
                    );

                    _spriteBatch.Draw(_deadEffectTex, effPos, effSourceRect, Color.White * alpha, 0f, Vector2.Zero, effScale, SpriteEffects.None, 0f);
                }

                var home = GetAvatarHomeRect(unit);
                float hpBarY;
                if (unit.Name == "Elma")
                {
                    float baseScale = ((float)home.Height / 320f) * 1.8f;
                    float drawH = 320f * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (!unit.IsEnemy && _testCharacterTex != null)
                {
                    float baseScale = ((float)home.Height / (float)_testCharacterTex.Height) * 1.4f;
                    float drawH = _testCharacterTex.Height * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (unit.IsEnemy && GetEnemyTexture(unit) != null)
                {
                    var enemyTex = GetEnemyTexture(unit);
                    float baseScale = ((float)home.Height / (float)enemyTex.Height) * 1.4f;
                    float drawH = enemyTex.Height * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else
                {
                    hpBarY = home.Y - HpBarGap - HpBarHeight;
                }

                var hpBg = new Rectangle(home.X, (int)hpBarY, home.Width, HpBarHeight);
                DrawRect(hpBg, new Color(60, 20, 20) * alpha);

                float hpRatio = unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0f;
                DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * hpRatio), hpBg.Height), (hpRatio > 0.5f ? Color.LimeGreen : hpRatio > 0.2f ? Color.Orange : Color.Red) * alpha);
                DrawRectBorder(hpBg, Color.Black * alpha, 1);
            }
        }

        private Vector2 GetUnitArrowTipPosition(Character unit)
        {
            var home = GetAvatarHomeRect(unit);
            float topY = home.Y;

            if (unit.Name == "Elma")
            {
                float baseScale = ((float)home.Height / 320f) * 1.8f;
                topY = home.Bottom - (320f * baseScale);
            }
            else if (!unit.IsEnemy && _testCharacterTex != null)
            {
                float baseScale = ((float)home.Height / (float)_testCharacterTex.Height) * 1.4f;
                topY = home.Bottom - (_testCharacterTex.Height * baseScale);
            }
            else if (unit.IsEnemy)
            {
                Texture2D enemyTex = GetEnemyTexture(unit);
                if (enemyTex != null)
                {
                    float baseScale = ((float)home.Height / (float)enemyTex.Height) * 1.4f;
                    topY = home.Bottom - (enemyTex.Height * baseScale);
                }
            }

            float hpBarY = topY - HpBarGap - HpBarHeight;
            return new Vector2(home.X + home.Width / 2f, hpBarY - 15f);
        }

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts)
                DrawTextCentered(ft.Text, ft.Position, ft.Color * MathHelper.Clamp(ft.Timer / FloatingTextDuration, 0f, 1f), ft.Scale);
        }

        private void DrawTurnArrows()
        {
            if (_battle.PendingAttacker != null)
                DrawDownArrow(GetUnitArrowTipPosition(_battle.PendingAttacker), 16, Color.Yellow);
            if (_hoveredEnemyTarget != null)
                DrawDownArrow(GetUnitArrowTipPosition(_hoveredEnemyTarget), 16, Color.Orange);
        }

        private void DrawDownArrow(Vector2 tip, float size, Color color)
        {
            DrawLine(tip + new Vector2(-size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(0, -size * 2.2f), tip + new Vector2(0, -size), color, 5);
        }

        private Rectangle GetAutoToggleButtonRect() => new Rectangle(ScreenWidth - 100, 30, 70, 70);

        private void DrawAutoToggleButton()
        {
            var rect = GetAutoToggleButtonRect();
            bool isManual = _battle.ManualMode;

            Color buttonColor = Color.White;
            if (_hoveredAutoToggle) buttonColor = Color.DeepSkyBlue;
            else if (!isManual) buttonColor = Color.Gold;

            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            float radius = rect.Width / 2f;

            for (float angle = 0; angle < MathHelper.TwoPi; angle += 0.03f)
            {
                int bx = (int)(center.X + (radius - 2) * (float)Math.Cos(angle));
                int by = (int)(center.Y + (radius - 2) * (float)Math.Sin(angle));
                _spriteBatch.Draw(_pixel, new Rectangle(bx, by, 3, 3), buttonColor);
            }

            DrawTextCentered("A", center, buttonColor, 1.8f);
        }

        private void DrawResultBanner()
        {
            if (_battle.State == BattleState.Running) return;

            if (_battle.State == BattleState.AllyWin && _isStoryMode)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Gold, 3);
                DrawTextCentered($"Stage 1-{_storyStageIndex + 1} Cleared!", new Vector2(rect.Center.X, rect.Center.Y), Color.Gold, 2f);

                var btnContinue = GetStoryWinContinueRect();
                DrawRect(btnContinue, _hoveredStoryWinContinue ? Color.Gold : new Color(50, 120, 50));
                DrawRectBorder(btnContinue, Color.White, 2);
                DrawTextCentered("Continue", new Vector2(btnContinue.Center.X, btnContinue.Center.Y), Color.White, 1.1f);

                var btnMenu = GetStoryWinMenuRect();
                DrawRect(btnMenu, _hoveredStoryWinMenu ? Color.Gold : new Color(150, 50, 50));
                DrawRectBorder(btnMenu, Color.White, 2);
                DrawTextCentered("Main Menu", new Vector2(btnMenu.Center.X, btnMenu.Center.Y), Color.White, 1.1f);
            }
            else if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                if (_currentItemChoices != null)
                {
                    DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 200));

                    DrawTextCentered("Victory!", new Vector2(ScreenWidth / 2f, 105), Color.Gold, 3.0f);
                    DrawTextCentered("Choose an Item", new Vector2(ScreenWidth / 2f, 160), Color.White, 1.6f);
                    DrawTextCentered($"Coins: {_coins}", new Vector2(ScreenWidth / 2f, 210), Color.Gold, 1.3f);

                    for (int i = 0; i < _currentItemChoices.Count; i++)
                    {
                        var rect = GetItemCardRect(i);
                        var item = _currentItemChoices[i];
                        bool hovered = _hoveredItemCard == i;
                        bool affordable = _coins >= item.Price;
                        Color rarityColor = GetRarityColor(item.Rarity);

                        Color baseCardBg = item.Rarity switch
                        {
                            ItemRarity.Common => new Color(30, 30, 45),
                            ItemRarity.Uncommon => new Color(20, 45, 30),
                            ItemRarity.Rare => new Color(20, 35, 55),
                            ItemRarity.Epic => new Color(40, 25, 55),
                            ItemRarity.Legendary => new Color(50, 40, 20),
                            ItemRarity.Mythic => new Color(55, 20, 20),
                            _ => new Color(30, 30, 45)
                        };

                        DrawRect(rect, hovered && affordable ? new Color(60, 60, 95) : baseCardBg);
                        DrawRectBorder(rect, !affordable ? Color.DarkGray : hovered ? Color.Gold : rarityColor, hovered && affordable ? 4 : 3);

                        DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 60), !affordable ? Color.Gray : rarityColor, 1.3f);
                        DrawTextCentered($"[{item.Rarity.ToString().ToUpper()}]", new Vector2(rect.Center.X, rect.Y + 110), rarityColor, 0.95f);
                        DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 185), Color.White, 0.9f);
                        DrawTextCentered($"Price: {item.Price} Coin", new Vector2(rect.Center.X, rect.Y + 245), affordable ? Color.LightGreen : Color.OrangeRed, 1f);
                    }

                    var skipRect = GetItemSkipButtonRect();
                    DrawRect(skipRect, _hoveredItemSkip ? Color.Gold : new Color(80, 80, 80));
                    DrawRectBorder(skipRect, Color.White, 3);
                    DrawTextCentered("Skip ->", new Vector2(skipRect.Center.X, skipRect.Center.Y), Color.White, 1.1f);
                }
            }
            else if (_battle.State == BattleState.EnemyWin)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Red, 3);
                DrawTextCentered("You Lose!", new Vector2(rect.Center.X, rect.Center.Y), Color.Red, 2f);

                var btnRestart = GetLoseRestartRect();
                DrawRect(btnRestart, _hoveredLoseRestart ? Color.Gold : new Color(50, 120, 50));
                DrawRectBorder(btnRestart, Color.White, 2);
                DrawTextCentered("Restart", new Vector2(btnRestart.Center.X, btnRestart.Center.Y), Color.White, 1.1f);

                var btnMenu = GetLoseMenuRect();
                DrawRect(btnMenu, _hoveredLoseMenu ? Color.Gold : new Color(150, 50, 50));
                DrawRectBorder(btnMenu, Color.White, 2);
                DrawTextCentered("Main Menu", new Vector2(btnMenu.Center.X, btnMenu.Center.Y), Color.White, 1.1f);
            }
        }

        private Color GetRarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common => Color.White,
            ItemRarity.Uncommon => Color.LimeGreen,
            ItemRarity.Rare => Color.DeepSkyBlue,
            ItemRarity.Epic => Color.MediumPurple,
            ItemRarity.Legendary => Color.Gold,
            ItemRarity.Mythic => Color.Red,
            _ => Color.White
        };

        private Rectangle GetItemCardRect(int i)
        {
            const int cardWidth = 400;
            const int cardHeight = 460;
            const int spacing = 50;
            int totalWidth = 3 * cardWidth + 2 * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = (ScreenHeight - cardHeight) / 2 + 20;
            return new Rectangle(startX + i * (cardWidth + spacing), y, cardWidth, cardHeight);
        }

        private Rectangle GetItemSkipButtonRect() => new Rectangle(ScreenWidth / 2 - 110, 830, 220, 56);
        private Rectangle GetStoryWinContinueRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetStoryWinMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetLoseRestartRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetLoseMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);

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