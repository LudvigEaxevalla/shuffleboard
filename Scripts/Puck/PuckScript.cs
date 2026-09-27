using Godot;
using System;
using System.Collections.Generic;

public partial class PuckScript : RigidBody2D, IProjectile
{
	[Export] Global global;
	Random random = new Random();
	public int minForce = 100;
	public int maxForce = 2500;
	[Export] public float distancePerValueIncrease = 100f;
	[Export] public int valueIncreasePerDistanceStep = 1;
	public int[] identifier {get; set;}
	public string[] puckColor = {"White", "Red", "Blue", "Green", "Purple", "Bronze", "Silver", "Gold", "Rainbow"};
	public string[] type = {"Blank", "Striped", "Solid", "Shiny", "Special"};
	public string[] special = {"None","Bomb", "Magnetic", "Converter"};
	public string[] rarity = {"Common", "Uncommon", "Rare", "Epic", "Legendary"};
	public int ValueOnDeath {get; set;}
	public int puckValue {get; set;}
	public float Multiplier {get; set;}
	private Sprite2D sprite;
	private Vector2 spriteBaseScale;
	private Label scoreBreakdownLabel;
	private Label travelValueLabel;
	private Tween travelValueTween;
	private Line2D outsideZoneMark;
	private HandleProjectiles projectileHandler;
	private float distanceTravelled;
	private int basePuckValue;
	private bool trackingTravelDistance;
	public int forceBuildUp => projectileHandler?.ForceBuildUp ?? minForce;
	private readonly HashSet<PointZoneScript> zonesInside = new();
	private readonly HashSet<StartArea> selectedStartArea = new();
	public bool shootable;
	public bool puckStopped;
	private AudioStreamPlayer2D shootSFX;
	private AudioStreamPlayer2D inZoneSFX;

	private enum SoundEffect
	{
		Shoot,
		Bounce,
		InZone
	}


	RigidBody2D IProjectile.PhysicsBody => this;
	int IProjectile.MinProjectileForce => minForce;
	int IProjectile.MaxProjectileForce => maxForce;
	bool IProjectile.CanBeLaunched => shootable;
	bool IProjectile.HasProjectileStopped => puckStopped;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		projectileHandler = GetNode<HandleProjectiles>("HandleProjectiles");
		shootSFX = GetNode<AudioStreamPlayer2D>("ShootSFX");
		inZoneSFX = GetNode<AudioStreamPlayer2D>("InZoneSFX");
		sprite = GetNode<Sprite2D>("Sprite2D");
		spriteBaseScale = sprite.Scale;
		scoreBreakdownLabel = new Label
		{
			Size = new Vector2(180, 36),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TopLevel = true,
			ZIndex = 100,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		ConfigureScoreLabel(scoreBreakdownLabel);
		AddChild(scoreBreakdownLabel);
		travelValueLabel = new Label
		{
			Size = new Vector2(180, 36),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TopLevel = true,
			ZIndex = 100,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		ConfigureScoreLabel(travelValueLabel);
		AddChild(travelValueLabel);
		outsideZoneMark = new Line2D
		{
			Width = 6,
			DefaultColor = Colors.Red,
			ZIndex = 1,
			Visible = false
		};
		outsideZoneMark.AddPoint(new Vector2(-35, -35));
		outsideZoneMark.AddPoint(new Vector2(35, 35));
		AddChild(outsideZoneMark);
		ZIndex = 20;
		LinearDamp = 2f;
		shootable = false;
		puckStopped = false;
		PuckBaseValue();
	}

	public override void _Process(double delta)
	{
		var currentPosition = GlobalPosition;
		if (travelValueLabel.Visible)
			travelValueLabel.GlobalPosition = currentPosition + new Vector2(-90, -18);

		if (!trackingTravelDistance)
			return;

		distanceTravelled += LinearVelocity.Length() * (float)delta;

		var distanceStep = Mathf.Max(1f, distancePerValueIncrease);
		var valueIncreases = (int)(distanceTravelled / distanceStep);
		var newValue = basePuckValue + valueIncreases * valueIncreasePerDistanceStep;
		if (newValue > puckValue)
		{
			puckValue = newValue;
			UpdateTravelValueLabel();
		}
	}

	public void PuckBaseValue()
	{
		puckValue = 5;
		basePuckValue = puckValue;
		Multiplier = 2;
	}
	public void SetShootable(bool value)
	{
		shootable = value;
	}

	public void PlayCountEffect()
	{
		sprite.Scale = spriteBaseScale;
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Bounce);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(sprite, "scale", spriteBaseScale * 1.4f, 0.2f);
		tween.TweenProperty(sprite, "scale", spriteBaseScale, 0.2f);
	}

	public void ShowScoreBreakdown()
	{
		scoreBreakdownLabel.Text = $"{puckValue * Multiplier:0.##}";
		var startPosition = GlobalPosition + new Vector2(-90, -65);
		scoreBreakdownLabel.GlobalPosition = startPosition;
		scoreBreakdownLabel.Visible = true;
		scoreBreakdownLabel.Scale = new Vector2(0.7f, 0.7f);

		var tween = scoreBreakdownLabel.CreateTween();
		tween.SetTrans(Tween.TransitionType.Bounce);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(scoreBreakdownLabel, "scale", Vector2.One, 0.25f);
		tween.Parallel().TweenProperty(scoreBreakdownLabel, "global_position", startPosition + new Vector2(0, -28), 0.35f);
	}

	private static void ConfigureScoreLabel(Label label)
	{
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeFontSizeOverride("font_size", 20);
	}

	private void UpdateTravelValueLabel()
	{
		travelValueLabel.Text = puckValue.ToString();
		travelValueLabel.GlobalPosition = GlobalPosition + new Vector2(-90, -18);
		travelValueLabel.Visible = true;
		travelValueLabel.Scale = new Vector2(0.7f, 0.7f);

		if (travelValueTween != null && travelValueTween.IsRunning())
			travelValueTween.Kill();

		travelValueTween = travelValueLabel.CreateTween();
		travelValueTween.SetTrans(Tween.TransitionType.Bounce);
		travelValueTween.SetEase(Tween.EaseType.Out);
		travelValueTween.TweenProperty(travelValueLabel, "scale", Vector2.One, 0.25f);
	}

	public void MarkOutsideScoringZone()
	{
		outsideZoneMark.Visible = true;
	}




	public void SetGlobal(Global globalNode)
	{
		global = globalNode;
	}

	public void SetInsideZone(PointZoneScript zone, bool inside)
	{
		if (inside)
		{
			zonesInside.Add(zone);
			inZoneSFX.Play();

		}
		else
		{
			zonesInside.Remove(zone);
		}
	}
	void IProjectile.OnProjectileReleased()
	{
		shootable = false;
		puckValue = basePuckValue;
		distanceTravelled = 0;
		trackingTravelDistance = true;
		UpdateTravelValueLabel();
		LinearDamp = (float)random.NextDouble() * (2.0f - 1.0f) + 1.0f;
		global?.OnPuckReleased();
		shootSFX.Play();
	}

	void IProjectile.OnProjectileStopped()
	{
		if (puckStopped)
			return;

		shootable = false;
		puckStopped = true;
		trackingTravelDistance = false;
		GD.Print("Shootable: " + shootable);
		global?.CheckForPucks();
	}


}
