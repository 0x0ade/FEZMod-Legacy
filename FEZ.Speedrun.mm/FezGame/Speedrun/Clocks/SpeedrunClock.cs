using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using FezGame.Speedrun.Clocks;
using FezGame.Services;
using FezEngine.Tools;


using FezEngine.Tools;
using FezEngine.Services;
using Common;
using FezEngine.Components.Scripting;

namespace FezGame.Speedrun.Clocks {
    public class SpeedrunClock : ISpeedrunClock {

        public bool InGame {
            get {
                return true;
            }
        }

        protected IGameStateManager gameState_;
        protected IGameStateManager GameState {
            get {
                if (gameState_ != null) {
                    return gameState_;
                }
                gameState_ = ServiceHelper.Get<IGameStateManager>();
                return gameState_;
            }
        }

        public TimeSpan Time { get; protected set; }
        public TimeSpan TimeLoading { get; protected set; }
        public List<Split> Splits { get; set; }
        protected DateTime timeNow = new DateTime(0);
        protected DateTime timePrev = new DateTime(0);
        protected readonly DateTime timeNull = new DateTime(0);

        public bool Strict { get; set; }

        protected bool running_ = false;

        public bool Running {
            get {
                return running_;
            }
            set {
                if (value && !running_) {
                    while (clockThread != null && Thread.CurrentThread != clockThread && clockThread.IsAlive) {
                        Thread.Sleep(0);
                    }
                    defaultSplitCases = FezSpeedrun.DefaultSplitCases;
                    Time = new TimeSpan();
                    clockThread = new Thread(ClockThreadLoop);
                    clockThread.IsBackground = true;
                    clockThread.Start();
                }

                running_ = value;
            }
        }

        public bool Paused { get; set; }

        protected List<SplitCase> defaultSplitCases = new List<SplitCase>();
        public ReadOnlyCollection<SplitCase> DefaultSplitCases {
            get {
                return defaultSplitCases.AsReadOnly();
            }
        }

        public List<SplitCase> SplitCases { get; set; }

        protected Thread clockThread;
        protected List<string> scheduledSplits = new List<string>();

        public SpeedrunClock() {
            Time = new TimeSpan();
            TimeLoading = new TimeSpan();
            Splits = new List<Split>();
            Strict = false;
            Running = false;
            SplitCases = new List<SplitCase>();
        }

        public void Split(string text) {
            scheduledSplits.Add(text);
        }

        protected void Split_(string text) {
            Splits.Add(new Split(text, new TimeSpan()));
        }

        public void Update() {
        }

        protected void ClockThreadLoop() {
            while (running_) {
                if (Paused || GameState == null) {
                    Thread.Sleep(0);
                    continue;
                }

                timeNow = DateTime.Now;

                if (GameState.Loading) {
                    if (timePrev == timeNull) {
                        timePrev = timeNow;
                    }
                    TimeLoading += timeNow - timePrev;
                    timePrev = timeNow;
                    continue;
                }

                while (scheduledSplits.Count > 0) {
                    Split_(scheduledSplits[0]);
                    scheduledSplits.RemoveAt(0);
                }

                if (GameState.TimePaused && !Strict) {
                    timePrev = timeNow;
                    continue;
                }
                if (timePrev == timeNull) {
                    timePrev = timeNow;
                }
                Time += timeNow - timePrev;
                if (Splits.Count > 0) {
                    Splits[Splits.Count-1].Time += timeNow - timePrev;
                }
                timePrev = timeNow;

                string split = null;
                foreach (SplitCase tosplit in defaultSplitCases) {
                    if (split != null) {
                        break;
                    }
                    split = tosplit(this);
                }
                foreach (SplitCase tosplit in SplitCases) {
                    if (split != null) {
                        break;
                    }
                    split = tosplit(this);
                }

                if (split != null) {
                    Split_(split);
                }

            }
        }

        public void Dispose() {
            Running = false;
        }

    }
}

