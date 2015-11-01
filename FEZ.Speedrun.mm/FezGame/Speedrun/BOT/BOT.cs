using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;
using FezGame.Speedrun.BOT.Levels;

namespace FezGame.Speedrun.BOT {
    //Broken optimized TASer.
    public class BOT {
        
        public TASComponent TAS;
        
        public BOT(TASComponent tas) {
            TAS = tas;
        }
        
        public void Update(GameTime gameTime) {
            //From my experience with Java, "switch"es eat up performance. Let's just hope C# / .NET is better.
            //TODO maybe un-static these? Or use reflection?
            switch (TAS.LevelManager.Name) {
                case "GOMEZ_HOUSE_2D":
                case "GOMEZ_HOUSE":
                case "GOMEZ_HOUSE_END_32":
                case "GOMEZ_HOUSE_END_64":
                    BOT_GOMEZ_HOUSE.Update(this, gameTime);
                    break;
                case "VILLAGEVILLE_2D":
                    BOT_VILLAGEVILLE_2D.Update(this, gameTime);
                    break;
                case "PARLOR":
                    BOT_PARLOR.Update(this, gameTime);
                    break;
                default:
                    break;
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

