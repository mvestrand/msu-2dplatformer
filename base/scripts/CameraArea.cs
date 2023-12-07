using Godot;
using System;

public class CameraArea : Area2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	// [Export] Rect2 bounds;


	// // Called when the node enters the scene tree for the first time.
	// public override void _Ready()
    // {
	// 	Connect("body_entered", this, nameof(_on_body_entered));
    // }

	// public void _on_body_entered(Node other) {
    //     if (other is Node2D node) {
    // 		var camController = CameraController.GetCurrent(node.GetViewport());
    //         if (camController != null && camController.target == node) {
	// 			Apply(camController);
	// 		}
    //     }
	// }

    // public void Apply(CameraController camera) {
	// 	camera.LimitLeft = (int)(bounds.Abs().Position.x + GlobalPosition.x);
	// 	camera.LimitTop = (int)(bounds.Abs().Position.y + GlobalPosition.y);

	// 	camera.LimitRight = (int)(bounds.Abs().End.x + GlobalPosition.x);
	// 	camera.LimitBottom = (int)(bounds.Abs().End.y + GlobalPosition.y);

	// 	camera.GlobalPosition = GlobalPosition;
	// }


//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
