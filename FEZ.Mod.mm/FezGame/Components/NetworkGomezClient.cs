using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Net;
using FezGame.Structure;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using FezGame.Components;


using Common;
using FezGame.Mod;
using FezEngine.Components;

namespace FezGame.Components {
    public class NetworkGomezClient {

        public static BinaryFormatter Formatter = new BinaryFormatter();

        public static NetworkGomezClient Instance;

        public TcpClient ManagementClient;
        public NetworkStream ManagementStream;
        public UdpClient Client;

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

        protected Thread updateThread_;
        public Thread UpdateThread {
            get {
                return updateThread_;
            }
            set {
                if (Client != null) {
                    return;
                }
                updateThread_ = value;
            }
        }

        public NetworkClientAction Action;

        public NetworkGomezClient(string ip = "localhost", int port = 1337) {
            Ip = ip;
            Port = port;
        }

        public void Start() {
            IPEndPoint endpoint;
            if (Ip != null) {
                IPAddress[] addresses = Dns.GetHostAddresses(Ip);
                endpoint = new IPEndPoint(addresses.Length == 0 ? IPAddress.Parse(Ip) : addresses[0], Port);
            } else {
                endpoint = new IPEndPoint(IPAddress.Loopback, Port);
                ModLogger.Log("JAFM.Client", "Waiting for any incoming connection...");
            }

            if (Client == null) {
                Client = new UdpClient(Port);
            }

            if (UpdateThread == null) {
                updateThread_ = new Thread(delegate() {
                    while (Client != null) {
                        byte[] data = Client.Receive(ref endpoint);
                        if (NetworkGomezServer.Instance != null && NetworkGomezServer.Instance.ClientEndpoint == null) {
                            NetworkGomezServer.Instance.ClientEndpoint = new IPEndPoint(endpoint.Address, NetworkGomezServer.Instance.Port);
                        }
                        object obj;
                        using (MemoryStream ms = new MemoryStream(data)) {
                            obj = Formatter.Deserialize(ms);
                        }
                        if (Action != null) {
                            Action(obj);
                        }
                        Thread.Sleep(0);
                    }
                });
                updateThread_.IsBackground = true;
            }
            UpdateThread.Start();

            if (NetworkGomezServer.Instance != null) {
                return;
            }
            if (FEZMod.EnableMultiplayerLocalhost) {
                NetworkGomezServer.Instance = new NetworkGomezServer(Port+1);
            } else {
                NetworkGomezServer.Instance = new NetworkGomezServer(Port);
                NetworkGomezServer.Instance.Client = Client;
            }
            NetworkGomezServer.Instance.ClientEndpoint = new IPEndPoint(endpoint.Address, NetworkGomezServer.Instance.Port);
            if (ManagementClient == null) {
                ManagementClient = new TcpClient(Ip, Port);
                ManagementStream = ManagementClient.GetStream();
            }
            NetworkGomezServer.Instance.ManagementClient = ManagementClient;
            NetworkGomezServer.Instance.ManagementStream = ManagementStream;
            NetworkGomezServer.Instance.StartListening();
        }

        public void Stop() {
            if (ManagementStream != null) {
                ManagementStream.Close();
                ManagementStream = null;
            }
            if (ManagementClient != null) {
                ManagementClient.Close();
                ManagementClient = null;
            }
            if (Client != null) {
                Client.Close();
                Client = null;
            }
        }

    }
}

