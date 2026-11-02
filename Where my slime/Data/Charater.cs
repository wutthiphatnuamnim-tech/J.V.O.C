using System.Collections.Generic;
using Where_My_Slime.Data;
using System;

namespace Where_My_Slime.Models
{
    public enum AttackType
    {
        Physical, // ใช้ P.Atk โจมตี (นักดาบ, นักธนู ฯลฯ)
        Magic     // ใช้ M.Atk โจมตี (นักเวท ฯลฯ)
    }

    // สเตตัสโตที่ผู้เล่นแจกแต้มได้ (STR/INT/VIT/DEX/LUX/CRT)
    public enum GrowthStat
    {
        STR, // เพิ่ม P.Atk
        INT, // เพิ่ม M.Atk และ M.Def
        VIT, // เพิ่ม HP และ P.Def
        DEX, // เพิ่มความแม่นยำ (Accuracy) และ Speed
        LUX, // เพิ่มอัตราคริติคอล (CritRate)
        CRT  // เพิ่มดาเมจคริติคอล (CritDamage)
    }

    // สเตตัสหลักที่การ์ด (UpgradeCard) และไอเทม (Item) สามารถปรับได้ ทั้งแบบ % และแบบหน่วยตรงๆ
    public enum StatKind
    {
        PAtk,
        MAtk,
        AllAtk,   // ปรับทั้ง PAtk และ MAtk พร้อมกัน
        MaxHp,
        Speed,
        CritRate,
        CritDamage,
        Accuracy,
        PDef,
        MDef
    }

    public struct AttackResult
    {
        public int Damage;
        public bool IsCrit;
        public bool IsMiss;
        public bool IsImmune;  // โดนโจมตีแต่มี Shield: ไม่โดนดาเมจ (ขึ้นคำว่า IMMUNE) และ Shield หายไป 1 Stack
        public bool IsMagical; // ใช้เลือกสีตัวเลขดาเมจ (แดง = กายภาพ, น้ำเงิน = เวทย์)
    }

    // สีของศัตรู (Elite): Red = Fire / Green = Poison / Purple = Weakness / Blue = ให้ Shield ทั้งทีม
    public enum EnemyVariant { None, Red, Green, Purple, Blue }

    public class Character
    {
        public string Name;
        public bool IsEnemy;

        // ---------- สเตตัสฐาน (Base) ----------
        private int _baseMaxHp;
        private int _baseSpeed;
        private int _basePAtk;
        private int _baseMAtk;
        private int _basePDef;
        private int _baseMDef;
        private float _baseAccuracy;
        private float _baseCritRate;
        private float _baseCritDamage;

        // ---------- แต้มโต (Growth) ----------
        public int STR;
        public int INT;
        public int VIT;
        public int DEX;
        public int LUX;
        public int CRT;

        public int UpgradePoints { get; set; }

        // ---------- สเตตัสสุดท้าย ----------
        public int MaxHp { get; private set; }
        public int Speed { get; private set; }
        public int PAtk { get; private set; }
        public int MAtk { get; private set; }
        public int PDef { get; private set; }
        public int MDef { get; private set; }
        public float Accuracy { get; private set; }
        public float CritRate { get; private set; }
        public float CritDamage { get; private set; }

        public int Hp;
        public AttackType Type;

        public float Gauge;
        public const float GaugeMax = 1000f;

        // ไอเท็มที่สวมใส่อยู่ 3 ช่อง: Weapon / Armor / Additional (เก็บตามลำดับค่า ItemSlot)
        private readonly Item[] _equipped = new Item[3];

        public IEnumerable<Item> EquippedItems
        {
            get { foreach (var i in _equipped) if (i != null) yield return i; }
        }

        public Item GetEquipped(ItemSlot slot) => _equipped[(int)slot];

        // สวมใส่ไอเท็มชื่อนี้อยู่หรือไม่ (ช่องไหนก็ได้)
        public bool HasEquipped(string itemName)
        {
            foreach (var i in _equipped) if (i != null && i.Name == itemName) return true;
            return false;
        }

