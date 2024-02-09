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

    public void EnableTracking()
    {
        if (!OSCServer.isRunning)
        {
            BeepoAvatar avatar0 = BeepoCore.currentAvatars[0];
            if (avatar0.AvatarType == BeepoAvatarType.AvatarVRM)
            {
                VMCTracking.Instance.VMCController.SetCurrentAvatar(avatar0.GetAvatarNode<BeepoAvatarVRM>());
                VMCTracking.Instance.OSCServer.Connect(uOSC.uOscServer.SignalName.OnServerStarted, Callable.From((int port) => { GD.Print("Server started on port " + port); }), (uint)ConnectFlags.OneShot);
                VMCTracking.Instance.OSCServer.StartServer();
            }
        }
    }

    public void DisableTracking()
    {
        if (VMCTracking.Instance.OSCServer.isRunning)
        {
            VMCTracking.Instance.OSCServer.StopServer();
        }
    }
}