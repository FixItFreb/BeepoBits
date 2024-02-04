using Godot;
using System.Threading;

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
            if(autoStart)
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
                EmitSignal(uOscServer.SignalName.OnDataReceived, new MessageObject(message));
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