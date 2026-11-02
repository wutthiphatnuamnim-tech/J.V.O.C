using System.Collections.Generic;
using Where_My_Slime.Models;

namespace Where_My_Slime.Data
{
    // "ต้นแบบ" (template) ของตัวละคร/ศัตรูหนึ่งตัว เก็บค่าสเตตัสตั้งต้นไว้ที่เดียว
    // แล้วค่อยสั่ง CreateInstance() เพื่อสร้าง Character จริงตอนเริ่มเกม/ด่านใหม่
    public class CharacterDefinition
    {
        public string Id;
        public string Name;
        public int Hp;
        public int Speed;
        public int PAtk;
        public int MAtk;
        public int PDef;
        public int MDef;
        public float Accuracy = 90f;
        public float CritRate = 5f;
        public float CritDamage = 50f;
        public AttackType Type;
        public bool IsEnemy;

        // ระบบ Gate ของบอส (0 = ไม่มี): โจมตีครบ GateMax ครั้งแล้วครั้งถัดไปจะตีหลายเป้าหมาย
        public int GateMax;
        public int OnHitBlessing;   // Blessing ที่มอบให้เป้าหมายทุกครั้งที่โดนตี
        public int BurstBlessing;   // Blessing ที่มอบให้แต่ละเป้าหมายตอน Gate เต็ม
        public int BurstTargets;    // จำนวนเป้าหมายตอน Gate เต็ม
        public float BurstDamageRate; // ดาเมจตอน Gate เต็มเทียบดาเมจปกติ (0 = เต็ม)
        public int BurstStunTurns;    // Stun ที่มอบให้ทุกเป้าหมายตอน Gate เต็ม

        // --- เพิ่มตัวแปรจัดการ SP และ Ultimate ---
        public int CurrentSP { get; set; }
        public int MaxSP { get; set; }

        // --- ตัวแปรสำหรับสถานะ (Buff/Debuff) ---
        public int TauntDuration { get; set; } // จำนวนเทิร์นที่โดน Elma ยั่วยุ
        public int AliceBuffDuration { get; set; } // ระยะเวลาบัฟของ Alice (เปลี่ยนจาก Swordman)

        // ฟังก์ชันเซ็ตค่า SP เริ่มต้น (เรียกใช้ตอนสร้างตัวละคร)
        public void InitializeSP()
        {
            CurrentSP = 0;
            if (Name == "Enos") MaxSP = 100;
            else if (Name == "Veyra") MaxSP = 120;
            else if (Name == "Lumi") MaxSP = 150;
            else if (Name == "Elma") MaxSP = 200;
            else if (Name == "Alice") MaxSP = 80; // เปลี่ยนจาก Swordman เป็น Alice
            else MaxSP = 100; // ค่าเริ่มต้นเผื่อตัวอื่น
        }

        // --- ดึงสเตตัสจริงที่รวมบัฟของ Alice แล้ว ไปใช้คำนวณดาเมจในเกม ---
        public int CurrentPAtk => PAtk + (AliceBuffDuration > 0 ? (int)(PAtk * 0.3f) : 0);
        public int CurrentPDef => PDef + (AliceBuffDuration > 0 ? (int)(PDef * 0.3f) : 0);
        public int CurrentSpeed => Speed + (AliceBuffDuration > 0 ? (int)(Speed * 0.3f) : 0);
        public float CurrentCritRate => CritRate + (AliceBuffDuration > 0 ? 30f : 0f);
        public float CurrentCritDamage => CritDamage + (AliceBuffDuration > 0 ? 120f : 0f);

        // กำหนดชื่อเท็กซ์เจอร์เริ่มต้นให้ตรงกับไฟล์ภาพของ Lumi
        public string IdleTextureName = "Char_Lumi_IDLE";
        public string AttackTextureName = "Char_Lumi_ATTACK";

        public Character CreateInstance()
        {
            var c = new Character(Name, Hp, Speed, PAtk, MAtk, PDef, Type, IsEnemy, MDef, Accuracy, CritRate, CritDamage);
            c.GateMax = GateMax;
            c.OnHitBlessing = OnHitBlessing;
            c.BurstBlessing = BurstBlessing;
            c.BurstTargets = BurstTargets;
            c.BurstDamageRate = BurstDamageRate;
            c.BurstStunTurns = BurstStunTurns;
            return c;
        }
    }

