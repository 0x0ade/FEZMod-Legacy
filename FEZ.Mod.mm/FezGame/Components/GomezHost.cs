#pragma warning disable 436
using System;
using Microsoft.Xna.Framework;
using FezEngine.Effects;
using FezEngine.Structure;
using FezGame.Services;
using MonoMod;
using FezGame.Structure;
using FezGame.Mod;
using FezEngine.Services;
using FezEngine.Mod;

namespace FezGame.Components {
    public class GomezHost {

        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameCameraManager CameraManager { [MonoModIgnore] get; [MonoModIgnore] set; }
        public IGameStateManager GameState { [MonoModIgnore] get; [MonoModIgnore] set; }
        public ILevelManager LevelManager { [MonoModIgnore] get; [MonoModIgnore] set; }

        private GomezEffect effect;
        private readonly Mesh playerMesh;
        public static GomezHost Instance;
        private TimeSpan sinceBackgroundChanged;
        private bool lastBackground;
        private bool lastHideFez;


        public extern void orig_Draw(GameTime gameTime);
        public void Draw(GameTime gameTime) {
            if (FEZMod.CreatingThumbnail) {
                return;
            }
            orig_Draw(gameTime);
        }

    }
}

