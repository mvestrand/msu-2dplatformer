using Godot;
using System;

public partial class Pickup : Area2D {
	private bool taken = false;
	public override void _Ready() {
		Monitorable = true;
		taken = false;
	}

	public void Take(Node taker) {
		if (taken)
			return;
		if (TryToPickup(taker)) {
			OnPickup();
		}
	}

	protected virtual bool TryToPickup(Node taker) { return true; }

	private void OnPickup() {
		taken = true;
		var animator = GetNode<AnimationPlayer>("AnimationPlayer");
		animator?.Play("pickup");
		SetDeferred("monitorable", false);
	}
}
