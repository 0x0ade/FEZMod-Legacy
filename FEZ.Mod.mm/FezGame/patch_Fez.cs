﻿using System;
using System.Reflection;
using FezGame.Mod;
using FezEngine.Mod;
using Microsoft.Xna.Framework;
using Common;
using MonoMod;

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
            return gameTime;
        }
        
        private GameTime setElapsed(GameTime gameTime, TimeSpan ts) {
            if (property_GameTime_ElapsedGameTime == null) {
                property_GameTime_ElapsedGameTime = gameTime.GetType().GetProperty("ElapsedGameTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (property_GameTime_TotalGameTime == null) {
                property_GameTime_TotalGameTime = gameTime.GetType().GetProperty("TotalGameTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            ReflectionHelper.SetValue(property_GameTime_TotalGameTime, gameTime, gameTime.TotalGameTime - gameTime.ElapsedGameTime + ts);
            ReflectionHelper.SetValue(property_GameTime_ElapsedGameTime, gameTime, ts);
            return gameTime;
        }
        
        public extern void orig_Update(GameTime gameTime);
        protected override void Update(GameTime gameTime) {
            gameTime = mul(gameTime, FEZMod.GameSpeed);
            
            FEZModEngine.UpdateGameTime = gameTime;
            
            orig_Update(gameTime);
        }
        
        public extern void orig_Draw(GameTime gameTime);
        protected override void Draw(GameTime gameTime) {
            gameTime = mul(gameTime, FEZMod.GameSpeed);
            
            FEZModEngine.DrawGameTime = gameTime;
            
            orig_Draw(gameTime);
        }
        
        public static extern void orig_LoadComponents(Fez game);
        public static void LoadComponents(Fez game) {
            orig_LoadComponents(game);
            FEZMod.LoadComponents(game);
        }

    }
}

