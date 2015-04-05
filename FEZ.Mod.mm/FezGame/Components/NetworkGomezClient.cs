﻿using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace FezGame.Components {
    public class NetworkGomezClient {

        public static BinaryFormatter Formatter = new BinaryFormatter();

        public static NetworkGomezClient Instance;

        public TcpClient Client;
        public NetworkStream Stream;

        public string ip_;
        public string Ip {
            get {
                return ip_;
            }
            set {
                if (Client != null) {
                    return;
                }
                ip_ = value;
            }
        }

        protected int port_;
        public int Port {
            get {
                return port_;
            }
            set {
                if (Client != null) {
                    return;
                }
                port_ = value;
            }
        }

        public NetworkGomezClient(string ip = "localhost", int port = 1337) {
            Ip = ip;
            Port = port;
        }

        public void Start() {
            Client = new TcpClient(Ip, Port);
            Stream = Client.GetStream();
            if (NetworkGomezServer.Instance != null) {
                return;
            }
            NetworkGomezServer.Instance = new NetworkGomezServer(Port);
            NetworkGomezServer.Instance.Stream = Stream;
            Stream.Write(new byte[] { 0 }, 0, 1);
        }

        public void Stop() {
            if (Stream != null) {
                Stream.Close();
            }
            if (Client != null) {
                Client.Close();
            }
        }

    }
}
