using System;
using System.Collections.Generic;
using System.Linq;
using Where_My_Slime.Data;
using Where_My_Slime.Models;

namespace Where_My_Slime.Battle
{
    public enum BattleState
    {
        Running,
        AllyWin,
        EnemyWin
    }

    public class BattleManager
    {
        public List<Character> Allies { get; private set; }
        public List<Character> Enemies { get; private set; }
        public List<string> Log { get; private set; } = new List<string>();
        public BattleState State { get; private set; } = BattleState.Running;

        public bool IsHardStage { get; private set; }
        public bool IsBossStage { get; private set; }
        public int RewardPoints { get; private set; }
        public int RewardCoins { get; private set; }

        private readonly Random _rng = new Random();
        private const int MaxLogLines = 8;
        private readonly Action _onShakeCamera;

        public Character ActiveAttacker { get; private set; }
        public Character ActiveTarget { get; private set; }
        public float AttackAnimTimer { get; private set; }
        public const float AttackAnimDuration = 0.4f;

        // ให้หน้าจอบอกจำนวนเฟรมแอนิเมชันโจมตีของตัวนั้น เพื่อคำนวณเวลาที่ดาเมจเข้า (เฟรมที่ 2)
        public Func<Character, int> AttackFrameCount;
        // ระยะเวลาอนิเมชันโจมตีเฉพาะตัว (คืนค่า <= 0 = ใช้ค่ามาตรฐาน AttackAnimDuration)
        public Func<Character, float> AttackDurationOverride;
        public float CurrentAttackDuration
        {
            get
            {
                float d = (ActiveAttacker != null && AttackDurationOverride != null) ? AttackDurationOverride(ActiveAttacker) : 0f;
                return d > 0f ? d : AttackAnimDuration;
            }
        }
        private bool _attackResolved;

        public bool ManualMode { get; private set; } = true;
        public Character PendingAttacker { get; private set; }

        public event Action<Character, AttackResult> OnAttackResolved;

        public delegate void HealResolvedEventHandler(Character target, int amount);
        public event HealResolvedEventHandler OnHealResolved;
        // ดาเมจจากสถานะ (พิษ) / ข้ามเทิร์นจาก Stun ให้ UI แสดงตัวเลขหรือข้อความ
        public event Action<Character, int> OnPoisonDamage;
        public event Action<Character> OnStunSkipped;
        public event Action<Character, int> OnFireDamage;
        // ข้อความเมื่อติดสถานะใหม่ (เช่น "Fire 2", "Melt", "Poison", "Weakness") ให้ UI แสดง
        public event Action<Character, string> OnStatusInflicted;
        public event Action<Character, int> OnBlessingApplied;

        public bool IsUsingUltimate = false;
        public Character UltimateCaster = null;

        // เพิ่มตัวแปรสำหรับจับเวลาอนิเมชัน Ultimate ให้เล่นจนจบก่อนคำนวณผล
        public float UltimateAnimTimer { get; private set; }
        public const float UltimateAnimDuration = 1.1f;

        public BattleManager(List<Character> allyParty, bool isHardStage = false, Action onShakeCamera = null, int floorsCleared = 0, bool isBossStage = false)
        {
            Allies = allyParty;
            foreach (var ally in Allies)
            {
                ally.Hp = ally.MaxHp;
                ally.InitializeSP();
            }

            IsHardStage = isHardStage;
            IsBossStage = isBossStage;

            // รีวอร์ด: Boss ให้คงที่ ไม่สนใจ Hard/Normal เพราะ Boss เป็นหมวดของตัวเอง
            RewardPoints = isBossStage ? 10 : (isHardStage ? 5 : 3);
            RewardCoins = isBossStage ? 125 : (isHardStage ? 20 : 10);
            _onShakeCamera = onShakeCamera;

            Enemies = CreateEnemies(isHardStage, floorsCleared, isBossStage);
        }

        public static List<Character> CreateStartingParty()
        {
            return CharacterIndex.CreateStartingParty();
        }

