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

	private Ring grabbedRing = null; 

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
		RingsCountSlider = GetNode<HSlider>("RingsCountSlider");
		RingsCountValue = GetNode<Label>("RingsCountValue");
		NewGameButton = GetNode<Button>("NewGameButton");
		SolveButton = GetNode<Button>("SolveButton");
		GameArea = GetNode<Control>("GameArea");
		StatusLabel = GetNode<Label>("StatusLabel");
		MovesLabel = GetNode<Label>("MovesLabel");

		RingsCountSlider.ValueChanged += OnRingsCountChanged;
		NewGameButton.Pressed += OnNewGame;
		SolveButton.Pressed += OnSolve;

		OnNewGame();
	}

	private void OnRingsCountChanged(double value)
	{
		RingsCountValue.Text = ((int)value).ToString();
	}

	private void OnNewGame()
	{
		ClearGame();
		CreateTowers();

		int ringsCount = (int)RingsCountSlider.Value;
		CreateRings(ringsCount);
		PlaceRingsOnFirstTower();

		movesCount = 0;
		UpdateMovesLabel();

		StatusLabel.Text = "Перетащите кольца для решения головоломки";
		isSolving = false;
	}

	private async void OnSolve()
	{
		if (isSolving) return;

		isSolving = true;
		StatusLabel.Text = "Автоматическое решение...";
		SolveButton.Disabled = true;

		await SolveHanoiAlgorithm();

		StatusLabel.Text = "Решение завершено!";
		SolveButton.Disabled = false;
		isSolving = false;
	}

	private void ClearGame()
	{
		foreach (var ring in rings)
		{
			if (IsInstanceValid(ring))
				ring.QueueFree();
		}
		rings.Clear();

		foreach (var tower in towers)
		{
			if (tower.Visual != null && IsInstanceValid(tower.Visual))
				tower.Visual.QueueFree();
			tower.Rings.Clear();
		}
	}

	private void CreateTowers()
	{
		towers.Clear();
		var towerNodes = new[]
		{
			GameArea.GetNode<Control>("Tower1"),
			GameArea.GetNode<Control>("Tower2"),
			GameArea.GetNode<Control>("Tower3")
		};

		for (int i = 0; i < 3; i++)
		{
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
			ring.RingSize = count - i;
			ring.RingColor = ringColors[i % ringColors.Length];
			ring.RingIndex = i;
			ring.Size = new Vector2(20 + ring.RingSize * 15, 30);
			GameArea.AddChild(ring);

			// 🆕 Подписка на сигналы мыши
			ring.OnRingGrabbed += OnRingGrabbed;
			ring.OnRingReleased += OnRingReleased;
			ring.OnRingDragged += OnRingDragged;

			rings.Add(ring);
		}
	}

	private void PlaceRingsOnFirstTower()
	{
		for (int i = 0; i < rings.Count; i++)
		{
			var ring = rings[i];
			var tower = towers[0];
			ring.Position = new Vector2(
				tower.Position.X + tower.Size.X / 2 - ring.Size.X / 2,
				tower.Position.Y + tower.Size.Y - 20 - (i + 1) * 35
			);
			tower.Rings.Add(ring);
			ring.CurrentTower = 0;
		}
	}

	private bool CheckWinCondition() => towers[2].Rings.Count == rings.Count;

	private void UpdateMovesLabel() => MovesLabel.Text = "Ходов: " + movesCount;

	private async Task SolveHanoiAlgorithm() => await HanoiRecursive(rings.Count, 0, 2, 1);

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
		if (towers[fromTower].Rings.Count == 0) return;

		var ring = towers[fromTower].Rings[^1];

		var endPos = new Vector2(
			towers[toTower].Position.X + towers[toTower].Size.X / 2 - ring.Size.X / 2,
			towers[toTower].Position.Y + towers[toTower].Size.Y - 20 - (towers[toTower].Rings.Count + 1) * 35
		);

		var tween = CreateTween();
		tween.TweenProperty(ring, "position", endPos, 0.4);
		await ToSignal(tween, Tween.SignalName.Finished);

		towers[fromTower].Rings.Remove(ring);
		towers[toTower].Rings.Add(ring);
		ring.CurrentTower = toTower;

		movesCount++;
		UpdateMovesLabel();

		await ToSignal(GetTree().CreateTimer(0.2), Timer.SignalName.Timeout);
	}

	
	private bool CanMoveRing(Ring ring)
	{
		var tower = towers[ring.CurrentTower];
		return tower.Rings.Count > 0 && tower.Rings[^1] == ring;
	}

	
	private int GetTowerAtPosition(Vector2 pos)
	{
		for (int i = 0; i < towers.Count; i++)
		{
			var t = towers[i];
			var rect = new Rect2(t.Position, t.Size);
			if (rect.HasPoint(pos))
				return i;
		}
		return -1;
	}


	private void TryPlaceRing(Ring ring, Vector2 mousePos)
	{
		int targetTowerIndex = GetTowerAtPosition(mousePos);
		if (targetTowerIndex == -1) return;

		var fromTower = towers[ring.CurrentTower];
		var toTower = towers[targetTowerIndex];

		if (toTower.Rings.Count == 0 || toTower.Rings[^1].RingSize > ring.RingSize)
		{
			fromTower.Rings.Remove(ring);
			toTower.Rings.Add(ring);
			ring.CurrentTower = targetTowerIndex;

			ring.Position = new Vector2(
				toTower.Position.X + toTower.Size.X / 2 - ring.Size.X / 2,
				toTower.Position.Y + toTower.Size.Y - 20 - toTower.Rings.Count * 35
			);

			movesCount++;
			UpdateMovesLabel();

			if (CheckWinCondition())
				StatusLabel.Text = "✅ Победа!";
		}
		else
		{
			// Нельзя положить — вернуть на место
			ring.Position = new Vector2(
				fromTower.Position.X + fromTower.Size.X / 2 - ring.Size.X / 2,
				fromTower.Position.Y + fromTower.Size.Y - 20 - fromTower.Rings.Count * 35
			);
		}
	}

	
	private void OnRingGrabbed(Ring ring)
	{
		if (!CanMoveRing(ring)) return;
		grabbedRing = ring;
		StatusLabel.Text = $"Вы выбрали кольцо {ring.RingSize}";
	}

	private void OnRingDragged(Ring ring, Vector2 mousePos)
	{
		if (grabbedRing == ring)
			ring.Position = mousePos - ring.Size / 2;
	}

	private void OnRingReleased(Ring ring, Vector2 mousePos)
	{
		if (grabbedRing == ring)
		{
			TryPlaceRing(ring, mousePos);
			grabbedRing = null;
		}
	}
}



