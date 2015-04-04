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

        public void orig_Update(GameTime gameTime) {
        }

        public void Update(GameTime gameTime) {
            orig_Update(gameTime);
            if (NetworkGomezServer.Instance != null && NetworkGomezServer.Instance.Stream != null) {
                //NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, );
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, playerMesh.Position);
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, playerMesh.Rotation);
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, playerMesh.Material.Opacity);
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, PlayerManager.Background);
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, PlayerManager.Action);
                if (playerMesh.FirstGroup.TextureMatrix != null) {
                    Matrix? nullable = playerMesh.FirstGroup.TextureMatrix.GetValueInTheMostBrutalWayEver<Matrix?>();
                    NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, nullable.GetValueOrDefault());
                } else {
                    NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, new Matrix?(default(Matrix)));
                }
                //NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, 0f);//effect.Background
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, playerMesh.Scale);
                NetworkGomezServer.Formatter.Serialize(NetworkGomezServer.Instance.Stream, lastHideFez);
            }
        }

    }
}

