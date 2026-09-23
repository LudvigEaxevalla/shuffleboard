using Godot;
using System;

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
	public int maxPucks = 10;
	public int pucksRemaining;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pucksRemaining = maxPucks;
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
		if (pucks < maxPucks)
		{
			AddNewPuck();
		}
	}

	public void AddNewPuck()
	{
		var puck = puckScene.Instantiate<Node2D>();
		AddChild(puck);
		puck.GlobalPosition = puckStartPosition;
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
