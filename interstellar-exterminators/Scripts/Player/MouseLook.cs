using Godot;

public partial class MouseLook : Node
{
    /// <summary>
    /// The node we want to rotate left and right 
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public Node3D YawTarget;

    /// <summary>
    /// Node to rotate vertically.
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public Node3D PitchTarget;

    [Export]
    [ExportGroup("Mouse Look Control")]
    public float MouseSensitivity = 0.0040f;

    /// <summary>
    /// The object that holds network ownership over this script.
    /// </summary>
    [Export]
    [ExportGroup("Networking")]
    public Node NetworkIdentity;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (NetworkIdentity == null || !NetworkIdentity.IsMultiplayerAuthority())
            return;

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
