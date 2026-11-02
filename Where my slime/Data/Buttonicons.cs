using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // ไอคอนปุ่มในหน้า Battle: sprite sheet ช่องละ 64x64 เรียงแนวนอน (Menu, Auto, Pause, Settings)
    public static class ButtonIcons
    {
        public const int Menu = 0;
        public const int Auto = 1;
        public const int Pause = 2;
        public const int Settings = 3;

        public static Texture2D Sheet;
        private const int Cell = 64;

        // fromFile = รูปจากไฟล์ UI/Button_Icons.png (ถ้าไม่มีให้ส่ง null แล้วจะใช้รูปที่ฝังในโค้ดแทน)
        public static void Load(GraphicsDevice device, Texture2D fromFile)
        {
            if (fromFile != null) { Sheet = fromFile; return; }
            try
            {
                byte[] bytes = Convert.FromBase64String(Base64);
                using (var stream = new MemoryStream(bytes)) Sheet = Texture2D.FromStream(device, stream);
            }
            catch { Sheet = null; }
        }

        public static bool Draw(SpriteBatch sb, int index, Rectangle dest, Color tint)
        {
            if (Sheet == null) return false;
            sb.Draw(Sheet, dest, new Rectangle(index * Cell, 0, Cell, Cell), tint);
            return true;
        }

        private const string Base64 = "iVBORw0KGgoAAAANSUhEUgAAAQAAAABACAYAAAD1Xam+AAADdElEQVR4nO3cUXKrMAyFYbhzN8P+F0N3Q1/KDOMELMmWZOD/HtskdgCdCEMyTQAAAAAA4A3m8g/btm0ZE4kyz/PHewai1Oor+vj8HznY22nClaBCBALAmbWjOj6PMIAXAsBB79MowgBeqgFw9wMuck0jYqx9jKz90voea/P2fv1W5fyy66N1Pv/6Tue9ohdPn75YixicAjTKLMTsbgD3RwA0sBS/pFi1r7tt20YIwIIAMPK8pHd8vHQcQsBHbft7Lvj2eH7tmCAADCQ7qVcxasKAEIAWi4BKkcVveV0WB6FBAChIbuP0/gSWjCEJAYIC02Q4BYg+cMqDPete6tHu4Z7neb6a09npAFcOdLTHXzbuA0iQVUyacbc/nvPB/bAIKHBVONmfpFedwNnfs+eMcdABVHgXf49PZQoaVgTAyxAWOCIAjHp++nNujiysAVTshT7qV3L5kRG0IACERi2ebwEFSKkDILsQssfvpSzY1tt4a/cFPGW73U3rdvcOdtYAHiTiTkQ8CwGQzKNgy9ckFHCGAEigvXHHgm4AEnwXINE+19r5e48xIHP3xVRtfdABAC9GAASL/oUZ4AoBALzYxxrAz8/P5RNq//emHT97vqV9PsuyfKzUr+s6/Ke/9/bM3l/Z45e850MHEEha4HcIAjwDtwInocgxAjqAINqCJyAQQd0BlOeu0SLHX9d1y36/iFXb36MHs/Z4pQOoWNd1G32nA1YEwImy6FtCwPpcggfeCAAFugE8jXoNILoAynOa2vg9ztlrY/RaGzi+BsHyTKPvVzoAI2k3cPaYMkDOAmX0Awj3xn0ABWnBjXp1YJ//qPPDWOgAlEYurGN4sV4BCQLg4Kpg9sLfH9Oz/a/9vWUsQgBXCICKsvCBJ2EN4E9Z4JJWv3Y1wHK6YHmOpHMBvqEDKCzLMktX6KcpvzOg+NGC7wJM7df1s74zkB0+b3C3baztZOkAJlmojPYlkYgbovB8BICCJAS8g0AyBsUPKQJASbo46DG25HUpfmhwFcBgWZbq7/cd/9+6vqCZl3UcfFf7Lop2m/fu3lrnQwAYSUJgpw0DSwdB8cOCAGigCYGdx+kBxQ8rAqBR5p2CFD5afRxAd7vuqeVZNJHbjuK/p9Gu4NABdBTRDVD46IkAcND7l34oenghAJxZw4CiBwAAAAAAQC+/Mw5NwWhfwGoAAAAASUVORK5CYII=";
    }
}