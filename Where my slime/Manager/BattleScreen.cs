using Where_My_Slime.Battle;
using Where_My_Slime.Data;
using Where_My_Slime.Manager;
using Where_My_Slime.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Where_My_Slime
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
        private Texture2D _elmaSkill01Tex;
        private Texture2D _elmaSkill02Tex;

        private Texture2D _lumiIdleTex;
        private Texture2D _lumiAttackTex;
        private Texture2D _lumiSkillTex;
        // --- แอนิเมชันร่ายสกิลของ Lumi (Char_Lumi_Skill.png: เฟรมสี่เหลี่ยมจัตุรัส เรียงแนวนอน เฟรมละ = ความสูงรูป) ก่อนยิงลูกเวทย์ ---
        private bool _lumiSkillCasting;
        private float _lumiSkillTimer;
        private Character _lumiSkillTarget;
        private const float LumiSkillFrameTime = 0.07f;
        private int LumiSkillFrameCount => _lumiSkillTex == null ? 1 : Math.Max(1, _lumiSkillTex.Width / Math.Max(1, _lumiSkillTex.Height));
        private Texture2D _lumiSkillBoomTex;
        private Texture2D _boomFxTex; //[cite: 7]

        // --- Texture ของ Alice / Swordman ---
        private Texture2D _aliceIdleTex;
        private Texture2D _aliceAttackTex;
        private Texture2D _aliceIdleModeTex;    // ท่า Idle ตอนมี Aura
        private Texture2D _aliceAttackModeTex;  // ท่าโจมตีตอนมี Aura

        private Texture2D _testCharacterTex;
        private Texture2D _veyraIdleTex;
        private Texture2D _veyraAttackTex;
        private Texture2D _veyraSkillTex;
        // --- แอนิเมชันสกิลของ Veyra (Veyra_Skill.png: เฟรมสี่เหลี่ยมจัตุรัส เรียงแนวนอน เฟรมละ = ความสูงรูป) ---
        private bool _veyraSkillActive;
        private float _veyraSkillTimer;
        private bool _veyraSkillCast;
        private Character _veyraSkillCaster;
        private Character _veyraSkillTarget;
        private const float VeyraSkillFrameTime = 0.08f;
        private const float VeyraSkillCastAt = 0.5f;
        private int VeyraSkillFrameCount => _veyraSkillTex == null ? 1 : Math.Max(1, _veyraSkillTex.Width / Math.Max(1, _veyraSkillTex.Height));
        private Rectangle VeyraSkillFrameRect(int i)
        {
            int size = _veyraSkillTex.Height;
            i = Math.Max(0, Math.Min(i, VeyraSkillFrameCount - 1));
            return new Rectangle(i * size, 0, size, size);
        }
        private Texture2D _enosIdleTex;
        private Texture2D _enosAttackTex;
        private Texture2D _enosSkillTex;

        // Enos: อนิเมชันโจมตี (3 เฟรม) และสกิล (18 เฟรม) เฟรมละ 480x480
        private bool _enosIsAnimatingAttack;
        private float _enosAttackHoldTimer;
        private Character _enosStoredTarget;
        private int _enosCustomFrameIndex;
        private float _enosCustomAnimTimer;
        private bool _enosHasAnimatedThisTurn;
        private bool _enosSkillActive;
        private float _enosSkillTimer;
        private bool _enosSkillCast;
        private Character _enosSkillCaster;
        private const float EnosSkillFrameTime = 0.07f;   // 18 เฟรม ~1.26 วินาที
        private const float EnosSkillCastAt = 0.55f;      // สัดส่วนเวลาของอนิเมชันที่ Aggro เริ่มมีผล

        // --- แอนิเมชันโจมตีของ Veyra (Veyra_Attack.png) ---
        private bool _veyraIsAnimatingAttack;
        private bool _veyraHasAnimatedThisTurn;
        // เวลาเล่นแอนิเมชันโจมตีทุกตัว (วินาที)
        private const float AttackPlayTime = BattleManager.AttackAnimDuration;
        private float _veyraAttackHoldTimer;
        private float _veyraCustomAnimTimer;
        private int _veyraCustomFrameIndex;
        private Character _veyraStoredTarget;
        private Texture2D _deadEffectTex;

        private Texture2D _testEnemyTex;
        private Texture2D _orcWarriorTex;
        private Texture2D _giantHornetIdleTex;
        private Texture2D _giantHornetAttackTex;
        private Texture2D _wolfTex;
        private Texture2D _wolfAttackTex;
        private float _wolfAnimTimer;
        private int _wolfFrameIndex;
        private float _golemAnimTimer;
        private int _golemFrameIndex;
        // Stone Golem idle sheet: 192x192 frames, 12 frames (row-major; works for a strip or a grid)
        private const int GolemFrames = 12;
        private const int GolemFrameSize = 192;
        private const float GolemScaleMul = 1.4f * EnemySizeBoost;
        // Wolf sheets: 192x192 frames in one horizontal strip (idle 6 frames, attack 10 frames)
        private const float EnemySizeBoost = 1.25f;   // ขยายขนาดศัตรูทุกตัว (1 = เดิม)
        private const float WolfScaleMul = 1.4f * EnemySizeBoost;
        private const float WolfAttackFrameTime = 0.07f;
        private Texture2D _ghostHealerTex;
        private Texture2D _stoneGolemTex;
        private Texture2D _stoneGolemAttackTex;
        private const float GolemAttackFrameTime = 0.08f;
        private Texture2D _bossAngelTex;
        private Texture2D _bossKingTex;

        private BattleManager _battle;
        private List<Character> _party;
        private Character[] _formationSlots;
        private List<Item> _inventory;
        private List<Item> _currentItemChoices;
        private int _coins;
        private bool _isStoryMode;
        private int _storyStageIndex;
        private bool _currentStageIsHard;
        private bool _currentStageIsBoss;

        // --- ซูมตัวละคร (คลิกขวาที่ตัวละคร/ศัตรู): ตัวใหญ่ซ้าย + แผงข้อมูลขวา ---
        private Character _zoomUnit;
        private bool _zoomDrawOnly;
        private Rectangle _zoomRect;
        private float _zoomAnim;
        private bool _prevRightDown;
        private bool _cameraShakeEnabled;
        private readonly Random _rng;

        private List<Character> _turnQueue = new List<Character>();
        private Character _lastTurnUnit = null;

        private float _elmaAnimTimer;
        private int _elmaFrameIndex;
        private bool _elmaIsAnimatingAttack;
        private float _elmaAttackHoldTimer;
        private Character _elmaStoredTarget;
        private int _elmaCustomFrameIndex;
        private float _elmaCustomAnimTimer;
        private bool _elmaHasAnimatedThisTurn;

        private float _lumiAnimTimer;
        private int _lumiFrameIndex;
        private bool _lumiIsAnimatingAttack;
        private float _lumiAttackHoldTimer;
        private Character _lumiStoredTarget;
        private int _lumiCustomFrameIndex;
        private float _lumiCustomAnimTimer;
        private bool _lumiHasAnimatedThisTurn;
        private Character _lumiCasterRef;

        // --- ตัวแปรควบคุมอนิเมชันสกิล Lumi (โปรเจกต์ไทล์พุ่งไปหาเป้าหมาย + ระเบิดใส่ศัตรูทุกตัว) ---
        private bool _lumiIsUsingUltimateAnim = false;
        private bool _lumiIsProjectileMoving = false;
        private Vector2 _lumiProjPos;
        private Vector2 _lumiProjTargetPos;
        private float _lumiProjSpeed = 1200f;
        private float _lumiUltimateTimer = 0f;
        private bool _lumiHasDealtDamage = false;
        private Character _lumiUltimateTarget;

        // --- ตัวแปรจัดการอนิเมชันของ Alice / Swordman ---
        private float _aliceAnimTimer;
        private int _aliceFrameIndex;
        private bool _aliceIsAnimatingAttack;
        private float _aliceAttackHoldTimer;
        private Character _aliceStoredTarget;
        private int _aliceCustomFrameIndex;
        private float _aliceCustomAnimTimer;
        private bool _aliceHasAnimatedThisTurn;
        private Character _aliceCasterRef;

        private float _hornetAnimTimer;
        private int _hornetFrameIndex;

        private bool _elmaIsUsingUltimateAnim = false;
        private float _elmaUltimateTimer = 0f;
        private bool _elmaHasDealtDamage = false;
        private Character _elmaUltimateTarget;
        private Character _elmaCasterRef;

        private float _shakeTimer;
        private float _shakeOffsetX;
        private float _battleEndDelayTimer = 0f;

        // ---------- กล่องสมบัติรางวัลหลังชนะด่าน (Challenge Mode) ----------
        // ระดับกล่องของด่านนี้ (null = ไม่มีกล่อง เช่น Story Mode) ตั้งค่าจาก Game1 หลัง StartNewBattle
        public ItemRarity? ChestRarity;
        // 0 = ยังไม่เริ่ม, 1 = กล่องปิดรอคลิก, 2 = อนิเมชันเปิด, 3 = โชว์เหรียญ, 4 = โชว์ไอเท็ม, 5 = จบ
        private int _chestPhase;
        private float _chestTimer;
        private ChestReward _chestReward;
        private const float ChestOpenDuration = 1.2f;

        // ข้อความเปิดด่าน: วิ่งเข้าจากขวา -> หยุดกลาง 1 วิ -> วิ่งออกซ้าย (รวม 1.5 วิ) ระหว่างนี้การต่อสู้ยังไม่เริ่ม
        private const float IntroSlideTime = 0.25f;
        private const float IntroHoldTime = 1.0f;
        private const float IntroDuration = IntroSlideTime * 2f + IntroHoldTime;
        private float _introTimer = 0f;

        // ปุ่มเมนู: ซ่อนปุ่ม II / S / A ไว้ กดแล้วปุ่มเหล่านั้นจะเลื่อนลงมาด้านล่าง
        private bool _buttonMenuOpen = false;
        private float _buttonMenuAnim = 0f;
        private bool _hoveredUiToggle = false;

        private bool _isWaitingForUltimateTarget = false;
        private Character _enosSkillTarget;
        private Character _enemySfxAttacker;

        // --- Skill Cutscene: โปรไฟล์ฟาง/ค้าง/ฟางหาย รวม 0.6 วินาที ---
        private const float CutsceneFadeIn = 0.15f;
        private const float CutsceneHold = 0.3f;
        private const float CutsceneFadeOut = 0.15f;
        private const float CutsceneTotal = CutsceneFadeIn + CutsceneHold + CutsceneFadeOut;
        private float _cutsceneTimer = -1f;           // < 0 = ไม่ได้เล่น
        private Character _cutsceneCaster;
        private bool _cutsceneFast;
        private Action _cutscenePendingAction;        // โหมดปกติ: รอให้คัตซีนจบก่อนค่อยเริ่มสกิล
        private Character _ultimateQueuedCaster = null; // กดสกิลแล้ว แต่ต้องรอให้การโจมตีที่กำลังเล่นอยู่จบและตัวละครกลับที่เดิมก่อน
        private float _targetGlowTimer = 0f;

        private readonly Dictionary<Character, float> _deathFadeAlphas = new Dictionary<Character, float>();
        private readonly Dictionary<Character, float> _deathTimers = new Dictionary<Character, float>();
        private readonly HashSet<Character> _playedDeathSound = new HashSet<Character>();

        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private const float FloatingTextDuration = 1.0f;
        private const float FloatingTextRiseSpeed = 100f; // ความเร็วขึ้นตอนเริ่ม (ชะลอลงเรื่อยๆ)
        private const float FloatingTextPopTime = 0.14f;

        // --- เอฟเฟกต์ตอนถูกโจมตี: ตัวกะพริบขาว + ถอยหลัง + หลอดเลือดสั่น ---
        private const float HitDuration = 0.35f;
        private const float HitRecoilDistance = 26f;
        private const float HitBarShake = 4f;
        private readonly Dictionary<Character, float> _hitTimers = new Dictionary<Character, float>();
        private readonly Dictionary<Texture2D, Texture2D> _whiteMasks = new Dictionary<Texture2D, Texture2D>();
        private const float FloatingTextPopScale = 1.6f;
        private const float DamageTextScale = 2.2f;
        private const float CritTextScale = 2.8f;
        private const float MissTextScale = 1.8f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private Character _hoveredEnemyTarget;
        private bool _hoveredPauseButton;
        private bool _hoveredAutoToggle;
        private bool _hoveredBattleSettings;
        private int _hoveredItemCard = -1;
        private bool _hoveredStoryWinContinue;
        private bool _hoveredStoryWinMenu;
        private bool _hoveredLoseRestart;
        private bool _hoveredLoseMenu;

        private readonly Action<AppState> _changeState;
        private readonly Action _openSettings;
        private readonly Action _playUIClick;
        private readonly Action _goToUpgrade;
        private readonly Action _startNewRunForChallenge;
        private readonly Action _openPause;

        public BattleScreen(
            GraphicsDevice graphicsDevice,
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Texture2D battleBg,
            Dictionary<string, Texture2D> textures,
            Random rng,
            Action<AppState> changeState,
            Action openSettings,
            Action playUIClick,
            Action goToUpgrade,
            Action startNewRunForChallenge,
            Action openPause)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _battleBackground = battleBg;
            _rng = rng;
            _changeState = changeState;
            _openSettings = openSettings;
            _playUIClick = playUIClick;
            _goToUpgrade = goToUpgrade;
            _startNewRunForChallenge = startNewRunForChallenge;
            _openPause = openPause;

            _elmaIdleTex = textures.GetValueOrDefault("ElmaIdle") ?? textures.GetValueOrDefault("elma_idle") ?? textures.GetValueOrDefault("char_elma_idle");
            _elmaAttackTex = textures.GetValueOrDefault("ElmaAttack") ?? textures.GetValueOrDefault("elma_attack") ?? textures.GetValueOrDefault("char_elma_attack");

            _elmaSkill01Tex = textures.GetValueOrDefault("ElmaSkill01") ?? textures.GetValueOrDefault("elma_skill01") ?? textures.GetValueOrDefault("Elma_Skill01") ??
                    textures.GetValueOrDefault("char_Elma_Skill01") ?? textures.GetValueOrDefault("char_elma_skill01") ??
                    textures.GetValueOrDefault("char_Elma_Skill01.png") ?? textures.GetValueOrDefault("char_elma_skill01.png");

            _elmaSkill02Tex = textures.GetValueOrDefault("ElmaSkill02") ?? textures.GetValueOrDefault("elma_skill02") ?? textures.GetValueOrDefault("Elma_Skill02") ??
                    textures.GetValueOrDefault("char_Elma_Skill02") ?? textures.GetValueOrDefault("char_elma_skill02") ??
                    textures.GetValueOrDefault("char_Elma_Skill02.png") ?? textures.GetValueOrDefault("char_elma_skill02.png");

            _lumiIdleTex = textures.GetValueOrDefault("Charater/Char_Lumi_IDLE") ?? textures.GetValueOrDefault("Char_Lumi_IDLE") ?? textures.GetValueOrDefault("CharLumiIdle") ?? textures.GetValueOrDefault("char_lumi_idle") ?? textures.GetValueOrDefault("LumiIdle");
            _lumiSkillTex = textures.GetValueOrDefault("Charater/Char_Lumi_Skill") ?? textures.GetValueOrDefault("Char_Lumi_Skill");
            _lumiAttackTex = textures.GetValueOrDefault("Charater/Char_Lumi_ATTACK") ?? textures.GetValueOrDefault("Char_Lumi_ATTACK") ?? textures.GetValueOrDefault("CharLumiAttack") ?? textures.GetValueOrDefault("char_lumi_attack") ?? textures.GetValueOrDefault("LumiAttack");

            _lumiSkillBoomTex = textures.GetValueOrDefault("skill_lumi_Boom") ?? textures.GetValueOrDefault("Bloom_Effect/skill_lumi_Boom");

            _boomFxTex = textures.GetValueOrDefault("Bloom_Effect/Boom_Fx") ??
                      textures.GetValueOrDefault("Boom_Fx") ??
                      textures.GetValueOrDefault("Bloom_Effect/Boom_Fx.png") ??
                      textures.GetValueOrDefault("Boom_Fx.png");

            _aliceIdleTex = textures.GetValueOrDefault("Charater/char_Alice_Idle") ??
                            textures.GetValueOrDefault("Charater/Char_Alice_Idle") ??
                            textures.GetValueOrDefault("char_Alice_Idle") ??
                            textures.GetValueOrDefault("Alice") ??
                            textures.GetValueOrDefault("Swordman") ??
                            textures.GetValueOrDefault("alice_idle");

            _aliceIdleModeTex = textures.GetValueOrDefault("char_Alice_Idle_mode");
            _aliceAttackModeTex = textures.GetValueOrDefault("char_Alice_Attack_mode");
            _aliceAttackTex = textures.GetValueOrDefault("Charater/char_Alice_Attack") ??
                            textures.GetValueOrDefault("Charater/Char_Alice_Attack") ??
                            textures.GetValueOrDefault("char_Alice_Attack") ??
                            textures.GetValueOrDefault("AliceAttack") ??
                            textures.GetValueOrDefault("alice_attack");

            _testCharacterTex = textures.GetValueOrDefault("TestChar");
            _veyraIdleTex = textures.GetValueOrDefault("Charater/Veyra_Idle") ?? textures.GetValueOrDefault("Veyra_Idle");
            _veyraAttackTex = textures.GetValueOrDefault("Charater/Veyra_Attack") ?? textures.GetValueOrDefault("Veyra_Attack");
            _veyraSkillTex = textures.GetValueOrDefault("Charater/Veyra_Skill") ?? textures.GetValueOrDefault("Veyra_Skill");
            _enosIdleTex = textures.GetValueOrDefault("Charater/char_Enos_Idle") ?? textures.GetValueOrDefault("char_Enos_Idle");
            _enosAttackTex = textures.GetValueOrDefault("Charater/char_Enos_Attack") ?? textures.GetValueOrDefault("char_Enos_Attack");
            _enosSkillTex = textures.GetValueOrDefault("Charater/char_Enos_Skill") ?? textures.GetValueOrDefault("char_Enos_Skill");
            _deadEffectTex = textures.GetValueOrDefault("DeadEffect") ?? textures.GetValueOrDefault("Effect/Dead_Effect");
            _testEnemyTex = textures.GetValueOrDefault("TestEnemy");
            _orcWarriorTex = textures.GetValueOrDefault("Orc");

            _giantHornetIdleTex = textures.GetValueOrDefault("Giant_Hornet") ?? textures.GetValueOrDefault("Enemy/enemyMySlime1_Bee_idle") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_idle") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_idle.png");
            _giantHornetAttackTex = textures.GetValueOrDefault("Enemy/enemyMySlime1_Bee_attack") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_attack") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_attack.png");

            _wolfTex = textures.GetValueOrDefault("Wolf");
            _wolfAttackTex = textures.GetValueOrDefault("Wolf_Attack");
            _ghostHealerTex = textures.GetValueOrDefault("Ghost");
            _stoneGolemTex = textures.GetValueOrDefault("StoneGolem");
            _stoneGolemAttackTex = textures.GetValueOrDefault("StoneGolem_Attack");
            _bossAngelTex = textures.GetValueOrDefault("BossSlimeAngel");
            _bossKingTex = textures.GetValueOrDefault("BossSlimeKing");
        }

        public void StartNewBattle(List<Character> party, Character[] formationSlots, bool isHard, bool isStoryMode, int storyStageIndex, ref int coins, List<Item> inventory, bool manualModePreference, bool cameraShakeEnabled, bool isBossStage = false)
        {
            _party = party;
            _formationSlots = formationSlots;
            _isStoryMode = isStoryMode;
            _storyStageIndex = storyStageIndex;
            _currentStageIsHard = isHard;
            _currentStageIsBoss = isBossStage;
            _cameraShakeEnabled = cameraShakeEnabled;
            _isWaitingForUltimateTarget = false;
            _ultimateQueuedCaster = null;

            _elmaIsUsingUltimateAnim = false;
            _elmaUltimateTarget = null;
            _elmaCasterRef = null;

            _lumiIsUsingUltimateAnim = false;
            _lumiIsProjectileMoving = false;
            _lumiUltimateTarget = null;
            _lumiSkillCasting = false;
            _lumiCasterRef = null;

            _aliceCasterRef = null;

            if (_party != null)
            {
                foreach (var c in _party)
                {
                    if (c != null)
                    {
                        c.Hp = c.MaxHp;
                        c.InitializeSP();
                    }
                }
            }

            if (_battle != null)
            {
                _battle.OnAttackResolved -= HandleAttackResolved;
                _battle.OnHealResolved -= HandleHealResolved;
                _battle.OnPoisonDamage -= HandlePoisonDamage;
                _battle.OnFireDamage -= HandleFireDamage;
                _battle.OnStatusInflicted -= HandleStatusInflicted;
                _battle.OnStunSkipped -= HandleStunSkipped;
                _battle.OnBlessingApplied -= HandleBlessingApplied;
            }

            // สเกลความยากตามจำนวนชั้นที่ผ่านมาแล้ว ใช้เฉพาะ Challenge Mode เท่านั้น (Story Mode ไม่ต้องสเกล)
            int floorsClearedForScaling = isStoryMode ? 0 : storyStageIndex;
            _battle = new BattleManager(_party, isHard, floorsCleared: floorsClearedForScaling, isBossStage: isBossStage);
            _battle.AttackFrameCount = GetAttackFrameCount;
            // Wolf: เล่นท่าโจมตี 10 เฟรมให้เห็นชัด (0.07 วิ/เฟรม) ดาเมจเข้าตอนเริ่มเฟรมที่ 2
            _battle.AttackDurationOverride = u => (IsWolf(u) && _wolfAttackTex != null) ? WolfFrameCount(_wolfAttackTex) * WolfAttackFrameTime
                : (IsGolem(u) && _stoneGolemAttackTex != null) ? GolemAttackFrameCount() * GolemAttackFrameTime : 0f;
            _auraParticles.Clear();
            _auraSpawnTimers.Clear();
            _currentItemChoices = null;
            _deathFadeAlphas.Clear();
            _zoomUnit = null; _zoomAnim = 0f; _zoomDrawOnly = false;
            _deathTimers.Clear();
            _playedDeathSound.Clear();
            _battleEndDelayTimer = 0f;
            _chestPhase = 0;
            _chestTimer = 0f;
            _chestReward = null;
            _introTimer = 0f;
            ResetTurnQueue();

            if (_isStoryMode && _storyStageIndex == 0)
            {
                if (_battle.Enemies != null && _battle.Enemies.Count > 1)
                {
                    _battle.Enemies.RemoveRange(1, _battle.Enemies.Count - 1);
                }
            }

            _battle.SetManualMode(manualModePreference);
            _battle.OnAttackResolved += HandleAttackResolved;
            _battle.OnHealResolved += HandleHealResolved;
            _battle.OnPoisonDamage += HandlePoisonDamage;
            _battle.OnFireDamage += HandleFireDamage;
            _battle.OnStatusInflicted += HandleStatusInflicted;
            _battle.OnStunSkipped += HandleStunSkipped;
            _battle.OnBlessingApplied += HandleBlessingApplied;
            _floatingTexts.Clear();
            _hitTimers.Clear();

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private void ResetTurnQueue()
        {
            _turnQueue.Clear();
            if (_battle == null) return;
            if (_battle.Allies != null)
            {
                foreach (var a in _battle.Allies) if (a != null && a.IsAlive) _turnQueue.Add(a);
            }
            if (_battle.Enemies != null)
            {
                foreach (var e in _battle.Enemies) if (e != null && e.IsAlive) _turnQueue.Add(e);
            }
            _lastTurnUnit = null;
        }

        private void UpdateTurnQueue()
        {
            if (_battle == null) return;

            if (_turnQueue.Count == 0)
            {
                ResetTurnQueue();
            }

            _turnQueue.RemoveAll(c => c == null || !c.IsAlive);

            var allAlive = new List<Character>();
            if (_battle.Allies != null)
            {
                foreach (var a in _battle.Allies) if (a != null && a.IsAlive) allAlive.Add(a);
            }
            if (_battle.Enemies != null)
            {
                foreach (var e in _battle.Enemies) if (e != null && e.IsAlive) allAlive.Add(e);
            }

            foreach (var c in allAlive)
            {
                if (!_turnQueue.Contains(c))
                {
                    _turnQueue.Add(c);
                }
            }

            Character currentUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;

            if (currentUnit != null && currentUnit.IsAlive)
            {
                if (_lastTurnUnit != null && _lastTurnUnit != currentUnit && _lastTurnUnit.IsAlive)
                {
                    _turnQueue.Remove(_lastTurnUnit);
                    if (!_turnQueue.Contains(_lastTurnUnit))
                    {
                        _turnQueue.Add(_lastTurnUnit);
                    }
                }

                _lastTurnUnit = currentUnit;

                if (_turnQueue.Contains(currentUnit))
                {
                    _turnQueue.Remove(currentUnit);
                }
                _turnQueue.Insert(0, currentUnit);
            }
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            _cameraShakeEnabled = enabled;
        }

        public Vector2 GetShakeOffset()
        {
            if (_shakeTimer > 0f)
            {
                _shakeOffsetX = (float)(_rng.Next(-5, 6));
                return new Vector2(_shakeOffsetX, 0f);
            }
            return Vector2.Zero;
        }

        private void HandleAttackResolved(Character target, AttackResult result)
        {
            if (_cameraShakeEnabled)
            {
                _shakeTimer = 0.25f;
            }

            var rect = GetAvatarHomeRect(target);
            float offsetX = (float)(_rng.Next(-45, 46));
            float offsetY = (float)(_rng.Next(-20, 21));
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f + offsetX, (rect.Y - 15f) + offsetY);

            if (result.IsImmune)
            {
                _floatingTexts.Add(new FloatingText { Position = pos, Text = "IMMUNE", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(150, 210, 255), Outline = new Color(10, 40, 110), Scale = DamageTextScale });
                return;
            }

            if (result.IsMiss)
            {
                _floatingTexts.Add(new FloatingText { Position = pos, Text = "MISS", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(210, 210, 210), Outline = new Color(40, 40, 40), Scale = MissTextScale });
                return;
            }

            // สไตล์ตัวเลข: ตัวหนามีขอบเข้ม — กายภาพ = แดง, เวทย์ = น้ำเงิน, คริติคอล = ขาวตัวใหญ่ + "!"
            _hitTimers[target] = HitDuration;

            string text = result.IsCrit ? $"{result.Damage}!" : $"{result.Damage}";
            Color color, outline;
            if (result.IsCrit) { color = new Color(250, 250, 250); outline = result.IsMagical ? new Color(0, 60, 200) : new Color(120, 20, 20); } // Crit: ตัวขาว ขอบแดง (เวทย์ = ขอบน้ำเงิน)
            else if (result.IsMagical) { color = new Color(60, 160, 255); outline = new Color(0, 35, 110); }
            else { color = new Color(235, 40, 40); outline = new Color(80, 0, 0); }
            float scale = result.IsCrit ? CritTextScale : DamageTextScale;
            _floatingTexts.Add(new FloatingText { Position = pos, Text = text, Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = color, Outline = outline, Scale = scale });

            if ((!target.IsAlive || target.Hp <= 0) && !_playedDeathSound.Contains(target))
            {
                _playedDeathSound.Add(target);
                AudioManager.PlaySfx("Audio/Deadsound");
            }
        }

        private void HandlePoisonDamage(Character target, int amount)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = $"-{amount}", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(150, 235, 70), Outline = new Color(30, 70, 0), Scale = DamageTextScale });
        }

        private void HandleFireDamage(Character target, int amount)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = $"-{amount}", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(255, 150, 50), Outline = new Color(110, 30, 0), Scale = DamageTextScale });
        }

        // ข้อความเมื่อติดสถานะ (Fire / Melt / Poison / Weakness) ลอยเหนือเป้าหมาย
        private void HandleStatusInflicted(Character target, string text)
        {
            Color col, outline;
            if (text.StartsWith("Fire")) { col = new Color(255, 150, 50); outline = new Color(110, 30, 0); }
            else if (text.StartsWith("Melt")) { col = new Color(120, 225, 235); outline = new Color(0, 50, 70); }
            else if (text.StartsWith("Poison")) { col = new Color(150, 235, 70); outline = new Color(30, 70, 0); }
            else if (text.StartsWith("Shield")) { col = new Color(150, 200, 255); outline = new Color(10, 40, 110); }
            else if (text.StartsWith("Cleanse")) { col = new Color(120, 255, 170); outline = new Color(0, 80, 40); }
            else { col = new Color(200, 130, 255); outline = new Color(60, 10, 90); }
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 55f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = text, Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = col, Outline = outline, Scale = DamageTextScale * 0.7f });
        }

        private void HandleBlessingApplied(Character target, int stacks)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 55f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = $"Blessing +{stacks}", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(255, 240, 150), Outline = new Color(110, 80, 0), Scale = DamageTextScale * 0.7f });
        }

        private void HandleStunSkipped(Character target)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = "STUN", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(255, 235, 80), Outline = new Color(90, 70, 0), Scale = DamageTextScale * 0.8f });
        }

        private void HandleHealResolved(Character target, int amount)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = $"+{amount}", Timer = FloatingTextDuration, Duration = FloatingTextDuration, Color = new Color(80, 235, 90), Outline = new Color(0, 70, 10), Scale = DamageTextScale });
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool leftClicked, ref bool manualModePreference, ref int coins, List<Item> inventory)
        {
            _coins = coins;
            _inventory = inventory;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _totalTime += delta;
            if (_introTimer < IntroDuration) _introTimer += delta;
            _buttonMenuAnim = MathHelper.Clamp(_buttonMenuAnim + (_buttonMenuOpen ? 1f : -1f) * delta / 0.2f, 0f, 1f);
            UpdateAuraParticles(delta);

            if (_shakeTimer > 0f) _shakeTimer -= delta;
            _targetGlowTimer += delta;

            // --- ซูมตัวละคร: คลิกขวาที่ตัวละครเพื่อเปิด / คลิกอะไรก็ได้เพื่อปิด (ระหว่างซูมเกมหยุดไว้) ---
            bool rightDownNow = mouse.RightButton == ButtonState.Pressed;
            bool rightClickedNow = rightDownNow && !_prevRightDown;
            _prevRightDown = rightDownNow;
            if (_zoomUnit != null)
            {
                _zoomAnim = Math.Min(1f, _zoomAnim + delta * 7f);
                if (rightClickedNow || leftClicked || keyboard.IsKeyDown(Keys.Escape)) { _zoomUnit = null; _zoomAnim = 0f; }
                return;
            }
            // ซูมได้ตลอด (แม้กำลังรอเลือกเป้าโจมตี/สกิล): ถ้าคลิกขวาโดนตัวละครจะซูม ถ้าคลิกขวาที่ว่างจะยกเลิกการเลือกเป้าตามเดิม
            if (rightClickedNow && _battle != null && _battle.State == BattleState.Running)
            {
                Character picked = null;
                foreach (var u in _battle.Enemies) if (u != null && u.IsAlive && GetAvatarHomeRect(u).Contains(mouse.Position)) { picked = u; break; }
                if (picked == null)
                    foreach (var u in _battle.Allies) if (u != null && u.IsAlive && GetAvatarHomeRect(u).Contains(mouse.Position)) { picked = u; break; }
                if (picked != null) { _zoomUnit = picked; _zoomAnim = 0f; _hoveredEnemyTarget = null; return; }
            }

            // --- Skill Cutscene ---
            if (_cutsceneTimer >= 0f)
            {
                _cutsceneTimer += delta;
                if (_cutsceneTimer >= CutsceneTotal)
                {
                    _cutsceneTimer = -1f;
                    _cutsceneCaster = null;
                    var pending = _cutscenePendingAction;
                    _cutscenePendingAction = null;
                    pending?.Invoke();
                }
                else if (_cutscenePendingAction != null)
                {
                    // โหมดปกติ: หยุดเกมไว้จนกว่าคัตซีนจะจบ
                    UpdateFloatingTexts(delta);
                    return;
                }
            }

            // --- อนิเมชันสกิลของ Veyra: เล่นแล้วให้ผลกลางอนิเมชัน ---
            if (_veyraSkillActive)
            {
                _veyraSkillTimer += delta;
                float vTotal = VeyraSkillFrameCount * VeyraSkillFrameTime;
                if (!_veyraSkillCast && _veyraSkillTimer >= vTotal * VeyraSkillCastAt)
                {
                    _veyraSkillCast = true;
                    if (_battle != null && _veyraSkillCaster != null && _veyraSkillCaster.IsAlive)
                        _battle.CastUltimate(_veyraSkillCaster, _veyraSkillTarget ?? _veyraSkillCaster);
                }
                if (_veyraSkillTimer >= vTotal)
                {
                    if (!_veyraSkillCast && _battle != null && _veyraSkillCaster != null && _veyraSkillCaster.IsAlive)
                        _battle.CastUltimate(_veyraSkillCaster, _veyraSkillTarget ?? _veyraSkillCaster);
                    _veyraSkillActive = false;
                    _veyraSkillCaster = null;
                    _veyraSkillTarget = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            // --- อนิเมชันสกิลของ Enos (Aggro ใส่ตัวเอง): เล่น 18 เฟรม แล้วให้ผลตอนกลางอนิเมชัน ---
            if (_enosSkillActive)
            {
                _enosSkillTimer += delta;
                float total = SpriteFrames.GridFrameCount(_enosSkillTex) * EnosSkillFrameTime;
                if (!_enosSkillCast && _enosSkillTimer >= total * EnosSkillCastAt)
                {
                    _enosSkillCast = true;
                    if (_battle != null && _enosSkillCaster != null && _enosSkillCaster.IsAlive)
                        _battle.CastUltimate(_enosSkillCaster, _enosSkillTarget ?? _enosSkillCaster);
                }
                if (_enosSkillTimer >= total)
                {
                    if (!_enosSkillCast && _battle != null && _enosSkillCaster != null && _enosSkillCaster.IsAlive)
                        _battle.CastUltimate(_enosSkillCaster, _enosSkillTarget ?? _enosSkillCaster);
                    _enosSkillActive = false;
                    _enosSkillCaster = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            if (_elmaIsUsingUltimateAnim)
            {
                _elmaIsAnimatingAttack = false;
                _elmaUltimateTimer += delta;

                if (_elmaUltimateTimer >= 0.8f && !_elmaHasDealtDamage)
                {
                    _elmaHasDealtDamage = true;
                    if (_battle != null && _elmaCasterRef != null && _elmaUltimateTarget != null)
                    {
                        _battle.CastUltimate(_elmaCasterRef, _elmaUltimateTarget);
                    }
                }

                if (_elmaUltimateTimer >= 1.5f)
                {
                    _elmaIsUsingUltimateAnim = false;
                    _elmaUltimateTarget = null;
                    _elmaCasterRef = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            // --- Lumi ร่ายสกิล (เล่นอนิเมชัน Char_Lumi_Skill ก่อน แล้วค่อยยิงลูกเวทย์) ---
            if (_lumiSkillCasting)
            {
                _lumiSkillTimer += delta;
                if (_lumiSkillTimer >= LumiSkillFrameCount * LumiSkillFrameTime)
                {
                    _lumiSkillCasting = false;
                    LaunchLumiProjectile(_lumiCasterRef, _lumiSkillTarget);
                }
                UpdateFloatingTexts(delta);
                return;
            }

            // --- อัปเดตการเคลื่อนที่โปรเจกต์ไทล์และอนิเมชันระเบิดของ Lumi ---
            if (_lumiIsProjectileMoving)
            {
                Vector2 direction = _lumiProjTargetPos - _lumiProjPos;
                float distance = direction.Length();
                if (distance > 15f)
                {
                    direction.Normalize();
                    _lumiProjPos += direction * _lumiProjSpeed * delta;
                }
                else
                {
                    _lumiIsProjectileMoving = false;
                    _lumiIsUsingUltimateAnim = true;
                    _lumiUltimateTimer = 0f;
                    _lumiHasDealtDamage = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            if (_lumiIsUsingUltimateAnim)
            {
                _lumiIsAnimatingAttack = false;
                _lumiUltimateTimer += delta;

                float totalDuration = 0.8f;
                if (_lumiUltimateTimer >= totalDuration && !_lumiHasDealtDamage)
                {
                    _lumiHasDealtDamage = true;
                    if (_battle != null && _lumiCasterRef != null)
                    {
                        // สกิล Lumi เป็น AoE ใส่ศัตรูทุกตัวอยู่แล้วภายใน ResolveUltimate()
                        // เรียกแค่ครั้งเดียวพอ ไม่งั้นดาเมจจะโดนคูณซ้ำตามจำนวนศัตรูที่ยังไม่ตาย
                        _battle.CastUltimate(_lumiCasterRef, _lumiUltimateTarget);
                    }
                }

                if (_lumiUltimateTimer >= 1.2f)
                {
                    _lumiIsUsingUltimateAnim = false;
                    _lumiUltimateTarget = null;
                    _lumiCasterRef = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            _elmaAnimTimer += delta;
            if (_elmaAnimTimer >= 0.12f)
            {
                _elmaAnimTimer = 0f;
                _elmaFrameIndex++;
            }

            _lumiAnimTimer += delta;
            if (_lumiAnimTimer >= 0.12f)
            {
                _lumiAnimTimer = 0f;
                _lumiFrameIndex++;
            }

            _aliceAnimTimer += delta;
            if (_aliceAnimTimer >= 0.12f)
            {
                _aliceAnimTimer = 0f;
                _aliceFrameIndex++;
            }

            _golemAnimTimer += delta;
            if (_golemAnimTimer >= 0.12f)
            {
                _golemAnimTimer = 0f;
                _golemFrameIndex++;
            }

            _wolfAnimTimer += delta;
            if (_wolfAnimTimer >= 0.12f)
            {
                _wolfAnimTimer = 0f;
                _wolfFrameIndex++;
            }

            _hornetAnimTimer += delta;
            if (_hornetAnimTimer >= 0.12f)
            {
                _hornetAnimTimer = 0f;
                _hornetFrameIndex++;
            }

            if (_battle != null)
            {
                UpdateDeathFades(delta);
                UpdateTurnQueue();

                if (_battle.State != BattleState.Running)
                {
                    _battleEndDelayTimer += delta;
                }

                bool isElmaAttacking = (_battle.ActiveAttacker != null && _battle.ActiveAttacker == _elmaCasterRef && !_elmaIsUsingUltimateAnim);
                if (!isElmaAttacking && _battle.ActiveAttacker != null && !string.IsNullOrEmpty(_battle.ActiveAttacker.Name))
                {
                    if (_battle.ActiveAttacker.Name.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0 && !_elmaIsUsingUltimateAnim)
                    {
                        isElmaAttacking = true;
                    }
                }

                if (isElmaAttacking)
                {
                    if (!_elmaHasAnimatedThisTurn)
                    {
                        _elmaIsAnimatingAttack = true;
                        _elmaAttackHoldTimer = AttackPlayTime;
                        _elmaStoredTarget = _battle.ActiveTarget;
                        _elmaCustomFrameIndex = 0;
                        _elmaCustomAnimTimer = 0f;
                        _elmaHasAnimatedThisTurn = true;
                        AudioManager.PlaySfx("Audio/Elma_Attack_Sound");
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
                        float timePerFrame = AttackPlayTime / totalFrames;
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

                bool isLumiAttacking = (_battle.ActiveAttacker != null && (IsLumi(_battle.ActiveAttacker)));
                if (isLumiAttacking)
                {
                    if (!_lumiHasAnimatedThisTurn)
                    {
                        _lumiIsAnimatingAttack = true;
                        _lumiAttackHoldTimer = AttackPlayTime;
                        _lumiStoredTarget = _battle.ActiveTarget;
                        _lumiCustomFrameIndex = 0;
                        _lumiCustomAnimTimer = 0f;
                        _lumiHasAnimatedThisTurn = true;
                        AudioManager.PlaySfx("Audio/Lumi_Attack_Sound");
                    }
                }
                else
                {
                    _lumiHasAnimatedThisTurn = false;
                }

                if (_lumiIsAnimatingAttack)
                {
                    _lumiAttackHoldTimer -= delta;
                    if (_lumiAttackTex != null)
                    {
                        int frameWidth = 320;
                        int totalFrames = Math.Max(1, _lumiAttackTex.Width / frameWidth);
                        float timePerFrame = AttackPlayTime / totalFrames;
                        _lumiCustomAnimTimer += delta;
                        if (_lumiCustomAnimTimer >= timePerFrame)
                        {
                            _lumiCustomAnimTimer = 0f;
                            if (_lumiCustomFrameIndex < totalFrames - 1)
                            {
                                _lumiCustomFrameIndex++;
                            }
                        }
                    }

                    if (_lumiAttackHoldTimer <= 0f)
                    {
                        _lumiIsAnimatingAttack = false;
                        _lumiStoredTarget = null;
                    }
                }

                bool isAliceAttacking = (_battle.ActiveAttacker != null && IsAlice(_battle.ActiveAttacker));
                if (isAliceAttacking)
                {
                    if (!_aliceHasAnimatedThisTurn)
                    {
                        _aliceIsAnimatingAttack = true;
                        _aliceAttackHoldTimer = AttackPlayTime;
                        _aliceStoredTarget = _battle.ActiveTarget;
                        _aliceCustomFrameIndex = 0;
                        _aliceCustomAnimTimer = 0f;
                        _aliceHasAnimatedThisTurn = true;
                        AudioManager.PlaySfx("Audio/Alice_Attack_Sound");
                    }
                }
                else
                {
                    _aliceHasAnimatedThisTurn = false;
                }

                if (_aliceIsAnimatingAttack)
                {
                    _aliceAttackHoldTimer -= delta;
                    if (_aliceAttackTex != null)
                    {
                        int frameWidth = 480;
                        int totalFrames = 6;
                        float timePerFrame = AttackPlayTime / totalFrames;
                        _aliceCustomAnimTimer += delta;
                        if (_aliceCustomAnimTimer >= timePerFrame)
                        {
                            _aliceCustomAnimTimer = 0f;
                            if (_aliceCustomFrameIndex < totalFrames - 1)
                            {
                                _aliceCustomFrameIndex++;
                            }
                        }
                    }

                    if (_aliceAttackHoldTimer <= 0f)
                    {
                        _aliceIsAnimatingAttack = false;
                        _aliceStoredTarget = null;
                    }
                }

                bool isVeyraAttacking = (_battle.ActiveAttacker != null && IsVeyra(_battle.ActiveAttacker));
                if (isVeyraAttacking)
                {
                    if (!_veyraHasAnimatedThisTurn)
                    {
                        _veyraIsAnimatingAttack = _veyraAttackTex != null;
                        _veyraAttackHoldTimer = AttackPlayTime;
                        _veyraStoredTarget = _battle.ActiveTarget;
                        _veyraCustomFrameIndex = 0;
                        _veyraCustomAnimTimer = 0f;
                        _veyraHasAnimatedThisTurn = true;
                        AudioManager.PlaySfx("Audio/Veyra_Attack_Sound");
                    }
                }
                else
                {
                    _veyraHasAnimatedThisTurn = false;
                }

                if (_veyraIsAnimatingAttack)
                {
                    _veyraAttackHoldTimer -= delta;
                    int veyraFrames = SpriteFrames.FrameCount(_veyraAttackTex, SpriteFrames.VeyraAttackFrameWidth);
                    float timePerFrame = AttackPlayTime / veyraFrames;
                    _veyraCustomAnimTimer += delta;
                    if (_veyraCustomAnimTimer >= timePerFrame)
                    {
                        _veyraCustomAnimTimer = 0f;
                        if (_veyraCustomFrameIndex < veyraFrames - 1) _veyraCustomFrameIndex++;
                    }

                    if (_veyraAttackHoldTimer <= 0f)
                    {
                        _veyraIsAnimatingAttack = false;
                        _veyraStoredTarget = null;
                    }
                }

                // เสียงโจมตีของศัตรู (Wolf / Giant Hornet / Stone Golem และ King ที่ใช้ท่าเดียวกัน): เล่นครั้งเดียวตอนเริ่มท่าโจมตี
                if (_battle.ActiveAttacker == null)
                {
                    _enemySfxAttacker = null;
                }
                else if (_battle.ActiveAttacker.IsEnemy && _enemySfxAttacker != _battle.ActiveAttacker)
                {
                    _enemySfxAttacker = _battle.ActiveAttacker;
                    string en = _enemySfxAttacker.Name ?? "";
                    if (en.IndexOf("wolf", StringComparison.OrdinalIgnoreCase) >= 0)
                        AudioManager.PlaySfx("Audio/Wolf_Attack_Sound");
                    else if (en.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 || en.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 || en.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0)
                        AudioManager.PlaySfx("Audio/GiantHornet_Attack");
                    else if (IsGolem(_enemySfxAttacker))
                        AudioManager.PlaySfx("Audio/Stonegolem_Attack_Sound");
                }

                bool isEnosAttacking = (_battle.ActiveAttacker != null && IsEnos(_battle.ActiveAttacker));
                if (isEnosAttacking)
                {
                    if (!_enosHasAnimatedThisTurn)
                    {
                        _enosIsAnimatingAttack = _enosAttackTex != null;
                        _enosAttackHoldTimer = AttackPlayTime;
                        _enosStoredTarget = _battle.ActiveTarget;
                        _enosCustomFrameIndex = 0;
                        _enosCustomAnimTimer = 0f;
                        _enosHasAnimatedThisTurn = true;
                        AudioManager.PlaySfx("Audio/Enos_Attack_Sound");
                    }
                }
                else
                {
                    _enosHasAnimatedThisTurn = false;
                }

                if (_enosIsAnimatingAttack)
                {
                    _enosAttackHoldTimer -= delta;
                    int enosFrames = SpriteFrames.GridFrameCount(_enosAttackTex);
                    float timePerFrame = AttackPlayTime / enosFrames;
                    _enosCustomAnimTimer += delta;
                    if (_enosCustomAnimTimer >= timePerFrame)
                    {
                        _enosCustomAnimTimer = 0f;
                        if (_enosCustomFrameIndex < enosFrames - 1) _enosCustomFrameIndex++;
                    }

                    if (_enosAttackHoldTimer <= 0f)
                    {
                        _enosIsAnimatingAttack = false;
                        _enosStoredTarget = null;
                    }
                }

                if (!_elmaIsAnimatingAttack && !_lumiIsAnimatingAttack && !_aliceIsAnimatingAttack && !_veyraIsAnimatingAttack && !_enosIsAnimatingAttack)
                {
                    if (!_isWaitingForUltimateTarget && !_battle.IsUsingUltimate && _introTimer >= IntroDuration)
                    {
                        // ถ้ากดสกิลค้างไว้ ให้เดินเกมต่อเฉพาะตอนที่ยังมีการโจมตีที่เล่นอยู่ (เพื่อให้จบและกลับที่เดิม)
                        // ห้ามเริ่มตาของตัวอื่นเพิ่ม ไม่งั้นจะมีการโจมตีต่อกันไม่รู้จบและไม่ถึงคิวเลือกเป้าหมายสักที
                        if (_ultimateQueuedCaster == null || _battle.ActiveAttacker != null)
                        {
                            _battle.Update(delta);
                        }
                    }
                }
            }

            UpdateFloatingTexts(delta);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick();
                _changeState(AppState.MainMenu);
                return;
            }

            if (_battle.State != BattleState.Running && _battleEndDelayTimer < 1.3f) return;

            UpdateUltimates(mouse, leftClicked);
            ProcessQueuedUltimate();

            // คลิกขวายกเลิกได้ทั้งตอนรอคิว และตอนเลือกเป้าหมาย
            if (_ultimateQueuedCaster != null && mouse.RightButton == ButtonState.Pressed)
            {
                _ultimateQueuedCaster = null;
            }

            if (_isWaitingForUltimateTarget && mouse.RightButton == ButtonState.Pressed)
            {
                _isWaitingForUltimateTarget = false;
                _battle.UltimateCaster = null;
                _battle.IsUsingUltimate = false;
                _elmaCasterRef = null;
                _lumiCasterRef = null;
            }

            if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                // เปิดกล่องสมบัติก่อน (เหรียญ -> ไอเท็ม) แล้วค่อยเข้าหน้า Victory
                if (ChestRarity.HasValue && _chestPhase != 5)
                {
                    UpdateChest(delta, leftClicked, ref coins);
                    return;
                }

                if (_currentItemChoices == null || _currentItemChoices.Count == 0)
                {
                    foreach (var c in _party) c.UpgradePoints += _battle.RewardPoints;
                    // Lucky Coin: เหรียญที่ได้ +25% ต่อ 1 คนที่สวมไอเท็มนี้ (ซ้อนกันได้)
                    int luckyCoinCount = 0;
                    foreach (var c in _party)
                    {
                        if (c.HasEquipped("Lucky Coin")) luckyCoinCount++;
                    }
                    coins += (int)Math.Round(_battle.RewardCoins * (1f + 0.25f * luckyCoinCount));
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
                    _inventory.Add(item);
                    _currentItemChoices = null;
                    _goToUpgrade();
                }

                return;
            }

            bool menuUsable = _buttonMenuOpen && _buttonMenuAnim > 0.95f;
            _hoveredPauseButton = menuUsable && GetPauseButtonRect().Contains(mouse.Position);
            _hoveredBattleSettings = menuUsable && GetBattleSettingsButtonRect().Contains(mouse.Position);
            _hoveredAutoToggle = GetAutoToggleButtonRect().Contains(mouse.Position);
            _hoveredUiToggle = GetUiToggleButtonRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredUiToggle)
                {
                    _playUIClick();
                    _buttonMenuOpen = !_buttonMenuOpen;
                }
                else if (_hoveredPauseButton)
                {
                    _playUIClick();
                    _openPause?.Invoke();
                }
                else if (_hoveredBattleSettings)
                {
                    _playUIClick();
                    _openSettings();
                }
                else if (_hoveredAutoToggle)
                {
                    _playUIClick();
                    _battle.SetManualMode(!_battle.ManualMode);
                    manualModePreference = _battle.ManualMode;
                }
            }

            _hoveredEnemyTarget = null;
            Character hoveredAllyTarget = null;

            if (_battle.PendingAttacker != null || _isWaitingForUltimateTarget)
            {
                bool isVeyraCaster = _battle.UltimateCaster != null && (_battle.UltimateCaster.Name?.IndexOf("veyra", StringComparison.OrdinalIgnoreCase) >= 0);

                if (_isWaitingForUltimateTarget && IsSelfTargetCaster(_battle.UltimateCaster))
                {
                    var self = _battle.UltimateCaster;
                    if (self.IsAlive && GetAvatarHomeRect(self).Contains(mouse.Position))
                    {
                        _hoveredEnemyTarget = self;
                        if (leftClicked)
                        {
                            _playUIClick();
                            _isWaitingForUltimateTarget = false;
                            _hoveredEnemyTarget = null;
                            StartSelfUltimate(self);
                        }
                    }
                }
                else if (_isWaitingForUltimateTarget && isVeyraCaster)
                {
                    foreach (var ally in _battle.Allies)
                    {
                        if (!ally.IsAlive) continue;
                        if (GetAvatarHomeRect(ally).Contains(mouse.Position)) { hoveredAllyTarget = ally; break; }
                    }

                    if (leftClicked && hoveredAllyTarget != null)
                    {
                        _playUIClick();
                        _isWaitingForUltimateTarget = false;
                        if (_veyraSkillTex != null)
                        {
                            _veyraSkillActive = true;
                            _veyraSkillTimer = 0f;
                            _veyraSkillCast = false;
                            _veyraSkillCaster = _battle.UltimateCaster;
                            _veyraSkillTarget = hoveredAllyTarget;
                            _battle.IsUsingUltimate = true;
                        }
                        else _battle.CastUltimate(_battle.UltimateCaster, hoveredAllyTarget);
                        hoveredAllyTarget = null;
                    }
                }
                else
                {
                    foreach (var enemy in _battle.Enemies)
                    {
                        if (!enemy.IsAlive) continue;
                        if (GetAvatarHomeRect(enemy).Contains(mouse.Position)) { _hoveredEnemyTarget = enemy; break; }
                    }

                    if (leftClicked && _hoveredEnemyTarget != null)
                    {
                        if (_isWaitingForUltimateTarget)
                        {
                            _playUIClick();

                            bool isElmaCaster = _battle.UltimateCaster != null &&
                                                (_battle.UltimateCaster == _elmaCasterRef ||
                                                 _battle.UltimateCaster.Name?.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0);

                            bool isLumiCaster = _battle.UltimateCaster != null &&
                                                (_battle.UltimateCaster == _lumiCasterRef ||
                                                 _battle.UltimateCaster.Name?.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0);

                            var skillCaster = _battle.UltimateCaster;
                            var skillTarget = _hoveredEnemyTarget;
                            Action castSkill = () =>
                            {
                                if (isElmaCaster)
                                {
                                    _elmaCasterRef = skillCaster;
                                    _elmaIsUsingUltimateAnim = true;
                                    _elmaUltimateTimer = 0f;
                                    _elmaHasDealtDamage = false;
                                    _elmaUltimateTarget = skillTarget;
                                    AudioManager.PlaySfx("Audio/Elma_Skill_Sound");
                                }
                                else if (IsEnos(skillCaster))
                                {
                                    StartEnosUltimate(skillCaster, skillTarget);
                                }
                                else if (isLumiCaster)
                                {
                                    _lumiCasterRef = skillCaster;
                                    AudioManager.PlaySfx("Audio/Lumi_Skill_Sound");
                                    if (_lumiSkillTex != null)
                                    {
                                        _lumiSkillCasting = true;
                                        _lumiSkillTimer = 0f;
                                        _lumiSkillTarget = skillTarget;
                                    }
                                    else LaunchLumiProjectile(skillCaster, skillTarget);
                                }
                                else
                                {
                                    _battle.CastUltimate(skillCaster, skillTarget);
                                }
                            };
                            castSkill();
                            _isWaitingForUltimateTarget = false;
                        }
                        else
                        {
                            _battle.TrySelectTarget(_hoveredEnemyTarget);
                        }
                        _hoveredEnemyTarget = null;
                    }
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
                        if (_isStoryMode) StartNewBattle(_party, _formationSlots, _currentStageIsHard, _isStoryMode, _storyStageIndex, ref coins, inventory, manualModePreference, _cameraShakeEnabled);
                        else _startNewRunForChallenge();
                    }
                    else if (_hoveredLoseMenu)
                    {
                        _playUIClick();
                        _changeState(AppState.MainMenu);
                    }
                }
            }
        }

        // ยังมีการโจมตี/แอนิเมชันค้างอยู่ไหม (ตัวละครยังไม่กลับที่เดิม)
        private bool IsAttackSequenceBusy()
        {
            return _battle.ActiveAttacker != null
                || _elmaIsAnimatingAttack || _lumiIsAnimatingAttack || _aliceIsAnimatingAttack || _veyraIsAnimatingAttack || _enosIsAnimatingAttack || _enosSkillActive || _veyraSkillActive
                || _elmaIsUsingUltimateAnim || _lumiIsUsingUltimateAnim || _lumiIsProjectileMoving || _lumiSkillCasting;
        }

        // สกิลที่กดค้างไว้: พอการโจมตีจบและทุกตัวกลับที่เดิมแล้ว ค่อยเปิดโหมดเลือกเป้าหมาย
        private void ProcessQueuedUltimate()
        {
            if (_ultimateQueuedCaster == null) return;

            var c = _ultimateQueuedCaster;

            // ยกเลิกคิวถ้าสถานการณ์เปลี่ยน (ตัวละครตาย / SP ไม่เต็ม / จบการต่อสู้)
            if (_battle.State != BattleState.Running || !c.IsAlive || c.CurrentSP < c.MaxSP || c.SwordmanBuffDuration > 0)
            {
                _ultimateQueuedCaster = null;
                return;
            }

            if (IsAttackSequenceBusy()) return;

            _ultimateQueuedCaster = null;

            // Alice: เป้าหมายคือตัวเองเท่านั้น ต้องกดที่ตัวเองก่อนสกิลถึงจะทำงาน / Enos: ต้องเลือกศัตรู 1 ตัว
            // กดสกิล -> ขึ้น Skill Cutscene ก่อน แล้วค่อยให้เลือกเป้าหมาย
            _battle.UltimateCaster = c;
            _battle.IsUsingUltimate = true;
            StartSkillCutscene(c, () => { _isWaitingForUltimateTarget = true; });

            if (c.Name?.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _elmaCasterRef = c;
            }
            else if (c.Name?.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _lumiCasterRef = c;
            }
        }

        // เริ่ม Skill Cutscene แล้วค่อยเริ่มสกิล: ปกติรอคัตซีนจบก่อน / Fast Cutscene เริ่มสกิลพร้อมกันเลย
        private void StartSkillCutscene(Character caster, Action startSkill)
        {
            if (caster == null || HeroProfiles.Get(caster.Name) == null)
            {
                startSkill();
                return;
            }

            _cutsceneCaster = caster;
            _cutsceneTimer = 0f;
            AudioManager.PlaySfx("Audio/Cutscene_Sound");
            _cutsceneFast = GameSettings.FastCutscene;
            if (_cutsceneFast)
            {
                _cutscenePendingAction = null;
                startSkill();
            }
            else _cutscenePendingAction = startSkill;
        }

        private static Color CutsceneColor(string name)
        {
            switch (name)
            {
                case "Alice": case "Swordman": return new Color(220, 70, 80);
                case "Elma": return new Color(70, 190, 100);
                case "Lumi": return new Color(160, 90, 220);
                case "Veyra": return new Color(235, 190, 60);
                case "Enos": return new Color(70, 140, 230);
                default: return new Color(200, 200, 210);
            }
        }

        // แถบสีเฉียงพร้อมไล่เฉด + เส้นขอบขาว (ไม่ใช้กรอบสี่เหลี่ยม)
        private void DrawSlantBand(int x0, int x1, float centerY, float height, float slope, Color color, float alpha)
        {
            const int step = 2;
            float mid = (x0 + x1) / 2f;
            for (int x = x0; x < x1; x += step)
            {
                float top = centerY - height / 2f + (x - mid) * slope;
                int h = (int)height;
                int q = Math.Max(1, h / 4);
                // ไล่เฉด: ขอบมืดกว่า กลางสว่างกว่า
                DrawRect(new Rectangle(x, (int)top, step, h), color * (0.55f * alpha));
                DrawRect(new Rectangle(x, (int)top + q, step, h - 2 * q), Color.Lerp(color, Color.White, 0.18f) * (0.55f * alpha));
                // เส้นขอบบน/ล่าง
                DrawRect(new Rectangle(x, (int)top - 4, step, 4), Color.White * (0.95f * alpha));
                DrawRect(new Rectangle(x, (int)top + h, step, 4), Color.White * (0.95f * alpha));
            }
        }

        private void DrawSkillCutscene()
        {
            if (_cutsceneTimer < 0f || _cutsceneCaster == null) return;
            var tex = HeroProfiles.Get(_cutsceneCaster.Name);
            if (tex == null) return;

            float t = _cutsceneTimer;
            float a = t < CutsceneFadeIn ? t / CutsceneFadeIn
                    : t < CutsceneFadeIn + CutsceneHold ? 1f
                    : 1f - (t - CutsceneFadeIn - CutsceneHold) / CutsceneFadeOut;
            a = MathHelper.Clamp(a, 0f, 1f);

            Color accent = CutsceneColor(_cutsceneCaster.Name);
            string label = (_cutsceneCaster.Name ?? "").ToUpperInvariant();
            float aspect = tex.Width / (float)tex.Height;

            if (!_cutsceneFast)
            {
                DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.55f * a));

                // ไม่ให้ทับแถบ Skill UI ด้านล่าง: ใช้พื้นที่เหนือขอบบนของการ์ดสกิล
                int skillTop = GetSkillCardRect(0).Y - 16;
                int h = Math.Min((int)(ScreenHeight * 0.86f), skillTop - 16);
                int w = (int)(h * aspect);
                int imgY = 16 + (skillTop - 16 - h) / 2;
                float cy = imgY + h * 0.55f;

                // แถบสีเฉียงพาดผ่านจอ (เลื่อนเข้ามาจากขวาเล็กน้อย)
                float bandSlide = (1f - a) * 120f;
                DrawSlantBand(0, ScreenWidth, cy + bandSlide * 0.2f, h * 0.38f, -0.10f, accent, a);

                // ตัวหนังสือชื่อตัวละคร + SKILL ข้างรูป
                float tx = ScreenWidth / 2f + w / 2f + 30f;
                float ty = cy + (tx - ScreenWidth / 2f) * -0.10f;
                DrawTextCentered(label, new Vector2(tx + 150f + 3, ty - 14 + 3), Color.Black * (0.6f * a), 3.4f);
                DrawTextCentered(label, new Vector2(tx + 150f, ty - 14), Color.White * a, 3.4f);
                DrawTextCentered("SKILL", new Vector2(tx + 150f, ty + 34), Color.Lerp(accent, Color.White, 0.5f) * a, 1.7f);

                // รูปโปรไฟล์ (เลื่อนเข้ามาจากซ้ายเล็กน้อย)
                var dest = new Rectangle((ScreenWidth - w) / 2 + (int)((1f - a) * -80f), imgY, w, h);
                _spriteBatch.Draw(tex, dest, Color.White * a);
            }
            else
            {
                // มุมซ้ายบน: แถบสีเฉียงสั้น + รูปเล็ก + ชื่อ
                int h = 300;
                int w = (int)(h * aspect);
                int slide = (int)((1f - a) * -60f);
                int x = 30 + slide;
                float cy = 30 + h * 0.62f;
                DrawSlantBand(Math.Max(0, x - 20), x + w + 330, cy, h * 0.30f, -0.10f, accent, a);
                DrawTextCentered(label, new Vector2(x + w + 150 + 2, cy - 10 + 2), Color.Black * (0.6f * a), 2.4f);
                DrawTextCentered(label, new Vector2(x + w + 150, cy - 10), Color.White * a, 2.4f);
                DrawTextCentered("SKILL", new Vector2(x + w + 150, cy + 28), Color.Lerp(accent, Color.White, 0.5f) * a, 1.3f);
                _spriteBatch.Draw(tex, new Rectangle(x, 30, w, h), Color.White * a);
            }
        }

        // ปล่อยลูกเวทย์ของ Lumi จากตัวไปหาเป้าหมาย (หลังอนิเมชันร่ายสกิลจบ)
        private void LaunchLumiProjectile(Character caster, Character target)
        {
            _lumiCasterRef = caster;
            var casterRect = GetAvatarHomeRect(caster);
            var targetRect = GetAvatarHomeRect(target);
            _lumiProjPos = new Vector2(casterRect.X + casterRect.Width / 2f, casterRect.Y + casterRect.Height / 2f);
            _lumiProjTargetPos = new Vector2(targetRect.X + targetRect.Width / 2f, targetRect.Y + targetRect.Height / 2f);
            _lumiIsProjectileMoving = true;
            _lumiUltimateTarget = target;
        }

        private bool IsSelfTargetCaster(Character c)
        {
            return c != null && BattleManager.IsAliceUnit(c);
        }

        // ร่ายสกิลของ Alice (ใช้กับตัวเองเท่านั้น) หลังผู้เล่นกดเลือกตัวเอง
        private void StartSelfUltimate(Character c)
        {
            _aliceCasterRef = c;
            _battle.CastUltimate(c, c);
        }

        // ร่ายสกิลของ Enos: ตีศัตรู 1 ตัว (40% P.ATK) และได้ Aggro ให้ตัวเอง
        private void StartEnosUltimate(Character c, Character target)
        {
            AudioManager.PlaySfx("Audio/Enos_Skill_Sound");
            if (_enosSkillTex != null)
            {
                // เล่นอนิเมชันสกิล 18 เฟรมก่อน แล้วดาเมจ + Aggro จะมีผลกลางอนิเมชัน
                _enosSkillActive = true;
                _enosSkillTimer = 0f;
                _enosSkillCast = false;
                _enosSkillCaster = c;
                _enosSkillTarget = target;
                _battle.UltimateCaster = c;
                _battle.IsUsingUltimate = true;
            }
            else _battle.CastUltimate(c, target);
        }

        // วงกลมเรืองแสงนุ่มๆ ซ้อนหลายชั้น (ยิ่งออกไปข้างนอกยิ่งจาง)
        private void DrawSoftGlow(Vector2 center, float radius, Color color, float strength)
        {
            for (int i = 0; i < 5; i++)
            {
                float r = radius * (1f - i * 0.17f);
                if (r < 2f) break;
                FillCircle(center, r, color * (strength * 0.10f));
            }
        }

        // เป้าสีแดง: วงแหวน + วงเล็กตรงกลาง + ขีด 4 ทิศ (ตามภาพตัวอย่าง) พร้อมแสงที่ตัวเป้าหมาย
        private void DrawTargetCrosshair(Rectangle rect, bool hovered, Color color)
        {
            Vector2 c = new Vector2(rect.Center.X, rect.Y + rect.Height * 0.55f);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(_targetGlowTimer * 6f);

            float R = rect.Width * 0.13f * (1f + (hovered ? 0.05f * pulse : 0f));
            int th = hovered ? 3 : 2;
            Color line = Color.Lerp(color, Color.White, hovered ? 0.15f : 0f) * (hovered ? 1f : 0.75f);

            // วงแหวนใหญ่ + วงเล็กตรงกลาง
            DrawCircleRing(c, R, th, line);
            DrawCircleRing(c, R * 0.24f, th, line);

            // ขีด 4 ทิศ (หมุนช้าๆ ตอนชี้ที่เป้า)
            float rot = hovered ? _targetGlowTimer * 0.9f : 0f;
            for (int k = 0; k < 4; k++)
            {
                float ang = rot + k * MathHelper.PiOver2;
                var dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                DrawLine(c + dir * (R * 0.55f), c + dir * (R * 1.55f), line, th);
            }
        }

        // ตอนกำลังเลือกเป้าหมายสกิล: ศัตรูทุกตัวที่เลือกได้เรืองแสงแดง ตัวที่ชี้อยู่มีเป้าแดงเต็ม
        private void DrawUltimateTargetGlow()
        {
            if (!_isWaitingForUltimateTarget || _battle.UltimateCaster == null) return;

            bool isVeyra = _battle.UltimateCaster.Name?.IndexOf("veyra", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSelf = IsSelfTargetCaster(_battle.UltimateCaster);
            var targets = isSelf ? new List<Character> { _battle.UltimateCaster } : (isVeyra ? _battle.Allies : _battle.Enemies);
            // ศัตรู = แดง / เป้าหมายฝั่งเดียวกัน (Veyra ฮีล, Alice ใช้กับตัวเอง) = น้ำเงิน
            Color glowColor = (isVeyra || isSelf) ? new Color(60, 140, 255) : new Color(235, 40, 40);

            foreach (var unit in targets)
            {
                if (unit == null || !unit.IsAlive) continue;

                var rect = GetAvatarHomeRect(unit);
                bool hovered = unit == _hoveredEnemyTarget || (isVeyra && rect.Contains(VirtualMouse.GetState().Position));
                DrawTargetCrosshair(rect, hovered, glowColor);
            }
        }

        private float _totalTime;
        private const float SkillCardScale = 0.94f;   // การ์ด 280x200 -> 263x188
        private const int SkillCardGap = 14;

        private Rectangle GetSkillCardRect(int i)
        {
            int w = (int)(SkillUiArt.Width * SkillCardScale);
            int h = (int)(SkillUiArt.Height * SkillCardScale);
            int totalWidth = 5 * w + 4 * SkillCardGap;
            int startX = ((ScreenWidth - totalWidth) / 2) + 120;
            int y = ScreenHeight - h - 26;
            return new Rectangle(startX + i * (w + SkillCardGap), y, w, h);
        }

        private void UpdateUltimates(MouseState mouse, bool leftClicked)
        {
            if (_battle.State != BattleState.Running || _introTimer < IntroDuration) return;

            for (int i = 0; i < 5; i++)
            {
                var c = _formationSlots[i];
                if (c != null && c.IsAlive && c.CurrentSP >= c.MaxSP && c.SwordmanBuffDuration <= 0)
                {
                    Rectangle hitBox = GetSkillCardRect(i);

                    if (hitBox.Contains(mouse.Position) && leftClicked && !_battle.IsUsingUltimate && !_isWaitingForUltimateTarget && _ultimateQueuedCaster == null)
                    {
                        _playUIClick();
                        // กดแล้วเข้าคิวก่อน ถ้ายังมีการโจมตีเล่นอยู่จะรอให้จบแล้วกลับที่เดิมก่อน ProcessQueuedUltimate ถึงจะเปิดให้เลือกเป้าหมาย
                        _ultimateQueuedCaster = c;
                        _skillBurstTimer[i] = SkillBurstDuration; // เอฟเฟกต์ตอนกดสกิล
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
            if (_hitTimers.Count > 0)
            {
                var keys = new List<Character>(_hitTimers.Keys);
                foreach (var k in keys)
                {
                    float left = _hitTimers[k] - delta;
                    if (left <= 0f) _hitTimers.Remove(k); else _hitTimers[k] = left;
                }
            }

            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Timer -= delta;
                float life = ft.Duration > 0f ? ft.Duration : FloatingTextDuration;
                float t = MathHelper.Clamp(1f - ft.Timer / life, 0f, 1f);
                ft.Position.Y -= FloatingTextRiseSpeed * (1f - t) * delta; // ลอยขึ้นแบบชะลอลง
                if (ft.Timer <= 0f) _floatingTexts.RemoveAt(i);
            }
        }

        public void Draw()
        {
            DrawBattleBackground();
            DrawTurnQueue();
            DrawTeam(_battle.Allies, isEnemyRow: false);
            DrawTeam(_battle.Enemies, isEnemyRow: true);
            DrawUltimateTargetGlow();

            // --- วาดโปรเจกต์ไทล์ skill_lumi_Boom (160x160, 11 เฟรม) กำลังเคลื่อนที่ไปหาเป้าหมาย ---
            if (_lumiIsProjectileMoving && _lumiSkillBoomTex != null)
            {
                int fw = 160;
                int fh = 160;
                int totalFrames = 11;
                int cols = Math.Max(1, _lumiSkillBoomTex.Width / fw);

                // คำนวณเฟรมตามเวลาเคลื่อนที่
                int currentFrame = (int)(_lumiProjPos.X / 20f) % totalFrames;
                int col = currentFrame % cols;
                int row = currentFrame / cols;

                Rectangle src = new Rectangle(col * fw, row * fh, fw, fh);
                Vector2 origin = new Vector2(fw / 2f, fh / 2f);

                _spriteBatch.Draw(_lumiSkillBoomTex, _lumiProjPos, src, Color.White, 0f, origin, 1.5f, SpriteEffects.None, 0f);
            }

            DrawFloatingTexts();
            DrawTurnArrows();

            DrawFloorIndicator();

            DrawUltimates();
            DrawSkillCutscene();

            if (_battleEndDelayTimer >= 1.3f)
            {
                DrawResultBanner();
            }

            DrawIntroBanner();

            DrawUnitZoom();

            if (_buttonMenuAnim > 0.02f)
            {
                DrawBattleSettingsButton();
                DrawPauseButton();
            }
            DrawAutoToggleButton();
            DrawUiToggleButton();

            if (_ultimateQueuedCaster != null)
            {
                DrawTextCentered($"{_ultimateQueuedCaster.Name}'s Ultimate ready - waiting for the attack to finish... (Right-Click to Cancel)", new Vector2(ScreenWidth / 2f, ScreenHeight - 290), Color.LightGray, 1.2f);
            }

            if (_isWaitingForUltimateTarget)
            {
                bool isVeyraCasting = _battle.UltimateCaster != null && (_battle.UltimateCaster.Name?.IndexOf("veyra", StringComparison.OrdinalIgnoreCase) >= 0);
                string targetTypeMsg = isVeyraCasting ? "an ally target" : "an enemy target";
                string casterName = _battle.UltimateCaster?.Name ?? "Character";
                if (IsSelfTargetCaster(_battle.UltimateCaster))
                    DrawTextCentered($"Click {casterName} to use the Skill on self! (Right-Click to Cancel)", new Vector2(ScreenWidth / 2f, ScreenHeight - 290), new Color(90, 160, 255), 1.3f);
                else
                    DrawTextCentered($"Select {targetTypeMsg} for {casterName}'s Ultimate! (Right-Click to Cancel)", new Vector2(ScreenWidth / 2f, ScreenHeight - 290), Color.Gold, 1.3f);
            }
        }

        private void ZoomText(string text, Vector2 pos, Color color, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            try { _spriteBatch.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); }
            catch (ArgumentException) { }
        }

        // ตัดข้อความเป็นหลายบรรทัดตามความกว้างที่กำหนด
        private List<string> WrapZoomText(string text, float maxWidth, float scale)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text) || _font == null) { result.Add(text ?? ""); return result; }
            string line = "";
            foreach (var word in text.Split(' '))
            {
                string test = line.Length == 0 ? word : line + " " + word;
                float w;
                try { w = _font.MeasureString(test).X * scale; } catch (ArgumentException) { w = 0; }
                if (w > maxWidth && line.Length > 0) { result.Add(line); line = word; }
                else line = test;
            }
            if (line.Length > 0) result.Add(line);
            return result;
        }

        private static string StatusDescription(StatusType t)
        {
            switch (t)
            {
                case StatusType.Aura: return "Powered-up stance: bonus ATK, DEF, Speed and Crit.";
                case StatusType.Blessing: return "+Max HP per stack. At 20 stacks the unit dies.";
                case StatusType.Lifesteal: return "Heals HP when dealing damage.";
                case StatusType.Stun: return "Skips its next turn.";
                case StatusType.Poison: return "Loses 8% of Max HP every turn.";
                case StatusType.Weakness: return "Attack power is reduced.";
                case StatusType.Aggro: return "Enemies must attack this unit.";
                case StatusType.Fire: return "Takes 100 fixed damage per stack. Over 3 stacks = Melt.";
                case StatusType.Melt: return "Max HP reduced by 20%.";
                case StatusType.Shield: return "Blocks the next hit completely (IMMUNE). Each hit removes 1 stack.";
                default: return "";
            }
        }

        // ซูมตัวละคร: ฉากมืด + ตัวใหญ่ด้านซ้าย + แผงข้อมูล (HP ที่เหลือ / DEF / ATK / สถานะ) ด้านขวา
        private void DrawUnitZoom()
        {
            if (_zoomUnit == null) return;
            var u = _zoomUnit;
            float a = MathHelper.Clamp(_zoomAnim, 0f, 1f);
            float e = 1f - (1f - a) * (1f - a);

            DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.78f * a));

            // ตัวละครขยาย (ใช้เส้นทางวาดเดิมของตัวนั้น โดยบังคับกรอบแสดงผลให้ใหญ่)
            var home = GetAvatarHomeRect(u);
            float factor = home.Height > 150 ? 2.0f : 3.4f;
            int size = (int)(home.Height * factor * (0.85f + 0.15f * e));
            _zoomRect = new Rectangle(560 - size / 2, 930 - size, size, size);
            _zoomUnit = u;
            _zoomDrawOnly = true;
            DrawTeam(new List<Character> { u }, u.IsEnemy);
            _zoomDrawOnly = false;

            // แผงข้อมูลด้านขวา
            int slide = (int)((1f - e) * 80f);
            // สูงตามจำนวนสถานะ + ข้อความอธิบายตัดบรรทัดอัตโนมัติ (ไม่ให้ข้อความขาด/ล้นกรอบ)
            var statusList = StatusIcons.Active(u);
            float descScale = 0.75f;
            float descWidth = 640 - 36 - 58 - 30;
            var descLines = new List<List<string>>();
            int listH = 0;
            foreach (var st in statusList)
            {
                var lines = WrapZoomText(StatusDescription(st.Type), descWidth, descScale);
                descLines.Add(lines);
                listH += Math.Max(54, 30 + lines.Count * 22 + 8);
            }
            if (statusList.Count == 0) listH = 40;
            int panelH = Math.Min(960, 400 + listH);
            var panel = new Rectangle(1080 + slide, Math.Max(40, (ScreenHeight - panelH) / 2), 640, panelH);
            Color accent = u.IsEnemy ? new Color(210, 70, 70) : new Color(70, 140, 230);
            DrawRect(panel, new Color(14, 14, 24) * (0.94f * a));
            DrawRectBorder(panel, accent * a, 3);
            DrawRect(new Rectangle(panel.X, panel.Y, panel.Width, 8), accent * a);

            int x = panel.X + 36;
            int y = panel.Y + 28;
            ZoomText(u.Name ?? "", new Vector2(x, y), Color.Gold * a, 1.8f);
            ZoomText(u.IsEnemy ? "ENEMY" : "ALLY", new Vector2(panel.Right - 120, y + 8), accent * a, 1.0f);
            y += 70;

            // HP ที่เหลือ
            int hp = Math.Max(0, u.Hp);
            ZoomText($"HP  {hp} / {u.MaxHp}", new Vector2(x, y), Color.White * a, 1.3f);
            y += 46;
            var hpBg = new Rectangle(x, y, panel.Width - 72, 26);
            DrawRect(hpBg, new Color(60, 20, 20) * a);
            float ratio = u.MaxHp > 0 ? MathHelper.Clamp((float)hp / u.MaxHp, 0f, 1f) : 0f;
            DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * ratio), hpBg.Height), (ratio > 0.5f ? Color.LimeGreen : ratio > 0.2f ? Color.Orange : Color.Red) * a);
            DrawRectBorder(hpBg, Color.Black * a, 2);
            y += 56;

            // DEF / ATK
            int colB = x + 300;
            ZoomText($"P. ATK:  {u.PAtk}", new Vector2(x, y), Color.White * a, 1.1f);
            ZoomText($"M. ATK:  {u.MAtk}", new Vector2(colB, y), Color.White * a, 1.1f);
            y += 42;
            ZoomText($"P. DEF:  {u.PDef}", new Vector2(x, y), Color.White * a, 1.1f);
            ZoomText($"M. DEF:  {u.MDef}", new Vector2(colB, y), Color.White * a, 1.1f);
            y += 54;

            DrawRect(new Rectangle(x, y, panel.Width - 72, 2), Color.Gold * (0.5f * a));
            y += 14;
            ZoomText("STATUS", new Vector2(x, y), Color.Gold * a, 1.1f);
            y += 40;

            if (statusList.Count == 0)
            {
                ZoomText("No status effects", new Vector2(x, y), Color.Gray * a, 0.95f);
            }
            else
            {
                for (int i = 0; i < statusList.Count; i++)
                {
                    var st = statusList[i];
                    var icon = new Rectangle(x, y, 44, 44);
                    StatusIcons.Draw(_spriteBatch, st.Type, icon, a);
                    string head = StatusIcons.Label(st.Type);
                    if (st.Turns > 0) head += $"  -  {st.Turns} turn" + (st.Turns > 1 ? "s" : "");
                    if (st.Stacks > 0) head += $"  x{st.Stacks}";
                    ZoomText(head, new Vector2(x + 58, y - 2), Color.White * a, 0.95f);
                    for (int l = 0; l < descLines[i].Count; l++)
                        ZoomText(descLines[i][l], new Vector2(x + 58, y + 24 + l * 22), Color.LightGray * a, descScale);
                    y += Math.Max(54, 30 + descLines[i].Count * 22 + 8);
                }
            }

            ZoomText("(Right-click or click anywhere to close)", new Vector2(panel.X + 36, panel.Bottom - 34), Color.Gray * a, 0.75f);
        }

        // จำนวนเฟรมแอนิเมชันโจมตีของแต่ละตัว (ใช้กำหนดเวลาที่ดาเมจเข้า = เฟรมที่ 2)
        private int GetAttackFrameCount(Character unit)
        {
            if (unit == null) return 0;
            if (IsAlice(unit)) return _aliceAttackTex != null ? 6 : 0;
            if (IsVeyra(unit)) return _veyraAttackTex != null ? SpriteFrames.FrameCount(_veyraAttackTex, SpriteFrames.VeyraAttackFrameWidth) : 0;
            if (IsLumi(unit)) return _lumiAttackTex != null ? Math.Max(1, _lumiAttackTex.Width / 320) : 0;
            if (IsEnos(unit)) return _enosAttackTex != null ? SpriteFrames.GridFrameCount(_enosAttackTex) : 0;
            string n = unit.Name ?? "";
            if (n.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0 || unit == _elmaCasterRef)
                return _elmaAttackTex != null ? Math.Max(1, _elmaAttackTex.Width / 320) : 0;
            if (IsWolf(unit)) return _wolfAttackTex != null ? WolfFrameCount(_wolfAttackTex) : 0;
            if (IsGolem(unit)) return _stoneGolemAttackTex != null ? GolemAttackFrameCount() : 0;
            if (_giantHornetAttackTex != null && (n.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0)) return 10;
            return 0;
        }

        private bool IsAlice(Character unit)
        {
            if (unit == null) return false;
            if (unit == _aliceCasterRef) return true;
            string name = unit.Name ?? "";
            return name == "Alice" || name == "Swordman" || name.Contains("Alice") || name.Contains("Swordman");
        }

        private void GetUnitPortraitInfo(Character unit, out Texture2D tex, out Rectangle? sourceRect)
        {
            if (IsElma(unit) && _elmaIdleTex != null)
            {
                tex = _elmaIdleTex;
                sourceRect = new Rectangle(0, 0, 160, 320);
                return;
            }

            if (IsLumi(unit) && _lumiIdleTex != null)
            {
                tex = _lumiIdleTex;
                sourceRect = new Rectangle(0, 0, 240, 320);
                return;
            }

            if (IsAlice(unit) && _aliceIdleTex != null)
            {
                tex = _aliceIdleTex;
                sourceRect = new Rectangle(0, 0, 480, 480);
                return;
            }

            if (IsEnos(unit) && _enosIdleTex != null)
            {
                tex = _enosIdleTex;
                sourceRect = SpriteFrames.EnosFrame(_enosIdleTex, 0);
                return;
            }

            if (IsVeyra(unit) && _veyraIdleTex != null)
            {
                tex = _veyraIdleTex;
                sourceRect = SpriteFrames.VeyraFrame(_veyraIdleTex, 0);
                return;
            }

            if (!unit.IsEnemy)
            {
                tex = _testCharacterTex;
                sourceRect = null;
                return;
            }

            if (IsGolem(unit) && _stoneGolemTex != null)
            {
                tex = _stoneGolemTex;
                sourceRect = GolemFrameRect(_stoneGolemTex, 0);
                return;
            }

            if (IsWolf(unit) && _wolfTex != null)
            {
                tex = _wolfTex;
                sourceRect = WolfFrameRect(_wolfTex, 0);
                return;
            }

            bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0);

            if (isGiantHornet && _giantHornetIdleTex != null)
            {
                tex = _giantHornetIdleTex;
                sourceRect = new Rectangle(0, 0, 192, 192);
                return;
            }

            tex = GetEnemyTexture(unit);
            sourceRect = null;
        }

        private static bool IsGolem(Character u)
        {
            return u != null && u.IsEnemy && !string.IsNullOrEmpty(u.Name)
                && (u.Name.IndexOf("golem", StringComparison.OrdinalIgnoreCase) >= 0
                    || u.Name.IndexOf("king", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Stone Golem attack sheet: 480x480 per frame, 9 frames (works for a single strip or a grid)
        private const int GolemAttackFrameSize = 480;
        private const int GolemAttackFrames = 9;
        // The attack canvas is 480 vs the 192 idle canvas: draw it at 192/480 of the idle scale so the Golem keeps the same size.
        // (If the Golem looks too small/big while attacking, change this multiplier.)
        private const float GolemAttackScaleMul = (float)GolemFrameSize / GolemAttackFrameSize;

        private int GolemAttackFrameCount() => _stoneGolemAttackTex == null ? 0 : GolemAttackFrames;

        private Rectangle GolemAttackFrameRect(int frame)
        {
            var tex = _stoneGolemAttackTex;
            frame = Math.Max(0, Math.Min(GolemAttackFrames - 1, frame));
            const int fs = GolemAttackFrameSize;
            if (tex.Height <= fs + 8)
            {
                // single row: place by exact pitch (guards against a few px of width error)
                float pitch = tex.Width / (float)GolemAttackFrames;
                int x = (int)Math.Round(frame * pitch);
                int w = Math.Min(fs, tex.Width - x);
                return new Rectangle(x, 0, Math.Max(1, w), Math.Min(fs, tex.Height));
            }
            int cols = Math.Max(1, tex.Width / fs);
            return new Rectangle((frame % cols) * fs, (frame / cols) * fs, fs, fs);
        }

        private static Rectangle GolemFrameRect(Texture2D tex, int frame)
        {
            int total = GolemFrames;
            frame = ((frame % total) + total) % total;
            if (tex.Height <= GolemFrameSize + 8)
            {
                // Single-row strip: the sheet may be a few px off (e.g. 2309 wide), so
                // place each frame by exact pitch to avoid drift / neighbour bleed.
                float pitch = tex.Width / (float)total;
                int x = (int)Math.Round(frame * pitch);
                int w = Math.Min(GolemFrameSize, tex.Width - x);
                return new Rectangle(x, 0, Math.Max(1, w), Math.Min(GolemFrameSize, tex.Height));
            }
            int cols = Math.Max(1, tex.Width / GolemFrameSize);
            return new Rectangle((frame % cols) * GolemFrameSize, (frame / cols) * GolemFrameSize, GolemFrameSize, GolemFrameSize);
        }

        private static bool IsWolf(Character u)
        {
            return u != null && u.IsEnemy && !string.IsNullOrEmpty(u.Name)
                && u.Name.IndexOf("wolf", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Frame size / count for a wolf strip sheet (square frames)
        private static int WolfFrameSize(Texture2D tex) { return 192; }
        private static int WolfFrameCount(Texture2D tex) { return Math.Max(1, tex.Width / 192); }

        private static Rectangle WolfFrameRect(Texture2D tex, int frame)
        {
            int fs = WolfFrameSize(tex);
            int count = WolfFrameCount(tex);
            frame = Math.Max(0, Math.Min(count - 1, frame));
            return new Rectangle(frame * fs, 0, fs, fs);
        }

        private void GetWolfDrawInfo(Character unit, out Texture2D tex, out Rectangle src)
        {
            bool attacking = _battle != null && _battle.ActiveAttacker == unit && _wolfAttackTex != null;
            tex = attacking ? _wolfAttackTex : _wolfTex;
            if (tex == null) { src = Rectangle.Empty; return; }
            int count = WolfFrameCount(tex);
            int frame = attacking
                ? Math.Min(count - 1, (int)(_battle.AttackAnimTimer / _battle.CurrentAttackDuration * count))
                : _wolfFrameIndex % count;
            src = WolfFrameRect(tex, frame);
        }

        private void DrawTurnQueue()
        {
            if (_battle == null) return;

            int startX = 50;
            int startY = 185;
            int boxWidth = 75;
            int boxHeight = 66;
            int spacing = 10;
            int maxShow = 7;

            Character activeUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;
            var order = PredictTurnOrder(maxShow);

            // หัวข้อแถบ
            DrawRect(new Rectangle(startX - 8, startY - 44, boxWidth + 16, 32), new Color(20, 20, 30, 220));
            DrawTextCentered($"TURN {_battle.TurnNumber}", new Vector2(startX + boxWidth / 2f, startY - 28), Color.Gold, 0.9f);

            int yCursor = startY;
            for (int count = 0; count < order.Count; count++)
            {
                var unit = order[count];
                bool isCurrentTurn = (count == 0 && unit == activeUnit);

                // ช่องแรก (ตาปัจจุบัน) ใหญ่กว่าช่องอื่นเล็กน้อย
                int w = isCurrentTurn ? boxWidth + 14 : boxWidth;
                int h = isCurrentTurn ? boxHeight + 10 : boxHeight;
                Rectangle rect = new Rectangle(isCurrentTurn ? startX - 7 : startX, yCursor, w, h);
                yCursor += h + spacing;

                Color bgColor = unit.IsEnemy ? new Color(140, 40, 40, 220) : new Color(40, 90, 160, 220);
                if (isCurrentTurn)
                {
                    bgColor = new Color(255, 215, 0, 240);
                }
                else if (count > 0)
                {
                    // ยิ่งไกลยิ่งจาง ให้รู้ว่าเป็นลำดับในอนาคต
                    float fade = MathHelper.Clamp(1f - count * 0.08f, 0.5f, 1f);
                    bgColor = bgColor * fade;
                }

                DrawRect(rect, bgColor);

                GetUnitPortraitInfo(unit, out Texture2D tex, out Rectangle? srcRect);
                if (tex != null)
                {
                    float srcW = srcRect.HasValue ? srcRect.Value.Width : tex.Width;
                    float srcH = srcRect.HasValue ? srcRect.Value.Height : tex.Height;

                    float scaleX = (float)(rect.Width - 6) / srcW;
                    float scaleY = (float)(rect.Height - 6) / srcH;
                    float scale = Math.Min(scaleX, scaleY);

                    Vector2 drawPos = new Vector2(
                        rect.Center.X - (srcW * scale) / 2f,
                        rect.Center.Y - (srcH * scale) / 2f
                    );

                    SpriteEffects effect = unit.IsEnemy ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    _spriteBatch.Draw(tex, drawPos, srcRect, Color.White, 0f, Vector2.Zero, scale, effect, 0f);
                }

                Color borderColor = isCurrentTurn ? Color.White : Color.DarkGray;
                int borderWidth = isCurrentTurn ? 3 : 2;
                DrawRectBorder(rect, borderColor, borderWidth);

                // ป้ายลำดับมุมขวาล่าง (ช่องแรกคือตาปัจจุบัน ไม่ต้องมีเลข)
                if (count > 0)
                {
                    var badge = new Rectangle(rect.Right - 18, rect.Bottom - 18, 18, 18);
                    DrawRect(badge, new Color(0, 0, 0, 200));
                    DrawTextCentered((count + 1).ToString(), new Vector2(badge.Center.X, badge.Center.Y), Color.White, 0.6f);
                }
            }
        }

        // ลำดับตา: ตัวที่กำลังเดิน แล้วตามด้วยคิวที่เหลือในรอบ (Speed มาก -> น้อย) และรอบถัดไป
        private List<Character> PredictTurnOrder(int maxShow)
        {
            var result = new List<Character>();
            Character active = _battle.PendingAttacker ?? _battle.ActiveAttacker;
            if (active != null && active.IsAlive) result.Add(active);

            // ลำดับที่เหลือในรอบนี้ + รอบถัดไป มาจากระบบเทิร์นแบบวนรอบของ BattleManager
            result.AddRange(_battle.PredictUpcoming(Math.Max(0, maxShow - result.Count)));
            return result;
        }

        // ---------- อนุภาคหลายเหลี่ยมสีรุ้งลอยขึ้นบนการ์ดสกิลตอนติดบัฟ (แบบในคลิป) ----------
        private class AuraParticle
        {
            public Vector2 Pos;   // ตำแหน่งในหน่วยพิกเซลของรูปกรอบ (366x206) นับจากมุมซ้ายบนของการ์ด
            public float Speed;
            public float Size;
            public float Age;
            public float Life;
            public float Rot;
            public float Hue;
        }

        private readonly Dictionary<Character, List<AuraParticle>> _auraParticles = new Dictionary<Character, List<AuraParticle>>();
        private readonly Dictionary<Character, float> _auraSpawnTimers = new Dictionary<Character, float>();
        private readonly Random _auraRng = new Random();

        // จำนวนด้านของรูปทรงที่ลอยขึ้น (ในคลิปเป็นรูปหกเหลี่ยม ถ้าอยากได้ห้าเหลี่ยมให้เปลี่ยนเป็น 5)
        private const int AuraPolygonSides = 6;

        // จุดเกิดอนุภาค: ขอบซ้าย ขอบขวา และขอบบนของช่องสกิล (พิกัดรูปกรอบ)
        private Vector2 SpawnAuraPos()
        {
            float r() => (float)_auraRng.NextDouble();
            int edge = _auraRng.Next(4);
            if (edge == 0) return new Vector2(208f + (r() - 0.5f) * 10f, 70f + r() * 105f);
            if (edge == 1) return new Vector2(353f + (r() - 0.5f) * 10f, 70f + r() * 105f);
            return new Vector2(210f + r() * 141f, 20f + (r() - 0.5f) * 8f);
        }

        private void UpdateAuraParticles(float delta)
        {
            if (_formationSlots == null) return;

            foreach (var c in _formationSlots)
            {
                if (c == null) continue;

                if (!_auraParticles.TryGetValue(c, out var list))
                {
                    list = new List<AuraParticle>();
                    _auraParticles[c] = list;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var p = list[i];
                    p.Age += delta;
                    p.Pos.Y -= p.Speed * delta;
                    p.Rot += delta * 0.8f;
                    if (p.Age >= p.Life) list.RemoveAt(i);
                }

                bool buffed = c.IsAlive && c.SwordmanBuffDuration > 0;
                if (!buffed) { _auraSpawnTimers[c] = 0f; continue; }

                _auraSpawnTimers.TryGetValue(c, out float timer);
                timer += delta;
                while (timer >= 0.06f)
                {
                    timer -= 0.06f;
                    list.Add(new AuraParticle
                    {
                        Pos = SpawnAuraPos(),
                        Speed = 18f + (float)_auraRng.NextDouble() * 30f,
                        Size = 3.5f + (float)_auraRng.NextDouble() * 5f,
                        Life = 1.0f + (float)_auraRng.NextDouble() * 0.9f,
                        Rot = (float)_auraRng.NextDouble() * MathHelper.TwoPi,
                        Hue = _totalTime * 0.7f + (float)_auraRng.NextDouble() * 0.15f
                    });
                }
                _auraSpawnTimers[c] = timer;
            }
        }

        // เติมรูปหลายเหลี่ยมด้านเท่า (สแกนไลน์)
        private void FillPolygon(Vector2 center, float radius, int sides, float rotation, Color color, Rectangle clip)
        {
            if (sides < 3 || radius < 1f) return;

            var v = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = rotation + i * MathHelper.TwoPi / sides;
                v[i] = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
            }

            int yMin = (int)Math.Floor(center.Y - radius);
            int yMax = (int)Math.Ceiling(center.Y + radius);
            var xs = new List<float>(4);

            for (int y = Math.Max(yMin, clip.Top); y <= Math.Min(yMax, clip.Bottom - 1); y++)
            {
                xs.Clear();
                float sy = y + 0.5f;
                for (int i = 0; i < sides; i++)
                {
                    Vector2 a = v[i], b = v[(i + 1) % sides];
                    if ((a.Y <= sy && b.Y > sy) || (b.Y <= sy && a.Y > sy))
                    {
                        float t = (sy - a.Y) / (b.Y - a.Y);
                        xs.Add(a.X + t * (b.X - a.X));
                    }
                }

                if (xs.Count >= 2)
                {
                    xs.Sort();
                    int x0 = Math.Max((int)Math.Round(xs[0]), clip.Left);
                    int x1 = Math.Min((int)Math.Round(xs[xs.Count - 1]), clip.Right);
                    if (x1 > x0) _spriteBatch.Draw(_pixel, new Rectangle(x0, y, x1 - x0, 1), color);
                }
            }
        }

        // สีรุ้งตามค่า hue 0..1 (วนซ้ำ)
        private static Color HueColor(float h)
        {
            h -= (float)Math.Floor(h);
            float r = MathHelper.Clamp(Math.Abs(h * 6f - 3f) - 1f, 0f, 1f);
            float g = MathHelper.Clamp(2f - Math.Abs(h * 6f - 2f), 0f, 1f);
            float b = MathHelper.Clamp(2f - Math.Abs(h * 6f - 4f), 0f, 1f);
            return new Color(r, g, b);
        }

        // ออร่าสายรุ้งรอบตัวละครตอนติดบัฟ: วาดสำเนาตัวละครเยื้องไปรอบทิศ แต่ละทิศสีต่างกันและวนเปลี่ยนตามเวลา
        private void DrawRainbowAura(Texture2D tex, Rectangle src, Vector2 pos, float scale, float alpha)
        {
            if (tex == null) return;
            float t = _totalTime * 0.7f;
            const int dirs = 12;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = (ring == 0 ? 3.5f : 7.5f) * Math.Max(0.6f, scale);
                float a = (ring == 0 ? 0.60f : 0.28f) * alpha;
                for (int i = 0; i < dirs; i++)
                {
                    float ang = i * MathHelper.TwoPi / dirs;
                    var off = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radius;
                    _spriteBatch.Draw(tex, pos + off, src, HueColor(t + i / (float)dirs) * a, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }

        // การ์ดสกิลยกขึ้นเล็กน้อยตอนเอาเมาส์ชี้ (ไล่ค่านุ่มๆ ตามเวลา)
        private const float SkillCardHoverLift = 12f;
        private readonly float[] _skillCardLift = new float[5];

        // เอฟเฟกต์ตอนกดใช้สกิล: วงกรอบทองขยายออก + แสงวาบขาว + ประกายพุ่งกระจาย
        private const float SkillBurstDuration = 0.6f;
        private readonly float[] _skillBurstTimer = new float[5];
        private float _skillLiftLastTime;

        private void DrawUltimates()
        {
            var mousePos = VirtualMouse.GetState().Position;
            float dt = MathHelper.Clamp(_totalTime - _skillLiftLastTime, 0f, 0.1f);
            _skillLiftLastTime = _totalTime;

            for (int i = 0; i < 5; i++)
            {
                var c = _formationSlots[i];
                var rect = GetSkillCardRect(i); // ใช้กรอบตำแหน่งเดิมตรวจเมาส์ ไม่ให้กระพริบตอนการ์ดลอยขึ้น
                bool alive = c != null && c.IsAlive;
                bool ready = alive && c.MaxSP > 0 && c.CurrentSP >= c.MaxSP && c.SwordmanBuffDuration <= 0;
                bool hovered = alive && rect.Contains(mousePos);

                float target = (hovered && ready) ? 1f : 0f; // ยกเฉพาะตอนสกิลพร้อมใช้ (SP เต็ม 100%)
                _skillCardLift[i] += (target - _skillCardLift[i]) * (1f - (float)Math.Exp(-16f * dt));
                if (Math.Abs(_skillCardLift[i] - target) < 0.01f) _skillCardLift[i] = target;

                var drawRect = rect;
                drawRect.Y -= (int)Math.Round(SkillCardHoverLift * _skillCardLift[i]);
                DrawSkillCard(drawRect, c, alive, ready, hovered);

                if (_skillBurstTimer[i] > 0f)
                {
                    _skillBurstTimer[i] = Math.Max(0f, _skillBurstTimer[i] - dt);
                    DrawSkillBurst(drawRect, 1f - _skillBurstTimer[i] / SkillBurstDuration);
                }
            }
        }

        // t: 0 -> 1 ตลอดเอฟเฟกต์
        private void DrawSkillBurst(Rectangle card, float t)
        {
            float fade = 1f - t;
            float ease = 1f - (1f - t) * (1f - t); // พุ่งเร็วแล้วชะลอ
            Vector2 center = new Vector2(card.Center.X, card.Center.Y);

            // แสงวาบสีขาวทับการ์ด (เฉพาะช่วงแรก)
            float flash = MathHelper.Clamp(1f - t * 3f, 0f, 1f);
            if (flash > 0f) DrawRect(card, Color.White * (0.55f * flash));

            // วงกรอบทองขยายออกจากการ์ดแล้วจางหาย
            int grow = (int)(ease * 46f);
            var ring = new Rectangle(card.X - grow, card.Y - grow, card.Width + grow * 2, card.Height + grow * 2);
            DrawRectBorder(ring, Color.Gold * (0.9f * fade), Math.Max(2, (int)(5f * fade)));
            if (t > 0.15f)
            {
                int grow2 = (int)(((t - 0.15f) / 0.85f) * 30f);
                DrawRectBorder(new Rectangle(card.X - grow2, card.Y - grow2, card.Width + grow2 * 2, card.Height + grow2 * 2), Color.White * (0.6f * fade), 2);
            }

            // ประกายพุ่งออกรอบทิศ
            const int sparks = 12;
            for (int k = 0; k < sparks; k++)
            {
                float ang = k * MathHelper.TwoPi / sparks + 0.3f;
                float dist = ease * (card.Width * 0.9f + (k % 3) * 18f);
                var pos = center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * dist;
                float size = (k % 2 == 0 ? 7f : 4.5f) * fade + 1f;
                FillCircle(pos, size, Color.Lerp(Color.Gold, Color.White, (k % 3) / 3f) * fade);
            }
        }

        // ---------- การ์ดสกิลแบบกรอบวงกลม (พอร์เทรต + ช่อง Ultimate + หลอด HP/SP) ----------
        private static readonly string[] GlyphShield = { ".######.", ".######.", ".######.", ".######.", "..####..", "..####..", "...##...", "........" };
        private static readonly string[] GlyphArrow = { "......##", ".....###", "....#.##", "...#....", "..#.....", ".#......", "#.......", "........" };
        private static readonly string[] GlyphCross = { "...##...", "...##...", ".######.", ".######.", "...##...", "...##...", "...##...", "........" };
        private static readonly string[] GlyphStar = { "...##...", "...##...", "########", ".######.", "..####..", ".##..##.", "##....##", "........" };
        private static readonly string[] GlyphSword = { "......##", ".....##.", "....##..", "#..##...", ".###....", "..##....", ".#.#....", "#...#..." };

        private readonly Dictionary<Character, Texture2D> _portraitCache = new Dictionary<Character, Texture2D>();

        private void GetCharacterGlyph(Character c, out string[] glyph, out Color color)
        {
            glyph = GlyphStar; color = Color.Gold;
            string n = c?.Name ?? "";
            if (n.Contains("Enos")) { glyph = GlyphShield; color = new Color(120, 180, 255); }
            else if (n.Contains("Elma")) { glyph = GlyphArrow; color = new Color(140, 230, 120); }
            else if (n.Contains("Veyra")) { glyph = GlyphCross; color = new Color(255, 120, 150); }
            else if (n.Contains("Lumi")) { glyph = GlyphStar; color = new Color(190, 140, 255); }
            else if (n.Contains("Alice")) { glyph = GlyphSword; color = new Color(255, 170, 90); }
        }

        private void DrawGlyph(string[] glyph, Rectangle box, Color color)
        {
            int cell = Math.Max(1, Math.Min(box.Width, box.Height) / 8);
            int offX = box.X + (box.Width - cell * 8) / 2;
            int offY = box.Y + (box.Height - cell * 8) / 2;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (glyph[y][x] == '#')
                        DrawRect(new Rectangle(offX + x * cell, offY + y * cell, cell, cell), color);
        }

        private void FillCircle(Vector2 center, float radius, Color color)
        {
            int r = (int)radius;
            for (int dy = -r; dy <= r; dy++)
            {
                float t = dy / radius;
                int half = (int)(radius * Math.Sqrt(Math.Max(0, 1 - t * t)));
                if (half <= 0) continue;
                _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - half, (int)center.Y + dy, half * 2, 1), color);
            }
        }

        private void DrawCircleRing(Vector2 center, float radius, int thickness, Color color)
        {
            int r = (int)radius;
            float inner = radius - thickness;
            for (int dy = -r; dy <= r; dy++)
            {
                float t = dy / radius;
                int outer = (int)(radius * Math.Sqrt(Math.Max(0, 1 - t * t)));
                if (outer <= 0) continue;
                int inn = 0;
                if (Math.Abs(dy) < inner)
                {
                    float ti = dy / inner;
                    inn = (int)(inner * Math.Sqrt(Math.Max(0, 1 - ti * ti)));
                }

                if (inn <= 0)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - outer, (int)center.Y + dy, outer * 2, 1), color);
                }
                else
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - outer, (int)center.Y + dy, outer - inn, 1), color);
                    _spriteBatch.Draw(_pixel, new Rectangle((int)center.X + inn, (int)center.Y + dy, outer - inn, 1), color);
                }
            }
        }

        // ตัดภาพหัว/ลำตัวของตัวละครเป็นพอร์เทรตกลม 128x128 (สร้างครั้งเดียวแล้วเก็บไว้)
        private Texture2D GetPortraitTexture(Character c)
        {
            if (c == null) return null;
            if (_portraitCache.TryGetValue(c, out var cached)) return cached;

            Texture2D result = null;
            try
            {
                GetUnitPortraitInfo(c, out Texture2D tex, out Rectangle? srcOpt);
                // ฮีโร่ที่มีรูปโปรไฟล์ (โฟลเดอร์ Profile) ใช้รูปนั้นแทนสไปรต์
                Texture2D profileTex = c.IsEnemy ? null : Where_My_Slime.Data.HeroProfiles.Get(c.Name);
                if (profileTex != null) { tex = profileTex; srcOpt = null; }
                if (tex != null)
                {
                    Rectangle frame = srcOpt ?? new Rectangle(0, 0, tex.Width, tex.Height);
                    frame = Rectangle.Intersect(frame, new Rectangle(0, 0, tex.Width, tex.Height));
                    var data = new Color[frame.Width * frame.Height];
                    tex.GetData(0, frame, data, 0, data.Length);

                    int minX = frame.Width, minY = frame.Height, maxX = -1, maxY = -1;
                    for (int y = 0; y < frame.Height; y++)
                        for (int x = 0; x < frame.Width; x++)
                            if (data[y * frame.Width + x].A > 20)
                            {
                                if (x < minX) minX = x; if (x > maxX) maxX = x;
                                if (y < minY) minY = y; if (y > maxY) maxY = y;
                            }

                    if (maxX >= 0)
                    {
                        int bw = maxX - minX + 1, bh = maxY - minY + 1;
                        int side = Math.Max(8, Math.Min(bh, (int)(bw * 1.25f)));
                        float cx = minX + bw / 2f;
                        int sx = (int)(cx - side / 2f);
                        int sy = minY - side / 12;
                        if (profileTex != null)
                        {
                            // รูปโปรไฟล์: ตัดสี่เหลี่ยมจัตุรัสเต็มความกว้างจากด้านบน (หัว + ลำตัว)
                            side = Math.Max(8, Math.Min(frame.Width, frame.Height));
                            sx = (frame.Width - side) / 2;
                            sy = Math.Min(frame.Height - side, (int)(frame.Height * 0.04f));
                        }

                        const int N = 128;
                        var outData = new Color[N * N];
                        var bg = new Color(58, 50, 46);
                        for (int py = 0; py < N; py++)
                        {
                            for (int px = 0; px < N; px++)
                            {
                                float dx = px + 0.5f - N / 2f, dy = py + 0.5f - N / 2f;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                float cover = MathHelper.Clamp(N / 2f - dist, 0f, 1f);
                                if (cover <= 0f) { outData[py * N + px] = Color.Transparent; continue; }

                                int tx = sx + px * side / N;
                                int ty = sy + py * side / N;
                                Color s = Color.Transparent;
                                if (tx >= 0 && ty >= 0 && tx < frame.Width && ty < frame.Height) s = data[ty * frame.Width + tx];

                                // วางตัวละครทับพื้นหลังสีเข้ม (ข้อมูลเป็น premultiplied)
                                float a = s.A / 255f;
                                Color col = new Color(
                                    (int)(s.R + bg.R * (1f - a)),
                                    (int)(s.G + bg.G * (1f - a)),
                                    (int)(s.B + bg.B * (1f - a)),
                                    255);
                                outData[py * N + px] = col * cover;
                            }
                        }

                        result = new Texture2D(_graphicsDevice, N, N);
                        result.SetData(outData);
                    }
                }
            }
            catch { result = null; }

            _portraitCache[c] = result;
            return result;
        }

        // การ์ดสกิลแบบใช้ภาพ 280x200 (กรอบ + ไอคอนสกิล) จากโฟลเดอร์ Ui_Skill
        private void DrawSkillCardArt(Rectangle rect, Character c, Texture2D frameTex, Texture2D iconTex, bool alive, bool ready, bool hovered)
        {
            float k = rect.Width / (float)SkillUiArt.Width;
            Rectangle Map(Rectangle r) => new Rectangle(rect.X + (int)(r.X * k), rect.Y + (int)(r.Y * k), Math.Max(1, (int)(r.Width * k)), Math.Max(1, (int)(r.Height * k)));
            float pulse = 0.5f + 0.5f * (float)Math.Sin(_totalTime * 5f);
            bool buffed = alive && c.SwordmanBuffDuration > 0;

            // กรอบ (วงกลม/ช่องสกิลในภาพเป็นสีทึบ ต้องวาดพอร์เทรตและตัวเติม SP ทับหลังจากนี้)
            Color tint = !alive ? new Color(110, 110, 110) : Color.White;
            _spriteBatch.Draw(frameTex, rect, tint);

            // พอร์เทรตในวงกลม
            var circleC = new Vector2(rect.X + SkillUiArt.CircleCenter.X * k, rect.Y + SkillUiArt.CircleCenter.Y * k);
            float circleR = SkillUiArt.CircleRadius * k;
            var portrait = GetPortraitTexture(c);
            if (portrait != null)
            {
                var dest = new Rectangle((int)(circleC.X - circleR), (int)(circleC.Y - circleR), (int)(circleR * 2), (int)(circleR * 2));
                _spriteBatch.Draw(portrait, dest, Color.White * (alive ? 1f : 0.35f));
            }

            // ช่องสกิล: เติมจากล่างขึ้นบนตาม SP
            var slot = Map(SkillUiArt.SkillSlot);
            float spRatio = 0f;
            if (alive)
            {
                spRatio = c.MaxSP > 0 ? MathHelper.Clamp((float)c.CurrentSP / c.MaxSP, 0f, 1f) : 0f;
                Color spCol = ready ? Color.Lerp(new Color(235, 180, 30), new Color(255, 230, 120), pulse) : new Color(30, 120, 220);
                if (buffed) spCol = HueColor(_totalTime * 0.7f);
                int fillH = (int)(slot.Height * spRatio);
                DrawRect(new Rectangle(slot.X, slot.Bottom - fillH, slot.Width, fillH), spCol * (ready ? 0.85f : 0.65f));
            }

            // ไอคอนสกิล (ชั้นทับ)
            if (iconTex != null)
                _spriteBatch.Draw(iconTex, rect, alive ? (ready ? Color.White : Color.White * 0.85f) : new Color(110, 110, 110));

            if (alive)
            {
                string label = ready ? "MAX" : $"{(int)(spRatio * 100)}%";
                DrawTextCentered(label, new Vector2(slot.Center.X, slot.Bottom - (int)(slot.Height * 0.12f)), ready ? Color.White : Color.LightGray, 1.0f);
            }

            // หลอด HP ทับแถบสีเขียวในกรอบ
            var bar = Map(SkillUiArt.Bar);
            DrawRect(bar, new Color(55, 18, 18));
            if (alive)
            {
                float hpRatio = c.MaxHp > 0 ? MathHelper.Clamp((float)c.Hp / c.MaxHp, 0f, 1f) : 0f;
                Color hpCol = hpRatio > 0.5f ? new Color(60, 200, 60) : (hpRatio > 0.2f ? Color.Orange : Color.Red);
                DrawRect(new Rectangle(bar.X, bar.Y, (int)(bar.Width * hpRatio), bar.Height), hpCol);
            }
            DrawRectBorder(bar, Color.Black * 0.6f, 1);

            // เอฟเฟกต์พร้อมใช้ / บัฟ Alice
            if (alive)
            {
                if (buffed)
                {
                    DrawCircleRing(circleC, circleR + 1f, 3, HueColor(_totalTime * 0.7f));
                    DrawRectBorder(slot, HueColor(_totalTime * 0.7f + 0.5f), 3);
                }
                if (ready)
                {
                    DrawCircleRing(circleC, circleR + 1f, 3, Color.Gold * (0.55f + 0.45f * pulse));
                    DrawRectBorder(slot, (hovered ? Color.White : Color.Gold) * (0.6f + 0.4f * pulse), 3);
                }
            }

            if (buffed && _auraParticles.TryGetValue(c, out var plist))
            {
                var clip = new Rectangle(slot.X - (int)(14 * k), slot.Y - (int)(14 * k), slot.Width + (int)(28 * k), slot.Height + (int)(28 * k));
                foreach (var p in plist)
                {
                    float life01 = MathHelper.Clamp(p.Age / p.Life, 0f, 1f);
                    float fade = (float)Math.Sin(life01 * Math.PI);
                    Color pc = Color.Lerp(HueColor(p.Hue + p.Age * 0.25f), Color.White, 0.25f) * (0.9f * fade);
                    FillPolygon(new Vector2(rect.X + p.Pos.X * k, rect.Y + p.Pos.Y * k), p.Size * k, AuraPolygonSides, p.Rot, pc, clip);
                }
            }
        }

        private void DrawSkillCard(Rectangle rect, Character c, bool alive, bool ready, bool hovered)
        {
            if (c != null && SkillUiArt.TryGet(c.Name, out var uiFrame, out var uiIcon))
            {
                DrawSkillCardArt(rect, c, uiFrame, uiIcon, alive, ready, hovered);
                return;
            }

            float k = rect.Width / (float)SkillFrameArt.Width;
            Rectangle Map(Rectangle r) => new Rectangle(rect.X + (int)(r.X * k), rect.Y + (int)(r.Y * k), Math.Max(1, (int)(r.Width * k)), Math.Max(1, (int)(r.Height * k)));
            Vector2 MapP(Vector2 p) => new Vector2(rect.X + p.X * k, rect.Y + p.Y * k);

            float pulse = 0.5f + 0.5f * (float)Math.Sin(_totalTime * 5f);
            Color dark = new Color(34, 30, 28);
            Vector2 circleC = MapP(SkillFrameArt.CircleCenter);
            float circleR = SkillFrameArt.CircleRadius * k;

            // ---------- ชั้นล่าง (อยู่ใต้กรอบ) ----------
            FillCircle(circleC, circleR + 1f, dark);
            if (c != null)
            {
                var portrait = GetPortraitTexture(c);
                if (portrait != null)
                {
                    var dest = new Rectangle((int)(circleC.X - circleR), (int)(circleC.Y - circleR), (int)(circleR * 2), (int)(circleR * 2));
                    _spriteBatch.Draw(portrait, dest, Color.White * (alive ? 1f : 0.35f));
                }
            }

            var slot = Map(SkillFrameArt.SkillSlot);
            DrawRect(slot, dark);

            var hpBar = Map(SkillFrameArt.HpBar);
            DrawRect(hpBar, new Color(55, 18, 18));

            float spRatio = 0f;
            bool buffed = alive && c.SwordmanBuffDuration > 0;
            if (alive)
            {
                float hpRatio = c.MaxHp > 0 ? MathHelper.Clamp((float)c.Hp / c.MaxHp, 0f, 1f) : 0f;
                Color hpCol = hpRatio > 0.5f ? new Color(60, 200, 60) : (hpRatio > 0.2f ? Color.Orange : Color.Red);
                DrawRect(new Rectangle(hpBar.X, hpBar.Y, (int)(hpBar.Width * hpRatio), hpBar.Height), hpCol);

                // ช่องสกิล: เติมจากล่างขึ้นบนตามความคืบหน้า SP (เต็มแล้วเป็นสีทองกะพริบ)
                spRatio = c.MaxSP > 0 ? MathHelper.Clamp((float)c.CurrentSP / c.MaxSP, 0f, 1f) : 0f;
                Color spCol = ready ? Color.Lerp(new Color(235, 180, 30), new Color(255, 230, 120), pulse) : new Color(30, 120, 220);
                if (buffed) spCol = HueColor(_totalTime * 0.7f);
                int fillH = (int)(slot.Height * spRatio);
                DrawRect(new Rectangle(slot.X, slot.Bottom - fillH, slot.Width, fillH), spCol * (ready ? 0.85f : 0.65f));

                GetCharacterGlyph(c, out string[] glyph, out Color glyphColor);
                int g = (int)(slot.Width * 0.55f);
                var iconBox = new Rectangle(slot.Center.X - g / 2, slot.Y + (int)(slot.Height * 0.14f), g, g);
                DrawGlyph(glyph, iconBox, ready ? Color.White : glyphColor);

                string label = ready ? "MAX" : $"{(int)(spRatio * 100)}%";
                DrawTextCentered(label, new Vector2(slot.Center.X, slot.Bottom - (int)(slot.Height * 0.13f)), ready ? Color.White : Color.LightGray, 1.0f);
            }

            // ---------- กรอบ ----------
            Color tint = !alive ? new Color(110, 110, 110) : Color.White;
            if (SkillFrameArt.Frame != null)
                _spriteBatch.Draw(SkillFrameArt.Frame, rect, tint);
            else
                DrawRectBorder(rect, Color.Gold, 2);

            // ---------- รูปหลายเหลี่ยมสีรุ้งลอยขึ้นตามขอบกรอบช่องสกิล (บัฟ Alice) ----------
            if (buffed && _auraParticles.TryGetValue(c, out var plist))
            {
                var clip = new Rectangle(slot.X - (int)(14 * k), slot.Y - (int)(14 * k), slot.Width + (int)(28 * k), slot.Height + (int)(28 * k));
                foreach (var p in plist)
                {
                    float life01 = MathHelper.Clamp(p.Age / p.Life, 0f, 1f);
                    float fade = (float)Math.Sin(life01 * Math.PI);
                    Color pc = Color.Lerp(HueColor(p.Hue + p.Age * 0.25f), Color.White, 0.25f) * (0.9f * fade);
                    FillPolygon(new Vector2(rect.X + p.Pos.X * k, rect.Y + p.Pos.Y * k), p.Size * k, AuraPolygonSides, p.Rot, pc, clip);
                }
            }

            // ---------- ชั้นบน (ทับกรอบ) ----------
            if (alive)
            {
                // ไอคอนประเภทการโจมตีในป้ายเพชร: ดาบ = กายภาพ, ดาว = เวทมนตร์
                Vector2 dc = MapP(SkillFrameArt.DiamondCenter);
                int dBox = (int)(22 * k);
                DrawGlyph(c.Type == AttackType.Magic ? GlyphStar : GlyphSword, new Rectangle((int)dc.X - dBox / 2, (int)dc.Y - dBox / 2, dBox, dBox), new Color(125, 78, 44));

                if (buffed)
                {
                    // บัฟ Alice: กรอบวงกลมกับช่องสกิลเรืองสีรุ้งวนเปลี่ยนสี
                    DrawCircleRing(circleC, circleR + 1f, 3, HueColor(_totalTime * 0.7f));
                    DrawRectBorder(slot, HueColor(_totalTime * 0.7f + 0.5f), 3);
                }

                if (ready)
                {
                    DrawCircleRing(circleC, circleR + 1f, 3, Color.Gold * (0.55f + 0.45f * pulse));
                    DrawRectBorder(slot, (hovered ? Color.White : Color.Gold) * (0.6f + 0.4f * pulse), 3);
                }
            }
        }

        private void DrawBattleBackground()
        {
            if (_battleBackground != null)
                _spriteBatch.Draw(_battleBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
        }

        private const int AvatarSize = 110;
        private const int BossRectSize = 220;   // กรอบใหญ่ใบเดียวของห้อง Boss (ตัวบอส + ช่องเป้า + เป้าสีแดง ขยายตามกรอบนี้)
        private const int RowSpacing = 165;
        private const int FormationTop = 330;
        private const int FrontRowCount = 3;
        private const int HpBarGap = 6;
        private const int HpBarHeight = 12;
        private const int AllyFrontX = 650;
        private const int AllyBackX = 430;
        private static int EnemyFrontX => ScreenWidth - AllyFrontX - AvatarSize;
        private static int EnemyBackX => ScreenWidth - AllyBackX - AvatarSize;

        private readonly Dictionary<(Texture2D, Rectangle), int> _frameTopCache = new Dictionary<(Texture2D, Rectangle), int>();

        // แถวพิกเซลแรกที่ไม่โปร่งใสของเฟรมนี้ (นับจากบนสุดของเฟรม)
        private int GetFrameTopOpaque(Texture2D tex, Rectangle src)
        {
            var key = (tex, src);
            if (_frameTopCache.TryGetValue(key, out int cached)) return cached;
            int top = 0;
            try
            {
                var data = new Color[src.Width * src.Height];
                tex.GetData(0, src, data, 0, data.Length);
                top = src.Height;
                for (int y = 0; y < src.Height && top == src.Height; y++)
                    for (int x = 0; x < src.Width; x++)
                        if (data[y * src.Width + x].A > 20) { top = y; break; }
                if (top >= src.Height) top = 0;
            }
            catch { top = 0; }
            _frameTopCache[key] = top;
            return top;
        }

        // ตำแหน่ง Y บนสุดของตัว Elma ตอนเล่นอนิเมชันสกิล (ตามขนาดและเฟรมที่วาดอยู่จริง)
        private bool TryGetElmaUltimateTop(Rectangle avatarRect, out float topY)
        {
            topY = 0f;
            Texture2D tex; int cols, rows, total; float progress;
            if (_elmaUltimateTimer < 0.8f)
            {
                tex = _elmaSkill01Tex; cols = 6; rows = 3; total = 18; progress = _elmaUltimateTimer / 0.8f;
            }
            else
            {
                tex = _elmaSkill02Tex; cols = 4; rows = 2; total = 8; progress = (_elmaUltimateTimer - 0.8f) / (1.5f - 0.8f);
            }
            if (tex == null) return false;

            int fw = tex.Width / cols, fh = tex.Height / rows;
            int frame = Math.Min(total - 1, Math.Max(0, (int)(progress * total)));
            var src = new Rectangle((frame % cols) * fw, (frame / cols) * fh, fw, fh);
            float scale = ((float)avatarRect.Height / fh) * 2.5f;
            float drawTop = avatarRect.Y + avatarRect.Height - fh * scale;
            topY = drawTop + GetFrameTopOpaque(tex, src) * scale;
            return true;
        }

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

            if (index < 0) index = 0;

            // ด่าน Boss มีศัตรูแค่ตัวเดียว ให้วางไว้กึ่งกลางแนวตั้งของพื้นที่ฟอร์เมชันฝั่งศัตรู
            // แทนที่จะใช้ตำแหน่ง front-row ช่องแรก (บนสุด) ตามปกติ ซึ่งทำให้ดูเอียงไปด้านบน
            if (unit.IsEnemy && team.Count == 1)
            {
                // ห้อง Boss: ใช้กรอบเดียวขนาดใหญ่ (ปรับขนาดที่ BossRectSize) กึ่งกลางเดิมของช่องเดี่ยว
                int cx = EnemyFrontX + AvatarSize / 2;
                int cy = FormationTop + RowSpacing + AvatarSize / 2;
                return new Rectangle(cx - BossRectSize / 2, cy - BossRectSize / 2, BossRectSize, BossRectSize);
            }

            bool isFront = index < FrontRowCount;
            int rowSlot = isFront ? index : index - FrontRowCount;
            int y = isFront ? FormationTop + rowSlot * RowSpacing : FormationTop + RowSpacing / 2 + rowSlot * RowSpacing;
            int x = unit.IsEnemy ? (isFront ? EnemyFrontX : EnemyBackX) : (isFront ? AllyFrontX : AllyBackX);
            return new Rectangle(x, y, AvatarSize, AvatarSize);
        }

        private bool IsElma(Character unit)
        {
            if (unit == null) return false;
            if (unit == _elmaCasterRef) return true;
            return !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ---- ปรับขนาด Veyra / Enos ให้ตัวที่เห็นสูงเท่า Elma (ใช้ Elma เป็นหลัก) ----
        private readonly Dictionary<Texture2D, Point> _vBoundsCache = new Dictionary<Texture2D, Point>();

        // ขอบบน/ล่างของพิกเซลที่ไม่โปร่งใสทั้งแผ่นรูป (X = top, Y = bottom แบบ exclusive)
        private bool TryGetVBounds(Texture2D tex, out int top, out int bottom)
        {
            top = 0; bottom = 0;
            if (tex == null) return false;
            if (_vBoundsCache.TryGetValue(tex, out var cached)) { top = cached.X; bottom = cached.Y; return bottom > top; }

            int minY = tex.Height, maxY = -1;
            try
            {
                var data = new Color[tex.Width * tex.Height];
                tex.GetData(data);
                for (int y = 0; y < tex.Height; y++)
                {
                    for (int x = 0; x < tex.Width; x++)
                    {
                        if (data[y * tex.Width + x].A > 20) { if (y < minY) minY = y; if (y > maxY) maxY = y; }
                    }
                }
            }
            catch { }

            var result = maxY >= 0 ? new Point(minY, maxY + 1) : new Point(0, 0);
            _vBoundsCache[tex] = result;
            top = result.X; bottom = result.Y;
            return bottom > top;
        }

        // ตัวคูณขนาดเพิ่มเติมของ Veyra เทียบกับ Elma (1.0 = เท่า Elma) ปรับตัวเลขนี้ได้ตามชอบ
        private const float VeyraSizeFactor = 0.88f;

        private static float ElmaScaleFor(float refHeight) => (refHeight / 320f) * 1.8f;

        // คำนวณสเกลที่ทำให้ตัวละครที่เห็นสูงเท่า Elma พร้อมระยะห่างเท้า/หัวเทียบกับ Elma
        private bool GetMatchedMetrics(Texture2D scaleTex, float refHeight, float sizeFactor, out float scale, out float elmaFootGap, out float elmaTopPad)
        {
            scale = 1f; elmaFootGap = 0f; elmaTopPad = 0f;
            if (!TryGetVBounds(_elmaIdleTex, out int eTop, out int eBottom)) return false;
            if (!TryGetVBounds(scaleTex, out int uTop, out int uBottom)) return false;

            float es = ElmaScaleFor(refHeight);
            scale = ((eBottom - eTop) * es) / (uBottom - uTop) * sizeFactor;
            elmaFootGap = (320f - eBottom) * es;
            elmaTopPad = eTop * es;
            return true;
        }

        // วาดตัวละครแบบปรับขนาดตาม Elma; คืนค่า false ถ้าคำนวณไม่ได้ (ให้ใช้วิธีเดิม)
        private bool DrawMatchedToElma(Texture2D drawTex, Rectangle sourceRect, Texture2D scaleTex, Rectangle avatarRect, float alpha, float sizeFactor = 1f, Character unit = null)
        {
            if (!GetMatchedMetrics(scaleTex, avatarRect.Height, sizeFactor, out float scale, out float footGap, out _)) return false;
            if (!TryGetVBounds(drawTex, out _, out int drawBottom)) return false;

            float drawW = sourceRect.Width * scale;
            float drawH = sourceRect.Height * scale;
            var pos = new Vector2(
                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                (avatarRect.Bottom - footGap) - drawBottom * scale);
            // ผ่าน DrawUnitSprite เพื่อให้ได้แสงเรืองตอนเลือกเป้า + เงาขาวตอนโดนตี เหมือนตัวละครอื่น
            DrawUnitSprite(unit, drawTex, pos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return true;
        }

        // วาดเฟรมจากชีตแบบตาราง (Attack/Skill ของ Enos): ใช้สเกลและตำแหน่งเท้าตามชีต Idle เพื่อให้ตัวไม่กระโดดตอนเปลี่ยนอนิเมชัน
        private bool DrawEnosGridFrame(Character unit, Texture2D tex, Rectangle src, Rectangle avatarRect, float alpha)
        {
            if (!GetMatchedMetrics(_enosIdleTex, avatarRect.Height, 1f, out float scale, out float footGap, out _)) return false;
            if (!TryGetVBounds(_enosIdleTex, out _, out int idleBottom)) return false;
            var pos = new Vector2(
                avatarRect.X + (avatarRect.Width - src.Width * scale) / 2f,
                (avatarRect.Bottom - footGap) - idleBottom * scale);
            DrawUnitSprite(unit, tex, pos, src, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return true;
        }

        // ตำแหน่ง Y ของ "บนหัว" (ขอบบนของเฟรมแบบเดียวกับที่ Elma ใช้วางหลอดเลือด/ลูกศร)
        private bool TryGetMatchedTopY(Texture2D scaleTex, float homeBottom, float homeHeight, float sizeFactor, out float topY)
        {
            topY = 0f;
            if (!GetMatchedMetrics(scaleTex, homeHeight, sizeFactor, out float scale, out float footGap, out float topPad)) return false;
            TryGetVBounds(scaleTex, out int uTop, out int uBottom);
            topY = (homeBottom - footGap) - (uBottom - uTop) * scale - topPad;
            return true;
        }

        private bool IsEnos(Character unit)
        {
            return unit != null && !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("enos", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsVeyra(Character unit)
        {
            return unit != null && !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("veyra", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ขนาดเฟรมของ Veyra (เดาจากสัดส่วนภาพ) ใช้คำนวณสเกล ตำแหน่งหลอดเลือด และหัวลูกศร
        private float VeyraFrameHeight => _veyraIdleTex != null ? _veyraIdleTex.Height : 320f;

        private bool IsLumi(Character unit)
        {
            if (unit == null) return false;
            if (unit == _lumiCasterRef) return true;
            return !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ตำแหน่งตอนพุ่งไปหาเป้า: ใช้ "ขนาดของตัวเอง" (ไม่ใช่ขนาดกรอบของเป้า) เพื่อไม่ให้ตัวละครขยาย/หดตอนไปตีบอส
        // วางชิดเส้นเท้าเดียวกับเป้า และเว้นระยะแนวนอนจากขอบซ้ายของกรอบเป้า
        private static Rectangle AtTargetRect(Rectangle home, Rectangle targetRect, int dx)
        {
            return new Rectangle(targetRect.X + dx, targetRect.Bottom - home.Height, home.Width, home.Height);
        }

        private Rectangle GetAvatarDisplayRect(Character unit)
        {
            if (_zoomDrawOnly && unit == _zoomUnit) return _zoomRect;
            var home = GetAvatarHomeRect(unit);
            bool isElma = IsElma(unit);
            bool isLumi = IsLumi(unit);
            bool isAlice = IsAlice(unit);

            if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaUltimateTarget);
                return AtTargetRect(home, targetRect, -240);
            }
            else if (isLumi && _lumiIsUsingUltimateAnim)
            {
                return GetAvatarHomeRect(unit);
            }
            else if (isElma && _elmaIsAnimatingAttack && _elmaStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }
            else if (isLumi && _lumiIsAnimatingAttack && _lumiStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_lumiStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }
            else if (isAlice && _aliceIsAnimatingAttack && _aliceStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_aliceStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }
            else if (_enosSkillActive && unit == _enosSkillCaster && _enosSkillTarget != null && _enosSkillTarget != unit)
            {
                // Enos พุ่งไปยืนหน้าศัตรูที่เลือก ระหว่างเล่นอนิเมชันสกิล
                var targetRect = GetAvatarHomeRect(_enosSkillTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }
            else if (IsEnos(unit) && _enosIsAnimatingAttack && _enosStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_enosStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }
            else if (IsVeyra(unit))
            {
                // Veyra ยิงจากตำแหน่งเดิม ไม่วาร์ปไปยืนหน้าเป้าหมาย
                return home;
            }
            else if (_battle.ActiveAttacker == unit && _battle.ActiveTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_battle.ActiveTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return AtTargetRect(home, targetRect, offsetX);
            }

            return home;
        }

        private Texture2D GetEnemyTexture(Character unit)
        {
            string name = unit.Name?.ToLower() ?? "";
            Texture2D tex = null;
            // Boss Slime King ใช้อนิเมชัน Stone Golem (ย้อมดำ) / Boss Slime Angel ใช้อนิเมชัน Giant Hornet (ย้อมขาว)
            if (name.Contains("angel")) tex = _giantHornetIdleTex ?? _bossAngelTex;
            else if (name.Contains("king")) tex = _stoneGolemTex ?? _bossKingTex;
            else if (name.Contains("orc") || name.Contains("orc_warrior")) tex = _orcWarriorTex;
            else if (name.Contains("bee") || name.Contains("hornet")) tex = _giantHornetIdleTex;
            else if (name.Contains("wolf")) tex = _wolfTex;
            else if (name.Contains("ghost") || name.Contains("ghost_healer")) tex = _ghostHealerTex;
            else if (name.Contains("stone") || name.Contains("golem")) tex = _stoneGolemTex;

            return tex ?? _testEnemyTex;
        }

        // 1 = เพิ่งโดนตี -> 0 = จบเอฟเฟกต์
        private float GetHitProgress(Character unit)
            => unit != null && _hitTimers.TryGetValue(unit, out var t) ? MathHelper.Clamp(t / HitDuration, 0f, 1f) : 0f;

        // ถอยหลังออกจากศัตรู: ฝั่งเราอยู่ซ้ายจึงถอยไปซ้าย ฝั่งศัตรูอยู่ขวาจึงถอยไปขวา (กระแทกทันทีแล้วค่อยๆ กลับ)
        private float GetHitRecoilX(Character unit)
        {
            float p = GetHitProgress(unit);
            return (unit.IsEnemy ? 1f : -1f) * HitRecoilDistance * p * p;
        }

        private Vector2 GetHitBarShake(Character unit)
        {
            float p = GetHitProgress(unit);
            if (p <= 0f) return Vector2.Zero;
            float phase = (1f - p) * 70f;
            return new Vector2((float)Math.Sin(phase) * HitBarShake * p, (float)Math.Cos(phase * 1.3f) * HitBarShake * 0.5f * p);
        }

        // เงาสีขาวของสไปรต์ (คงรูปร่างจาก alpha เดิม) ใช้ทำเอฟเฟกต์กะพริบขาว
        private Texture2D GetWhiteMask(Texture2D tex)
        {
            if (tex == null) return null;
            if (_whiteMasks.TryGetValue(tex, out var cached)) return cached;

            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                byte a = data[i].A;
                data[i] = new Color(a, a, a, a); // สีขาวแบบ premultiplied alpha
            }
            var mask = new Texture2D(tex.GraphicsDevice, tex.Width, tex.Height);
            mask.SetData(data);
            _whiteMasks[tex] = mask;
            return mask;
        }

        // วาดสไปรต์ตัวละคร และถ้ากำลังถูกโจมตีให้วาดเงาขาวทับอีกชั้น
        // ตัวนี้กำลังเป็น "เป้าหมายที่เลือกได้" อยู่หรือไม่ (ใช้ทั้งแสงรอบตัวและเป้าแดง)
        private bool GetTargetHighlight(Character unit, out Color color, out float strength)
        {
            color = Color.Transparent;
            strength = 0f;
            if (_battle == null || unit == null || !unit.IsAlive) return false;

            float pulse = 0.5f + 0.5f * (float)Math.Sin(_targetGlowTimer * 6f);

            if (_isWaitingForUltimateTarget && _battle.UltimateCaster != null)
            {
                var caster = _battle.UltimateCaster;
                bool isVeyra = caster.Name?.IndexOf("veyra", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isSelf = IsSelfTargetCaster(caster);
                bool selectable = isSelf ? unit == caster : (isVeyra ? !unit.IsEnemy : unit.IsEnemy);
                if (!selectable) return false;

                bool hovered = unit == _hoveredEnemyTarget || (isVeyra && GetAvatarHomeRect(unit).Contains(VirtualMouse.GetState().Position));
                color = (isVeyra || isSelf) ? new Color(60, 140, 255) : new Color(235, 40, 40);
                strength = hovered ? 1f : (0.45f + 0.25f * pulse);
                return true;
            }

            if (_battle.PendingAttacker != null && unit == _hoveredEnemyTarget)
            {
                color = new Color(235, 40, 40);
                strength = 1f;
                return true;
            }
            return false;
        }

        // ศัตรูสี (Elite): ย้อมสไปรต์ให้ออกโทนสีของตัวเอง
        private static Color TintForVariant(Character unit, Color color)
        {
            if (unit != null && unit.IsEnemy && !string.IsNullOrEmpty(unit.Name))
            {
                // Boss Slime King = ดำ / Boss Slime Angel = ขาว (ผสมเข้าหาขาวเพื่อให้สว่างจริง)
                if (unit.Name.IndexOf("king", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new Color((int)(color.R * 0.18f), (int)(color.G * 0.18f), (int)(color.B * 0.20f), color.A);
                if (unit.Name.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new Color((int)(color.R + (255 - color.R) * 0.8f), (int)(color.G + (255 - color.G) * 0.8f), (int)(color.B + (255 - color.B) * 0.8f), color.A);
            }
            if (unit == null || unit.Variant == EnemyVariant.None) return color;
            Color t = unit.Variant == EnemyVariant.Red ? new Color(255, 140, 130)
                    : unit.Variant == EnemyVariant.Green ? new Color(150, 255, 150)
                    : unit.Variant == EnemyVariant.Blue ? new Color(140, 185, 255)
                    : new Color(215, 150, 255);
            return new Color((int)(color.R * t.R / 255f), (int)(color.G * t.G / 255f), (int)(color.B * t.B / 255f), color.A);
        }

        private void DrawUnitSprite(Character unit, Texture2D tex, Vector2 pos, Rectangle? src, Color color, float rot, Vector2 origin, float scale, SpriteEffects fx, float depth)
        {
            // แสงเรืองรอบตัวเป้าหมายที่กำลังถูกเลือก (วาดเงาสีซ้อนหลายชั้นรอบตัว)
            if (GetTargetHighlight(unit, out Color glowCol, out float glowStr))
            {
                var glowMask = GetWhiteMask(tex);
                if (glowMask != null)
                {
                    float[] radii = { 9f, 6f, 3f };
                    float[] alphas = { 0.14f, 0.28f, 0.55f };
                    for (int ring = 0; ring < radii.Length; ring++)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            float ang = k * MathHelper.PiOver4;
                            var off = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radii[ring];
                            _spriteBatch.Draw(glowMask, pos + off, src, glowCol * (alphas[ring] * glowStr * (color.A / 255f)), rot, origin, scale, fx, depth);
                        }
                    }
                }
            }

            _spriteBatch.Draw(tex, pos, src, TintForVariant(unit, color), rot, origin, scale, fx, depth);

            float flash = MathHelper.Clamp(GetHitProgress(unit) * 1.4f, 0f, 1f);
            if (flash <= 0f) return;
            var mask = GetWhiteMask(tex);
            if (mask != null)
                _spriteBatch.Draw(mask, pos, src, color * flash, rot, origin, scale, fx, depth);
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
                avatarRect.X += (int)GetHitRecoilX(unit);
                bool isElma = IsElma(unit);
                bool isLumi = IsLumi(unit);
                bool isAlice = IsAlice(unit);
                bool isVeyraUnit = IsVeyra(unit) && _veyraIdleTex != null;
                bool isEnosUnit = IsEnos(unit) && _enosIdleTex != null;

                if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
                {
                    float customX = avatarRect.X;
                    float customY = avatarRect.Y;

                    if (_elmaUltimateTimer < 0.8f)
                    {
                        if (_elmaSkill01Tex != null)
                        {
                            int totalFrames = 18;
                            int cols = 6;
                            int rows = 3;

                            int fw = _elmaSkill01Tex.Width / cols;
                            int fh = _elmaSkill01Tex.Height / rows;

                            float subProgress = _elmaUltimateTimer / 0.8f;
                            int frameIndex = Math.Min(totalFrames - 1, (int)(subProgress * totalFrames));

                            int col = frameIndex % cols;
                            int row = frameIndex / cols;

                            Rectangle srcRect = new Rectangle(col * fw, row * fh, fw, fh);

                            float scale = ((float)avatarRect.Height / fh) * 2.5f;
                            float drawW = fw * scale;
                            float drawH = fh * scale;
                            Vector2 drawPos = new Vector2(
                                customX + (avatarRect.Width - drawW) / 2f,
                                customY + avatarRect.Height - drawH
                            );

                            DrawUnitSprite(unit, _elmaSkill01Tex, drawPos, srcRect, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        if (_elmaSkill02Tex != null)
                        {
                            int totalFrames = 8;
                            int cols = 4;
                            int rows = 2;

                            int fw = _elmaSkill02Tex.Width / cols;
                            int fh = _elmaSkill02Tex.Height / rows;

                            float subProgress = (_elmaUltimateTimer - 0.8f) / (1.5f - 0.8f);
                            int frameIndex = Math.Min(totalFrames - 1, (int)(subProgress * totalFrames));

                            int col = frameIndex % cols;
                            int row = frameIndex / cols;

                            Rectangle srcRect = new Rectangle(col * fw, row * fh, fw, fh);

                            float scale = ((float)avatarRect.Height / fh) * 2.5f;
                            float drawW = fw * scale;
                            float drawH = fh * scale;
                            Vector2 drawPos = new Vector2(
                                customX + (avatarRect.Width - drawW) / 2f,
                                customY + avatarRect.Height - drawH
                            );

                            DrawUnitSprite(unit, _elmaSkill02Tex, drawPos, srcRect, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                    }
                }
                else if (isElma)
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

                            DrawUnitSprite(unit, _elmaAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
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

                        DrawUnitSprite(unit, _elmaIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isLumi)
                {
                    float baseScale = ((float)avatarRect.Height / 320f) * 1.8f;

                    if (_lumiSkillCasting && unit == _lumiCasterRef && _lumiSkillTex != null)
                    {
                        int fs = _lumiSkillTex.Height;
                        int sf = Math.Min((int)(_lumiSkillTimer / LumiSkillFrameTime), LumiSkillFrameCount - 1);
                        var skillSrc = new Rectangle(sf * fs, 0, fs, fs);
                        float sScale = ((float)avatarRect.Height / fs) * 1.8f;
                        float sW = fs * sScale;
                        float sH = fs * sScale;
                        var sPos = new Vector2(avatarRect.X + (avatarRect.Width - sW) / 2f, avatarRect.Bottom - sH);
                        DrawUnitSprite(unit, _lumiSkillTex, sPos, skillSrc, Color.White * alpha, 0f, Vector2.Zero, sScale, SpriteEffects.None, 0f);
                    }
                    else if (_lumiIsAnimatingAttack)
                    {
                        if (_lumiAttackTex != null)
                        {
                            int frameWidth = 320;
                            int frameHeight = 320;
                            int totalFrames = Math.Max(1, _lumiAttackTex.Width / frameWidth);
                            int currentFrame = Math.Min(_lumiCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            DrawUnitSprite(unit, _lumiAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (_lumiIdleTex != null)
                    {
                        int frameWidth = 240;
                        int frameHeight = 320;
                        int totalFrames = 16;
                        int currentFrame = _lumiFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        DrawUnitSprite(unit, _lumiIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isAlice)
                {
                    float baseScale = ((float)avatarRect.Height / 480f) * 2.2f;

                    // มี Aura อยู่ -> ใช้ชีต _mode แทนชีตเดิม (ไม่มีไฟล์ _mode ก็ใช้ชีตเดิม + ออร่ารุ้งเหมือนเดิม)
                    bool aliceModeAttack = unit.HasAura && _aliceAttackModeTex != null;
                    bool aliceModeIdle = unit.HasAura && _aliceIdleModeTex != null;
                    Texture2D aliceAttackDraw = aliceModeAttack ? _aliceAttackModeTex : _aliceAttackTex;
                    Texture2D aliceIdleDraw = aliceModeIdle ? _aliceIdleModeTex : _aliceIdleTex;

                    if (_aliceIsAnimatingAttack)
                    {
                        if (aliceAttackDraw != null)
                        {
                            int frameWidth = 480;
                            int frameHeight = 480;
                            int totalFrames = 6;
                            int currentFrame = Math.Min(_aliceCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            if (unit.HasAura && !aliceModeAttack) DrawRainbowAura(aliceAttackDraw, sourceRect, drawPos, baseScale, alpha);
                            DrawUnitSprite(unit, aliceAttackDraw, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (aliceIdleDraw != null)
                    {
                        int frameWidth = 480;
                        int frameHeight = 480;
                        int totalFrames = 4;
                        int currentFrame = _aliceFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        if (unit.HasAura && !aliceModeIdle) DrawRainbowAura(aliceIdleDraw, sourceRect, drawPos, baseScale, alpha);
                        DrawUnitSprite(unit, aliceIdleDraw, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isVeyraUnit)
                {
                    bool useVeyraSkill = _veyraSkillActive && unit == _veyraSkillCaster && _veyraSkillTex != null;
                    bool useVeyraAttack = !useVeyraSkill && _veyraIsAnimatingAttack && _veyraAttackTex != null;
                    Texture2D veyraTex = useVeyraSkill ? _veyraSkillTex : (useVeyraAttack ? _veyraAttackTex : _veyraIdleTex);
                    Rectangle sourceRect = useVeyraSkill
                        ? VeyraSkillFrameRect((int)(_veyraSkillTimer / VeyraSkillFrameTime))
                        : useVeyraAttack
                            ? SpriteFrames.VeyraAttackFrame(_veyraAttackTex, _veyraCustomFrameIndex)
                            : SpriteFrames.VeyraFrame(_veyraIdleTex, _aliceFrameIndex);
                    if (!DrawMatchedToElma(veyraTex, sourceRect, _veyraIdleTex, avatarRect, alpha, VeyraSizeFactor, unit))
                    {
                        float baseScale = ((float)avatarRect.Height / sourceRect.Height) * 1.4f;
                        float drawW = sourceRect.Width * baseScale;
                        float drawH = sourceRect.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        DrawUnitSprite(unit, veyraTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isEnosUnit)
                {
                    Texture2D enosTex = _enosIdleTex;
                    Rectangle sourceRect;
                    if (_enosSkillActive && unit == _enosSkillCaster && _enosSkillTex != null)
                    {
                        enosTex = _enosSkillTex;
                        int sf = Math.Min((int)(_enosSkillTimer / EnosSkillFrameTime), SpriteFrames.GridFrameCount(_enosSkillTex) - 1);
                        sourceRect = SpriteFrames.GridFrame(_enosSkillTex, sf);
                    }
                    else if (_enosIsAnimatingAttack && _enosAttackTex != null)
                    {
                        enosTex = _enosAttackTex;
                        sourceRect = SpriteFrames.GridFrame(_enosAttackTex, _enosCustomFrameIndex);
                    }
                    else sourceRect = SpriteFrames.EnosFrame(_enosIdleTex, _aliceFrameIndex);

                    bool enosDrawn = enosTex != _enosIdleTex
                        ? DrawEnosGridFrame(unit, enosTex, sourceRect, avatarRect, alpha)
                        : DrawMatchedToElma(enosTex, sourceRect, _enosIdleTex, avatarRect, alpha, 1f, unit);
                    if (!enosDrawn)
                    {
                        float baseScale = ((float)avatarRect.Height / sourceRect.Height) * 1.4f;
                        float drawW = sourceRect.Width * baseScale;
                        float drawH = sourceRect.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        DrawUnitSprite(unit, enosTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (unit.IsEnemy)
                {
                    bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                        (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (IsGolem(unit) && _stoneGolemTex != null)
                    {
                        bool golemAtk = _battle.ActiveAttacker == unit && _stoneGolemAttackTex != null;
                        Texture2D golemTex = golemAtk ? _stoneGolemAttackTex : _stoneGolemTex;
                        int golemAtkCount = golemAtk ? GolemAttackFrameCount() : 0;
                        Rectangle golemSrc = golemAtk
                            ? GolemAttackFrameRect((int)(_battle.AttackAnimTimer / _battle.CurrentAttackDuration * golemAtkCount))
                            : GolemFrameRect(_stoneGolemTex, _golemFrameIndex);
                        float baseScale = ((float)avatarRect.Height / GolemFrameSize) * GolemScaleMul;
                        if (golemAtk) baseScale *= GolemAttackScaleMul;
                        float drawW = golemSrc.Width * baseScale;
                        float drawH = golemSrc.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );
                        DrawUnitSprite(unit, golemTex, drawPos, golemSrc, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                    else if (IsWolf(unit) && _wolfTex != null)
                    {
                        GetWolfDrawInfo(unit, out Texture2D wolfTex, out Rectangle wolfSrc);
                        if (wolfTex != null)
                        {
                            float baseScale = ((float)avatarRect.Height / wolfSrc.Height) * WolfScaleMul;
                            float drawW = wolfSrc.Width * baseScale;
                            float drawH = wolfSrc.Height * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );
                            DrawUnitSprite(unit, wolfTex, drawPos, wolfSrc, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (isGiantHornet)
                    {
                        bool isHornetAttacking = (_battle.ActiveAttacker == unit && _giantHornetAttackTex != null);
                        Texture2D hornetTex = isHornetAttacking ? _giantHornetAttackTex : _giantHornetIdleTex;

                        if (hornetTex != null)
                        {
                            int frameWidth = 192;
                            int frameHeight = 192;
                            int totalFrames = isHornetAttacking ? 10 : 7;
                            int cols = isHornetAttacking ? 4 : Math.Max(1, hornetTex.Width / frameWidth);
                            int currentFrame = isHornetAttacking
                                ? Math.Min(totalFrames - 1, (int)(_battle.AttackAnimTimer / BattleManager.AttackAnimDuration * totalFrames))
                                : _hornetFrameIndex % totalFrames;

                            int col = currentFrame % cols;
                            int row = currentFrame / cols;

                            Rectangle sourceRect = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                            float baseScale = ((float)avatarRect.Height / frameHeight) * 0.65f * EnemySizeBoost;
                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            DrawUnitSprite(unit, hornetTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        Texture2D enemyTex = GetEnemyTexture(unit);
                        if (enemyTex != null)
                        {
                            float baseScale = ((float)avatarRect.Height / enemyTex.Height) * 1.4f * EnemySizeBoost;
                            float drawW = enemyTex.Width * baseScale;
                            float drawH = enemyTex.Height * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            DrawUnitSprite(unit, enemyTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.FlipHorizontally, 0f);
                        }
                    }
                }
                else
                {
                    if (_testCharacterTex != null)
                    {
                        float baseScale = ((float)avatarRect.Height / _testCharacterTex.Height) * 1.4f;
                        float drawW = _testCharacterTex.Width * baseScale;
                        float drawH = _testCharacterTex.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        DrawUnitSprite(unit, _testCharacterTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }

                // --- แสดงผลเอฟเฟกต์ Boom_Fx (ขนาด 160x160 มี 8 เฟรม) บน Enemy ทุกตัว เมื่อโปรเจกต์ไทล์พุ่งถึงเป้าหมาย ---
                if (_lumiIsUsingUltimateAnim && _boomFxTex != null && unit.IsEnemy && unit.IsAlive)
                {
                    float totalDuration = 0.8f;
                    float progress = MathHelper.Clamp(_lumiUltimateTimer / totalDuration, 0f, 1f);
                    int boomFrameIndex = (int)(progress * 8f);
                    if (boomFrameIndex > 7) boomFrameIndex = 7;

                    int fw = 160;
                    int fh = 160;
                    int cols = Math.Max(1, _boomFxTex.Width / fw);
                    int col = boomFrameIndex % cols;
                    int row = boomFrameIndex / cols;

                    Rectangle boomSrc = new Rectangle(col * fw, row * fh, fw, fh);
                    float boomScale = ((float)avatarRect.Height / fh) * 1.5f;
                    float boomW = fw * boomScale;
                    float boomH = fh * boomScale;
                    Vector2 finalPos = new Vector2(
                        avatarRect.X + (avatarRect.Width - boomW) / 2f,
                        avatarRect.Bottom - boomH
                    );

                    _spriteBatch.Draw(_boomFxTex, finalPos, boomSrc, Color.White * alpha, 0f, Vector2.Zero, boomScale, SpriteEffects.None, 0f);
                }

                // --- ป้องกัน Dead Effect วาดทับซ้อนขณะที่สกิล Lumi กำลังเล่นอนิเมชันอยู่ ---
                if ((!unit.IsAlive || unit.Hp <= 0) && _deadEffectTex != null && !_lumiIsUsingUltimateAnim && !_lumiIsProjectileMoving)
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

                if (_zoomDrawOnly) continue; // โหมดซูม: วาดแค่ตัวสไปรต์ ไม่วาดหลอดเลือด

                var home = GetAvatarHomeRect(unit);
                float hpBarY;
                int hpBarX = home.X;

                if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
                {
                    hpBarX = avatarRect.X - (int)GetHitRecoilX(unit); // avatarRect ถูกขยับถอยหลังไปแล้ว กันบวกซ้ำ
                    hpBarY = avatarRect.Y - HpBarGap - HpBarHeight;
                    // สกิลของ Elma วาดตัวใหญ่ 2.5 เท่า (ยกแขนสูง): วางหลอดเลือดไว้เหนือจุดสูงสุดของภาพเฟรมปัจจุบัน ไม่ให้ทับตัว
                    if (TryGetElmaUltimateTop(avatarRect, out float skillTopY))
                        hpBarY = Math.Max(4f, skillTopY - HpBarGap - HpBarHeight);
                }
                else if (isElma || isLumi)
                {
                    float baseScale = ((float)home.Height / 320f) * 1.8f;
                    float drawH = 320f * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (isAlice)
                {
                    float baseScale = ((float)home.Height / 480f) * 1.4f;
                    float drawH = 480f * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (isVeyraUnit || isEnosUnit)
                {
                    if (TryGetMatchedTopY(isEnosUnit ? _enosIdleTex : _veyraIdleTex, home.Bottom, home.Height, isEnosUnit ? 1f : VeyraSizeFactor, out float matchedTop))
                    {
                        hpBarY = matchedTop - HpBarGap - HpBarHeight;
                    }
                    else
                    {
                        float frameH = isEnosUnit ? 480f : 320f;
                        float baseScale = ((float)home.Height / frameH) * 1.4f;
                        float drawH = frameH * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                }
                else if (!unit.IsEnemy && _testCharacterTex != null)
                {
                    float baseScale = ((float)home.Height / _testCharacterTex.Height) * 1.4f;
                    float drawH = _testCharacterTex.Height * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (unit.IsEnemy)
                {
                    bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                        (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (IsGolem(unit) && _stoneGolemTex != null)
                    {
                        float baseScale = ((float)home.Height / GolemFrameSize) * GolemScaleMul;
                        hpBarY = (home.Bottom - GolemFrameSize * baseScale) - HpBarGap - HpBarHeight;
                    }
                    else if (IsWolf(unit) && _wolfTex != null)
                    {
                        int wfs = WolfFrameSize(_wolfTex);
                        float baseScale = ((float)home.Height / wfs) * WolfScaleMul;
                        float drawH = wfs * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                    else if (isGiantHornet)
                    {
                        float baseScale = ((float)home.Height / 192f) * 0.65f * EnemySizeBoost;
                        float drawH = 192f * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                    else
                    {
                        var enemyTex = GetEnemyTexture(unit);
                        float baseScale = ((float)home.Height / enemyTex.Height) * 1.4f * EnemySizeBoost;
                        float drawH = enemyTex.Height * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                }
                else
                {
                    hpBarY = home.Y - HpBarGap - HpBarHeight;
                }

                var barShake = GetHitBarShake(unit);
                var hpBg = new Rectangle(hpBarX + (int)(GetHitRecoilX(unit) + barShake.X), (int)(hpBarY + barShake.Y), home.Width, HpBarHeight);
                DrawRect(hpBg, new Color(60, 20, 20) * alpha);

                float hpRatio = unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0f;
                DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * hpRatio), hpBg.Height), (hpRatio > 0.5f ? Color.LimeGreen : hpRatio > 0.2f ? Color.Orange : Color.Red) * alpha);
                DrawRectBorder(hpBg, Color.Black * alpha, 1);

                DrawStatusIcons(unit, hpBg, alpha);
                if (unit.GateMax > 0 && unit.IsAlive) DrawGates(unit, hpBg, alpha);
            }
        }

        // Gate ของบอส: แถบเล็กใต้หลอดเลือด แบ่ง 3 ช่องด้วยเส้นเฉียง เติมสีม่วงไล่เฉดทีละช่อง ครบแล้วเรืองแสงกะพริบ
        private void DrawGates(Character unit, Rectangle hpBar, float alpha)
        {
            int n = Math.Max(1, unit.GateMax);
            int w = (int)(hpBar.Width * 0.62f);
            const int h = 11;
            var bar = new Rectangle(hpBar.X + 2, hpBar.Bottom + 5, w, h);
            const float slant = 0.55f; // เส้นแบ่งเอียงไปทางขวาด้านบน
            bool ready = unit.GateReady;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(_totalTime * 8f);

            // แสงเรืองรอบแถบตอนพร้อมปล่อย
            if (ready) DrawRect(new Rectangle(bar.X - 4, bar.Y - 4, bar.Width + 8, bar.Height + 8), new Color(255, 120, 255) * (0.18f + 0.22f * pulse) * alpha);

            // กรอบนอก (ดำ) + พื้นช่องว่าง (ลาเวนเดอร์อ่อนไล่เฉด)
            DrawRect(new Rectangle(bar.X - 2, bar.Y - 2, bar.Width + 4, bar.Height + 4), Color.Black * alpha);
            for (int row = 0; row < h; row++)
            {
                float k = row / (float)(h - 1);
                DrawRect(new Rectangle(bar.X, bar.Y + row, bar.Width, 1), Color.Lerp(new Color(246, 240, 252), new Color(190, 180, 210), k) * alpha);
            }

            // ช่องที่เติมแล้ว: ม่วงไล่เฉดบนสว่าง-ล่างเข้ม ขอบขวาเป็นเส้นเฉียง
            // สีช่อง Gate ตามสีศัตรู: Red = ส้มแดง, Green = เขียว, อื่นๆ (Purple / บอส) = ม่วง
            Color gTop = new Color(205, 115, 225), gTopHi = new Color(255, 170, 255), gBot = new Color(115, 40, 145), gBotHi = new Color(170, 70, 200);
            if (unit.Variant == EnemyVariant.Red) { gTop = new Color(255, 130, 60); gTopHi = new Color(255, 200, 110); gBot = new Color(160, 40, 20); gBotHi = new Color(220, 80, 40); }
            else if (unit.Variant == EnemyVariant.Green) { gTop = new Color(130, 220, 90); gTopHi = new Color(200, 255, 150); gBot = new Color(30, 110, 40); gBotHi = new Color(70, 170, 70); }
            if (unit.Variant == EnemyVariant.Blue) { gTop = new Color(110, 170, 255); gTopHi = new Color(190, 225, 255); gBot = new Color(30, 70, 160); gBotHi = new Color(70, 120, 220); }
            Color top = ready ? Color.Lerp(gTop, gTopHi, pulse) : gTop;
            Color bottom = ready ? Color.Lerp(gBot, gBotHi, pulse) : gBot;
            for (int row = 0; row < h; row++)
            {
                float k = row / (float)(h - 1);
                Color c = Color.Lerp(top, bottom, k) * alpha;
                float shift = (h - 1 - row) * slant;
                for (int i = 0; i < unit.GateCount && i < n; i++)
                {
                    int x0 = i == 0 ? bar.X : (int)(bar.X + bar.Width * i / (float)n + shift);
                    int x1 = i == n - 1 ? bar.Right : (int)(bar.X + bar.Width * (i + 1) / (float)n + shift);
                    x1 = Math.Min(x1, bar.Right);
                    if (x1 > x0) DrawRect(new Rectangle(x0, bar.Y + row, x1 - x0, 1), c);
                }
            }
            // เส้นสะท้อนแสงด้านบน
            for (int i = 0; i < unit.GateCount && i < n; i++)
            {
                float shift = (h - 1) * slant;
                int x0 = i == 0 ? bar.X : (int)(bar.X + bar.Width * i / (float)n + shift);
                int x1 = i == n - 1 ? bar.Right : (int)(bar.X + bar.Width * (i + 1) / (float)n + shift);
                x1 = Math.Min(x1, bar.Right);
                if (x1 - x0 > 4) DrawRect(new Rectangle(x0 + 1, bar.Y + 1, x1 - x0 - 2, 1), Color.White * (0.55f * alpha));
            }

            // เส้นแบ่งเฉียงสีดำ
            for (int i = 1; i < n; i++)
            {
                for (int row = 0; row < h; row++)
                {
                    int x = (int)(bar.X + bar.Width * i / (float)n + (h - 1 - row) * slant);
                    if (x < bar.Right - 1) DrawRect(new Rectangle(x, bar.Y + row, 2, 1), Color.Black * alpha);
                }
            }

            // ขอบในสว่างบางๆ ให้ดูมีมิติ
            DrawRectBorder(bar, new Color(255, 255, 255) * (0.28f * alpha), 1);
        }

        // ไอคอนสถานะ (Buff/Debuff) ใต้หลอดเลือด พร้อมตัวเลข Stack / เทิร์นที่เหลือ
        private void DrawStatusIcons(Character unit, Rectangle hpBar, float alpha)
        {
            if (!unit.IsAlive) return;
            var list = StatusIcons.Active(unit);
            if (list.Count == 0) return;

            const int size = 28;
            const int gap = 3;
            int x = hpBar.X;
            int y = hpBar.Y - size - 3; // อยู่เหนือหลอดเลือด
            foreach (var st in list)
            {
                var r = new Rectangle(x, y, size, size);
                if (StatusIcons.Draw(_spriteBatch, st.Type, r, alpha) && _font != null)
                {
                    const float s = 0.6f;
                    // เลขเทิร์นอยู่มุมซ้ายล่าง / Stack อยู่มุมขวาล่าง
                    if (st.Turns > 0)
                    {
                        string t = st.Turns.ToString();
                        Vector2 sz = _font.MeasureString(t) * s;
                        Vector2 p = new Vector2(r.X + 1, r.Bottom - sz.Y - 1);
                        _spriteBatch.DrawString(_font, t, p + new Vector2(1, 1), Color.Black * alpha, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                        _spriteBatch.DrawString(_font, t, p, Color.White * alpha, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                    }
                    if (st.Stacks > 0)
                    {
                        string t = st.Stacks.ToString();
                        Vector2 sz = _font.MeasureString(t) * s;
                        Vector2 p = new Vector2(r.Right - sz.X - 1, r.Bottom - sz.Y - 1);
                        _spriteBatch.DrawString(_font, t, p + new Vector2(1, 1), Color.Black * alpha, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                        _spriteBatch.DrawString(_font, t, p, Color.Gold * alpha, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                    }
                }
                x += size + gap;
            }
        }

        private Vector2 GetUnitArrowTipPosition(Character unit)
        {
            var home = GetAvatarHomeRect(unit);
            float topY = home.Y;
            bool isElma = IsElma(unit);
            bool isLumi = IsLumi(unit);
            bool isAlice = IsAlice(unit);

            if (isElma || isLumi)
            {
                float baseScale = ((float)home.Height / 320f) * 1.8f;
                topY = home.Bottom - (320f * baseScale);
            }
            else if (isAlice)
            {
                float baseScale = ((float)home.Height / 480f) * 1.4f;
                topY = home.Bottom - (480f * baseScale);
            }
            else if ((IsVeyra(unit) && _veyraIdleTex != null) || (IsEnos(unit) && _enosIdleTex != null))
            {
                if (!TryGetMatchedTopY(IsEnos(unit) ? _enosIdleTex : _veyraIdleTex, home.Bottom, home.Height, IsEnos(unit) ? 1f : VeyraSizeFactor, out topY))
                {
                    float frameH = IsEnos(unit) ? 480f : VeyraFrameHeight;
                    float baseScale = ((float)home.Height / frameH) * 1.4f;
                    topY = home.Bottom - (frameH * baseScale);
                }
            }
            else if (!unit.IsEnemy && _testCharacterTex != null)
            {
                float baseScale = ((float)home.Height / _testCharacterTex.Height) * 1.4f;
                topY = home.Bottom - (_testCharacterTex.Height * baseScale);
            }
            else if (unit.IsEnemy)
            {
                bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                    (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0);

                if (IsGolem(unit) && _stoneGolemTex != null)
                {
                    float baseScale = ((float)home.Height / GolemFrameSize) * GolemScaleMul;
                    topY = home.Bottom - (GolemFrameSize * baseScale);
                }
                else if (IsWolf(unit) && _wolfTex != null)
                {
                    int wfs = WolfFrameSize(_wolfTex);
                    float baseScale = ((float)home.Height / wfs) * WolfScaleMul;
                    topY = home.Bottom - (wfs * baseScale);
                }
                else if (isGiantHornet)
                {
                    float baseScale = ((float)home.Height / 192f) * 0.65f * EnemySizeBoost;
                    topY = home.Bottom - (192f * baseScale);
                }
                else
                {
                    Texture2D enemyTex = GetEnemyTexture(unit);
                    if (enemyTex != null)
                    {
                        float baseScale = ((float)home.Height / enemyTex.Height) * 1.4f * EnemySizeBoost;
                        topY = home.Bottom - (enemyTex.Height * baseScale);
                    }
                }
            }

            float hpBarY = topY - HpBarGap - HpBarHeight;
            return new Vector2(home.X + home.Width / 2f, hpBarY - 15f);
        }

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts)
            {
                float life = ft.Duration > 0f ? ft.Duration : FloatingTextDuration;
                float age = life - ft.Timer;
                float t = MathHelper.Clamp(age / life, 0f, 1f);

                // เด้งขยายตอนเกิด แล้วหดลงเป็นขนาดปกติ
                float pop = 1f;
                if (age < FloatingTextPopTime)
                {
                    float p = 1f - age / FloatingTextPopTime;
                    pop = 1f + (FloatingTextPopScale - 1f) * p * p;
                }

                // ค้างชัดๆ 60% แรก แล้วค่อยจางหายใน 40% ที่เหลือ
                float alpha = t < 0.6f ? 1f : MathHelper.Clamp(1f - (t - 0.6f) / 0.4f, 0f, 1f);

                Color outline = ft.Outline == default(Color) ? new Color(30, 0, 0) : ft.Outline;
                DrawOutlinedTextCentered(ft.Text, ft.Position, ft.Color * alpha, outline * alpha, ft.Scale * pop);
            }
        }

        private void DrawOutlinedTextCentered(string text, Vector2 center, Color fill, Color outline, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            Vector2 size;
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeTest(text); size = _font.MeasureString(text); }

            float o = Math.Max(2f, scale * 1.4f); // ความหนาขอบ
            Vector2[] dirs =
            {
                new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1),
                new Vector2(-0.7f, -0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, 0.7f)
            };
            try
            {
                foreach (var d in dirs)
                    _spriteBatch.DrawString(_font, text, center + d * o, outline, 0f, size / 2f, scale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, text, center, fill, 0f, size / 2f, scale, SpriteEffects.None, 0f);
            }
            catch (ArgumentException) { }
        }

        private void DrawTurnArrows()
        {
            Character activeUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;
            if (activeUnit != null)
                DrawDownArrow(GetUnitArrowTipPosition(activeUnit), 16, Color.Yellow);
            // เลือกเป้าโจมตีปกติ: ใช้เป้าสีแดงแทนลูกศร (ตอนเลือกเป้าสกิลจะวาดใน DrawUltimateTargetGlow แล้ว)
            if (_hoveredEnemyTarget != null && !_isWaitingForUltimateTarget)
                DrawTargetCrosshair(GetAvatarHomeRect(_hoveredEnemyTarget), true, new Color(235, 40, 40));
        }

        private void DrawDownArrow(Vector2 tip, float size, Color color)
        {
            DrawLine(tip + new Vector2(-size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(0, -size * 2.2f), tip + new Vector2(0, -size), color, 5);
        }

        // ปุ่มเมนูอยู่มุมขวาบน ส่วนปุ่ม II / S / A เลื่อนลงมาเรียงต่อกันด้านล่างเมื่อเปิดเมนู
        private Rectangle GetUiToggleButtonRect() => new Rectangle(ScreenWidth - 100, 30, 70, 70);

        private Rectangle GetMenuSlotRect(int slot)
        {
            float k = _buttonMenuAnim;
            k = 1f - (1f - k) * (1f - k);
            int y = 30 + (int)(slot * 90 * k);
            return new Rectangle(ScreenWidth - 100, y, 70, 70);
        }

        private Rectangle GetPauseButtonRect() => GetMenuSlotRect(1);
        private Rectangle GetBattleSettingsButtonRect() => GetMenuSlotRect(2);
        // ปุ่ม Auto อยู่ข้างปุ่มเมนูตลอดเวลา
        private Rectangle GetAutoToggleButtonRect() => new Rectangle(ScreenWidth - 190, 30, 70, 70);

        // ปุ่มกลมของ HUD: พื้นเข้มโปร่งแสง + วงแหวน + ไอคอนจากรูป ButtonIcons
        private void DrawHudButton(Rectangle rect, int icon, Color col, string fallbackText)
        {
            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            float radius = rect.Width / 2f;
            FillCircle(center + new Vector2(2, 3), radius - 1, Color.Black * 0.3f);
            FillCircle(center, radius - 1, new Color(15, 20, 35) * 0.75f);

            for (float angle = 0; angle < MathHelper.TwoPi; angle += 0.03f)
            {
                int bx = (int)(center.X + (radius - 2) * (float)Math.Cos(angle));
                int by = (int)(center.Y + (radius - 2) * (float)Math.Sin(angle));
                _spriteBatch.Draw(_pixel, new Rectangle(bx, by, 3, 3), col);
            }

            var box = new Rectangle((int)center.X - 22, (int)center.Y - 22, 44, 44);
            if (!ButtonIcons.Draw(_spriteBatch, icon, box, col))
                DrawTextCentered(fallbackText, center, col, 1.6f);
        }

        // ปุ่มเมนู (กดแล้วปุ่ม II / S เลื่อนลงมา)
        private void DrawUiToggleButton()
        {
            var rect = GetUiToggleButtonRect();
            DrawHudButton(rect, ButtonIcons.Menu, _hoveredUiToggle ? Color.DeepSkyBlue : (_buttonMenuOpen ? Color.Gold : Color.White), "=");
        }

        // ข้อความเปิดด่านวิ่งผ่านกลางจอ
        private void DrawIntroBanner()
        {
            if (_introTimer >= IntroDuration) return;

            float t = _introTimer;
            float cx = ScreenWidth / 2f;
            float offX;
            float bandAlpha;
            if (t < IntroSlideTime)
            {
                float k = t / IntroSlideTime;
                float e = 1f - (1f - k) * (1f - k);
                offX = (1f - e) * (ScreenWidth * 0.75f);
                bandAlpha = k;
            }
            else if (t < IntroSlideTime + IntroHoldTime)
            {
                offX = 0f;
                bandAlpha = 1f;
            }
            else
            {
                float k = (t - IntroSlideTime - IntroHoldTime) / IntroSlideTime;
                offX = -(k * k) * (ScreenWidth * 0.75f);
                bandAlpha = 1f - k;
            }

            float y = ScreenHeight / 2f - 120f;
            var band = new Rectangle(0, (int)y - 70, ScreenWidth, 140);
            DrawRect(band, Color.Black * (0.55f * bandAlpha));
            DrawRect(new Rectangle(0, band.Y, ScreenWidth, 3), Color.Gold * bandAlpha);
            DrawRect(new Rectangle(0, band.Bottom - 3, ScreenWidth, 3), Color.Gold * bandAlpha);

            string difficultyStr = _currentStageIsBoss ? "Boss" : (_currentStageIsHard ? "Hard" : "Normal");
            string title = $"Floor {_storyStageIndex + 1} - {difficultyStr}";
            Color titleCol = _currentStageIsBoss ? new Color(255, 90, 90) : (_currentStageIsHard ? new Color(255, 140, 90) : Color.Gold);
            DrawTextCentered(title, new Vector2(cx + offX + 3, y - 15 + 4), Color.Black * 0.7f, 3.0f);
            DrawTextCentered(title, new Vector2(cx + offX, y - 15), titleCol, 3.0f);
            DrawTextCentered("BATTLE START!", new Vector2(cx + offX, y + 40), Color.White, 1.6f);
        }

        private void DrawPauseButton()
        {
            DrawHudButton(GetPauseButtonRect(), ButtonIcons.Pause, _hoveredPauseButton ? Color.DeepSkyBlue : Color.White, "||");
        }

        private void DrawAutoToggleButton()
        {
            Color col = Color.White;
            if (_hoveredAutoToggle) col = Color.DeepSkyBlue;
            else if (!_battle.ManualMode) col = Color.Gold;
            DrawHudButton(GetAutoToggleButtonRect(), ButtonIcons.Auto, col, "A");
        }

        private void DrawBattleSettingsButton()
        {
            DrawHudButton(GetBattleSettingsButtonRect(), ButtonIcons.Settings, _hoveredBattleSettings ? Color.DeepSkyBlue : Color.White, "S");
        }

        // ---------- ตัวช่วยตกแต่งหน้า Victory ----------
        private void DrawUiGradient(Rectangle r, Color top, Color bottom)
        {
            const int step = 4;
            for (int y = 0; y < r.Height; y += step)
            {
                float t = r.Height <= 1 ? 0f : (float)y / (r.Height - 1);
                _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y + y, r.Width, Math.Min(step, r.Height - y)), Color.Lerp(top, bottom, t));
            }
        }

        private void DrawUiButton(Rectangle r, Color baseColor, bool hovered, int border = 2)
        {
            UiKit.StoneButton(_spriteBatch, _pixel, r, baseColor, hovered, border);
        }

        private void DrawUiCoin(Vector2 c, float radius)
        {
            FillCircle(c, radius, new Color(150, 105, 10));
            FillCircle(c, radius - 2, new Color(255, 205, 50));
            FillCircle(c, radius - 5, new Color(235, 170, 25));
            DrawRect(new Rectangle((int)(c.X - 2), (int)(c.Y - radius * 0.45f), 4, (int)(radius * 0.9f)), new Color(255, 235, 140));
        }

        private void DrawUiWrapped(string text, Vector2 center, float maxWidth, Color color, float scale, float lineHeight)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            var words = text.Split(' ');
            var lines = new List<string>();
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

        private void UpdateChest(float delta, bool leftClicked, ref int coins)
        {
            if (_chestPhase == 0)
            {
                _chestReward = ChestSystem.Open(ChestRarity.Value, _rng);
                _chestPhase = 1;
                _chestTimer = 0f;
                return;
            }

            _chestTimer += delta;
            switch (_chestPhase)
            {
                case 1:
                    if (leftClicked && _chestTimer > 0.3f) { _playUIClick(); _chestPhase = 2; _chestTimer = 0f; }
                    break;
                case 2:
                    if (_chestTimer >= ChestOpenDuration)
                    {
                        coins += _chestReward.Coins;
                        _chestPhase = 3;
                        _chestTimer = 0f;
                    }
                    break;
                case 3:
                    if (leftClicked && _chestTimer > 0.4f)
                    {
                        _playUIClick();
                        if (_chestReward.Item != null)
                        {
                            _inventory.Add(_chestReward.Item);
                            _chestPhase = 4;
                            _chestTimer = 0f;
                        }
                        else _chestPhase = 5;
                    }
                    break;
                case 4:
                    if (leftClicked && _chestTimer > 0.4f) { _playUIClick(); _chestPhase = 5; }
                    break;
            }
        }

        // สีที่อ่านออกบนพื้นมืด (Secret เป็นสีดำจึงใช้เทาอ่อนแทน)
        private Color GetVisibleRarityColor(ItemRarity r)
        {
            if (r == ItemRarity.Secret) return new Color(170, 170, 195);
            return GetRarityColor(r);
        }

        private void DrawChestOverlay()
        {
            ItemRarity rar = ChestRarity.Value;
            Color accent = GetVisibleRarityColor(rar);
            float vt = _totalTime;
            DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 225));

            float cx = ScreenWidth / 2f, cy = ScreenHeight / 2f - 20f;

            if (_chestPhase == 1 || _chestPhase == 2)
            {
                float t = _chestPhase == 2 ? MathHelper.Clamp(_chestTimer / ChestOpenDuration, 0f, 1f) : 0f;
                float size = 380f;
                float scale = size / 128f;

                // แสงเรืองด้านหลัง
                float glow = 0.16f + 0.08f * (float)Math.Sin(vt * 3f) + t * 0.25f;
                for (int i = 0; i < 4; i++)
                {
                    float r = size * (0.55f + i * 0.14f);
                    FillCircle(new Vector2(cx, cy), r, accent * (glow / (1f + i)));
                }

                float shakeX = 0f, bobY = 0f;
                float lidAngle = 0f;
                if (_chestPhase == 1) bobY = (float)Math.Sin(vt * 2.5f) * 8f;
                else
                {
                    if (t < 0.5f) shakeX = (float)Math.Sin(_chestTimer * 70f) * 12f * (t / 0.5f);
                    else
                    {
                        float k = (t - 0.5f) / 0.5f;
                        lidAngle = -MathHelper.SmoothStep(0f, 1f, k) * 0.95f;
                    }
                }

                float left = cx - size / 2f + shakeX;
                float top = cy - size / 2f + bobY;
                float mid = top + size / 2f;

                // ลำแสงจากในกล่อง
                if (lidAngle < 0f)
                {
                    float k = -lidAngle / 0.95f;
                    var beam = new Rectangle((int)(left + size * 0.2f), (int)(mid - 700f * k), (int)(size * 0.6f), (int)(700f * k));
                    DrawUiGradient(beam, Color.Transparent, accent * (0.55f * k));
                    for (int sp = 0; sp < 16; sp++)
                    {
                        float ph = (vt * 1.4f + sp * 0.173f) % 1f;
                        float sx = left + size * (0.25f + (sp * 37 % 50) / 100f);
                        float sy = mid - ph * 380f * k;
                        DrawRect(new Rectangle((int)sx, (int)sy, 8, 8), HueColor(sp * 0.09f) * (float)Math.Sin(ph * Math.PI) * k);
                    }
                }

                if (ChestIcons.Sheet != null)
                {
                    // ตัวกล่อง (ครึ่งล่าง) + ฝา (ครึ่งบน) หมุนรอบบานพับมุมซ้ายล่างของฝา
                    _spriteBatch.Draw(ChestIcons.Sheet, new Rectangle((int)left, (int)mid, (int)size, (int)(size / 2f)), ChestIcons.Source(rar, 64, 64), Color.White);
                    _spriteBatch.Draw(ChestIcons.Sheet, new Vector2(left + 14f * scale, mid), ChestIcons.Source(rar, 0, 64), Color.White, lidAngle, new Vector2(14f, 64f), scale, SpriteEffects.None, 0f);
                }

                // แฟลชขาวตอนเปิดเต็มที่
                if (t > 0.8f) DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White * ((t - 0.8f) / 0.2f * 0.8f));

                if (_chestPhase == 1)
                {
                    float blink = 0.55f + 0.45f * (float)Math.Sin(vt * 4f);
                    DrawTextCentered($"{rar.ToString().ToUpper()} CHEST", new Vector2(cx, cy - size / 2f - 70f), accent, 2.2f);
                    DrawTextCentered("Click anywhere to open chest", new Vector2(cx, ScreenHeight - 130f), Color.White * blink, 1.5f);
                }
            }
            else if (_chestPhase == 3)
            {
                float p = MathHelper.Clamp(_chestTimer / 0.55f, 0f, 1f);
                float pop = 1f + 1.2f * (float)Math.Pow(p - 1f, 3) + 0.2f * (float)Math.Pow(p - 1f, 2); // ease-out-back
                pop = Math.Max(0f, pop);
                float bounce = (float)Math.Abs(Math.Sin(p * Math.PI * 2f)) * (1f - p) * 90f;
                FillCircle(new Vector2(cx, cy), 230f * pop, Color.Gold * 0.12f);
                DrawUiCoin(new Vector2(cx, cy - bounce), 95f * pop);
                DrawTextCentered($"+{_chestReward.Coins} G", new Vector2(cx, cy + 170f), Color.Gold * p, 3f);
                if (_chestTimer > 0.4f)
                    DrawTextCentered("Click anywhere to continue", new Vector2(cx, ScreenHeight - 130f), Color.White * (0.55f + 0.45f * (float)Math.Sin(vt * 4f)), 1.5f);
            }
            else if (_chestPhase == 4 && _chestReward.Item != null)
            {
                var item = _chestReward.Item;
                Color rc = GetVisibleRarityColor(item.Rarity);
                float p = MathHelper.Clamp(_chestTimer / 0.45f, 0f, 1f);
                float rise = (1f - p) * (1f - p) * 80f;

                for (int i = 0; i < 4; i++) FillCircle(new Vector2(cx, cy), 300f * (0.6f + i * 0.2f), rc * (0.07f * p / (1f + i)));

                var card = new Rectangle((int)(cx - 250), (int)(cy - 300 + rise), 500, 560);
                float a = p;
                DrawRect(new Rectangle(card.X + 8, card.Y + 10, card.Width, card.Height), Color.Black * (0.45f * a));
                DrawUiGradient(card, new Color(36, 36, 52) * a, new Color(14, 14, 24) * a);
                DrawRectBorder(card, rc * a, 4);
                DrawRectBorder(new Rectangle(card.X + 10, card.Y + 10, card.Width - 20, card.Height - 20), rc * (0.25f * a), 1);

                DrawTextCentered("You got", new Vector2(card.Center.X, card.Y + 40), Color.White * a, 1.4f);
                var plate = new Rectangle(card.Center.X - 110, card.Y + 78, 220, 220);
                DrawRect(plate, new Color(10, 10, 18) * a);
                DrawRectBorder(plate, rc * a, 3);
                ItemIcons.Draw(_spriteBatch, item, new Rectangle(plate.X + 14, plate.Y + 14, 192, 192), a);

                DrawTextCentered(item.Name, new Vector2(card.Center.X, card.Y + 335), rc * a, 1.7f);
                var badge = new Rectangle(card.Center.X - 90, card.Y + 365, 180, 34);
                DrawRect(badge, rc * (0.18f * a));
                DrawRectBorder(badge, rc * (0.8f * a), 1);
                DrawTextCentered(item.Rarity.ToString().ToUpper(), new Vector2(badge.Center.X, badge.Center.Y), rc * a, 1f);
                DrawUiWrapped(item.Description, new Vector2(card.Center.X, card.Y + 440), card.Width - 70, Color.White * a, 1f, 32f);

                if (_chestTimer > 0.4f)
                    DrawTextCentered("Click anywhere to continue", new Vector2(cx, ScreenHeight - 80f), Color.White * (0.55f + 0.45f * (float)Math.Sin(vt * 4f)), 1.5f);
            }
        }

        private void DrawResultBanner()
        {
            if (_battle.State == BattleState.Running) return;

            if (_battle.State == BattleState.AllyWin && !_isStoryMode && ChestRarity.HasValue && _chestPhase >= 1 && _chestPhase < 5)
            {
                DrawChestOverlay();
                return;
            }

            if (_battle.State == BattleState.AllyWin && _isStoryMode)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Gold, 3);
                DrawTextCentered($"Stage 1-{_storyStageIndex + 1} Cleared!", new Vector2(rect.Center.X, rect.Center.Y), Color.Gold, 2f);

                var btnContinue = GetStoryWinContinueRect();
                DrawUiButton(btnContinue, new Color(55, 135, 55), _hoveredStoryWinContinue);
                DrawTextCentered("Continue", new Vector2(btnContinue.Center.X, btnContinue.Center.Y), Color.White, 1.1f);

                var btnMenu = GetStoryWinMenuRect();
                DrawUiButton(btnMenu, new Color(170, 55, 55), _hoveredStoryWinMenu);
                DrawTextCentered("Main Menu", new Vector2(btnMenu.Center.X, btnMenu.Center.Y), Color.White, 1.1f);
            }
            else if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                if (_currentItemChoices != null)
                {
                    DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 205));
                    DrawUiGradient(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(40, 30, 70) * 0.35f, Color.Transparent);

                    // ริบบิ้นหัวเรื่อง + ประกายลอย
                    float vt = _totalTime;
                    var ribbon = new Rectangle(ScreenWidth / 2 - 420, 50, 840, 120);
                    DrawUiGradient(ribbon, Color.Transparent, new Color(120, 90, 20) * 0.45f);
                    DrawRect(new Rectangle(ribbon.X, ribbon.Y + 4, ribbon.Width, 3), Color.Gold * 0.7f);
                    DrawRect(new Rectangle(ribbon.X, ribbon.Bottom - 3, ribbon.Width, 3), Color.Gold * 0.7f);
                    for (int sp = 0; sp < 14; sp++)
                    {
                        float phase = (vt * 0.35f + sp * 0.137f) % 1f;
                        float sx = ribbon.X + 30 + (sp * 173 % 780);
                        float sy = ribbon.Bottom - phase * 130f;
                        float sa = (float)Math.Sin(phase * Math.PI);
                        DrawRect(new Rectangle((int)sx, (int)sy, 8, 8), HueColor(sp * 0.07f + vt * 0.2f) * (0.8f * sa));
                    }
                    DrawTextCentered("Victory!", new Vector2(ScreenWidth / 2f + 3, 103 + 4), Color.Black * 0.7f, 3.2f);
                    DrawTextCentered("Victory!", new Vector2(ScreenWidth / 2f, 103), Color.Lerp(Color.Gold, Color.White, 0.25f + 0.25f * (float)Math.Sin(vt * 3f)), 3.2f);
                    DrawTextCentered("Choose an Item", new Vector2(ScreenWidth / 2f, 160), Color.White, 1.5f);

                    // สรุปรางวัลที่ได้จากด่านนี้
                    int luckyCount = 0;
                    foreach (var pc in _party) { if (pc.HasEquipped("Lucky Coin")) luckyCount++; }
                    int shownCoins = (int)Math.Round(_battle.RewardCoins * (1f + 0.25f * luckyCount));
                    var rewardBox = new Rectangle(ScreenWidth / 2 - 260, 196, 520, 44);
                    DrawRect(rewardBox, new Color(10, 10, 18, 220));
                    DrawRectBorder(rewardBox, Color.Gold * 0.6f, 2);
                    DrawTextCentered($"+{_battle.RewardPoints} Points", new Vector2(rewardBox.X + 130, rewardBox.Center.Y), Color.LimeGreen, 1.1f);
                    DrawUiCoin(new Vector2(rewardBox.X + 292, rewardBox.Center.Y), 11f);
                    DrawTextCentered($"+{shownCoins} G", new Vector2(rewardBox.X + 395, rewardBox.Center.Y), Color.Gold, 1.1f);

                    for (int i = 0; i < _currentItemChoices.Count; i++)
                    {
                        var baseRect = GetItemCardRect(i);
                        var item = _currentItemChoices[i];
                        bool hovered = _hoveredItemCard == i;
                        Color rarityColor = GetRarityColor(item.Rarity);
                        var rect = hovered ? new Rectangle(baseRect.X, baseRect.Y - 8, baseRect.Width, baseRect.Height) : baseRect;

                        Color baseCardBg = item.Rarity switch
                        {
                            ItemRarity.Common => new Color(42, 42, 62),
                            ItemRarity.Uncommon => new Color(28, 66, 42),
                            ItemRarity.Rare => new Color(28, 50, 82),
                            ItemRarity.Epic => new Color(60, 38, 84),
                            ItemRarity.Legendary => new Color(84, 64, 26),
                            ItemRarity.Mythic => new Color(88, 28, 28),
                            ItemRarity.Secret => new Color(64, 64, 78),
                            ItemRarity.Unique => new Color(70, 50, 90),
                            _ => new Color(42, 42, 62)
                        };

                        if (hovered) DrawRect(new Rectangle(rect.X - 8, rect.Y - 8, rect.Width + 16, rect.Height + 16), rarityColor * (0.12f + 0.07f * (float)Math.Sin(vt * 5f)));
                        DrawRect(new Rectangle(rect.X + 6, rect.Y + 8, rect.Width, rect.Height), Color.Black * 0.4f);
                        DrawUiGradient(rect, Color.Lerp(baseCardBg, Color.White, hovered ? 0.2f : 0.06f), new Color(14, 14, 24));
                        DrawUiGradient(new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, 16), rarityColor * 0.5f, Color.Transparent);
                        DrawRectBorder(rect, hovered ? Color.Gold : rarityColor, hovered ? 4 : 3);
                        DrawRectBorder(new Rectangle(rect.X + 9, rect.Y + 9, rect.Width - 18, rect.Height - 18), rarityColor * 0.22f, 1);

                        var plate = new Rectangle(rect.Center.X - 76, rect.Y + 34, 152, 152);
                        DrawRect(plate, new Color(10, 10, 18));
                        DrawRectBorder(plate, rarityColor * 0.9f, 2);
                        ItemIcons.Draw(_spriteBatch, item, new Rectangle(plate.X + 16, plate.Y + 16, 120, 120));

                        DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 218), rarityColor, 1.3f);

                        var badge = new Rectangle(rect.Center.X - 70, rect.Y + 245, 140, 28);
                        DrawRect(badge, rarityColor * 0.18f);
                        DrawRectBorder(badge, rarityColor * 0.8f, 1);
                        DrawTextCentered(item.Rarity.ToString().ToUpper(), new Vector2(badge.Center.X, badge.Center.Y), rarityColor, 0.8f);

                        DrawUiWrapped(item.Description, new Vector2(rect.Center.X, rect.Y + 306), rect.Width - 60, Color.White, 0.9f, 28f);

                        var pill = new Rectangle(rect.Center.X - 70, rect.Bottom - 66, 140, 40);
                        DrawRect(pill, new Color(10, 24, 12));
                        DrawRectBorder(pill, Color.LimeGreen * 0.8f, 2);
                        DrawTextCentered("FREE", new Vector2(pill.Center.X, pill.Center.Y), Color.LimeGreen, 1.2f);
                    }
                }
            }
            else if (_battle.State == BattleState.EnemyWin)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Red, 3);
                DrawTextCentered("You Lose!", new Vector2(rect.Center.X, rect.Center.Y), Color.Red, 2f);

                var btnRestart = GetLoseRestartRect();
                DrawUiButton(btnRestart, new Color(55, 135, 55), _hoveredLoseRestart);
                DrawTextCentered("Restart", new Vector2(btnRestart.Center.X, btnRestart.Center.Y), Color.White, 1.1f);

                var btnMenu = GetLoseMenuRect();
                DrawUiButton(btnMenu, new Color(170, 55, 55), _hoveredLoseMenu);
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
            ItemRarity.Secret => Color.Black,
            ItemRarity.Unique => Where_My_Slime.Data.RarityColors.Rainbow(),
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

        private void DrawFloorIndicator()
        {
            int currentFloor = _storyStageIndex + 1;
            string difficultyStr = _currentStageIsBoss ? "Boss" : (_currentStageIsHard ? "Hard" : "Normal");
            string floorText = $"Floor {currentFloor} {difficultyStr}";
            Vector2 position = new Vector2(ScreenWidth / 2f, 75f);
            DrawTextCentered(floorText, position, Color.White, 2.2f);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
        }

        private string GetSafeTest(string text)
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
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeTest(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}