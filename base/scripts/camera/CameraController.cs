using Godot;
using System;

public class CameraController : Node2D {

	[Export] public Node2D target;



	public override void _Ready() {
        
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(float delta) {
        if (target == null)
			return;
    
    }
}
