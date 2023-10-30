using Godot;
using System;

public class PickupZone : Area2D {
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Connect("area_entered", this, nameof(_on_area_entered));
	}

	public void _on_area_entered(Area2D area) {
		GD.Print("pickup");
		if (area is Pickup pickup) {
			pickup.Take(Owner);
		}
	}
}
