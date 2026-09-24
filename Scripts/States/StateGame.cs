using Godot;
using System;

public partial class StateGame : Node2D
{
	private HandleGameStates handleGameStates;
	public float temporaryPoints = 0;
	public float points = 0;
	public int rounds = 8;
	public int discards = 4;
	public int cash = 0;
	public int puckPile = 10;
	public int hand = 4;
	private Global global;
	private enum GamePlayStage {
		PlayerDecision,
		Shoot,
		Count,
		AddPoints,
		Multiply,
		Cards,
		ClearBoard

	}
	private enum GameState
	{
		Playing,
		Paused,
		NewRound,
		Shop,
		GameOver
	}

	GamePlayStage currentGPS = GamePlayStage.PlayerDecision;
	GameState currentGameState = GameState.Playing;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		handleGameStates = new HandleGameStates();
		global = GetTree().CurrentScene as Global;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		switch(currentGPS)
		{
			case GamePlayStage.PlayerDecision:
				handleGameStates.HandlePlayerDecision();
				break;
			case GamePlayStage.Shoot:
				handleGameStates.HandleShooting();
				break;
			case GamePlayStage.Count:
				handleGameStates.HandleCountStage();
				break;
			case GamePlayStage.Cards:
				handleGameStates.HandleCardReading();
				break;
			case GamePlayStage.ClearBoard:
				handleGameStates.HandleClearBoard();
				break;
		}

		switch(currentGameState)
		{
			case GameState.Playing:
				// Handle playing state
				break;
			case GameState.Paused:
				// Handle paused state
				break;
			case GameState.NewRound:
				// Handle new round state
				break;
			case GameState.Shop:
				// Handle shop state
				break;
			case GameState.GameOver:
				// Handle game over state
				break;
		}
	}
}
