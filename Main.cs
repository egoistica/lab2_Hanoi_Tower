using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Main : Control
{
    [Export] public HSlider RingsCountSlider { get; set; }
    [Export] public Label RingsCountValue { get; set; }
    [Export] public Button NewGameButton { get; set; }
    [Export] public Button SolveButton { get; set; }
    [Export] public Control GameArea { get; set; }
    [Export] public Label StatusLabel { get; set; }
    [Export] public Label MovesLabel { get; set; }

    private List<Tower> towers = new List<Tower>();
    private List<Ring> rings = new List<Ring>();
    private int movesCount = 0;
    private bool isSolving = false;

    // Цвета для колец
    private readonly Color[] ringColors = {
        new Color(1.0f, 0.2f, 0.2f),  // Красный
        new Color(1.0f, 0.6f, 0.2f),  // Оранжевый
        new Color(1.0f, 1.0f, 0.2f),  // Желтый
        new Color(0.2f, 1.0f, 0.2f),  // Зеленый
        new Color(0.2f, 0.6f, 1.0f),  // Голубой
        new Color(0.6f, 0.2f, 1.0f),  // Фиолетовый
        new Color(1.0f, 0.2f, 0.8f),  // Розовый
        new Color(0.8f, 0.8f, 0.8f)   // Серый
    };

    public override void _Ready()
    {
        // Получаем ссылки на узлы
        RingsCountSlider = GetNode<HSlider>("RingsCountSlider");
        RingsCountValue = GetNode<Label>("RingsCountValue");
        NewGameButton = GetNode<Button>("NewGameButton");
        SolveButton = GetNode<Button>("SolveButton");
        GameArea = GetNode<Control>("GameArea");
        StatusLabel = GetNode<Label>("StatusLabel");
        MovesLabel = GetNode<Label>("MovesLabel");

        // Подключаем сигналы
        RingsCountSlider.ValueChanged += OnRingsCountChanged;
        NewGameButton.Pressed += OnNewGame;
        SolveButton.Pressed += OnSolve;

        // Инициализируем игру
        OnNewGame();
    }

    private void OnRingsCountChanged(double value)
    {
        RingsCountValue.Text = ((int)value).ToString();
    }

    private void OnNewGame()
    {
        // Очищаем предыдущее состояние
        ClearGame();

        // Создаем новые башни
        CreateTowers();

        // Создаем кольца
        int ringsCount = (int)RingsCountSlider.Value;
        CreateRings(ringsCount);

        // Размещаем кольца на первой башне
        PlaceRingsOnFirstTower();

        // Сбрасываем счетчик ходов
        movesCount = 0;
        UpdateMovesLabel();

        // Обновляем статус
        StatusLabel.Text = "Перетащите кольца для решения головоломки";
        isSolving = false;
    }

    private async void OnSolve()
    {
        if (isSolving) return;

        isSolving = true;
        StatusLabel.Text = "Автоматическое решение...";
        SolveButton.Disabled = true;

        // Запускаем алгоритм решения
        await SolveHanoiAlgorithm();

        StatusLabel.Text = "Решение завершено!";
        SolveButton.Disabled = false;
        isSolving = false;
    }

    private void ClearGame()
    {
        // Удаляем все кольца
        foreach (var ring in rings)
        {
            if (IsInstanceValid(ring))
            {
                ring.QueueFree();
            }
        }
        rings.Clear();

        // Удаляем визуальные стержни и очищаем башни
        foreach (var tower in towers)
        {
            if (tower.Visual != null && IsInstanceValid(tower.Visual))
            {
                tower.Visual.QueueFree();
            }
            tower.Rings.Clear();
        }
    }

    private void CreateTowers()
    {
        towers.Clear();

        // Получаем узлы башен
        var towerNodes = new[]
        {
            GameArea.GetNode<Control>("Tower1"),
            GameArea.GetNode<Control>("Tower2"),
            GameArea.GetNode<Control>("Tower3")
        };

        for (int i = 0; i < 3; i++)
        {
            // Создаем визуальный стержень
            var towerVisual = new TowerVisual();
            towerVisual.Position = towerNodes[i].Position;
            towerVisual.Size = new Vector2(100, 400);
            GameArea.AddChild(towerVisual);

            var tower = new Tower
            {
                TowerIndex = i,
                Position = towerNodes[i].Position,
                Size = new Vector2(100, 400),
                Visual = towerVisual
            };
            towers.Add(tower);
        }
    }

    private void CreateRings(int count)
    {
        rings.Clear();

        for (int i = 0; i < count; i++)
        {
            var ring = new Ring();
            ring.RingSize = count - i; // Большие кольца имеют больший размер
            ring.RingColor = ringColors[i % ringColors.Length];
            ring.RingIndex = i;
            ring.Position = Vector2.Zero;
            ring.Size = new Vector2(20 + ring.RingSize * 15, 30);
            rings.Add(ring);

            // Добавляем кольцо в сцену
            GameArea.AddChild(ring);

            // Убираем обработку мыши - только автоматическое решение
        }
    }

    private void PlaceRingsOnFirstTower()
    {
        // Размещаем все кольца на первой башне
        for (int i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            var tower = towers[0];

            // Позиционируем кольцо на основании стержня
            // Основание стержня находится на высоте Size.Y - 20
            ring.Position = new Vector2(
                tower.Position.X + tower.Size.X / 2 - ring.Size.X / 2,
                tower.Position.Y + tower.Size.Y - 20 - (i + 1) * 35  // Размещаем на основании
            );

            // Добавляем в башню
            tower.Rings.Add(ring);
            ring.CurrentTower = 0;
        }
    }

    // Убраны методы обработки мыши - только автоматическое решение

    private bool CheckWinCondition()
    {
        // Проверяем, что все кольца находятся на третьей башне
        return towers[2].Rings.Count == rings.Count;
    }

    private void UpdateMovesLabel()
    {
        MovesLabel.Text = "Ходов: " + movesCount;
    }

    private async Task SolveHanoiAlgorithm()
    {
        // Алгоритм решения Ханойской башни
        await HanoiRecursive(rings.Count, 0, 2, 1);
    }

    private async Task HanoiRecursive(int n, int from, int to, int aux)
    {
        if (n == 1)
        {
            await MoveRingAnimation(from, to);
            return;
        }

        await HanoiRecursive(n - 1, from, aux, to);
        await MoveRingAnimation(from, to);
        await HanoiRecursive(n - 1, aux, to, from);
    }

    private async Task MoveRingAnimation(int fromTower, int toTower)
    {
        if (towers[fromTower].Rings.Count == 0)
            return;

        var ring = towers[fromTower].Rings[towers[fromTower].Rings.Count - 1];

        // Анимируем перемещение
        var startPos = ring.Position;
        var endPos = new Vector2(
            towers[toTower].Position.X + towers[toTower].Size.X / 2 - ring.Size.X / 2,
            towers[toTower].Position.Y + towers[toTower].Size.Y - 20 - (towers[toTower].Rings.Count + 1) * 35  // Размещаем на основании
        );

        // Создаем анимацию
        var tween = CreateTween();
        tween.TweenProperty(ring, "position", endPos, 0.5);
        await ToSignal(tween, Tween.SignalName.Finished);

        // Обновляем состояние
        towers[fromTower].Rings.Remove(ring);
        towers[toTower].Rings.Add(ring);
        ring.CurrentTower = toTower;

        movesCount++;
        UpdateMovesLabel();

        // Небольшая пауза между ходами
        await ToSignal(GetTree().CreateTimer(0.2), Timer.SignalName.Timeout);
    }
}

