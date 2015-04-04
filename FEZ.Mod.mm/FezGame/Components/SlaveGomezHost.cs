using System;
using FezGame.Components.Actions;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace FezGame.Components {
    public class SlaveGomezHost : DrawableGameComponent {

        public static SlaveGomezHost Instance;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }
        [ServiceDependency]
        public ILevelManager LevelManager { private get; set; }
        [ServiceDependency]
        public ILightingPostProcess LightingPostProcess { private get; set; }
        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        private GomezEffect effect;
        private readonly Mesh playerMesh;

        public Vector3 Position;
        public Quaternion Rotation;
        public float Opacity = 1f;
        public bool Background = false;
        protected AnimatedTexture Animation;
        public ActionType Action = ActionType.Idle;
        protected ActionType PrevAction;
        public Matrix TextureMatrix;
        public float EffectBackground;
        public Vector3 Scale;
        public bool NoMoreFez = false;

        public SlaveGomezHost(Game game) 
            : base(game) {
            playerMesh = new Mesh {
                SamplerState = SamplerState.PointClamp
            };
            UpdateOrder = 12;
            DrawOrder = 10;
            Instance = this;
        }

        public void DoDraw() {
            if (GameState.Loading || PlayerManager.Hidden || GameState.InMap || FezMath.AlmostEqual(PlayerManager.GomezOpacity, 0f)) {
                return;
            }
            playerMesh.Position = Position;
            playerMesh.Rotation = Rotation;
            playerMesh.Material.Opacity = Opacity;
            if (Action.SkipSilhouette()) {
                GraphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.NoSilhouette);
                playerMesh.DepthWrites = false;
                playerMesh.AlwaysOnTop = true;
                effect.Silhouette = true;
                playerMesh.Draw();
            }
            if (Background) {
                GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Hole);
                playerMesh.AlwaysOnTop = true;
                playerMesh.DepthWrites = false;
                effect.Silhouette = false;
                playerMesh.Draw();
            }
            GraphicsDevice.PrepareStencilWrite(StencilMask.Gomez);
            playerMesh.AlwaysOnTop = PlayerManager.Action.NeedsAlwaysOnTop();
            playerMesh.DepthWrites = true;
            effect.Silhouette = false;
            playerMesh.Draw();
            GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }

        public override void Draw(GameTime gameTime) {
            if (GameState.StereoMode && !GameState.FarawaySettings.InTransition) {
                return;
            }
            DoDraw();
        }

        public override void Initialize() {
            playerMesh.AddFace(new Vector3(1f), new Vector3(0f, 0.25f, 0f), FaceOrientation.Front, true, true);
            LevelManager.LevelChanged += delegate {
                effect.ColorSwapMode = ((LevelManager.WaterType != LiquidType.Sewer) ? ((LevelManager.WaterType != LiquidType.Lava) ? ((!LevelManager.BlinkingAlpha) ? ColorSwapMode.None : ColorSwapMode.Cmyk) : ColorSwapMode.VirtualBoy) : ColorSwapMode.Gameboy);
            };
            LightingPostProcess.DrawGeometryLights += new Action(PreDraw);
            base.Initialize();
        }

        protected override void LoadContent() {
            playerMesh.Effect = effect = new GomezEffect();
        }

        private void PreDraw() {
            if (GameState.Loading || PlayerManager.Hidden || GameState.InFpsMode) {
                return;
            }
            effect.Pass = LightingEffectPass.Pre;
            if (!PlayerManager.FullBright) {
                GraphicsDevice.PrepareStencilWrite(StencilMask.Level);
            } else {
                GraphicsDevice.PrepareStencilWrite(StencilMask.None);
            }
            playerMesh.Draw();
            GraphicsDevice.PrepareStencilWrite(StencilMask.None);
            effect.Pass = LightingEffectPass.Main;
        }

        public override void Update(GameTime gameTime) {
            if (NetworkGomezClient.Instance == null) {
                return;
            }

            Position = (Vector3) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            Rotation = (Quaternion) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            Opacity = (float) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            Background = (bool) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            Action = (ActionType) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            TextureMatrix = (Matrix) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            //EffectBackground = (float) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            Scale = (Vector3) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);
            NoMoreFez = (bool) NetworkGomezClient.Formatter.Deserialize(NetworkGomezClient.Instance.Stream);

            playerMesh.FirstGroup.TextureMatrix.Set(TextureMatrix);
            effect.Background = EffectBackground;
            playerMesh.Scale = Scale;
            effect.NoMoreFez = NoMoreFez;

            if (Action != PrevAction) {
                Animation = PlayerManager.GetAnimation(Action);
            }
            PrevAction = Action;
            if (Animation == null) {
                return;
            }
            effect.Animation = Animation.Texture;
        }

    }
}