        // จำนวนชิ้นที่สวมใส่ใน Set เดียวกัน (เตรียมไว้สำหรับโบนัส Set ในอนาคต)
        public int CountEquippedInSet(string setName)
        {
            if (string.IsNullOrEmpty(setName)) return 0;
            int n = 0;
            foreach (var i in _equipped) if (i != null && i.SetName == setName) n++;
            return n;
        }

        // ---------- ระบบ Ultimate Skill & Buffs ----------
        public int CurrentSP { get; set; }
        public int MaxSP { get; set; }
        public int TauntDuration { get; set; } // จำนวนเทิร์นที่โดน Elma ยั่วยุ
        public int SwordmanBuffDuration { get; set; } // ระยะเวลาบัฟของ Swordman (นับเป็นจำนวนการโจมตี)

        // ---------- สถานะ (Buff / Debuff) ----------
        public const int BlessingMaxStacks = 20;      // ครบ 20 Stack = ตายทันที
        public const int BlessingHpPerStack = 16;     // +MaxHP ต่อ 1 Stack
        public const int BlessingDamageThreshold = 16;// โดนดาเมจเกินค่านี้ -1 Stack
        public const int BlessingDecayRounds = 4;     // ครบกี่เทิร์นลด 1 Stack
        public const int AuraRounds = 3;
        public const int LifestealRounds = 3;
        public const int PoisonRounds = 2;
        public const int WeaknessRounds = 4;
        public const int FireRounds = 3;            // Fire อยู่ได้ 3 เทิร์น (ติดซ้ำรีเฟรช)
        public const int FireMaxStacks = 3;         // สะสมได้สูงสุด 3 Stack
        public const int FireDamagePerStack = 100;  // Fix Dmg 100 ต่อ Stack ทุกจบเทิร์น
        public const int MeltRounds = 3;            // Melt อยู่ 3 เทิร์น
        public const float MeltHpPenalty = 0.20f;   // Melt: -20% MaxHP
        public const float LifestealRate = 0.05f;
        public const float PoisonHpRate = 0.08f;
        public const int WeaknessAtkPenalty = 10;

        public int AuraTurns { get; set; }       // Buff: Aura
        public int BlessingStacks { get; private set; } // Buff: Blessing
        public int BlessingTimer { get; set; }   // นับเทิร์นสำหรับลด Stack
        public int LifestealTurns { get; set; }  // Buff: Lifesteal
        public int ShieldStacks { get; set; }    // Buff: Shield (สะสมได้ไม่จำกัด ไม่หมดเวลา) กันการโจมตีได้ครั้งละ 1 Stack
        public int StunTurns { get; set; }       // Debuff: Stun (ข้ามเทิร์นของตัวนี้)
        public int AggroTurns { get; set; }      // Buff: Aggro (ศัตรูต้องมาตีตัวนี้)
        public const int AggroRounds = 2;
        public int PoisonTurns { get; set; }     // Debuff: Poison
        public int WeaknessTurns { get; set; }   // Debuff: Weakness
        public int FireStacks { get; set; }      // Debuff: Fire (สูงสุด 3 Stack)
        public int FireTurns { get; set; }
        public int MeltTurns { get; set; }       // Debuff: Melt (-20% MaxHP)
        public EnemyVariant Variant { get; set; }

        // ติด Fire 1 Stack: ถ้าครบ 3 Stack อยู่แล้ว (เกิน 3) จะติด Melt แทน คืนค่า true เมื่อเกิด Melt
        public bool AddFire()
        {
            if (!IsAlive) return false;
            FireTurns = FireRounds;
            if (FireStacks < FireMaxStacks)
            {
                FireStacks++;
                return false;
            }
            MeltTurns = MeltRounds;
            RecalculateStats();
            return true;
        }

        // ระบบ Gate ของบอส (GateMax = 0 คือไม่มี)
        public int GateMax { get; set; }
        public int GateCount { get; set; }
        public int OnHitBlessing { get; set; }
        public int BurstBlessing { get; set; }
        public int BurstTargets { get; set; }
        public float BurstDamageRate { get; set; }   // ดาเมจตอน Gate เต็มเป็นสัดส่วนของดาเมจปกติ (0 = เต็ม 100%)
        public int BurstStunTurns { get; set; }      // Stun ที่มอบให้แต่ละเป้าหมายตอน Gate เต็ม (0 = ไม่มี)
        public bool GateReady => GateMax > 0 && GateCount >= GateMax;

