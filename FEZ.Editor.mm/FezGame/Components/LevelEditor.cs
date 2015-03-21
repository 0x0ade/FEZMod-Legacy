using Common;
using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezGame.Editor.Widgets;
using FezGame.Mod;
using FezGame.Editor;

namespace FezGame.Components {
    public class LevelEditor : DrawableGameComponent, ILevelEditor {

        [ServiceDependency]
        public IMouseStateManager MouseState { get; set; }
        [ServiceDependency]
        public ISoundManager SoundManager { get; set; }
        [ServiceDependency]
        public IInputManager InputManager { get; set; }
        [ServiceDependency]
        public IGameService GameService { get; set; }
        [ServiceDependency]
        public IGameStateManager GameState { get; set; }
        [ServiceDependency]
        public IGameCameraManager CameraManager { get; set; }
        [ServiceDependency]
        public IFontManager FontManager { get; set; }
        [ServiceDependency]
        public IPlayerManager PlayerManager { get; set; }
        [ServiceDependency]
        public IGameLevelManager LevelManager { get; set; }
        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }
        [ServiceDependency]
        public IDotManager DotManager { get; set; }
        [ServiceDependency]
        public ISpeechBubbleManager SpeechBubble { get; set; }
        [ServiceDependency]
        public IContentManagerProvider CMProvider { get; set; }

        public static LevelEditor Instance;

        public SpriteBatch SpriteBatch { get; set; }
        public GlyphTextRenderer GTR { get; set; }

        protected float SinceMouseMoved = 3f;
        protected bool CursorHovering = false;
        protected Texture2D GrabbedCursor;
        protected Texture2D CanClickCursor;
        protected Texture2D ClickedCursor;
        protected Texture2D PointerCursor;

        protected int SkipLoading = 0;

        public DateTime BuildDate { get; protected set; }

        protected KeyValuePair<TrileEmplacement, TrileInstance>[] tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[8192];

        public TrileInstance HoveredTrile { get; set; }
        public BoundingBox HoveredBox { get; set; }
        public FaceOrientation HoveredFace { get; set; }
        public int TrileId { get; set; }

        public List<EditorWidget> Widgets { get; set; }
        public List<Action> Scheduled { get; set; }

        public InfoWidget InfoWidget;
        public TopBarWidget TopBarWidget;
        public TrilePickerWidget TrilePickerWidget;

        protected EditorWidget DraggingWidget;
        protected EditorWidget FocusedWidget;

