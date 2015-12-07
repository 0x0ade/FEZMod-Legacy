﻿using System;
using FezGame.Mod;
using FezEngine.Mod;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Net;
using System.IO;
using FezEngine.Components;

namespace FezGame.Components {
    public class NetworkGomezServer {

        public static BinaryFormatter Formatter = new BinaryFormatter();

        public static NetworkGomezServer Instance;

        public static int SendModulo = 4;

        public TcpListener ManagementListener;
        public TcpClient ManagementClient;
        public NetworkStream ManagementStream;
        public UdpClient Client;

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

        public NetworkServerAction Action;

        public IPEndPoint ClientEndpoint;

        public string ServerName;
        public string ServerInfo;
        public string ServerInfoFile = "serverinfo.txt";

        public NetworkGomezServer(int port = 1337) {
            Port = port;
        }

        public void StartListening() {
            if (ManagementListener == null) {
                ManagementListener = new TcpListener(Port); //FIXME find out which address to use for the non-obsolete constructor
                ManagementListener.Start();
            }

            if (Client == null) {
                ModLogger.Log("JAFM", "Hosting on port " + Port);
                Client = new UdpClient(Port);
            }

            if (UpdateThread == null) {
                updateThread_ = new Thread(delegate() {
                    while (Client != null) {
                        if (Action != null && ClientEndpoint != null) {
                            object obj = Action();
                            if (obj == null) {
                                Thread.Sleep(0);
                                continue;
                            }
                            byte[] data;
                            using (MemoryStream ms = new MemoryStream()) {
                                Formatter.Serialize(ms, obj);
                                data = ms.ToArray();
                            }
                            Client.Send(data, data.Length, ClientEndpoint);
                        }
                        Thread.Sleep(0);
                    }
                });
                updateThread_.IsBackground = true;
            }
            UpdateThread.Start();

            if (NetworkGomezClient.Instance != null) {
                return;
            }
            Waiters.Wait(ManagementListener.Pending, delegate() {
                ManagementClient = ManagementListener.AcceptTcpClient();
                ManagementStream = ManagementClient.GetStream();
                if (FEZMod.EnableMultiplayerLocalhost) {
                    NetworkGomezClient.Instance = new NetworkGomezClient(null, Port + 1);
                } else {
                    NetworkGomezClient.Instance = new NetworkGomezClient(null, Port);
                    NetworkGomezClient.Instance.Client = Client;
                }
                NetworkGomezClient.Instance.ManagementClient = ManagementClient;
                NetworkGomezClient.Instance.ManagementStream = ManagementStream;
                NetworkGomezClient.Instance.Start();
            });
        }

        public void StopListening() {
            if (ManagementStream != null) {
                ManagementStream.Close();
                ManagementStream = null;
            }
            if (ManagementClient != null) {
                ManagementClient.Close();
                ManagementClient = null;
            }
            if (ManagementListener != null) {
                ManagementListener.Stop();
                ManagementListener = null;
            }
            if (Client != null) {
                Client.Close();
                Client = null;
            }
        }

        public void Broadcast(bool once = false) {
            if (FEZMod.EnableMultiplayerLocalhost) {
                return;
            }
            if (ServerName == null && ServerInfo == null && File.Exists(ServerInfoFile)) {
                using (FileStream fs = File.Open(ServerInfoFile, FileMode.Open)) {
                    using (StreamReader reader = new StreamReader(fs)) {
                        ServerName = reader.ReadLine().Trim();
                        ServerInfo = reader.ReadToEnd().Trim();
                    }
                }
            }
            if (ServerName != null || ServerInfo != null) {
                string name = Uri.EscapeDataString(ServerName ?? "");
                string info = Uri.EscapeDataString(ServerInfo ?? "");
                ModLogger.Log("JAFM.Server", info);
                using (WebClient client = new WebClient()) {
                    string ip = client.DownloadString("http://fezmod.tk/servers/broadcast.php?name="+name+"&info="+info);
                    if (!once) {
                        Waiters.Wait(15, () => Broadcast());
                    }
                }
            }
        }

    }
}

