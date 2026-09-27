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
		SetStatus($"Round {stateGame.RoundDisplay} | Choose a start position | Hands: {stateGame.hand}");
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
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is StartArea startArea)
				startArea.SetAvailable(available);
		}

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