﻿using Godot;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace uOSC
{

    public partial class uOscClient : Node
    {
        [Export] public string address = "127.0.0.1";

        [Export] public int port = 3333;

        [Export] public int maxQueueSize = 100;

        [Export] public float dataTransimissionInterval = 0f;

#if NETFX_CORE
    Udp udp_ = new Uwp.Udp();
    Thread thread_ = new Uwp.Thread();
#else
        Udp udp_ = new DotNet.Udp();
        Thread thread_ = new DotNet.Thread();
#endif
        Queue<object> messages_ = new Queue<object>();
        object lockObject_ = new object();

        [Signal] public delegate void OnClientStartedEventHandler(string address, int port);
        [Signal] public delegate void OnClientStoppedEventHandler(string address, int port);

        string address_ = "";
        int port_ = 0;

        public bool isRunning
        {
            get { return udp_.isRunning; }
        }

        public override void _Ready()
        {
            StartClient();
        }

        public void StartClient()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            udp_.StartClient(address, port);
            thread_.Start(source, UpdateSend);
            address_ = address;
            port_ = port;
            EmitSignal(uOscClient.SignalName.OnClientStarted, address, port);
        }

        public void StopClient()
        {
            thread_.Stop();
            udp_.Stop();
            EmitSignal(uOscClient.SignalName.OnClientStopped, address, port);
        }

        public override void _Process(double delta)
        {
            UpdateChangePortAndAddress();
        }

        void UpdateChangePortAndAddress()
        {
            if (port_ == port && address_ == address) return;

            StopClient();
            StartClient();
        }

        void UpdateSend()
        {
            while (messages_.Count > 0)
            {
                var sw = Stopwatch.StartNew();

                object message;
                lock (lockObject_)
                {
                    message = messages_.Dequeue();
                }

                using (var stream = new MemoryStream())
                {
                    if (message is Message)
                    {
                        ((Message)message).Write(stream);
                    }
                    else if (message is Bundle)
                    {
                        ((Bundle)message).Write(stream);
                    }
                    else
                    {
                        continue;
                    }
                    udp_.Send(Util.GetBuffer(stream), (int)stream.Position);
                }

                if (dataTransimissionInterval > 0f)
                {
                    var ticks = (long)Mathf.Round(dataTransimissionInterval / 1000f * Stopwatch.Frequency);
                    while (sw.ElapsedTicks < ticks) ;
                }
            }
        }

        void Add(object data)
        {
            lock (lockObject_)
            {
                messages_.Enqueue(data);

                while (messages_.Count > maxQueueSize)
                {
                    messages_.Dequeue();
                }
            }
        }

        public void Send(string address, params object[] values)
        {
            Send(new Message()
            {
                address = address,
                values = values
            });
        }

        public void Send(Message message)
        {
            Add(message);
        }

        public void Send(Bundle bundle)
        {
            Add(bundle);
        }
    }

}