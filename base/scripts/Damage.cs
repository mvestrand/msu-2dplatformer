using System.Collections;
using System.Collections.Generic;
using Godot;

/// <summary>
/// This class handles the dealing of damage to health components.
/// </summary>
public partial class Damage : Area2D {
	[Export] public int teamId = 0;

	[Export] public int damageAmount = 1;
	[Export] public bool destroyAfterDamage = false;
    [Export] public bool dealDamageOnEnter = true;
	[Export] public bool dealDamageOnStay = false;

    [Signal] public delegate void DamageDealt(Node target, TakeDamageOutcome outcome);


    public override void _Ready() {
        if (dealDamageOnEnter) {
            Connect("area_entered", this, nameof(_on_area_entered));
            Connect("body_entered", this, nameof(_on_body_entered));
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (dealDamageOnStay) {
            HitArea();
        }

    }

    public void HitArea() {
        var areas = GetOverlappingAreas();
        foreach (Area2D area in areas) {
            if (area.GetNode<IHealth>("Health") is IHealth health) {
                DealDamage(health);
            }
        }
        var bodies = GetOverlappingBodies();
        foreach (Node body in bodies) {
            if (body.GetNode<IHealth>("Health") is IHealth health) {
                DealDamage(health);
            }
        }
    }

    public void _on_area_entered(Area2D other) {
        if (other.GetNode<IHealth>("Health") is IHealth health) {
            DealDamage(health);
        }
    }

    public void _on_body_entered(Node other) {
        if (other.GetNode<IHealth>("Health") is IHealth health) {
            DealDamage(health);
        }
    }

	/// <summary>
	/// Description:
	/// This function deals damage to a health component 
	/// if the collided with gameobject has a health component attached AND it is on a different team.
	/// Input:
	/// GameObject collisionGameObject
	/// Return:
	/// void (no return)
	/// </summary>
	/// <param name="collisionGameObject">The game object that has been collided with</param>
	private void DealDamage(IHealth target) {
		if (target.TeamId != teamId) {
            var outcome = target.TakeDamage(damageAmount);
            if (outcome != TakeDamageOutcome.Ignored) {
                GD.Print("dmg dealt");
                EmitSignal(nameof(DamageDealt), target as Node, outcome);
            }
            if (outcome != TakeDamageOutcome.Ignored && destroyAfterDamage) {
                Owner.QueueFree();
            }
		}
	}
}
