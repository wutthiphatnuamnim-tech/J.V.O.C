using How.Battle;
using How.Data;
using How.Manager;
using How.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace How
{
    internal class FloatingText
    {
        public Vector2 Position;
        public string Text;
        public float Timer;
        public Color Color;
        public float Scale;
    }

    public enum AppState
    {
        Logo,
        Intro,
        MainMenu,
        ModeSelect,
        Settings,
        Credits,
        TeamSetup,
        Battle,
        Upgrade,
        ItemSelect,
        Rest,
        Map,
        StoryModeSelect
    }

    internal enum TeamSetupOrigin
    {
        Story,
        ChallengeStart,
        ChallengeMap
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private BattleScreen _battleScreen;
        private Texture2D _pixel;
        private Texture2D _battleBackground;
        private Texture2D _introBackground;
        private Texture2D _logoBackground;
        private SpriteFont _font;
        private readonly Random _rng = new Random();

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

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private AppState _appState = AppState.Logo;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;

        private float _logoTimer;
        private const float LogoDuration = 2f;

        private float _introTimer;
        private const string IntroText = "Test 1 2 3 4 5 4RindaHAHAHA\n Tomyamkung";
        private const float IntroCharsPerSecond = 30f;
        private const float IntroInputGracePeriod = 0.2f;
        private int _hoveredMenuIndex = -1;

        // ตัวแปรสำหรับตั้งค่า Camera Shake
        private bool _cameraShakeEnabled = true;
        private bool _hoveredSettingsClose;
        private bool _hoveredSettingsToggle;

        private List<Character> _party;
        private List<Character> _allHeroes = new List<Character>();
        private Character[] _formationSlots = new Character[5];
        private TeamSetupOrigin _teamSetupOrigin;
        private bool _currentStageIsHard;
        private bool _manualModePreference = true;

        private int _upgradeSelectedIndex = -1;
        private int _upgradeSelectedTab = 0;
        private List<Item> _inventory = new List<Item>();
        private int _coins;

        private bool _showTeamWarning;
        private float _teamWarningTimer;

        private bool _hoveredStatTab;
        private bool _hoveredEquipTab;
        private int _hoveredHeroTrayIndex = -1;
        private int _hoveredStatPlusRow = -1;
        private int _hoveredStatMinusRow = -1;
        private bool _hoveredConfirmButton;
        private bool _hoveredContinue;
        private int _hoveredRestItemRow = -1;
        private bool _hoveredRestUnequip;
        private bool _hoveredRestContinue;

        private bool _isStoryMode;
        private int _storyStageIndex = 0;
        private const int MaxStoryStages = 5;
        private bool _hoveredStoryLeft;
        private bool _hoveredStoryRight;
        private bool _hoveredStoryCenter;
        private bool _hoveredStoryBack;

        private bool _hoveredModeStory;
        private bool _hoveredModeChallenge;
        private bool _hoveredModeBack;

        private bool _hoveredTeamStartButton;
        private bool _hoveredTeamBackButton;
        private int _hoveredSlotIndex = -1;

        private static readonly GrowthStat[] GrowthStats =
            { GrowthStat.STR, GrowthStat.INT, GrowthStat.VIT, GrowthStat.DEX, GrowthStat.LUX, GrowthStat.CRT };
        private int[] _pendingAllocations = new int[GrowthStats.Length];

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        private void PlayUIClick()
        {
            AudioManager.PlaySfx("Audio/UI_Click");
        }

        private void StartNewRunForChallenge()
        {
            _isStoryMode = false;
            _teamSetupOrigin = TeamSetupOrigin.ChallengeStart;
            _coins = 0;
            _inventory.Clear();
            _upgradeSelectedIndex = -1;
            _upgradeSelectedTab = 0;

            _allHeroes = BattleManager.CreateStartingParty();
            Array.Clear(_formationSlots, 0, _formationSlots.Length);

            for (int i = 0; i < Math.Min(_allHeroes.Count, 5); i++)
            {
                _formationSlots[i] = _allHeroes[i];
            }

            _appState = AppState.TeamSetup;
        }

        private void StartNewBattleForParty(bool isHard)
        {
            _party = new List<Character>();
            foreach (var c in _formationSlots)
            {
                if (c != null) _party.Add(c);
            }
            if (_party.Count == 0 && _allHeroes.Count > 0)
            {
                _party.Add(_allHeroes[0]);
            }

            _currentStageIsHard = isHard;

            _battleScreen.StartNewBattle(_party, _formationSlots, isHard, _isStoryMode, _storyStageIndex, ref _coins, _inventory, _manualModePreference, _cameraShakeEnabled);
            _appState = AppState.Battle;

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private Texture2D LoadTextureHelper(string folderName, string fileName)
        {
            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                string foundPath = null;

                for (int i = 0; i < 5; i++)
                {
                    string p1 = Path.Combine(current, folderName, fileName + ".png");
                    string p2 = Path.Combine(current, "Content", folderName, fileName + ".png");
                    string p3 = Path.Combine(current, fileName + ".png");

                    if (File.Exists(p1)) { foundPath = p1; break; }
                    if (File.Exists(p2)) { foundPath = p2; break; }
                    if (File.Exists(p3)) { foundPath = p3; break; }

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }

                if (foundPath != null)
                {
                    using (var stream = File.OpenRead(foundPath))
                    {
                        return Texture2D.FromStream(GraphicsDevice, stream);
                    }
                }
                else
                {
                    return Content.Load<Texture2D>($"{folderName}/{fileName}");
                }
            }
            catch
            {
                try { return Content.Load<Texture2D>(fileName); } catch { return null; }
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            AudioManager.LoadAll(
                Content,
                new string[] { "Audio/UI_Click", "Audio/Deadsound" },
                new string[] { "Audio/menu_theme", "Audio/battle_theme" }
            );

            string[] fontCandidates = { "DefaultFont", "SpriteFont", "Font", "Fonts/DefaultFont" };
            foreach (var name in fontCandidates)
            {
                try
                {
                    _font = Content.Load<SpriteFont>(name);
                    break;
                }
                catch
                {
                    _font = null;
                }
            }

            try { _battleBackground = Content.Load<Texture2D>("bg_Battle_01"); } catch { _battleBackground = null; }
            try { _introBackground = Content.Load<Texture2D>("Intro_BG_Test001"); } catch { _introBackground = null; }
            try { _logoBackground = Content.Load<Texture2D>("LogoTest1"); } catch { _logoBackground = null; }

            try { _elmaIdleTex = Content.Load<Texture2D>("char_Elma_Idle"); } catch { _elmaIdleTex = null; }
            try { _elmaAttackTex = Content.Load<Texture2D>("char_Elma_Attack"); } catch { _elmaAttackTex = null; }

            _deadEffectTex = LoadTextureHelper("Effect", "Dead_Effect");

            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                string foundPath = null;

                for (int i = 0; i < 5; i++)
                {
                    string p1 = Path.Combine(current, "Charater", "Test_Charater.png");
                    string p2 = Path.Combine(current, "Content", "Charater", "Test_Charater.png");
                    string p3 = Path.Combine(current, "Test_Charater.png");
                    string p4 = Path.Combine(current, "Character", "Test_Charater.png");

                    if (File.Exists(p1)) { foundPath = p1; break; }
                    if (File.Exists(p2)) { foundPath = p2; break; }
                    if (File.Exists(p3)) { foundPath = p3; break; }
                    if (File.Exists(p4)) { foundPath = p4; break; }

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }

                if (foundPath != null)
                {
                    using (var stream = File.OpenRead(foundPath))
                    {
                        _testCharacterTex = Texture2D.FromStream(GraphicsDevice, stream);
                    }
                }
                else
                {
                    _testCharacterTex = Content.Load<Texture2D>("Test_Charater");
                }
            }
            catch
            {
                _testCharacterTex = null;
            }

            _testEnemyTex = LoadTextureHelper("Enemy", "Test_Enemy");
            _orcWarriorTex = LoadTextureHelper("Enemy", "orc_warrior");
            _goblinArcherTex = LoadTextureHelper("Enemy", "goblin_archer");
            _darkMageTex = LoadTextureHelper("Enemy", "dark_mage");
            _ghostHealerTex = LoadTextureHelper("Enemy", "ghost_healer");
            _stoneGiantTex = LoadTextureHelper("Enemy", "stone_giant");

            var texDict = new Dictionary<string, Texture2D>
            {
                { "ElmaIdle", _elmaIdleTex },
                { "ElmaAttack", _elmaAttackTex },
                { "TestChar", _testCharacterTex },
                { "DeadEffect", _deadEffectTex },
                { "TestEnemy", _testEnemyTex },
                { "Orc", _orcWarriorTex },
                { "Goblin", _goblinArcherTex },
                { "DarkMage", _darkMageTex },
                { "Ghost", _ghostHealerTex },
                { "StoneGiant", _stoneGiantTex }
            };

            _battleScreen = new BattleScreen(
                GraphicsDevice,
                _spriteBatch,
                _font,
                _pixel,
                _battleBackground,
                texDict,
                _rng,
                state => _appState = state,
                PlayUIClick,
                () => { _upgradeSelectedIndex = -1; _upgradeSelectedTab = 0; ResetPendingAllocations(); _appState = AppState.Upgrade; },
                StartNewRunForChallenge
            );
        }

        protected override void Update(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_showTeamWarning)
            {
                _teamWarningTimer -= delta;
                if (_teamWarningTimer <= 0f) _showTeamWarning = false;
            }

            switch (_appState)
            {
                case AppState.Logo:
                    UpdateLogo(gameTime, keyboard, leftClicked);
                    break;

                case AppState.Intro:
                    UpdateIntro(gameTime, keyboard, leftClicked);
                    break;

                case AppState.MainMenu:
                    UpdateMainMenu(mouse, leftClicked, keyboard);
                    break;

                case AppState.ModeSelect:
                    UpdateModeSelect(mouse, leftClicked, keyboard);
                    break;

                case AppState.StoryModeSelect:
                    UpdateStoryModeSelect(mouse, leftClicked, keyboard);
                    break;

                case AppState.TeamSetup:
                    UpdateTeamSetup(mouse, leftClicked, keyboard);
                    break;

                case AppState.Settings:
                    UpdateSettings(mouse, leftClicked, keyboard);
                    break;

                case AppState.Credits:
                    if (leftClicked || keyboard.IsKeyDown(Keys.Escape))
                    {
                        if (leftClicked) PlayUIClick();
                        _appState = AppState.MainMenu;
                    }
                    break;

                case AppState.Battle:
                    _battleScreen.Update(gameTime, keyboard, mouse, leftClicked, ref _manualModePreference, ref _coins, _inventory);
                    break;

                case AppState.Upgrade:
                    UpdateUpgrade(mouse, leftClicked);
                    break;

                case AppState.ItemSelect:
                    UpdateItemSelect(mouse, leftClicked);
                    break;

                case AppState.Rest:
                    UpdateRest(mouse, leftClicked);
                    break;

                case AppState.Map:
                    UpdateMap(mouse, leftClicked);
                    break;
            }

            _previousMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
        }

        private void UpdateLogo(GameTime gameTime, KeyboardState keyboard, bool leftClicked)
        {
            _logoTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!_previousKeyboardState.IsKeyDown(pressedKeys[i]))
                {
                    anyNewKeyPressed = true;
                    break;
                }
            }

            if (_logoTimer >= LogoDuration || leftClicked || anyNewKeyPressed)
            {
                _appState = AppState.Intro;
                _introTimer = 0f;
            }
        }

        private void UpdateIntro(GameTime gameTime, KeyboardState keyboard, bool leftClicked)
        {
            _introTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!_previousKeyboardState.IsKeyDown(pressedKeys[i]))
                {
                    anyNewKeyPressed = true;
                    break;
                }
            }

            bool inputPressed = leftClicked || anyNewKeyPressed;
            bool pastGracePeriod = _introTimer >= IntroInputGracePeriod;

            int totalChars = IntroText.Length;
            float fullRevealTime = totalChars / IntroCharsPerSecond;
            bool fullyRevealed = _introTimer >= fullRevealTime;

            if (inputPressed && pastGracePeriod)
            {
                if (!fullyRevealed)
                    _introTimer = fullRevealTime;
                else
                    _appState = AppState.MainMenu;
            }
        }

        private void UpdateMainMenu(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            AudioManager.PlayBgm("Audio/menu_theme");

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

            _hoveredMenuIndex = -1;
            for (int i = 0; i < MenuLabels.Length; i++)
            {
                if (!MenuEnabled[i]) continue;
                var layout = GetMenuButtonLayout(i);
                if (layout.Rect.Contains(mouse.Position))
                    _hoveredMenuIndex = i;
            }

            if (leftClicked && _hoveredMenuIndex >= 0)
            {
                PlayUIClick();
                switch (_hoveredMenuIndex)
                {
                    case 0: _appState = AppState.ModeSelect; break;
                    case 1: _appState = AppState.Settings; break;
                    case 2: _appState = AppState.Credits; break;
                }
            }
        }

        private Rectangle GetSettingsWindowRect() => new Rectangle(ScreenWidth / 2 - 450, ScreenHeight / 2 - 275, 900, 550);
        private Rectangle GetSettingsCloseButtonRect() => new Rectangle(GetSettingsWindowRect().Right - 70, GetSettingsWindowRect().Y + 20, 50, 50);
        private Rectangle GetSettingsToggleRect() => new Rectangle(GetSettingsWindowRect().X + 620, GetSettingsWindowRect().Y + 170, 140, 45);

        private void UpdateSettings(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                PlayUIClick();
                _appState = AppState.MainMenu;
                return;
            }

            var closeRect = GetSettingsCloseButtonRect();
            var toggleRect = GetSettingsToggleRect();

            _hoveredSettingsClose = closeRect.Contains(mouse.Position);
            _hoveredSettingsToggle = toggleRect.Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredSettingsClose)
                {
                    PlayUIClick();
                    _appState = AppState.MainMenu;
                }
                else if (_hoveredSettingsToggle)
                {
                    PlayUIClick();
                    _cameraShakeEnabled = !_cameraShakeEnabled;
                }
            }
        }

        private Rectangle GetModeSelectStoryCardRect() => new Rectangle(ScreenWidth / 2 - 440, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectChallengeCardRect() => new Rectangle(ScreenWidth / 2 + 40, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectBackRect() => new Rectangle(40, 40, 150, 50);

        private void UpdateModeSelect(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                PlayUIClick();
                _appState = AppState.MainMenu;
                return;
            }

            _hoveredModeStory = GetModeSelectStoryCardRect().Contains(mouse.Position);
            _hoveredModeChallenge = GetModeSelectChallengeCardRect().Contains(mouse.Position);
            _hoveredModeBack = GetModeSelectBackRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredModeBack) { PlayUIClick(); _appState = AppState.MainMenu; }
                else if (_hoveredModeStory) { PlayUIClick(); _appState = AppState.StoryModeSelect; }
                else if (_hoveredModeChallenge) { PlayUIClick(); StartNewRunForChallenge(); }
            }
        }

        private Rectangle GetStoryCenterCardRect() => new Rectangle(ScreenWidth / 2 - 180, ScreenHeight / 2 - 250, 360, 500);
        private Rectangle GetStoryLeftCardRect() => new Rectangle(ScreenWidth / 2 - 420, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryRightCardRect() => new Rectangle(ScreenWidth / 2 + 120, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryLeftArrowRect() => new Rectangle(ScreenWidth / 2 - 600, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryRightArrowRect() => new Rectangle(ScreenWidth / 2 + 540, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryBackButtonRect() => new Rectangle(40, 40, 150, 50);
        private Rectangle GetTeamSetupStartButtonRect() => new Rectangle(ScreenWidth / 2 - 110, ScreenHeight - 250, 220, 50);
        private Rectangle GetTeamSetupBackButtonRect() => new Rectangle(40, 40, 150, 50);

        private Rectangle GetFormationSlotRect(int i)
        {
            const int size = 120;
            const int gap = 25;
            int startX = ScreenWidth / 2 - 200;
            int startY = ScreenHeight / 2 - 240;

            if (i < 3)
            {
                return new Rectangle(startX + size + 60, startY - 30 + i * (size + gap - 10), size, size);
            }
            else
            {
                int backIndex = i - 3;
                return new Rectangle(startX, startY + backIndex * (size + gap), size, size);
            }
        }

        private Rectangle GetHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _allHeroes.Count * w + (_allHeroes.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private Rectangle GetUpgradeStatTabRect() => new Rectangle(120, 320, 70, 70);
        private Rectangle GetUpgradeEquipTabRect() => new Rectangle(120, 410, 70, 70);
        private Rectangle GetUpgradeCharacterCardRect() => new Rectangle(230, 220, 280, 520);
        private Rectangle GetUpgradeInfoPanelRect() => new Rectangle(530, 220, 380, 520);
        private Rectangle GetUpgradeRightPanelRect() => new Rectangle(930, 220, 520, 520);

        private Rectangle GetStatRowMinusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 150, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetStatRowPlusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 80, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetUpgradeConfirmButtonRect() => new Rectangle(GetUpgradeRightPanelRect().X + 30, GetUpgradeRightPanelRect().Bottom - 80, GetUpgradeRightPanelRect().Width - 60, 50);

        private Rectangle GetEquipItemSlotRect() => new Rectangle(GetUpgradeRightPanelRect().Center.X - 50, GetUpgradeRightPanelRect().Y + 80, 100, 100);
        private Rectangle GetEquipInventoryItemRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().X + 30, GetUpgradeRightPanelRect().Y + 220 + rowIndex * 90, GetUpgradeRightPanelRect().Width - 60, 75);
        private Rectangle GetEquipUnequipButtonRect() => new Rectangle(GetUpgradeRightPanelRect().Center.X - 100, GetUpgradeRightPanelRect().Y + 195, 200, 38);

        private Rectangle GetUpgradeHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _party.Count * w + (_party.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private void UpdateStoryModeSelect(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape)) { PlayUIClick(); _appState = AppState.ModeSelect; return; }

            _hoveredStoryCenter = GetStoryCenterCardRect().Contains(mouse.Position);
            _hoveredStoryLeft = _storyStageIndex > 0 && GetStoryLeftArrowRect().Contains(mouse.Position);
            _hoveredStoryRight = _storyStageIndex < MaxStoryStages - 1 && GetStoryRightArrowRect().Contains(mouse.Position);
            _hoveredStoryBack = GetStoryBackButtonRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredStoryLeft) { PlayUIClick(); _storyStageIndex--; }
                else if (_hoveredStoryRight) { PlayUIClick(); _storyStageIndex++; }
                else if (_hoveredStoryBack) { PlayUIClick(); _appState = AppState.ModeSelect; }
                else if (_hoveredStoryCenter)
                {
                    PlayUIClick();
                    _isStoryMode = true;
                    _teamSetupOrigin = TeamSetupOrigin.Story;
                    _allHeroes = BattleManager.CreateStartingParty();
                    Array.Clear(_formationSlots, 0, _formationSlots.Length);

                    if (_storyStageIndex == 0)
                    {
                        Character mage = null;
                        foreach (var c in _allHeroes) { if (c.Name == "Mage") { mage = c; break; } }
                        if (mage == null && _allHeroes.Count > 0) mage = _allHeroes[0];
                        _formationSlots[1] = mage;
                    }
                    else
                    {
                        for (int i = 0; i < Math.Min(_allHeroes.Count, 5); i++) _formationSlots[i] = _allHeroes[i];
                    }
                    _appState = AppState.TeamSetup;
                }
            }
        }

        private void UpdateTeamSetup(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape)) { PlayUIClick(); GoBackFromTeamSetup(); return; }

            _hoveredTeamStartButton = GetTeamSetupStartButtonRect().Contains(mouse.Position);
            _hoveredTeamBackButton = GetTeamSetupBackButtonRect().Contains(mouse.Position);

            _hoveredSlotIndex = -1;
            for (int i = 0; i < 5; i++)
            {
                if (GetFormationSlotRect(i).Contains(mouse.Position)) _hoveredSlotIndex = i;
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _allHeroes.Count; i++)
            {
                if (GetHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            if (leftClicked)
            {
                if (_hoveredTeamBackButton) { PlayUIClick(); GoBackFromTeamSetup(); }
                else if (_hoveredTeamStartButton)
                {
                    bool hasCharacter = false;
                    foreach (var c in _formationSlots)
                    {
                        if (c != null) { hasCharacter = true; break; }
                    }

                    if (hasCharacter)
                    {
                        PlayUIClick();
                        StartBattleFromTeamSetup();
                    }
                    else
                    {
                        PlayUIClick();
                        _showTeamWarning = true;
                        _teamWarningTimer = 2.0f;
                    }
                }
                else if (_hoveredSlotIndex >= 0)
                {
                    PlayUIClick();
                    if (!(_isStoryMode && _storyStageIndex == 0)) { _formationSlots[_hoveredSlotIndex] = null; }
                }
                else if (_hoveredHeroTrayIndex >= 0)
                {
                    PlayUIClick();
                    if (!(_isStoryMode && _storyStageIndex == 0))
                    {
                        var clickedHero = _allHeroes[_hoveredHeroTrayIndex];
                        int existingSlot = -1;
                        for (int i = 0; i < 5; i++) { if (_formationSlots[i] == clickedHero) { existingSlot = i; break; } }

                        if (existingSlot >= 0)
                        {
                            int activeCount = 0;
                            for (int i = 0; i < 5; i++) if (_formationSlots[i] != null) activeCount++;
                            if (activeCount > 1) _formationSlots[existingSlot] = null;
                        }
                        else
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                if (_formationSlots[i] == null) { _formationSlots[i] = clickedHero; break; }
                            }
                        }
                    }
                }
            }
        }

        private void GoBackFromTeamSetup()
        {
            if (_isStoryMode) _appState = AppState.StoryModeSelect;
            else if (_teamSetupOrigin == TeamSetupOrigin.ChallengeStart) _appState = AppState.ModeSelect;
            else _appState = AppState.Map;
        }

        private void StartBattleFromTeamSetup()
        {
            StartNewBattleForParty(_teamSetupOrigin == TeamSetupOrigin.ChallengeMap && _currentStageIsHard);
        }

        private void UpdateUpgrade(MouseState mouse, bool leftClicked)
        {
            _hoveredStatTab = GetUpgradeStatTabRect().Contains(mouse.Position);
            _hoveredEquipTab = GetUpgradeEquipTabRect().Contains(mouse.Position);

            if (leftClicked && GetUpgradeCharacterCardRect().Contains(mouse.Position))
            {
                if (_upgradeSelectedIndex >= 0)
                {
                    PlayUIClick();
                    _upgradeSelectedIndex = -1;
                    _upgradeSelectedTab = 0;
                    ResetPendingAllocations();
                }
            }

            if (leftClicked && _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count)
            {
                if (_hoveredStatTab)
                {
                    PlayUIClick();
                    if (_upgradeSelectedTab == 0) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 0; ResetPendingAllocations(); }
                }
                else if (_hoveredEquipTab)
                {
                    PlayUIClick();
                    if (_upgradeSelectedTab == 1) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 1; ResetPendingAllocations(); }
                }
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _party.Count; i++)
            {
                if (GetUpgradeHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            if (leftClicked && _hoveredHeroTrayIndex >= 0 && _hoveredHeroTrayIndex != _upgradeSelectedIndex)
            {
                PlayUIClick();
                _upgradeSelectedIndex = _hoveredHeroTrayIndex;
                if (_upgradeSelectedTab < 0) _upgradeSelectedTab = 0;
                ResetPendingAllocations();
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count && _upgradeSelectedTab >= 0)
            {
                var selected = _party[_upgradeSelectedIndex];

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
                        if (_hoveredStatPlusRow >= 0 && remaining > 0) { PlayUIClick(); _pendingAllocations[_hoveredStatPlusRow]++; }
                        else if (_hoveredStatMinusRow >= 0 && _pendingAllocations[_hoveredStatMinusRow] > 0) { PlayUIClick(); _pendingAllocations[_hoveredStatMinusRow]--; }
                        else if (_hoveredConfirmButton)
                        {
                            PlayUIClick();
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
                    _hoveredRestItemRow = -1;
                    _hoveredRestUnequip = false;

                    for (int row = 0; row < _inventory.Count; row++)
                    {
                        if (GetEquipInventoryItemRect(row).Contains(mouse.Position)) _hoveredRestItemRow = row;
                    }
                    _hoveredRestUnequip = selected.EquippedItem != null && GetEquipUnequipButtonRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredRestItemRow >= 0)
                        {
                            PlayUIClick();
                            var newItem = _inventory[_hoveredRestItemRow];
                            var previousItem = selected.EquippedItem;
                            selected.EquipItem(newItem);
                            _inventory.RemoveAt(_hoveredRestItemRow);
                            if (previousItem != null) _inventory.Add(previousItem);
                        }
                        else if (_hoveredRestUnequip)
                        {
                            PlayUIClick();
                            var removedItem = selected.EquippedItem;
                            selected.UnequipItem();
                            if (removedItem != null) _inventory.Add(removedItem);
                        }
                    }
                }
            }

            _hoveredContinue = GetContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredContinue)
            {
                PlayUIClick();
                _upgradeSelectedIndex = -1;
                _upgradeSelectedTab = 0;
                ResetPendingAllocations();
                _appState = AppState.Map;
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
        }

        private void UpdateItemSelect(MouseState mouse, bool leftClicked) { }

        private void UpdateRest(MouseState mouse, bool leftClicked)
        {
            _hoveredRestContinue = GetRestContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredRestContinue)
            {
                PlayUIClick();
                _appState = AppState.Map;
            }
        }

        private void UpdateMap(MouseState mouse, bool leftClicked)
        {
            int hoveredMapChoice = -1;
            for (int i = 0; i < 2; i++)
            {
                if (GetMapButtonRect(i).Contains(mouse.Position)) hoveredMapChoice = i;
            }

            if (leftClicked && hoveredMapChoice >= 0)
            {
                PlayUIClick();
                _currentStageIsHard = (hoveredMapChoice == 1);
                _teamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                Array.Clear(_formationSlots, 0, _formationSlots.Length);
                for (int i = 0; i < Math.Min(_party.Count, 5); i++) _formationSlots[i] = _party[i];
                _appState = AppState.TeamSetup;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            Color clearColor = (_appState == AppState.Logo || _appState == AppState.Intro) ? Color.Black : new Color(30, 30, 45);
            GraphicsDevice.Clear(clearColor);

            _spriteBatch.Begin();

            switch (_appState)
            {
                case AppState.Logo: DrawLogo(); break;
                case AppState.Intro: DrawIntro(); break;
                case AppState.MainMenu: DrawMainMenu(); break;
                case AppState.ModeSelect: DrawModeSelect(); break;
                case AppState.StoryModeSelect: DrawStoryModeSelect(); break;
                case AppState.TeamSetup: DrawTeamSetup(); break;
                case AppState.Settings: DrawMainMenu(); DrawSettings(); break;
                case AppState.Credits: DrawPlaceholderScreen("Credit", "Made with MonoGame - click anywhere or press Esc to go back"); break;
                case AppState.Battle:
                    _spriteBatch.End();
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                    _battleScreen.Draw();
                    _spriteBatch.End();
                    _spriteBatch.Begin();
                    break;
                case AppState.Upgrade: DrawUpgrade(); break;
                case AppState.ItemSelect: DrawItemSelect(); break;
                case AppState.Rest: DrawRest(); break;
                case AppState.Map: DrawMap(); break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawLogo()
        {
            if (_logoBackground != null)
                _spriteBatch.Draw(_logoBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            else
                DrawTextCentered("LOGO", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.White, 3f);
        }

        private const string GameTitle = "Where is my slime";
        private const int TitleY = 180;
        private static readonly string[] MenuLabels = { "Play", "Setting", "Credit" };
        private static readonly bool[] MenuEnabled = { true, true, true };

        private const int MenuButtonSpacing = 130;
        private const float MenuButtonScale = 2.6f;
        private const float TitleScale = 4.5f;
        private const int MenuRightMargin = 80;

        private struct MenuButtonLayout
        {
            public Rectangle Rect;
            public Vector2 Center;
            public Vector2 TextSize;
        }

        private MenuButtonLayout GetMenuButtonLayout(int index)
        {
            if (_font == null)
                return new MenuButtonLayout { Rect = Rectangle.Empty, Center = Vector2.Zero, TextSize = Vector2.Zero };

            string label = MenuLabels[index];
            Vector2 rawSize = _font.MeasureString(label);
            Vector2 scaledSize = rawSize * MenuButtonScale;

            int centerX = ScreenWidth - MenuRightMargin - (int)(scaledSize.X / 2f);
            int centerY = TitleY + 140 + index * MenuButtonSpacing;

            var rect = new Rectangle(
                (int)(centerX - scaledSize.X / 2f - 30),
                (int)(centerY - scaledSize.Y / 2f - 12),
                (int)scaledSize.X + 60,
                (int)scaledSize.Y + 24);

            return new MenuButtonLayout { Rect = rect, Center = new Vector2(centerX, centerY), TextSize = rawSize };
        }

        private const int IntroTextBoxHeight = 200;
        private const int IntroDividerThickness = 6;
        private const float IntroTextScale = 2.5f;

        private void DrawIntro()
        {
            int lineY = ScreenHeight - IntroTextBoxHeight - IntroDividerThickness;

            if (_introBackground != null)
                _spriteBatch.Draw(_introBackground, new Rectangle(0, 0, ScreenWidth, lineY), Color.White);

            var lineRect = new Rectangle(0, lineY, ScreenWidth, IntroDividerThickness);
            DrawRect(lineRect, Color.Black);

            var textBoxRect = new Rectangle(0, lineY + IntroDividerThickness, ScreenWidth, IntroTextBoxHeight);
            DrawRect(textBoxRect, new Color(0, 0, 0, 210));

            int totalChars = IntroText.Length;
            int revealedChars = Math.Min(totalChars, (int)(_introTimer * IntroCharsPerSecond));
            string revealedText = IntroText.Substring(0, revealedChars);
            DrawText(revealedText, new Vector2(60, textBoxRect.Y + 30), Color.White, IntroTextScale);

            bool fullyRevealed = revealedChars >= totalChars;
            if (fullyRevealed)
                DrawTextCentered("Click or press any key to continue", new Vector2(ScreenWidth / 2f, textBoxRect.Bottom - 30), Color.LightGray, 0.8f);
        }

        private void DrawMainMenu()
        {
            if (_font != null)
            {
                Vector2 titleSize = _font.MeasureString(GameTitle) * TitleScale;
                float titleCenterX = ScreenWidth - MenuRightMargin - titleSize.X / 2f;
                DrawTextCentered(GameTitle, new Vector2(titleCenterX, TitleY), Color.Gold, TitleScale);
            }

            for (int i = 0; i < MenuLabels.Length; i++)
            {
                var layout = GetMenuButtonLayout(i);
                bool hovered = _hoveredMenuIndex == i;
                bool enabled = MenuEnabled[i];
                Color color = !enabled ? Color.DimGray : hovered ? Color.Gold : Color.White;

                DrawTextCentered(MenuLabels[i], layout.Center, color, MenuButtonScale);

                if (hovered && enabled)
                {
                    var arrowRect = new Rectangle(layout.Rect.X - 45, (int)(layout.Center.Y - 15), 25, 30);
                    DrawArrow(arrowRect, false, Color.Gold, 4);
                }
            }
        }

        private void DrawSettings()
        {
            // วาดพื้นหลังมืดโปร่งแสงทับเมนูหลัก
            DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

            var windowRect = GetSettingsWindowRect();
            DrawRect(windowRect, Color.White);
            DrawRectBorder(windowRect, Color.Black, 4);

            // หัวข้อ Setting
            DrawTextCentered("Setting", new Vector2(windowRect.Center.X, windowRect.Y + 50), Color.Black, 2.2f);

            // หัวข้อ Camera Shake
            DrawText("Camera Shake", new Vector2(windowRect.X + 80, windowRect.Y + 175), Color.Black, 1.5f);

            // พื้นหลัง Track ของ Toggle Switch
            var toggleRect = GetSettingsToggleRect();
            DrawRect(toggleRect, new Color(220, 220, 220));
            DrawRectBorder(toggleRect, Color.Black, 2);

            // ข้อความตัวหนังสือจางๆ ด้านฝั่งที่ไม่ได้เลือก
            DrawTextCentered("ON", new Vector2(toggleRect.X + 35, toggleRect.Center.Y), _cameraShakeEnabled ? Color.Transparent : Color.Gray, 1.1f);
            DrawTextCentered("OFF", new Vector2(toggleRect.Right - 35, toggleRect.Center.Y), !_cameraShakeEnabled ? Color.Transparent : Color.Gray, 1.1f);

            // สี่เหลี่ยมเลื่อนได้ (Slider Box)
            int boxWidth = 75;
            int boxX = _cameraShakeEnabled ? toggleRect.X : toggleRect.Right - boxWidth;
            var sliderBox = new Rectangle(boxX, toggleRect.Y, boxWidth, toggleRect.Height);

            Color sliderColor = _cameraShakeEnabled ? new Color(40, 160, 60) : new Color(180, 50, 50);
            string sliderText = _cameraShakeEnabled ? "ON" : "OFF";

            DrawRect(sliderBox, sliderColor);
            DrawRectBorder(sliderBox, Color.Black, 2);
            DrawTextCentered(sliderText, new Vector2(sliderBox.Center.X, sliderBox.Center.Y), Color.White, 1.1f);

            // ปุ่มปิด X (มุมขวาบน)
            var closeRect = GetSettingsCloseButtonRect();
            DrawRect(closeRect, _hoveredSettingsClose ? Color.DarkRed : new Color(220, 40, 40));
            DrawRectBorder(closeRect, Color.Black, 3);
            DrawTextCentered("X", new Vector2(closeRect.Center.X, closeRect.Center.Y - 2), Color.White, 1.8f);
        }

        private void DrawModeSelect()
        {
            DrawTextCentered("Select Game Mode", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 2.8f);

            var storyRect = GetModeSelectStoryCardRect();
            DrawRect(storyRect, _hoveredModeStory ? new Color(50, 50, 75) : new Color(30, 30, 45));
            DrawRectBorder(storyRect, _hoveredModeStory ? Color.Gold : Color.White, _hoveredModeStory ? 5 : 3);
            DrawTextCentered("Story Mode", new Vector2(storyRect.Center.X, storyRect.Center.Y), Color.White, 2f);

            var challengeRect = GetModeSelectChallengeCardRect();
            DrawRect(challengeRect, _hoveredModeChallenge ? new Color(50, 50, 75) : new Color(30, 30, 45));
            DrawRectBorder(challengeRect, _hoveredModeChallenge ? Color.Gold : Color.White, _hoveredModeChallenge ? 5 : 3);
            DrawTextCentered("Challenge mode", new Vector2(challengeRect.Center.X, challengeRect.Center.Y), Color.White, 2f);

            var backRect = GetModeSelectBackRect();
            DrawRect(backRect, _hoveredModeBack ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawPlaceholderScreen(string title, string subtitle)
        {
            DrawTextCentered(title, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f - 40), Color.Gold, 2.4f);
            DrawTextCentered(subtitle, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f + 30), Color.White, 1f);
        }

        private void DrawStoryModeSelect()
        {
            DrawTextCentered("Story Mode", new Vector2(ScreenWidth / 2f, 100), Color.Gold, 2.5f);

            if (_storyStageIndex > 0)
            {
                var leftRect = GetStoryLeftCardRect();
                DrawRect(leftRect, new Color(20, 20, 25));
                DrawRectBorder(leftRect, Color.Gray, 3);
                DrawTextCentered($"1-{_storyStageIndex}", new Vector2(ScreenWidth / 2 - 300, leftRect.Bottom - 60), Color.Gray, 1.2f);
            }

            if (_storyStageIndex < MaxStoryStages - 1)
            {
                var rightRect = GetStoryRightCardRect();
                DrawRect(rightRect, new Color(20, 20, 25));
                DrawRectBorder(rightRect, Color.Gray, 3);
                DrawTextCentered($"1-{_storyStageIndex + 2}", new Vector2(ScreenWidth / 2 + 300, rightRect.Bottom - 60), Color.Gray, 1.2f);
            }

            var centerRect = GetStoryCenterCardRect();
            DrawRect(centerRect, _hoveredStoryCenter ? new Color(50, 50, 70) : new Color(30, 30, 45));
            DrawRectBorder(centerRect, _hoveredStoryCenter ? Color.Gold : Color.White, 5);

            DrawTextCentered($"1-{_storyStageIndex + 1}", new Vector2(centerRect.Center.X, centerRect.Bottom - 80), Color.White, 3f);

            if (_hoveredStoryCenter)
                DrawTextCentered("Click to Play", new Vector2(centerRect.Center.X, centerRect.Center.Y), Color.Gold, 1.5f);

            if (_storyStageIndex > 0)
                DrawArrow(GetStoryLeftArrowRect(), true, _hoveredStoryLeft ? Color.Gold : Color.White, 8);

            if (_storyStageIndex < MaxStoryStages - 1)
                DrawArrow(GetStoryRightArrowRect(), false, _hoveredStoryRight ? Color.Gold : Color.White, 8);

            var backRect = GetStoryBackButtonRect();
            DrawRect(backRect, _hoveredStoryBack ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawTeamSetup()
        {
            string modeTitle = _isStoryMode ? $"Story Mode (1-{_storyStageIndex + 1})" : "Challenge Mode";
            DrawTextCentered($"Team Setup - {modeTitle}", new Vector2(ScreenWidth / 2f, 50), Color.Gold, 2.2f);

            for (int i = 0; i < 5; i++)
            {
                var slotRect = GetFormationSlotRect(i);
                bool hovered = _hoveredSlotIndex == i;
                Character c = _formationSlots[i];

                Color bgColor = c != null ? new Color(50, 100, 180) : (hovered ? Color.DarkSlateGray : new Color(35, 35, 50));
                DrawRect(slotRect, bgColor);
                DrawRectBorder(slotRect, hovered ? Color.Gold : Color.White, hovered ? 3 : 2);

                if (c != null)
                {
                    DrawTextCentered(c.Name, new Vector2(slotRect.Center.X, slotRect.Center.Y - 15), Color.White, 1f);
                    DrawTextCentered($"HP:{c.MaxHp}", new Vector2(slotRect.Center.X, slotRect.Center.Y + 15), Color.LightGreen, 0.8f);
                }
                else
                {
                    DrawTextCentered("+", new Vector2(slotRect.Center.X, slotRect.Center.Y), Color.LightGray, 2.5f);
                }
            }

            int lineY = ScreenHeight - 200;
            DrawLine(new Vector2(100, lineY), new Vector2(ScreenWidth - 100, lineY), Color.Gold, 3);
            DrawText("Hero Tray", new Vector2(120, lineY - 35), Color.Gold, 1.2f);

            for (int i = 0; i < _allHeroes.Count; i++)
            {
                var hero = _allHeroes[i];
                var trayRect = GetHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;

                bool inTeam = false;
                for (int s = 0; s < 5; s++) { if (_formationSlots[s] == hero) { inTeam = true; break; } }

                Color heroBg = inTeam ? new Color(60, 60, 60) : (hovered ? new Color(70, 70, 100) : new Color(30, 30, 45));
                DrawRect(trayRect, heroBg);
                DrawRectBorder(trayRect, inTeam ? Color.Gray : (hovered ? Color.Gold : Color.White), hovered ? 3 : 2);

                DrawTextCentered(hero.Name, new Vector2(trayRect.Center.X, trayRect.Y + 30), inTeam ? Color.Gray : Color.Gold, 1.1f);
                DrawTextCentered($"HP:{hero.MaxHp}", new Vector2(trayRect.Center.X, trayRect.Y + 70), Color.White, 0.8f);

                string badge = inTeam ? "[In Team]" : "[Available]";
                DrawTextCentered(badge, new Vector2(trayRect.Center.X, trayRect.Bottom - 25), inTeam ? Color.DimGray : Color.LightGreen, 0.75f);
            }

            var startRect = GetTeamSetupStartButtonRect();
            DrawRect(startRect, _hoveredTeamStartButton ? Color.Gold : new Color(50, 150, 50));
            DrawRectBorder(startRect, Color.White, 3);
            DrawTextCentered("Start Battle", new Vector2(startRect.Center.X, startRect.Center.Y), Color.White, 1.1f);

            if (_showTeamWarning)
            {
                DrawTextCentered("Please choose a character to play", new Vector2(ScreenWidth / 2f, startRect.Y - 30), Color.Red, 1.3f);
            }

            var backRect = GetTeamSetupBackButtonRect();
            DrawRect(backRect, _hoveredTeamBackButton ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawArrow(Rectangle rect, bool isLeft, Color color, int thickness)
        {
            Vector2 top = isLeft ? new Vector2(rect.Right, rect.Top) : new Vector2(rect.Left, rect.Top);
            Vector2 mid = isLeft ? new Vector2(rect.Left, rect.Center.Y) : new Vector2(rect.Right, rect.Center.Y);
            Vector2 bot = isLeft ? new Vector2(rect.Right, rect.Bottom) : new Vector2(rect.Left, rect.Bottom);

            DrawLine(top, mid, color, thickness);
            DrawLine(mid, bot, color, thickness);
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

        private void DrawUpgrade()
        {
            DrawTextCentered("Upgrade", new Vector2(ScreenWidth / 2f, 80), Color.Gold, 2.6f);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count)
            {
                var statTabRect = GetUpgradeStatTabRect();
                DrawRect(statTabRect, _upgradeSelectedTab == 0 ? Color.Gold : (_hoveredStatTab ? new Color(70, 70, 100) : new Color(40, 40, 60)));
                DrawRectBorder(statTabRect, Color.White, 2);

                DrawRect(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Red);
                DrawRectBorder(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.LimeGreen);
                DrawRectBorder(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.DeepSkyBlue);
                DrawRectBorder(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.Black, 1);

                var equipTabRect = GetUpgradeEquipTabRect();
                DrawRect(equipTabRect, _upgradeSelectedTab == 1 ? Color.Gold : (_hoveredEquipTab ? new Color(70, 70, 100) : new Color(40, 40, 60)));
                DrawRectBorder(equipTabRect, Color.White, 2);

                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.White);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.Black, 1);
                DrawRect(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.White, 1);
                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.White, 1);
            }

            var cardRect = GetUpgradeCharacterCardRect();
            DrawRect(cardRect, new Color(25, 25, 35));
            DrawRectBorder(cardRect, Color.White, 3);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count)
            {
                var selectedChar = _party[_upgradeSelectedIndex];
                DrawTextCentered(selectedChar.Name, new Vector2(cardRect.Center.X, cardRect.Bottom + 35), Color.Gold, 1.3f);

                if (selectedChar.Name == "Elma" && _elmaIdleTex != null)
                {
                    int frameWidth = 160;
                    int frameHeight = 320;
                    Rectangle sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                    float scale = Math.Min((cardRect.Width - 40) / (float)frameWidth, (cardRect.Height - 40) / (float)frameHeight);
                    float drawW = frameWidth * scale;
                    float drawH = frameHeight * scale;
                    Vector2 drawPos = new Vector2(
                        cardRect.Center.X - drawW / 2f,
                        cardRect.Center.Y - drawH / 2f
                    );
                    _spriteBatch.Draw(_elmaIdleTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if (_testCharacterTex != null)
                {
                    float scale = Math.Min((cardRect.Width - 40) / (float)_testCharacterTex.Width, (cardRect.Height - 40) / (float)_testCharacterTex.Height);
                    float drawW = _testCharacterTex.Width * scale;
                    float drawH = _testCharacterTex.Height * scale;
                    Vector2 drawPos = new Vector2(
                        cardRect.Center.X - drawW / 2f,
                        cardRect.Center.Y - drawH / 2f
                    );
                    _spriteBatch.Draw(_testCharacterTex, drawPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                DrawTextCentered("+", new Vector2(cardRect.Center.X, cardRect.Center.Y), Color.LightGray, 3f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count)
            {
                var selectedChar = _party[_upgradeSelectedIndex];

                var infoRect = GetUpgradeInfoPanelRect();
                DrawRect(infoRect, new Color(20, 20, 30, 240));
                DrawRectBorder(infoRect, Color.Gold, 2);

                DrawText("INFO", new Vector2(infoRect.X + 15, infoRect.Y + 12), Color.Gold, 0.9f);

                DrawText($"{selectedChar.Name}", new Vector2(infoRect.X + 25, infoRect.Y + 45), Color.White, 1.2f);

                int pendingStr = _pendingAllocations[0];
                int pendingInt = _pendingAllocations[1];
                int pendingVit = _pendingAllocations[2];
                int pendingDex = _pendingAllocations[3];
                int pendingLux = _pendingAllocations[4];
                int pendingCrt = _pendingAllocations[5];

                int bonusHp = pendingVit * 50;
                int bonusPAtk = pendingStr * 10;
                int bonusPDef = pendingVit * 5;
                int bonusMAtk = pendingInt * 10;
                int bonusMDef = pendingVit * 3;
                int bonusCritRate = (pendingCrt * 2) + pendingLux;
                int bonusCritDmg = pendingCrt * 1;
                int bonusSpeed = pendingDex * 2;
                int bonusAccuracy = pendingDex * 1;
                int bonusEvide = pendingLux * 1;

                int statStartY = infoRect.Y + 95;
                int rowGap = 38;
                int col2X = infoRect.X + 210;

                DrawStatWithBonus($"HP: {selectedChar.MaxHp}", bonusHp > 0 ? $" (+{bonusHp})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 0), 0.9f);
                DrawStatWithBonus($"P. DEF: {selectedChar.PDef}", bonusPDef > 0 ? $" (+{bonusPDef})" : "", new Vector2(col2X, statStartY + rowGap * 0), 0.9f);

                DrawStatWithBonus($"P. ATK: {selectedChar.PAtk}", bonusPAtk > 0 ? $" (+{bonusPAtk})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 1), 0.9f);
                DrawStatWithBonus($"M. ATK: {selectedChar.MAtk}", bonusMAtk > 0 ? $" (+{bonusMAtk})" : "", new Vector2(col2X, statStartY + rowGap * 1), 0.9f);

                DrawStatWithBonus($"M. DEF: {selectedChar.MDef}", bonusMDef > 0 ? $" (+{bonusMDef})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 2), 0.9f);
                DrawStatWithBonus($"CRIT RATE: {(int)selectedChar.CritRate}%", bonusCritRate > 0 ? $" (+{bonusCritRate}%)" : "", new Vector2(col2X, statStartY + rowGap * 2), 0.9f);

                DrawStatWithBonus($"CRIT DMG: {(int)selectedChar.CritDamage}%", bonusCritDmg > 0 ? $" (+{bonusCritDmg}%)" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 3), 0.9f);
                DrawStatWithBonus($"SPEED: {selectedChar.Speed}", bonusSpeed > 0 ? $" (+{bonusSpeed})" : "", new Vector2(col2X, statStartY + rowGap * 3), 0.9f);

                DrawStatWithBonus($"ACC: {(int)selectedChar.Accuracy}%", bonusAccuracy > 0 ? $" (+{bonusAccuracy}%)" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 4), 0.9f);
                DrawStatWithBonus($"EVIDE: {selectedChar.Evasion}", bonusEvide > 0 ? $" (+{bonusEvide})" : "", new Vector2(col2X, statStartY + rowGap * 4), 0.9f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _party.Count && _upgradeSelectedTab >= 0)
            {
                var selectedChar = _party[_upgradeSelectedIndex];
                var rightRect = GetUpgradeRightPanelRect();
                DrawRect(rightRect, new Color(20, 20, 30, 240));
                DrawRectBorder(rightRect, Color.Gold, 2);

                if (_upgradeSelectedTab == 0)
                {
                    DrawText("STAT", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);
                    int pendingUsed = SumPendingAllocations();
                    int remainingPts = selectedChar.UpgradePoints - pendingUsed;

                    var ptsBox = new Rectangle(rightRect.Right - 110, rightRect.Y + 15, 85, 36);
                    DrawRect(ptsBox, new Color(40, 40, 55));
                    DrawRectBorder(ptsBox, Color.LimeGreen, 2);
                    DrawTextCentered($"{remainingPts}", new Vector2(ptsBox.Center.X, ptsBox.Center.Y), Color.LimeGreen, 1f);

                    for (int row = 0; row < GrowthStats.Length; row++)
                    {
                        var statName = GrowthStats[row].ToString();
                        int baseVal = GetGrowthValue(selectedChar, GrowthStats[row]);
                        int pending = _pendingAllocations[row];

                        var minusRect = GetStatRowMinusButtonRect(row);
                        var plusRect = GetStatRowPlusButtonRect(row);

                        string rowText = pending > 0 ? $"{statName} : {baseVal} (+{pending})" : $"{statName} : {baseVal}";
                        DrawText(rowText, new Vector2(rightRect.X + 30, minusRect.Y + 8), pending > 0 ? Color.Gold : Color.White, 1f);

                        DrawRect(minusRect, pending > 0 ? (_hoveredStatMinusRow == row ? Color.Gold : new Color(150, 50, 50)) : Color.DarkGray);
                        DrawRectBorder(minusRect, Color.White, 2);
                        DrawTextCentered("-", new Vector2(minusRect.Center.X, minusRect.Center.Y), Color.White, 1.2f);

                        DrawRect(plusRect, remainingPts > 0 ? (_hoveredStatPlusRow == row ? Color.Gold : new Color(50, 150, 50)) : Color.DarkGray);
                        DrawRectBorder(plusRect, Color.White, 2);
                        DrawTextCentered("+", new Vector2(plusRect.Center.X, plusRect.Center.Y), Color.White, 1.2f);
                    }

                    var confirmRect = GetUpgradeConfirmButtonRect();
                    DrawRect(confirmRect, pendingUsed > 0 ? (_hoveredConfirmButton ? Color.Gold : new Color(50, 120, 50)) : new Color(60, 60, 60));
                    DrawRectBorder(confirmRect, Color.White, 2);
                    DrawTextCentered(pendingUsed > 0 ? $"Confirm ({pendingUsed} pt)" : "Confirm", new Vector2(confirmRect.Center.X, confirmRect.Center.Y), Color.White, 1.1f);
                }
                else if (_upgradeSelectedTab == 1)
                {
                    DrawText("Equipment", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);

                    var equipSlot = GetEquipItemSlotRect();
                    DrawRect(equipSlot, new Color(40, 40, 60));
                    DrawRectBorder(equipSlot, selectedChar.EquippedItem != null ? Color.LimeGreen : Color.White, 2);
                    if (selectedChar.EquippedItem != null)
                    {
                        DrawTextCentered(selectedChar.EquippedItem.Name, new Vector2(equipSlot.Center.X, equipSlot.Center.Y - 10), Color.LimeGreen, 0.75f);
                        DrawTextCentered($"[{selectedChar.EquippedItem.Rarity}]", new Vector2(equipSlot.Center.X, equipSlot.Center.Y + 15), Color.White, 0.7f);
                    }
                    else
                    {
                        DrawTextCentered("+", new Vector2(equipSlot.Center.X, equipSlot.Center.Y), Color.Gray, 2f);
                    }

                    if (selectedChar.EquippedItem != null)
                    {
                        var unequipBtn = GetEquipUnequipButtonRect();
                        DrawRect(unequipBtn, _hoveredRestUnequip ? Color.Gold : new Color(150, 50, 50));
                        DrawRectBorder(unequipBtn, Color.White, 1);
                        DrawTextCentered("Unequip", new Vector2(unequipBtn.Center.X, unequipBtn.Center.Y), Color.White, 0.85f);
                    }

                    DrawText("Inventory Items:", new Vector2(rightRect.X + 30, rightRect.Y + 245), Color.White, 0.9f);
                    if (_inventory.Count == 0)
                    {
                        DrawText("No items in inventory", new Vector2(rightRect.X + 30, rightRect.Y + 280), Color.Gray, 0.9f);
                    }

                    for (int row = 0; row < Math.Min(_inventory.Count, 2); row++)
                    {
                        var itemRow = GetEquipInventoryItemRect(row);
                        bool hoveredRow = _hoveredRestItemRow == row;
                        DrawRect(itemRow, hoveredRow ? new Color(50, 50, 80) : new Color(30, 30, 45));
                        DrawRectBorder(itemRow, hoveredRow ? Color.Gold : Color.White, 2);

                        var invItem = _inventory[row];
                        DrawText(invItem.Name, new Vector2(itemRow.X + 15, itemRow.Y + 12), Color.Gold, 0.9f);
                        DrawText(invItem.Description, new Vector2(itemRow.X + 15, itemRow.Y + 40), Color.White, 0.75f);
                    }
                }
            }

            int trayLineY = ScreenHeight - 200;
            DrawLine(new Vector2(100, trayLineY), new Vector2(ScreenWidth - 100, trayLineY), Color.Gold, 3);
            DrawText("Hero Tray", new Vector2(120, trayLineY - 35), Color.Gold, 1.2f);

            for (int i = 0; i < _party.Count; i++)
            {
                var hero = _party[i];
                var trayRect = GetUpgradeHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;
                bool selected = _upgradeSelectedIndex == i;

                Color heroBg = selected ? new Color(60, 100, 180) : (hovered ? new Color(70, 70, 100) : new Color(30, 30, 45));
                DrawRect(trayRect, heroBg);
                DrawRectBorder(trayRect, selected ? Color.Gold : Color.White, selected ? 3 : 2);

                DrawTextCentered(hero.Name, new Vector2(trayRect.Center.X, trayRect.Y + 30), Color.Gold, 1.1f);
                DrawTextCentered($"Pts: {hero.UpgradePoints}", new Vector2(trayRect.Center.X, trayRect.Y + 70), Color.LightGreen, 0.8f);
            }

            var contRect = GetContinueButtonRect();
            DrawRect(contRect, _hoveredContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Done ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private Rectangle GetContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);
        private Rectangle GetRestContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        private void DrawItemSelect() { }

        private void DrawRest()
        {
            DrawTextCentered("Rest Camp", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 3f);
            DrawTextCentered($"Coins: {_coins}     |     Inventory: {_inventory.Count} item(s) available", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.2f);

            var contRect = GetRestContinueButtonRect();
            DrawRect(contRect, _hoveredRestContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Continue ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private Rectangle GetMapButtonRect(int i) => new Rectangle(((ScreenWidth - (2 * 560 + 80)) / 2) + i * (560 + 80), (ScreenHeight - 280) / 2 + 20, 560, 280);

        private void DrawMap()
        {
            DrawTextCentered("Choose Your Path", new Vector2(ScreenWidth / 2f, 140), Color.Gold, 2.6f);
            for (int i = 0; i < 2; i++)
            {
                var rect = GetMapButtonRect(i);
                bool hovered = GetMapButtonRect(i).Contains(Mouse.GetState().Position);
                DrawRect(rect, hovered ? new Color(70, 70, 110) : new Color(30, 30, 45));
                DrawRectBorder(rect, hovered ? Color.Gold : Color.White, hovered ? 4 : 2);
                DrawTextCentered(i == 0 ? "Normal Stage" : "Hard Stage", new Vector2(rect.Center.X, rect.Y + 60), Color.Gold, 1.6f);
                DrawTextCentered(i == 0 ? "Reward: +5 Points" : "Enemy HP / ATK +150%\nReward: +10 Points", new Vector2(rect.Center.X, rect.Center.Y + 30), Color.White, 1f);
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
    }
}