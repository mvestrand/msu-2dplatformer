using Godot;
using System;

public static class Math {

    public static Vector2 Min(Vector2 a, Vector2 b) {
		return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
	}

    public static Vector2 Max(Vector2 a, Vector2 b) {
		return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
	}

    public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) {
		return new Vector2(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
	}

    public static Vector2 Clamp(Vector2 value, Rect2 bounds) {
		return Clamp(value, bounds.Position, bounds.End);
	}

    public static Vector2 Clamp(ref this Rect2 bounds, Vector2 value) {
		return Clamp(value, bounds.Position, bounds.End);
	}

    public static Vector2 MoveToward(this Vector2 from, Rect2 to, float delta) {
        if (to.HasPoint(from))
			return from;
		return from.MoveToward(Clamp(from, to.Position, to.End), delta);
	}

    public static Vector2 MoveToward(this Vector2 from, Rect2 to, Vector2 delta) {
        if (to.HasPoint(from))
			return from;
		return new Vector2(
			Mathf.MoveToward(from.x, Mathf.Clamp(from.x, to.Position.x, to.End.x), delta.x),
			Mathf.MoveToward(from.y, Mathf.Clamp(from.y, to.Position.y, to.End.y), delta.y));
	}

    public static Vector2 Damp(Vector2 start, Vector2 target, float lambda, float dt) {
		return start.LinearInterpolate(target, 1 - Mathf.Exp(-lambda * dt));
	}
}
