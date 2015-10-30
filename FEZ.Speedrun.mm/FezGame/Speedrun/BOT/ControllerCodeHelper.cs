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
            /*0 1*/.Add(CodeInputAll.RotateRight)
            /*0 2*/.AddFrame()
            /*0 1*/.AddFrame(CodeInputAll.RotateRight)
            /*1 2*/.AddFrame(CodeInputAll.RotateLeft)
            /*2 1*/.AddFrame(CodeInputAll.RotateRight)
            /*1 2*/.AddFrame(CodeInputAll.RotateLeft)
            /*2 0*/.AddFrame()
            /*1 0*/.AddFrame(CodeInputAll.RotateLeft)
            /*2 0*/.AddFrame()
            /*1 0*/.AddFrame(CodeInputAll.RotateLeft)
            /*2 1*/.AddFrame(CodeInputAll.RotateRight);
            /*0 2*/
            /*0 0*/

        public static KeySequence PipeRoom = new KeySequence()
            .Add(CodeInputAll.Up)
            .AddFrame(CodeInputAll.RotateRight)
            .AddFrame(CodeInputAll.Left)
            .AddFrame(CodeInputAll.Jump)
            .AddFrame(CodeInputAll.Right)
            .AddFrame(CodeInputAll.RotateLeft)
            .AddFrame(CodeInputAll.Down)
            .AddFrame(CodeInputAll.RotateRight);

        public static KeySequence WaterfallDoor = new KeySequence()
            .Add(CodeInputAll.Left)
            .AddFrame(CodeInputAll.RotateLeft)
            .AddFrame(CodeInputAll.Left)
            .AddFrame(CodeInputAll.Right)
            .AddFrame(CodeInputAll.RotateRight)
            .AddFrame(CodeInputAll.Down)
            .AddFrame(CodeInputAll.Up)
            .AddFrame(CodeInputAll.RotateLeft);

        public static KeySequence Waterfall = new KeySequence()
            .Add(CodeInputAll.Left)
            .AddFrame(CodeInputAll.RotateLeft)
            .AddFrame(CodeInputAll.Right)
            .AddFrame(CodeInputAll.RotateRight)
            .AddFrame(CodeInputAll.Up)
            .AddFrame(CodeInputAll.Jump)
            .AddFrame(CodeInputAll.Down)
            .AddFrame(CodeInputAll.RotateRight)
            .AddFrame()
            .AddFrame(CodeInputAll.RotateRight);

        public static KeySequence InfiniteFall = new KeySequence()
            .Add(CodeInputAll.Right)
            .AddFrame(CodeInputAll.RotateLeft)
            .AddFrame(CodeInputAll.Right)
            .AddFrame(CodeInputAll.Jump)
            .AddFrame(CodeInputAll.RotateRight)
            .AddFrame(CodeInputAll.Down)
            .AddFrame(CodeInputAll.Jump)
            .AddFrame(CodeInputAll.Up);

    }
}