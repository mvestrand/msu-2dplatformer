using Godot;

/// <summary>
/// EnemyBase-derived enemy class which walks in a direction until it hits a wall
/// </summary>
public class WalkingEnemy : EnemyBase {
	/// <summary>
	/// Enum to track which direction this enemy is walking
	/// </summary>
	public enum WalkDirections { Right, Left, None }

    [Export] public NodePath leftEdgePath;
    public RayCast2D leftEdgeTest;
    [Export] public NodePath rightEdgePath;
    public RayCast2D rightEdgeTest;

	[Export] public WalkDirections walkDirection = WalkDirections.None;
	[Export] public bool willTurnAroundAtEdge = false;

    // The sprite renderer for this enemy
    //private SpriteRenderer spriteRenderer = null;
    // private AnimatedSprite sprite;
    // private AnimationPlayer animator;

	/// <summary>
	/// Description:
	/// Sets up this enemy
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	public override void _Ready() {
        //sprite = GetNode<AnimatedSprite>("AnimatedSprite");
        leftEdgeTest = GetNode<RayCast2D>(leftEdgePath);
        rightEdgeTest = GetNode<RayCast2D>(rightEdgePath);        
	}

    /// <summary>
    /// Description:
    /// Override of EnemyBase.GetMovement
    /// Moves in a direction until a wall is hit, then switches direction
    /// Input: 
    /// none
    /// Return: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The movement for this frame</returns>
    protected override Vector2 GetSteerVelocity(float delta) {

        DetermineWalkDirection();

		// Determine the movement vector based on the direction that the enemy is currently moving in
		switch (walkDirection) {
			case WalkDirections.None:
				enemyState = EnemyState.Idle;
				return Vector2.Zero;
			case WalkDirections.Left:
				enemyState = EnemyState.Walking;
				return Vector2.Left * moveSpeed;
			case WalkDirections.Right:
				enemyState = EnemyState.Walking;
				return Vector2.Right * moveSpeed;
			default:
				return base.GetSteerVelocity(delta);
		}
	}

	/// <summary>
	/// Description:
	/// Determines whether a change in direction is needed, and changes it if necessary
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	private void DetermineWalkDirection() {
		if (TestWall() || GetIsNearEdge()) {
			TurnAround();
		}
		// if (sprite != null) {
		// 	sprite.FlipH = ( walkDirection == WalkDirections.Right );
		// }
	}

	/// <summary>
	/// Description:
	/// Turns the enemy around
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	private void TurnAround() {
        //GD.Print("turn");
		if (walkDirection == WalkDirections.Left) {
			walkDirection = WalkDirections.Right;
		} else if (walkDirection == WalkDirections.Right) {
			walkDirection = WalkDirections.Left;
		}
	}

	/// <summary>
	/// Description:
	/// Tests whether this enemy has hit a wall in the direction that it is walking in
	/// Input: 
	/// none
	/// Return:
	/// bool
	/// </summary>
	/// <returns>bool: Whether or not this enemy has hit a wall in the direction it is walking</returns>
	protected virtual bool TestWall() {
		// switch (walkDirection) {
		// 	case WalkDirections.Left:
		// 		if (wallTestLeft != null) {
		// 			return wallTestLeft.CheckGrounded();
		// 		}
		// 		break;
		// 	case WalkDirections.Right:
		// 		if (wallTestRight != null) {
		// 			return wallTestRight.CheckGrounded();
		// 		}
		// 		break;
		// }
		return IsOnWall();
	}

	/// <summary>
	/// Description:
	/// Tests each edge check in the list "edgeTesters" to see if the enemy must change directions.
	/// Input: 
	/// none
	/// Return: 
	/// bool
	/// </summary>
	/// <returns>bool: Whether or not this enemy is near an edge and must turn around</returns>
	private bool GetIsNearEdge() {
		RayCast2D check = null;
		if (walkDirection == WalkDirections.Left) {
			check = leftEdgeTest;
		} else if (walkDirection == WalkDirections.Right) {
			check = rightEdgeTest;
		}
		if (check != null) {
            check.ForceRaycastUpdate();
            if (!check.IsColliding())
    			return willTurnAroundAtEdge;
		}
		return false;
	}
}
