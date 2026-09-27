using Godot;
using System;
using System.Collections.Generic;

public partial class PointZoneScript : Area2D
{
	[Export]
	float zoneMultiplier {get; set;}
	[Export] 
	Color color;
	private readonly HashSet<PuckScript> pucksInside = new();
	private readonly HashSet<PuckScript> pucksScored = new();
	public int PuckCount => pucksInside.Count;
	public float ZoneValue
	{
		get
		{
			float totalValue = PuckCount * 10;
			foreach (var puck in pucksInside)
				totalValue += puck.puckValue;
			return totalValue;
		}
	}
	public float ZonePointsPerPuck => ZoneValue * zoneMultiplier;
	private StateGame stateGame;
	Sprite2D sprite;
	float currentSize;
	private Label scoreBreakdownLabel;
	private Tween scoreLabelTween;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		stateGame = GetTree().CurrentScene.GetNode<StateGame>("STATE_GAME");
		scoreBreakdownLabel = new Label
		{
			Size = new Vector2(200, 36),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TopLevel = true,
			ZIndex = 100,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		scoreBreakdownLabel.Size = new Vector2(
			Mathf.Max(1f, Mathf.Min(620f, GetViewportRect().Size.X - 16f)),
			64f);
		scoreBreakdownLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		scoreBreakdownLabel.AddThemeColorOverride("font_color", Colors.White);
		scoreBreakdownLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		scoreBreakdownLabel.AddThemeConstantOverride("outline_size", 6);
		scoreBreakdownLabel.AddThemeFontSizeOverride("font_size", 30);
		AddChild(scoreBreakdownLabel);
		sprite.SelfModulate = color;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		currentSize = sprite.GetRect().Size.Length();
	}
	public void OnBodyEntered(Node2D body)
	{
		if (body is PuckScript puck)
		{
			pucksInside.Add(puck);
			puck.SetInsideZone(this, true);
			GD.Print($"{Name} contains {PuckCount} puck(s)");
		}
	}

	public bool CanScorePuck(PuckScript puck)
	{
		return pucksInside.Contains(puck) && !pucksScored.Contains(puck);
	}

	public bool TryAddZoneScore(PuckScript puck)
	{
		if (!CanScorePuck(puck) || !pucksScored.Add(puck))
			return false;

		ShowScoreBreakdown();
		stateGame.temporaryPoints += ZonePointsPerPuck;
		return true;
	}

	public void ResetRoundCount()
	{
		pucksScored.Clear();
		scoreBreakdownLabel.Visible = false;
	}

	private void ShowScoreBreakdown()
	{
		var pointsPerPuck = ZonePointsPerPuck;
		scoreBreakdownLabel.Text = $"{ZoneValue:0.##} x {zoneMultiplier:0.##} | {pucksScored.Count} pucks | +{pointsPerPuck * pucksScored.Count:0.##}";
		var startPosition = ClampLabelPosition(GlobalPosition + new Vector2(-100, -70));
		var targetPosition = ClampLabelPosition(startPosition + new Vector2(0, -28));
		scoreBreakdownLabel.GlobalPosition = startPosition;
		scoreBreakdownLabel.Visible = true;
		scoreBreakdownLabel.Scale = new Vector2(0.7f, 0.7f);

		if (scoreLabelTween != null && scoreLabelTween.IsRunning())
			scoreLabelTween.Kill();

		scoreLabelTween = scoreBreakdownLabel.CreateTween();
		scoreLabelTween.SetTrans(Tween.TransitionType.Bounce);
		scoreLabelTween.SetEase(Tween.EaseType.Out);
		scoreLabelTween.TweenProperty(scoreBreakdownLabel, "scale", Vector2.One, 0.25f);
		scoreLabelTween.Parallel().TweenProperty(scoreBreakdownLabel, "global_position", targetPosition, 0.35f);
	}

	private Vector2 ClampLabelPosition(Vector2 position)
	{
		const float margin = 8;
		var viewport = GetViewportRect();
		var minX = viewport.Position.X + margin;
		var minY = viewport.Position.Y + margin;
		var maxX = Mathf.Max(minX, viewport.End.X - scoreBreakdownLabel.Size.X - margin);
		var maxY = Mathf.Max(minY, viewport.End.Y - scoreBreakdownLabel.Size.Y - margin);
		return new Vector2(Mathf.Clamp(position.X, minX, maxX), Mathf.Clamp(position.Y, minY, maxY));
	}

	public void OnBodyExited(Node2D body)
	{
		if (body is PuckScript puck)
		{
			pucksInside.Remove(puck);
			puck.SetInsideZone(this, false);
			GD.Print($"{Name} contains {PuckCount} puck(s)");
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
