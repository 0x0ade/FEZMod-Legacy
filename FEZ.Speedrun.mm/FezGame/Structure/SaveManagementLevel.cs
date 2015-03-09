using Common;
using System;
using FezGame.Components;
using FezGame.Services;
using FezGame.Tools;
using FezEngine.Components;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Structure {
    public class SaveManagementLevel : MenuLevel {

        [ServiceDependency]
        public MenuBase MenuBase { get; set; }

        public void orig_Initialize() {
        }

        public void Initialize() {
            orig_Initialize();
            AddItem("SaveTimesTitle", delegate() {
                ModLogger.Log("JAFM.Speedrun", "Menu not implemented!");
            });
        }

    }
}

