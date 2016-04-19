using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using FezEngine.Mod;

namespace FezGame.Components
{
    public class ADEIntro : CustomIntro {
        
        private SpriteBatch spriteBatch;
                
        private Texture2D logoTL;
        private Texture2D logoDR;
        private float offset;
        private float opacity;
        
        public ADEIntro(Intro intro)
            : base(intro) {
        }
        
        public override void LoadContent() {
            base.LoadContent();
            
            logoTL = cm.Load<Texture2D>("other textures/splash/ade_tl");
            logoDR = cm.Load<Texture2D>("other textures/splash/ade_dr");
            
            spriteBatch = new SpriteBatch(Intro.GraphicsDevice);
        }
        
        public override void Reset() {
            base.Reset();
            
            offset = 0f;
            opacity = 0f;
        }
        
        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            
            double time = phaseTime.TotalSeconds;
            if (phase == Phase.Wait) {
                ChangePhase();
                time = 0.0;
            }
            switch (phase) {
                case Phase.FadeIn:
                    if (time >= 1.0) {
                        ChangePhase();
                        break;
                    }
                    opacity = Easing.EaseIn(FezMath.Saturate(time * 8.0), EasingType.Quadratic);
                    //offset = 1f + (float) Math.Cos(Math.PI * (time / 1.0)) + 1f;
                    offset = 1f + Easing.EaseIn(1f - time, EasingType.Quadratic);
                    break;
                case Phase.FadeOut:
                    if (time >= 0.5) {
                        ChangePhase();
                        opacity = 0f;
                        break;
                    }
                    opacity = 1f - Easing.EaseOut(Math.Max((time - 0.25) / 0.25, 0.0), EasingType.Quadratic);
                    offset = (float) Math.Cos(Math.PI * (time / 1.0));
                    break;
            }
        }
        
        public override void Draw(GameTime gameTime) {
            Intro.GraphicsDevice.Clear(Color.White);
            
            Vector2 screenSize = new Vector2(Intro.GraphicsDevice.Viewport.Width, Intro.GraphicsDevice.Viewport.Height);
            Vector2 screenMid = FezMath.Round(screenSize / 2f);
            
            float viewScale = Intro.GraphicsDevice.GetViewScale();
            
            Vector2 offsTL = 0.25f * new Vector2(logoTL.Width, -logoTL.Height) * (1f - offset);
            Vector2 offsDR = -offsTL;
            
            spriteBatch.BeginPoint();
            spriteBatch.Draw(
                logoTL,
                screenMid - FezMath.Round(new Vector2(logoTL.Width, logoTL.Height) / 2f) + offsTL,
                new Color(1f, 1f, 1f, opacity)
            );
            spriteBatch.Draw(
                logoDR,
                screenMid - FezMath.Round(new Vector2(logoTL.Width, logoDR.Height) / 2f) + offsDR,
                new Color(1f, 1f, 1f, opacity)
            );
            spriteBatch.End();
        }
        
    }
}