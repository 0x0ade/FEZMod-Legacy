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
	/*
	 * Sequences of inputs made to ease life when we want to use corner jumps, corner kicks, TP jumps and stuff like that
	 */
	public static class TricksHelper
	{
		// Gomez is on the right side of the ledge
		public static KeySequence CornerKick_Right = new KeySequence ()
			.Add (CodeInputAll.Right)
			.AddFrame ()
			.AddFrame (CodeInputAll.Jump);
	}
}

