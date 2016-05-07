using FezGame.Mod;
using FezEngine.Mod;
using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using FezGame.Editor;
using FezGame.Mod.Gui;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Input;
#if FNA
using Microsoft.Xna.Framework.Input;
using SDL2;
#endif

namespace FezGame.Components {
    public partial class LevelEditor : DrawableGameComponent, ILevelEditor, IGuiHandler {
        
        public List<GuiWidget> Widgets { get; set; }
        public List<Action> Scheduled { get; set; }

        public InfoWidget InfoWidget;
        public TopBarWidget TopBarWidget;
        public AssetPickerWidget AssetPickerWidget;
        public List<AssetPickerWidget> AssetPickerWidgets = new List<AssetPickerWidget>();
        public ContainerWidget AssetPickerPickerWidget;
        public List<ButtonWidget> AssetPickerLabels = new List<ButtonWidget>();
        
        protected GuiWidget DraggingWidget;
        protected GuiWidget FocusedWidget;

        public Color DefaultForeground {
            get {
                return FezEditor.Settings.DefaultForeground;
            }
            set {
                FezEditor.Settings.DefaultForeground = value;
            }
        }
        public Color DefaultBackground {
            get {
                return FezEditor.Settings.DefaultBackground;
            }
            set {
                FezEditor.Settings.DefaultBackground = value;
            }
        }
        
