using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using MonoMod;

namespace FezGame.Components {
	public class patch_GameNpcState : GameNpcState {
        
        public ISpeechBubbleManager SpeechManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        
        public patch_GameNpcState(Game game, NpcInstance npc)
            : base(game, npc) {
            //no-op
        }
        
        private extern void orig_Talk();
        private void Talk() {
            orig_Talk();
            
            Action action = () => ((patch_ISpeechBubbleManager) SpeechManager).Speaker = Npc.Name.ToLowerInvariant();
            
            if (PlayerManager.Action == ActionType.WalkingTo) {
                Waiters.Wait(() => PlayerManager.Action != ActionType.WalkingTo, action).AutoPause = true;
            } else {
                action();
            }
        }
        
    }
}