using System;
using System.Collections.Generic;
using Where_My_Slime.Models;

namespace Where_My_Slime.Data
{
    public enum ItemRarity
    {
        Common,     // สีขาว
        Uncommon,   // สีเขียว
        Rare,       // สีฟ้า
        Epic,       // สีม่วง
        Legendary,  // สีทอง
        Mythic,     // สีแดง
        Secret,     // สีดำ
        Unique      // สีรุ้ง
    }

    // ช่องสวมใส่ของตัวละคร (ค่า enum ใช้เป็นลำดับ index ของช่องใน Character)
    public enum ItemSlot
    {
        Weapon,
        Armor,
        Additional
    }

    public class Item
    {
        public Item Clone() => (Item)MemberwiseClone();

        public string Name { get; set; }
        public string Description { get; set; }
        public ItemRarity Rarity { get; set; }
        public int Price { get; set; }

        // ชนิด/ช่องของไอเท็ม: ถ้าไม่ได้กำหนดเอง จะดูจากชื่อในตาราง ItemPool.SlotFor
        private ItemSlot? _slot;
        public ItemSlot Slot
        {
            get { return _slot ?? ItemPool.SlotFor(Name); }
            set { _slot = value; }
        }

        // ชื่อ Set ที่ไอเท็มนี้สังกัด (เตรียมไว้สำหรับโบนัส Set ในอนาคต; null = ไม่มี Set)
        public string SetName { get; set; }

        // สเตตัสหลัก (Primary Stat)
        public StatKind Stat { get; set; }
        public float Amount { get; set; }
        public bool IsPercent { get; set; }

        // สเตตัสรอง (Secondary Stat - เช่น M.ATK ของ Necklace หรือ M.Def ของ Dragon Mail)
        public StatKind SecondaryStat { get; set; }
        public float SecondaryAmount { get; set; }
        public bool SecondaryIsPercent { get; set; }

        // สเตตัสที่สาม (Tertiary Stat - สำหรับไอเท็มระดับ Mythic)
        public StatKind TertiaryStat { get; set; }
        public float TertiaryAmount { get; set; }
        public bool TertiaryIsPercent { get; set; }
    }

    public static class ItemPool
    {
        // ตารางกำหนดช่องของไอเท็มแต่ละชิ้น (ชื่อไหนไม่อยู่ในตารางจะเป็น Additional)
        private static readonly Dictionary<string, ItemSlot> SlotTable = new Dictionary<string, ItemSlot>
        {
            // Weapon
            ["Broken Blade"] = ItemSlot.Weapon,
            ["Stick"] = ItemSlot.Weapon,
            ["Sharp Blade"] = ItemSlot.Weapon,
            ["Arcane Orb"] = ItemSlot.Weapon,
            ["Berserk Fang"] = ItemSlot.Weapon,
            ["Twin Blade"] = ItemSlot.Weapon,
            ["Magic Gloves"] = ItemSlot.Weapon,
            ["Ancient Katana"] = ItemSlot.Weapon,
            ["Ancient Staff"] = ItemSlot.Weapon,
            ["Backsword (seal)"] = ItemSlot.Weapon,
            ["Evil Grimoire"] = ItemSlot.Weapon,
            ["Fallen Angel Magic Sword"] = ItemSlot.Weapon,

            // Armor
            ["Wood Plate"] = ItemSlot.Armor,
            ["Iron Plate"] = ItemSlot.Armor,
            ["Platinum Plate"] = ItemSlot.Armor,
            ["Tunic"] = ItemSlot.Armor,
            ["Dragon Mail"] = ItemSlot.Armor,
            ["Shogun Armor"] = ItemSlot.Armor,
            ["Ancient Armor"] = ItemSlot.Armor,
            ["Black&White Coat"] = ItemSlot.Armor,
            ["Fallen Angel Armor"] = ItemSlot.Armor,
            ["Fast Boots"] = ItemSlot.Armor,
            ["Swift Boots"] = ItemSlot.Armor
        };

        public static ItemSlot SlotFor(string name)
        {
            return name != null && SlotTable.TryGetValue(name, out var slot) ? slot : ItemSlot.Additional;
        }

        private static List<Item> _allItems;

        // รายการไอเท็มทั้งหมดในเกม (สร้างครั้งเดียวแล้วเก็บไว้)
        public static List<Item> AllItems => _allItems ?? (_allItems = BuildMasterPool());

        // ไอเท็มทั้งหมดของระดับที่ระบุ (ใช้กับกล่องสมบัติ)
        public static List<Item> GetByRarity(ItemRarity rarity)
        {
            var list = new List<Item>();
            foreach (var i in AllItems) if (i.Rarity == rarity) list.Add(i);
            return list;
        }

