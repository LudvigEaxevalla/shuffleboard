using Godot;
using System;
using System.Drawing;
using System.Collections.Generic;

public partial class Global : Node2D
{
	[Export]
	PuckScript puckScript;
	[Export]
	PackedScene puckScene = GD.Load<PackedScene>("res://Scenes/Puck.tscn");
	[Export]
	Arrow arrow;
	[Export]
	ForceBar forceBar;
	private Label statusLabel;
	private Label scoreLabel;
	private Vector2 statusLabelBaseScale;
	Vector2 puckStartPosition;
	public int pucks;
	public int maxPucks = 10;
	public int pucksRemaining;
	private StartArea selectedStartArea;
	private Vector2 nextPuckPosition;
	private bool hasSelectedPuckPosition;
	private readonly List<Node2D> puckRoots = new();
	private StateGame stateGame;
	private StartArea focusedStartArea;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pucksRemaining = maxPucks;
		puckStartPosition = puckScript.GlobalPosition;
		arrow = GetNode<Arrow>("Puck/Arrow");
		forceBar = GetNode<ForceBar>("In-game-UI/HBoxContainer/ForceBar");
		statusLabel = GetNode<Label>("In-game-UI/StatusLabel");
		scoreLabel = GetNode<Label>("In-game-UI/ScoreLabel");
		statusLabelBaseScale = statusLabel.Scale;
		stateGame = GetNode<StateGame>("STATE_GAME");
		puckRoots.Add(puckScript.GetParent<Node2D>());
		puckScript.GetParent<Node2D>().Visible = false;
		puckScript.SetGlobal(this);
		puckScript.SetShootable(false);
		forceBar.SetPuck(puckScript);
		SetStatus("Choose a start position");
		SetScore(stateGame.points);
		arrow.Visible = true;
		SetStartAreasAvailable(true);
	}

	public void CheckForPucks()
	{
		pucks++;
		pucksRemaining--;
//		puckRemainingIndicator.UpdateLabel();
		GD.Print("Puck stopped: " + pucks);
	}

	public void SetStatus(string message, bool animate = false)
	{
		statusLabel.Text = message;
		if (animate)
		{
			statusLabel.Scale = statusLabelBaseScale;
			var tween = statusLabel.CreateTween();
			tween.SetTrans(Tween.TransitionType.Bounce);
			tween.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(statusLabel, "scale", statusLabelBaseScale * 1.12f, 0.2f);
			tween.TweenProperty(statusLabel, "scale", statusLabelBaseScale, 0.2f);
		}
	}

	public void SetScore(float score)
	{
		scoreLabel.Text = $"SCORE\n{score:0.##}";
	}

	public void SelectStartArea(StartArea area)
	{
		if (selectedStartArea != null || puckScript == null || !stateGame.CanChooseStartPosition)
			return;

		selectedStartArea = area;
		nextPuckPosition = area.GlobalPosition;
		hasSelectedPuckPosition = true;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is StartArea startArea)
				startArea.SetAvailable(false);
		}

		var puckRoot = puckScript.GetParent<Node2D>();
		puckRoot.GlobalPosition = area.GlobalPosition;
		puckRoot.Visible = true;
		puckScript.LinearVelocity = Vector2.Zero;
		puckScript.AngularVelocity = 0;
		puckScript.Sleeping = true;
		puckScript.SetShootable(true);
		stateGame.OnStartPositionSelected();
	}

	public override void _Input(InputEvent @event)
	{
		if (stateGame == null || !stateGame.CanChooseStartPosition || @event is not InputEventJoypadButton joypadButton || !joypadButton.Pressed)
			return;

		var direction = joypadButton.ButtonIndex switch
		{
			JoyButton.DpadLeft => Vector2.Left,
			JoyButton.DpadRight => Vector2.Right,
			JoyButton.DpadUp => Vector2.Up,
			JoyButton.DpadDown => Vector2.Down,
			_ => Vector2.Zero
		};

		if (direction != Vector2.Zero)
		{
			NavigateStartArea(direction);
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event.IsActionPressed("select_area") && focusedStartArea != null && focusedStartArea.avalible)
		{
			focusedStartArea.SelectArea();
			GetViewport().SetInputAsHandled();
		}
	}

	public void OnPuckReleased()
	{
		selectedStartArea = null;
		SetStartAreasAvailable(false);
	}

	public void SpawnNextPuck()
	{
		var puckRoot = puckScene.Instantiate<Node2D>();
		AddChild(puckRoot);
		puckRoot.GlobalPosition = puckStartPosition;
		puckRoot.Visible = false;
		puckRoots.Add(puckRoot);
		puckScript = puckRoot.GetNode<PuckScript>("CharacterBody2D");
		puckScript.SetGlobal(this);
		puckScript.SetShootable(false);
		forceBar.SetPuck(puckScript);
		arrow.Reparent(puckRoot, false);
		arrow.Visible = true;
		SetStartAreasAvailable(true);
		SetStatus($"Round {stateGame.RoundDisplay} | Choose a start position | Moves: {stateGame.moves}");
		GD.Print("Added new puck at " + puckRoot.GlobalPosition);
		GD.Print("Pucks used: " + pucks);
		GD.Print("Pucks remaining: " + pucksRemaining);
	}

	public List<PuckScript> GetShotPucksInOrder()
	{
		var shotPucks = new List<PuckScript>();
		foreach (var puckRoot in puckRoots)
		{
			if (!GodotObject.IsInstanceValid(puckRoot))
				continue;

			var puck = puckRoot.GetNodeOrNull<PuckScript>("CharacterBody2D");
			if (puck != null && puck.puckStopped && puck.identifier != null && puck.identifier.Length > 0)
				shotPucks.Add(puck);
		}

		shotPucks.Sort((left, right) => left.identifier[0].CompareTo(right.identifier[0]));
		return shotPucks;
	}

	public PointZoneScript FindScoringZone(PuckScript puck)
	{
		foreach (Node child in GetChildren())
		{
			if (child is PointZoneScript zone && zone.CanScorePuck(puck))
				return zone;
		}

		return null;
	}

	public void ClearBoard()
	{
		arrow.Reparent(this, false);
		arrow.Visible = false;
		foreach (Node child in GetChildren())
		{
			if (child is PointZoneScript zone)
				zone.ResetRoundCount();
		}

		foreach (var puckRoot in puckRoots)
		{
			if (GodotObject.IsInstanceValid(puckRoot))
				puckRoot.QueueFree();
		}

		puckRoots.Clear();
		puckScript = null;
		selectedStartArea = null;
		SetStartAreasAvailable(false);
	}

	public void StartNextRound()
	{
		pucks = 0;
		pucksRemaining = maxPucks;
		SpawnNextPuck();
	}

	private void SetStartAreasAvailable(bool available)
	{
		focusedStartArea?.SetControllerFocused(false);
		focusedStartArea = null;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is StartArea startArea)
				startArea.SetAvailable(available);
		}

		if (!available)
			return;

		var focusPosition = puckScript != null ? puckScript.GlobalPosition : puckStartPosition;
		var nearestDistanceSquared = float.MaxValue;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is not StartArea startArea || !startArea.avalible)
				continue;

			var distanceSquared = focusPosition.DistanceSquaredTo(startArea.GlobalPosition);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearestDistanceSquared = distanceSquared;
				focusedStartArea = startArea;
			}
		}

		focusedStartArea?.SetControllerFocused(true);
	}

	private void NavigateStartArea(Vector2 direction)
	{
		if (focusedStartArea == null || !focusedStartArea.avalible)
			return;

		StartArea nextArea = null;
		var bestAlignment = -1f;
		var bestDistanceSquared = float.MaxValue;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is not StartArea candidate || !candidate.avalible || candidate == focusedStartArea)
				continue;

			var offset = candidate.GlobalPosition - focusedStartArea.GlobalPosition;
			var forwardDistance = offset.Dot(direction);
			if (forwardDistance <= 0f)
				continue;

			var distanceSquared = offset.LengthSquared();
			var alignment = forwardDistance / Mathf.Sqrt(distanceSquared);
			if (alignment > bestAlignment || (Mathf.IsEqualApprox(alignment, bestAlignment) && distanceSquared < bestDistanceSquared))
			{
				bestAlignment = alignment;
				bestDistanceSquared = distanceSquared;
				nextArea = candidate;
			}
		}

		if (nextArea == null)
			return;

		focusedStartArea.SetControllerFocused(false);
		focusedStartArea = nextArea;
		focusedStartArea.SetControllerFocused(true);
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("reset"))
		{
			GetTree().ReloadCurrentScene();
			GD.Print("Scene reset");
		}

	}
}