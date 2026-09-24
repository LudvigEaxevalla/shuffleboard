using Godot;
using System;
using System.Collections.Generic;

public partial class PuckScript : RigidBody2D
{
	[Export] Global global;
	Random random = new Random();
	public int minForce = 100;
	public int maxForce = 2500;
	public string[] color = {"White", "Red", "Blue", "Green", "Purple", "Bronze", "Silver", "Gold", "Rainbow"};
	public string[] type = {"Blank", "Striped", "Solid", "Shiny", "Special"};
	public string[] special = {"None","Bomb", "Magnetic", "Converter"};
	public string[] rarity = {"Common", "Uncommon", "Rare", "Epic", "Legendary"};
	public int ValueOnExplosion {get; set;}
	public int Add {get; set;}
	public float Multiplier {get; set;}
	public Sprite2D[] sprite;
	float force {get; set;}
	float dir;
	public int forceBuildUp;
	bool forceHitMax;
	bool forceApplied;
	float timeSinceRelease;
	private readonly HashSet<PointZoneScript> zonesInside = new();
	private readonly HashSet<StartArea> selectedStartArea = new();
	public bool Active;
	public bool puckStopped;
	private enum State
	{
		Idle,
		BuildingForce,
		Released,
		Stopped
	}
	State currentState = State.Idle;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ZIndex = 20;
		LinearDamp = 2f;
		forceBuildUp = minForce;
		Active = true;
		puckStopped = false;
		force = 0;
	}




	public void ResetForce()
	{
		force = 0;
	}

	public void SetGlobal(Global globalNode)
	{
		global = globalNode;
	}

	public void SetInsideZone(PointZoneScript zone, bool inside)
	{
		if (inside)
			zonesInside.Add(zone);
		else
			zonesInside.Remove(zone);

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (zonesInside.Count > 0)
			DrawArc(Vector2.Zero, 50, 0, Mathf.Tau, 64, Color.FromHsv(0, 1, 1, 0.2f), 6);
	}
	public void SetForceAndDirection(float newForce, float newDir)
	{
		LinearDamp = (float)random.NextDouble() * (2.0f - 1.0f) + 1.0f;
		force = newForce;
		dir = newDir;
		timeSinceRelease = 0;
		ApplyCentralImpulse(new Vector2(newForce * MathF.Cos(dir), newForce * MathF.Sin(dir)));
		forceApplied = true;
		GD.Print("Force set to: " + newForce);
	}

	public void ForceBuildUp()
	{
		if (forceHitMax)
		{
			forceBuildUp -= 5;
			if (forceBuildUp <= minForce)
			{
				forceBuildUp = minForce;
				forceHitMax = false;
			}
		}
		else
		{
			if (forceBuildUp < minForce)
			{
				forceBuildUp = minForce;
			}
			else
			{
				forceBuildUp += 5;
			}

			if (forceBuildUp >= maxForce)
			{
				forceBuildUp = maxForce;
				forceHitMax = true;
			}
		}
	}

	public void Release()
	{
		global?.OnPuckReleased();
		dir = GlobalPosition.DirectionTo(GetGlobalMousePosition()).Angle();
		SetForceAndDirection(forceBuildUp, dir);
	}

	public void OnPuckStop()
	{
		if (puckStopped)
			return;

		Active = false;
		puckStopped = true;
		GD.Print("Active: " + Active);
		global?.CheckForPucks();
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		switch(currentState)
		{
			case State.Idle:
				if (Active && Input.IsActionJustPressed("ui_select"))
				{
					forceApplied = false;
					forceBuildUp = minForce;
					forceHitMax = false;
					currentState = State.BuildingForce;
				}
				break;
			case State.BuildingForce:
				if (Input.IsActionPressed("ui_select"))
				{
					ForceBuildUp();
				}
				else if (Input.IsActionJustReleased("ui_select"))
				{
					Release();
					currentState = State.Released;
				}
				break;
			case State.Released:
				timeSinceRelease += (float)delta;
				if (timeSinceRelease > 0.1f && (Sleeping || LinearVelocity.Length() <= 0.1f))
				{
					currentState = State.Stopped;
				}
				break;
			case State.Stopped:
				OnPuckStop();
				break;
		}

		/*if (Input.IsActionJustPressed("ui_select"))
		{
			forceApplied = false;
			forceBuildUp = minForce;
			forceHitMax = false;
		}

		if (Input.IsActionPressed("ui_select"))
		{
			ForceBuildUp();
		}

		if (Input.IsActionJustReleased("ui_select") && !forceApplied && Active)
		{
			dir = GlobalPosition.DirectionTo(GetGlobalMousePosition()).Angle();
			SetForceAndDirection(forceBuildUp, dir);
			if (force <= 0)
			{
				puckStopped = true;
				Active = false;
				GD.Print("Puck stopped: " + puckStopped);
				GD.Print("Active: " + Active);
			}
		} 
		//GD.Print("X: " + GlobalPosition.X + ", Y: " + GlobalPosition.Y);
		//GD.Print("Force Build Up: " + forceBuildUp); */
	} 
}
