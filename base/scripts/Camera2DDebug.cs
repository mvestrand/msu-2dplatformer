using Godot;
using System;

public class Camera2DDebug : Camera2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

	public override void _Process(float delta) {
		base._Process(delta);
		Update();
	}

	public override void _Draw() {
		base._Draw();
        Rect2 margins = GetViewportRect();
        margins.Size *= Zoom;
        margins.Position -= margins.Size / 2;

        // Scale rect using the current drag margins
        margins.Position *= new Vector2(DragMarginLeft, DragMarginTop);
        margins.Size = margins.Size * new Vector2(DragMarginRight, DragMarginBottom) / 2 - margins.Position;
        margins.Position += GetCameraScreenCenter() - GlobalPosition;
        DrawRect(margins, Colors.AliceBlue, false);
	}
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
