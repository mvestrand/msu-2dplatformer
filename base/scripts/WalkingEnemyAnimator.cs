using Godot;

public class WalkingEnemyAnimator : Node
{
    private WalkingEnemy enemy;
    private AnimatedSprite sprite;
    private AnimationPlayer animator;

    public override void _Ready() {
        enemy = GetParentOrNull<WalkingEnemy>();
        sprite = GetNodeOrNull<AnimatedSprite>("../AnimatedSprite");
        animator = GetNodeOrNull<AnimationPlayer>("../AnimationPlayer");   
    }

    public override void _Process(float delta) {
        if (enemy == null)
            return;
        
        if (sprite != null) {
            sprite.FlipH = (enemy.walkDirection == WalkingEnemy.WalkDirections.Right);
        }
        if (animator != null) {
            switch (enemy.enemyState) {
                case EnemyState.Walking:
                    animator.Play("walk");
                    break;
                case EnemyState.Idle:
                    animator.Play("idle");
                    break;
                default:
                    break;
            }
        }
    }
}
