using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class BeepoAvatarPNG : RigidBody3D
{
    private Array<PNGBone> bones;
    private SpeechState currentSpeech = SpeechState.NotSpeaking;

    // Movement configuration
    [Export] private bool enableHover = false;
    [Export] private bool enableJump = false;
    [Export] private double frequencyX = 0;
    [Export] private double magnitudeX = 1;
    [Export] private double frequencyY = 0;
    [Export] private double magnitudeY = 1;
    [Export] private double jumpMagnitude = 1.5;

    // Movement state
    private Vector3 originalPosition = Vector3.Zero;
    private double hoveringProgress = 0;
    private double jumpProgress = 4; // set to 4 so we're not jumping by default

    // PID stuff
    private float proportionalGain = 20.0f;
    private float integralGain = 0.01f;
    private float derivativeGain = 10.0f;
    private Vector3 lastError = Vector3.Zero;
    private Vector3 integrationStored = Vector3.Zero;
    private float integrationClamp = 10.0f;


    public void StartSpeaking()
    {
        if (enableJump && jumpProgress >= Math.PI) jumpProgress = 0; // Only start another jump if the previous one already finished
        currentSpeech = SpeechState.Speaking;
        UpdateSprites();
    }

    public void StopSpeaking()
    {
        currentSpeech = SpeechState.NotSpeaking;
        UpdateSprites();
    }

    public void UpdateSprites()
    {
        foreach (PNGBone bones in bones)
        {
            bones.UpdateSpeech(currentSpeech);
        }
    }

    public override void _Ready()
    {
        originalPosition = GlobalPosition;

        // find all avatar bones
        Array<Node> children = GetChildren();
        IEnumerable<Node> foundBones = children.Where(child => child is PNGBone);
        bones = new Array<PNGBone>(foundBones.Cast<PNGBone>());
        UpdateSprites();
    }

    public override void _Process(double delta)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        var desiredPosition = originalPosition;

        if (enableHover)
        {
            hoveringProgress += delta;
            var offsetY = Math.Sin(hoveringProgress * frequencyY) * magnitudeY;
            var offsetX = Math.Sin(hoveringProgress * frequencyX) * magnitudeX;

            desiredPosition.X += (float)offsetX;
            desiredPosition.Y += (float)offsetY;
        }

        if (enableJump && jumpProgress < Math.PI) // Using a sine function for the jump which returns to 0 at x=PI
        {
            jumpProgress += delta * 3;
            var offsetJump = Math.Sin(jumpProgress) * jumpMagnitude;
            desiredPosition.Y += (float)offsetJump;
        }

        ApplyForce(calculatePIDForce(delta, GlobalPosition, desiredPosition));
    }

    private Vector3 calculatePIDForce(double delta, Vector3 currentPosition, Vector3 desiredPosition)
    {
        // P term
        Vector3 error = desiredPosition - currentPosition;
        Vector3 p = error * proportionalGain;

        // I term
        integrationStored += error / (float)delta;
        Vector3 clampMin = new(-integrationClamp, -integrationClamp, -integrationClamp);
        Vector3 clampMax = new(integrationClamp, integrationClamp, integrationClamp);
        integrationStored = integrationStored.Clamp(clampMin, clampMax);

        Vector3 i = integrationStored * integralGain;

        // D term
        Vector3 valueRateOfChange = (error - lastError) / (float)delta;
        lastError = error;

        Vector3 d = valueRateOfChange * derivativeGain;

        return p + i + d;
    }
}
