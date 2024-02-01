using Godot;
using System.Collections.Generic;
using System;
using System.Data;
using System.Collections.Specialized;

public interface ICameraConstraint {
	int ConstraintPriority { get; }
	Vector2 ApplyConstraintOnTarget(Vector2 focus, Vector2 screenSize);
	// void ApplyCameraSettings(CameraSettings settings);
	void ApplyCameraSettings(ref CameraSettings settings);
}

public class CameraRig : Node2D {

	[Export] private CameraSettings baseSettings = new CameraSettings();
	private CameraSettings settings = new CameraSettings();
	private Node2D target;


	[Export] NodePath camPath;
	private Camera2D cam;
	public Camera2D Cam { get { return cam; } }

	private Vector2 dragOffset = Vector2.Zero;
	private bool lockToGround = true;
	private float lastGroundY;

	bool _debugDraw = false;
    [Export] public bool DebugDraw { get { return _debugDraw; } set { _debugDraw = value; DebugUpdateVisibility(); } }


	#region Constraints
	private ICameraConstraint activeConstraint;
	private List<ICameraConstraint> inactiveConstraints = new List<ICameraConstraint>();

	public void AddConstraint(ICameraConstraint constraint) {
        
        if (activeConstraint == null) {
			SetActiveConstraint(constraint);
		} else if (constraint.ConstraintPriority > activeConstraint.ConstraintPriority) {
			inactiveConstraints.Add(activeConstraint);
			SetActiveConstraint(constraint);
		} else {
			inactiveConstraints.Add(constraint);
		}
    }

    public void RemoveConstraint(ICameraConstraint constraint) {
        if (activeConstraint == constraint) {
			SetActiveConstraint(PopNextConstraint());
		} else {
			inactiveConstraints.Remove(constraint);
		}
    }

    private ICameraConstraint PopNextConstraint() {
        if (inactiveConstraints.Count == 0)
			return null;
		ICameraConstraint best = inactiveConstraints[0];
        foreach (ICameraConstraint con in inactiveConstraints) {
            if (con.ConstraintPriority > best.ConstraintPriority) {
				best = con;
			}
        }
		inactiveConstraints.Remove(best);
		return best;
	}

    private void SetActiveConstraint(ICameraConstraint constraint) {
        if (constraint == activeConstraint)
			return;
        baseSettings.CopyTo(settings);
        constraint?.ApplyCameraSettings(ref settings);
        activeConstraint = constraint;
        if (target.GetPath() != settings.targetPath)
    		target = GetNodeOrNull<Node2D>(settings.targetPath);
	}

    #endregion

	public override void _Ready() {

        baseSettings.CopyTo(settings);
		target = GetNodeOrNull<Node2D>(settings.targetPath);
		cam = GetNodeOrNull<Camera2D>(camPath);
		SetupDebugDrawing();
	}

	public override void _Process(float delta) {
		base._Process(delta);
		if (_debugDraw)
            Update();
	}

	public override void _PhysicsProcess(float delta) {
		if (target == null)
			return;

		Vector2 targetPos = target.GlobalPosition;
		Vector2 initTargetPos = targetPos;
		Vector2 afterGroundLock = targetPos;

		// Handle player specific camera
		if (target is PlayerController player) {
			targetPos += new Vector2(player.Facing == Facing2D.Left ? -settings.lookaheadOffset : settings.lookaheadOffset, 0);
			initTargetPos = targetPos;



			// Ignore y value when jumping
			targetPos = ApplyGroundLock(targetPos, player.IsOnFloor());
			afterGroundLock = targetPos;

			UpdateDragMargins(player.Velocity, delta);

			// Apply margins
			targetPos.x = Mathf.MoveToward(targetPos.x, GlobalPosition.x + dragOffset.x, dragMargin.x) - dragOffset.x;
			// if (!lockToGround) {
			targetPos.y = Mathf.MoveToward(targetPos.y, GlobalPosition.y + dragOffset.y, dragMargin.y) - dragOffset.y;
			// }
		}

		Vector2 beforeConstraints = targetPos;
		targetPos = ApplyConstraints(targetPos);

		GlobalPosition = Math.Damp(GlobalPosition, targetPos, dampRate, delta);
		DebugUpdateState(targetPos, initTargetPos, afterGroundLock, beforeConstraints);
	}

    // private bool IgnoreYDragMargins() {
	// 	return yLockState == CameraGroundLock.SeekGround && !AlwaysFreeFollow;
	// }


