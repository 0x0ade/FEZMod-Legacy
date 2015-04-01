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
                Widgets.Add(window = new ContainerWidget(Game) {
                    Size = new Vector2(256f, 144f),
                    Label = "New level"
                });
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                WindowHeaderWidget windowHeader;
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                ButtonWidget windowLabelName;
                window.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 0f)
                });
                TextFieldWidget windowFieldName;
                window.Widgets.Add(windowFieldName = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelName.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                });
                windowFieldName.Fill("Levels");

                ButtonWidget windowLabelWidth;
                window.Widgets.Add(windowLabelWidth = new ButtonWidget(Game, "Width:") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 24f)
                });
                TextFieldWidget windowFieldWidth;
                window.Widgets.Add(windowFieldWidth = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelWidth.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelWidth.Size.X, windowLabelWidth.Position.Y)
                });

                ButtonWidget windowLabelHeight;
                window.Widgets.Add(windowLabelHeight = new ButtonWidget(Game, "Height:") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 48f)
                });
                TextFieldWidget windowFieldHeight;
                window.Widgets.Add(windowFieldHeight = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelHeight.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelHeight.Size.X, windowLabelHeight.Position.Y)
                });

                ButtonWidget windowLabelDepth;
                window.Widgets.Add(windowLabelDepth = new ButtonWidget(Game, "Depth:") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 72f)
                });
                TextFieldWidget windowFieldDepth;
                window.Widgets.Add(windowFieldDepth = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelDepth.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelDepth.Size.X, windowLabelDepth.Position.Y)
                });

                ButtonWidget windowLabelTrileset;
                window.Widgets.Add(windowLabelTrileset = new ButtonWidget(Game, "Trileset:") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(96f, 24f),
                    UpdateBounds = false,
                    LabelCentered = false,
                    Position = new Vector2(0f, 96f)
                });
                TextFieldWidget windowFieldTrileset;
                window.Widgets.Add(windowFieldTrileset = new TextFieldWidget(Game) {
                    Size = new Vector2(window.Size.X - windowLabelTrileset.Size.X, 24f),
                    UpdateBounds = false,
                    Position = new Vector2(windowLabelTrileset.Size.X, windowLabelTrileset.Position.Y)
                });

                window.Widgets.Add(new ButtonWidget(Game, "CREATE", delegate() {
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
                }) {
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });
            }));

            TextFieldWidget fieldOpen;
            button.Widgets.Add(new ButtonWidget(Game, "Open", new EditorWidget[] {
                fieldOpen = new TextFieldWidget(Game, "", "Levels") {
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "LOAD", delegate() {
                    GameState.Loading = true;
                    SkipLoading = 8;
                    LevelManager.ChangeLevel(fieldOpen.Text);
                }) {
                    LabelCentered = true
                }
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
                Widgets.Add(window = new ContainerWidget(Game) {
                    Size = new Vector2(256f, 48f),
                    Label = "Overwrite level?"
                });
                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
                window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

                window.Widgets.Add(new ButtonWidget(Game, "Level already existing. Overwrite?") {
                    Background = new Color(EditorWidget.DefaultBackground, 0f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, 0f)
                });

                window.Widgets.Add(new ButtonWidget(Game, "YES", save) {
                    Size = new Vector2(window.Size.X / 2f, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, 24f)
                });

                window.Widgets.Add(new ButtonWidget(Game, "NO", windowHeader.CloseButtonWidget.Action) {
                    Size = new Vector2(window.Size.X / 2f, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(window.Size.X / 2f, 24f)
                });
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

            TextFieldWidget fieldPPT;
            button.Widgets.Add(new ButtonWidget(Game, "Pixels per Trixel", new EditorWidget[] {
                fieldPPT = new TextFieldWidget(Game, "", new string[] {
                    "0.25",
                    "1",
                    "2",
                    "3",
                    "4"
                }) {
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    CameraManager.PixelsPerTrixel = float.Parse(fieldPPT.Text);
                }) {
                    LabelCentered = true
                }
            }));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Level"));
            button.Background.A = 0;
            TextFieldWidget fieldName;
            TextFieldWidget fieldWidth;
            TextFieldWidget fieldHeight;
            TextFieldWidget fieldDepth;
            button.Widgets.Add(new ButtonWidget(Game, "Settings", new EditorWidget[] {
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Name:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldName = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.Name,
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Width:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWidth = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.X).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldHeight = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.Y).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Depth:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldDepth = new TextFieldWidget(Game) {
                        RefreshValue = () => ((int) LevelManager.Size.Z).ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    GameLevelManagerHelper.Level.Name = fieldName.Text;
                    GameLevelManagerHelper.Level.Size = new Vector3(
                        int.Parse(fieldWidth.Text),
                        int.Parse(fieldHeight.Text),
                        int.Parse(fieldDepth.Text)
                    );
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSpawnX;
            TextFieldWidget fieldSpawnY;
            TextFieldWidget fieldSpawnZ;
            TextFieldWidget fieldSpawnFace;
            button.Widgets.Add(new ButtonWidget(Game, "Spawnpoint", new EditorWidget[] {
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "X:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnX = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition.Id.X.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Y:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnY = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition.Id.Y.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Z:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnZ = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition.Id.Z.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Face:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnFace = new TextFieldWidget(Game, "", Enum.GetNames(typeof(FaceOrientation))) {
                        RefreshValue = () => LevelManager.StartingPosition.Face.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    LevelManager.StartingPosition.Id.X = int.Parse(fieldSpawnX.Text);
                    LevelManager.StartingPosition.Id.Y = int.Parse(fieldSpawnY.Text);
                    LevelManager.StartingPosition.Id.Z = int.Parse(fieldSpawnZ.Text);
                    LevelManager.StartingPosition.Face = (FaceOrientation) Enum.Parse(typeof(FaceOrientation), fieldSpawnFace.Text);
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSky;
            button.Widgets.Add(new ButtonWidget(Game, "Sky", new EditorWidget[] {
                fieldSky = new TextFieldWidget(Game, "", "Skies") {
                    RefreshValue = () => (LevelManager.Sky != null) ? LevelManager.Sky.Name : "",
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    Sky sky = fieldSky.Text.Length > 0 ? CMProvider.CurrentLevel.Load<Sky>("Skies/" + fieldSky.Text) : null;
                    if (sky != null) {
                        GameLevelManagerHelper.Level.Sky = sky;
                        GameLevelManagerHelper.Level.SkyName = sky.Name;
                        LevelManager.ChangeSky(sky);
                    }
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSong;
            button.Widgets.Add(new ButtonWidget(Game, "Song", new EditorWidget[] {
                fieldSong = new TextFieldWidget(Game, "", "Music") {
                    RefreshValue = () => (LevelManager.Song != null) ? LevelManager.SongName : "",
                    Size = new Vector2(160f, 24f),
                    Position = new Vector2(0f, 0f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    TrackedSong song = fieldSong.Text.Length > 0 ? CMProvider.CurrentLevel.Load<TrackedSong>("Music/" + fieldSong.Text) : null;
                    if (song != null) {
                        song.Initialize();
                        GameLevelManagerHelper.Level.Song = song;
                        LevelManager.SongChanged = true;
                        SoundManager.PlayNewSong(song.Name);
                        SoundManager.UpdateSongActiveTracks();
                    }
                    GameLevelManagerHelper.Level.SongName = song != null ? song.Name : null;
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldWaterHeight;
            TextFieldWidget fieldWaterType;
            button.Widgets.Add(new ButtonWidget(Game, "Water", new EditorWidget[] {
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Type:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWaterType = new TextFieldWidget(Game, "", Enum.GetNames(typeof(LiquidType))) {
                        RefreshValue = () => LevelManager.WaterType.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    },
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new EditorWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(EditorWidget.DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldWaterHeight = new TextFieldWidget(Game, "") {
                        RefreshValue = () => LevelManager.WaterHeight.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    },
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    LevelManager.WaterType = (LiquidType) Enum.Parse(typeof(LiquidType), fieldWaterType.Text);
                    LevelManager.WaterHeight = float.Parse(fieldWaterHeight.Text);
                }) {
                    LabelCentered = true
                }
            }));


            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Scripting"));
            button.Background.A = 0;

            button.Widgets.Add(new ButtonWidget(Game, "Volumes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Volumes"
                });
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (Volume volume in LevelManager.Volumes.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+volume.Id+"] "+EditorUtils.ToString(volume.From)+" - "+EditorUtils.ToString(volume.To)) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));

            button.Widgets.Add(new ButtonWidget(Game, "Scripts", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Scripts";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (Script script in LevelManager.Scripts.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+script.Id+"] "+script.Name+" ("+(script.Triggerless ? "none" : (script.Triggers.Count == 1 ? script.Triggers[0].ToString() : "..."))+") : "+(script.Actions.Count == 1 ? script.Actions[0].ToString() : "...")) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Art Objects", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Art Objects";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (ArtObjectInstance ao in LevelManager.ArtObjects.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+ao.Id+"] "+ao.ArtObjectName+": "+EditorUtils.ToString(ao.Position)) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Background Planes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Background Planes";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (BackgroundPlane bp in LevelManager.BackgroundPlanes.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+bp.Id+"] "+bp.TextureName+": "+EditorUtils.ToString(bp.Position)) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Groups", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 256f;
                window.Size.Y = 24f;
                window.Label = "Groups";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (TrileGroup group_ in LevelManager.Groups.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+group_.Id+"] "+group_.Name) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "NPCs", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 256f;
                window.Size.Y = 24f;
                window.Label = "NPCs";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (NpcInstance npc in LevelManager.NonPlayerCharacters.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, "["+npc.Id+"] "+npc.Name) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Paths", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 128f;
                window.Size.Y = 24f;
                window.Label = "Paths";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (MovementPath path in LevelManager.Paths.Values) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, path.Id.ToString()) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Muted Loops", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true
                });
                window.Size.X = 512f;
                window.Size.Y = 24f;
                window.Label = "Muted Loops";
                window.Widgets.Add(new WindowHeaderWidget(Game));

                int i = 0;
                foreach (string loop in LevelManager.MutedLoops) {
                    window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                        new ButtonWidget(Game, loop) {
                            Size = new Vector2(window.Size.X - 48f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        },
                        new ButtonWidget(Game, "C") {
                            Background = new Color(0f, 0f, 0.125f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 48f, 0f)
                        },
                        new ButtonWidget(Game, "X") {
                            Background = new Color(0.5f, 0f, 0f, 1f),
                            Size = new Vector2(24f, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(window.Size.X - 24f, 0f)
                        }
                    }) {
                        Size = new Vector2(window.Size.X, 24f),
                        Background = new Color(EditorWidget.DefaultBackground, 0f)
                    });

                    i++;
                }

                window.Size.Y += i * 24f;
                window.Size.Y = Math.Min(512f, window.Size.Y);

                window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                }) {
                    Background = new Color(0f, 0.125f, 0f, 1f),
                    Size = new Vector2(window.Size.X, 24f),
                    UpdateBounds = false,
                    LabelCentered = true,
                    Position = new Vector2(0f, window.Size.Y - 24f)
                });

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Ambience Tracks", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "Ambience Tracks"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (AmbienceTrack track in LevelManager.AmbienceTracks) {
                        window.Widgets.Add(new ContainerWidget(Game, new EditorWidget[] {
                            new ButtonWidget(Game, track.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.AmbienceTracks.Remove(track);
                                SoundManager.PlayNewAmbience();
                                window.Refresh();
                            }) {
                                Background = new Color(0.5f, 0f, 0f, 1f),
                                Size = new Vector2(24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = true,
                                Position = new Vector2(window.Size.X - 24f, 0f)
                            }
                        }) {
                            Size = new Vector2(window.Size.X, 24f),
                            Background = new Color(EditorWidget.DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 144f),
                            Label = "Add Ambience Track"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(EditorWidget.DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });
                        Dictionary<string, string>.KeyCollection keysOrig = SoundManager.GetPrivate<Dictionary<string, string>>("MusicAliases").Keys;
                        List<string> keys = new List<string>();
                        foreach (string keyOrig in keysOrig) {
                            if (!keyOrig.StartsWith("ambience")) {
                                continue;
                            }
                            string key = keyOrig.Replace("\\", " ^ ").Replace("ambience", "Ambience");
                            keys.Add(key);
                        }
                        windowFieldName.Fill(keys);

                        CheckboxWidget windowCheckDawn;
                        windowAdd.Widgets.Add(windowCheckDawn = new CheckboxWidget(Game, "Dawn") {
                            Background = new Color(EditorWidget.DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });

                        CheckboxWidget windowCheckDay;
                        windowAdd.Widgets.Add(windowCheckDay = new CheckboxWidget(Game, "Day") {
                            Background = new Color(EditorWidget.DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });

                        CheckboxWidget windowCheckDusk;
                        windowAdd.Widgets.Add(windowCheckDusk = new CheckboxWidget(Game, "Dusk") {
                            Background = new Color(EditorWidget.DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });

                        CheckboxWidget windowCheckNight;
                        windowAdd.Widgets.Add(windowCheckNight = new CheckboxWidget(Game, "Night") {
                            Background = new Color(EditorWidget.DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            LevelManager.AmbienceTracks.Add(new AmbienceTrack() {
                                Name = windowFieldName.Text,
                                Dawn = windowCheckDawn.Checked,
                                Day = windowCheckDay.Checked,
                                Dusk = windowCheckDusk.Checked,
                                Night = windowCheckNight.Checked
                            });
                            SoundManager.PlayNewAmbience();
                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, windowAdd.Size.Y - 24f)
                        });

                        windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
                        windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
                    }) {
                        Background = new Color(0f, 0.125f, 0f, 1f),
                        Size = new Vector2(window.Size.X, 24f),
                        UpdateBounds = false,
                        LabelCentered = true,
                        Position = new Vector2(0f, window.Size.Y - 24f)
                    });

                    return null;
                };
                window.Refresh();

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
            GTR = new GlyphTextRenderer(Game);

            PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
            CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
            ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
            GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");

            SpriteBatch = new SpriteBatch(GraphicsDevice);
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

            if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.Paused) {
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
            if (GameState.Loading || GameState.InMap || GameState.InMenuCube || GameState.Paused || !FEZMod.Preloaded) {
                return;
            }

            Viewport viewport = GraphicsDevice.Viewport;
            float viewScale = SettingsManager.GetViewScale(GraphicsDevice);

            float cursorScale = viewScale * 2f;
            Point cursorPosition = SettingsManager.PositionInViewport(MouseState);

            GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            SpriteBatch.BeginPoint();

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
            for (int i = widgets.Count - 1; i >= 0; i--) {
                EditorWidget widget = widgets[i];
                widget.LevelEditor = this;
                if (update) {
                    widget.Update(gameTime);
                }
                bool cursorOnChild = cursorOnWidget;
                if (widget.ShowChildren) {
                    cursorOnChild = cursorOnWidget || UpdateWidgets(gameTime, widget.Widgets, false);
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

