using Godot;
using System;

public class CameraArea : Area2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	// [Export] Rect2 bounds;

	[Export] Rect2 bounds;
	[Export] bool xMinBound = false;
	[Export] bool yMinBound = false;
	[Export] bool xMaxBound = false;
	[Export] bool yMaxBound = false;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		Connect("body_entered", this, nameof(_on_body_entered));
		Connect("body_exited", this, nameof(_on_body_exited));
    }

	public void _on_body_entered(Node other) {
		// GD.Print(this.GetPath().ToString() + ": entered by " + other.GetPath());
		if (other is Node2D node) {
    		var camController = CameraController.GetCurrent(node.GetViewport());
            if (camController != null && camController.target == node) {
				camController.AddConstraint(this);
			}
        }
	}

	public void _on_body_exited(Node other) {
		// GD.Print(this.GetPath().ToString() + ": exited by " + other.GetPath());
        if (other is Node2D node) {
    		var camController = CameraController.GetCurrent(node.GetViewport());
            if (camController != null && camController.target == node) {
				// GD.Print("A");
				camController.RemoveConstraint(this);
			}
        }
	}

    public Vector2 ApplyConstraint(Vector2 camPos) {
    	var camController = CameraController.GetCurrent(GetViewport());
		var viewSize = camController.Zoom * (GetViewportRect().Size / 2);
		if (xMinBound) {
			camPos.x = Mathf.Max(camPos.x, bounds.Position.x + viewSize.x);
		}
		if (xMaxBound) {
			camPos.x = Mathf.Min(camPos.x, bounds.End.x - viewSize.x);
		}
		if (yMinBound) {
			camPos.y = Mathf.Max(camPos.y, bounds.Position.y + viewSize.y);
		}
		if (yMaxBound) {
			camPos.y = Mathf.Min(camPos.y, bounds.End.y - viewSize.y);
		}

		return camPos;
	}


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
