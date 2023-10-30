using System;
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
	[Export] public bool tryUseSubpixelCamera = false;

	private bool useSubpixelCamera = false;
	private Vector2 actualPosition = Vector2.Zero;

	private WeakReference<ShaderMaterial> subpixelShaderMat;


	public override void _Ready() {
		target = GetNode<Node2D>(targetPath);
		if (tryUseSubpixelCamera)
			SetupSubpixelCamera();
	}

	public override void _PhysicsProcess(float delta) {
		SetCameraPosition(delta);
	}

	public override void _Process(float delta) {
		SetCameraPosition(delta);
	}

	private void SetupSubpixelCamera() {
		useSubpixelCamera = false;
		var vpContainer = GetViewport().GetParentOrNull<ViewportContainer>();
		if (vpContainer == null) {
			GD.PushWarning("Subpixel camera setup failed: Viewport \"" + GetViewport().Name + "\" has no parent. To use the subpixel camera it must be in a ViewportContainer with a shader material using GameWindow.gdshader");
			return;
		}
		var mat = vpContainer.Material as ShaderMaterial;
		if (mat == null) {
			GD.PushWarning("Subpixel camera setup failed: Viewport \"" + vpContainer.Name + "\" does not use a ShaderMaterial. To use the subpixel camera the Viewport must be in a ViewportContainer with a shader material using GameWindow.gdshader");
			return;
		}
		if (mat.GetShaderParam("cam_offset") == null) {
			GD.PushWarning("Subpixel camera setup failed: Failed to find shader param \"cam_offset\" in the material of \"" + vpContainer.Name + "\". To use the subpixel camera the Viewport must be in a ViewportContainer with a shader material using GameWindow.gdshader");
			return;
		}
		// Found 
		subpixelShaderMat = new WeakReference<ShaderMaterial>(mat);
		useSubpixelCamera = true;

	}

	public Vector2 GetPos() {
		if (useSubpixelCamera)
			return actualPosition;
		else
			return GlobalPosition;
	}

	public void SetPos(Vector2 pos) {
		if (useSubpixelCamera) {
			actualPosition = pos;
			GlobalPosition = pos.Round();
			var subpixelPosition = GlobalPosition - actualPosition;
			// GD.Print(actualPosition, GlobalPosition, subpixelPosition);
			if (subpixelShaderMat.TryGetTarget(out var mat)) {
				mat.SetShaderParam("cam_offset", subpixelPosition);
			}
		} else {
			GlobalPosition = pos;
		}
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

			SetPos(desiredCameraPosition);
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
		return GetPos();
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
				result = GetPos();
				break;
			case CameraStyles.Overhead:
				result = targetPosition;
				break;
			case CameraStyles.DistanceFollow:
				result = targetPosition + ( GetPos() - targetPosition ).LimitLength(maxDistanceFromTarget);
				break;
			case CameraStyles.OffsetFollow:
				result = targetPosition + cameraOffset;
				break;
			case CameraStyles.LerpFollow:
				result = GetPos().LinearInterpolate(targetPosition, camFollowSpeed * delta);
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
