using Godot;
using System;

public partial class Arrow : Node2D
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Rotation = GlobalPosition.DirectionTo(GetGlobalMousePosition()).Angle();
		if (Input.IsActionJustReleased("ui_select"))
		{
			Visible = false;
		}
	}
}