        private List<Character> CreateEnemies(bool isHardStage, int floorsCleared, bool isBossStage)
        {
            List<Character> enemies = isBossStage
                ? new List<Character> { CharacterIndex.CreateEnemy(_rng.Next(2) == 0 ? "boss_slime_king" : "boss_slime_angel") } // บอสสุ่มระหว่าง King กับ Angel
                : CharacterIndex.CreateEnemyWave();

            if (isHardStage)
            {
                foreach (var e in enemies)
                {
                    e.ModifyBaseStat(StatKind.MaxHp, 50f, isPercent: true);
                    e.ModifyBaseStat(StatKind.AllAtk, 10f, isPercent: true);
                    e.Hp = e.MaxHp;
                }
            }

            // Challenge Mode: ศัตรูแข็งแกร่งขึ้นทบต้นตามจำนวนชั้นที่ผ่านมาแล้ว HP x1.2, ATK x1.08, Speed x1.05 และ Def x1.04 ต่อชั้น
            if (floorsCleared > 0)
            {
                float hpPercentIncrease = ((float)Math.Pow(1.2, floorsCleared) - 1f) * 100f;
                float atkPercentIncrease = ((float)Math.Pow(1.08, floorsCleared) - 1f) * 100f;
                float speedPercentIncrease = ((float)Math.Pow(1.05, floorsCleared) - 1f) * 100f;
                float defPercentIncrease = ((float)Math.Pow(1.04, floorsCleared) - 1f) * 100f;
                foreach (var e in enemies)
                {
                    e.ModifyBaseStat(StatKind.MaxHp, hpPercentIncrease, isPercent: true);
                    e.ModifyBaseStat(StatKind.AllAtk, atkPercentIncrease, isPercent: true);
                    e.ModifyBaseStat(StatKind.Speed, speedPercentIncrease, isPercent: true);
                    e.ModifyBaseStat(StatKind.PDef, defPercentIncrease, isPercent: true);
                    e.ModifyBaseStat(StatKind.MDef, defPercentIncrease, isPercent: true);
                    e.Hp = e.MaxHp;
                }
            }

            return enemies;
        }

        public IEnumerable<Character> AllUnits => Allies.Concat(Enemies);

        // ---------- ระบบเทิร์นแบบวนรอบ ----------
        // TurnNumber = เลข Turn ปัจจุบัน (เพิ่มขึ้น 1 เมื่อทุกตัวเดินครบ)
        public int TurnNumber { get; private set; } = 1;

        private readonly List<Character> _roundQueue = new List<Character>();
        private bool _roundStarted;

        private List<Character> SortedBySpeed()
        {
            // OrderByDescending เป็น stable: Speed เท่ากันให้ฝั่งเราก่อน
            return AllUnits.Where(u => u != null && u.IsAlive).OrderByDescending(u => u.CurrentSpeed).ToList();
        }

        private void BuildRoundQueue()
        {
            _roundQueue.Clear();
            _roundQueue.AddRange(SortedBySpeed());
            _roundStarted = true;
        }

        // จบรอบ = ครบ 1 Turn: เพิ่มเลข Turn และลดตัวนับยั่วยุ (Taunt) ของศัตรู
        private void EndRound()
        {
            TurnNumber++;
            foreach (var e in Enemies)
                if (e.TauntDuration > 0) e.TauntDuration--;
            TickStatuses();
            _roundStarted = false;
        }

        // ทุกจบ 1 Turn: พิษทำดาเมจ, นับถอยหลังสถานะ, Blessing ลด 1 Stack ทุก 4 เทิร์น
        private void TickStatuses()
        {
            foreach (var u in AllUnits.ToList())
            {
                if (u == null || !u.IsAlive) continue;

                if (u.PoisonTurns > 0)
                {
                    int dmg = Math.Max(1, (int)(u.MaxHp * Character.PoisonHpRate));
                    u.Hp = Math.Max(0, u.Hp - dmg);
                    u.PoisonTurns--;
                    AddLog($"{u.Name} takes {dmg} poison damage");
                    OnPoisonDamage?.Invoke(u, dmg);
                    if (!u.IsAlive) { AddLog($"{u.Name} has been defeated!"); continue; }
                }

                if (u.FireStacks > 0)
                {
                    int fdmg = u.FireStacks * Character.FireDamagePerStack;
                    u.Hp = Math.Max(0, u.Hp - fdmg);
                    AddLog($"{u.Name} burns for {fdmg} damage");
                    OnFireDamage?.Invoke(u, fdmg);
                    if (--u.FireTurns <= 0) { u.FireTurns = 0; u.FireStacks = 0; }
                    if (!u.IsAlive) { AddLog($"{u.Name} has been defeated!"); continue; }
                }

                if (u.MeltTurns > 0 && --u.MeltTurns <= 0) { u.MeltTurns = 0; u.RecalculateStats(); }

                if (u.AuraTurns > 0) u.AuraTurns--;
                if (u.AggroTurns > 0) u.AggroTurns--;
                if (u.LifestealTurns > 0) u.LifestealTurns--;
                if (u.WeaknessTurns > 0) u.WeaknessTurns--;

                if (u.BlessingStacks > 0 && ++u.BlessingTimer >= Character.BlessingDecayRounds)
                {
                    u.BlessingTimer = 0;
                    u.RemoveBlessing(1);
                }
            }
        }