        public bool HasAura => SwordmanBuffDuration > 0 || AuraTurns > 0;
        public bool HasLifesteal => LifestealTurns > 0;

        // เพิ่ม Blessing ทีละ Stack: +16 MaxHP ต่อ Stack, ครบ 20 Stack ตายทันที
        public void AddBlessing(int stacks = 1)
        {
            if (!IsAlive || stacks <= 0) return;
            int old = MaxHp;
            BlessingStacks = Math.Min(BlessingMaxStacks, BlessingStacks + stacks);
            BlessingTimer = 0;
            RecalculateStats();
            if (MaxHp > old) Hp += MaxHp - old;
            if (BlessingStacks >= BlessingMaxStacks) { Hp = 0; }
        }

        public void RemoveBlessing(int stacks = 1)
        {
            if (BlessingStacks <= 0) return;
            BlessingStacks = Math.Max(0, BlessingStacks - stacks);
            RecalculateStats(); // MaxHP ลดลง และ Hp จะถูกจำกัดไม่เกิน MaxHp
        }

        public void ClearStatuses()
        {
            SwordmanBuffDuration = 0; TauntDuration = 0;
            AuraTurns = 0; AggroTurns = 0; LifestealTurns = 0; StunTurns = 0; PoisonTurns = 0; WeaknessTurns = 0;
            FireStacks = 0; FireTurns = 0; ShieldStacks = 0;
            BlessingTimer = 0;
            bool needRecalc = BlessingStacks != 0 || MeltTurns != 0;
            BlessingStacks = 0; MeltTurns = 0;
            if (needRecalc) RecalculateStats();
        }

        // ดึงสเตตัสจริงที่รวมบัฟของ Swordman (ถ้าระยะเวลาบัฟ > 0 จะบวกสเตตัสเพิ่ม)
        // Aura (+10% P.ATK, +20% P.DEF, +30% Speed, +25 Crit Rate, +50 Crit DMG) และ Weakness (-10 All.ATK) รวมอยู่ในค่าเหล่านี้
        private int WeakPenalty => WeaknessTurns > 0 ? WeaknessAtkPenalty : 0;
        public int CurrentPAtk => Math.Max(0, PAtk + (HasAura ? (int)(PAtk * 0.1f) : 0) - WeakPenalty);
        public int CurrentMAtk => Math.Max(0, MAtk + (HasAura ? (int)(MAtk * 0.3f) : 0) - WeakPenalty);
        public int CurrentPDef => PDef + (HasAura ? (int)(PDef * 0.2f) : 0);
        public int CurrentSpeed => Speed + (HasAura ? (int)(Speed * 0.3f) : 0);
        public float CurrentCritRate => CritRate + (HasAura ? 25f : 0f);
        public float CurrentCritDamage => CritDamage + (HasAura ? 50f : 0f);

        // ปรับตัวคูณให้ตรงกับสูตรพรีวิวในหน้า Upgrade ของ Game1.cs
        // ต้องตรงกับค่าใน ApplyGrowthPoint และหน้า Upgrade เสมอ ไม่งั้นพอสวมใส่/ถอดไอเท็ม (RecalculateStats) สเตตัสที่อัพไว้จะเพี้ยน
        private const float StrToPAtk = 12f;
        private const float IntToMAtk = 12f;
        private const float IntToMDef = 9f;
        private const float VitToHp = 50f;
        private const float VitToPDef = 7f;
        private const float DexToSpeed = 2f;
        private const float DexToAccuracy = 1f;
        private const float LuxToCritRate = 4f;
        private const float CrtToCritRate = 0.25f;

        // โอกาสหลบมาจาก "ส่วนต่าง Speed": ถ้าเป้าหมายเร็วกว่าผู้โจมตี ทุก 1 Speed ที่เร็วกว่า = หลบได้ +1% (สูงสุด 50%)
        public const float SpeedDodgePerPoint = 1f;
        public const float SpeedDodgeCap = 50f;
        private const float CrtToCritDamage = 5f; // 1 CRT = +5% Crit Damage

