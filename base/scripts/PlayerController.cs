using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

public enum Facing2D {
	Right,
	Left
}

/// <summary>
/// Class which handles player movement
/// </summary>
public partial class PlayerController : KinematicBody2D {
	//public GroundCheck groundCheck = null;
	[Export] public NodePath spritePath;
	public AnimatedSprite sprite;
	[Export] public NodePath healthPath;
	[Export] public NodePath killboxPath;
	public Health health;
	//[Export] public Health playerHealth;

	// Which way the player is facing right now
	public Facing2D Facing {
		get {
			if (moveInput.x > 0) {
				return Facing2D.Right;
			} else if (moveInput.x < 0) {
				return Facing2D.Left;
			} else {
				if (sprite != null && sprite.FlipH == true)
					return Facing2D.Left;
				return Facing2D.Right;
			}
		}
	}

	[Export] public float gravity = (int)ProjectSettings.GetSetting("physics/2d/default_gravity");
	[Export] public float walkSpeed = 200;
	[Export] public float runSpeed = 350;
	[Export] public float sprintSpeed = 500;
	[Export] public float airMaxAccelSpeed = 200;

	[Export] public float moveAccel = 0;
	[Export] public float jumpPower = 500.0f;
	[Export] public float maxFallSpeed = 1000f;
	[Export] public float groundFriction = 500;
	[Export] public float airFriction = 20;
	[Export] public float coyoteTime = 0.2f;
	[Export] public bool freeGroundJumps = true;
	[Export] public int allowedJumps = 1;
	[Export] public float jumpDuration = 0.1f;
	[Export] public PackedScene jumpEffect = null;
	[Export] public PackedScene landEffect = null;
	[Export] public float storeVelocityTime = 0.1f;
	[Export] public float minSpeed = 10f;
	[Export(PropertyHint.Layers2dPhysics)] public uint passThroughLayers;

	// [Export] public List<string> passThroughLayers = new List<string>();

	// The number of times this player has jumped since being grounded
	private int timesJumped = 0;
	// Whether the player is in the middle of a jump right now
	private bool Jumping {
		get { return jumpTime > 0; }
	}
	private float jumpTime = 0;
	public Vector2 Velocity = Vector2.Zero;
	public Vector2 storedVelocity = Vector2.Zero;



	private Vector2 moveInput = Vector2.Zero;
	private bool jumpHeld = false;
	private bool jumpPressed = false;
	private bool runHeld = false;
	private void UpdateInputs() {
		if (!health.IsAlive())
			return;
		moveInput.x = Input.GetAxis("p1_move_left", "p1_move_right");
		moveInput.y = Input.GetAxis("p1_move_up", "p1_move_down");
		jumpPressed = Input.IsActionJustPressed("p1_jump");
		jumpHeld = Input.IsActionPressed("p1_jump");
		runHeld = Input.IsActionPressed("p1_run");
	}

	public enum PlayerState {
		Idle,
		Walk,
		Jump,
		Fall,
		Dead
	}

	public PlayerState state = PlayerState.Idle;
	private bool forcePlayAnim = false;

	public override void _Ready() {
		sprite = GetNode<AnimatedSprite>(spritePath);
		health = GetNode<Health>(healthPath);
		//Connect("DamageDealt", GetNode(killboxPath), nameof(Bounce));
	}

#if DEBUG
	private void HandleDebugInputs() {
		bool nextCP = Input.IsActionJustPressed("debug_next_cp");
		bool prevCP = Input.IsActionJustPressed("debug_prev_cp");
		bool respawnPressed = Input.IsActionJustPressed("debug_respawn");

		if (nextCP) {
			health.SetNextRespawn();
		} else if (prevCP) {
			health.SetPrevRespawn();
		}

		if (nextCP || prevCP || respawnPressed)
			health.Respawn();
	}


#endif


	/// <summary>
	/// Description:
	/// Standard Unity function called once every frame after update
	/// Every frame, process input, move the player, determine which way they should face, and choose which state they are in
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	public override void _PhysicsProcess(float delta) {
		//base._PhysicsProcess(delta);
		UpdateInputs();
		UpdateFreeJump(delta);

		HandleAdvancedMovement(delta);

		HandleMovementInput(delta);
		HandleJump(delta);
		//UpdateSpriteDirection();
		if (IsOnFloor() && !Jumping)
			Velocity = MoveAndSlideWithSnap(Velocity, Vector2.Down * 10, Vector2.Up, true);
		else {
			Velocity = MoveAndSlide(Velocity, Vector2.Up, true);
		}
		LandingCheck();
		DetermineState();
#if DEBUG
		HandleDebugInputs();
#endif
	}

    private void HandleAdvancedMovement(float delta) {
        // Dead or zero input
            // On ground
                // Run held
                    // Slow: Med friction
                // Run not held
                    // Brake: high friction
            // Midair
                // Slow: Med friction
        
        // Input in direction of movement
            // On ground
                // Run held
                    // Accel to run speed
                // Run not held
                    // Slow: to walk speed
                    // Set to walk speed
                // targetVel = (run ? walkSpeed : runSpeed) * input.x
                // if targetVel < vel
                    //  
            // Midair

        // Input against direction of movement
            // On ground
            // Midair
    }

	private float freeJumpTime = 0;

    private void UpdateFreeJump(float delta) {
        if (IsOnFloor()) {
			freeJumpTime = coyoteTime;
		} else if (coyoteTime > 0) {
			freeJumpTime -= delta;
		}
    }