        // ลำดับที่จะเดินต่อจากนี้ (ไม่รวมตัวที่กำลังเดินอยู่) ใช้แสดงแถบ TURN
        // ต่อท้ายด้วยลำดับของรอบถัดไป (เรียงตาม Speed ปัจจุบัน)
        public List<Character> PredictUpcoming(int max)
        {
            var result = new List<Character>();
            foreach (var u in _roundQueue)
            {
                if (u != null && u.IsAlive && result.Count < max) result.Add(u);
            }

            var nextRound = SortedBySpeed();
            while (result.Count < max && nextRound.Count > 0)
            {
                foreach (var u in nextRound)
                {
                    if (result.Count >= max) break;
                    result.Add(u);
                }
            }
            return result;
        }

        public void Update(float deltaSeconds)
        {
            if (State != BattleState.Running) return;

            // จัดการเวลาอนิเมชัน Ultimate ให้เล่นจนครบกำหนดเวลาก่อนค่อยประมวลผล
            if (IsUsingUltimate)
            {
                UltimateAnimTimer += deltaSeconds;
                if (UltimateAnimTimer >= UltimateAnimDuration)
                {
                    ResolveUltimate();
                    IsUsingUltimate = false;
                    UltimateCaster = null;
                    UltimateAnimTimer = 0f;
                }
                return;
            }

            if (ActiveAttacker != null)
            {
                float currentAnimDuration = CurrentAttackDuration;

                string attackerName = ActiveAttacker.Name ?? "";
                bool isHornet = attackerName.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                attackerName.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                attackerName.IndexOf("angel", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isHornet)
                {
                    currentAnimDuration = AttackAnimDuration;
                }

                AttackAnimTimer += deltaSeconds;

                // ดาเมจเข้าตอนเริ่มเฟรมที่ 2 (ถ้าไม่รู้จำนวนเฟรม ให้เข้าตอนจบแอนิเมชัน)
                int frames = AttackFrameCount != null ? AttackFrameCount(ActiveAttacker) : 0;
                float hitTime = frames >= 2 ? currentAnimDuration / frames : currentAnimDuration;
                if (!_attackResolved && AttackAnimTimer >= hitTime)
                {
                    _attackResolved = true;
                    ResolveAttack();
                }

                if (AttackAnimTimer >= currentAnimDuration)
                {
                    _attackResolved = false;
                    ActiveAttacker = null;
                    ActiveTarget = null;
                    AttackAnimTimer = 0f;
                }
                return;
            }

            if (PendingAttacker != null) return;

            // ระบบเทิร์นแบบวนรอบ: ทุกตัวที่ยังมีชีวิตได้เดินคนละครั้งตามลำดับ Speed มาก -> น้อย
            // เมื่อทุกตัวเดินครบแล้วนับเป็น 1 Turn แล้วเริ่มรอบใหม่ (เรียงใหม่ตาม Speed ณ ตอนนั้น)
            Character readyUnit = null;
            while (readyUnit == null)
            {
                if (_roundQueue.Count == 0)
                {
                    if (_roundStarted) EndRound();
                    BuildRoundQueue();
                    if (_roundQueue.Count == 0) break;
                }

                var next = _roundQueue[0];
                _roundQueue.RemoveAt(0);
                if (next != null && next.IsAlive)
                {
                    // Stun: ข้ามเทิร์นของตัวนี้ไปตัวถัดไป
                    if (next.StunTurns > 0)
                    {
                        next.StunTurns = 0; // ถึงเทิร์นของตัวที่ติด Stun: ข้ามเทิร์นนี้แล้วสถานะหายไปทันที (ไม่ค้างหลายเทิร์น)
                        AddLog($"{next.Name} is stunned and skips the turn");
                        OnStunSkipped?.Invoke(next);
                        continue;
                    }
                    readyUnit = next;
                }
            }

            if (readyUnit != null)
            {
                if (!readyUnit.IsEnemy && ManualMode)
                    PendingAttacker = readyUnit;
                else
                    StartAttack(readyUnit);
            }

            CheckBattleEnd();
        }

