using Godot;
using System;

public class CameraRoot : Node2D {
	[Export] public NodePath targetPath;
	public Node2D target = null;
	[Export] public int lookaheadOffset = 32;
	[Export] public float dampRate = 8;

	[Export] public bool freeFollowY = false;
	[Export] NodePath camera;
	private Camera2D _cam;
	public Camera2D Cam { get { return _cam; } }
	[Export] NodePath indicatorPath;
	Node2D indicator;
	[Export] NodePath dragIndicatorPath;
	Node2D dragIndicator;

	[Export] public float limitLeft = -10000000;
	[Export] public float limitTop = -10000000;
	[Export] public float limitRight = 10000000;
	[Export] public float limitBottom = 10000000;

	public Vector2 dragOffset = Vector2.Zero;
	[Export] public Vector2 dragMargin = new Vector2(128f,64f);

    [Export] public Vector2 maxDragOffset = new Vector2(128f,64f);
	[Export] public Vector2 dragOffsetSpeed = new Vector2(128f, 0f);
	[Export] public float groundYUnlockMargin = 32f;
	[Export] public float airYUnlockMargin = 160f;
	private float lastGroundY;

	// [Export(PropertyHint.Range, "0,1")] public float dragMarginLeft = 0.2f;
	// [Export(PropertyHint.Range, "0,1")] public float dragMarginTop = 0.2f;
	// [Export(PropertyHint.Range, "0,1")] public float dragMarginRight = 0.2f;
	// [Export(PropertyHint.Range, "0,1")] public float dragMarginBottom = 0.2f;


	private bool _lockY = true;
	public bool FreeY { get { return freeFollowY || !_lockY; } }

	public override void _Ready() {
		target = GetNodeOrNull<Node2D>(targetPath);
		_cam = GetNodeOrNull<Camera2D>(camera);
		indicator = GetNodeOrNull<Node2D>(indicatorPath);
		dragIndicator = GetNodeOrNull<Node2D>(dragIndicatorPath);
	}

	public override void _Process(float delta) {
		base._Process(delta);
		Update();
	}

	public override void _PhysicsProcess(float delta) {
        if (target == null)
			return;

		Vector2 targetPosition = target.GlobalPosition;
		Vector2 predragTargetPosition = targetPosition;

		// Handle player specific camera
		if (target is PlayerController player) {
			targetPosition += new Vector2(player.Facing == Facing2D.Left ? -lookaheadOffset : lookaheadOffset, 0);

            predragTargetPosition = targetPosition;

            if (player.IsOnFloor()) {
				lastGroundY = GlobalPosition.y;
			}
            // Ignore y value when jumping
            if (!_lockY && player.IsOnFloor() && Mathf.IsEqualApprox(GlobalPosition.y, targetPosition.y, 1f)) {
				_lockY = true;
			} else if (_lockY && player.IsOnFloor() && !Mathf.IsEqualApprox(Mathf.MoveToward(targetPosition.y, GlobalPosition.y, groundYUnlockMargin), GlobalPosition.y, .1f)) {
				_lockY = false;
			} else if (_lockY && !player.IsOnFloor() && !Mathf.IsEqualApprox(Mathf.MoveToward(targetPosition.y, GlobalPosition.y, airYUnlockMargin), GlobalPosition.y, .1f)) {
 				_lockY = false;
            }
            if (!player.IsOnFloor() && !FreeY) {
				targetPosition.y = lastGroundY;
			}
			// Rect2 dragMargin = GetMarginRect(new Vector2(dragMarginLeft, dragMarginTop), new Vector2(dragMarginRight, dragMarginBottom));
			// dragMargin.Position += GlobalPosition;

            if (player.Velocity.x > 0) {
				dragOffset.x = Mathf.MoveToward(dragOffset.x, -maxDragOffset.x, dragOffsetSpeed.x * delta);
			} else if (player.Velocity.x < 0) {
				dragOffset.x = Mathf.MoveToward(dragOffset.x, maxDragOffset.x, dragOffsetSpeed.x * delta);
            }
            // if (player.Velocity.y > 0) {
			// 	dragOffset.y = Mathf.MoveToward(dragOffset.y, -maxDragOffset.y, dragOffsetSpeed.y * delta);
			// } else if (player.Velocity.y < 0) {
			// 	dragOffset.y = Mathf.MoveToward(dragOffset.y, maxDragOffset.y, dragOffsetSpeed.y * delta);
            // }

			targetPosition.x = Mathf.MoveToward(targetPosition.x, GlobalPosition.x + dragOffset.x, dragMargin.x);
            if (!FreeY) {
    			targetPosition.y = Mathf.MoveToward(targetPosition.y, GlobalPosition.y + dragOffset.y, dragMargin.y);
            }
			targetPosition -= dragOffset;
			//targetPosition -= dragOffset;

			// if (targetPosition.x >= dragMargin.Position.x && targetPosition.x <= dragMargin.End.x) {
			// 	targetPosition.x = GlobalPosition.x;
			// }
			// if (targetPosition.y >= dragMargin.Position.y && targetPosition.y <= dragMargin.End.y) {
			// 	targetPosition.y = GlobalPosition.y;
			// }
		}

		// Apply camera limits
		LimitsOrderingCheck();
		var viewSize = ViewportSize() / 2;
		targetPosition.x = Mathf.Clamp(targetPosition.x, limitLeft+viewSize.x, limitRight-viewSize.x);
		targetPosition.y = Mathf.Clamp(targetPosition.y, limitTop+viewSize.y, limitBottom-viewSize.y);


		GlobalPosition = Damp(GlobalPosition, targetPosition, dampRate, delta);
		if (indicator != null)
			indicator.GlobalPosition = targetPosition;
        if (dragIndicator != null)
            dragIndicator.GlobalPosition = predragTargetPosition;

		Update();
	}

