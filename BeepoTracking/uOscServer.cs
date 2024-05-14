using Godot;
using System.Threading;
using System.Collections.Generic;

namespace uOSC
{
    public partial class uOscServer : Node
    {
        [Export] public int port = 3333;

        [Export] public bool autoStart = true;

#if NETFX_CORE
    Udp udp_ = new Uwp.Udp();
    Thread thread_ = new Uwp.Thread();
#else
        Udp udp_ = new DotNet.Udp();
        Thread thread_ = new DotNet.Thread();
#endif
        Parser parser_ = new Parser();

        [Signal] public delegate void OnDataReceivedEventHandler(MessageObject data);
        [Signal] public delegate void OnServerStartedEventHandler(int port);
        [Signal] public delegate void OnServerStoppedEventHandler(int port);

#if UNITY_EDITOR
    public DataReceiveEvent _onDataReceivedEditor = new DataReceiveEvent();
#endif

        int port_ = 0;
        bool isStarted_ = false;

        private uint boneTrackingAddress = "/VMC/Ext/Bone/Pos".Hash();
        private uint blendTrackingAddress = "/VMC/Ext/Blend/Val".Hash();
        private uint blendApplyAddress = "/VMC/Ext/Blend/Apply".Hash();

        private List<BlendShapeTrackingData> blendShapesToApply = new List<BlendShapeTrackingData>();

        public bool isRunning
        {
            get { return udp_.isRunning; }
        }

        public override void _EnterTree()
        {
            port_ = port;
        }

        public override void _Ready()
        {
            if (autoStart)
            {
                StartServer();
            }
        }

        public void StartServer()
        {
            if (isStarted_) return;

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            udp_.StartServer(port);
            thread_.Start(source, UpdateMessage);

            isStarted_ = true;

            EmitSignal(uOscServer.SignalName.OnServerStarted, port);
        }

        public void StopServer()
        {
            if (!isStarted_) return;

            thread_.Stop();
            udp_.Stop();

            isStarted_ = false;

            EmitSignal(uOscServer.SignalName.OnServerStopped, port);
        }

        public override void _Process(double delta)
        {
            UpdateReceive();
            UpdateChangePort();
        }

        void UpdateReceive()
        {
            while (parser_.messageCount > 0)
            {
                var message = parser_.Dequeue();

                uint addressHash = message.address.Hash();
                object[] values = message.values;

                if (addressHash == boneTrackingAddress)
                {
                    BoneTrackingEvent boneEvent = new BoneTrackingEvent();
                    boneEvent.name = (string)values[0];
                    boneEvent.rot = new Quaternion((float)values[4], -(float)values[5], -(float)values[6], (float)values[7]).Normalized();

                    BeepoCore.GetInstance().SendEvent(boneEvent, "TrackingEvents");
                }
                else if (addressHash == blendTrackingAddress)
                {
                    BlendShapeTrackingData blendShapeData = new BlendShapeTrackingData();
                    string animName = (string)values[0];
                    blendShapeData.value = (float)values[1];
                    blendShapeData.hash = animName.Hash();
                    blendShapesToApply.Add(blendShapeData);
                }
                else if (addressHash == blendApplyAddress)
                {
                    var blendShapeEvent = new BlendShapeTrackingEvent();
                    blendShapeEvent.data = blendShapesToApply;
                    BeepoCore.GetInstance().SendEvent(blendShapeEvent, "TrackingEvents");

                    // Clear all blend shape data ready for next update
                    blendShapesToApply = new List<BlendShapeTrackingData>();
                }
            }
        }

        void UpdateChangePort()
        {
            if (port_ == port) return;

            StopServer();
            StartServer();
            port_ = port;
        }

        void UpdateMessage()
        {
            while (udp_.messageCount > 0)
            {
                var buf = udp_.Receive();
                int pos = 0;
                parser_.Parse(buf, ref pos, buf.Length);
            }
        }
    }

}