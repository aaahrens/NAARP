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

    /// <summary>
    /// Reference to our network authority component.
    /// </summary>
    private NetworkAuthority networkAuthority;

    public override void _Ready()
    {
        networkAuthority = this.FindInSceneTreeOfType<NetworkAuthority>();
        if (networkAuthority == null)
        {
            GD.PushError("MouseLookServer: No NetworkAuthority found in scene tree.");
        }
    }

    /// <summary>
    /// Receives yaw delta from client and applies it to the authoritative yaw target.
    /// </summary>
    /// <param name="yawDelta">Yaw change in radians.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestYawDelta(float yawDelta)
    {
        // Only the server should ever apply authoritative yaw.
        if (!Multiplayer.IsServer())
            return;

        // If we don't have a valid authority component with a PeerId, we can't validate anything.
        if (networkAuthority == null || networkAuthority.PeerId == 0)
            return;

        int sender = Multiplayer.GetRemoteSenderId();
        if (sender != networkAuthority.PeerId)
        {
            GD.PushWarning(
                $"MouseLookServer: Rejected yaw RPC from peer {sender}, owner is {networkAuthority.PeerId}."
            );
            return;
        }

        if (YawTarget != null)
            YawTarget.RotateY(-yawDelta);
    }
}
