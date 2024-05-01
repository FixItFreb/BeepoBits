using Godot;
using System;

public partial class SimpleInput : Node
{
    [Export] public Camera3D controlledCamera;

    [Export] public float moveSpeed = 2.0f;
    [Export] public float mouseInputScale = 0.001f;

    private StringName CamForwardAction = "cam_forward";
    private StringName CamBackwardAction = "cam_backward";
    private StringName CamLeftAction = "cam_left";
    private StringName CamRightAction = "cam_right";
    private StringName CamUpAction = "cam_up";
    private StringName CamDownAction = "cam_down";
    private StringName CamMouseLookAction = "cam_mouselook";
    private StringName CamResetAction = "cam_reset";
    private StringName AvatarMoveModifierAction = "avatar_move_mod";
    private StringName ShiftAction = "shift";
    private StringName ControlAction = "control";

    private Vector2 mouseRelative = Vector2.Zero;

    private Vector2 maxCamX = new Vector2(Mathf.DegToRad(-85.0f), Mathf.DegToRad(85.0f));
    private bool shiftHeld = false;
    private bool controlHeld = false;

    public override void _Ready()
    {
        if (controlledCamera == null)
        {
            // controlledCamera = BeepoCore.CurrentCamera;
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        // if (inputEvent.TryCast(out InputEventMouseMotion motion))
        // {
        //     mouseRelative = motion.Relative;
        //     if (Input.IsActionPressed(CamMouseLookAction))
        //     {
        //         if (shiftHeld)
        //         {
        //             BeepoCore.AvatarAnchor.RotateY(mouseRelative.X * mouseInputScale);
        //         }
        //         else if (controlHeld)
        //         {
        //             BeepoCore.AvatarAnchor.RotateX(mouseRelative.Y * mouseInputScale);
        //         }
        //         else
        //         {
        //             controlledCamera.RotateY(-mouseRelative.X * mouseInputScale);
        //             controlledCamera.RotateX(-mouseRelative.Y * mouseInputScale);
        //             controlledCamera.Rotation = controlledCamera.Rotation.WithX(Mathf.Clamp(controlledCamera.Rotation.X, maxCamX.X, maxCamX.Y)).WithZ(0);
        //         }
        //     }
        //     else if (Input.IsActionPressed(AvatarMoveModifierAction))
        //     {
        //         BeepoCore.AvatarAnchor.Translate(new Vector3(mouseRelative.X * mouseInputScale, -mouseRelative.Y * mouseInputScale, 0));
        //     }
        // }

        if (inputEvent.IsActionPressed(CamResetAction))
        {
            // if(shiftHeld)
            // {
            //     ToolboxTransformPresets.Instance.ResetAvatar();
            // }
            // else
            // {
            //     ToolboxTransformPresets.Instance.ResetCam();
            // }
            return;
        }

        if (inputEvent.IsActionPressed(ShiftAction))
        {
            shiftHeld = true;
        }
        else if (inputEvent.IsActionReleased(ShiftAction))
        {
            shiftHeld = false;
        }

        if (inputEvent.IsActionPressed(ControlAction))
        {
            controlHeld = true;
        }
        else if (inputEvent.IsActionReleased(ControlAction))
        {
            controlHeld = false;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed(CamForwardAction))
        {
            controlledCamera.GlobalPosition -= controlledCamera.Basis.Z * moveSpeed * (float)delta;
        }
        else if (Input.IsActionPressed(CamBackwardAction))
        {
            controlledCamera.GlobalPosition += controlledCamera.Basis.Z * moveSpeed * (float)delta;
        }

        if (Input.IsActionPressed(CamLeftAction))
        {
            controlledCamera.GlobalPosition -= controlledCamera.Basis.X * moveSpeed * (float)delta;
        }
        else if (Input.IsActionPressed(CamRightAction))
        {
            controlledCamera.GlobalPosition += controlledCamera.Basis.X * moveSpeed * (float)delta;
        }

        if (Input.IsActionPressed(CamUpAction))
        {
            controlledCamera.GlobalPosition += controlledCamera.Basis.Y * moveSpeed * (float)delta;
        }
        else if (Input.IsActionPressed(CamDownAction))
        {
            controlledCamera.GlobalPosition -= controlledCamera.Basis.Y * moveSpeed * (float)delta;
        }
    }
}