        public static List<Item> GetRandomItems(int count, Random rng)
        {
            // Secret / Unique หาได้จากกล่องสมบัติเท่านั้น: ไม่ติดมากับรางวัลเลือกไอเท็มหรือร้านค้า
            List<Item> masterPool = AllItems.FindAll(i => i.Rarity != ItemRarity.Secret && i.Rarity != ItemRarity.Unique);

            List<Item> results = new List<Item>();
            for (int i = 0; i < count; i++)
            {
                int index = rng.Next(masterPool.Count);
                results.Add(masterPool[index].Clone());
            }

            return results;
        }

        private static List<Item> BuildMasterPool()
        {
            return new List<Item>()
            {
                // ---------- Common (ระดับ Common - สีขาว) ----------
                new Item { Name = "Broken Blade", Description = "P.ATK +4%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.PAtk, Amount = 0.04f, IsPercent = true },
                new Item { Name = "Stick", Description = "M.ATK +4%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MAtk, Amount = 0.04f, IsPercent = true },
                new Item { Name = "Wood Plate", Description = "P.Def +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.PDef, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Broken Charm", Description = "M.Def +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MDef, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Health Ring", Description = "HP +50", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MaxHp, Amount = 50f, IsPercent = false },
                new Item { Name = "Fast Boots", Description = "Speed +8", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.Speed, Amount = 8f, IsPercent = false },
                new Item { Name = "Clover Leaf", Description = "Crit Rate +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.CritRate, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Grass Ring", Description = "HP +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MaxHp, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Broken Spirit", Description = "Crit DMG +12%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.CritDamage, Amount = 0.12f, IsPercent = true },

                // ---------- Uncommon (ระดับ Uncommon - สีเขียว) ----------
                new Item { Name = "Sharp Blade", Description = "P.ATK +15%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PAtk, Amount = 0.15f, IsPercent = true },
                new Item { Name = "Arcane Orb", Description = "M.ATK +15%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MAtk, Amount = 0.15f, IsPercent = true },
                new Item { Name = "Iron Plate", Description = "P.Def +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PDef, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Ward Charm", Description = "M.Def +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MDef, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Vitality Ring", Description = "HP +150", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MaxHp, Amount = 150f, IsPercent = false },
                new Item { Name = "Swift Boots", Description = "Speed +18", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.Speed, Amount = 18f, IsPercent = false },
                new Item { Name = "Hawk Eye", Description = "Crit Rate +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.CritRate, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Lucky Coin", Description = "Coin Gain +25%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PAtk, Amount = 0f, IsPercent = false },
                new Item { Name = "Berserk Fang", Description = "Crit DMG +35%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.CritDamage, Amount = 0.35f, IsPercent = true },

                // ---------- Rare (ระดับ Rare - สีฟ้า) ----------
                new Item { Name = "Twin Blade", Description = "P.ATK +30%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.PAtk, Amount = 0.30f, IsPercent = true },
                new Item { Name = "Magic Gloves", Description = "M.ATK +30%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.MAtk, Amount = 0.30f, IsPercent = true },
                new Item { Name = "Platinum Plate", Description = "P.Def +35%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.PDef, Amount = 0.35f, IsPercent = true },
                new Item { Name = "Tunic", Description = "M.Def +35%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.MDef, Amount = 0.35f, IsPercent = true },
                new Item {
                    Name = "Shogun Armor",
                    Description = "HP +200, P.ATK +145",
                    Rarity = ItemRarity.Rare,
                    Price = 50,
                    Stat = StatKind.MaxHp, Amount = 200f, IsPercent = false,
                    SecondaryStat = StatKind.PAtk, SecondaryAmount = 145f, SecondaryIsPercent = false
                },
                new Item {
                    Name = "Beast Ring",
                    Description = "Crit DMG +120% decrease MaxHP 80%",
                    Rarity = ItemRarity.Rare,
                    Price = 50,
                    Stat = StatKind.CritDamage, Amount = 1.20f, IsPercent = true,
                    SecondaryStat = StatKind.MaxHp, SecondaryAmount = -0.80f, SecondaryIsPercent = true
                },

                // ---------- Epic (ระดับ Epic - สีม่วง) ----------
                new Item { Name = "Ancient Katana", Description = "P.ATK +200 \n(P.ATK + 14% if STR > 15)", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.PAtk, Amount = 200f, IsPercent = false },
                new Item { Name = "Ancient Staff", Description = "M.ATK +200 \n(M.ATK + 14% if INT > 15)", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.MAtk, Amount = 200f, IsPercent = false },
                new Item { Name = "Demon Eye", Description = "Crit Rate +100% if CRT > 15", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.CritRate, Amount = 1.0f, IsPercent = true },
                new Item {
                    Name = "Ancient Armor",
                    Description = "HP +1500, All.Def +150",
                    Rarity = ItemRarity.Epic,
                    Price = 100,
                    Stat = StatKind.MaxHp, Amount = 1500f, IsPercent = false,
                    SecondaryStat = StatKind.PDef, SecondaryAmount = 150f, SecondaryIsPercent = false,
                    TertiaryStat = StatKind.MDef, TertiaryAmount = 150f, TertiaryIsPercent = false
                },

                // ---------- Legendary (ระดับ Legendary - สีทอง) ----------
                new Item {
                    Name = "Strongest Protection Necklace",
                    Description = "increase All.ATK * VIT(VIT * 1.3%)",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.PAtk, Amount = 0.013f, IsPercent = true,
                    SecondaryStat = StatKind.MAtk, SecondaryAmount = 0.013f, SecondaryIsPercent = true
                },
                new Item {
                    Name = "Dragon Mail",
                    Description = "Ignore All.Def + 90%, Speed +50",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.PDef, Amount = 0.90f, IsPercent = true,
                    SecondaryStat = StatKind.MDef, SecondaryAmount = 0.90f, SecondaryIsPercent = true,
                    TertiaryStat = StatKind.Speed, TertiaryAmount = 50f, TertiaryIsPercent = false
                },
                new Item {
                    Name = "Backsword (seal)",
                    Description = "P.ATK +400, Crit DMG +67, Crit Rate +56",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.PAtk, Amount = 400f, IsPercent = false,
                    SecondaryStat = StatKind.CritDamage, SecondaryAmount = 67f, SecondaryIsPercent = false,
                    TertiaryStat = StatKind.CritRate, TertiaryAmount = 56f, TertiaryIsPercent = false
                },
                new Item {
                    Name = "Evil Grimoire",
                    Description = "M.ATK +400, Crit DMG +67, Crit Rate +56",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.MAtk, Amount = 400f, IsPercent = false,
                    SecondaryStat = StatKind.CritDamage, SecondaryAmount = 67f, SecondaryIsPercent = false,
                    TertiaryStat = StatKind.CritRate, TertiaryAmount = 56f, TertiaryIsPercent = false
                },

                // ---------- Mythic (ระดับ Mythic - สีแดง) ----------
                new Item {
                    Name = "Black&White Coat",
                    Description = "HP +20%, Speed +70, All.ATK +2%",
                    Rarity = ItemRarity.Mythic,
                    Price = 500,
                    Stat = StatKind.MaxHp, Amount = 0.20f, IsPercent = true,
                    SecondaryStat = StatKind.Speed, SecondaryAmount = 70f, SecondaryIsPercent = false,
                    TertiaryStat = StatKind.AllAtk, TertiaryAmount = 0.02f, TertiaryIsPercent = true
                },
                new Item {
                    Name = "Ring of Legendary Hero",
                    Description = "P.ATK, M.ATK + 50%, MaxHP +4000 \nincrease Crit Rate/DMG 50% if CRT > 10",
                    Rarity = ItemRarity.Mythic,
                    Price = 500,
                    Stat = StatKind.PAtk, Amount = 0.50f, IsPercent = true,
                    SecondaryStat = StatKind.MAtk, SecondaryAmount = 0.50f, SecondaryIsPercent = true,
                    TertiaryStat = StatKind.MaxHp, TertiaryAmount = 4000f, TertiaryIsPercent = false
                },

                // ---------- Secret (ระดับ Secret - สีดำ) ----------
                new Item {
                    Name = "Fallen Angel Magic Sword",
                    Description = "All.ATK +1700, MaxHP +1200\nIgnore All.DEF 65%",
                    Rarity = ItemRarity.Secret,
                    Price = 2000,
                    Stat = StatKind.AllAtk, Amount = 1700f, IsPercent = false,
                    SecondaryStat = StatKind.MaxHp, SecondaryAmount = 1200f, SecondaryIsPercent = false
                },
                new Item {
                    Name = "Fallen Angel Armor",
                    Description = "P.DEF +400, M.DEF -90%, MaxHP +7000",
                    Rarity = ItemRarity.Secret,
                    Price = 2000,
                    Stat = StatKind.PDef, Amount = 400f, IsPercent = false,
                    SecondaryStat = StatKind.MDef, SecondaryAmount = -0.90f, SecondaryIsPercent = true,
                    TertiaryStat = StatKind.MaxHp, TertiaryAmount = 7000f, TertiaryIsPercent = false
                }
            };
        }
    }
}
namespace Where_My_Slime.Data
{
    // สีตามระดับไอเท็ม (Secret = ดำ, Unique = รุ้งไล่สีตามเวลา)
    public static class RarityColors
    {
        public static Microsoft.Xna.Framework.Color Rainbow()
        {
            float h = (System.Environment.TickCount % 3000) / 3000f * 6f;
            int i = (int)h; float f = h - i;
            float q = 1f - f;
            float r, g, b;
            switch (i % 6)
            {
                case 0: r = 1; g = f; b = 0; break;
                case 1: r = q; g = 1; b = 0; break;
                case 2: r = 0; g = 1; b = f; break;
                case 3: r = 0; g = q; b = 1; break;
                case 4: r = f; g = 0; b = 1; break;
                default: r = 1; g = 0; b = q; break;
            }
            return new Microsoft.Xna.Framework.Color(r, g, b);
        }
    }
}