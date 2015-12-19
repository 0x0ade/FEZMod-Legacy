using FezGame.Mod;
using FezGame.Structure;
using FezGame.Services;
using FezGame.Components;
using FezEngine.Mod;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;
using MonoMod;

namespace FezGame.Components.Actions {
    public class ReadSign : PlayerAction {
        
        public readonly static Color ColorBG = new Color(140, 92, 76); //darkest brown (post - right)
        public readonly static Color ColorFG = new Color(243, 234, 231); //white
        
        public ISpeechBubbleManager SpeechBubble { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public ReadSign(Game game)
            : base(game) {
            //no-op
        }
        
        [MonoModIgnore] public extern void orig_TestConditions();
        public void TestConditions() {
            if (!IsOnSign() || InputManager.CancelTalk != FezButtonState.Pressed) {
                return;
            }
            
            switch (this.PlayerManager.Action) {
                case ActionType.Teetering:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Idle:
                case ActionType.LookingLeft:
                case ActionType.LookingRight:
                case ActionType.LookingUp:
                case ActionType.LookingDown:
                case ActionType.Walking:
                case ActionType.Running:
                case ActionType.Sliding:
                case ActionType.Landing:
                    patch_ISpeechBubbleManager speechBubbleEXT = (patch_ISpeechBubbleManager) SpeechBubble;
                    //speechBubbleEXT.Speaker = "sign";
                    speechBubbleEXT.ColorBG = FEZModEngine.Settings.ModdedSpeechBubbles ? ColorBG : Color.Black;
                    speechBubbleEXT.ColorFG = FEZModEngine.Settings.ModdedSpeechBubbles ? ColorFG : Color.White;
                    orig_TestConditions();
                    break;
            }
        }
        
        [MonoModIgnore] private extern bool IsOnSign();
        
        [MonoModIgnore] protected override extern bool IsActionAllowed(ActionType type);
    }
}