// Класс для представления башни
public class Tower
{
    public int TowerIndex { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public List<Ring> Rings { get; set; } = new List<Ring>();
    public TowerVisual Visual { get; set; }
}

// Класс для представления кольца
public partial class Ring : Control
{
    public int RingSize { get; set; }
    public int RingIndex { get; set; }
    public int CurrentTower { get; set; } = 0;
    public Color RingColor { get; set; } = Colors.White;

    public override void _Ready()
    {
        // Отключаем обработку мыши - только автоматическое решение
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        // Рисуем кольцо
        var rect = new Rect2(Vector2.Zero, Size);
        
        // Основной цвет кольца
        DrawRect(rect, RingColor);
        
        // Градиентный эффект (светлая полоса сверху)
        var lightRect = new Rect2(0, 0, Size.X, Size.Y / 3);
        var lightColor = new Color(RingColor.R + 0.3f, RingColor.G + 0.3f, RingColor.B + 0.3f, RingColor.A);
        DrawRect(lightRect, lightColor);
        
        // Тень снизу
        var shadowRect = new Rect2(0, Size.Y * 2 / 3, Size.X, Size.Y / 3);
        var shadowColor = new Color(RingColor.R - 0.2f, RingColor.G - 0.2f, RingColor.B - 0.2f, RingColor.A);
        DrawRect(shadowRect, shadowColor);
        
        // Обводка
        DrawRect(rect, Colors.Black, false, 2.0f);
        
        // Внутренняя полость кольца (если размер позволяет)
        if (Size.X > 20)
        {
            var innerRect = new Rect2(Size.X / 4, 0, Size.X / 2, Size.Y);
            DrawRect(innerRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        }
    }

    // Убираем обработку мыши - оставляем только автоматическое решение
}