    // Index กลาง เก็บข้อมูลตัวละครพันธมิตรและศัตรูทั้งหมดของเกม
    public static class CharacterIndex
    {
        // ---------- ปาร์ตี้พันธมิตรเริ่มต้น (เปลี่ยน Swordman เป็น Alice) ----------
        public static readonly Dictionary<string, CharacterDefinition> Allies = new Dictionary<string, CharacterDefinition>
        {
            ["alice"] = new CharacterDefinition { Id = "alice", Name = "Alice", Hp = 1200, Speed = 95, PAtk = 300, MAtk = 20, PDef = 60, MDef = 40, Type = AttackType.Physical, IsEnemy = false },
            ["elma"] = new CharacterDefinition { Id = "elma", Name = "Elma", Hp = 950, Speed = 115, PAtk = 400, MAtk = 30, PDef = 40, MDef = 30, Type = AttackType.Physical, IsEnemy = false },
            ["lumi"] = new CharacterDefinition { Id = "lumi", Name = "Lumi", Hp = 850, Speed = 80, PAtk = 30, MAtk = 400, PDef = 35, MDef = 100, Type = AttackType.Magic, IsEnemy = false, IdleTextureName = "Char_Lumi_IDLE", AttackTextureName = "Char_Lumi_ATTACK" },
            ["veyra"] = new CharacterDefinition { Id = "veyra", Name = "Veyra", Hp = 1000, Speed = 90, PAtk = 40, MAtk = 140, PDef = 70, MDef = 110, Type = AttackType.Magic, IsEnemy = false },
            ["enos"] = new CharacterDefinition { Id = "enos", Name = "Enos", Hp = 1250, Speed = 70, PAtk = 120, MAtk = 120, PDef = 200, MDef = 120, Type = AttackType.Physical, IsEnemy = false }
        };

        // ---------- ศัตรูทั้งหมดที่มีในเกม ----------
        public static readonly Dictionary<string, CharacterDefinition> Enemies = new Dictionary<string, CharacterDefinition>
        {
            ["orc_warrior"] = new CharacterDefinition { Id = "orc_warrior", Name = "Orc Warrior", Hp = 970, Speed = 85, PAtk = 170, MAtk = 20, PDef = 55, MDef = 30, Type = AttackType.Physical, IsEnemy = true },
            ["giant_hornet"] = new CharacterDefinition { Id = "giant_hornet", Name = "Giant Hornet", Hp = 900, Speed = 110, PAtk = 150, MAtk = 25, PDef = 35, MDef = 50, Type = AttackType.Physical, IsEnemy = true },
            ["wolf"] = new CharacterDefinition { Id = "wolf", Name = "Wolf", Hp = 800, Speed = 90, PAtk = 190, MAtk = 25, PDef = 30, MDef = 85, Type = AttackType.Physical, IsEnemy = true },
            ["ghost_healer"] = new CharacterDefinition { Id = "ghost_healer", Name = "Ghost Healer", Hp = 950, Speed = 75, PAtk = 35, MAtk = 150, PDef = 45, MDef = 110, Type = AttackType.Magic, IsEnemy = true },
            ["stone_golem"] = new CharacterDefinition { Id = "stone_golem", Name = "Stone Golem", Hp = 1600, Speed = 60, PAtk = 160, MAtk = 10, PDef = 95, MDef = 40, Type = AttackType.Physical, IsEnemy = true },

            // บอสประจำ Challenge Mode: เจอทุกๆ 5 ชั้น เป็นศัตรูตัวเดียวแต่สเตตัสสูงกว่าศัตรูทั่วไปมาก
            ["boss_slime_king"] = new CharacterDefinition
            {
                Id = "boss_slime_king",
                Name = "Boss Slime King",
                Hp = 6000,
                Speed = 80,
                PAtk = 260,
                MAtk = 220,
                PDef = 110,
                MDef = 100,
                Type = AttackType.Physical,
                IsEnemy = true,
                // Gate 3 ช่อง: ตีครบ 3 ครั้ง ครั้งถัดไปตี 5 เป้าหมาย ดาเมจ 40% และมอบ Stun ให้ทุกตัวที่โดน
                GateMax = 3,
                BurstTargets = 5,
                BurstDamageRate = 0.4f,
                BurstStunTurns = 1
            },

            // บอสทูตสวรรค์: ตีแล้วมอบ Blessing 2 Stack ให้เป้าหมาย มี Gate 3 ช่อง ตีครบ 3 ครั้ง ครั้งถัดไปตี 5 เป้าหมาย มอบ Blessing 5 Stack ให้ทุกคน
            ["boss_slime_angel"] = new CharacterDefinition
            {
                Id = "boss_slime_angel",
                Name = "Boss Slime Angel",
                Hp = 6500,
                Speed = 85,
                PAtk = 40,
                MAtk = 40,
                PDef = 500,
                MDef = 500,
                Type = AttackType.Physical,
                IsEnemy = true,
                GateMax = 3,
                OnHitBlessing = 2,
                BurstBlessing = 5,
                BurstTargets = 5
            },
        };

