using System;
using Microsoft.Xna.Framework;
using FezEngine.Effects;
using FezEngine.Structure;
using FezGame.Services;
using MonoMod;
using FezGame.Structure;
using FezGame.Mod;
using Common;

namespace FezGame.Components {
    public class GomezHost {

        public IPlayerManager PlayerManager { [MonoModIgnore] get; [MonoModIgnore] set; }

        private GomezEffect effect;
        private readonly Mesh playerMesh;
        public static GomezHost Instance;
        private TimeSpan sinceBackgroundChanged;
        private bool lastBackground;
        private bool lastHideFez;

        protected NetworkGomezData networkData;

        public void orig_Update(GameTime gameTime) {
        }

        public void Update(GameTime gameTime) {
            orig_Update(gameTime);
            if (NetworkGomezServer.Instance != null && NetworkGomezServer.Instance.Stream != null) {
                NetworkGomezServer.Instance.Update = NetworkGomezServer.Instance.Update ?? UpdateNetGomez;
            }
        }

        public void UpdateNetGomez() {
            if (networkData == null) {
                networkData = new NetworkGomezData();
            }

            networkData.DataId++;

            networkData.Position = playerMesh.Position;
            networkData.Rotation = playerMesh.Rotation;
            networkData.Opacity = playerMesh.Material.Opacity;
            networkData.Background = PlayerManager.Background;
            networkData.Action = PlayerManager.Action;
            if (playerMesh.FirstGroup.TextureMatrix != null) {
                Matrix? nullable = playerMesh.FirstGroup.TextureMatrix.GetValueInTheMostBrutalWayEver<Matrix?>();
                if (nullable != null) {
                  networkData.TextureMatrix = nullable.GetValueOrDefault();
                }
            }
            //networkData.EffectBackground = 0f;
            networkData.Scale = playerMesh.Scale;
            networkData.NoMoreFez = lastHideFez;
            NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, networkData);
        }

    }
}

