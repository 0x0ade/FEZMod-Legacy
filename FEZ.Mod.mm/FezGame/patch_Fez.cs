using System;
using System.Collections.Generic;
using System.Reflection;
using FezGame.Mod;
using Microsoft.Xna.Framework;
using Common;

namespace FezGame {
    public class patch_Fez : Fez {
        
        private PropertyInfo property_GameTime_ElapsedGameTime;
        private PropertyInfo property_GameTime_TotalGameTime;
        
        private GameTime mul(GameTime gameTime, double d) {
            if (d == 1d) {
                return gameTime;
            }
            if (d <= 0.25d) {
                d = 0.25d;
            }
            
            if (property_GameTime_ElapsedGameTime == null) {
                property_GameTime_ElapsedGameTime = gameTime.GetType().GetProperty("ElapsedGameTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (property_GameTime_TotalGameTime == null) {
                property_GameTime_TotalGameTime = gameTime.GetType().GetProperty("TotalGameTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            TimeSpan egt = gameTime.ElapsedGameTime;
            TimeSpan tgt = gameTime.TotalGameTime;
            tgt -= egt;
            egt = TimeSpan.FromTicks((long) (egt.Ticks * d));
            tgt += egt;
            ReflectionHelper.SetValue(property_GameTime_ElapsedGameTime, gameTime, egt);
            ReflectionHelper.SetValue(property_GameTime_TotalGameTime, gameTime, tgt);
            //property_GameTime_ElapsedGameTime.SetValue(gameTime, egt, null);
            //property_GameTime_TotalGameTime.SetValue(gameTime, tgt, null);
            return gameTime;
        }
        
        public void orig_Update(GameTime gameTime) {
        }
        
        public void Update(GameTime gameTime) {
            orig_Update(mul(gameTime, FEZMod.GameSpeed));
        }
        
        public void orig_Draw(GameTime gameTime) {
        }
        
        public void Draw(GameTime gameTime) {
            orig_Draw(mul(gameTime, FEZMod.GameSpeed));
        }
        
        public static void orig_LoadComponents(Fez game) {
        }

        public static void LoadComponents(Fez game) {
            orig_LoadComponents(game);
            FEZMod.LoadComponents(game);
        }

    }
}

