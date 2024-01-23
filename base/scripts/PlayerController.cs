using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

/** @todo
Convert JumpPower to a system of equations with
T = JumpTime
J = JumpImpulse
H = JumpHeight

Given a JumpTime T:
    J = g * (T / 2)
    H = (1/2) * g * (T / 2)^2   = (1/8) * g * T^2

Given a JumpImpulse J:
    T = 2 * (J / G)
    H = (1/2) * g * (T / 2)^2   = (1/8) * g * T^2

Given a JumpHeight H:
    T = 2 * sqrt(2 * H / g)
    J = g * (T / 2)

Move speed system of equations with
        Accel   MaxSpeed    Time    Run-up Dist.
Walk    Aw      Vw          Tw      Dw
Run     Ar      Vr          Tr      Dr
Sprint  As      Vs          Ts      Ds

Walk & Run (assume max speed v is always given)
Given acceleration a:
    t = v / a
    d = (1/2) * v^2 / a

Given run-up time t:
    a = v / t
    d = (1/2) * v * t

Given run-up distance d:
    t = 2 * d / v
    a = (1/2) * v^2 / d

Sprint (given max speed Vs):
Given acceleration As:
    Ts = (Vs - Vr) / As
    Ds = Vr * Ts + (1/2) * As * Ts^2

Given sprint time to max speed Ts:
    As = (Vs - Vr) / Ts
    Ds = Vr * Ts + (1/2) * (Vs - Vr) * Ts =     (1/2) * (Vs + Vr) * Ts

Total sprint windup time and distance:
    Ttotal = Tr + Td + Ts
    Dtotal = Dr + Vr * Td + Ds


Jump distance equations:

DWalk = walking per jump distance
DRun = Running per jump distance
DSprint = Sprinting per jump distance

    DWalk = WalkSpeed * T
    DRun = RunSpeed * T
    DSprint = SprintSpeed * T


**/
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
			if (inputs.Move.x > 0) {
				return Facing2D.Right;
			} else if (inputs.Move.x < 0) {
				return Facing2D.Left;
			} else {
				if (sprite != null && sprite.FlipH == true)
					return Facing2D.Left;
				return Facing2D.Right;
			}
		}
	}

	[Export] public float gravity = (int)ProjectSettings.GetSetting("physics/2d/default_gravity");
	[Export] public float jumpPower = 500.0f;
	[Export] public float maxFallSpeed = 1000f;
	[Export] public float coyoteTime = 0.2f;
	[Export] public bool freeGroundJumps = true;
	[Export] public int allowedJumps = 1;
	[Export] public float jumpDuration = 0.1f;
	[Export] public PackedScene jumpEffect = null;
	[Export] public PackedScene landEffect = null;
	[Export(PropertyHint.Layers2dPhysics)] public uint passThroughLayers;
	[Export] public float animRunSpeed = 200f;

	// [Export] public List<string> passThroughLayers = new List<string>();

	// The number of times this player has jumped since being grounded
	private int timesJumped = 0;
	// Whether the player is in the middle of a jump right now
	private bool Jumping {
		get { return jumpTime > 0; }
	}
	private float jumpTime = 0.1f;
	public Vector2 Velocity = Vector2.Zero;

	public class PlayerInputs {
		public Vector2 Move { get{ return _move;}}
		public bool JumpHeld { get { return _jumpHeld; }}
		public bool JumpPressed { get { return _jumpPressed; }}
		public bool RunHeld { get { return _runHeld; }}

		private Vector2 _move = Vector2.Zero;
		private bool _jumpHeld = false;
		private bool _jumpPressed = false;
		private bool _runHeld = false;

		public void Update() {
			_move.x = Input.GetAxis("p1_move_left", "p1_move_right");
			_move.y = Input.GetAxis("p1_move_up", "p1_move_down");
			_jumpPressed = Input.IsActionJustPressed("p1_jump");
			_jumpHeld = Input.IsActionPressed("p1_jump");
			_runHeld = Input.IsActionPressed("p1_run");
		}

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
	private PlayerInputs inputs = new PlayerInputs();

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
		inputs.Update();
		UpdateFreeJump(delta);
		HandleMovementInput(delta);
		ApplyGravity(delta);
		UpdatePassthrough();
		HandleJump(delta);
		if (IsOnFloor() && !Jumping)
			Velocity = MoveAndSlideWithSnap(Velocity, Vector2.Down * 10, Vector2.Up, true);
		else {
			Velocity = MoveAndSlide(Velocity, Vector2.Up, true);
		}
		LandingCheck();
		DetermineState();
		UpdateSpriteDirection();
		UpdateAnimationSpeed();
#if DEBUG
		HandleDebugInputs();
#endif
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

    private void GetLandingMaterial() {
        
    }

    // [Export] public float moveSpeed = 200;
	// [Export] public float moveAccel = 200;
	// [Export] public float moveDecel = 200;
	// [Export] public float moveExponent = 2.2f;
	// // [Export] public float walkSpeed = 200;
	// // [Export] public float runSpeed = 350;
	// // [Export] public float sprintSpeed = 500;
	// // [Export] public float airMaxAccelSpeed = 200;

	// [Export] public float groundFriction = 500;
	// [Export] public float airFriction = 20;
	// [Export] public float storeVelocityTime = 0.1f;
	// [Export] public float minSpeed = 10f;

	// private void HandleMovementInputA(float delta) {
	// 	if (Mathf.Abs(moveInput.x) > 0 && state != PlayerState.Dead) {
	// 		if (moveAccel > 0) {
	// 			if (Mathf.Sign(moveInput.x) != Mathf.Sign(Velocity.x)) {
	// 				ApplyFriction(delta);
	// 			}
	// 			if (Mathf.Abs(Velocity.x) < minSpeed)
	// 				Velocity.x = Mathf.Sign(moveInput.x) * minSpeed;
	// 			Velocity.x += moveAccel * moveInput.x * delta;
	// 			Velocity.x = Mathf.Clamp(Velocity.x, -walkSpeed, walkSpeed);
	// 			sprite.FlipH = moveInput.x < 0;
	// 		} else {
	// 			Velocity.x = walkSpeed * moveInput.x;
	// 			sprite.FlipH = moveInput.x < 0;
	// 		}
	// 	} else { // Apply friction
	// 		ApplyFriction(delta);
	// 		if (Mathf.Abs(Velocity.x) < minSpeed)
	// 			Velocity.x = 0;
	// 	}
	// }

	// private void ApplyFriction(float delta) {
	// 	float friction = IsOnFloor() ? groundFriction : airFriction;
	// 	Velocity.x = Mathf.Lerp(Velocity.x, 0, Mathf.Clamp(friction * delta, 0, 1));
	// }


	public enum MoveMode {
		Simple,
		ExponentialForce,
        AccelerationCurve,
		Advanced
	}

	[Export(PropertyHint.Enum,"Simple,Exponential Force,Acceleration Curve,Advanced")] public MoveMode moveMode;

	private void HandleMovementInput(float delta) {
		switch (moveMode) {
			case MoveMode.Simple:
				SimpleMove(delta);
				break;
			case MoveMode.ExponentialForce:
				ExpForceMove(delta);
				break;
            case MoveMode.AccelerationCurve:
				AccelCurveMove(delta);
				break;
			case MoveMode.Advanced:
				AdvMove(delta);
				break;

		}
	}

	[Export] float simple_moveSpeed = 200;
	private void SimpleMove(float delta) {
		Velocity.x = inputs.Move.x * simple_moveSpeed;
	}

	[Export] float exp_moveSpeed = 200;
	[Export] float exp_moveMult = 1f;
	[Export] float exp_brakeMult = 1f;
	[Export] float exp_forceExponent = 2;
	[Export] float exp_friction = 0.2f;
	private void ExpForceMove(float delta) {
		float targetXVelocity = 0;
        if (state != PlayerState.Dead)
            targetXVelocity = inputs.Move.x * exp_moveSpeed;

		float velDiff = targetXVelocity - Velocity.x;
		float forceMult = ( !Mathf.IsEqualApprox(targetXVelocity, 0, 0.01f) ? exp_moveMult : exp_brakeMult );

		float force = Mathf.Pow(Mathf.Abs(velDiff) * forceMult, exp_forceExponent);
		Velocity.x = Mathf.MoveToward(Velocity.x, targetXVelocity, force*delta);
		

		if (Mathf.IsEqualApprox(targetXVelocity, 0, 0.01f)) {
			// float frictionForce = Mathf.Min(Mathf.Abs(Velocity.x), exp_friction);
			Velocity.x = Mathf.MoveToward(Velocity.x, 0, exp_friction*delta);

		}
	}

	[Export] Curve curve_accelCurve;
	[Export] float curve_velDiffScaling = 100;
	[Export] float curve_accelScaling = 100;
	[Export] float curve_maxSpeed = 200;
    private void AccelCurveMove(float delta) {
		float targetXVelocity = 0;
        if (state != PlayerState.Dead)
            targetXVelocity = inputs.Move.x * curve_maxSpeed;

		float velDiff = targetXVelocity - Velocity.x;


		//GD.Print(velDiff);
		float force = curve_accelScaling * curve_accelCurve.Interpolate(Mathf.Abs(velDiff) / curve_velDiffScaling);
		Velocity.x = Mathf.MoveToward(Velocity.x, targetXVelocity, force*delta);
		//GD.Print(Mathf.Abs(velDiff) / curve_velDiffScaling);

		// if (Mathf.IsEqualApprox(targetXVelocity, 0, 0.01f)) {
		// 	// float frictionForce = Mathf.Min(Mathf.Abs(Velocity.x), exp_friction);
		// 	Velocity.x = Mathf.MoveToward(Velocity.x, 0, exp_friction*delta);

		// }

    }

	[Export] float adv_groundFriction = 1;
	[Export] float adv_airFriction = 0.65f;
	[Export] float adv_walkSpeed = 200;
	[Export] float adv_runSpeed = 300;
	[Export] float adv_sprintSpeed = 400;
	[Export] float adv_sprintAccel = 200;
	[Export] float adv_sprintReduce = 100;
	[Export] float adv_sprintStartupTime = 1;
	[Export] float adv_walkAccel = 1000;
	[Export] float adv_runAccel = 1000;
	[Export] float adv_walkReduce = 600;
	[Export] float adv_runReduce = 600;
	[Export] float adv_airMaxSpeedGain = 200;
	[Export] bool adv_canSprint = true;
	[Export] float adv_changeDirMult = 1.5f;
	[Export] float adv_brakeMult = 1.5f;

	float adv_sprintDelayLeft = 0;
	//bool adv_isSkidding = false;
	private void AdvMove(float delta) {
		float targetXVelocity = 0;
        if (state != PlayerState.Dead)
            targetXVelocity = inputs.Move.x * (inputs.RunHeld ? adv_runSpeed : adv_walkSpeed);

        // Sprint delay logic
		if (Mathf.Abs(Velocity.x) > adv_runSpeed) { // Already at sprinting speed
			adv_sprintDelayLeft = 0;
		} else if (Mathf.Abs(Velocity.x) == adv_runSpeed && IsOnFloor()) { // At running speed, count down delay
			adv_sprintDelayLeft -= delta;
		} else { // Not at max speed, reset delay countdown
			adv_sprintDelayLeft = adv_sprintStartupTime;
		}


		bool isSprinting = adv_canSprint && adv_sprintDelayLeft <= 0 && Mathf.Sign(inputs.Move.x) == Mathf.Sign(Velocity.x) && inputs.RunHeld;
		if (isSprinting) {
			targetXVelocity = inputs.Move.x * adv_sprintSpeed;
		}


        // Cap midair speed gain
        if (!IsOnFloor()) {
			float maxAirSpeed = Mathf.Max(Mathf.Abs(Velocity.x), adv_airMaxSpeedGain);
			targetXVelocity = Mathf.Clamp(targetXVelocity, Mathf.Min(Velocity.x, -adv_airMaxSpeedGain), Mathf.Max(Velocity.x, adv_airMaxSpeedGain));
		}



		float friction = ( IsOnFloor() ? adv_groundFriction : adv_airFriction );
		float acceleration;

        // Overspeed, decelerate using the reduce rate values
        if (Mathf.Abs(Velocity.x) > Mathf.Abs(targetXVelocity) && Mathf.Sign(Velocity.x) == Mathf.Sign(targetXVelocity)) {
            if (isSprinting)
				acceleration = adv_sprintReduce;
			else
    			acceleration = ( inputs.RunHeld ? adv_runReduce : adv_walkReduce );
        } else {
            if (isSprinting)
				acceleration = adv_sprintAccel;
			else
    			acceleration = ( inputs.RunHeld ? adv_runAccel : adv_walkAccel );
        }
        if (Mathf.Abs(targetXVelocity) == 0)
			acceleration *= adv_brakeMult;
        else if (Mathf.Sign(Velocity.x) != Mathf.Sign(targetXVelocity))
			acceleration *= adv_changeDirMult;

		float lastVelocity = Velocity.x;
		Velocity.x = Mathf.MoveToward(Velocity.x, targetXVelocity, acceleration * friction * delta);

        // if (lastVelocity != Velocity.x)
		// 	GD.Print(Velocity.x);
	}

	/*	-> with velocity,  <- against velocity,  o neutral, (r) run button 
			|	o				|	->					|	-> (r)			|	<-			|	<- (r)		|
	idle	|	nop				|	walk-accel			|	run-accel		|	-			|	-			|
	walk	|	stop to idle	|	walk-accel			|	run-accel		|	brake-accel	|	brake-accel	|
	run
	*/




	private void ApplyGravity(float delta) {
		if (!IsOnFloor()) {
			Velocity.y = Mathf.Min(Velocity.y + gravity * delta, maxFallSpeed);
		}
	}


	private void UpdatePassthrough() {
		if (inputs.Move.y > 0.5) {
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
			if (!inputs.JumpHeld) { // End jump if jump button is released
				jumpTime = 0;
			}
		}
		if (inputs.JumpPressed) {
			TryJump();
		}
		//GD.Print(Velocity);

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
		if (!Jumping && (timesJumped < allowedJumps || freeJump) && state != PlayerState.Dead) {
			float new_y = -jumpPower * powerMultiplier;
			if (Velocity.y < 2*new_y) // Can't jump if already moving up very quickly
				return;
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
		if (inputs.JumpHeld) {
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
	private void UpdateSpriteDirection() {
		if (sprite != null) {
			if (Facing == Facing2D.Left) {
				sprite.FlipH = true;
			} else {
				sprite.FlipH = false;
			}
		}
	}

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
				sprite.SpeedScale = 1;
				sprite.Play("idle");
				break;
			case PlayerState.Walk:
				sprite.Play("walk");
				break;
			case PlayerState.Jump:
				sprite.SpeedScale = 1;
				sprite.Play("jump");
				break;
			case PlayerState.Dead:
				sprite.SpeedScale = 1;
				sprite.Play("dead");
				break;
			case PlayerState.Fall:
				sprite.SpeedScale = 1;
				sprite.Play("jump");
				break;
		}
	}
    
    private void UpdateAnimationSpeed() {
        switch (state) {
            case PlayerState.Walk:
 				sprite.SpeedScale = Mathf.Abs(Velocity.x / animRunSpeed);
				break;
			default:
				sprite.SpeedScale = 1;
				break;
		}
    }

}
