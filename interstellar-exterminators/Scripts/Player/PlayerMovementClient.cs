using Godot;

/// <summary>
/// Handles local input and sends movement requests to the server.
/// </summary>
public partial class PlayerMovementClient : Node
{
    /// <summary>
    /// Reference to the server-side movement node.
    /// </summary>
    private PlayerMovementServer playerMovementServer;

    public override void _Ready()
    {
        playerMovementServer = this.FindInSceneTreeOfType<PlayerMovementServer>();
    }

    public override void _Process(double delta)
	{
        if (playerMovementServer == null)
            return;

        // Only allow the local authority to send movement requests
        if (!IsMultiplayerAuthority())
            return;

        // Build movement direction based on input
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_forward"))
            direction += Vector3.Forward;
        if (Input.IsActionPressed("move_backward"))
            direction += Vector3.Back;
        if (Input.IsActionPressed("move_left"))
            direction += Vector3.Left;
        if (Input.IsActionPressed("move_right"))
            direction += Vector3.Right;

        direction = direction.Normalized();
        bool jump = Input.IsActionJustPressed("jump");

        if(direction != Vector3.Zero || jump)
        {
            playerMovementServer.RpcId(playerMovementServer.GetMultiplayerAuthority(), "RequestMove", direction, jump);
        }
    }
}
