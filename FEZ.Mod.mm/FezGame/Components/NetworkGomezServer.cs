using System;
using Common;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Net;

namespace FezGame.Components {
    public class NetworkGomezServer {

        public static BinaryFormatter Formatter = new BinaryFormatter();

        public static NetworkGomezServer Instance;

        public TcpListener Listener;
        public TcpClient Client;
        public NetworkStream Stream;

        protected int port_;
        public int Port {
            get {
                return port_;
            }
            set {
                if (Listener != null) {
                    return;
                }
                port_ = value;
            }
        }

        public NetworkGomezServer(int port = 1337) {
            Port = port;
        }

        public void StartListening() {
            ModLogger.Log("JAFM", "Hosting on port "+Port);
            Listener = new TcpListener(Port);
            Listener.Start();
            Client = Listener.AcceptTcpClient();
            Stream = Client.GetStream();
            if (NetworkGomezClient.Instance != null) {
                return;
            }
            Stream.Read(new byte[1], 0, 1);//It automatically waits
            NetworkGomezClient.Instance = new NetworkGomezClient(null, Port);
            NetworkGomezClient.Instance.Stream = Stream;
        }

        public void StopListening() {
            if (Stream != null) {
                Stream.Close();
            }
            if (Client != null) {
                Client.Close();
            }
            if (Listener != null) {
                Listener.Stop();
            }
        }

    }
}