        public void SetManualMode(bool manual)
        {
            ManualMode = manual;
            if (!manual && PendingAttacker != null)
            {
                var attacker = PendingAttacker;
                PendingAttacker = null;
                StartAttack(attacker);
            }
        }

        public bool TrySelectTarget(Character target)
        {
            if (PendingAttacker == null) return false;
            if (target == null || !target.IsAlive || !target.IsEnemy) return false;

            ActiveAttacker = PendingAttacker;
            ActiveTarget = target;
            AttackAnimTimer = 0f;
            PendingAttacker = null;
            return true;
        }

        private void StartAttack(Character attacker)
        {
            var targets = attacker.IsEnemy ? Allies : Enemies;
            var aliveTargets = targets.Where(t => t.IsAlive).ToList();
            if (aliveTargets.Count == 0) return;

            Character target = null;

            // Aggro: ศัตรูต้องเลือกตีตัวที่มี Aggro ก่อน (ถ้ามีหลายตัวสุ่มในกลุ่มนั้น)
            if (attacker.IsEnemy)
            {
                var aggroTargets = aliveTargets.Where(t => t.AggroTurns > 0).ToList();
                if (aggroTargets.Count > 0) target = aggroTargets[_rng.Next(aggroTargets.Count)];
            }

            if (target == null && attacker.IsEnemy && attacker.TauntDuration > 0)
            {
                target = aliveTargets.FirstOrDefault(t => t.Name == "Enos");
            }

            if (target == null)
            {
                target = aliveTargets[_rng.Next(aliveTargets.Count)];
            }

            ActiveAttacker = attacker;
            ActiveTarget = target;
            AttackAnimTimer = 0f;
        }

