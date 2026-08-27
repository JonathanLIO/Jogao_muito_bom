using Godot;
using System.Collections.Generic;

public partial class AnswerZone : Control
{
    [Export] public Label AnswerLabel { get; set; }
    [Export] public ColorRect ColorRect { get; set; }
    [Export] public Area2D DetectionArea { get; set; }

    private readonly List<Player> _playersInside = new();

    public override void _Ready()
    {
        if (DetectionArea != null)
        {
            DetectionArea.BodyEntered += OnBodyEntered;
            DetectionArea.BodyExited += OnBodyExited;
        }

        ResetZone();

        // Escuta a mudança de tamanho da UI para ajustar o tamanho da colisão
        Resized += UpdateCollisionSize;
    
        // Atualiza o tamanho na inicialização
        UpdateCollisionSize();

        ResetZone();
    }

    private void UpdateCollisionSize()
    {
        if (DetectionArea == null) return;

        var collisionShape = DetectionArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collisionShape != null && collisionShape.Shape is RectangleShape2D rectShape)
        {
            // Garante que o recurso de colisão seja único para cada instância de zona
            rectShape = (RectangleShape2D)rectShape.Duplicate();
            collisionShape.Shape = rectShape;

            // 1. Define o tamanho do retângulo igual ao tamanho do Control (UI)
            rectShape.Size = Size;

            // 2. Centraliza a colisão no meio do Control (já que a origem do Control é no canto superior esquerdo)
            collisionShape.Position = Size / 2;
        }
    }

    public void SetZoneColor(Color color)
    {
        if (ColorRect != null)
        {
            ColorRect.Color = color;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player && player.IsAlive)
        {
            _playersInside.Add(player);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Player player)
        {
            _playersInside.Remove(player);
        }
    }

    public void SetAnswerText(string text)
    {
        if (AnswerLabel != null) AnswerLabel.Text = text;
    }

    public void SetFloorVisible(bool visible)
    {
        if (ColorRect != null) ColorRect.Visible = visible;
        if (AnswerLabel != null) AnswerLabel.Visible = visible;
    }

    public List<Player> GetPlayersInside() => _playersInside;

    public void ResetZone()
    {
        SetFloorVisible(true);
    }
}