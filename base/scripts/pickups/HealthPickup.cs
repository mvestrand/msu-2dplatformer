using Godot;
using System;

public partial class HealthPickup : Pickup {

	[Export] public int healAmount = 5;

	protected override bool TryToPickup(Node taker) {
		if (taker != null && taker.IsInGroup("player") && taker.GetNode("Health") is IHealth health) {
			return health.ReceiveHealing(healAmount);
		}
		return false;
	}

}
