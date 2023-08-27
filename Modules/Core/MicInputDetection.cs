using Godot;
using System;

public partial class MicInputDetection : AudioStreamPlayer
{
    [Export] public bool captureInput = false;
    private AudioEffectCapture captureEffect;
    private int micBusIndex = -1;

    public override void _Ready()
    {
        micBusIndex = AudioServer.GetBusIndex("Mic");
        captureEffect = (AudioEffectCapture)AudioServer.GetBusEffect(micBusIndex, 0);

        string[] inputs = AudioServer.GetInputDeviceList();
        for (int i = 0; i < inputs.Length; i++)
        {
            GD.Print("Device " + i + " : " + inputs[i]);
        }
        //AudioServer.InputDevice = "Microphone (2- USB Audio Device)";
    }

    public override void _Process(double delta)
    {
        if(captureInput)
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
            //GD.Print("Mic Input: " + sum.ToString("0.000000"));
        }
    }
}
