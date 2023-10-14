using Godot;
using System;

public partial class ScorePickup : Pickup {

    protected override bool TryToPickup(Node taker) {
        if (taker != null && taker.IsInGroup("player")) {

            // ADD SCORE HERE

            return true;
        }
        return false;
    }

}
