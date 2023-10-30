extends Area2D

# Declare member variables here. Examples:
# var a = 2
# var b = "text"


var active: bool = false;

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
#	pass

func SetRespawnPoint(body: Node) -> bool:
	if body.is_in_group("player"):
		var health := body.get_node("Health")
		if !health: # Guard against misnamed Health component
			return false
		health.SetRespawnNode(self)
		return true
	return false

func Activate() -> void:
	if not active:
		active = true
		$AnimationPlayer.play("activate")


func _on_Checkpoint_body_entered(body: Node):
	if body.is_in_group("player"):
		if not active: # Only activate on first player contact
			if SetRespawnPoint(body):
				Activate()

