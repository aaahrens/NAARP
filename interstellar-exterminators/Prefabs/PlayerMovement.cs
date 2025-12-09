using Godot;

public partial class PlayerMovement : Node
{
	[Export] public CharacterBody3D Body;

	/// <summary>
	/// The axis we're moving on.
	/// </summary>
	[Export] 
	public Node3D DirectionBasis;

	/// <summary>
	/// The players movement speed in meters per second.
	/// </summary>
	[Export (PropertyHint.Range, "0,20,0.1")]
	public float MoveSpeed = 5.0f;

	/// <summary>
	/// The players initial jump speed in meters per second.
	/// </summary>
	[Export(PropertyHint.Range, "0,20,0.1")]
	public float JumpVelocity = 5.0f;

	/// <summary>
	/// Gravities acceleration in meters per second.
	/// </summary>
	private float gravity = 9.8f;

	public override void _Ready()
	{
		if (Body == null)
		{
			GD.PushError("CharacterMover: Body is not assigned.");
			SetPhysicsProcess(false);
			return;
		}

		if (DirectionBasis == null)
		{
			// Default to moving relative to the body if nothing is set
			DirectionBasis = Body;
		}

		gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Body == null || DirectionBasis == null)
			return;

		Vector3 velocity = Body.Velocity;

		Basis basis = DirectionBasis.GlobalTransform.Basis;
		Vector3 forward = -basis.Z;
		Vector3 right = basis.X;

		Vector3 direction = Vector3.Zero;

		if (Input.IsActionPressed("move_forward"))
			direction += forward;
		if (Input.IsActionPressed("move_backward"))
			direction -= forward;
		if (Input.IsActionPressed("move_left"))
			direction -= right;
		if (Input.IsActionPressed("move_right"))
			direction += right;

		direction = direction.Normalized();

		velocity.X = direction.X * MoveSpeed;
		velocity.Z = direction.Z * MoveSpeed;

		if (!Body.IsOnFloor())
		{
			velocity.Y -= gravity * (float)delta;
		}
		else
		{
			if (Input.IsActionJustPressed("jump"))
				velocity.Y = JumpVelocity;
			else
				velocity.Y = 0f;
		}

		Body.Velocity = velocity;
		Body.MoveAndSlide();
	}
}
