using Godot;
using System;

public class LevelClear : CanvasLayer
{

	public PackedScene nextLevel;
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
	}


	public void GoToNextLevel() {
		GetTree().Paused = false;
        GetTree().ChangeSceneTo(nextLevel);
		QueueFree();
    }
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
