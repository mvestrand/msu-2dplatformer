using Godot;
using System;

public enum DoorState {
	Null,
	Locked,
	Closed,
	Open
}

public partial class Door : KinematicBody2D {
	[Export] private bool _keepOpen = false;
	public bool KeepOpen {
		get { return _keepOpen; }
		set { _keepOpen = value; UpdatePhysicsProcessToggle(); }
	}
	private AnimationTree animator;
	[Export] private DoorState _targetState = DoorState.Locked;
	public DoorState TargetState { get { return _targetState; } set { _targetState = value; UpdateAnimator(); UpdatePhysicsProcessToggle(); } }
	[Export] public string keyID;
	[Export] public DoorState overriddenState = DoorState.Null;

	[Export] public PackedScene unlockSuccessEffect;
	[Export] public PackedScene unlockFailEffect;

	private Area2D holdZone;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		animator = GetNodeOrNull<AnimationTree>("AnimationTree");
		holdZone = GetNodeOrNull<Area2D>("HoldZone");
		//activationZone.Connect("body_entered", this, nameof(OnActivationBodyEnter));
		UpdateAnimator();
	}

	public override void _PhysicsProcess(float delta) {
		// Check if the door should close
		if (holdZone != null && !AreaHasOverlaps(holdZone) && !KeepOpen) {
			TryClose();
		}
	}

	private bool AreaHasOverlaps(Area2D area) {
		return area.GetOverlappingAreas().Count > 0 || area.GetOverlappingBodies().Count > 0;
	}

	public void OnActivationBodyEnter(Node other) {
		GD.Print("door->body");
		if (TargetState != DoorState.Open) { // Open the door if it isn't already open
			var keys = other.GetNodeOrNull<KeyRing>("KeyRing");
			TryOpen(keys);
		}
	}

	public void OnActivationAreaEnter(Area2D area) {
		GD.Print("door->area");
		if (TargetState != DoorState.Open) { // Open the door if it isn't already open
			var keys = area?.Owner?.GetNodeOrNull<KeyRing>("KeyRing");
			TryOpen(keys);
		}
	}

	public bool CanOpen(KeyRing keys = null) { return ( TargetState == DoorState.Closed && !HasOverride() ) || CanUnlock(keys); }

	public bool TryOpen(KeyRing keys = null) {
		if (TargetState == DoorState.Locked) {
			// Try to unlock
			if (keys != null) {
				if (TryUnlock(keys, open: true)) {
					SpawnEffect(unlockSuccessEffect);
					return true;
				} else {
					SpawnEffect(unlockFailEffect);
				}
			}
		} else if (TargetState == DoorState.Closed && !HasOverride()) {
			TargetState = DoorState.Open;
			return true;
		}
		return false;
	}


	public bool CanClose() {
		return TargetState == DoorState.Open && !HasOverride();
	}
	public bool TryClose() {
		if (CanClose()) {
			TargetState = DoorState.Closed;
			return true;
		}
		return false;
	}

	public bool CanUnlock(KeyRing keys) {
		return TargetState == DoorState.Locked && !HasOverride() && keys != null && keys.HasKey(keyID);
	}

	public bool TryUnlock(KeyRing keys, bool open = true) {
		if (CanUnlock(keys)) {
			TargetState = ( open ? DoorState.Open : DoorState.Closed );
			return true;
		}
		return false;
	}

	public bool HasOverride() { return overriddenState != DoorState.Null; }

	public void Override(DoorState forcedState = DoorState.Locked) {
		if (!HasOverride())
			overriddenState = TargetState;
		TargetState = forcedState;
	}

	public void OverrideRelease() {
		if (HasOverride()) {
			var tmp = overriddenState;
			overriddenState = DoorState.Null;
			TargetState = tmp;
		}
	}

	private void UpdateAnimator() {
		if (animator == null)
			return;
		animator.Set("parameters/conditions/open", TargetState == DoorState.Open);
		animator.Set("parameters/conditions/closed", TargetState != DoorState.Open);
		animator.Set("parameters/conditions/locked", TargetState == DoorState.Locked);
		animator.Set("parameters/conditions/unlocked", TargetState != DoorState.Locked);
	}

	private void UpdatePhysicsProcessToggle() {
		SetPhysicsProcess(!_keepOpen && TargetState == DoorState.Open && !HasOverride()); // Start checking activation area if open and not being kept open        
	}

	private void SpawnEffect(PackedScene effect) {
		if (effect == null)
			return;
		var parent = GetParent<Node2D>();
		var inst = effect.Instance<Node2D>();
		GetViewport().AddChild(inst);
		inst.GlobalPosition = parent.GlobalPosition;
		inst.GlobalRotation = parent.GlobalRotation;
	}

}
