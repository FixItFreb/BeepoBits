using Godot;
using Godot.Collections;
using uOSC;

public partial class VMCTracking : Node
{
    private static VMCTracking _instance;
    public static VMCTracking Instance { get { return _instance; } }

    [Export] private uOscServer oscServer;
    public uOscServer OSCServer { get { return oscServer; } }

    [Export] private VMCController vmcController;
    public VMCController VMCController { get { return vmcController; } }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        oscServer.Connect(uOscServer.SignalName.OnDataReceived, Callable.From((MessageObject message) =>
        {
            //GD.Print(message.data.values);
            vmcController.ReceiveOSCMessage(message);
        }));
    }
}