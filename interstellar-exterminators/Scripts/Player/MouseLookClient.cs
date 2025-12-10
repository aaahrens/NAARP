using Godot;

/// <summary>
/// Handles local mouse input, rotates camera instantly, and sends yaw input to the server.
/// </summary>
public partial class MouseLookClient : Node
{
    /// <summary>
    /// The node we want to rotate left and right (local yaw).
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public Node3D YawTarget;

    /// <summary>
    /// Node to rotate vertically (local pitch).
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public Node3D PitchTarget;

    /// <summary>
    /// Mouse sensitivity for look.
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public float MouseSensitivity = 0.0040f;

    /// <summary>
    /// Reference to the server-side look simulation node.
    /// </summary>
    private MouseLookServer server;

    public override void _Ready()
    {
        server = GetTree().GetFirstNodeInGroup("MouseLookServer") as MouseLookServer;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority())
            return;

        if (@event is not InputEventMouseMotion motion)
            return;

        // Local, instant camera rotation
        if (YawTarget != null)
            YawTarget.RotateY(-motion.Relative.X * MouseSensitivity);

        if (PitchTarget != null)
        {
            PitchTarget.RotateX(-motion.Relative.Y * MouseSensitivity);

            var rot = PitchTarget.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
            PitchTarget.Rotation = rot;
        }

        // Send yaw delta to server for authoritative facing
        if (server != null)
            server.RpcId(server.GetMultiplayerAuthority(), "RequestYawDelta", motion.Relative.X * MouseSensitivity);
    }
}
