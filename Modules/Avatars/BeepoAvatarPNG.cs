using Godot;
using System;
using System.IO.Pipes;

public partial class BeepoAvatarPNG : RigidBody3D
{
    [Export] private BeepoAvatar avatar;
    public BeepoAvatar Avatar { get { return avatar; } }

    [Export] private Sprite3D avatarSprite;
    public Sprite3D AvatarSprite { get { return avatarSprite; } }

    [Export] private CollisionShape3D collider;

    // Movement configuration
    [Export] private double frequencyX = 0;
    [Export] private double magnitudeX = 1;
    [Export] private double frequencyY = 0;
    [Export] private double magnitudeY = 1;
    [Export] private double jumpMagnitude = 2.5;

    // Movement state
    private Vector3 originalPosition = Vector3.Zero;
    private RigidBody3D node;
    private double hoveringProgress = 0;
    private double jumpProgress = 4; // set to 4 so we're not jumping by default

    // PID stuff
    private float proportionalGain = 10.0f;
    private float integralGain = 0.01f;
    private float derivativeGain = 5.0f;
    private Vector3 lastError = Vector3.Zero;
    private Vector3 integrationStored = Vector3.Zero;
    private float integrationClamp = 10.0f;


    public void StartSpeaking()
    {
        if (jumpProgress >= Math.PI) jumpProgress = 0; // Only start another jump if the previous one already finished
    }

    public void StopSpeaking()
    {
    }

    public override void _Ready()
    {
        node = (RigidBody3D)GetNode(".");
        originalPosition = node.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        hoveringProgress += delta;
        var offsetY = Math.Sin(hoveringProgress * frequencyY) * magnitudeY;
        var offsetX = Math.Sin(hoveringProgress * frequencyX) * magnitudeX;

        var desiredPosition = originalPosition;
        desiredPosition.X += (float)offsetX;
        desiredPosition.Y += (float)offsetY;

        if (jumpProgress < Math.PI) // Using a sine function for the jump which returns to 0 at x=PI
        {
            jumpProgress += delta * 3;
            var offsetJump = Math.Sin(jumpProgress) * jumpMagnitude;
            desiredPosition.Y += (float)offsetJump;
        }

        node.ApplyForce(calculatePIDForce(delta, node.GlobalPosition, desiredPosition));
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
