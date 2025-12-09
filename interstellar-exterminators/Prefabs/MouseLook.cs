using Godot;

public partial class MouseLook : Node
{
    /// <summary>
    /// The node we want to rotate left and right 
    /// </summary>
    [Export] 
    public Node3D YawTarget;

    /// <summary>
    /// Node to rotate vertically.
    /// </summary>
    [Export] 
    public Node3D PitchTarget;

    [Export] 
    public float MouseSensitivity = 0.0040f;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion)
            return;

        if (YawTarget != null)
            YawTarget.RotateY(-motion.Relative.X * MouseSensitivity);

        if (PitchTarget != null)
        {
            PitchTarget.RotateX(-motion.Relative.Y * MouseSensitivity);

            var rot = PitchTarget.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
            PitchTarget.Rotation = rot;
        }
    }
}
