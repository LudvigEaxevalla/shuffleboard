using Godot;
using System;
using System.Collections.Generic;

public partial class PointZoneScript : Area2D
{
	[Export]
	int zoneValue {get; set;}
	[Export]
	float zoneMultiplier {get; set;}
	[Export] 
	Color color;
	private readonly HashSet<PuckScript> pucksInside = new();
	private readonly HashSet<PuckScript> pucksScored = new();
	public int PuckCount => pucksInside.Count;
	public float ZonePointsPerPuck => zoneValue * zoneMultiplier;
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
		var pointsPerPuck = zoneValue * zoneMultiplier;
		scoreBreakdownLabel.Text = $"{pointsPerPuck:0.##}  {pucksScored.Count} pucks | +{pointsPerPuck * pucksScored.Count:0.##} x {zoneMultiplier:0.##}";
		var startPosition = GlobalPosition + new Vector2(-100, -70);
		scoreBreakdownLabel.GlobalPosition = startPosition;
		scoreBreakdownLabel.Visible = true;
		scoreBreakdownLabel.Scale = new Vector2(0.7f, 0.7f);

		if (scoreLabelTween != null && scoreLabelTween.IsRunning())
			scoreLabelTween.Kill();

		scoreLabelTween = scoreBreakdownLabel.CreateTween();
		scoreLabelTween.SetTrans(Tween.TransitionType.Bounce);
		scoreLabelTween.SetEase(Tween.EaseType.Out);
		scoreLabelTween.TweenProperty(scoreBreakdownLabel, "scale", Vector2.One, 0.25f);
		scoreLabelTween.Parallel().TweenProperty(scoreBreakdownLabel, "global_position", startPosition + new Vector2(0, -28), 0.35f);
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
