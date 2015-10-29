using System;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using FezEngine;
using FezGame.Components;
using FezEngine.Structure.Input;
using FezGame.Mod;
using FezEngine.Structure;
using FezEngine.Services;
using FezEngine.Tools;
using System.Collections.Generic;

namespace FezGame.Speedrun.BOT {
    public static class ControllerCodeHelper {
        
        public static KeySequence MonoclePainting = new KeySequence()
            .Add(CodeInput.SpinRight)
            .AddFrame()
            .AddFrame(CodeInput.SpinRight)
            .AddFrame(CodeInput.SpinLeft)
            .AddFrame(CodeInput.SpinRight)
            .AddFrame(CodeInput.SpinLeft)
            .AddFrame()
            .AddFrame(CodeInput.SpinLeft)
            .AddFrame()
            .AddFrame(CodeInput.SpinLeft)
            .AddFrame(CodeInput.SpinRight);
        
    }
}
