using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Where_My_Slime.Data
{
    // ตัดเฟรมจากสไปรต์ชีตที่เรียงเฟรมแนวนอน
    public static class SpriteFrames
    {
        // Veyra_Idle.png: 4 เฟรม เฟรมละ 240x320
        public const int VeyraFrameWidth = 240;
        // Veyra_Attack.png: 5 เฟรม เฟรมละ 320x320
        public const int VeyraAttackFrameWidth = 320;
        // char_Enos_Idle.png: 4 เฟรม เฟรมละ 480x480
        public const int EnosFrameWidth = 480;

        public static int FrameCount(Texture2D tex, int frameWidth)
        {
            if (tex == null || frameWidth <= 0) return 1;
            return System.Math.Max(1, tex.Width / frameWidth);
        }

        public static Rectangle Frame(Texture2D tex, int index, int frameWidth)
        {
            int count = FrameCount(tex, frameWidth);
            int i = ((index % count) + count) % count;
            return new Rectangle(i * frameWidth, 0, frameWidth, tex.Height);
        }

        public static Rectangle VeyraFrame(Texture2D tex, int index) => Frame(tex, index, VeyraFrameWidth);
        public static Rectangle VeyraAttackFrame(Texture2D tex, int index) => Frame(tex, index, VeyraAttackFrameWidth);
        public static Rectangle EnosFrame(Texture2D tex, int index) => Frame(tex, index, EnosFrameWidth);

        // สไปรต์ชีตแบบตาราง (เฟรมละ 480x480 เรียงซ้ายไปขวา หมดแถวแล้วลงแถวถัดไป) ใช้กับ Enos Attack (3 เฟรม) และ Skill (18 เฟรม)
        public static int GridFrameCount(Texture2D tex, int size = EnosFrameWidth)
        {
            if (tex == null) return 1;
            return System.Math.Max(1, (tex.Width / size) * (tex.Height / size));
        }

        public static Rectangle GridFrame(Texture2D tex, int index, int size = EnosFrameWidth)
        {
            int cols = System.Math.Max(1, tex.Width / size);
            int count = GridFrameCount(tex, size);
            int i = ((index % count) + count) % count;
            return new Rectangle((i % cols) * size, (i / cols) * size, size, size);
        }
    }
}