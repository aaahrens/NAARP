using Godot;
using System;

/// <summary>
/// Handles local input and sends movement requests to the server.
/// </summary>
public partial class PlayerMovementClient : Node
{
    /// <summary>
    /// Reference to the server-side movement node.
    /// </summary>
    private PlayerMovementServer server;

    public override void _Ready()
    {
        server = GetTree().GetFirstNodeInGroup("PlayerMovementServer") as PlayerMovementServer;
    }

    public override void _Process(double delta)
	{
        if (server == null)
            return;

        // Only allow the local authority to send movement requests
        if (!IsMultiplayerAuthority())
            return;

        // Build movement direction based on input
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_forward"))
            direction += -server.DirectionBasis.GlobalTransform.Basis.Z;
        if (Input.IsActionPressed("move_backward"))
            direction -= -server.DirectionBasis.GlobalTransform.Basis.Z;
        if (Input.IsActionPressed("move_left"))
            direction -= server.DirectionBasis.GlobalTransform.Basis.X;
        if (Input.IsActionPressed("move_right"))
            direction += server.DirectionBasis.GlobalTransform.Basis.X;

        direction = direction.Normalized();
        bool jump = Input.IsActionJustPressed("jump");

        // Send movement request to server via RPC
        server.RpcId(server.GetMultiplayerAuthority(), "RequestMove", direction, jump, Multiplayer.GetUniqueId());
    }
}
