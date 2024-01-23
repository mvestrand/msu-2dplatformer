using Godot;
using System;

public class PickupTileMap : TileMap
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	[Export] PackedScene scorePickup;

	public enum PickupType {
		Empty = -1,
        Score = 0
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		foreach (Vector2 cellpos in GetUsedCells()) {
			var cell = GetCellv(cellpos);
            if (cell == (int)PickupType.Score) {
				var pickup = scorePickup.Instance<Node2D>();
				pickup.Position = MapToWorld(cellpos) + new Vector2(8,8); // * Scale?
				AddChild(pickup);
				SetCellv(cellpos, (int)PickupType.Empty);
			}
		}
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