public class Tower
{
	public int TowerIndex { get; set; }
	public Vector2 Position { get; set; }
	public Vector2 Size { get; set; }
	public List<Ring> Rings { get; set; } = new List<Ring>();
	public TowerVisual Visual { get; set; }
}



public partial class Ring : Control
{
	public int RingSize { get; set; }
	public int RingIndex { get; set; }
	public int CurrentTower { get; set; } = 0;
	public Color RingColor { get; set; } = Colors.White;

	public event Action<Ring> OnRingGrabbed;
	public event Action<Ring, Vector2> OnRingDragged;
	public event Action<Ring, Vector2> OnRingReleased;

	private bool isDragging = false;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop; // 🆕 Разрешаем обработку
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					isDragging = true;
					OnRingGrabbed?.Invoke(this);
				}
				else if (isDragging)
				{
					isDragging = false;
					OnRingReleased?.Invoke(this, GetGlobalMousePosition());
				}
			}
		}
		else if (@event is InputEventMouseMotion && isDragging)
		{
			OnRingDragged?.Invoke(this, GetGlobalMousePosition());
		}
	}

	public override void _Draw()
	{
		var rect = new Rect2(Vector2.Zero, Size);

		// Основной цвет кольца
		DrawRect(rect, RingColor);

		// Градиентный эффект (светлая полоса сверху)
		var lightRect = new Rect2(0, 0, Size.X, Size.Y / 3);
		var lightColor = new Color(
			Mathf.Clamp(RingColor.R + 0.3f, 0, 1),
			Mathf.Clamp(RingColor.G + 0.3f, 0, 1),
			Mathf.Clamp(RingColor.B + 0.3f, 0, 1),
			RingColor.A
		);
		DrawRect(lightRect, lightColor);

		// Тень снизу
		var shadowRect = new Rect2(0, Size.Y * 2 / 3, Size.X, Size.Y / 3);
		var shadowColor = new Color(
			Mathf.Clamp(RingColor.R - 0.2f, 0, 1),
			Mathf.Clamp(RingColor.G - 0.2f, 0, 1),
			Mathf.Clamp(RingColor.B - 0.2f, 0, 1),
			RingColor.A
		);
		DrawRect(shadowRect, shadowColor);

		// Обводка
		DrawRect(rect, Colors.Black, false, 2.0f);

		// Внутренняя полость кольца
		if (Size.X > 20)
		{
			var innerRect = new Rect2(Size.X / 4, 0, Size.X / 2, Size.Y);
			DrawRect(innerRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
		}
	}
}
