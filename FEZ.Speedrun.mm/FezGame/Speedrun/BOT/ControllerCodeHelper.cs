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

		public static KeySequence MonoclePainting = new KeySequence ()
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

		public static KeySequence PipeRoom = new KeySequence ()
			.Add (CodeInput.Up)
			.AddFrame (CodeInput.SpinRight)
			.AddFrame (CodeInput.Left)
			.AddFrame (CodeInput.Jump)
			.AddFrame (CodeInput.Right)
			.AddFrame (CodeInput.SpinLeft)
			.AddFrame (CodeInput.Down)
			.AddFrame (CodeInput.SpinRight);

		public static KeySequence WaterfallDoor = new KeySequence ()
			.Add (CodeInput.Left)
			.AddFrame (CodeInput.SpinLeft)
			.AddFrame (CodeInput.Left)
			.AddFrame (CodeInput.Right)
			.AddFrame (CodeInput.SpinRight)
			.AddFrame (CodeInput.Down)
			.AddFrame (CodeInput.Up)
			.AddFrame (CodeInput.SpinLeft);

		public static KeySequence Waterfall = new KeySequence ()
			.Add (CodeInput.Left)
			.AddFrame (CodeInput.SpinLeft)
			.AddFrame (CodeInput.Right)
			.AddFrame (CodeInput.SpinRight)
			.AddFrame (CodeInput.Up)
			.AddFrame (CodeInput.Jump)
			.AddFrame (CodeInput.Down)
			.AddFrame (CodeInput.SpinRight)
			.AddFrame ()
			.AddFrame (CodeInput.SpinRight);

		public static KeySequence InfiniteFall = new KeySequence ()
			.Add (CodeInput.Right)
			.AddFrame (CodeInput.SpinLeft)
			.AddFrame (CodeInput.Right)
			.AddFrame (CodeInput.Jump)
			.AddFrame (CodeInput.SpinRight)
			.AddFrame (CodeInput.Down)
			.AddFrame (CodeInput.Jump)
			.AddFrame (CodeInput.Up);

	}
}