        public Character(string name, int hp, int speed, int pAtk, int mAtk, int pDef, AttackType type, bool isEnemy,
            int mDef = 0, float accuracy = 100f, float critRate = 0f, float critDamage = 150f)
        {
            Name = name;
            _baseMaxHp = hp;
            _baseSpeed = speed;
            _basePAtk = pAtk;
            _baseMAtk = mAtk;
            _basePDef = pDef;
            _baseMDef = mDef == 0 ? pDef : mDef;
            _baseAccuracy = accuracy;
            _baseCritRate = critRate;
            _baseCritDamage = critDamage;

            Type = type;
            IsEnemy = isEnemy;
            Gauge = 0f;

            RecalculateStats();
            Hp = MaxHp;
            InitializeSP();
        }

        // ฟังก์ชันเซ็ตค่า SP เริ่มต้นตามอาชีพ
        public void InitializeSP()
        {
            CurrentSP = 0;
            // เริ่มด่านใหม่: ล้างบัฟ/ยั่วยุที่ค้างมาจากด่านก่อน
            ClearStatuses();
            if (Name == "Enos") MaxSP = 100;
            else if (Name == "Veyra") MaxSP = 120;
            else if (Name == "Lumi") MaxSP = 150;
            else if (Name == "Elma") MaxSP = 200;
            else if (Name == "Alice") MaxSP = 80;
            else MaxSP = 100;
        }

        public bool IsAlive => Hp > 0;

        public bool TickGauge(float deltaSeconds)
        {
            if (!IsAlive) return false;
            // ใช้ CurrentSpeed เผื่อมีการติดบัฟของ Swordman ที่เพิ่ม Speed +30%
            Gauge += CurrentSpeed * deltaSeconds * 20f;
            if (Gauge >= GaugeMax)
            {
                Gauge = 0f;
                return true;
            }
            return false;
        }

