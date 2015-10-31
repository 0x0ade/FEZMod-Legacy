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

        private bool dataUpdated = false;

        protected NetworkGomezData networkData;

        public void orig_Update(GameTime gameTime) {
        }

        public void Update(GameTime gameTime) {
            orig_Update(gameTime);
            if (NetworkGomezServer.Instance != null) {
                NetworkGomezServer.Instance.Action = NetworkGomezServer.Instance.Action ?? UpdateNetGomez;
            }
            dataUpdated = true;
        }

        public object UpdateNetGomez() {
            if (!dataUpdated) {
                return null;
            }
            dataUpdated = false;

            if (networkData == null) {
                networkData = new NetworkGomezData();
            }

            networkData.DataId++;

            if (networkData.DataId % NetworkGomezServer.SendModulo != 0) {
                return null;
            }

            networkData.Position = playerMesh.Position;
            networkData.Rotation = playerMesh.Rotation;
            networkData.Opacity = playerMesh.Material.Opacity;
            networkData.Background = PlayerManager.Background;
            networkData.Action = PlayerManager.Action;
            if (playerMesh.FirstGroup.TextureMatrix != null) {
                //FIXME
                /*Matrix? nullable = playerMesh.FirstGroup.TextureMatrix.Value;
                if (nullable != null) {
                  networkData.TextureMatrix = nullable.GetValueOrDefault();
                }*/
            }
            //networkData.EffectBackground = 0f;
            networkData.Scale = playerMesh.Scale;
            networkData.NoMoreFez = lastHideFez;

            networkData.Viewpoint = CameraManager.Viewpoint;

            networkData.InCutscene = GameState.InCutscene;
            networkData.InMap = GameState.InMap;
            networkData.InMenuCube = GameState.InMenuCube;
            networkData.Paused = GameState.Paused;

            networkData.Level = LevelManager.Name;

            return networkData;
        }

        public void orig_Draw(GameTime gameTime) {
        }

        public void Draw(GameTime gameTime) {
            if (FEZMod.CreatingThumbnail) {
                return;
            }
            orig_Draw(gameTime);
        }

    }
}

