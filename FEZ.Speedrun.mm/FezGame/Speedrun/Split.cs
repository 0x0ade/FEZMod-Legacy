using System;

namespace FezGame.Speedrun {
    public class Split {

        public string Text;
        public TimeSpan Time;

        public Split(string text = "", TimeSpan time = new TimeSpan()) {
            Text = text;
            Time = time;
        }

    }
}