        // คำนวณสเตตัสสุดท้ายใหม่ทั้งหมด
        public void RecalculateStats()
        {
            int hpCalc = _baseMaxHp + (int)(VIT * VitToHp);
            int speedCalc = _baseSpeed + (int)(DEX * DexToSpeed);
            int pAtkCalc = _basePAtk + (int)(STR * StrToPAtk);
            int mAtkCalc = _baseMAtk + (int)(INT * IntToMAtk);
            int pDefCalc = _basePDef + (int)(VIT * VitToPDef);
            int mDefCalc = _baseMDef + (int)(INT * IntToMDef);
            float accCalc = Math.Min(100f, _baseAccuracy + DEX * DexToAccuracy);

            // อัป LUX เพิ่ม Crit Rate, อัป CRT เพิ่ม Crit DMG
            float critRateCalc = Math.Min(100f, _baseCritRate + (LUX * LuxToCritRate) + (CRT * CrtToCritRate));
            // อัป CRT เพิ่ม Crit DMG โดยตรง
            float critDmgCalc = _baseCritDamage + CRT * CrtToCritDamage;

            // ประมวลผลโบนัสจากไอเท็มที่สวมใส่
            {
                void ApplyStat(StatKind kind, float amount, bool isPercent)
                {
                    switch (kind)
                    {
                        case StatKind.PAtk: pAtkCalc = isPercent ? (int)(pAtkCalc * (1 + amount)) : pAtkCalc + (int)amount; break;
                        case StatKind.MAtk: mAtkCalc = isPercent ? (int)(mAtkCalc * (1 + amount)) : mAtkCalc + (int)amount; break;
                        case StatKind.AllAtk:
                            pAtkCalc = isPercent ? (int)(pAtkCalc * (1 + amount)) : pAtkCalc + (int)amount;
                            mAtkCalc = isPercent ? (int)(mAtkCalc * (1 + amount)) : mAtkCalc + (int)amount;
                            break;
                        case StatKind.MaxHp: hpCalc = isPercent ? (int)(hpCalc * (1 + amount)) : hpCalc + (int)amount; break;
                        case StatKind.Speed: speedCalc = isPercent ? (int)(speedCalc * (1 + amount)) : speedCalc + (int)amount; break;
                        // Crit Rate / Crit DMG เป็นหน่วย % อยู่แล้ว จึงบวกเพิ่มเป็น "แต้ม %" (เช่น +8% = +8 แต้ม) ไม่คูณทบ
                        // (ถ้าคูณ ค่าฐาน 5% x 1.08 จะได้แค่ 5.4% แทบไม่ต่างจากเดิม)
                        case StatKind.CritRate: critRateCalc = isPercent ? critRateCalc + amount * 100f : critRateCalc + amount; break;
                        case StatKind.CritDamage: critDmgCalc = isPercent ? critDmgCalc + amount * 100f : critDmgCalc + amount; break;
                        case StatKind.Accuracy: accCalc = isPercent ? accCalc * (1 + amount) : accCalc + amount; break;
                        case StatKind.PDef: pDefCalc = isPercent ? (int)(pDefCalc * (1 + amount)) : pDefCalc + (int)amount; break;
                        case StatKind.MDef: mDefCalc = isPercent ? (int)(mDefCalc * (1 + amount)) : mDefCalc + (int)amount; break;
                    }
                }

                foreach (var equipped in EquippedItems)
                {
                    string itemName = equipped.Name;

                    // ไอเท็มที่มีเงื่อนไขพิเศษ: ถ้าไม่เข้าเงื่อนไขจะไม่ได้โบนัสหลัก
                    bool applyGeneric = true;
                    if (itemName == "Demon Eye") applyGeneric = CRT > 15;
                    else if (itemName == "Strongest Protection Necklace") applyGeneric = false;

                    if (applyGeneric)
                    {
                        ApplyStat(equipped.Stat, equipped.Amount, equipped.IsPercent);

                        if (equipped.SecondaryAmount != 0 || equipped.SecondaryStat != default(StatKind))
                        {
                            ApplyStat(equipped.SecondaryStat, equipped.SecondaryAmount, equipped.SecondaryIsPercent);
                        }

                        if (equipped.TertiaryAmount != 0 || equipped.TertiaryStat != default(StatKind))
                        {
                            ApplyStat(equipped.TertiaryStat, equipped.TertiaryAmount, equipped.TertiaryIsPercent);
                        }
                    }

                    if (itemName == "Ancient Katana" && STR > 15) pAtkCalc = (int)(pAtkCalc * 1.14f);
                    else if (itemName == "Ancient Staff" && INT > 15) mAtkCalc = (int)(mAtkCalc * 1.14f);
                    else if (itemName == "Strongest Protection Necklace")
                    {
                        // All.ATK เพิ่มตาม VIT: VIT x 1.3%
                        float mult = 1f + VIT * 0.013f;
                        pAtkCalc = (int)(pAtkCalc * mult);
                        mAtkCalc = (int)(mAtkCalc * mult);
                    }
                    else if (itemName == "Ring of Legendary Hero" && CRT > 10)
                    {
                        critRateCalc += 50f;
                        critDmgCalc += 50f;
                    }
                }
            }

            MaxHp = Math.Max(1, hpCalc) + BlessingStacks * BlessingHpPerStack;
            if (MeltTurns > 0) MaxHp = Math.Max(1, (int)(MaxHp * (1f - MeltHpPenalty)));
            Speed = Math.Max(1, speedCalc);
            PAtk = pAtkCalc;
            MAtk = mAtkCalc;
            PDef = pDefCalc;
            MDef = mDefCalc;
            Accuracy = Math.Min(100f, accCalc);
            CritRate = Math.Min(100f, critRateCalc);
            CritDamage = critDmgCalc;

            if (Hp > MaxHp) Hp = MaxHp;
        }

