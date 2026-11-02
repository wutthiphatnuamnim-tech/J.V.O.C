using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // เกมวาดทุกอย่างในพื้นที่เสมือน 1920x1080 แล้วย่อ/ขยายพร้อมแถบดำ (letterbox) ให้พอดีหน้าจอจริง
    // คลาสนี้แปลงตำแหน่งเมาส์จากพิกเซลหน้าจอจริงกลับเป็นพิกัด 1920x1080 ที่เกมใช้
    public static class VirtualMouse
    {
        public const int Width = 1920;
        public const int Height = 1080;

        public static float Scale { get; private set; } = 1f;
        public static Vector2 Offset { get; private set; } = Vector2.Zero;

        public static void Update(int backBufferWidth, int backBufferHeight)
        {
            if (backBufferWidth <= 0 || backBufferHeight <= 0) return;
            Scale = Math.Min(backBufferWidth / (float)Width, backBufferHeight / (float)Height);
            Offset = new Vector2((backBufferWidth - Width * Scale) / 2f, (backBufferHeight - Height * Scale) / 2f);
        }

        // พื้นที่บนหน้าจอจริงที่ใช้แสดงภาพเกม
        public static Rectangle DestRectangle =>
            new Rectangle((int)Math.Round(Offset.X), (int)Math.Round(Offset.Y), (int)Math.Round(Width * Scale), (int)Math.Round(Height * Scale));

        public static MouseState GetState()
        {
            MouseState m = Mouse.GetState();
            int x = (int)Math.Round((m.X - Offset.X) / Scale);
            int y = (int)Math.Round((m.Y - Offset.Y) / Scale);
            return new MouseState(x, y, m.ScrollWheelValue, m.LeftButton, m.MiddleButton, m.RightButton, m.XButton1, m.XButton2);
        }
    }
}