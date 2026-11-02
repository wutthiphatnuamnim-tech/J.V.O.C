using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // ไอคอนไอเท็มทั้งหมดอยู่ในรูปเดียว (sprite sheet) ช่องละ 64x64 เรียง 7 คอลัมน์ ตามลำดับใน Order
    public static class ItemIcons
    {
        public static Texture2D Sheet;

        private const int Cell = 64;
        private const int Cols = 7;

        private static readonly string[] Order =
        {
            "Broken Blade",
            "Stick",
            "Wood Plate",
            "Broken Charm",
            "Health Ring",
            "Fast Boots",
            "Clover Leaf",
            "Broken Spirit",
            "Sharp Blade",
            "Arcane Orb",
            "Iron Plate",
            "Ward Charm",
            "Vitality Ring",
            "Swift Boots",
            "Hawk Eye",
            "Lucky Coin",
            "Berserk Fang",
            "Twin Blade",
            "Magic Gloves",
            "Platinum Plate",
            "Tunic",
            "Beast Ring",
            "Ancient Katana",
            "Ancient Staff",
            "Demon Eye",
            "Strongest Protection Necklace",
            "Dragon Mail",
            "Ring of Legendary Hero",
            "Grass Ring",
            "Shogun Armor",
            "Ancient Armor",
            "Backsword (seal)",
            "Evil Grimoire",
            "Black&White Coat",
            "Fallen Angel Magic Sword",
            "Fallen Angel Armor"
        };

        private static readonly Dictionary<string, int> Index = BuildIndex();

        private static Dictionary<string, int> BuildIndex()
        {
            var map = new Dictionary<string, int>();
            for (int i = 0; i < Order.Length; i++) map[Order[i]] = i;
            return map;
        }

        // fromFile = รูปที่โหลดจากไฟล์ Item/Items_Icons.png (ถ้าไม่มีให้ส่ง null แล้วจะใช้รูปที่ฝังในโค้ดแทน)
        public static void Load(GraphicsDevice device, Texture2D fromFile)
        {
            if (fromFile != null)
            {
                Sheet = fromFile;
                return;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(ItemIconsData.Base64);
                using (var stream = new MemoryStream(bytes))
                {
                    Sheet = Texture2D.FromStream(device, stream);
                }
            }
            catch
            {
                Sheet = null;
            }
        }

        // วาดไอคอนของไอเท็มลงในกรอบ dest คืนค่า false ถ้าไม่มีรูป/ไม่มีไอคอนของไอเท็มนี้ (ผู้เรียกจะได้ข้ามไป)
        public static bool Draw(SpriteBatch spriteBatch, Item item, Rectangle dest, float alpha = 1f)
        {
            if (Sheet == null || item == null || item.Name == null) return false;
            if (!Index.TryGetValue(item.Name, out int i)) return false;

            var src = new Rectangle((i % Cols) * Cell, (i / Cols) * Cell, Cell, Cell);
            spriteBatch.Draw(Sheet, dest, src, Color.White * alpha);
            return true;
        }
    }
}