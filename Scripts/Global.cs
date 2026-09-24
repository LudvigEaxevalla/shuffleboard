using Godot;
using System;
using System.Drawing;

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
	Vector2 puckStartPosition;
	public int pucks;
	public int moves = 10;
	public int pucksRemaining;
	private StartArea selectedStartArea;
	private Vector2 nextPuckPosition;
	private bool hasSelectedPuckPosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pucksRemaining = moves;
		puckStartPosition = puckScript.GlobalPosition;
		arrow = GetNode<Arrow>("Puck/Arrow");
		forceBar = GetNode<ForceBar>("In-game-UI/HBoxContainer/ForceBar");
		forceBar.SetPuck(puckScript);
	}

	public void CheckForPucks()
	{
		pucks++;
		pucksRemaining--;
//		puckRemainingIndicator.UpdateLabel();
		GD.Print("Puck stopped: " + pucks);
		if (pucks < moves)
		{
			AddNewPuck();
		}
	}

	public void SelectStartArea(StartArea area)
	{
		if (selectedStartArea != null)
			return;

		selectedStartArea = area;
		nextPuckPosition = area.GlobalPosition;
		hasSelectedPuckPosition = true;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is StartArea startArea)
				startArea.SetAvailable(startArea == area ? true : false);
		}

		var puckRoot = puckScript.GetParent<Node2D>();
		puckRoot.GlobalPosition = area.GlobalPosition;
		puckScript.LinearVelocity = Vector2.Zero;
		puckScript.AngularVelocity = 0;
		puckScript.Sleeping = true;
	}

	public void OnPuckReleased()
	{
		selectedStartArea = null;
		foreach (Node child in GetNode<Node2D>("Starting Areas").GetChildren())
		{
			if (child is StartArea startArea)
				startArea.SetAvailable(true);
		}
	}

	public void AddNewPuck()
	{
		var puck = puckScene.Instantiate<Node2D>();
		AddChild(puck);
		puck.GlobalPosition = hasSelectedPuckPosition ? nextPuckPosition : puckStartPosition;
		puckScript = puck.GetNode<PuckScript>("CharacterBody2D");
		puckScript.SetGlobal(this);
		forceBar.SetPuck(puckScript);
		arrow.Reparent(puck, false);
		arrow.Visible = true;
		GD.Print("Added new puck at " + puck.GlobalPosition);
		GD.Print("Pucks used: " + pucks);
		GD.Print("Pucks remaining: " + pucksRemaining);

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