        public void ApplyGrowthPoint(GrowthStat stat)
        {
            // เพิ่มแค่ตัวนับแต้ม แล้วให้ RecalculateStats() คำนวณสเตตัสทั้งหมดใหม่จากค่าฐาน + แต้ม + ไอเท็ม
            // เพื่อให้มีแหล่งคำนวณเดียว (เดิมบวกเข้าสเตตัสสุดท้ายตรงๆ ซึ่งไม่ตรงกับตอนสวม/ถอดไอเท็ม ทำให้ค่าเพี้ยนสะสม)
            int oldMaxHp = MaxHp;

            switch (stat)
            {
                case GrowthStat.STR: STR++; break;
                case GrowthStat.INT: INT++; break;
                case GrowthStat.VIT: VIT++; break;
                case GrowthStat.DEX: DEX++; break;
                case GrowthStat.LUX: LUX++; break;
                case GrowthStat.CRT: CRT++; break;
            }

            RecalculateStats();

            // HP สูงสุดที่เพิ่มขึ้น ให้เลือดปัจจุบันเพิ่มตามด้วย
            if (MaxHp > oldMaxHp) Hp += MaxHp - oldMaxHp;
        }

        public void ModifyBaseStat(StatKind kind, float amount, bool isPercent)
        {
            switch (kind)
            {
                case StatKind.PAtk:
                    _basePAtk = isPercent ? (int)(_basePAtk * (1 + amount / 100f)) : _basePAtk + (int)amount;
                    break;
                case StatKind.MAtk:
                    _baseMAtk = isPercent ? (int)(_baseMAtk * (1 + amount / 100f)) : _baseMAtk + (int)amount;
                    break;
                case StatKind.AllAtk:
                    _basePAtk = isPercent ? (int)(_basePAtk * (1 + amount / 100f)) : _basePAtk + (int)amount;
                    _baseMAtk = isPercent ? (int)(_baseMAtk * (1 + amount / 100f)) : _baseMAtk + (int)amount;
                    break;
                case StatKind.MaxHp:
                    _baseMaxHp = isPercent ? (int)(_baseMaxHp * (1 + amount / 100f)) : _baseMaxHp + (int)amount;
                    if (_baseMaxHp < 1) _baseMaxHp = 1;
                    break;
                case StatKind.Speed:
                    _baseSpeed = isPercent ? (int)(_baseSpeed * (1 + amount / 100f)) : _baseSpeed + (int)amount;
                    if (_baseSpeed < 1) _baseSpeed = 1;
                    break;
                case StatKind.CritRate:
                    _baseCritRate = isPercent ? _baseCritRate * (1 + amount / 100f) : _baseCritRate + amount;
                    break;
                case StatKind.CritDamage:
                    _baseCritDamage = isPercent ? _baseCritDamage * (1 + amount / 100f) : _baseCritDamage + amount;
                    break;
                case StatKind.Accuracy:
                    _baseAccuracy = isPercent ? _baseAccuracy * (1 + amount / 100f) : _baseAccuracy + amount;
                    break;
                case StatKind.PDef:
                    _basePDef = isPercent ? (int)(_basePDef * (1 + amount / 100f)) : _basePDef + (int)amount;
                    break;
                case StatKind.MDef:
                    _baseMDef = isPercent ? (int)(_baseMDef * (1 + amount / 100f)) : _baseMDef + (int)amount;
                    break;
            }

            RecalculateStats();
        }

        // สวมไอเท็มลงช่องตามชนิดของมัน (Weapon/Armor/Additional) คืนค่าไอเท็มเดิมในช่องนั้นที่ถูกแทนที่ (ถ้ามี)
        public Item EquipItem(Item item)
        {
            if (item == null) return null;
            int idx = (int)item.Slot;
            var previous = _equipped[idx];
            _equipped[idx] = item;
            RecalculateStats();
            return previous;
        }

        // ถอดไอเท็มออกจากช่องที่ระบุ คืนค่าไอเท็มที่ถอดออก
        public Item UnequipItem(ItemSlot slot)
        {
            int idx = (int)slot;
            var removed = _equipped[idx];
            if (removed == null) return null;
            _equipped[idx] = null;
            RecalculateStats();
            return removed;
        }

        public void ReduceCurrentHpPercent(float percent)
        {
            int amount = (int)(MaxHp * (percent / 100f));
            Hp = Math.Max(1, Hp - amount);
        }

        public const float IgnoreDefPercent = 0.65f;

