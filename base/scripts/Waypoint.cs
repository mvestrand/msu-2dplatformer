using Godot;
using System;

public class Waypoint : Node2D
{

	[Export] public Tween.TransitionType transition = Tween.TransitionType.Linear;
    [Export] public Tween.EaseType ease = Tween.EaseType.InOut;
	[Export] public float duration = 1f;
	[Export] public float delay = 1f;
}