        private void ResolveAttack()
        {
            if (ActiveAttacker == null || ActiveTarget == null || !ActiveAttacker.IsAlive) return;
            if (!ActiveTarget.IsAlive) return;

            _onShakeCamera?.Invoke();

            var result = ActiveAttacker.CalculateAttack(ActiveTarget, _rng);
            result.IsMagical = ActiveAttacker.Type == AttackType.Magic;
            var atk = ActiveAttacker;
            bool burst = atk.GateReady; // Gate เต็ม: ครั้งนี้ตีหลายเป้าหมาย
            if (burst && atk.BurstDamageRate > 0f && !result.IsMiss)
                result.Damage = Math.Max(1, (int)(result.Damage * atk.BurstDamageRate));

            // Shield: กันการโจมตีได้ทั้งครั้ง (ไม่โดนดาเมจ ไม่โดนสถานะจากการตีครั้งนี้) แล้ว Shield หาย 1 Stack
            bool immune = !result.IsMiss && TryAbsorbWithShield(ActiveTarget, ref result);

            if (result.IsMiss)
            {
                AddLog($"{ActiveAttacker.Name} misses {ActiveTarget.Name}!");
            }
            else if (!immune)
            {
                ActiveTarget.TakeDamage(result.Damage);
                if (atk.GateMax > 0) ApplyBlessing(ActiveTarget, burst ? atk.BurstBlessing : atk.OnHitBlessing);
                if (burst) ApplyBurstStun(atk, ActiveTarget);
                if (burst) ApplyVariantStatus(atk, ActiveTarget);
                // Lifesteal: ฟื้น HP 5% ของดาเมจที่ทำได้
                if (ActiveAttacker.HasLifesteal && ActiveAttacker.IsAlive)
                {
                    int heal = Math.Max(1, (int)(result.Damage * Character.LifestealRate));
                    int before = ActiveAttacker.Hp;
                    ActiveAttacker.Hp = Math.Min(ActiveAttacker.MaxHp, ActiveAttacker.Hp + heal);
                    int gained = ActiveAttacker.Hp - before;
                    if (gained > 0) OnHealResolved?.Invoke(ActiveAttacker, gained);
                }
                string critText = result.IsCrit ? " (CRIT!)" : "";
                AddLog($"{ActiveAttacker.Name} hits {ActiveTarget.Name} for {result.Damage} damage{critText}");

                // ตัวที่กำลังติด Aura จากสกิล (Alice) ห้ามสะสม SP เพิ่ม เพราะ SP ใช้นับจำนวนการโจมตีที่เหลือของ Aura
                if (ActiveAttacker.SwordmanBuffDuration <= 0)
                    ActiveAttacker.CurrentSP = Math.Min(ActiveAttacker.CurrentSP + 50, ActiveAttacker.MaxSP);
                if (ActiveTarget.SwordmanBuffDuration <= 0)
                    ActiveTarget.CurrentSP = Math.Min(ActiveTarget.CurrentSP + 20, ActiveTarget.MaxSP);

                if (!ActiveTarget.IsAlive)
                    AddLog($"{ActiveTarget.Name} has been defeated!");
            }

            // Aura ของ Alice: โจมตี 1 ครั้ง = ลด 20% ของหลอด SP (ครบ 5 ครั้งหรือ SP เป็น 0 Aura หายไป)
            if (ActiveAttacker.SwordmanBuffDuration > 0)
            {
                ActiveAttacker.SwordmanBuffDuration--;
                ActiveAttacker.CurrentSP = ActiveAttacker.MaxSP * ActiveAttacker.SwordmanBuffDuration / SwordmanBuffAttacks;
            }

            // บอสที่มี Gate: ตีธรรมดา +1 Gate / Gate เต็มตีเพิ่มเป้าหมายอื่นจนครบ BurstTargets แล้วรีเซ็ต Gate
            if (atk.GateMax > 0)
            {
                if (burst)
                {
                    var pool = (atk.IsEnemy ? Allies : Enemies).Where(t => t != null && t.IsAlive && t != ActiveTarget).OrderBy(t => _rng.Next()).Take(Math.Max(0, atk.BurstTargets - 1)).ToList();
                    AddLog($"{atk.Name} unleashes its Gate!");
                    foreach (var t in pool)
                    {
                        var r = atk.CalculateAttack(t, _rng);
                        r.IsMagical = atk.Type == AttackType.Magic;
                        if (!r.IsMiss && atk.BurstDamageRate > 0f)
                            r.Damage = Math.Max(1, (int)(r.Damage * atk.BurstDamageRate));
                        bool imm = !r.IsMiss && TryAbsorbWithShield(t, ref r);
                        if (!r.IsMiss && !imm)
                        {
                            t.TakeDamage(r.Damage);
                            AddLog($"{atk.Name} hits {t.Name} for {r.Damage} damage");
                            ApplyBlessing(t, atk.BurstBlessing);
                            ApplyBurstStun(atk, t);
                            if (!t.IsAlive) AddLog($"{t.Name} has been defeated!");
                        }
                        else if (!imm) AddLog($"{atk.Name} misses {t.Name}!");
                        OnAttackResolved?.Invoke(t, r);
                    }
                    atk.GateCount = 0;
                    if (atk.Variant == EnemyVariant.Blue) GrantShieldToTeam(atk);
                }
                else atk.GateCount = Math.Min(atk.GateMax, atk.GateCount + 1);
            }

            OnAttackResolved?.Invoke(ActiveTarget, result);
            CheckBattleEnd();
        }

        // Shield: ถ้ามี Stack เหลือ -> ไม่โดนดาเมจ (IsImmune) และลด 1 Stack
        private bool TryAbsorbWithShield(Character target, ref AttackResult r)
        {
            if (target == null || r.IsMiss || target.ShieldStacks <= 0) return false;
            target.ShieldStacks--;
            r.Damage = 0;
            r.IsCrit = false;
            r.IsImmune = true;
            AddLog($"{target.Name} is immune! (Shield left: {target.ShieldStacks})");
            return true;
        }

        // Blue: Gate เต็มแล้วโจมตีครั้งถัดไป -> มอบ Shield 1 Stack ให้ศัตรูทุกตัวที่ยังมีชีวิต (ไม่จำกัดเทิร์น)
        private void GrantShieldToTeam(Character source)
        {
            var team = source.IsEnemy ? Enemies : Allies;
            foreach (var u in team)
            {
                if (u == null || !u.IsAlive) continue;
                u.ShieldStacks++;
                OnStatusInflicted?.Invoke(u, "Shield +1");
            }
            AddLog($"{source.Name} grants Shield to the whole team!");
        }

