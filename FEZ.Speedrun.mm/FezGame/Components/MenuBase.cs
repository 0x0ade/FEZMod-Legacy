using System;
using FezGame.Speedrun;
using System.Text;

namespace FezGame.Components {
    public class MenuBase {

        protected void orig_StartNewGame() {
        }

        protected void StartNewGame() {
            orig_StartNewGame();
            if (FezSpeedrun.LiveSplitClient != null) {
                byte[] msg = Encoding.ASCII.GetBytes("initgametime\r\n");
                FezSpeedrun.LiveSplitStream.Write(msg, 0, msg.Length);
                msg = Encoding.ASCII.GetBytes("starttimer\r\n");
                FezSpeedrun.LiveSplitStream.Write(msg, 0, msg.Length);
            }
        }

    }
}

