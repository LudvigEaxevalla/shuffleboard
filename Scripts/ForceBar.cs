using Godot;
using System;

public partial class ForceBar : ProgressBar
{
	/*[Export] 
	PuckScript puckScript;
	*/
	private PuckScript puckScript;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		UpdateRange();
	}

	public void SetPuck(PuckScript newPuck)
	{
		puckScript = newPuck;
		UpdateRange();
	}

	void UpdateRange()
	{
		if (puckScript == null)
			return;

		MinValue = puckScript.minForce;
		MaxValue = puckScript.maxForce;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (puckScript != null)
			Value = puckScript.forceBuildUp;
	}
}