        // Veyra: ลบ Debuff 1 อย่างออกจากเป้าหมาย (ลำดับ: Stun > Melt > Fire > Poison > Weakness)
        private void CleanseOneDebuff(Character t)
        {
            if (t == null || !t.IsAlive) return;
            string removed = null;
            if (t.StunTurns > 0) { t.StunTurns = 0; removed = "Stun"; }
            else if (t.MeltTurns > 0) { t.MeltTurns = 0; t.RecalculateStats(); removed = "Melt"; }
            else if (t.FireStacks > 0) { t.FireStacks = 0; t.FireTurns = 0; removed = "Fire"; }
            else if (t.PoisonTurns > 0) { t.PoisonTurns = 0; removed = "Poison"; }
            else if (t.WeaknessTurns > 0) { t.WeaknessTurns = 0; removed = "Weakness"; }
            if (removed == null) return;
            AddLog($"{t.Name} is cleansed of {removed}");
            OnStatusInflicted?.Invoke(t, $"Cleanse {removed}");
        }

        // ศัตรูสี (Elite): Gate เต็มแล้วโจมตีโดนจะมอบสถานะตามสี Red=Fire, Green=Poison, Purple=Weakness
        private void ApplyVariantStatus(Character attacker, Character target)
        {
            if (attacker == null || target == null || !target.IsAlive) return;
            switch (attacker.Variant)
            {
                case EnemyVariant.Red:
                    bool melted = target.AddFire();
                    AddLog(melted ? $"{target.Name} is melting!" : $"{target.Name} is burning ({target.FireStacks}/{Character.FireMaxStacks})");
                    OnStatusInflicted?.Invoke(target, melted ? "Melt" : $"Fire {target.FireStacks}");
                    break;
                case EnemyVariant.Green:
                    target.PoisonTurns = Math.Max(target.PoisonTurns, Character.PoisonRounds);
                    AddLog($"{target.Name} is poisoned");
                    OnStatusInflicted?.Invoke(target, "Poison");
                    break;
                case EnemyVariant.Purple:
                    target.WeaknessTurns = Math.Max(target.WeaknessTurns, Character.WeaknessRounds);
                    AddLog($"{target.Name} is weakened");
                    OnStatusInflicted?.Invoke(target, "Weakness");
                    break;
            }
        }

        // Gate เต็ม: มอบ Stun ให้ตัวที่โดนโจมตี (ข้ามเทิร์นถัดไปของตัวนั้น)
        private void ApplyBurstStun(Character attacker, Character target)
        {
            if (attacker == null || target == null || !target.IsAlive || attacker.BurstStunTurns <= 0) return;
            target.StunTurns = Math.Max(target.StunTurns, attacker.BurstStunTurns);
            AddLog($"{target.Name} is stunned!");
        }

        private void ApplyBlessing(Character target, int stacks)
        {
            if (target == null || !target.IsAlive || stacks <= 0) return;
            target.AddBlessing(stacks);
            AddLog($"{target.Name} gains {stacks} Blessing ({target.BlessingStacks}/{Character.BlessingMaxStacks})");
            OnBlessingApplied?.Invoke(target, stacks);
            if (!target.IsAlive) AddLog($"{target.Name} is overwhelmed by Blessing!");
        }

        // จำนวนครั้งที่โจมตีได้ขณะติดบัฟ Ultimate ของ Alice (ตี 1 ครั้ง ลด 20%)
        public const int SwordmanBuffAttacks = 5;

        public static bool IsAliceUnit(Character c)
        {
            string n = c?.Name ?? "";
            return n == "Swordman" || n == "Alice" || n.Contains("Alice");
        }

        public void CastUltimate(Character caster, Character target)
        {
            if (caster == null) return;

            // Alice: SP ค้างที่ 100% ตลอดช่วงบัฟ (จะลดลง 20% ต่อการโจมตี 1 ครั้ง) ตัวอื่นใช้แล้วหลอดรีเซ็ตเป็น 0
            if (!IsAliceUnit(caster)) caster.CurrentSP = 0;
            IsUsingUltimate = true;
            UltimateCaster = caster;
            ActiveTarget = target;
            UltimateAnimTimer = 0f;

            // คำนวณดาเมจ/ฮีล ทันทีตอนนี้เลย เพราะฝั่ง BattleScreen เป็นคนคุมจังหวะอนิเมชัน
            // เองอยู่แล้ว (ถึงค่อยเรียก CastUltimate ตอนอนิเมชันเดินมาถึงจังหวะที่ควรลงดาเมจ)
            // ถ้าไม่ Resolve ที่นี่ จะไม่มีใครเรียก ResolveUltimate() ให้เลย เพราะ BattleScreen
            // จะไม่เรียก Update(delta) ของคลาสนี้ตลอดช่วงที่ IsUsingUltimate เป็น true
            ResolveUltimate();

            IsUsingUltimate = false;
            UltimateCaster = null;
        }

