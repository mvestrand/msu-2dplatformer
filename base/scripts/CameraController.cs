using System.Collections;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Class which handles camera movement
/// </summary>
public partial class CameraController : Camera2D {
    [Export] public NodePath targetPath;
	public Node2D target = null;

	/// <summary>
	/// Enum to determine camera movement styles
	/// </summary>
	public enum CameraStyles {
		Locked,
		Overhead,
		DistanceFollow,
		OffsetFollow,
        LerpFollow,
		BetweenTargetAndMouse
	}

	[Export] public CameraStyles cameraMovementStyle = CameraStyles.Locked;
	[Export] public float maxDistanceFromTarget = 100.0f;
	[Export] public Vector2 cameraOffset = Vector2.Zero;
	[Export] public float mouseTracking = 0.5f;
    [Export] public float camFollowSpeed = 1f;

	public override void _Ready() {
        target = GetNode<Node2D>(targetPath);
	}


    public override void _PhysicsProcess(float delta) {
        SetCameraPosition(delta);
    }

    public override void _Process(float delta) {
        SetCameraPosition(delta);
    }

    /// <summary>
    /// Description:
    /// Sets the camera's position according to the settings
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void SetCameraPosition(float delta) {
		if (target != null) {
			Vector2 targetPosition = GetTargetPosition();
			Vector2 mousePosition = GetPlayerMousePosition();
			Vector2 desiredCameraPosition = ComputeCameraPosition(targetPosition, mousePosition, delta);

			GlobalPosition = desiredCameraPosition;
		}
	}

	/// <summary>
	/// Description:
	/// Gets the follow target's position
	/// Input: 
	/// none
	/// Returns: 
	/// Vector3
	/// </summary>
	/// <returns>Vector3: The position of the target assigned to this camera controller.</returns>
	public Vector2 GetTargetPosition() {
		if (target != null) {
			return target.GlobalPosition;
		}
		return GlobalPosition;
	}

	/// <summary>
	/// Description:
	/// Finds and returns the mouse position
	/// Input: 
	/// none
	/// Returns: 
	/// Vector3
	/// </summary>
	/// <returns>Vector3: The position of the player's mouse in world coordinates</returns>
	public Vector2 GetPlayerMousePosition() {
        return GetGlobalMousePosition();
	}

	/// <summary>
	/// Description:
	/// Takes the target's position and mouse position, and returns the desired position of the camera
	/// Input: 
	/// Vector3 targetPosition, Vector3 offsetPosition
	/// Returns:
	/// Vector3
	/// </summary>
	/// <param name="targetPosition"> The position of the target the camera is following. </param>
	/// <param name="mousePosition"> The position of the mouse in world space used to determine distance from the target. </param>
	/// <returns>Vector3: The position the camera should be at</returns>
	public Vector2 ComputeCameraPosition(Vector2 targetPosition, Vector2 mousePosition, float delta) {
		Vector2 result = Vector2.Zero;
		switch (cameraMovementStyle) {
			case CameraStyles.Locked:
				result = GlobalPosition;
				break;
			case CameraStyles.Overhead:
				result = targetPosition;
				break;
			case CameraStyles.DistanceFollow:
				result = targetPosition + (GlobalPosition - targetPosition).LimitLength(maxDistanceFromTarget);
				break;
			case CameraStyles.OffsetFollow:
				result = targetPosition + cameraOffset;
				break;
            case CameraStyles.LerpFollow:
                result = GlobalPosition.LinearInterpolate(targetPosition, camFollowSpeed*delta);
                break;
			case CameraStyles.BetweenTargetAndMouse:
				Vector2 desiredPosition = targetPosition.LinearInterpolate(mousePosition, mouseTracking);
				Vector2 difference = desiredPosition - targetPosition;
				difference = difference.LimitLength(maxDistanceFromTarget);
				result = targetPosition + difference;
				break;
		}
		return result;
	}
}
