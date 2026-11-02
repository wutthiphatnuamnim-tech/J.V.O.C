using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // ผลการเปิดกล่อง: เหรียญ + ไอเท็ม 1 ชิ้น
    public class ChestReward
    {
        public int Coins;
        public Item Item;
    }

    // กฎของกล่องสมบัติ: ด่านไหนได้กล่องอะไร / กล่องแต่ละใบให้อะไร (ระดับกล่องใช้ ItemRarity เดียวกับไอเท็ม)
    public static class ChestSystem
    {
        public const int HighFloorStart = 10;

        // โอกาสที่ด่านธรรมดา/ด่านยากจะมีกล่อง (ด่าน Boss ได้กล่องเสมอ) ปรับตัวเลขนี้เพื่อให้กล่องออกบ่อย/น้อยลง
        public const double ChestDropChance = 0.6;

        // สุ่มระดับกล่องของด่าน (สุ่มเท่ากันในช่วงที่กำหนด)
        // floorNumber = ชั้นที่กำลังจะไปสู้ (นับจาก 1)
        public static ItemRarity RollChestTier(bool isHard, bool isBoss, int floorNumber, Random rng)
        {
            ItemRarity lo, hi;
            if (isBoss) { lo = ItemRarity.Legendary; hi = ItemRarity.Secret; }
            else if (floorNumber >= HighFloorStart)
            {
                if (isHard) { lo = ItemRarity.Rare; hi = ItemRarity.Mythic; }
                else { lo = ItemRarity.Uncommon; hi = ItemRarity.Epic; }
            }
            else
            {
                if (isHard) { lo = ItemRarity.Uncommon; hi = ItemRarity.Legendary; }
                else { lo = ItemRarity.Common; hi = ItemRarity.Rare; }
            }
            return (ItemRarity)rng.Next((int)lo, (int)hi + 1);
        }

        public static void CoinRange(ItemRarity chest, out int min, out int max)
        {
            switch (chest)
            {
                case ItemRarity.Common: min = 5; max = 10; break;
                case ItemRarity.Uncommon: min = 20; max = 35; break;
                case ItemRarity.Rare: min = 55; max = 110; break;
                case ItemRarity.Epic: min = 250; max = 600; break;
                case ItemRarity.Legendary: min = 700; max = 1100; break;
                case ItemRarity.Mythic: min = 1250; max = 2000; break;
                case ItemRarity.Secret: min = 2007; max = 2100; break;
                default: min = 5; max = 10; break;
            }
        }

        // น้ำหนักโอกาส (เปอร์เซ็นต์) ของระดับไอเท็มในกล่องแต่ละใบ
        private static KeyValuePair<ItemRarity, double>[] ItemWeights(ItemRarity chest)
        {
            KeyValuePair<ItemRarity, double> W(ItemRarity r, double w) => new KeyValuePair<ItemRarity, double>(r, w);
            switch (chest)
            {
                case ItemRarity.Common: return new[] { W(ItemRarity.Common, 80), W(ItemRarity.Uncommon, 15), W(ItemRarity.Rare, 5) };
                case ItemRarity.Uncommon: return new[] { W(ItemRarity.Common, 10), W(ItemRarity.Uncommon, 75), W(ItemRarity.Rare, 10), W(ItemRarity.Epic, 5) };
                case ItemRarity.Rare: return new[] { W(ItemRarity.Uncommon, 20), W(ItemRarity.Rare, 70), W(ItemRarity.Epic, 8), W(ItemRarity.Legendary, 2), W(ItemRarity.Mythic, 0.1) };
                case ItemRarity.Epic: return new[] { W(ItemRarity.Rare, 10), W(ItemRarity.Epic, 75), W(ItemRarity.Legendary, 10), W(ItemRarity.Mythic, 0.5) };
                case ItemRarity.Legendary: return new[] { W(ItemRarity.Epic, 20), W(ItemRarity.Legendary, 50), W(ItemRarity.Mythic, 30) };
                case ItemRarity.Mythic: return new[] { W(ItemRarity.Legendary, 20), W(ItemRarity.Mythic, 80), W(ItemRarity.Secret, 0.01) };
                case ItemRarity.Secret: return new[] { W(ItemRarity.Mythic, 50), W(ItemRarity.Secret, 50) };
                default: return new[] { W(ItemRarity.Common, 100) };
            }
        }

        public static ChestReward Open(ItemRarity chest, Random rng)
        {
            CoinRange(chest, out int min, out int max);
            var reward = new ChestReward { Coins = rng.Next(min, max + 1) };

            var table = ItemWeights(chest);
            double total = 0; foreach (var t in table) total += t.Value;
            double roll = rng.NextDouble() * total;
            ItemRarity picked = table[table.Length - 1].Key;
            foreach (var t in table)
            {
                if (roll < t.Value) { picked = t.Key; break; }
                roll -= t.Value;
            }

            // ถ้าระดับที่สุ่มได้ยังไม่มีไอเท็มในเกม ให้ถอยไประดับที่ต่ำกว่าที่มีไอเท็ม
            var pool = ItemPool.GetByRarity(picked);
            for (int r = (int)picked - 1; pool.Count == 0 && r >= 0; r--) pool = ItemPool.GetByRarity((ItemRarity)r);
            if (pool.Count > 0) reward.Item = pool[rng.Next(pool.Count)].Clone();
            return reward;
        }
    }

    // รูปกล่องสมบัติ 8 ระดับ ช่องละ 128x128 เรียงแนวนอนตามลำดับ ItemRarity (Common..Unique)
    public static class ChestIcons
    {
        public static Texture2D Sheet;
        private const int Cell = 128;

        public static void Load(GraphicsDevice device, Texture2D fromFile)
        {
            if (fromFile != null) { Sheet = fromFile; return; }
            try
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(Base64)))
                    Sheet = Texture2D.FromStream(device, stream);
            }
            catch { Sheet = null; }
        }

        // ส่วน yFrom..yFrom+height ของรูปกล่อง (ใช้แยกฝา/ตัวกล่อง)
        public static Rectangle Source(ItemRarity r, int yFrom = 0, int height = Cell)
            => new Rectangle((int)r * Cell, yFrom, Cell, height);

        public static bool Draw(SpriteBatch sb, ItemRarity r, Rectangle dest, float alpha = 1f)
        {
            if (Sheet == null) return false;
            sb.Draw(Sheet, dest, Source(r), Color.White * alpha);
            return true;
        }

        private const string Base64 = "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";
    }
}