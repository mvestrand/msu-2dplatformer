using Godot;
using System;

[Tool]
public class CameraRoom : Area2D, ICameraConstraint {

	[Export] NodePath camRootPath;
	CameraRig camRoot;
	Rect2 bounds;

	[Export(PropertyHint.Range, "0,1")] float marginLeft;
	[Export(PropertyHint.Range,"0,1")] float marginTop;
	[Export(PropertyHint.Range,"0,1")] float marginBottom;
	[Export(PropertyHint.Range,"0,1")] float marginRight;

	[Export] bool clamp;

    [Export] bool ForceUpdate { get { return false; } set { UpdateBounds(); } }

	[Export] public int ConstraintPriority { get; set; }

	public override void _Ready()
    {
		camRoot = GetNodeOrNull<CameraRig>(camRootPath);
		Connect("body_entered", this, nameof(_on_body_entered));
		Connect("body_exited", this, nameof(_on_body_exited));
		UpdateBounds();
	}

	public void _on_body_entered(Node other) {
		camRoot?.AddConstraint(this);
		// if (other is PlayerController player) {
		//     if (camController != null && camController.target == node) {
		// 		camController.AddConstraint(this);
		// 	}
		// }
	}

	public void _on_body_exited(Node other) {
		camRoot?.RemoveConstraint(this);
        // if (other is Node2D node) {
    	// 	var camController = CameraController.GetCurrent(node.GetViewport());
        //     if (camController != null && camController.target == node) {
		// 		GD.Print("A");
		// 		camController.RemoveConstraint(this);
		// 	}
        // }
	}

    private bool UpdateBounds() {
		bounds = new Rect2();
		Update();
		var shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (shape == null)
			return false;
        if (shape.Shape is RectangleShape2D rect) {
            bounds.Position = shape.GlobalPosition - rect.Extents;
            bounds.Size = rect.Extents * 2;
			// GD.Print(bounds.ToString() + ", s="+shape.GlobalPosition + ", x="+rect.Extents);
			return true;
        }
		return false;
	}

    public Vector2 ApplyConstraintOnTarget(Vector2 focus, Vector2 screenDim) {
        if (UpdateBounds()) {
			Rect2 focusBounds = new Rect2(
                bounds.Position + new Vector2(marginLeft, marginTop) * screenDim,
				bounds.Size - new Vector2(marginLeft, marginTop) * screenDim - new Vector2(marginRight, marginBottom) * screenDim);

			Rect2 camCenterBounds = new Rect2(bounds.Position + screenDim / 2, bounds.Size - screenDim);
			Vector2 t = ( focus - focusBounds.Position ) / focusBounds.Size;
            if (clamp) {
				t.x = Mathf.Clamp(t.x, 0f, 1f);
				t.y = Mathf.Clamp(t.y, 0f, 1f);
			}
			return t * camCenterBounds.Size + camCenterBounds.Position;
		}
		return focus;
	}

	public override void _Draw() {
		base._Draw();
		DrawRect(bounds, Colors.Blue, false, 4, true);
	}
}
