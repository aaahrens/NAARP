using Godot;

/// <summary>
/// Receives yaw input from client and applies authoritative yaw to the character.
/// </summary>
public partial class MouseLookServer : Node
{
    /// <summary>
    /// The node to rotate for authoritative yaw (used for movement/hits).
    /// </summary>
    [Export]
    [ExportGroup("Mouse Look Control")]
    public Node3D YawTarget;

    public override void _Ready()
    {
        AddToGroup("MouseLookServer");
    }

    /// <summary>
    /// Receives yaw delta from client and applies it to the authoritative yaw target.
    /// </summary>
    /// <param name="yawDelta">Yaw change in radians.</param>
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void RequestYawDelta(float yawDelta)
    {
        if (YawTarget != null)
            YawTarget.RotateY(-yawDelta);
    }
}