	private bool HasFreeJump() {
		return freeJumpTime > 0 && freeGroundJumps;
	}

    private void ExhaustFreeJump() {
		freeJumpTime = 0;
	}

	private float LandEffectMinSpeed = 5;
	private float fallSpeed = 0;
	private void LandingCheck() {
		if (IsOnFloor()) {
			if (fallSpeed > LandEffectMinSpeed) {
				SpawnEffect(landEffect);
			}
			fallSpeed = 0;
		} else {
			fallSpeed = Velocity.y;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="delta"></param>
	private void HandleMovementInput(float delta) {
		// Horizontal movement        
		if (Mathf.Abs(moveInput.x) > 0 && state != PlayerState.Dead) {
			if (moveAccel > 0) {
				if (Mathf.Sign(moveInput.x) != Mathf.Sign(Velocity.x)) {
					ApplyFriction(delta);
				}
				if (Mathf.Abs(Velocity.x) < minSpeed)
					Velocity.x = Mathf.Sign(moveInput.x) * minSpeed;
				Velocity.x += moveAccel * moveInput.x * delta;
				Velocity.x = Mathf.Clamp(Velocity.x, -walkSpeed, walkSpeed);
				sprite.FlipH = moveInput.x < 0;
			} else {
				Velocity.x = walkSpeed * moveInput.x;
				sprite.FlipH = moveInput.x < 0;
			}
		} else { // Apply friction
			ApplyFriction(delta);
			if (Mathf.Abs(Velocity.x) < minSpeed)
				Velocity.x = 0;
		}
		// Apply gravity
		if (!IsOnFloor()) {
			Velocity.y = Mathf.Min(Velocity.y + gravity * delta, maxFallSpeed);
		}
		UpdatePassthrough();
	}

	private void ApplyFriction(float delta) {
		float friction = IsOnFloor() ? groundFriction : airFriction;
		Velocity.x = Mathf.Lerp(Velocity.x, 0, Mathf.Clamp(friction * delta, 0, 1));
	}

	private void UpdatePassthrough() {
		if (moveInput.y > 0.5) {
			CollisionMask &= ~passThroughLayers;
		} else {
			CollisionMask |= passThroughLayers;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="delta"></param>
	private void HandleJump(float delta) {
		if (Jumping) {
			jumpTime -= delta;
		}
		if (jumpPressed) {
			TryJump();
		}

	}

	/// <summary>
	/// Description:
	/// Coroutine which causes the player to jump.
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	/// <returns>IEnumerator: makes coroutine possible</returns>
	private void TryJump(float powerMultiplier = 1.0f) {
		bool freeJump = HasFreeJump();
		if ((timesJumped < allowedJumps || freeJump) && state != PlayerState.Dead) {
			float new_y = -jumpPower * powerMultiplier;
			jumpTime = jumpDuration;
			SpawnEffect(jumpEffect);
			Velocity.y = new_y;
			if (freeJump)
				ExhaustFreeJump();
			else
                timesJumped++;
		}
	}

	/// <summary>
	/// Description:
	/// Spawns an effect
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	private void SpawnEffect(PackedScene effect) {
		if (effect != null) {
			var inst = effect.Instance<Node2D>();
			GetViewport().AddChild(inst);
			inst.GlobalPosition = GlobalPosition;
		}
	}

	/// <summary>
	/// Description:
	/// Bounces the player upwards, refunding jumps.
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	public void Bounce() {
		timesJumped = 0;
		if (jumpHeld) {
			TryJump(1.5f);
		} else {
			TryJump(1.0f);
		}
	}

	/// <summary>
	/// Description:
	/// Determines which way the player should be facing, then makes them face in that direction
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	// private void UpdateSpriteDirection() {
	// 	if (sprite != null) {
	// 		if (Facing == Facing2D.Left) {
	// 			sprite.FlipH = true;
	// 		} else {
	// 			sprite.FlipH = false;
	// 		}
	// 	}
	// }

	/// <summary>
	/// Description:
	/// Sets the player's current state
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	/// <param name="newState">The PlayerState to set the current state to</param>
	private void SetState(PlayerState newState) {
		var oldState = state;
		state = newState;
		if (oldState != newState || forcePlayAnim) {

			PlayStateAnimation();
			forcePlayAnim = false;
		}
	}

	/// <summary>
	/// Description:
	/// Determines which state is appropriate for the player currently
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	private void DetermineState() {
		if (!health.IsAlive()) {
			SetState(PlayerState.Dead);
		} else if (IsOnFloor()) {
			if (Mathf.Abs(Velocity.x) > 0.1) {
				SetState(PlayerState.Walk);
			} else {
				SetState(PlayerState.Idle);
			}
			if (!Jumping) {
				timesJumped = 0;
			}
		} else {
			if (Jumping) {
				SetState(PlayerState.Jump);
			} else {
				SetState(PlayerState.Fall);
			}
		}
	}

	private void PlayStateAnimation() {
		switch (state) {
			case PlayerState.Idle:
				sprite.Play("idle");
				break;
			case PlayerState.Walk:
				sprite.Play("walk");
				break;
			case PlayerState.Jump:
				sprite.Play("jump");
				break;
			case PlayerState.Dead:
				sprite.Play("dead");
				break;
			case PlayerState.Fall:
				sprite.Play("jump");
				break;
		}
	}

}
