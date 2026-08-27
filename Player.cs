using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 300.0f;
    public bool IsAlive { get; private set; } = true;

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority()) return;
        
        if (!IsAlive) return;

        // Pega os inputs padrão do Godot (setas ou WASD)
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = direction * Speed;
        
        MoveAndSlide();
    }

    public void Eliminate()
    {
        IsAlive = false;
        Visible = false;
        
        // Desativa a colisão para o jogador não atrapalhar
        GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
    }

    public void Respawn(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
        IsAlive = true;
        Visible = true;
        GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
    }
}