    private void LimitsOrderingCheck() {
        if (limitTop > limitBottom)
			(limitTop, limitBottom) = (limitBottom, limitTop);
        if (limitLeft > limitRight)
			(limitLeft, limitRight) = (limitRight, limitLeft);
    }

    private static Vector2 Damp(Vector2 start, Vector2 target, float lambda, float dt) {
		return start.LinearInterpolate(target, 1 - Mathf.Exp(-lambda * dt));
	}

    // private Vector2 CanvasToGlobal(Vector2 point) {
	// 	return GetViewportTransform().Inverse() * point;
	// }

    private Vector2 ViewportSize() {
        Rect2 margins = GetViewportRect();
        margins.Size *= Cam.Zoom;
		return margins.Size;
	}

    private Rect2 GetMarginRect(Vector2 topLeft, Vector2 bottomRight) {
        Rect2 margins = GetViewportRect();
        margins.Size *= Cam.Zoom;
        margins.Position -= margins.Size / 2;

        // Scale rect using the current drag margins
        margins.Position *= topLeft;
        margins.Size = margins.Size * bottomRight / 2 - margins.Position;
		return margins;
	}

    // private bool IsInMargins(Vector2 ) {
    //     Rect2 margins = GetViewportRect();
    //     margins.Size *= Cam.Zoom;
    //     margins.Position -= margins.Size / 2;

    //     // Scale rect using the current drag margins
    //     margins.Position *= new Vector2(dragMarginLeft, dragMarginTop);
    //     margins.Size = margins.Size * new Vector2(dragMarginRight, dragMarginBottom) / 2 - margins.Position;
	// 	point.x = Mathf.Clamp(point.x, margins.Position.x, margins.End.x);
	// 	point.y = Mathf.Clamp(point.y, margins.Position.y, margins.End.y);
	// 	return point;
	// }

    public override void _Draw() {
		base._Draw();

		// Rect2 margins = GetMarginRect(new Vector2(dragMarginLeft, dragMarginTop), new Vector2(dragMarginRight, dragMarginBottom));
        DrawRect(new Rect2(dragOffset - dragMargin, dragMargin*2), (FreeY ? Colors.Green: Colors.Red), false);
	}

}
