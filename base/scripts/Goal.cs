using Godot;
using System;

public partial class Goal : Area2D {
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    [Export] public PackedScene nextLevel;
    [Export] public PackedScene winScreen;

    private bool hasWon = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        hasWon = false;
        Connect("area_entered", this, nameof(_on_area_entered));
		Connect("body_entered", this, nameof(_on_body_entered));

    }


    public void ShowWinScreen() {
		hasWon = true;
		GetTree().Paused = true;
		LevelClear inst = (LevelClear)winScreen.Instance();
		inst.nextLevel = nextLevel;
		GetTree().Root.AddChild(inst);
	}

    
	public void _on_area_entered(Area2D other) {
		// Victory
        if (!hasWon) {
			ShowWinScreen();
            hasWon = true;
        }
	}

	public void _on_body_entered(Node other) {
		// Victory
		if (!hasWon) {
            ShowWinScreen();
            hasWon = true;
        }
	}
}
