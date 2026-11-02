using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าโลโก้เปิดเกม: โชว์ภาพโลโก้ไว้ช่วงเวลาสั้นๆ แล้วไปหน้า Intro
    public class LogoScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _logoBackground;

        private readonly Action _goToIntro;

        private float _logoTimer;
        private const float FitMargin = 0.7f; // โลโก้กินพื้นที่สูงสุดกี่ส่วนของจอ (เว้นขอบไว้)
        private const float FadeIn = 0.7f;
        private const float Hold = 1.2f;
        private const float FadeOut = 0.7f;
        private const float LogoDuration = FadeIn + Hold + FadeOut;
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public LogoScreen(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Texture2D logoBackground, Action goToIntro)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _logoBackground = logoBackground;
            _goToIntro = goToIntro;
        }

        public void Reset()
        {
            _logoTimer = 0f;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, KeyboardState previousKeyboard, bool leftClicked)
        {
            _logoTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!previousKeyboard.IsKeyDown(pressedKeys[i]))
                {
                    anyNewKeyPressed = true;
                    break;
                }
            }

            // กดข้าม: กระโดดไปเริ่ม Fade ออกเลย (ไม่ตัดฉับ)
            if ((leftClicked || anyNewKeyPressed) && _logoTimer < FadeIn + Hold)
                _logoTimer = FadeIn + Hold;

            if (_logoTimer >= LogoDuration)
            {
                _goToIntro?.Invoke();
            }
        }

        public void Draw()
        {
            // Fade เข้า -> ค้าง -> Fade ออก
            float a = _logoTimer < FadeIn ? _logoTimer / FadeIn
                    : _logoTimer < FadeIn + Hold ? 1f
                    : 1f - (_logoTimer - FadeIn - Hold) / FadeOut;
            a = MathHelper.Clamp(a, 0f, 1f);

            if (_logoBackground != null)
            {
                // อยู่กลางจอ เว้นขอบ รักษาสัดส่วนภาพ (พื้นหลังหน้านี้เป็นสีดำ)
                float scale = Math.Min(ScreenWidth * FitMargin / _logoBackground.Width, ScreenHeight * FitMargin / _logoBackground.Height);
                int w = (int)(_logoBackground.Width * scale);
                int h = (int)(_logoBackground.Height * scale);
                _spriteBatch.Draw(_logoBackground, new Rectangle((ScreenWidth - w) / 2, (ScreenHeight - h) / 2, w, h), Color.White * a);
            }
            else
                DrawTextCentered("LOGO", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.White * a, 3f);
        }

        private string GetSafeText(string text)
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
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeText(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}