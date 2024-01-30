using Godot;
using System;

public static class Math {
    public static Vector2 Damp(Vector2 start, Vector2 target, float lambda, float dt) {
		return start.LinearInterpolate(target, 1 - Mathf.Exp(-lambda * dt));
	}
}