        // ศัตรูที่สุ่มเจอในด่านปกติ (สุ่มจำนวน (ตอนนี้ตั้งไว้ 5 ตัวเต็มทีม) แต่ละตัวสุ่มจากกลุ่มนี้ ซ้ำตัวได้)
        public static readonly string[] EnemyPool = { "giant_hornet", "wolf", "stone_golem" };
        public const int EnemyWaveMin = 5;   // จำนวนศัตรูต่อทีมขั้นต่ำ (สุ่มตั้งแต่ Min ถึง Max รวมตัว Max)
        public const int EnemyWaveMax = 5;

        private static readonly System.Random _waveRng = new System.Random();

        public static Character CreateAlly(string id) => Allies[id].CreateInstance();

        public static Character CreateEnemy(string id) => Enemies[id].CreateInstance();

        public static List<Character> CreateStartingParty()
        {
            var party = new List<Character>();
            foreach (var def in Allies.Values)
                party.Add(def.CreateInstance());
            return party;
        }

        // โอกาสที่ศัตรูสุ่มได้สี (ที่เหลือเป็นปกติ) แล้วสุ่มเท่าๆ กันระหว่าง Red / Green / Purple / Blue
        public const float VariantChance = 0.75f;

        // ใส่สี (Elite) ให้ศัตรู: เติมชื่อนำ, ปรับสเตตัส (% ของค่าฐาน), มี Gate 2 ช่องเมื่อ Gate เต็มจะติดสถานะตามสี
        public static void ApplyVariant(Character c, EnemyVariant v)
        {
            if (c == null || v == EnemyVariant.None) return;
            float hp, atk, spd, def;
            switch (v)
            {
                case EnemyVariant.Red: hp = 10f; atk = 8f; spd = -5f; def = 7f; break;  // Fire
                case EnemyVariant.Green: hp = 2f; atk = 4f; spd = 2f; def = -7f; break;  // Poison
                case EnemyVariant.Blue: hp = 0f; atk = 0f; spd = 0f; def = 0f; break;  // Blue: Shield ให้ทั้งทีม (ไม่ปรับสเตตัส)
                default: hp = -12f; atk = 10f; spd = 6f; def = 15f; break;  // Purple: Weakness
            }
            c.Name = v + " " + c.Name;
            c.Variant = v;
            c.ModifyBaseStat(StatKind.MaxHp, hp, true);
            c.ModifyBaseStat(StatKind.AllAtk, atk, true);
            c.ModifyBaseStat(StatKind.Speed, spd, true);
            c.ModifyBaseStat(StatKind.PDef, def, true);
            c.ModifyBaseStat(StatKind.MDef, def, true);
            c.GateMax = 2;
            c.BurstTargets = 1;
            c.Hp = c.MaxHp;
        }

        public static List<Character> CreateEnemyWave(string[] waveIds = null)
        {
            bool randomWave = waveIds == null;
            if (waveIds == null)
            {
                waveIds = new string[_waveRng.Next(EnemyWaveMin, EnemyWaveMax + 1)];
                for (int i = 0; i < waveIds.Length; i++)
                    waveIds[i] = EnemyPool[_waveRng.Next(EnemyPool.Length)];
            }
            var wave = new List<Character>();
            foreach (var id in waveIds)
            {
                var e = Enemies[id].CreateInstance();
                if (randomWave && _waveRng.NextDouble() < VariantChance)
                    ApplyVariant(e, (EnemyVariant)_waveRng.Next(1, 5));
                wave.Add(e);
            }
            return wave;
        }
    }
}