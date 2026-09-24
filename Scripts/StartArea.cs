using Godot;
using System;

public partial class StartArea : Area2D
{
	public bool avalible;
	public bool highLighted;
	public bool selected;
	float alpha;
	private Global global;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		global = GetTree().CurrentScene as Global;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
		alpha = Modulate.A;
		avalible = true;
		QueueRedraw();
	}

	public void OnMouseEntered()
	{
		if (!avalible)
			return;

		GD.Print("Mouse entered start area " + "highLighted: " + highLighted + " " + Name);
		highLighted = true;
		SetAlpha(Mathf.Min(1.0f, alpha + 0.4f));
		QueueRedraw();
	}

	public void SelectArea()
	{
		if (!avalible || global == null)
			return;

		selected = true;
		GD.Print("Start area selected: " + Name);
		highLighted = false;
		global.SelectStartArea(this);
	}

	public void SetAvailable(bool value)
	{
		avalible = value;
		selected = false;
		SetAlpha(value ? alpha : alpha * 0.4f);
		QueueRedraw();
	}

	public void OnMouseExited()
	{
		if (!avalible)
			return;

		GD.Print("Mouse exited start area " + "highLighted: " + highLighted + " " + Name);
		highLighted = false;
		SetAlpha(alpha);
		QueueRedraw();
	}

	private void SetAlpha(float value)
	{
		Color current = Modulate;
		Modulate = new Color(current.R, current.G, current.B, value);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (highLighted && Input.IsActionJustPressed("left_click"))
		{
			SelectArea();
			selected = false;
		}
	}
}
