using Godot;
using System;

public partial class PlayerMovementServer : Node
{
    /// <summary>
    /// The characters body we actually want to move.
    /// </summary>
    [Export]
    [ExportGroup("Movement Control")]
    public CharacterBody3D Body;

    /// <summary>
    /// The axis we're moving on.
    /// </summary>
    [Export]
    [ExportGroup("Movement Control")]
    public Node3D DirectionBasis;

    /// <summary>
    /// The players movement speed in meters per second.
    /// </summary>
    [Export(PropertyHint.Range, "0,20,0.1")]
    [ExportGroup("Movement Control")]
    public float MoveSpeed = 5.0f;

    /// <summary>
    /// The players initial jump speed in meters per second.
    /// </summary>
    [Export(PropertyHint.Range, "0,20,0.1")]
    [ExportGroup("Movement Control")]
    public float JumpVelocity = 5.0f;

    /// <summary>
    /// Gravities acceleration in meters per second.
    /// </summary>
    private float gravity;

    private Vector3 requestedDirection = Vector3.Zero;
    private bool requestedJump = false;

    public override void _Ready()
    {
        AddToGroup("PlayerMovementServer");

        if (Body == null)
            Body = GetParent<CharacterBody3D>();

        if (DirectionBasis == null)
            DirectionBasis = Body;

        gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    }

    /// <summary>
    /// Receives movement requests from clients, validates sender authority.
    /// </summary>
    /// <param name="direction">Requested movement direction.</param>
    /// <param name="jump">Requested jump action.</param>
    /// <param name="senderAuthority">Sender's network authority ID.</param>
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void RequestMove(Vector3 direction, bool jump, int senderAuthority)
    {
        // Validate sender authority
        if (Multiplayer.GetRemoteSenderId() != senderAuthority)
            return;

        requestedDirection = direction;
        requestedJump = jump;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Body == null || DirectionBasis == null)
            return;

        Vector3 velocity = Body.Velocity;

        velocity.X = requestedDirection.X * MoveSpeed;
        velocity.Z = requestedDirection.Z * MoveSpeed;

        if (!Body.IsOnFloor())
        {
            velocity.Y -= gravity * (float)delta;
        }
        else
        {
            if (requestedJump)
                velocity.Y = JumpVelocity;
            else
                velocity.Y = 0f;
        }

        Body.Velocity = velocity;
        Body.MoveAndSlide();

        // Reset the requested movements until we need to process them again.
        requestedJump = false;
        requestedDirection = Vector3.Zero;
    }
}
