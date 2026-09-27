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
	private bool mouseHighlighted;
	private bool controllerFocused;
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

		mouseHighlighted = true;
		RefreshHighlight(true);

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
		mouseHighlighted = false;
		controllerFocused = false;
		highLighted = false;
		SetAlpha(value ? alpha : alpha * 0.4f);
		QueueRedraw();
	}

	public void OnMouseExited()
	{
		if (!avalible)
			return;

		mouseHighlighted = false;
		RefreshHighlight(false);
	}

	public void SetControllerFocused(bool focused)
	{
		if (!avalible)
			return;

		controllerFocused = focused;
		RefreshHighlight(focused);
	}

	private void RefreshHighlight(bool playHoverSound)
	{
		highLighted = avalible && (mouseHighlighted || controllerFocused);
		SetAlpha(highLighted ? Mathf.Min(1.0f, alpha + 0.4f) : alpha);
		QueueRedraw();
		var size = highLighted ? new Vector2(1.2f, 1.2f) : Vector2.One;
		TweenEffect(size, 0.2f);
		if (playHoverSound && highLighted)
			hoverSFX.Play();
	}

	private void SetAlpha(float value)
	{
		Color current = Modulate;
		Modulate = new Color(current.R, current.G, current.B, value);
	}


	public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
	{
		if (!highLighted || !@event.IsActionPressed("select_area"))
			return;

		SelectArea();
		selected = false;
	}
}