        public AttackResult CalculateAttack(Character target, Random rng)
        {
            if (rng.NextDouble() * 100 > Accuracy)
                return new AttackResult { Damage = 0, IsCrit = false, IsMiss = true };

            // การหลบคิดจากส่วนต่าง Speed: เป้าหมายเร็วกว่าผู้โจมตีกี่แต้ม ก็หลบได้กี่เปอร์เซ็นต์ (ตามค่าคงที่ด้านบน)
            float dodgeChance = Math.Min(SpeedDodgeCap, Math.Max(0f, (target.CurrentSpeed - CurrentSpeed) * SpeedDodgePerPoint));
            if (rng.NextDouble() * 100 < dodgeChance)
                return new AttackResult { Damage = 0, IsCrit = false, IsMiss = true };

            // เปลี่ยนไปดึงค่า CurrentPAtk / CurrentPDef เพื่อให้ระบบคำนวณบัฟของ Swordman เข้าไปด้วย
            int atkStat = Type == AttackType.Physical ? CurrentPAtk : CurrentMAtk;
            int defStat = Type == AttackType.Physical ? target.CurrentPDef : target.MDef;

            if (HasEquipped("Dragon Mail"))
            {
                defStat = (int)(defStat * 0.1f);
            }

            // ดาบ Fallen Angel: Ignore All.DEF 65% = เอาค่า DEF ของเป้าหมายมาคิดแค่ 35% (DEF 100 -> 35) ทุกการโจมตี
            if (HasEquipped("Fallen Angel Magic Sword"))
            {
                defStat = (int)(defStat * (1f - IgnoreDefPercent));
            }

            int baseDamage = atkStat - (int)(defStat * 0.5f);
            if (baseDamage < 1) baseDamage = 1;

            float variance = 0.9f + (float)rng.NextDouble() * 0.2f;
            float damage = baseDamage * variance;

            // ดึงค่า CurrentCritRate / CurrentCritDamage เพื่อคำนวณบัฟ
            bool isCrit = rng.NextDouble() * 100 < CurrentCritRate;
            if (isCrit)
                damage *= 1f + CurrentCritDamage / 100f;

            int finalDamage = Math.Max(1, (int)damage);
            return new AttackResult { Damage = finalDamage, IsCrit = isCrit, IsMiss = false };
        }

        // ดาเมจของสกิล (Ultimate): โดนเสมอ (ไม่มี Miss/หลบ) แต่คิด DEF ของเป้าหมาย (รวมของที่ลด/ทะลุ DEF) และติด Crit ได้
        // multiplier = ตัวคูณของค่าโจมตี เช่น 2.5 = 250% / magical = ใช้ M.ATK vs M.DEF แทน P.ATK vs P.DEF
        public AttackResult CalculateSkillDamage(Character target, Random rng, float multiplier, bool magical)
        {
            int atkStat = magical ? CurrentMAtk : CurrentPAtk;
            int defStat = magical ? target.MDef : target.CurrentPDef;

            if (HasEquipped("Dragon Mail")) defStat = (int)(defStat * 0.1f);
            if (HasEquipped("Fallen Angel Magic Sword")) defStat = (int)(defStat * (1f - IgnoreDefPercent));

            // ตัวคูณสกิลคูณกับค่าโจมตีก่อน แล้วค่อยหัก DEF (เหมือนการโจมตีปกติ)
            int baseDamage = (int)(atkStat * multiplier) - (int)(defStat * 0.5f);
            if (baseDamage < 1) baseDamage = 1;

            float damage = baseDamage * (0.9f + (float)rng.NextDouble() * 0.2f);
            bool isCrit = rng.NextDouble() * 100 < CurrentCritRate;
            if (isCrit) damage *= 1f + CurrentCritDamage / 100f;

            return new AttackResult { Damage = Math.Max(1, (int)damage), IsCrit = isCrit, IsMiss = false, IsMagical = magical };
        }

        public void TakeDamage(int amount)
        {
            Hp -= amount;
            if (Hp < 0) Hp = 0;
            // Blessing: โดนดาเมจเกิน 16 ในครั้งเดียว -1 Stack
            if (amount > BlessingDamageThreshold && BlessingStacks > 0 && Hp > 0) RemoveBlessing(1);
        }
    }
}