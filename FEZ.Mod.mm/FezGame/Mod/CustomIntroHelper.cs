using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Globalization;
using System.Threading;
using MonoMod;
using FezGame.Components;
using FezEngine.Mod;
using System.Collections.Generic;

namespace FezGame.Mod {
    public static class CustomIntroHelper {
        
        public static List<CustomIntro> Intros = new List<CustomIntro>();
        
        public static int CurrentIndex;
        public static CustomIntro Current {
            get {
                return CurrentIndex < 0 || Intros.Count <= CurrentIndex ? null : Intros[CurrentIndex];
            }
        }
        
        public static void Create(Intro intro) {
            Intros = new List<CustomIntro>();
            
            for (int i = 0; i < FEZMod.Modules.Count; i++) {
                FezModule mod = FEZMod.Modules[i] as FezModule;
                if (mod == null) {
                    continue;
                }
                CustomIntro[] intros = mod.CreateIntros();
                if (intros == null) {
                    continue;
                }
                Intros.AddRange(intros);
            }
            
            if (Intros.Count == 0) {
                Intros.Add(new ADEIntro(intro));
            }
            
            Intros.Sort(delegate(CustomIntro a, CustomIntro b) {
                return a.Index - b.Index;
            });
            
            LoadContent();
            Reset();
        }
        
        public static void LoadContent() {
            for (int i = 0; i < Intros.Count; i++) {
                Intros[i].LoadContent();
            }
        }
        
        public static void ChangeIntro() {
            CurrentIndex++;
        }
        
        public static void Reset() {
            CurrentIndex = 0;
            for (int i = 0; i < Intros.Count; i++) {
                Intros[i].Reset();
            }
        }
        
    }
}