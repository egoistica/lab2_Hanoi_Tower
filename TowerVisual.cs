using Godot;

public partial class TowerVisual : Control
{
    public override void _Ready()
    {
        // Включаем обработку ввода
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _Draw()
    {
        // Рисуем стержень
        var rect = new Rect2(Vector2.Zero, Size);
        
        // Основание стержня (платформа)
        var baseRect = new Rect2(0, Size.Y - 20, Size.X, 20);
        DrawRect(baseRect, new Color(0.6f, 0.4f, 0.2f)); // Коричневый цвет для основания
        
        // Сам стержень (вертикальный столб)
        var poleRect = new Rect2(Size.X / 2 - 4, 0, 8, Size.Y - 20);
        DrawRect(poleRect, new Color(0.8f, 0.6f, 0.4f)); // Светло-коричневый для стержня
        
        // Тень для основания
        var shadowRect = new Rect2(2, Size.Y - 18, Size.X - 4, 18);
        DrawRect(shadowRect, new Color(0.4f, 0.2f, 0.1f, 0.3f));
        
        // Обводка
        DrawRect(baseRect, Colors.Black, false, 2.0f);
        DrawRect(poleRect, Colors.Black, false, 2.0f);
        
        // Декоративные элементы
        // Верхушка стержня
        var topRect = new Rect2(Size.X / 2 - 6, 0, 12, 8);
        DrawRect(topRect, new Color(0.9f, 0.7f, 0.5f));
        DrawRect(topRect, Colors.Black, false, 1.0f);
    }
}
