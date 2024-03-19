using Godot;
using System;

public partial class MicInputDetection : AudioStreamPlayer
{
    private static MicInputDetection _instance;
    public static MicInputDetection Instance { get { return _instance; } }

    [Export] private Timer micOffTimer;
    [Export] public bool captureInput = false;
    private AudioEffectCapture captureEffect;
    private int micBusIndex = -1;
    private bool micOpen = false;

    [Export] public float micThreshold = 0.5f;

    [Signal] public delegate void MicOpenEventHandler();
    [Signal] public delegate void MicCloseEventHandler();

    private void StopMic()
    {
        EmitSignal(SignalName.MicClose);
        micOpen = false;
    }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        micBusIndex = AudioServer.GetBusIndex("Mic");
        captureEffect = (AudioEffectCapture)AudioServer.GetBusEffect(micBusIndex, 0);

        string[] inputs = AudioServer.GetInputDeviceList();
        for (int i = 0; i < inputs.Length; i++)
        {
            GD.Print("Device " + i + " : " + inputs[i]);
        }
    }

    public override void _Process(double delta)
    {
        if (captureInput)
        {
            int sampleSize = captureEffect.GetFramesAvailable();
            Vector2[] values = captureEffect.GetBuffer(sampleSize);
            Vector2 sum = Vector2.Zero;
            if (values.Length > 0)
            {
                foreach (Vector2 v in values)
                {
                    sum += v.Abs();
                }
                sum /= sampleSize;
            }

            if (sum.Length() >= micThreshold)
            {
                micOffTimer.Start();
                if (!micOpen)
                {
                    micOpen = true;
                    EmitSignal(SignalName.MicOpen);
                }
            }
            //GD.Print("Mic Input: " + sum.ToString("0.000000"));
        }
    }
}
