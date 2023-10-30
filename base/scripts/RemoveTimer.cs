using Godot;
using System;

public class RemoveTimer : Timer {
	[Export] public float Lifetime = 1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (!IsConnected("timeout", this, nameof(_on_timeout)))
			Connect("timeout", this, nameof(_on_timeout));
		if (Paused)
			Stop();
		Start(Lifetime);
	}

	private void _on_timeout() {
		var parent = GetParent();
		parent?.QueueFree();
	}

	//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	//  public override void _Process(float delta)
	//  {
	//      
	//  }
}
