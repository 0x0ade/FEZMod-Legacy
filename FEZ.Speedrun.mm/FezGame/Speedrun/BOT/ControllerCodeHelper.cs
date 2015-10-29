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
            /*l r*///l: left; r: right; 0: not pressed; 1: pressed; 2: released;
            /*0 1*/.Add(CodeInput.SpinRight)
            /*0 2*/.AddFrame()
            /*0 1*/.AddFrame(CodeInput.SpinRight)
            /*1 2*/.AddFrame(CodeInput.SpinLeft)
            /*2 1*/.AddFrame(CodeInput.SpinRight)
            /*1 2*/.AddFrame(CodeInput.SpinLeft)
            /*2 0*/.AddFrame()
            /*1 0*/.AddFrame(CodeInput.SpinLeft)
            /*2 0*/.AddFrame()
            /*1 0*/.AddFrame(CodeInput.SpinLeft)
            /*2 1*/.AddFrame(CodeInput.SpinRight);
            /*0 2*/
            /*0 0*/
        
    }
}
