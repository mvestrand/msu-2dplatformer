using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public interface IHealth {
    int TeamId { get; }
    int MaxHp { get; set; }
    int Hp { get; set; }
    bool IsInvincible();
    bool IsAlive();
    TakeDamageOutcome TakeDamage(int amount);
    bool ReceiveHealing(int amount, int overheal = 0);
    void Die();
}

// public interface ILives {
//     int Lives { get; set; }
//     int MaxLives { get; set; }
//     float RespawnDelay { get; set; }
// }

public enum TakeDamageOutcome { Ignored = 0, Blocked = 1, Received = 2, Killed = 4 }


/// <summary>
/// This class handles the health state of a game object.
/// 
/// Implementation Notes: 2D Rigidbodies must be set to never sleep for this to interact with trigger stay damage
/// </summary>
public partial class Health : Node, IHealth {
    [Export] public int teamId = 0;
    public int TeamId { get { return teamId; } }
    [Export] private int _maxHp = 1;
    public int MaxHp { get { return _maxHp; } set { _maxHp = value; }} 
	[Export] private int _hp = 1;
    public int Hp { 
        get { return _hp; } 
        set { 
            if (value != _hp) { 
                var tmp = _hp; 
                _hp = value;
                EmitSignal(nameof(HealthChanged), tmp, _hp);
            }
        }
    }
	[Export] public float invincibleTimeOnHit = 1f;
    [Export] public bool invincibleAlways = false;
    [Export] public bool ignoreOnInvincible = false;
    [Export] public bool ignoreOnDead = true;

	[Export] public PackedScene deathEffect;
	[Export] public PackedScene hitEffect;

    public bool IsAlive() { return _hp > 0; }
    public bool IsInvincible() { return invincibleAlways || invincibleTimeLeft > 0; }


    [Signal] public delegate void HealthChanged(float oldHp, float newHp);
    [Signal] public delegate void Damaged(float amount);
    [Signal] public delegate void Healed(float amount);
    [Signal] public delegate void Died();

	[Export] public bool useLives = false;
	[Export] public int currentLives = 3;
	[Export] public int maximumLives = 5;
	[Export] public float respawnDelay = 3f;
    [Export] public bool infiniteRespawns = false;
    [Export] public Vector2 respawnOffset = Vector2.Zero;



    private float invincibleTimeLeft = 0;
	private float respawnDelayLeft = 0;


	/// <summary>
	/// Description:
	/// Standard Unity function called once before the first update
	/// Input:
	/// none
	/// Return:
	/// void (no return)
	/// </summary>
	public override void _Ready() {
        var parent = GetParent<Node2D>();
        if (parent != null)
		    SetRespawnPoint(parent.GlobalPosition);
	}

	/// <summary>
	/// Description:
	/// Standard Unity function called once per frame
	/// Input:
	/// none
	/// Return:
	/// void (no return)
	/// </summary>
	public override void _PhysicsProcess(float delta) {
		UpdateInvincibility(delta);
		RespawnCheck(delta);
	}

	// The time left before respawn

	/// <summary>
	/// Description:
	/// Checks to see if the player should be respawned yet and only respawns them if the alloted time has passed
	/// Input:
	/// none
	/// Return:
	/// void (no return)
	/// </summary>
	private void RespawnCheck(float delta) {
		if (!IsAlive() && respawnDelay > 0) {
            respawnDelayLeft -= delta;
			if (respawnDelayLeft <= 0) {
				Respawn();
			}
		}
	}

	/// <summary>
	/// Description:
	/// Checks against the current time and the time when the health can be damaged again.
	/// Removes invicibility if the time frame has passed
	/// Input:
	/// None
	/// Returns:
	/// void (no return)
	/// </summary>
	private void UpdateInvincibility(float delta) {
		if (invincibleTimeLeft > 0) { invincibleTimeLeft -= delta; }
	}

	// The position that the health's gameobject will respawn at
	private Vector2 respawnPosition;

	/// <summary>
	/// Description:
	/// Changes the respawn position to a new position
	/// Input:
	/// Vector3 newRespawnPosition
	/// Returns:
	/// void (no return)
	/// </summary>
	/// <param name="newRespawnPosition">The new position to respawn at</param>
	public void SetRespawnPoint(Vector2 newRespawnPosition) {
		respawnPosition = newRespawnPosition;
	}

