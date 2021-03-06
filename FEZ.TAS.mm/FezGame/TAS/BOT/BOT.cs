using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;
using FezGame.TAS.BOT.Levels;
using System.Collections.Generic;
using System.Reflection;

namespace FezGame.TAS.BOT {
    //Broken optimized TASer.
    public class BOT {
        
        private readonly static Type[] levelConstructorParamTypes = {typeof(BOT)};
        
        public TASComponent TAS;
        
        public List<BOT_LEVEL> Levels = new List<BOT_LEVEL>();
        public List<ConstructorInfo> LevelConstructors = new List<ConstructorInfo>();
        public BOT_LEVEL Level;
        
        public BOT(TASComponent tas) {
            TAS = tas;
        }
        
        public virtual void Initialize() {
            TAS.LevelManager.LevelChanged += LevelChanged;
            
            object[] levelConstructorParams = {this};
            if (LevelConstructors.Count == 0) {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < asms.Length; ai++) {
                    Assembly asm = asms[ai];
                    if (asm.GetName().Name.EndsWith(".mm")) {
                        continue;
                    }
                    Type[] types = asm.GetTypes();
                    for (int ti = 0; ti < types.Length; ti++) {
                        Type type = types[ti];
                        if (type.Namespace != "FezGame.Speedrun.BOT.Levels" || type.IsAbstract) {
                            continue;
                        }
                        //TODO maybe check more exactly whether to create an instance or not...
                        ConstructorInfo constructor = type.GetConstructor(levelConstructorParamTypes);
                        LevelConstructors.Add(constructor);
                        Levels.Add((BOT_LEVEL) constructor.Invoke(levelConstructorParams));
                    }
                }
            } else {
                for (int ci = 0; ci < LevelConstructors.Count; ci++) {
                    Levels.Add((BOT_LEVEL) LevelConstructors[ci].Invoke(levelConstructorParams));
                }
            }
        }
        
        public virtual void Dispose() {
            TAS.LevelManager.LevelChanged -= LevelChanged;
            
            Levels.Clear();
        }
        
        public virtual void LevelChanged() {
            for (int li = 0; li < Levels.Count; li++) {
                BOT_LEVEL level = Levels[li];
                for (int ni = 0; ni < level.Levels.Length; ni++) {
                    string name = level.Levels[ni];
                    if (name == TAS.LevelManager.Name) {
                        Level = level;
                        level.Time++;
                        return;
                    }
                }
            }
            Level = null;
        }
        
        public virtual void Update(GameTime gameTime) {
            if (TAS.GameState.Paused) {
                return;
            }
            if (Level != null) {
                if (TAS.PlayerManager.CanControl && TAS.PlayerManager.Action != ActionType.ExitDoor) {
                    Level.Time += gameTime.ElapsedGameTime.TotalSeconds;
                }
                
                Level.Update(gameTime);
            }
        }
        
        public static double Delta(double oldt, double newt) {
            double d = oldt - newt;
            return d == oldt ? 0d : Math.Max(0d, d);
        }
        
        //Logic helpers
        
        /*
        public bool CanClimb() {
            return TAS.PlayerManager.Action.IsOnLedge() ||
                TAS.PlayerManager.Action.IsClimbingLadder() ||
                TAS.PlayerManager.Action.IsClimbingVine() ||
                IsOnClimb();
        }
        
        private bool IsOnClimb() {
            //Decompiled code; hnnnnnnng
            Vector3 vector = TAS.CameraManager.Viewpoint.ForwardVector();
            Vector3 vector2 = TAS.CameraManager.Viewpoint.RightVector();
            float num = float.MaxValue; //3,402823E+38; //?
            bool flag = false;
            TrileInstance trileInstance = null;
            NearestTriles nearestTriles = TAS.LevelManager.NearestTrile(TAS.PlayerManager.Position - 0.002f * Vector3.UnitY);
            bool flag2 = (nearestTriles.Surface != null && nearestTriles.Surface.Trile.ActorSettings.Type == ActorType.Vine);
            PointCollision[] cornerCollision = TAS.PlayerManager.CornerCollision;
            for (int i = 0; i < cornerCollision.Length; i++) {
                PointCollision pointCollision = cornerCollision[i];
                if (pointCollision.Instances.Surface != null && TestClimbCollision(pointCollision.Instances.Surface, true)) {
                    TrileInstance surface = pointCollision.Instances.Surface;
                    float num2 = surface.Position.Dot(vector);
                    if (flag2 && num2 < num && TestClimbCollision(pointCollision.Instances.Surface, true)) {
                        num = num2;
                        trileInstance = surface;
                    }
                }
            }
            foreach (NearestTriles current in TAS.PlayerManager.AxisCollision.Values) {
                if (current.Surface != null && TestClimbCollision(current.Surface, false)) {
                    TrileInstance surface2 = current.Surface;
                    float num3 = surface2.Position.Dot(vector);
                    if (flag2 && num3 < num) {
                        flag = true;
                        num = num3;
                        trileInstance = surface2;
                    }
                }
            }
            
            return trileInstance != null;
        }
        
        private bool TestClimbCollision(TrileInstance instance, bool onAxis) {
            //Yet again decompiler code. Hnnnng
            TrileActorSettings actorSettings = instance.Trile.ActorSettings;
            float combinedPhi = FezMath.WrapAngle(actorSettings.Face.ToPhi() + instance.Phi);
            Axis axis = FezMath.AxisFromPhi(combinedPhi);
            return ((actorSettings.Type == ActorType.Vine || actorSettings.Type == ActorType.Ladder) && axis == TAS.CameraManager.Viewpoint.VisibleAxis() == onAxis) || instance.GetRotatedFace(TAS.CameraManager.VisibleOrientation) == CollisionType.TopOnly;
        }
        */
        
    }
}

