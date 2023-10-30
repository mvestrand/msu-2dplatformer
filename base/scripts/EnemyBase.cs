using Godot;

public enum EnemyState { Walking, Dead, Idle }

/// <summary>
/// This class contains settings for and handles the control of an enemy
/// </summary>
public abstract partial class EnemyBase : KinematicBody2D {
	[Export] public float moveSpeed = 2f;

	[Export] public EnemyState enemyState;

	[Export] public bool shouldSnap = true;
	[Export] public Vector2 snapVec = Vector2.Down;
	[Export] public Vector2 upVec = Vector2.Up;
	[Export] public Vector2 gravity = Vector2.Down * (int)ProjectSettings.GetSetting("physics/2d/default_gravity");
	public Vector2 Velocity = Vector2.Zero;


	public override void _PhysicsProcess(float delta) {
		// Every frame, get the desired movement of this enemy, then move it.
		var steering = GetSteerVelocity(delta);
		UpdateVelocity(delta, steering);
		if (shouldSnap)
			Velocity = MoveAndSlideWithSnap(Velocity, snapVec, upVec);
		else
			Velocity = MoveAndSlide(Velocity, upVec);
	}

	protected virtual Vector2 GetSteerVelocity(float delta) {
		return Vector2.Zero;
	}

	protected virtual void UpdateVelocity(float delta, Vector2 steering) {
		Velocity.x = steering.x;
		if (!IsOnFloor()) {
			Velocity += gravity * delta;
		}
	}
}
