using Godot;
using System;

public partial class StartArea : Area2D
{
	public bool avalible;
	public bool highLighted;
	public bool selected;
	float alpha;
	private Global global;
	private Sprite2D sprite;
	private AudioStreamPlayer2D hoverSFX;
	private AudioStreamPlayer2D selectSFX;	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		hoverSFX = GetNode<AudioStreamPlayer2D>("HoverSFX");
		selectSFX = GetNode<AudioStreamPlayer2D>("SelectSFX");
		global = GetTree().CurrentScene as Global;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
		alpha = Modulate.A;
		avalible = true;
		QueueRedraw();
	}

	public void TweenEffect(Vector2 size, float duration)
	{
		var tween =CreateTween();
		tween.SetTrans(Tween.TransitionType.Bounce);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(sprite, "scale", size, duration);
	}
	public void OnMouseEntered()
	{
		if (!avalible)
			return;

		highLighted = true;
		hoverSFX.Play();
		SetAlpha(Mathf.Min(1.0f, alpha + 0.4f));
		QueueRedraw();
		Vector2 size = new Vector2(1.2f, 1.2f);
		float duration = 0.2f;
		TweenEffect(size, duration);

	}

	public void SelectArea()
	{
		if (!avalible || global == null)
			return;

		selected = true;
		GD.Print("Start area selected: " + Name);
		highLighted = false;
		global.SelectStartArea(this);
		selectSFX.Play();
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

		highLighted = false;
		SetAlpha(alpha);
		QueueRedraw();
		Vector2 size = new Vector2(1f , 1f);
		float duration = 0.2f;
		TweenEffect(size, duration);
	}

	private void SetAlpha(float value)
	{
		Color current = Modulate;
		Modulate = new Color(current.R, current.G, current.B, value);
	}


	public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
	{
		if (!highLighted || !@event.IsActionPressed("left_click"))
			return;

		SelectArea();
		selected = false;
	}
}
