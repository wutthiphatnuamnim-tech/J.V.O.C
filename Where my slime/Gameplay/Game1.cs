using Where_My_Slime.Battle;
using Where_My_Slime.Manager;
using Where_My_Slime.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace Where_My_Slime
{
    internal class FloatingText
    {
        public Vector2 Position;
        public string Text;
        public float Timer;
        public float Duration;   // อายุทั้งหมดของข้อความ (ใช้คำนวณ pop / fade)
        public Color Color;
        public Color Outline;    // สีขอบตัวเลข
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
        Pause,
        Upgrade,
        ItemSelect,
        Shop,
        Rest,
        Map,
        StoryModeSelect
    }

    // Game1 เหลือหน้าที่แค่ "คนกลาง" (orchestrator): โหลดรีซอร์สที่ใช้ร่วมกัน,
    // ประกอบหน้าจอ (Screen) แต่ละคลาสเข้าด้วยกันผ่าน callback, แล้ว Update/Draw
    // หน้าจอที่กำลัง active อยู่ตาม _appState — ตรรกะจริงของแต่ละหน้าอยู่ในคลาส *Screen.cs แยกไฟล์
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _pixel;
        private Texture2D _battleBackground;
        private Texture2D _introBackground;
        private Texture2D _logoBackground;
        private Texture2D _mainMenuBackground;
        private SpriteFont _font;
        private SpriteFont _menuFont;
        private readonly Random _rng = new Random();

        private Texture2D _aliceIdleTex;
        private Texture2D _aliceAttackTex;
        private Texture2D _aliceIdleModeTex;
        private Texture2D _aliceAttackModeTex;
        private Texture2D _veyraIdleTex;
        private Texture2D _veyraAttackTex;
        private Texture2D _enosIdleTex;
        private Texture2D _enosAttackTex;
        private Texture2D _enosSkillTex;
        private Texture2D _veyraSkillTex;
        private Texture2D _elmaIdleTex;
        private Texture2D _elmaAttackTex;
        private Texture2D _elmaSkill01Tex;
        private Texture2D _elmaSkill02Tex;
        private Texture2D _lumiIdleTex;
        private Texture2D _lumiAttackTex;
        private Texture2D _lumiSkillBoomTex;
        private Texture2D _lumiSkillTex;
        private Texture2D _boomFxTex;
        private Texture2D _titleTex;
        private Texture2D _testCharacterTex;
        private Texture2D _deadEffectTex;
        private Texture2D _testEnemyTex;
        private Texture2D _orcWarriorTex;
        private Texture2D _goblinArcherTex;
        private Texture2D _wolfTex;
        private Texture2D _wolfAttackTex;
        private Texture2D _ghostHealerTex;
        private Texture2D _stoneGolemTex;
        private Texture2D _stoneGolemAttackTex;
        private Texture2D _bossAngelTex;
        private Texture2D _bossKingTex;
        private Texture2D _beeIdleTex;
        private Texture2D _beeAttackTex;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private AppState _appState = AppState.Logo;
        private AppState _settingsReturnState = AppState.MainMenu;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;

        private readonly GameSession _session = new GameSession();

        // --- หน้าจอต่างๆ (แต่ละหน้าแยกเป็นคลาสของตัวเอง เหมือน BattleScreen) ---
        private BattleScreen _battleScreen;
        private LogoScreen _logoScreen;
        private CreditsScreen _creditsScreen;
        private IntroScreen _introScreen;
        private MainMenuScreen _mainMenuScreen;
        private ModeSelectScreen _modeSelectScreen;
        private StoryModeSelectScreen _storyModeSelectScreen;
        private TeamSetupScreen _teamSetupScreen;
        private SettingsScreen _settingsScreen;
        private PauseScreen _pauseScreen;
        private UpgradeScreen _upgradeScreen;
        private ItemSelectScreen _itemSelectScreen;
        private ShopScreen _shopScreen;
        private RestScreen _restScreen;
        private MapScreen _mapScreen;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // เริ่มเกมแบบเต็มจอ (borderless ที่ขนาดเดสก์ท็อป) กด F11 หรือ Alt+Enter เพื่อสลับกับโหมดหน้าต่าง
            _graphics.HardwareModeSwitch = false;
            ApplyDisplayMode(true);
        }

        private RenderTarget2D _sceneTarget;
        private bool _isFullscreen = true;
        private bool _applyingDisplayMode;

        private void ApplyDisplayMode(bool fullscreen)
        {
            _applyingDisplayMode = true;
            var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            if (fullscreen)
            {
                _graphics.PreferredBackBufferWidth = dm.Width;
                _graphics.PreferredBackBufferHeight = dm.Height;
                _graphics.IsFullScreen = true;
            }
            else
            {
                // โหมดหน้าต่าง: สัดส่วน 16:9 ไม่ใหญ่เกินจอ
                int w = Math.Min(ScreenWidth, Math.Max(960, dm.Width - 120));
                _graphics.PreferredBackBufferWidth = w;
                _graphics.PreferredBackBufferHeight = w * 9 / 16;
                _graphics.IsFullScreen = false;
            }

            _graphics.ApplyChanges();
            _isFullscreen = fullscreen;
            _applyingDisplayMode = false;
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (_applyingDisplayMode || _isFullscreen) return;
            var b = Window.ClientBounds;
            if (b.Width <= 0 || b.Height <= 0) return;

            _applyingDisplayMode = true;
            _graphics.PreferredBackBufferWidth = b.Width;
            _graphics.PreferredBackBufferHeight = b.Height;
            _graphics.ApplyChanges();
            _applyingDisplayMode = false;
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            Window.IsBorderless = false;
            Window.ClientSizeChanged += OnClientSizeChanged;
            base.Initialize();
        }

        private void PlayUIClick()
        {
            AudioManager.PlaySfx("Audio/UI_Click");
        }

        private void EnterMapScreen()
        {
            _appState = AppState.Map;
            _session.IsShopOnMap = _rng.NextDouble() < 0.5;
        }

        private void StartNewRunForChallenge()
        {
            _session.IsStoryMode = false;
            _session.StoryStageIndex = 0;
            _session.ChestRolledForStage = -1;
            _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeStart;
            _session.CurrentStageIsHard = false;
            _session.IsNextBattleBoss = false;
            _session.Coins = 0;
            _session.Inventory.Clear();
            _upgradeScreen.EnterFresh();

            _session.AllHeroes = BattleManager.CreateStartingParty();
            Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);

            for (int i = 0; i < Math.Min(_session.AllHeroes.Count, 5); i++)
            {
                _session.FormationSlots[i] = _session.AllHeroes[i];
            }

            _appState = AppState.TeamSetup;
        }

        private void StartNewBattleForParty(bool isHard)
        {
            _session.Party = new List<Character>();
            foreach (var c in _session.FormationSlots)
            {
                if (c != null) _session.Party.Add(c);
            }
            if (_session.Party.Count == 0 && _session.AllHeroes.Count > 0)
            {
                _session.Party.Add(_session.AllHeroes[0]);
            }

            _session.CurrentStageIsHard = isHard;

            bool isBoss = !_session.IsStoryMode && _session.IsNextBattleBoss;
            _battleScreen.StartNewBattle(_session.Party, _session.FormationSlots, isHard, _session.IsStoryMode, _session.StoryStageIndex, ref _session.Coins, _session.Inventory, _session.ManualModePreference, _session.CameraShakeEnabled, isBossStage: isBoss);
            _battleScreen.ChestRarity = _session.IsStoryMode ? null : _session.PendingChest;
            _appState = AppState.Battle;

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private void GoBackFromTeamSetup()
        {
            if (_session.IsStoryMode) _appState = AppState.StoryModeSelect;
            else if (_session.TeamSetupOrigin == TeamSetupOrigin.ChallengeStart) _appState = AppState.ModeSelect;
            else
            {
                // ตอนเลือก Path บนแผนที่ ระบบบวก StoryStageIndex (ชั้นที่ผ่านแล้ว) ไปแล้ว
                // ถ้ากด Back กลับแผนที่โดยไม่ลดคืน พอเลือก Path ใหม่จะบวกซ้ำ ทำให้ข้ามชั้นโดยไม่ต้องสู้
                if (_session.TeamSetupOrigin == TeamSetupOrigin.ChallengeMap && _session.StoryStageIndex > 0)
                {
                    _session.StoryStageIndex--;
                }
                _session.IsNextBattleBoss = false;
                _session.CurrentStageIsHard = false;
                _appState = AppState.Map;
            }
        }

        private void StartBattleFromTeamSetup()
        {
            StartNewBattleForParty(_session.TeamSetupOrigin == TeamSetupOrigin.ChallengeMap && _session.CurrentStageIsHard);
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

            // ภาพเกมทั้งหมดวาดลง render target ขนาด 1920x1080 ก่อน แล้วค่อยขยายให้พอดีหน้าจอ
            _sceneTarget = new RenderTarget2D(GraphicsDevice, ScreenWidth, ScreenHeight);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            AudioManager.LoadAll(
                Content,
                new string[] { "Audio/UI_Click", "Audio/Deadsound", "Audio/Lumi_Skill_Sound", "Audio/Lumi_Attack_Sound", "Audio/Alice_Attack_Sound", "Audio/Veyra_Attack_Sound", "Audio/Elma_Attack_Sound", "Audio/Elma_Skill_Sound", "Audio/Cutscene_Sound", "Audio/Wolf_Attack_Sound", "Audio/Stonegolem_Attack_Sound", "Audio/GiantHornet_Attack", "Audio/Enos_Attack_Sound", "Audio/Enos_Skill_Sound", "Audio/Buying", "Audio/Selling" },
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

            // ฟอนต์เฉพาะหน้าเมนูหลัก (Quicksand) — ถ้าหาไฟล์ไม่เจอจะ fallback ไปใช้ฟอนต์หลักแทน
            string[] menuFontCandidates = { "Quicksand-VariableFont_wght", "Fonts/Quicksand-VariableFont_wght" };
            foreach (var name in menuFontCandidates)
            {
                try
                {
                    _menuFont = Content.Load<SpriteFont>(name);
                    break;
                }
                catch
                {
                    _menuFont = null;
                }
            }
            if (_menuFont == null) _menuFont = _font;

            try { _battleBackground = Content.Load<Texture2D>("bg_Battle_01"); } catch { _battleBackground = null; }
            try { _introBackground = Content.Load<Texture2D>("Intro_BG_Test001"); } catch { _introBackground = null; }
            // โลโก้เกม: ใช้ LogoGame ก่อน (ผ่าน Content.mgcb หรือไฟล์ .png ข้างเกม) ถ้าไม่มีค่อยใช้ LogoTest1
            try { _logoBackground = Content.Load<Texture2D>("LogoGame"); }
            catch
            {
                _logoBackground = LoadTextureHelper("", "LogoGame");
                if (_logoBackground == null)
                {
                    try { _logoBackground = Content.Load<Texture2D>("LogoTest1"); } catch { _logoBackground = null; }
                }
            }

            try { _elmaIdleTex = Content.Load<Texture2D>("char_Elma_Idle"); } catch { _elmaIdleTex = null; }
            try { _elmaAttackTex = Content.Load<Texture2D>("char_Elma_Attack"); } catch { _elmaAttackTex = null; }

            _elmaSkill01Tex = LoadTextureHelper("Charater", "char_Elma_Skill01");
            _elmaSkill02Tex = LoadTextureHelper("Charater", "char_Elma_Skill02");

            _aliceIdleTex = LoadTextureHelper("Charater", "char_Alice_Idle");
            _veyraIdleTex = LoadTextureHelper("Charater", "Veyra_Idle");
            _veyraAttackTex = LoadTextureHelper("Charater", "Veyra_Attack");
            _veyraSkillTex = LoadTextureHelper("Charater", "Veyra_Skill");
            _enosIdleTex = LoadTextureHelper("Charater", "char_Enos_Idle");
            _enosAttackTex = LoadTextureHelper("Charater", "char_Enos_Attack");
            _enosSkillTex = LoadTextureHelper("Charater", "char_Enos_Skill");
            _aliceAttackTex = LoadTextureHelper("Charater", "char_Alice_Attack");
            _aliceIdleModeTex = LoadTextureHelper("Charater", "char_Alice_Idle_mode");
            _aliceAttackModeTex = LoadTextureHelper("Charater", "char_Alice_Attack_mode");

            _lumiIdleTex = LoadTextureHelper("Charater", "Char_Lumi_IDLE");
            _lumiAttackTex = LoadTextureHelper("Charater", "Char_Lumi_ATTACK");
            _lumiSkillTex = LoadTextureHelper("Charater", "Char_Lumi_Skill");

            _lumiSkillBoomTex = LoadTextureHelper("Bloom_Effect", "skill_lumi_Boom");
            _boomFxTex = LoadTextureHelper("Bloom_Effect", "Boom_Fx");

            _deadEffectTex = LoadTextureHelper("Effect", "Dead_Effect");

            // ไอคอนไอเท็ม (sprite sheet): ถ้าไม่เจอไฟล์ Item/Items_Icons.png จะใช้รูปที่ฝังในโค้ดแทน
            Where_My_Slime.Data.ItemIcons.Load(GraphicsDevice, LoadTextureHelper("Item", "Items_Icons"));
            Where_My_Slime.Data.StatusIcons.Load(GraphicsDevice, null);
            Where_My_Slime.Data.ChestIcons.Load(GraphicsDevice, null);

            // กรอบการ์ดสกิลตอนต่อสู้: ไม่เจอไฟล์ UI/Skill_Frame.png จะใช้รูปที่ฝังในโค้ดแทน
            Where_My_Slime.Data.SkillFrameArt.Load(GraphicsDevice, LoadTextureHelper("UI", "Skill_Frame"));
            // การ์ด Skill ตอนต่อสู้ (280x200): Ui_Skill/icon_char_Skill_<ชื่อ>.png = กรอบ, ...<ชื่อ>1.png = ไอคอนสกิล
            foreach (var heroName in new[] { "Alice", "Elma", "Enos", "Lumi", "Veyra" })
            {
                Where_My_Slime.Data.SkillUiArt.Set(heroName,
                    LoadTextureHelper("Ui_Skill", "icon_char_Skill_" + heroName),
                    LoadTextureHelper("Ui_Skill", "icon_char_Skill_" + heroName + "1"));
            }
            Where_My_Slime.Data.ButtonIcons.Load(GraphicsDevice, LoadTextureHelper("UI", "Button_Icons"));

            // รูปไตเติ้ล "Where my Slime" ที่ใช้แทนตัวหนังสือบนหน้าเมนูหลัก
            // ถ้าไฟล์อยู่คนละโฟลเดอร์ ให้บอกชื่อโฟลเดอร์มาเพิ่มใน LoadTextureHelper ตรงนี้ได้เลย
            _titleTex = LoadTextureHelper("", "Tilte_0.01");

            // พื้นหลังหน้าเมนูหลัก (1280x720 ขยายเต็มจอ 1920x1080) หาไฟล์ Mainmenu_Background.png ในโฟลเดอร์เกม/Content/Background/UI
            _mainMenuBackground = LoadTextureHelper("", "Mainmenu_Background")
                ?? LoadTextureHelper("Background", "Mainmenu_Background")
                ?? LoadTextureHelper("UI", "Mainmenu_Background");

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

            // รูปโปรไฟล์ฮีโร่ (โฟลเดอร์ Profile) — ไม่มีไฟล์ก็ใช้ภาพสไปรต์เดิม
            Where_My_Slime.Data.HeroProfiles.Set("Alice", LoadTextureHelper("Profile", "Alice_Profile"));
            Where_My_Slime.Data.HeroProfiles.Set("Elma", LoadTextureHelper("Profile", "Elma_Profile"));
            Where_My_Slime.Data.HeroProfiles.Set("Enos", LoadTextureHelper("Profile", "Enos_Profile"));
            Where_My_Slime.Data.HeroProfiles.Set("Lumi", LoadTextureHelper("Profile", "Lumi_Profile"));
            Where_My_Slime.Data.HeroProfiles.Set("Veyra", LoadTextureHelper("Profile", "Veyra_Profile") ?? LoadTextureHelper("Profile", "Vedra_Profile"));

            _testEnemyTex = LoadTextureHelper("Enemy", "Test_Enemy");
            _orcWarriorTex = LoadTextureHelper("Enemy", "orc_warrior");
            _goblinArcherTex = LoadTextureHelper("Enemy", "goblin_archer");
            _wolfTex = LoadTextureHelper("Enemy", "wolf_idle_spritesheet-export")
                ?? LoadTextureHelper("Enemy", "wolf_idle spritesheet-export")
                ?? LoadTextureHelper("Enemy", "wolf");
            _wolfAttackTex = LoadTextureHelper("Enemy", "wolf_attack-Sheet")
                ?? LoadTextureHelper("Enemy", "wolf_attack_Sheet");
            _ghostHealerTex = LoadTextureHelper("Enemy", "ghost_healer");
            _stoneGolemTex = LoadTextureHelper("Enemy", "Stonegolem_idle-Sheet") ?? LoadTextureHelper("Enemy", "StoneGolem_idle-Sheet") ?? LoadTextureHelper("Enemy", "stone_golem") ?? LoadTextureHelper("Enemy", "Stone_Golem") ?? LoadTextureHelper("Enemy", "stone_giant");
            _stoneGolemAttackTex = LoadTextureHelper("Enemy", "stonegolem_attack_Sheet")
                ?? LoadTextureHelper("Enemy", "StoneGolem_attack_Sheet")
                ?? LoadTextureHelper("Enemy", "Stonegolem_attack_Sheet")
                ?? LoadTextureHelper("Enemy", "stonegolem_attack-Sheet");
            _bossAngelTex = LoadTextureHelper("Enemy", "boss_slime_Angel") ?? LoadTextureHelper("Enemy", "boss_slime_angel");
            _bossKingTex = LoadTextureHelper("Enemy", "boss_slime_King") ?? LoadTextureHelper("Enemy", "boss_slime_king");

            _beeIdleTex = LoadTextureHelper("Enemy", "enemyMySlive1_Bee_idle") ?? LoadTextureHelper("Enemy", "enemyMySlime1_Bee_idle");
            _beeAttackTex = LoadTextureHelper("Enemy", "enemyMySlive1_Bee_attack") ?? LoadTextureHelper("Enemy", "enemyMySlime1_Bee_attack");

            var texDict = new Dictionary<string, Texture2D>
            {
                { "ElmaIdle", _elmaIdleTex },
                { "ElmaAttack", _elmaAttackTex },
                { "char_Elma_Skill01", _elmaSkill01Tex },
                { "char_Elma_Skill02", _elmaSkill02Tex },
                { "Alice", _aliceIdleTex },
                { "Swordman", _aliceIdleTex },
                { "Veyra_Idle", _veyraIdleTex },
                { "Veyra_Attack", _veyraAttackTex },
                { "Charater/Veyra_Attack", _veyraAttackTex },
                { "Veyra_Skill", _veyraSkillTex },
                { "Charater/Veyra_Skill", _veyraSkillTex },
                { "char_Enos_Idle", _enosIdleTex },
                { "Charater/char_Enos_Idle", _enosIdleTex },
                { "Charater/char_Enos_Attack", _enosAttackTex },
                { "Charater/char_Enos_Skill", _enosSkillTex },
                { "Charater/Veyra_Idle", _veyraIdleTex },

                { "char_Alice_Idle", _aliceIdleTex },
                { "char_Alice_Attack", _aliceAttackTex },
                { "char_Alice_Idle_mode", _aliceIdleModeTex },
                { "char_Alice_Attack_mode", _aliceAttackModeTex },

                { "Char_Lumi_IDLE", _lumiIdleTex },
                { "Char_Lumi_ATTACK", _lumiAttackTex },
                { "Char_Lumi_Skill", _lumiSkillTex },
                { "Charater/Char_Lumi_Skill", _lumiSkillTex },
                { "skill_lumi_Boom", _lumiSkillBoomTex },
                { "Boom_Fx", _boomFxTex },
                { "Bloom_Effect/Boom_Fx", _boomFxTex },

                { "Giant_Hornet", _beeIdleTex },
                { "enemyMySlime1_Bee", _beeIdleTex },
                { "enemyMySlime1_Bee_idle", _beeIdleTex },
                { "enemyMySlime1_Bee_attack", _beeAttackTex },
                { "GiantHornet", _beeIdleTex },
                { "Bee", _beeIdleTex },

                { "TestChar", _testCharacterTex },
                { "DeadEffect", _deadEffectTex },
                { "TestEnemy", _testEnemyTex },
                { "Orc", _orcWarriorTex },
                { "Goblin", _goblinArcherTex },
                { "Wolf", _wolfTex },
                { "Wolf_Attack", _wolfAttackTex },
                { "Ghost", _ghostHealerTex },
                { "StoneGolem", _stoneGolemTex },
                { "StoneGolem_Attack", _stoneGolemAttackTex },
                { "BossSlimeAngel", _bossAngelTex },
                { "BossSlimeKing", _bossKingTex }
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
                () => {
                    _settingsReturnState = AppState.Battle;
                    _appState = AppState.Settings;
                },
                PlayUIClick,
                () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                StartNewRunForChallenge,
                () => { _appState = AppState.Pause; }
            );

            _creditsScreen = new CreditsScreen(_spriteBatch, _font, _pixel);
            _logoScreen = new LogoScreen(
                _spriteBatch, _font, _pixel, _logoBackground,
                goToIntro: () => { _appState = AppState.MainMenu; }); // ตัดหน้า Intro ออก ข้ามจาก Logo ไป MainMenu โดยตรง

            _introScreen = new IntroScreen(
                _spriteBatch, _font, _pixel, _introBackground,
                goToMainMenu: () => _appState = AppState.MainMenu);

            _mainMenuScreen = new MainMenuScreen(
                _spriteBatch, _menuFont, _pixel, _titleTex,
                goToModeSelect: () => _appState = AppState.ModeSelect,
                openSettings: () => { _settingsReturnState = AppState.MainMenu; _appState = AppState.Settings; },
                goToCredits: () => { _creditsScreen.Reset(); _appState = AppState.Credits; },
                exitGame: () => Exit(),
                playUIClick: PlayUIClick);

            _modeSelectScreen = new ModeSelectScreen(
                _spriteBatch, _font, _pixel,
                goToMainMenu: () => _appState = AppState.MainMenu,
                goToStoryModeSelect: () => _appState = AppState.StoryModeSelect,
                startNewRunForChallenge: StartNewRunForChallenge,
                playUIClick: PlayUIClick);

            _storyModeSelectScreen = new StoryModeSelectScreen(
                _spriteBatch, _font, _pixel, _session,
                goToModeSelect: () => _appState = AppState.ModeSelect,
                goToTeamSetup: () => _appState = AppState.TeamSetup,
                playUIClick: PlayUIClick);

            _teamSetupScreen = new TeamSetupScreen(
                _spriteBatch, _font, _pixel, _session,
                _elmaIdleTex, _lumiIdleTex, _aliceIdleTex, _veyraIdleTex, _enosIdleTex, _testCharacterTex,
                goBack: GoBackFromTeamSetup,
                startBattle: StartBattleFromTeamSetup,
                playUIClick: PlayUIClick);

            _settingsScreen = new SettingsScreen(
                _spriteBatch, _font, _pixel, _session,
                close: () => _appState = _settingsReturnState,
                onToggleCameraShake: enabled => _battleScreen.SetCameraShakeEnabled(enabled),
                playUIClick: PlayUIClick);

            _pauseScreen = new PauseScreen(
                _spriteBatch, _font, _pixel,
                goToMainMenu: () => _appState = AppState.MainMenu,
                restart: () =>
                {
                    if (_session.IsStoryMode)
                    {
                        StartNewBattleForParty(_session.CurrentStageIsHard);
                        _appState = AppState.Battle;
                    }
                    else
                    {
                        StartNewRunForChallenge();
                    }
                },
                continueBattle: () => _appState = AppState.Battle,
                playUIClick: PlayUIClick);

            _upgradeScreen = new UpgradeScreen(
                _spriteBatch, _font, _pixel, _session,
                _elmaIdleTex, _lumiIdleTex, _aliceIdleTex, _veyraIdleTex, _enosIdleTex, _testCharacterTex,
                onDone: EnterMapScreen,
                playUIClick: PlayUIClick);

            _itemSelectScreen = new ItemSelectScreen(
                _spriteBatch, _font, _pixel, _session, _rng,
                onDone: () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                playUIClick: PlayUIClick);

            _shopScreen = new ShopScreen(
                _spriteBatch, _font, _pixel, _session,
                onDone: () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                playUIClick: PlayUIClick);

            _restScreen = new RestScreen(
                _spriteBatch, _font, _pixel, _session,
                continueToMap: EnterMapScreen,
                playUIClick: PlayUIClick);

            _mapScreen = new MapScreen(
                _spriteBatch, _font, _pixel, _session,
                chooseStage: isHard =>
                {
                    _session.CurrentStageIsHard = isHard;
                    _session.IsNextBattleBoss = false;
                    _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                    _session.StoryStageIndex++;

                    Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);
                    for (int i = 0; i < Math.Min(_session.Party.Count, 5); i++) _session.FormationSlots[i] = _session.Party[i];
                    _appState = AppState.TeamSetup;
                },
                chooseShop: scrollVal =>
                {
                    _shopScreen.EnterFresh(_rng, scrollVal);
                    _appState = AppState.Shop;
                },
                chooseBoss: () =>
                {
                    _session.CurrentStageIsHard = false;
                    _session.IsNextBattleBoss = true;
                    _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                    _session.StoryStageIndex++;

                    Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);
                    for (int i = 0; i < Math.Min(_session.Party.Count, 5); i++) _session.FormationSlots[i] = _session.Party[i];
                    _appState = AppState.TeamSetup;
                },
                playUIClick: PlayUIClick);
        }

        protected override void Update(GameTime gameTime)
        {
            VirtualMouse.Update(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);

            var mouse = VirtualMouse.GetState();
            var keyboard = Keyboard.GetState();

            // F11 หรือ Alt+Enter = สลับเต็มจอ/หน้าต่าง
            bool altEnter = (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)) && keyboard.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter);
            bool f11 = keyboard.IsKeyDown(Keys.F11) && !_previousKeyboardState.IsKeyDown(Keys.F11);
            if (f11 || altEnter) ApplyDisplayMode(!_isFullscreen);

            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            switch (_appState)
            {
                case AppState.Logo:
                    _logoScreen.Update(gameTime, keyboard, _previousKeyboardState, leftClicked);
                    break;
                case AppState.Intro:
                    _introScreen.Update(gameTime, keyboard, _previousKeyboardState, leftClicked);
                    break;
                case AppState.MainMenu:
                    _mainMenuScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.ModeSelect:
                    _modeSelectScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.StoryModeSelect:
                    _storyModeSelectScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.TeamSetup:
                    _teamSetupScreen.Update(gameTime, mouse, leftClicked, keyboard);
                    break;
                case AppState.Settings:
                    _settingsScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.Credits:
                    _creditsScreen.Update(gameTime);
                    if (leftClicked || keyboard.IsKeyDown(Keys.Escape))
                    {
                        if (leftClicked) PlayUIClick();
                        _appState = AppState.MainMenu;
                    }
                    break;
                case AppState.Battle:
                    _battleScreen.Update(gameTime, keyboard, mouse, leftClicked, ref _session.ManualModePreference, ref _session.Coins, _session.Inventory);
                    break;
                case AppState.Pause:
                    _pauseScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.Upgrade:
                    _upgradeScreen.Update(mouse, leftClicked, keyboard, _previousKeyboardState);
                    break;
                case AppState.ItemSelect:
                    _itemSelectScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Shop:
                    _shopScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Rest:
                    _restScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Map:
                    _mapScreen.Update(mouse, leftClicked);
                    break;
            }

            _previousMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_sceneTarget);

            Color clearColor = (_appState == AppState.Logo || _appState == AppState.Intro) ? Color.Black : new Color(30, 30, 45);
            GraphicsDevice.Clear(clearColor);

            _spriteBatch.Begin();

            switch (_appState)
            {
                case AppState.Logo: _logoScreen.Draw(); break;
                case AppState.Intro: _introScreen.Draw(); break;
                case AppState.MainMenu: DrawMainMenuBackground(); _mainMenuScreen.Draw(); break;
                case AppState.ModeSelect: _modeSelectScreen.Draw(); break;
                case AppState.StoryModeSelect: _storyModeSelectScreen.Draw(); break;
                case AppState.TeamSetup: _teamSetupScreen.Draw(); break;
                case AppState.Settings:
                    if (_settingsReturnState == AppState.Battle)
                    {
                        _spriteBatch.End();
                        _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                        _battleScreen.Draw();
                        _spriteBatch.End();
                        _spriteBatch.Begin();
                    }
                    else
                    {
                        DrawMainMenuBackground();
                        _mainMenuScreen.Draw();
                    }
                    _settingsScreen.Draw();
                    break;
                case AppState.Credits: _creditsScreen.Draw(); break;
                case AppState.Battle:
                    _spriteBatch.End();
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                    _battleScreen.Draw();
                    _spriteBatch.End();
                    _spriteBatch.Begin();
                    break;
                case AppState.Pause:
                    _spriteBatch.End();
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                    _battleScreen.Draw();
                    _spriteBatch.End();
                    _spriteBatch.Begin();
                    _pauseScreen.Draw();
                    break;
                case AppState.Upgrade: _upgradeScreen.Draw(); break;
                case AppState.ItemSelect: _itemSelectScreen.Draw(); break;
                case AppState.Shop: _shopScreen.Draw(); break;
                case AppState.Rest: _restScreen.Draw(); break;
                case AppState.Map: _mapScreen.Draw(); break;
            }

            _spriteBatch.End();

            // ขยายภาพ 1920x1080 ให้พอดีหน้าจอจริง (คงสัดส่วน มีแถบดำถ้าจำเป็น)
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            _spriteBatch.Draw(_sceneTarget, VirtualMouse.DestRectangle, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMainMenuBackground()
        {
            if (_mainMenuBackground != null)
                _spriteBatch.Draw(_mainMenuBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
        }

        private void DrawPlaceholderScreen(string title, string subtitle)
        {
            DrawTextCentered(title, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f - 40), Color.Gold, 2.4f);
            DrawTextCentered(subtitle, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f + 30), Color.White, 1f);
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