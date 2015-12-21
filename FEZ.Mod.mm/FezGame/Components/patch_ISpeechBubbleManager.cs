using Common;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using MonoMod;
using FezEngine.Mod;
using FezGame.Mod;

namespace FezGame.Components {
	public interface patch_ISpeechBubbleManager : ISpeechBubbleManager {
        Color ColorFG { get; set; }
        Color ColorSecondaryFG { get; set; }
        Color ColorBG { get; set; }
        string Speaker { get; set; }
    }
}