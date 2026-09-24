using Godot;
using System;
using System.Collections.Generic;

public partial class PointZoneScript : Area2D
{
	[Export]
	int points = 1;
	[Export]
	float multiplier;
	[Export] 
	Color color;
	private readonly HashSet<PuckScript> pucksInside = new();
	public int PuckCount => pucksInside.Count;
	Sprite2D sprite;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		sprite.SelfModulate = color;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
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
