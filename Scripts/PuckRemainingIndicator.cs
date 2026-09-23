using Godot;
using System;

public partial class PuckRemainingIndicator : Label
{
	Global global;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		global = GetNode<Global>("Global");
		Text = "Pucks Remaining: " + global.pucksRemaining;
	}

	public void UpdateLabel()
	{
		Text = "Pucks Remaining: " + global.pucksRemaining;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
