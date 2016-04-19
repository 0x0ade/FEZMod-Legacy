using FezEngine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using FezGame.Mod;

namespace FezGame.Components
{
    public class CustomIntro {
        
        public Intro Intro;
        public ContentManager cm; //lower-case to avoid conflict with FezEngine.Services.CM
        
        protected Phase phase = Phase.FadeIn;
        protected TimeSpan phaseTime = new TimeSpan();
        
        public virtual int Index { get { return 0; } }
        
        public CustomIntro(Intro intro) {
            Intro = intro;
        }
        
        //We're loading our content with the real intro. LoadContent gets called in the helper.
        public virtual void LoadContent() {
            cm = Intro.CMProvider.Get(CM.Intro);
        }
        
        public virtual void Reset() {
            phase = Phase.FadeIn;
            phaseTime = new TimeSpan();
        }
        
        public virtual void Update(GameTime gameTime) {
            phaseTime += gameTime.ElapsedGameTime;
        }
        
        public virtual void Draw(GameTime gameTime) {
        }
        
        public virtual void ChangePhase() {
            switch (phase) {
                case Phase.FadeIn: phase = Phase.Wait; break;
                case Phase.Wait: phase = Phase.FadeOut; break;
                case Phase.FadeOut: CustomIntroHelper.ChangeIntro(); break;
            }
            phaseTime = new TimeSpan();
        }
        
        public enum Phase {
            FadeIn,
            Wait,
            FadeOut
        }
        
    }
}