using Godot;
using System;

public class MovingPlatform : Node2D
{
    public enum WrapMode {
        Clamp,
        Loop,
        InstantLoop,
        PingPong
    }


	[Export] public bool enabled = true;
	[Export] NodePath waypointsPath;
	[Export] int startingIndex = 0;

	[Export] WrapMode wrap = WrapMode.PingPong;
	[Export] public bool reversed = false;
	[Export] public float moveSpeed = 100;
	[Export] public int usedWaypoints = 5;

	Node2D waypoints;
	float waitTimeLeft;
	bool waiting;
	Waypoint target;

    public int MaxWaypoints {
		get { return Mathf.Min(usedWaypoints, waypoints.GetChildCount()); }
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		waypoints = GetNodeOrNull<Node2D>(waypointsPath);
		target = waypoints.GetChildOrNull<Waypoint>(startingIndex);
		ArriveAtTargetWaypoint();
	}

    private Waypoint GetNextWaypoint(out bool wasWrapped) {

        // Return the starting target if the current target is invalid
        if (target == null || target.GetParentOrNull<Node2D>() != waypoints) {
			wasWrapped = false;
			return waypoints.GetChild<Waypoint>(Mathf.PosMod(startingIndex, MaxWaypoints));
		}

		int nextIndex = GetNextWrappedIndex(wrap, target.GetIndex(), MaxWaypoints, !reversed, out wasWrapped);
		return waypoints.GetChild<Waypoint>(nextIndex);
	}

    /// <summary>
    /// Gets the next index after the given index, using a given wrapping mode.
    /// </summary>
    /// <param name="wrap"> The wrap mode to use. </param>
    /// <param name="current"> The index to start at. This must be between 0 and count. </param>
    /// <param name="count"> The maximum index value; exclusive. </param>
    /// <param name="forward"> True if iterating forward through the indices, false if iterating backwards. </param>
    /// <param name="wasWrapped"> Set to true if the index was wrapped, including when it is clamped; otherwise false. </param>
    /// <returns> The wrapped value of the next index. </returns>
    public static int GetNextWrappedIndex(WrapMode wrap, int current, int count, bool forward, out bool wasWrapped) {

        int next = current + (forward ? 1 : -1);
		wasWrapped = next < 0 || next >= count;

		switch (wrap) {
			default:
            case WrapMode.Clamp:
				return Mathf.Clamp(next, 0, count-1); // Mathf.Clamp is exclusive of max value

			case WrapMode.InstantLoop:
            case WrapMode.Loop:
				return Mathf.PosMod(next, count); // Mathf.PosMod is exclusive of max value

            case WrapMode.PingPong:
				return next + ( wasWrapped ? ( next < 0 ? +2 : -2 ) : 0 ); // Shift next back into bounds if it left them
		}
    }

    public void SetTargetToNextWaypoint(bool instant = false) {
        // Handle invalid
        if (waypoints == null) {
			GD.PrintErr($"Moving platform \"{GetPath()}\" has invalid waypoint list \"{waypointsPath}\"");
			enabled = false;
			return;
        }
        if (MaxWaypoints <= 1) {
			GD.PrintErr($"Moving platform \"{GetPath()}\" has waypoint list \"{waypoints.GetPath()}\" with fewer than 2 Waypoint child nodes");
			enabled = false;
			return;
		}

		Waypoint next = GetNextWaypoint(out bool wasWrapped);
        
        // Handle wrap mode specific adjustments needed when being wrapped
        if (wasWrapped) {
            if (wrap == WrapMode.InstantLoop)
				instant = true;
            else if (wrap == WrapMode.PingPong)
				reversed = !reversed;
		}

		SetTarget(next, instant);
	}

    public void SetTarget(Waypoint waypoint, bool instant=false) {
		target = waypoint;
		waiting = false;
        if (instant)
			ArriveAtTargetWaypoint();
	}

    public void ArriveAtTargetWaypoint() {
        if (target == null)
			return;

		GlobalPosition = target.GlobalPosition;

		if (target.delay > 0) { 
    		waiting = true;
			waitTimeLeft = target.delay;
		} else {
			waiting = false;
			SetTargetToNextWaypoint();
		}
	}

	public override void _PhysicsProcess(float delta) {
        if (!enabled || target == null) {
			return;
		}
        if (waiting) {
			waitTimeLeft -= delta;
            if (waitTimeLeft <= 0) {
				waiting = false;
				SetTargetToNextWaypoint();
			}
		} else {
            if (GlobalPosition != target.GlobalPosition) {
				GlobalPosition = GlobalPosition.MoveToward(target.GlobalPosition, moveSpeed * delta);
			} else {
				ArriveAtTargetWaypoint();
			}

        }
	}
}
