using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FezGame.Services;
using FezEngine.Tools;

namespace FezGame.Speedrun.Clocks {
    public class LiveSplitClock : ISpeedrunClock {

        public bool InGame {
            get {
                return false;
            }
            set {
                //LiveSplit is not in-game.
            }
        }

        public ISpeedrunClock Clock;

        public TcpClient Client;
        public NetworkStream Stream;
        public bool Sync = false;

        protected bool WasLoading;

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

        public TimeSpan Time {
            get {
                return Clock.Time;
            }
        }

        public TimeSpan TimeLoading {
            get {
                return Clock.TimeLoading;
            }
        }

        public List<Split> Splits {
            get {
                return Clock.Splits;
            }
            set {
                Clock.Splits = value;
            }
        }

        public double Direction {
            get {
                return 1D;
            }
            set {
                //LiveSplit can only run in one direction.
            }
        }

        public bool Strict {
            get {
                return Clock.Strict;
            }
            set {
                Clock.Strict = value;
            }
        }

        public bool Running {
            get {
                return Clock.Running;
            }
            set {
                if (Clock.Running != value && Client != null) {
                    if (value) {
                        byte[] msg = Encoding.ASCII.GetBytes("initgametime\r\n");
                        Stream.Write(msg, 0, msg.Length);
                        msg = Encoding.ASCII.GetBytes("starttimer\r\n");
                        Stream.Write(msg, 0, msg.Length);
                        if (Sync) {
                            msg = Encoding.ASCII.GetBytes("pausegametime\r\n");
                            Stream.Write(msg, 0, msg.Length);
                        }
                    } else {
                        byte[] msg = Encoding.ASCII.GetBytes("split\r\n");
                        Stream.Write(msg, 0, msg.Length);
                    }
                }
                Clock.Running = value;
            }
        }

        public bool Paused {
            get {
                return Clock.Paused;
            }
            set {
                Clock.Paused = value;
            }
        }

        public ReadOnlyCollection<SplitCase> DefaultSplitCases {
            get {
                return Clock.DefaultSplitCases;
            }
        }

        public List<SplitCase> SplitCases {
            get {
                return Clock.SplitCases;
            }
            set {
                Clock.SplitCases = value;
            }
        }

        public LiveSplitClock(string ip, int port) {
            Client = new TcpClient("localhost", port);
            Stream = Client.GetStream();
        }

        public void Split(string text) {
            Clock.Split(text);
        }

        public void Update() {
            Clock.Update();

            if (GameState == null) {
                return;
            }

            if (GameState.Loading) {
                if (Client != null && !WasLoading) {
                    byte[] msg = Encoding.ASCII.GetBytes("pausegametime\r\n");
                    Stream.Write(msg, 0, msg.Length);
                }
                WasLoading = true;
                return;
            }

            if (WasLoading) {
                WasLoading = false;
                if (Client != null) {
                    byte[] msg = Encoding.ASCII.GetBytes("unpausegametime\r\n");
                    Stream.Write(msg, 0, msg.Length);
                    msg = Encoding.ASCII.GetBytes("setloadingtimes " + Clock.TimeLoading + "\r\n");
                    Stream.Write(msg, 0, msg.Length);
                }
            }

            if (Sync) {
                byte[] msg = Encoding.ASCII.GetBytes("setgametime " + Clock.Time + "\r\n");
                Stream.Write(msg, 0, msg.Length);
            }

        }

        public void Dispose() {
            Running = false;
            if (Client != null) {
                Stream.Close();
                Client.Close();
                Client = null;
            }
        }

    }
}