    private void UpdateDragMargins(Vector2 playerVel, float dt) {
        if (playerVel.x > 0) {
				dragOffset.x = Mathf.MoveToward(dragOffset.x, -maxDragOffset.x, dragOffsetSpeed.x * dt);
        } else if (playerVel.x < 0) {
				dragOffset.x = Mathf.MoveToward(dragOffset.x, maxDragOffset.x, dragOffsetSpeed.x * dt);
        }
    }

	private Vector2 ApplyGroundLock(Vector2 targetPos, bool grounded) {
        if (AlwaysFreeFollow)
			return targetPos;

		if (grounded) { // On ground
			lastGroundY = targetPos.y;
			lockToGround = true;
		} else if (lockToGround) { // Midair but still locked
            if (Mathf.Abs(targetPos.y - GlobalPosition.y) > airYUnlockMargin) // Is past unlock margin
				lockToGround = false;
			else // Locked and still within lock margins
				targetPos.y = lastGroundY;
		}
		return targetPos;
	}
    
    private bool IsBeyondMargin(float src, float dest, float margin) {
		return Mathf.Abs(dest - src) > margin;
	}

	private Vector2 ApplyConstraints(Vector2 targetPosition) {
		if (activeConstraint != null) {
			targetPosition = activeConstraint.ApplyConstraintOnTarget(targetPosition, GetViewportRect().Size * Cam.Zoom);
		}

		return targetPosition;
	}

	#region Limits
	[Export] public float limitLeft = -10000000;
	[Export] public float limitTop = -10000000;
	[Export] public float limitRight = 10000000;
	[Export] public float limitBottom = 10000000;

    private Vector2 ApplyLimits(Vector2 target) {
		LimitsOrderingCheck();
		var viewSize = ViewportSize() / 2;
		target.x = Mathf.Clamp(target.x, limitLeft+viewSize.x, limitRight-viewSize.x);
		target.y = Mathf.Clamp(target.y, limitTop+viewSize.y, limitBottom-viewSize.y);
        return target;
    }


    private void LimitsOrderingCheck() {
        if (limitTop > limitBottom)
			(limitTop, limitBottom) = (limitBottom, limitTop);
        if (limitLeft > limitRight)
			(limitLeft, limitRight) = (limitRight, limitLeft);
    }

    #endregion


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

    #region DebugDrawing
	[Export] NodePath indicatorPath;
	Node2D finalIndicator;
	[Export] NodePath dragIndicatorPath;
	Node2D initIndicator;
	[Export] NodePath groundLockPath;
	Node2D groundLockIndicator;
	[Export] NodePath preConstrainPath;
	Node2D preConstrainIndicator;

    private void SetupDebugDrawing() {
		finalIndicator = GetNodeOrNull<Node2D>(indicatorPath);
		initIndicator = GetNodeOrNull<Node2D>(dragIndicatorPath);
		groundLockIndicator = GetNodeOrNull<Node2D>(groundLockPath);
		preConstrainIndicator = GetNodeOrNull<Node2D>(preConstrainPath);
		DebugUpdateVisibility();
    }

    private void DebugUpdateVisibility() {
        if (finalIndicator != null)
            finalIndicator.Visible = _debugDraw;
        if (initIndicator != null)
            initIndicator.Visible = _debugDraw;
        if (groundLockIndicator != null)
            groundLockIndicator.Visible = _debugDraw;
        if (preConstrainIndicator != null)
            preConstrainIndicator.Visible = _debugDraw;
		Update();
	}

    private void DebugUpdateState(Vector2 finalPos, Vector2 initPos, Vector2 groundLockPos, Vector2 preConstrainPos) {
        if (!_debugDraw)
			return;
		if (finalIndicator != null)
			finalIndicator.GlobalPosition = finalPos;
        if (initIndicator != null)
            initIndicator.GlobalPosition = initPos;
        if (groundLockIndicator != null)
			groundLockIndicator.GlobalPosition = groundLockPos;
        if (preConstrainIndicator != null)
			preConstrainIndicator.GlobalPosition = preConstrainPos;
		Update();
    }

    public override void _Draw() {
		base._Draw();
        if (_debugDraw)
            DrawRect(new Rect2(dragOffset - dragMargin, dragMargin*2), (lockToGround ? Colors.Red : Colors.Green), false);
	}
    #endregion 

}