        public LevelEditor(Game game)
            : base(game) {
            UpdateOrder = -10;
            DrawOrder = 4000;
            TrileId = 0;
            Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            Scheduled = new List<Action>();

            BuildDate = ReadBuildDate();

            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GTR = new GlyphTextRenderer(Game);

            Game.Window.TextInput += delegate(Object sender, TextInputEventArgs e) {
                if (FocusedWidget != null) {
                    FocusedWidget.TextInput(e.Character);
                }
            };

            Widgets = new List<EditorWidget>();

            ButtonWidget button;

            //TOP BAR
            Widgets.Add(TopBarWidget = new TopBarWidget(Game));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "File"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "New", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 144f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "New level";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelName;
                window.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:"));
                windowLabelName.Background.A = 0;
                windowLabelName.Size.X = 96f;
                windowLabelName.Size.Y = 24f;
                windowLabelName.UpdateBounds = false;
                windowLabelName.LabelCentered = false;
                windowLabelName.Position.X = 0f;
                windowLabelName.Position.Y = 0f;
                TextFieldWidget windowFieldName;
                window.Widgets.Add(windowFieldName = new TextFieldWidget(Game));
                windowFieldName.Size.X = window.Size.X - windowLabelName.Size.X;
                windowFieldName.Size.Y = 24f;
                windowFieldName.UpdateBounds = false;
                windowFieldName.Position.X = windowLabelName.Size.X;
                windowFieldName.Position.Y = windowLabelName.Position.Y;
                windowFieldName.Fill("Levels");

                ButtonWidget windowLabelWidth;
                window.Widgets.Add(windowLabelWidth = new ButtonWidget(Game, "Width:"));
                windowLabelWidth.Background.A = 0;
                windowLabelWidth.Size.X = 96f;
                windowLabelWidth.Size.Y = 24f;
                windowLabelWidth.UpdateBounds = false;
                windowLabelWidth.LabelCentered = false;
                windowLabelWidth.Position.X = 0f;
                windowLabelWidth.Position.Y = 24f;
                TextFieldWidget windowFieldWidth;
                window.Widgets.Add(windowFieldWidth = new TextFieldWidget(Game));
                windowFieldWidth.Size.X = window.Size.X - windowLabelWidth.Size.X;
                windowFieldWidth.Size.Y = 24f;
                windowFieldWidth.UpdateBounds = false;
                windowFieldWidth.Position.X = windowLabelWidth.Size.X;
                windowFieldWidth.Position.Y = windowLabelWidth.Position.Y;

                ButtonWidget windowLabelHeight;
                window.Widgets.Add(windowLabelHeight = new ButtonWidget(Game, "Height:"));
                windowLabelHeight.Background.A = 0;
                windowLabelHeight.Size.X = 96f;
                windowLabelHeight.Size.Y = 24f;
                windowLabelHeight.UpdateBounds = false;
                windowLabelHeight.LabelCentered = false;
                windowLabelHeight.Position.X = 0f;
                windowLabelHeight.Position.Y = 48f;
                TextFieldWidget windowFieldHeight;
                window.Widgets.Add(windowFieldHeight = new TextFieldWidget(Game));
                windowFieldHeight.Size.X = window.Size.X - windowLabelHeight.Size.X;
                windowFieldHeight.Size.Y = 24f;
                windowFieldHeight.UpdateBounds = false;
                windowFieldHeight.Position.X = windowLabelHeight.Size.X;
                windowFieldHeight.Position.Y = windowLabelHeight.Position.Y;

                ButtonWidget windowLabelDepth;
                window.Widgets.Add(windowLabelDepth = new ButtonWidget(Game, "Depth:"));
                windowLabelDepth.Background.A = 0;
                windowLabelDepth.Size.X = 96f;
                windowLabelDepth.Size.Y = 24f;
                windowLabelDepth.UpdateBounds = false;
                windowLabelDepth.LabelCentered = false;
                windowLabelDepth.Position.X = 0f;
                windowLabelDepth.Position.Y = 72f;
                TextFieldWidget windowFieldDepth;
                window.Widgets.Add(windowFieldDepth = new TextFieldWidget(Game));
                windowFieldDepth.Size.X = window.Size.X - windowLabelDepth.Size.X;
                windowFieldDepth.Size.Y = 24f;
                windowFieldDepth.UpdateBounds = false;
                windowFieldDepth.Position.X = windowLabelDepth.Size.X;
                windowFieldDepth.Position.Y = windowLabelDepth.Position.Y;

                ButtonWidget windowLabelTrileset;
                window.Widgets.Add(windowLabelTrileset = new ButtonWidget(Game, "Trileset:"));
                windowLabelTrileset.Background.A = 0;
                windowLabelTrileset.Size.X = 96f;
                windowLabelTrileset.Size.Y = 24f;
                windowLabelTrileset.UpdateBounds = false;
                windowLabelTrileset.LabelCentered = false;
                windowLabelTrileset.Position.X = 0f;
                windowLabelTrileset.Position.Y = 96f;
                TextFieldWidget windowFieldTrileset;
                window.Widgets.Add(windowFieldTrileset = new TextFieldWidget(Game));
                windowFieldTrileset.Size.X = window.Size.X - windowLabelTrileset.Size.X;
                windowFieldTrileset.Size.Y = 24f;
                windowFieldTrileset.UpdateBounds = false;
                windowFieldTrileset.Position.X = windowLabelTrileset.Size.X;
                windowFieldTrileset.Position.Y = windowLabelTrileset.Position.Y;
                windowFieldTrileset.Fill("Trile Sets");

                ButtonWidget windowButtonCreate;
                window.Widgets.Add(windowButtonCreate = new ButtonWidget(Game, "CREATE", delegate() {
                    Level level = CreateNewLevel(
                        windowFieldName.Text,
                        int.Parse(windowFieldWidth.Text),
                        int.Parse(windowFieldHeight.Text),
                        int.Parse(windowFieldDepth.Text),
                        windowFieldTrileset.Text
                    );
                    GameState.Loading = true;
                    SkipLoading = 8;
                    GameLevelManagerHelper.ChangeLevel(level);
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonCreate.Size.X = window.Size.X;
                windowButtonCreate.Size.Y = 24f;
                windowButtonCreate.UpdateBounds = false;
                windowButtonCreate.LabelCentered = true;
                windowButtonCreate.Position.X = 0f;
                windowButtonCreate.Position.Y = window.Size.Y - windowButtonCreate.Size.Y;
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Open", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 48f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Open level";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelName;
                window.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:"));
                windowLabelName.Background.A = 0;
                windowLabelName.Size.X = 96f;
                windowLabelName.Size.Y = 24f;
                windowLabelName.UpdateBounds = false;
                windowLabelName.LabelCentered = false;
                windowLabelName.Position.X = 0f;
                windowLabelName.Position.Y = 0f;
                TextFieldWidget windowFieldName;
                window.Widgets.Add(windowFieldName = new TextFieldWidget(Game));
                windowFieldName.Size.X = window.Size.X - windowLabelName.Size.X;
                windowFieldName.Size.Y = 24f;
                windowFieldName.UpdateBounds = false;
                windowFieldName.Position.X = windowLabelName.Size.X;
                windowFieldName.Position.Y = windowLabelName.Position.Y;
                windowFieldName.Fill("Levels");

                ButtonWidget windowButtonLoad;
                window.Widgets.Add(windowButtonLoad = new ButtonWidget(Game, "LOAD", delegate() {
                    GameState.Loading = true;
                    SkipLoading = 8;
                    LevelManager.ChangeLevel(windowFieldName.Text);
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonLoad.Size.X = window.Size.X;
                windowButtonLoad.Size.Y = 24f;
                windowButtonLoad.UpdateBounds = false;
                windowButtonLoad.LabelCentered = true;
                windowButtonLoad.Position.X = 0f;
                windowButtonLoad.Position.Y = window.Size.Y - windowButtonLoad.Size.Y;
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Save", delegate() {
                WindowHeaderWidget windowHeader = null;

                string filePath = ("Resources\\levels\\"+(LevelManager.Name.ToLower())).Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString())+".xml";
                FileInfo file = new FileInfo(filePath);

                Action save = delegate() {
                    if (file.Exists) {
                        file.Delete();
                    }
                    ModLogger.Log("JAFM.Engine", "Saving level "+LevelManager.Name);
                    GameLevelManagerHelper.Save(LevelManager.Name);
                    if (windowHeader != null) {
                        windowHeader.CloseButtonWidget.Action();
                    }
                };

                if (!file.Exists) {
                    save();
                    return;
                }

                ContainerWidget window;
                ButtonWidget windowButton;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 64f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Overwrite level?";
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                window.Widgets.Add(windowButton = new ButtonWidget(Game, "Level already existing. Overwrite?"));
                windowButton.Background.A = 0;
                windowButton.Size.X = window.Size.X;
                windowButton.Size.Y = 24f;
                windowButton.UpdateBounds = false;
                windowButton.LabelCentered = true;
                windowButton.Position.X = 0;
                windowButton.Position.Y = window.Size.Y / 2 - 24f;

                window.Widgets.Add(windowButton = new ButtonWidget(Game, "YES", save));
                windowButton.Size.X = 48f;
                windowButton.Size.Y = 24f;
                windowButton.UpdateBounds = false;
                windowButton.LabelCentered = true;
                windowButton.Position.X = window.Size.X / 2 - windowButton.Size.X - 4f;
                windowButton.Position.Y = window.Size.Y / 2;

                window.Widgets.Add(windowButton = new ButtonWidget(Game, "NO", windowHeader.CloseButtonWidget.Action));
                windowButton.Size.X = 48f;
                windowButton.Size.Y = 24f;
                windowButton.UpdateBounds = false;
                windowButton.LabelCentered = true;
                windowButton.Position.X = window.Size.X / 2 + 4f;
                windowButton.Position.Y = window.Size.Y / 2;
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "View"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Perspective", new EditorWidget[] {
                new ButtonWidget(Game, "Front", () => CameraManager.ChangeViewpoint(Viewpoint.Front)),
                new ButtonWidget(Game, "Left", () => CameraManager.ChangeViewpoint(Viewpoint.Left)),
                new ButtonWidget(Game, "Back", () => CameraManager.ChangeViewpoint(Viewpoint.Back)),
                new ButtonWidget(Game, "Right", () => CameraManager.ChangeViewpoint(Viewpoint.Right)),
                new ButtonWidget(Game, "Perspective", () => CameraManager.ChangeViewpoint(Viewpoint.Perspective))
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Pixels per Trixel", new EditorWidget[] {
                new ButtonWidget(Game, "0.25", () => CameraManager.PixelsPerTrixel = 0.25f),
                new ButtonWidget(Game, "1", () => CameraManager.PixelsPerTrixel = 1f),
                new ButtonWidget(Game, "2", () => CameraManager.PixelsPerTrixel = 2f),
                new ButtonWidget(Game, "3", () => CameraManager.PixelsPerTrixel = 3f),
                new ButtonWidget(Game, "4", () => CameraManager.PixelsPerTrixel = 4f),
            }, delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 48f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Pixels per Trixel";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                TextFieldWidget windowFieldPPT;
                window.Widgets.Add(windowFieldPPT = new TextFieldWidget(Game));
                windowFieldPPT.Text = CameraManager.PixelsPerTrixel.ToString();
                windowFieldPPT.Size.X = window.Size.X;
                windowFieldPPT.Size.Y = 24f;
                windowFieldPPT.UpdateBounds = false;
                windowFieldPPT.Position.X = 0f;
                windowFieldPPT.Position.Y = 0f;
                windowFieldPPT.Fill(new string[] {
                    "0.25",
                    "1",
                    "2",
                    "3",
                    "4"
                });

                ButtonWidget windowButtonChange;
                window.Widgets.Add(windowButtonChange = new ButtonWidget(Game, "CHANGE", delegate() {
                    CameraManager.PixelsPerTrixel = float.Parse(windowFieldPPT.Text);
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonChange.Size.X = window.Size.X;
                windowButtonChange.Size.Y = 24f;
                windowButtonChange.UpdateBounds = false;
                windowButtonChange.LabelCentered = true;
                windowButtonChange.Position.X = 0f;
                windowButtonChange.Position.Y = window.Size.Y - windowButtonChange.Size.Y;
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Level"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Change settings", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 120f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Change settings";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelName;
                window.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:"));
                windowLabelName.Background.A = 0;
                windowLabelName.Size.X = 96f;
                windowLabelName.Size.Y = 24f;
                windowLabelName.UpdateBounds = false;
                windowLabelName.LabelCentered = false;
                windowLabelName.Position.X = 0f;
                windowLabelName.Position.Y = 0f;
                TextFieldWidget windowFieldName;
                window.Widgets.Add(windowFieldName = new TextFieldWidget(Game));
                windowFieldName.Text = LevelManager.Name;
                windowFieldName.Size.X = window.Size.X - windowLabelName.Size.X;
                windowFieldName.Size.Y = 24f;
                windowFieldName.UpdateBounds = false;
                windowFieldName.Position.X = windowLabelName.Size.X;
                windowFieldName.Position.Y = windowLabelName.Position.Y;
                windowFieldName.Fill("Levels");

                ButtonWidget windowLabelWidth;
                window.Widgets.Add(windowLabelWidth = new ButtonWidget(Game, "Width:"));
                windowLabelWidth.Background.A = 0;
                windowLabelWidth.Size.X = 96f;
                windowLabelWidth.Size.Y = 24f;
                windowLabelWidth.UpdateBounds = false;
                windowLabelWidth.LabelCentered = false;
                windowLabelWidth.Position.X = 0f;
                windowLabelWidth.Position.Y = 24f;
                TextFieldWidget windowFieldWidth;
                window.Widgets.Add(windowFieldWidth = new TextFieldWidget(Game));
                windowFieldWidth.Text = ((int) LevelManager.Size.X).ToString();
                windowFieldWidth.Size.X = window.Size.X - windowLabelWidth.Size.X;
                windowFieldWidth.Size.Y = 24f;
                windowFieldWidth.UpdateBounds = false;
                windowFieldWidth.Position.X = windowLabelWidth.Size.X;
                windowFieldWidth.Position.Y = windowLabelWidth.Position.Y;

                ButtonWidget windowLabelHeight;
                window.Widgets.Add(windowLabelHeight = new ButtonWidget(Game, "Height:"));
                windowLabelHeight.Background.A = 0;
                windowLabelHeight.Size.X = 96f;
                windowLabelHeight.Size.Y = 24f;
                windowLabelHeight.UpdateBounds = false;
                windowLabelHeight.LabelCentered = false;
                windowLabelHeight.Position.X = 0f;
                windowLabelHeight.Position.Y = 48f;
                TextFieldWidget windowFieldHeight;
                window.Widgets.Add(windowFieldHeight = new TextFieldWidget(Game));
                windowFieldHeight.Text = ((int) LevelManager.Size.Y).ToString();
                windowFieldHeight.Size.X = window.Size.X - windowLabelHeight.Size.X;
                windowFieldHeight.Size.Y = 24f;
                windowFieldHeight.UpdateBounds = false;
                windowFieldHeight.Position.X = windowLabelHeight.Size.X;
                windowFieldHeight.Position.Y = windowLabelHeight.Position.Y;

                ButtonWidget windowLabelDepth;
                window.Widgets.Add(windowLabelDepth = new ButtonWidget(Game, "Depth:"));
                windowLabelDepth.Background.A = 0;
                windowLabelDepth.Size.X = 96f;
                windowLabelDepth.Size.Y = 24f;
                windowLabelDepth.UpdateBounds = false;
                windowLabelDepth.LabelCentered = false;
                windowLabelDepth.Position.X = 0f;
                windowLabelDepth.Position.Y = 72f;
                TextFieldWidget windowFieldDepth;
                window.Widgets.Add(windowFieldDepth = new TextFieldWidget(Game));
                windowFieldDepth.Text = ((int) LevelManager.Size.Z).ToString();
                windowFieldDepth.Size.X = window.Size.X - windowLabelDepth.Size.X;
                windowFieldDepth.Size.Y = 24f;
                windowFieldDepth.UpdateBounds = false;
                windowFieldDepth.Position.X = windowLabelDepth.Size.X;
                windowFieldDepth.Position.Y = windowLabelDepth.Position.Y;

                ButtonWidget windowButtonChange;
                window.Widgets.Add(windowButtonChange = new ButtonWidget(Game, "CHANGE", delegate() {
                    GameLevelManagerHelper.Level.Name = windowFieldName.Text;
                    GameLevelManagerHelper.Level.Size = new Vector3(
                        int.Parse(windowFieldWidth.Text),
                        int.Parse(windowFieldHeight.Text),
                        int.Parse(windowFieldDepth.Text)
                    );
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonChange.Size.X = window.Size.X;
                windowButtonChange.Size.Y = 24f;
                windowButtonChange.UpdateBounds = false;
                windowButtonChange.LabelCentered = true;
                windowButtonChange.Position.X = 0f;
                windowButtonChange.Position.Y = window.Size.Y - windowButtonChange.Size.Y;
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Change spawnpoint", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 120f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Change spawnpoint";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelX;
                window.Widgets.Add(windowLabelX = new ButtonWidget(Game, "X:"));
                windowLabelX.Background.A = 0;
                windowLabelX.Size.X = 96f;
                windowLabelX.Size.Y = 24f;
                windowLabelX.UpdateBounds = false;
                windowLabelX.LabelCentered = false;
                windowLabelX.Position.X = 0f;
                windowLabelX.Position.Y = 0f;
                TextFieldWidget windowFieldX;
                window.Widgets.Add(windowFieldX = new TextFieldWidget(Game));
                windowFieldX.Text = LevelManager.StartingPosition.Id.X.ToString();
                windowFieldX.Size.X = window.Size.X - windowLabelX.Size.X;
                windowFieldX.Size.Y = 24f;
                windowFieldX.UpdateBounds = false;
                windowFieldX.Position.X = windowLabelX.Size.X;
                windowFieldX.Position.Y = windowLabelX.Position.Y;

                ButtonWidget windowLabelY;
                window.Widgets.Add(windowLabelY = new ButtonWidget(Game, "Y:"));
                windowLabelY.Background.A = 0;
                windowLabelY.Size.X = 96f;
                windowLabelY.Size.Y = 24f;
                windowLabelY.UpdateBounds = false;
                windowLabelY.LabelCentered = false;
                windowLabelY.Position.X = 0f;
                windowLabelY.Position.Y = 24f;
                TextFieldWidget windowFieldY;
                window.Widgets.Add(windowFieldY = new TextFieldWidget(Game));
                windowFieldY.Text = LevelManager.StartingPosition.Id.Y.ToString();
                windowFieldY.Size.X = window.Size.X - windowLabelY.Size.X;
                windowFieldY.Size.Y = 24f;
                windowFieldY.UpdateBounds = false;
                windowFieldY.Position.X = windowLabelY.Size.X;
                windowFieldY.Position.Y = windowLabelY.Position.Y;

                ButtonWidget windowLabelZ;
                window.Widgets.Add(windowLabelZ = new ButtonWidget(Game, "Z:"));
                windowLabelZ.Background.A = 0;
                windowLabelZ.Size.X = 96f;
                windowLabelZ.Size.Y = 24f;
                windowLabelZ.UpdateBounds = false;
                windowLabelZ.LabelCentered = false;
                windowLabelZ.Position.X = 0f;
                windowLabelZ.Position.Y = 48f;
                TextFieldWidget windowFieldZ;
                window.Widgets.Add(windowFieldZ = new TextFieldWidget(Game));
                windowFieldZ.Text = LevelManager.StartingPosition.Id.Z.ToString();
                windowFieldZ.Size.X = window.Size.X - windowLabelZ.Size.X;
                windowFieldZ.Size.Y = 24f;
                windowFieldZ.UpdateBounds = false;
                windowFieldZ.Position.X = windowLabelZ.Size.X;
                windowFieldZ.Position.Y = windowLabelZ.Position.Y;

                ButtonWidget windowLabelFace;
                window.Widgets.Add(windowLabelFace = new ButtonWidget(Game, "Face:"));
                windowLabelFace.Background.A = 0;
                windowLabelFace.Size.X = 96f;
                windowLabelFace.Size.Y = 24f;
                windowLabelFace.UpdateBounds = false;
                windowLabelFace.LabelCentered = false;
                windowLabelFace.Position.X = 0f;
                windowLabelFace.Position.Y = 72f;
                TextFieldWidget windowFieldFace;
                window.Widgets.Add(windowFieldFace = new TextFieldWidget(Game));
                windowFieldFace.Text = LevelManager.StartingPosition.Face.ToString();
                windowFieldFace.Size.X = window.Size.X - windowLabelFace.Size.X;
                windowFieldFace.Size.Y = 24f;
                windowFieldFace.UpdateBounds = false;
                windowFieldFace.Position.X = windowLabelFace.Size.X;
                windowFieldFace.Position.Y = windowLabelFace.Position.Y;
                windowFieldFace.Fill(Enum.GetNames(typeof(FaceOrientation)));

                ButtonWidget windowButtonChange;
                window.Widgets.Add(windowButtonChange = new ButtonWidget(Game, "CHANGE", delegate() {
                    LevelManager.StartingPosition.Id.X = int.Parse(windowFieldX.Text);
                    LevelManager.StartingPosition.Id.Y = int.Parse(windowFieldY.Text);
                    LevelManager.StartingPosition.Id.Z = int.Parse(windowFieldZ.Text);
                    LevelManager.StartingPosition.Face = (FaceOrientation) Enum.Parse(typeof(FaceOrientation), windowFieldFace.Text);
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonChange.Size.X = window.Size.X;
                windowButtonChange.Size.Y = 24f;
                windowButtonChange.UpdateBounds = false;
                windowButtonChange.LabelCentered = true;
                windowButtonChange.Position.X = 0f;
                windowButtonChange.Position.Y = window.Size.Y - windowButtonChange.Size.Y;
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Change sky", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 48f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Change sky";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelSky;
                window.Widgets.Add(windowLabelSky = new ButtonWidget(Game, "Sky:"));
                windowLabelSky.Background.A = 0;
                windowLabelSky.Size.X = 96f;
                windowLabelSky.Size.Y = 24f;
                windowLabelSky.UpdateBounds = false;
                windowLabelSky.LabelCentered = false;
                windowLabelSky.Position.X = 0f;
                windowLabelSky.Position.Y = 0f;
                TextFieldWidget windowFieldSky;
                window.Widgets.Add(windowFieldSky = new TextFieldWidget(Game));
                if (LevelManager.Sky != null) {
                    windowFieldSky.Text = LevelManager.Sky.Name;
                }
                windowFieldSky.Size.X = window.Size.X - windowLabelSky.Size.X;
                windowFieldSky.Size.Y = 24f;
                windowFieldSky.UpdateBounds = false;
                windowFieldSky.Position.X = windowLabelSky.Size.X;
                windowFieldSky.Position.Y = windowLabelSky.Position.Y;
                windowFieldSky.Fill("Skies");

                ButtonWidget windowButtonChange;
                window.Widgets.Add(windowButtonChange = new ButtonWidget(Game, "CHANGE", delegate() {
                    Sky sky = windowFieldSky.Text.Length > 0 ? CMProvider.CurrentLevel.Load<Sky>("Skies/" + windowFieldSky.Text) : null;
                    if (sky != null) {
                        GameLevelManagerHelper.Level.Sky = sky;
                        GameLevelManagerHelper.Level.SkyName = sky.Name;
                        LevelManager.ChangeSky(sky);
                    }
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonChange.Size.X = window.Size.X;
                windowButtonChange.Size.Y = 24f;
                windowButtonChange.UpdateBounds = false;
                windowButtonChange.LabelCentered = true;
                windowButtonChange.Position.X = 0f;
                windowButtonChange.Position.Y = window.Size.Y - windowButtonChange.Size.Y;
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Change song", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 256f;
                window.Size.Y = 48f;
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Label = "Change song";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelSong;
                window.Widgets.Add(windowLabelSong = new ButtonWidget(Game, "Song:"));
                windowLabelSong.Background.A = 0;
                windowLabelSong.Size.X = 96f;
                windowLabelSong.Size.Y = 24f;
                windowLabelSong.UpdateBounds = false;
                windowLabelSong.LabelCentered = false;
                windowLabelSong.Position.X = 0f;
                windowLabelSong.Position.Y = 0f;
                TextFieldWidget windowFieldSong;
                window.Widgets.Add(windowFieldSong = new TextFieldWidget(Game));
                if (LevelManager.Song != null) {
                    windowFieldSong.Text = LevelManager.SongName;
                }
                windowFieldSong.Size.X = window.Size.X - windowLabelSong.Size.X;
                windowFieldSong.Size.Y = 24f;
                windowFieldSong.UpdateBounds = false;
                windowFieldSong.Position.X = windowLabelSong.Size.X;
                windowFieldSong.Position.Y = windowLabelSong.Position.Y;
                windowFieldSong.Fill("Music");

                ButtonWidget windowButtonChange;
                window.Widgets.Add(windowButtonChange = new ButtonWidget(Game, "CHANGE", delegate() {
                    TrackedSong song = windowFieldSong.Text.Length > 0 ? CMProvider.CurrentLevel.Load<TrackedSong>("Music/" + windowFieldSong.Text) : null;
                    if (song != null) {
                        song.Initialize();
                        GameLevelManagerHelper.Level.Song = song;
                        LevelManager.SongChanged = true;
                        SoundManager.PlayNewSong(song.Name);
                        SoundManager.UpdateSongActiveTracks();
                    }
                    GameLevelManagerHelper.Level.SongName = song != null ? song.Name : null;
                    windowHeader.CloseButtonWidget.Action();
                }));
                windowButtonChange.Size.X = window.Size.X;
                windowButtonChange.Size.Y = 24f;
                windowButtonChange.UpdateBounds = false;
                windowButtonChange.LabelCentered = true;
                windowButtonChange.Position.X = 0f;
                windowButtonChange.Position.Y = window.Size.Y - windowButtonChange.Size.Y;
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Scripting"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Volumes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Volumes";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                int i = 0;
                foreach (Volume volume in LevelManager.Volumes.Values) {
                    ButtonWidget windowButtonVolume;
                    window.Widgets.Add(windowButtonVolume = new ButtonWidget(Game, "["+volume.Id+"] "+VectorHelper.ToString(volume.From)+" - "+VectorHelper.ToString(volume.To)));
                    windowButtonVolume.Size.X = window.Size.X - 48f;
                    windowButtonVolume.Size.Y = 24f;
                    windowButtonVolume.UpdateBounds = false;
                    windowButtonVolume.LabelCentered = false;
                    windowButtonVolume.Position.X = 0f;
                    windowButtonVolume.Position.Y = i * 24f;

                    ButtonWidget windowButtonClone;
                    window.Widgets.Add(windowButtonClone = new ButtonWidget(Game, "C"));
                    windowButtonClone.Background.B = 31;
                    windowButtonClone.Size.X = 24f;
                    windowButtonClone.Size.Y = 24f;
                    windowButtonClone.UpdateBounds = false;
                    windowButtonClone.LabelCentered = true;
                    windowButtonClone.Position.X = window.Size.X - 48f;
                    windowButtonClone.Position.Y = windowButtonVolume.Position.Y;

                    ButtonWidget windowButtonRemove;
                    window.Widgets.Add(windowButtonRemove = new ButtonWidget(Game, "X"));
                    windowButtonRemove.Background.R = 255;
                    windowButtonRemove.Size.X = 24f;
                    windowButtonRemove.Size.Y = 24f;
                    windowButtonRemove.UpdateBounds = false;
                    windowButtonRemove.LabelCentered = true;
                    windowButtonRemove.Position.X = window.Size.X - 24f;
                    windowButtonRemove.Position.Y = windowButtonVolume.Position.Y;

                    i++;
                }

                window.Size.Y += i * 24f;

                ButtonWidget windowButtonAdd;
                window.Widgets.Add(windowButtonAdd = new ButtonWidget(Game, "+", delegate() {
                }));
                windowButtonAdd.Background.G = 31;
                windowButtonAdd.Size.X = window.Size.X;
                windowButtonAdd.Size.Y = 24f;
                windowButtonAdd.UpdateBounds = false;
                windowButtonAdd.LabelCentered = true;
                windowButtonAdd.Position.X = 0f;
                windowButtonAdd.Position.Y = window.Size.Y - windowButtonAdd.Size.Y;

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Scripts", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game));
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Scripts";
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                int i = 0;
                foreach (Script script in LevelManager.Scripts.Values) {
                    ButtonWidget windowButtonScript;
                    window.Widgets.Add(windowButtonScript = new ButtonWidget(Game, "["+script.Id+"] "+script.Name+" ("+(script.Triggerless ? "none" : (script.Triggers.Count == 1 ? script.Triggers[0].ToString() : "..."))+") : "+(script.Actions.Count == 1 ? script.Actions[0].ToString() : "...")));
                    windowButtonScript.Size.X = window.Size.X - 48f;
                    windowButtonScript.Size.Y = 24f;
                    windowButtonScript.UpdateBounds = false;
                    windowButtonScript.LabelCentered = false;
                    windowButtonScript.Position.X = 0f;
                    windowButtonScript.Position.Y = i * 24f;

                    ButtonWidget windowButtonClone;
                    window.Widgets.Add(windowButtonClone = new ButtonWidget(Game, "C"));
                    windowButtonClone.Background.B = 31;
                    windowButtonClone.Size.X = 24f;
                    windowButtonClone.Size.Y = 24f;
                    windowButtonClone.UpdateBounds = false;
                    windowButtonClone.LabelCentered = true;
                    windowButtonClone.Position.X = window.Size.X - 48f;
                    windowButtonClone.Position.Y = windowButtonScript.Position.Y;

                    ButtonWidget windowButtonRemove;
                    window.Widgets.Add(windowButtonRemove = new ButtonWidget(Game, "X"));
                    windowButtonRemove.Background.R = 255;
                    windowButtonRemove.Size.X = 24f;
                    windowButtonRemove.Size.Y = 24f;
                    windowButtonRemove.UpdateBounds = false;
                    windowButtonRemove.LabelCentered = true;
                    windowButtonRemove.Position.X = window.Size.X - 24f;
                    windowButtonRemove.Position.Y = windowButtonScript.Position.Y;

                    i++;
                }

                window.Size.Y += i * 24f;

                ButtonWidget windowButtonAdd;
                window.Widgets.Add(windowButtonAdd = new ButtonWidget(Game, "+", delegate() {
                }));
                windowButtonAdd.Background.G = 31;
                windowButtonAdd.Size.X = window.Size.X;
                windowButtonAdd.Size.Y = 24f;
                windowButtonAdd.UpdateBounds = false;
                windowButtonAdd.LabelCentered = true;
                windowButtonAdd.Position.X = 0f;
                windowButtonAdd.Position.Y = window.Size.Y - windowButtonAdd.Size.Y;

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Editor"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Toggle theme", delegate() {
                EditorWidget.DefaultBackground.R = (byte) (255 - EditorWidget.DefaultBackground.R);
                EditorWidget.DefaultBackground.G = (byte) (255 - EditorWidget.DefaultBackground.G);
                EditorWidget.DefaultBackground.B = (byte) (255 - EditorWidget.DefaultBackground.B);

                EditorWidget.DefaultForeground.R = (byte) (255 - EditorWidget.DefaultForeground.R);
                EditorWidget.DefaultForeground.G = (byte) (255 - EditorWidget.DefaultForeground.G);
                EditorWidget.DefaultForeground.B = (byte) (255 - EditorWidget.DefaultForeground.B);

                foreach (EditorWidget widget in Widgets) {
                    widget.UpdateTheme();
                }
            }));

            //INFO
            Widgets.Add(InfoWidget = new InfoWidget(Game));

            //TRILE PICKER
            Widgets.Add(TrilePickerWidget = new TrilePickerWidget(Game));

        }

        public void Preload() {
            PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
            CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
            ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
            GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");
        }

        public override void Update(GameTime gameTime) {
            while (Scheduled.Count > 0) {
                Scheduled[0]();
                Scheduled.RemoveAt(0);
            }

            if ((--SkipLoading) == 0) {
                GameState.Loading = false;
                return;
            }

            if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.InFpsMode) {
                return;
            }

            SinceMouseMoved += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0) {
                SinceMouseMoved = 0f;
            }

            CursorHovering = false;
            HoveredTrile = null;

            Vector3 right = CameraManager.InverseView.Right;
            Vector3 up = CameraManager.InverseView.Up;
            Vector3 forward = CameraManager.InverseView.Forward;
            Ray ray = new Ray(GraphicsDevice.Viewport.Unproject(new Vector3(MouseState.Position.X, MouseState.Position.Y, 0.0f), CameraManager.Projection, CameraManager.View, Matrix.Identity), forward);
            float intersectionMin = float.MaxValue;

            //Ugly thread-safety workaround
            int trilesCount = LevelManager.Triles.Count;
            LevelManager.Triles.CopyTo(tmpTriles.Length < trilesCount ? (tmpTriles = new KeyValuePair<TrileEmplacement, TrileInstance>[trilesCount]) : tmpTriles, 0);

            for (int i = 0; i < trilesCount; i++) {
                TrileInstance trile = tmpTriles[i].Value;
                BoundingBox box = new BoundingBox(trile.Position, trile.Position + new Vector3(1f));
                float? intersection = ray.Intersects(box);
                if (intersection.HasValue && intersection < intersectionMin) {
                    HoveredTrile = trile;
                    HoveredBox = box;
                    intersectionMin = intersection.Value;
                }
            }

            TrilePickerWidget.Position.Y = GraphicsDevice.Viewport.Height - TrilePickerWidget.Size.Y;
            InfoWidget.Position.Y = TrilePickerWidget.Position.Y - InfoWidget.Size.Y;

            bool cursorInMenu = UpdateWidgets(gameTime, Widgets, true);

            if (DraggingWidget != null && (MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.LeftButton.State == MouseButtonStates.DragEnded)) {
                DraggingWidget.Dragging(gameTime, MouseState.LeftButton.State);
                cursorInMenu = true;

                if (MouseState.LeftButton.State == MouseButtonStates.DragEnded) {
                    DraggingWidget = null;
                }
            }

            if (cursorInMenu) {
                CursorHovering = true;
                return;
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                if (FocusedWidget != null) {
                    FocusedWidget.Unfocus(gameTime);
                }
                FocusedWidget = null;
            }

            if (HoveredTrile != null) {
                HoveredFace = GetHoveredFace(HoveredBox, ray);
                CursorHovering = true;
            }

            if (MouseState.LeftButton.State == MouseButtonStates.Clicked && HoveredTrile != null && LevelManager.TrileSet != null && LevelManager.TrileSet.Triles.ContainsKey(TrileId)) {
                AddTrile(CreateNewTrile(TrileId, new TrileEmplacement(HoveredTrile.Position - FezMath.AsVector(HoveredFace))));
            }

            if (MouseState.RightButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                LevelManager.ClearTrile(HoveredTrile);
                HoveredTrile = null;
            }

            if (MouseState.MiddleButton.State == MouseButtonStates.Clicked && HoveredTrile != null) {
                TrileId = HoveredTrile.TrileId;
            }

            CameraManager.PixelsPerTrixel = Math.Max(0.25f, CameraManager.PixelsPerTrixel + 0.25f * MouseState.WheelTurns);

        }

        public override void Draw(GameTime gameTime) {
            if (GameState.InMap || GameState.Loading || !FEZMod.Preloaded) {
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);

            GraphicsDeviceExtensions.SetBlendingMode(GraphicsDevice, BlendingMode.Alphablending);
            GraphicsDeviceExtensions.BeginPoint(SpriteBatch);

            foreach (EditorWidget widget in Widgets) {
                widget.LevelEditor = this;
                widget.Draw(gameTime);
            }

            SpriteBatch.Draw(MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging ? GrabbedCursor : (CursorHovering ? (MouseState.LeftButton.State == MouseButtonStates.Down || MouseState.RightButton.State == MouseButtonStates.Down ? ClickedCursor : CanClickCursor) : PointerCursor), 
                new Vector2(
                    (float) cursorPosition.X - cursorScale * 11.5f,
                    (float) cursorPosition.Y - cursorScale * 8.5f
                ), new Rectangle?(),
                new Color(1f, 1f, 1f, FezMath.Saturate((float) (1.0 - ((double) SinceMouseMoved - 2.0)))),
                0.0f,
                Vector2.Zero,
                cursorScale,
                SpriteEffects.None,
                0.0f);

            SpriteBatch.End();
        }

        protected bool UpdateWidgets(GameTime gameTime, List<EditorWidget> widgets, Boolean update) {
            bool cursorOnWidget = false;
            foreach (EditorWidget widget in widgets) {
                widget.LevelEditor = this;
                if (update) {
                    widget.Update(gameTime);
                }
                bool cursorOnChild = false;
                if (widget.ShowChildren) {
                    cursorOnChild = UpdateWidgets(gameTime, widget.Widgets, false);
                }
                if (widget.Position.X + widget.Offset.X <= MouseState.Position.X && MouseState.Position.X <= widget.Position.X + widget.Offset.X + widget.Size.X &&
                    widget.Position.Y + widget.Offset.Y <= MouseState.Position.Y && MouseState.Position.Y <= widget.Position.Y + widget.Offset.Y + widget.Size.Y) {
                    cursorOnWidget = true;
                    widget.Hover(gameTime);
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 1);
                        if (FocusedWidget != null) {
                            FocusedWidget.Unfocus(gameTime);
                        }
                        FocusedWidget = widget;
                    }
                    if (!cursorOnChild && MouseState.RightButton.State == MouseButtonStates.Clicked) {
                        widget.Click(gameTime, 3);
                    }
                    if (!cursorOnChild && MouseState.LeftButton.State == MouseButtonStates.DragStarted) {
                        if (DraggingWidget != null) {
                            DraggingWidget.Dragging(gameTime, MouseButtonStates.DragEnded);
                        }
                        DraggingWidget = widget;
                        DraggingWidget.Dragging(gameTime, MouseButtonStates.DragStarted);
                    }
                    widget.Scroll(gameTime, MouseState.WheelTurns);
                }
                cursorOnWidget = cursorOnWidget || cursorOnChild;
            }
            return cursorOnWidget;
        }

        public Level CreateNewLevel(string name, int width, int height, int depth, string trileset) {
            Level level = new Level();
            level.Name = name;
            level.Size = new Vector3(width, height, depth);
            level.TrileSetName = trileset;
            level.StartingPosition = new TrileFace();
            level.StartingPosition.Face = FaceOrientation.Front;
            level.StartingPosition.Id = new TrileEmplacement(width / 2, height / 2, depth / 2);
            PlayerManager.Position = level.StartingPosition.Id.AsVector;
            return level;
        }

        public TrileInstance CreateNewTrile(int trileId, TrileEmplacement emplacement) {
            TrileInstance trile = new TrileInstance(emplacement, trileId);
            LevelManager.Triles[emplacement] = trile;

            trile.SetPhiLight(CameraManager.Viewpoint.ToPhi());

            trile.PhysicsState = new InstancePhysicsState(trile);
            trile.Enabled = true;

            return trile;
        }

        public void AddTrile(TrileInstance trile) {
            trile.Update();
            LevelMaterializer.AddInstance(trile);
            LevelMaterializer.RebuildTriles(true);
            LevelMaterializer.RebuildInstances();
            LevelMaterializer.UpdateInstance(trile);
            trile.RefreshTrile();

            if (LevelManager.Triles.Count == 1) {
                PlayerManager.CheckpointGround = trile;
                PlayerManager.RespawnAtCheckpoint();
            }
        }

        protected FaceOrientation GetHoveredFace(BoundingBox box, Ray ray) {
            float intersectionMin = float.MaxValue;

            BoundingBox[] sides = new BoundingBox[6];
            sides[0] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Min.X, box.Max.Y, box.Max.Z));
            sides[1] = new BoundingBox(new Vector3(box.Max.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[2] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Min.Y, box.Max.Z));
            sides[3] = new BoundingBox(new Vector3(box.Min.X, box.Max.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));
            sides[4] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Min.Z), new Vector3(box.Max.X, box.Max.Y, box.Min.Z));
            sides[5] = new BoundingBox(new Vector3(box.Min.X, box.Min.Y, box.Max.Z), new Vector3(box.Max.X, box.Max.Y, box.Max.Z));

            FaceOrientation[] faces = new FaceOrientation[6];
            faces[0] = FaceOrientation.Right;
            faces[1] = FaceOrientation.Left;
            faces[2] = FaceOrientation.Top;
            faces[3] = FaceOrientation.Down;
            faces[4] = FaceOrientation.Front;
            faces[5] = FaceOrientation.Back;

            FaceOrientation face = FaceOrientation.Top;

            for (int i = 0; i < 6; i++) {
                float? intersection = ray.Intersects(sides[i]);
                if (intersection.HasValue && intersection < intersectionMin) {
                    intersectionMin = intersection.Value;
                    face = faces[i];
                }
            }

            return face;
        }

        protected DateTime ReadBuildDate() {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            Stream s = null;

            try {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            } finally {
                if (s != null) {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }
    }
}

