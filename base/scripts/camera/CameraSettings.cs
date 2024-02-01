using Godot;
using System;

public class CameraSettings : Resource {

	[Export] public NodePath targetPath;
	[Export] public int lookaheadOffset = 32;
	[Export] public float dampRate = 8;

	[Export] public Vector2 limitTopLeft = new Vector2(-1000000,-1000000);
	[Export] public Vector2 limitBottomRight = new Vector2(1000000,1000000);

	[Export] public Vector2 dragMargin = new Vector2(128f,64f);
    [Export] public Vector2 maxDragOffset = new Vector2(128f,64f);
	[Export] public Vector2 dragOffsetSpeed = new Vector2(128f, 0f);
	[Export] public float airYUnlockMargin = 160f;
	[Export] public bool alwaysFreeFollow = false;

    public void CopyTo(CameraSettings settings) {
		settings.targetPath = targetPath;
		settings.lookaheadOffset = lookaheadOffset;
		settings.dampRate = dampRate;
		settings.limitTopLeft = limitTopLeft;
		settings.limitBottomRight = limitBottomRight;
		settings.dragMargin = dragMargin;
		settings.maxDragOffset = maxDragOffset;
		settings.dragOffsetSpeed = dragOffsetSpeed;
		settings.airYUnlockMargin = airYUnlockMargin;
		settings.alwaysFreeFollow = alwaysFreeFollow;
	}
    
}
