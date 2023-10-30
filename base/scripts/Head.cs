using Godot;


public partial class Head : Area2D {

	[Export] public NodePath healthPath;
	public IHealth health;
	[Export] public int damage = 1;

	public override void _Ready() {
		Connect("area_entered", this, nameof(_on_area_entered));
		Connect("body_entered", this, nameof(_on_body_entered));
		health = GetNode(healthPath) as IHealth;
	}

	public void _on_area_entered(Area2D other) {
		HandleCollision(other);
	}

	public void _on_body_entered(Node other) {
		HandleCollision(other);
	}

	private void HandleCollision(Node other) {
		if (!health.IsAlive())
			return;
		if (other.IsInGroup("feet")) {
			var player = other.Owner as PlayerController;
			player.Bounce();
			health.TakeDamage(damage);
		}
	}


}