	/// <summary>
	/// Description:
	/// Repositions the health's game object to the respawn position and resets the health to the default value
	/// Input:
	/// None
	/// Returns:
	/// void (no return)
	/// </summary>
	void Respawn() {
        var parent = GetParent<Node2D>();
        parent.GlobalPosition = respawnPosition + respawnOffset;
		Hp = MaxHp;
        respawnDelayLeft = 0;
        invincibleTimeLeft = invincibleTimeOnHit;
	}

	/// <summary>
	/// Description:
	/// Applies damage to the health unless the health is invincible.
	/// Input:
	/// int damageAmount
	/// Returns:
	/// void (no return)
	/// </summary>
	/// <param name="amount">The amount of damage to take</param>
	public TakeDamageOutcome TakeDamage(int amount) {
        GD.Print("dmg");
		if (IsInvincible())
			return ignoreOnInvincible ? TakeDamageOutcome.Ignored : TakeDamageOutcome.Blocked;
        if (!IsAlive())
			return ignoreOnDead ? TakeDamageOutcome.Ignored : TakeDamageOutcome.Blocked;
        Hp = Mathf.Max(Hp - amount, 0);
        EmitSignal(nameof(Damaged), amount);
        if (!IsAlive()) {
            GD.Print("die");
            Die();
            return TakeDamageOutcome.Received | TakeDamageOutcome.Killed;
        }
        SpawnEffect(hitEffect);
        return TakeDamageOutcome.Received;
	}

    public bool ReceiveHealing(int amount, int overheal = 0) {
        var newHp = Mathf.Min(Hp + amount, MaxHp + overheal); // Limit new hp by the max hp shifted by overheal
        var hpGain = newHp - Hp;
        if (hpGain > 0) { // Only receive healing if health would be gained
            Hp = newHp;
            EmitSignal(nameof(Healed), hpGain);
            return true;
        } else {
            return false;
        }
    }

    private void SpawnEffect(PackedScene effect) {
        if (effect == null)
            return;
        var parent = GetParent<Node2D>();
        var inst = effect.Instance<Node2D>();
		GetTree().Root.AddChild(inst);
		inst.GlobalPosition = parent.GlobalPosition;
        inst.GlobalRotation = parent.GlobalRotation;
    }

	/// <summary>
	/// Description:
	/// Gives the health script more lives if the health is using lives
	/// Input:
	/// int bonusLives
	/// Return:
	/// void
	/// </summary>
	/// <param name="bonusLives">The number of lives to add</param>
	// public void AddLives(int bonusLives) {
	// 	if (useLives) {
	// 		currentLives += bonusLives;
	// 		if (currentLives > maximumLives) {
	// 			currentLives = maximumLives;
	// 		}
	// 		GameManager.UpdateUIElements();
	// 	}
	// }


	/// <summary>
	/// Description:
	/// Handles the death of the health. If a death effect is set, it is created. If lives are being used, the health is respawned.
	/// If lives are not being used or the lives are 0 then the health's game object is destroyed.
	/// Input:
	/// None
	/// Returns:
	/// void (no return)
	/// </summary>
	public void Die() {
		if (deathEffect != null)
            SpawnEffect(deathEffect);

        EmitSignal(nameof(Died));
        if (IsAlive()) {
            Hp = 0;
        }

		if (useLives) {
			if (currentLives > 0 || infiniteRespawns) {
                if (!infiniteRespawns)
        			currentLives -= 1;
	
    			if (respawnDelay == 0) {
					Respawn();
				} else {
					respawnDelayLeft = respawnDelay;
				}
			} else {
				if (respawnDelay != 0) {
					respawnDelayLeft = respawnDelay;
				} else {
                    GetParent().QueueFree();
				}
				//GameOver();
			}

		} else {
			//GameOver();
            GetParent().QueueFree();
		}
	}

	/// <summary>
	/// Description:
	/// Tries to notify the game manager that the game is over
	/// Input: 
	/// none
	/// Return: 
	/// void (no return)
	/// </summary>
	// public void GameOver() {
	// 	if (GameManager.instance != null && gameObject.tag == "Player") {
	// 		GameManager.instance.GameOver();
	// 	}
	// }

}