        private void ResolveUltimate()
        {
            if (UltimateCaster == null) return;
            var caster = UltimateCaster;
            var target = ActiveTarget;

            string name = caster.Name ?? "";

            if (name == "Enos")
            {
                // Enos: ตีศัตรู 1 ตัวด้วย 40% P.ATK แล้วได้ Aggro 2 เทิร์น ศัตรูจะมาตี Enos
                if (target != null && target != caster && target.IsAlive)
                {
                    var res = caster.CalculateSkillDamage(target, _rng, 0.4f, false);
                    if (!TryAbsorbWithShield(target, ref res))
                    {
                        target.TakeDamage(res.Damage);
                        target.CurrentSP = Math.Min(target.CurrentSP + 20, target.MaxSP);
                    }
                    OnAttackResolved?.Invoke(target, res);
                }
                caster.AggroTurns = Character.AggroRounds;
                AddLog($"{caster.Name} uses Ultimate and gains Aggro for {Character.AggroRounds} turns!");
            }
            else if (name == "Elma")
            {
                if (target == null || !target.IsAlive) return;
                var res = caster.CalculateSkillDamage(target, _rng, 2.5f, false);
                if (!TryAbsorbWithShield(target, ref res))
                {
                    target.TakeDamage(res.Damage);
                    target.CurrentSP = Math.Min(target.CurrentSP + 20, target.MaxSP);
                }
                OnAttackResolved?.Invoke(target, res);
            }
            else if (name == "Veyra")
            {
                int casterShare = (int)(caster.MaxHp * 0.25f);
                foreach (var ally in Allies)
                {
                    if (ally != null && ally.IsAlive)
                    {
                        int healAmount = casterShare + (int)(ally.MaxHp * 0.02f);
                        ally.Hp = Math.Min(ally.MaxHp, ally.Hp + healAmount);
                        OnHealResolved?.Invoke(ally, healAmount);
                    }
                }
                // ลบ Debuff 1 อย่างจากเป้าหมายที่เลือก (ถ้าไม่มีเป้าหมายใช้ตัว Veyra เอง)
                CleanseOneDebuff(target != null && target.IsAlive ? target : caster);
            }
            else if (name == "Mage" || name == "Lumi" || name.Contains("Lumi"))
            {
                foreach (var enemy in Enemies.ToList())
                {
                    if (enemy.IsAlive)
                    {
                        // สกิล Lumi เป็นเวทย์ (M.ATK vs M.DEF) คิด Crit แยกแต่ละเป้า / ตัวเลขสีน้ำเงิน
                        var res = caster.CalculateSkillDamage(enemy, _rng, 1.1f, true);
                        if (!TryAbsorbWithShield(enemy, ref res))
                        {
                            enemy.TakeDamage(res.Damage);
                            enemy.CurrentSP = Math.Min(enemy.CurrentSP + 20, enemy.MaxSP);
                        }
                        OnAttackResolved?.Invoke(enemy, res);
                    }
                }
            }
            else if (name == "Swordman" || name == "Alice" || name.Contains("Alice"))
            {
                caster.SwordmanBuffDuration = SwordmanBuffAttacks;
                caster.CurrentSP = caster.MaxSP;
                AddLog($"{caster.Name} uses Ultimate and gains Aura for {SwordmanBuffAttacks} attacks!");
            }

            CheckBattleEnd();
        }

        private void CheckBattleEnd()
        {
            if (Allies.All(a => !a.IsAlive))
            {
                State = BattleState.EnemyWin;
                AddLog("Lose!");
            }
            else if (Enemies.All(e => !e.IsAlive))
            {
                State = BattleState.AllyWin;
                AddLog("Win!");
            }
        }

        private void AddLog(string message)
        {
            Log.Add(message);
            if (Log.Count > MaxLogLines)
                Log.RemoveAt(0);
        }
    }
}