        protected void SetupGui() {
            Widgets = new List<GuiWidget>();

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
                    Background = new Color(DefaultBackground, 0f),
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
                windowFieldName.Fill(ContentPaths.Levels);

                ButtonWidget windowLabelWidth;
                window.Widgets.Add(windowLabelWidth = new ButtonWidget(Game, "Width:") {
                    Background = new Color(DefaultBackground, 0f),
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
                    Background = new Color(DefaultBackground, 0f),
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
                    Background = new Color(DefaultBackground, 0f),
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
                    Background = new Color(DefaultBackground, 0f),
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
                windowFieldTrileset.Fill(ContentPaths.TrileSets);

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
            button.Widgets.Add(new ButtonWidget(Game, "Open", new GuiWidget[] {
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

            button.Widgets.Add(new ButtonWidget(Game, "Recreate thumbnail", new GuiWidget[] {
                new ButtonWidget(Game, "128px (default)", () => CreateThumbnail(128)),
                new ButtonWidget(Game, "256px", () => CreateThumbnail(256)),
                new ButtonWidget(Game, "512px", () => CreateThumbnail(512))
            }));
            /*button.Widgets.Add(new ButtonWidget(Game, "Save (XML)", () => Save()));
            button.Widgets.Add(new ButtonWidget(Game, "Save (binary)", () => Save(true)));*/
            button.Widgets.Add(new ButtonWidget(Game, "Save", () => Save(true)));

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "View"));
            button.Background.A = 0;

            button.Widgets.Add(new ButtonWidget(Game, "Perspective", new GuiWidget[] {
                new ButtonWidget(Game, "Front", () => CameraManager.ChangeViewpoint(Viewpoint.Front)),
                new ButtonWidget(Game, "Left", () => CameraManager.ChangeViewpoint(Viewpoint.Left)),
                new ButtonWidget(Game, "Back", () => CameraManager.ChangeViewpoint(Viewpoint.Back)),
                new ButtonWidget(Game, "Right", () => CameraManager.ChangeViewpoint(Viewpoint.Right)),
                new ButtonWidget(Game, "Perspective", () => CameraManager.ChangeViewpoint(Viewpoint.Perspective))
            }));

            TextFieldWidget fieldPPT;
            button.Widgets.Add(new ButtonWidget(Game, "Pixels per Trixel", new GuiWidget[] {
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
            button.Widgets.Add(new ButtonWidget(Game, "Settings", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Name:") {
                        Background = new Color(DefaultBackground, 0f),
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
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Width:") {
                        Background = new Color(DefaultBackground, 0f),
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
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(DefaultBackground, 0f),
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
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Depth:") {
                        Background = new Color(DefaultBackground, 0f),
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
            button.Widgets.Add(new ButtonWidget(Game, "Spawnpoint", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "X:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnX = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.X.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Y:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnY = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.Y.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Z:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnZ = new TextFieldWidget(Game) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "0" : LevelManager.StartingPosition.Id.Z.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Face:") {
                        Background = new Color(DefaultBackground, 0f),
                        LabelCentered = false,
                        Position = new Vector2(0f, 0f)
                    },
                    fieldSpawnFace = new TextFieldWidget(Game, "", Enum.GetNames(typeof(FaceOrientation))) {
                        RefreshValue = () => LevelManager.StartingPosition == null ? "Front" : LevelManager.StartingPosition.Face.ToString(),
                        Size = new Vector2(160f, 24f),
                        Position = new Vector2(96f, 0f)
                    }
                }) {
                    Size = new Vector2(256f, 24f)
                },
                new ButtonWidget(Game, "CHANGE", delegate() {
                    if (LevelManager.StartingPosition == null) {
                        LevelManager.StartingPosition = new TrileFace();
                    }
                    LevelManager.StartingPosition.Id.X = int.Parse(fieldSpawnX.Text);
                    LevelManager.StartingPosition.Id.Y = int.Parse(fieldSpawnY.Text);
                    LevelManager.StartingPosition.Id.Z = int.Parse(fieldSpawnZ.Text);
                    LevelManager.StartingPosition.Face = (FaceOrientation) Enum.Parse(typeof(FaceOrientation), fieldSpawnFace.Text);
                }) {
                    LabelCentered = true
                }
            }));

            TextFieldWidget fieldSky;
            button.Widgets.Add(new ButtonWidget(Game, "Sky", new GuiWidget[] {
                fieldSky = new TextFieldWidget(Game, "", ContentPaths.Skies) {
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
            button.Widgets.Add(new ButtonWidget(Game, "Song", new GuiWidget[] {
                fieldSong = new TextFieldWidget(Game, "", ContentPaths.Music) {
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
            button.Widgets.Add(new ButtonWidget(Game, "Water", new GuiWidget[] {
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Type:") {
                        Background = new Color(DefaultBackground, 0f),
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
                new ContainerWidget(Game, new GuiWidget[] {
                    new ButtonWidget(Game, "Height:") {
                        Background = new Color(DefaultBackground, 0f),
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

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (Volume volume in LevelManager.Volumes.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+volume.Id+"] "+EditorUtils.ToString(volume.From)+" - "+EditorUtils.ToString(volume.To)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.Volumes.Remove(volume.Id);
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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 192f),
                            Label = "Add Volume"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelId;
                        windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelFrom;
                        windowAdd.Widgets.Add(windowLabelFrom = new ButtonWidget(Game, "From:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldFrom;
                        windowAdd.Widgets.Add(windowFieldFrom = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelFrom.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelFrom.Size.X, windowLabelFrom.Position.Y)
                        });

                        ButtonWidget windowLabelTo;
                        windowAdd.Widgets.Add(windowLabelTo = new ButtonWidget(Game, "To:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldTo;
                        windowAdd.Widgets.Add(windowFieldTo = new TextFieldWidget(Game, "1; 1; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelTo.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelTo.Size.X, windowLabelTo.Position.Y)
                        });

                        CheckboxWidget windowCheckFront;
                        windowAdd.Widgets.Add(windowCheckFront = new CheckboxWidget(Game, "Front") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });

                        CheckboxWidget windowCheckLeft;
                        windowAdd.Widgets.Add(windowCheckLeft = new CheckboxWidget(Game, "Left") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });

                        CheckboxWidget windowCheckBack;
                        windowAdd.Widgets.Add(windowCheckBack = new CheckboxWidget(Game, "Back") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });

                        CheckboxWidget windowCheckRight;
                        windowAdd.Widgets.Add(windowCheckRight = new CheckboxWidget(Game, "Right") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 144f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            string[] fromSplit = windowFieldFrom.Text.Split(new char[] {';'});
                            string[] toSplit = windowFieldTo.Text.Split(new char[] {';'});
                            Volume volume = new Volume() {
                                Id = int.Parse(windowFieldId.Text),
                                From = new Vector3(
                                    float.Parse(fromSplit[0].Trim()),
                                    float.Parse(fromSplit[1].Trim()),
                                    float.Parse(fromSplit[2].Trim())
                                ),
                                To = new Vector3(
                                    float.Parse(toSplit[0].Trim()),
                                    float.Parse(toSplit[1].Trim()),
                                    float.Parse(toSplit[2].Trim())
                                )
                            };
                            if (windowCheckFront.Checked) {volume.Orientations.Add(FaceOrientation.Front);}
                            if (windowCheckLeft.Checked) {volume.Orientations.Add(FaceOrientation.Left);}
                            if (windowCheckBack.Checked) {volume.Orientations.Add(FaceOrientation.Back);}
                            if (windowCheckRight.Checked) {volume.Orientations.Add(FaceOrientation.Right);}
                            LevelManager.Volumes.Add(volume.Id, volume);
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
                    window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
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
                        Background = new Color(DefaultBackground, 0f)
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
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Art Objects"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (ArtObjectInstance ao in LevelManager.ArtObjects.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+ao.Id+"] "+ao.ArtObjectName+": "+EditorUtils.ToString(ao.Position)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                int trileGroupId = ao.ActorSettings.AttachedGroup.HasValue ? ao.ActorSettings.AttachedGroup.Value : -1;
                                if (LevelManager.Groups.ContainsKey(trileGroupId)) {
                                    TrileGroup trileGroup = LevelManager.Groups[trileGroupId];
                                    while (trileGroup.Triles.Count > 0) {
                                        LevelManager.ClearTrile(trileGroup.Triles[0]);
                                    }
                                    LevelManager.Groups.Remove(trileGroupId);
                                }
                                LevelManager.ArtObjects.Remove(ao.Id);
                                ao.Dispose();
                                LevelMaterializer.RegisterSatellites();

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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = i * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);
                    
                    return null;
                };

                window.Refresh();

                window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
                window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            }));
            button.Widgets.Add(new ButtonWidget(Game, "Background Planes", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Background Planes"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (BackgroundPlane bp in LevelManager.BackgroundPlanes.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+bp.Id+"] "+bp.TextureName+": "+EditorUtils.ToString(bp.Position)) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.RemovePlane(bp);
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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 240f),
                            Label = "Add Background Plane"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        int maxID = 0;
                        foreach (int id in LevelManager.BackgroundPlanes.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }

                        ButtonWidget windowLabelId;
                        windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldName;
                        windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game, "", ContentPaths.BackgroundPlanes) {
                            Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });

                        ButtonWidget windowLabelPosition;
                        windowAdd.Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldPosition;
                        windowAdd.Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelPosition.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
                        });

                        ButtonWidget windowLabelRotation;
                        windowAdd.Widgets.Add(windowLabelRotation = new ButtonWidget(Game, "Rotation:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });
                        TextFieldWidget windowFieldRotation;
                        windowAdd.Widgets.Add(windowFieldRotation = new TextFieldWidget(Game, "0; 0; 0; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelRotation.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelRotation.Size.X, windowLabelRotation.Position.Y)
                        });

                        ButtonWidget windowLabelScale;
                        windowAdd.Widgets.Add(windowLabelScale = new ButtonWidget(Game, "Scale:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });
                        TextFieldWidget windowFieldScale;
                        windowAdd.Widgets.Add(windowFieldScale = new TextFieldWidget(Game, "1; 1; 1") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelScale.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelScale.Size.X, windowLabelScale.Position.Y)
                        });

                        ButtonWidget windowLabelFilter;
                        windowAdd.Widgets.Add(windowLabelFilter = new ButtonWidget(Game, "Filter:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });
                        TextFieldWidget windowFieldFilter;
                        windowAdd.Widgets.Add(windowFieldFilter = new TextFieldWidget(Game, "#FFFFFFFF") {
                            Size = new Vector2(windowAdd.Size.X - windowLabelFilter.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelFilter.Size.X, windowLabelFilter.Position.Y)
                        });

                        CheckboxWidget windowCheckboxBillboard;
                        windowAdd.Widgets.Add(windowCheckboxBillboard = new CheckboxWidget(Game, "Billboard") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 144f)
                        });

                        CheckboxWidget windowCheckboxLightMap;
                        windowAdd.Widgets.Add(windowCheckboxLightMap = new CheckboxWidget(Game, "Lightmap") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 168f)
                        });

                        CheckboxWidget windowCheckboxOverbright;
                        windowAdd.Widgets.Add(windowCheckboxOverbright = new CheckboxWidget(Game, "Overbright") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 192f)
                        });

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                            string[] rotationSplit = windowFieldRotation.Text.Split(new char[] {';'});
                            string[] scaleSplit = windowFieldScale.Text.Split(new char[] {';'});
                            BackgroundPlane plane = new BackgroundPlane() {
                                Id = int.Parse(windowFieldId.Text),
                                TextureName = windowFieldName.Text,
                                Position = new Vector3(
                                    float.Parse(positionSplit[0].Trim()),
                                    float.Parse(positionSplit[1].Trim()),
                                    float.Parse(positionSplit[2].Trim())
                                ),
                                Rotation = new Quaternion(
                                    float.Parse(rotationSplit[0].Trim()),
                                    float.Parse(rotationSplit[1].Trim()),
                                    float.Parse(rotationSplit[2].Trim()),
                                    float.Parse(rotationSplit[3].Trim())
                                ),
                                Scale = new Vector3(
                                    float.Parse(scaleSplit[0].Trim()),
                                    float.Parse(scaleSplit[1].Trim()),
                                    float.Parse(scaleSplit[2].Trim())
                                ),
                                Billboard = windowCheckboxBillboard.Checked,
                                LightMap = windowCheckboxLightMap.Checked,
                                AllowOverbrightness = windowCheckboxOverbright.Checked
                            };
                            Color filter = new Color();
                            filter.PackedValue = Convert.ToUInt32(windowFieldFilter.Text.Substring(1), 16);
                            plane.Filter = filter;
                            plane.Animated = CMProvider.CurrentLevel.Load<object>("Background Planes/" + plane.TextureName) is AnimatedTexture;
                            plane.HostMesh = ((!plane.Animated) ? LevelMaterializer.StaticPlanesMesh : LevelMaterializer.AnimatedPlanesMesh);
                            plane.Initialize();
                            LevelManager.AddPlane(plane);

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
            button.Widgets.Add(new ButtonWidget(Game, "Groups", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "Groups"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (TrileGroup group_ in LevelManager.Groups.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+group_.Id+"] "+group_.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                while (group_.Triles.Count > 0) {
                                    LevelManager.ClearTrile(group_.Triles[0]);
                                }
                                LevelManager.Groups.Remove(group_.Id);
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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
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
            button.Widgets.Add(new ButtonWidget(Game, "NPCs", delegate() {
                ContainerWidget window;
                Widgets.Add(window = new ContainerWidget(Game) {
                    UpdateBounds = true,
                    Size = new Vector2(256f, 24f),
                    Label = "NPCs"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();

                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (NpcInstance npc in LevelManager.NonPlayerCharacters.Values) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, "["+npc.Id+"] "+npc.Name) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.NonPlayerCharacters.Remove(npc.Id);
                                ServiceHelper.RemoveComponent(npc.State);
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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f * 2f, 240f),
                            Label = "Add NPC",
                            UpdateBounds = false
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        int maxID = 0;
                        foreach (int id in LevelManager.NonPlayerCharacters.Keys) {
                            if (id >= maxID) {
                                maxID = id + 1;
                            }
                        }

                        float columnWidth = 256f;
                        ContainerWidget[] columns = new ContainerWidget[2];
                        for (int ci = columns.Length - 1; ci >= 0; ci--) {
                            columns[ci] = new ContainerWidget(Game) {
                                Position = new Vector2(ci * columnWidth, 0),
                                Size = new Vector2(columnWidth, windowAdd.Size.Y),
                                UpdateBounds = ci != 0,
                                Background = new Color(DefaultBackground, 0f)
                            };
                            windowAdd.Widgets.Add(columns[ci]);
                        }

                        ButtonWidget windowLabelId;
                        columns[0].Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 0f)
                        });
                        TextFieldWidget windowFieldId;
                        columns[0].Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                            Size = new Vector2(columnWidth - windowLabelId.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
                        });

                        ButtonWidget windowLabelName;
                        columns[0].Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });
                        TextFieldWidget windowFieldName;
                        columns[0].Widgets.Add(windowFieldName = new TextFieldWidget(Game, "", ContentPaths.CharacterAnimations) {
                            Size = new Vector2(columnWidth - windowLabelName.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
                        });

                        ButtonWidget windowLabelPosition;
                        columns[0].Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });
                        TextFieldWidget windowFieldPosition;
                        columns[0].Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(columnWidth - windowLabelPosition.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
                        });

                        ButtonWidget windowLabelDestination;
                        columns[0].Widgets.Add(windowLabelDestination = new ButtonWidget(Game, "Destination:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });
                        TextFieldWidget windowFieldDestination;
                        columns[0].Widgets.Add(windowFieldDestination = new TextFieldWidget(Game, "0; 0; 0") {
                            Size = new Vector2(columnWidth - windowLabelDestination.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelDestination.Size.X, windowLabelDestination.Position.Y)
                        });

                        ButtonWidget windowLabelWalkSpeed;
                        columns[0].Widgets.Add(windowLabelWalkSpeed = new ButtonWidget(Game, "Walk speed:") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(96f, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 96f)
                        });
                        TextFieldWidget windowFieldWalkSpeed;
                        columns[0].Widgets.Add(windowFieldWalkSpeed = new TextFieldWidget(Game, "1") {
                            Size = new Vector2(columnWidth - windowLabelWalkSpeed.Size.X, 24f),
                            UpdateBounds = false,
                            Position = new Vector2(windowLabelWalkSpeed.Size.X, windowLabelWalkSpeed.Position.Y)
                        });

                        CheckboxWidget windowCheckboxAvoidsGomez;
                        columns[0].Widgets.Add(windowCheckboxAvoidsGomez = new CheckboxWidget(Game, "Avoids Gomez") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 120f)
                        });

                        CheckboxWidget windowCheckboxOverrideTalk;
                        columns[1].Widgets.Add(windowCheckboxOverrideTalk = new CheckboxWidget(Game, "Override talk") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = false
                        });

                        Dictionary<string, string>.KeyCollection speechLines = typeof(GameText).GetPrivateStatic<Dictionary<string, string>>("Fallback").Keys;
                        columns[1].Widgets.Add(new ButtonWidget(Game, "+ SPEECH", delegate() {
                            ContainerWidget widgetSpeech = null;
                            TextFieldWidget widgetFieldSpeech = null;
                            columns[1].Widgets.Insert(columns[1].Widgets.Count - 2, widgetSpeech = new ContainerWidget(Game, new GuiWidget[] {
                                widgetFieldSpeech = new TextFieldWidget(Game) {
                                    Size = new Vector2(columnWidth - 24f, 24f),
                                    UpdateBounds = false,
                                    Position = new Vector2(0f, 0f)
                                },
                                new ButtonWidget(Game, "X", delegate() {
                                    columns[1].Widgets.Remove(widgetSpeech);
                                }) {
                                    Background = new Color(0.5f, 0f, 0f, 1f),
                                    Size = new Vector2(24f, 24f),
                                    UpdateBounds = false,
                                    LabelCentered = true,
                                    Position = new Vector2(columnWidth - 24f, 0f)
                                }
                            }) {
                                Size = new Vector2(columnWidth, 24f),
                                Background = new Color(DefaultBackground, 0f)
                            });
                            widgetFieldSpeech.Fill(speechLines);
                        }) {
                            Background = new Color(0f, 0.125f, 0f, 1f),
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = true
                        });

                        columns[0].Widgets.Add(new ButtonWidget(Game, "ADD NPC", delegate() {
                            string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                            string[] destinationSplit = windowFieldDestination.Text.Split(new char[] {';'});
                            NpcInstance npc = new NpcInstance() {
                                Id = int.Parse(windowFieldId.Text),
                                Name = windowFieldName.Text,
                                Position = new Vector3(
                                    float.Parse(positionSplit[0].Trim()),
                                    float.Parse(positionSplit[1].Trim()),
                                    float.Parse(positionSplit[2].Trim())
                                ),
                                DestinationOffset = new Vector3(
                                    float.Parse(destinationSplit[0].Trim()),
                                    float.Parse(destinationSplit[1].Trim()),
                                    float.Parse(destinationSplit[2].Trim())
                                ),
                                WalkSpeed = float.Parse(windowFieldWalkSpeed.Text),
                                AvoidsGomez = windowCheckboxAvoidsGomez.Checked
                            };

                            for (int ri = 0; ri < columns[1].Widgets.Count; ri++) {
                                GuiWidget rowWidget = columns[1].Widgets[ri];
                                if (!(rowWidget is ContainerWidget) || rowWidget.Widgets.Count != 2) {
                                    continue;
                                }
                                TextFieldWidget lineWidget = (TextFieldWidget) rowWidget.Widgets[0];
                                SpeechLine line = new SpeechLine() {
                                    Text = lineWidget.Text
                                };
                                if (ri == 0 && windowCheckboxOverrideTalk.Checked) {
                                    line.OverrideContent = new NpcActionContent() {
                                        AnimationName = "Talk"
                                    };
                                }
                                npc.Speech.Add(line);
                            }
                                

                            string root = ContentPaths.CharacterAnimations + "/" + npc.Name;
                            IEnumerable<string> list = CMProvider.GetAllIn(root);
                            foreach (string item_ in list) {
                                string item = item_.Substring(root.Length + 1).ToUpper();
                                NpcAction key;
                                if (!Enum.TryParse<NpcAction>(item, true, out key)) {
                                    continue;
                                }
                                npc.Actions[key] = new NpcActionContent() {
                                    AnimationName = key.ToString()
                                };
                            }

                            LevelManager.NonPlayerCharacters[npc.Id] = npc;
                            npc.State = new GameNpcState(Game, npc);
                            ServiceHelper.AddComponent(npc.State);
                            npc.State.Initialize();
                            ServiceHelper.Get<NpcHost>().GetPrivate<List<NpcState>>("NpcStates").Add(npc.State);

                            windowAddHeader.CloseButtonWidget.Action();
                            window.Refresh();
                        }) {
                            Size = new Vector2(columnWidth, 24f),
                            UpdateBounds = false,
                            LabelCentered = true,
                            Position = new Vector2(0f, columns[0].Size.Y - 24f)
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
                    window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
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
                        Background = new Color(DefaultBackground, 0f)
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
                    UpdateBounds = true,
                    Size = new Vector2(512f, 24f),
                    Label = "Muted Loops"
                });

                window.RefreshValue = delegate() {
                    window.Widgets.Clear();
                    window.Widgets.Add(new WindowHeaderWidget(Game));

                    int i = 0;
                    foreach (string loop in LevelManager.MutedLoops) {
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                            new ButtonWidget(Game, loop) {
                                Size = new Vector2(window.Size.X - 24f, 24f),
                                UpdateBounds = false,
                                LabelCentered = false,
                                Position = new Vector2(0f, 0f)
                            },
                            new ButtonWidget(Game, "X", delegate() {
                                LevelManager.MutedLoops.Remove(loop);
                                SoundManager.UpdateSongActiveTracks();
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
                            Background = new Color(DefaultBackground, 0f)
                        });

                        i++;
                    }

                    window.Size.Y = (i+1) * 24f;
                    window.Size.Y = Math.Min(512f, window.Size.Y);

                    window.Widgets.Add(new ButtonWidget(Game, "+", delegate() {
                        ContainerWidget windowAdd;
                        Widgets.Add(windowAdd = new ContainerWidget(Game) {
                            Size = new Vector2(256f, 48f),
                            Label = "Add Muted Loop"
                        });
                        WindowHeaderWidget windowAddHeader;
                        windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

                        ButtonWidget windowLabelName;
                        windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                            Background = new Color(DefaultBackground, 0f),
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
                        List<Loop> keysOrig = SoundManager.CurrentlyPlayingSong.Loops;
                        List<string> keys = new List<string>();
                        foreach (Loop keyOrig in keysOrig) {
                            keys.Add(keyOrig.Name);
                        }
                        windowFieldName.Fill(keys);

                        windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                            LevelManager.MutedLoops.Add(windowFieldName.Text);
                            SoundManager.UpdateSongActiveTracks();
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
                        window.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
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
                            Background = new Color(DefaultBackground, 0f)
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
                            Background = new Color(DefaultBackground, 0f),
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
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 24f)
                        });

                        CheckboxWidget windowCheckDay;
                        windowAdd.Widgets.Add(windowCheckDay = new CheckboxWidget(Game, "Day") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 48f)
                        });

                        CheckboxWidget windowCheckDusk;
                        windowAdd.Widgets.Add(windowCheckDusk = new CheckboxWidget(Game, "Dusk") {
                            Background = new Color(DefaultBackground, 0f),
                            Size = new Vector2(windowAdd.Size.X, 24f),
                            UpdateBounds = false,
                            LabelCentered = false,
                            Position = new Vector2(0f, 72f)
                        });

                        CheckboxWidget windowCheckNight;
                        windowAdd.Widgets.Add(windowCheckNight = new CheckboxWidget(Game, "Night") {
                            Background = new Color(DefaultBackground, 0f),
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

            TopBarWidget.Widgets.Add(button = new ButtonWidget(Game, "Settings"));
            button.Background.A = 0;
            button.Widgets.Add(new ButtonWidget(Game, "Invert theme", delegate() {
                FezEditor.Settings.DefaultBackground.R = (byte) (255 - FezEditor.Settings.DefaultBackground.R);
                FezEditor.Settings.DefaultBackground.G = (byte) (255 - FezEditor.Settings.DefaultBackground.G);
                FezEditor.Settings.DefaultBackground.B = (byte) (255 - FezEditor.Settings.DefaultBackground.B);

                FezEditor.Settings.DefaultForeground.R = (byte) (255 - FezEditor.Settings.DefaultForeground.R);
                FezEditor.Settings.DefaultForeground.G = (byte) (255 - FezEditor.Settings.DefaultForeground.G);
                FezEditor.Settings.DefaultForeground.B = (byte) (255 - FezEditor.Settings.DefaultForeground.B);

                foreach (GuiWidget widget in Widgets) {
                    widget.UpdateTheme();
                }
            }));
            TextFieldWidget fieldBackupHistory;
            button.Widgets.Add(new ContainerWidget(Game, new GuiWidget[] {
                new ButtonWidget(Game, "Backup Depth:") {
                    Background = new Color(DefaultBackground, 0f),
                    LabelCentered = false,
                    Position = new Vector2(0f, 0f)
                },
                fieldBackupHistory = new TextFieldWidget(Game) {
                    RefreshValue = () => FezEditor.Settings.BackupHistory.ToString(),
                    Size = new Vector2(48f, 24f),
                    Position = new Vector2(144f, 0f)
                }
            }) {
                Size = new Vector2(192f, 24f)
            });
            button.Widgets.Add(new ButtonWidget(Game, "Save", delegate() {
                FezEditor.Settings.BackupHistory = int.Parse(fieldBackupHistory.Text);
                FezEditor.Settings.Save();
            }));

            //INFO
            Widgets.Add(InfoWidget = new EditorInfoWidget(Game));

            //ASSET PICKER
            AssetPickerWidgets.Add(AssetPickerWidget = new TrilePickerWidget(Game));
            AssetPickerLabels.Add(new ButtonWidget(Game, "Triles"));
            AssetPickerWidgets.Add(new ArtObjectPickerWidget(Game));
            AssetPickerLabels.Add(new ButtonWidget(Game, "Art Objects"));
            
            Widgets.Add(AssetPickerPickerWidget = new ContainerWidget(Game) {
                UpdateBounds = false,
                Size = new Vector2(0f, 24f)
            });
            AssetPickerPickerWidget.Widgets.AddRange(AssetPickerLabels);
            
            Widgets.AddRange(AssetPickerWidgets);
        }
        
        public void ShowTrilePlacementWindow(int id = 0) {
            ContainerWidget window;
            Widgets.Add(window = new ContainerWidget(Game) {
                Size = new Vector2(256f, 144f),
                Label = "Add trile"
            });
            window.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (window.Size.X / 2);
            window.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (window.Size.Y / 2);
            WindowHeaderWidget windowHeader;
            window.Widgets.Add(windowHeader = new WindowHeaderWidget(Game));

            ButtonWidget windowLabelID;
            window.Widgets.Add(windowLabelID = new ButtonWidget(Game, "ID:") {
                Size = new Vector2(96f, 24f),
                Position = new Vector2(0f, 0f),
                UpdateBounds = false,
                LabelCentered = false
            });
            windowLabelID.Background.A = 0;
            TextFieldWidget windowFieldID;
            window.Widgets.Add(windowFieldID = new TextFieldWidget(Game, id.ToString()) {
                Size = new Vector2(window.Size.X - windowLabelID.Size.X, 24f),
                Position = new Vector2(windowLabelID.Size.X, windowLabelID.Position.Y),
                UpdateBounds = false
            });

            ButtonWidget windowLabelX;
            window.Widgets.Add(windowLabelX = new ButtonWidget(Game, "X:") {
                Size = new Vector2(96f, 24f),
                Position = new Vector2(0f, 24f),
                UpdateBounds = false,
                LabelCentered = false
            });
            TextFieldWidget windowFieldX;
            window.Widgets.Add(windowFieldX = new TextFieldWidget(Game, ((int) PlayerManager.Position.X).ToString()) {
                Size = new Vector2(window.Size.X - windowLabelX.Size.X, 24f),
                Position = new Vector2(windowLabelX.Size.X, windowLabelX.Position.Y),
                UpdateBounds = false
            });

            ButtonWidget windowLabelY;
            window.Widgets.Add(windowLabelY = new ButtonWidget(Game, "Y:") {
                Size = new Vector2(96f, 24f),
                Position = new Vector2(0f, 48f),
                UpdateBounds = false,
                LabelCentered = false
            });
            TextFieldWidget windowFieldY;
            window.Widgets.Add(windowFieldY = new TextFieldWidget(Game, ((int) PlayerManager.Position.Y).ToString()) {
                Size = new Vector2(window.Size.X - windowLabelY.Size.X, 24f),
                Position = new Vector2(windowLabelY.Size.X, windowLabelY.Position.Y),
                UpdateBounds = false
            });

            ButtonWidget windowLabelZ;
            window.Widgets.Add(windowLabelZ = new ButtonWidget(Game, "Z:") {
                Size = new Vector2(96f, 24f),
                Position = new Vector2(0f, 72f),
                UpdateBounds = false,
                LabelCentered = false
            });
            TextFieldWidget windowFieldZ;
            window.Widgets.Add(windowFieldZ = new TextFieldWidget(Game, ((int) PlayerManager.Position.Z).ToString()) {
                Size = new Vector2(window.Size.X - windowLabelZ.Size.X, 24f),
                Position = new Vector2(windowLabelZ.Size.X, windowLabelZ.Position.Y),
                UpdateBounds = false
            });

            ButtonWidget windowLabelFace;
            window.Widgets.Add(windowLabelFace = new ButtonWidget(Game, "Face:") {
                Size = new Vector2(96f, 24f),
                Position = new Vector2(0f, 96f),
                UpdateBounds = false,
                LabelCentered = false
            });
            TextFieldWidget windowFieldFace;
            window.Widgets.Add(windowFieldFace = new TextFieldWidget(Game, LevelManager.StartingPosition.Face.ToString(),
                Enum.GetNames(typeof(FaceOrientation))) {
                Size = new Vector2(window.Size.X - windowLabelFace.Size.X, 24f),
                Position = new Vector2(windowLabelFace.Size.X, windowLabelFace.Position.Y),
                UpdateBounds = false
            });

            ButtonWidget windowButtonCreate;
            window.Widgets.Add(windowButtonCreate = new ButtonWidget(Game, "CREATE", delegate() {
                int trileId = int.Parse(windowFieldID.Text);
                TrileInstance trile = CreateNewTrile(trileId,new TrileEmplacement(
                                        int.Parse(windowFieldX.Text),
                                        int.Parse(windowFieldY.Text),
                                        int.Parse(windowFieldZ.Text)
                ));
                trile.Phi = ((FaceOrientation) Enum.Parse(typeof(FaceOrientation), windowFieldFace.Text)).ToPhi();
                AddTrile(trile);
                windowHeader.CloseButtonWidget.Action();
            }) {
                Size = new Vector2(window.Size.X, 24f),
                Position = new Vector2(0f, window.Size.Y - 24f),
                UpdateBounds = false,
                LabelCentered = true
            });
        }
        
        public void ShowArtObjectPlacementWindow(string name = "") {
            ContainerWidget windowAdd;
            Widgets.Add(windowAdd = new ContainerWidget(Game) {
                Size = new Vector2(256f, 168f),
                Label = "Add Art Object"
            });
            WindowHeaderWidget windowAddHeader;
            windowAdd.Widgets.Add(windowAddHeader = new WindowHeaderWidget(Game));

            int maxID = 0;
            foreach (int id in LevelManager.ArtObjects.Keys) {
                if (id >= maxID) {
                    maxID = id + 1;
                }
            }

            ButtonWidget windowLabelId;
            windowAdd.Widgets.Add(windowLabelId = new ButtonWidget(Game, "ID:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 0f)
            });
            TextFieldWidget windowFieldId;
            windowAdd.Widgets.Add(windowFieldId = new TextFieldWidget(Game, maxID.ToString()) {
                Size = new Vector2(windowAdd.Size.X - windowLabelId.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelId.Size.X, windowLabelId.Position.Y)
            });

            ButtonWidget windowLabelName;
            windowAdd.Widgets.Add(windowLabelName = new ButtonWidget(Game, "Name:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 24f)
            });
            TextFieldWidget windowFieldName;
            windowAdd.Widgets.Add(windowFieldName = new TextFieldWidget(Game, name, ContentPaths.ArtObjects) {
                Size = new Vector2(windowAdd.Size.X - windowLabelName.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelName.Size.X, windowLabelName.Position.Y)
            });

            ButtonWidget windowLabelPosition;
            windowAdd.Widgets.Add(windowLabelPosition = new ButtonWidget(Game, "Position:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 48f)
            });
            TextFieldWidget windowFieldPosition;
            windowAdd.Widgets.Add(windowFieldPosition = new TextFieldWidget(Game, "0; 0; 0") {
                Size = new Vector2(windowAdd.Size.X - windowLabelPosition.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelPosition.Size.X, windowLabelPosition.Position.Y)
            });

            ButtonWidget windowLabelRotation;
            windowAdd.Widgets.Add(windowLabelRotation = new ButtonWidget(Game, "Rotation:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 72f)
            });
            TextFieldWidget windowFieldRotation;
            windowAdd.Widgets.Add(windowFieldRotation = new TextFieldWidget(Game, "0; 0; 0; 1") {
                Size = new Vector2(windowAdd.Size.X - windowLabelRotation.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelRotation.Size.X, windowLabelRotation.Position.Y)
            });

            ButtonWidget windowLabelCenter;
            windowAdd.Widgets.Add(windowLabelCenter = new ButtonWidget(Game, "Center:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 96f)
            });
            TextFieldWidget windowFieldCenter;
            windowAdd.Widgets.Add(windowFieldCenter = new TextFieldWidget(Game, "0; 0; 0") {
                Size = new Vector2(windowAdd.Size.X - windowLabelCenter.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelCenter.Size.X, windowLabelCenter.Position.Y)
            });

            ButtonWidget windowLabelScale;
            windowAdd.Widgets.Add(windowLabelScale = new ButtonWidget(Game, "Scale:") {
                Background = new Color(DefaultBackground, 0f),
                Size = new Vector2(96f, 24f),
                UpdateBounds = false,
                LabelCentered = false,
                Position = new Vector2(0f, 120f)
            });
            TextFieldWidget windowFieldScale;
            windowAdd.Widgets.Add(windowFieldScale = new TextFieldWidget(Game, "1; 1; 1") {
                Size = new Vector2(windowAdd.Size.X - windowLabelScale.Size.X, 24f),
                UpdateBounds = false,
                Position = new Vector2(windowLabelScale.Size.X, windowLabelScale.Position.Y)
            });

            windowAdd.Widgets.Add(new ButtonWidget(Game, "ADD", delegate() {
                string[] positionSplit = windowFieldPosition.Text.Split(new char[] {';'});
                string[] rotationSplit = windowFieldRotation.Text.Split(new char[] {';'});
                string[] centerSplit = windowFieldCenter.Text.Split(new char[] {';'});
                string[] scaleSplit = windowFieldScale.Text.Split(new char[] {';'});
                ArtObjectInstance ao = new ArtObjectInstance(windowFieldName.Text) {
                    Id = int.Parse(windowFieldId.Text),
                    Position = new Vector3(
                        float.Parse(positionSplit[0].Trim()),
                        float.Parse(positionSplit[1].Trim()),
                        float.Parse(positionSplit[2].Trim())
                    ),
                    Rotation = new Quaternion(
                        float.Parse(rotationSplit[0].Trim()),
                        float.Parse(rotationSplit[1].Trim()),
                        float.Parse(rotationSplit[2].Trim()),
                        float.Parse(rotationSplit[3].Trim())
                    ),
                    Scale = new Vector3(
                        float.Parse(scaleSplit[0].Trim()),
                        float.Parse(scaleSplit[1].Trim()),
                        float.Parse(scaleSplit[2].Trim())
                    )
                };
                ao.ActorSettings = new ArtObjectActorSettings() {
                    RotationCenter = new Vector3(
                        float.Parse(centerSplit[0].Trim()),
                        float.Parse(centerSplit[1].Trim()),
                        float.Parse(centerSplit[2].Trim())
                    )
                };
                ao.ArtObject = CMProvider.CurrentLevel.Load<ArtObject>("Art objects/"+ao.ArtObjectName);
                ao.Initialize();
                LevelManager.ArtObjects[ao.Id] = ao;
                LevelMaterializer.RegisterSatellites();

                windowAddHeader.CloseButtonWidget.Action();
            }) {
                Size = new Vector2(windowAdd.Size.X, 24f),
                UpdateBounds = false,
                LabelCentered = true,
                Position = new Vector2(0f, windowAdd.Size.Y - 24f)
            });

            windowAdd.Position.X = GraphicsDevice.Viewport.Width / 2 - (int) (windowAdd.Size.X / 2);
            windowAdd.Position.Y = GraphicsDevice.Viewport.Height / 2 - (int) (windowAdd.Size.Y / 2);
        }
        
    }
}