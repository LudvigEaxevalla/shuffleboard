using Godot;
using System.Collections.Generic;

public partial class StateGame : Node2D
{
	public float temporaryPoints;
	public float points;
	public int rounds = 3;
	public int discards = 4;
	public int cash;
	public int puckPile = 10;
	public int hand = 4;
	private int nextProjectileIdentifier = 1;
	private int totalRounds;
	private Global global;
	private readonly List<PuckScript> pucksToCount = new();
	private int nextPuckToCount;
	private float countDelay;
	private float scoreCountElapsed;
	private float scoreCountStart;
	private float scoreCountTarget;
	private PuckScript pendingZonePuck;
	private PointZoneScript pendingZone;
	private const int HandsPerRound = 4;
	private const float ScoreCountDuration = 1.5f;

	private enum GamePlayStage
	{
		ChooseStartPosition,
		Shoot,
		Count,
		ScoreCountUp,
		ClearBoard,
		GameOver
	}

	private GamePlayStage currentGPS = GamePlayStage.ChooseStartPosition;
	public bool CanChooseStartPosition => currentGPS == GamePlayStage.ChooseStartPosition;
	public string RoundDisplay => $"{totalRounds - rounds + 1}/{totalRounds}";

	public override void _Ready()
	{
		totalRounds = rounds;
		global = GetTree().CurrentScene as Global;
	}

	public int GetNextProjectileIdentifier()
	{
		return nextProjectileIdentifier++;
	}

	public void OnStartPositionSelected()
	{
		if (CanChooseStartPosition)
		{
			currentGPS = GamePlayStage.Shoot;
			global.SetStatus($"Round {RoundDisplay} | Aim and shoot | Hands: {hand}");
		}
	}

	public void OnProjectileStopped()
	{
		if (currentGPS != GamePlayStage.Shoot)
			return;

		if (hand > 0)
			hand--;

		GD.Print("Hands left: " + hand);
		if (hand > 0)
		{
			currentGPS = GamePlayStage.ChooseStartPosition;
			global.SpawnNextPuck();
			return;
		}

		pucksToCount.Clear();
		pucksToCount.AddRange(global.GetShotPucksInOrder());
		nextPuckToCount = 0;
		countDelay = 0;
		currentGPS = GamePlayStage.Count;
		global.SetStatus($"Round {RoundDisplay} | Counting pucks");
	}

	public override void _Process(double delta)
	{
		switch (currentGPS)
		{
			case GamePlayStage.Count:
				CountNextPuck((float)delta);
				break;
			case GamePlayStage.ScoreCountUp:
				CountScoreUp((float)delta);
				break;
			case GamePlayStage.ClearBoard:
				StartNextRound();
				break;
		}
	}

	private void CountNextPuck(float delta)
	{
		if (countDelay > 0)
		{
			countDelay -= delta;
			if (countDelay > 0)
				return;
		}

		if (pendingZone != null && pendingZonePuck != null)
		{
			if (pendingZone.TryAddZoneScore(pendingZonePuck))
			{
				global.SetStatus($"Puck {nextPuckToCount}/{pucksToCount.Count} | Zone +{pendingZone.ZonePointsPerPuck:0.##}", true);
			}

			pendingZone = null;
			pendingZonePuck = null;
			countDelay = 0.65f;
			return;
		}

		if (nextPuckToCount < pucksToCount.Count)
		{
			var countNumber = nextPuckToCount + 1;
			var puck = pucksToCount[nextPuckToCount++];
			var puckIdentifier = puck.identifier[0];
			puck.PlayCountEffect();
			var scoringZone = global.FindScoringZone(puck);
			if (scoringZone != null)
			{
				var puckPoints = puck.puckValue * puck.Multiplier;
				puck.ShowScoreBreakdown();
				temporaryPoints += puckPoints;
				pendingZonePuck = puck;
				pendingZone = scoringZone;
				GD.Print("Counted puck " + puckIdentifier + " in a scoring zone");
				global.SetStatus($"Round {RoundDisplay} | Puck {countNumber}/{pucksToCount.Count} | Puck +{puckPoints:0.##} | Zone next", true);
				countDelay = 0.65f;
			}
			else
			{
				puck.MarkOutsideScoringZone();
				GD.Print("Puck " + puckIdentifier + " was outside the scoring zones");
				global.SetStatus($"Round {RoundDisplay} | Puck {countNumber}/{pucksToCount.Count} missed", true);
				countDelay = 0.85f;
			}
			return;
		}

		var roundPoints = temporaryPoints;
		temporaryPoints = 0;
		scoreCountStart = points;
		points += roundPoints;
		scoreCountTarget = points;
		if (roundPoints == 0)
		{
			global.SetScore(points);
			FinishRoundCount();
			return;
		}

		scoreCountElapsed = 0;
		currentGPS = GamePlayStage.ScoreCountUp;
		global.SetStatus($"Round {RoundDisplay} counted | Adding score", true);
	}

	private void CountScoreUp(float delta)
	{
		scoreCountElapsed += delta;
		var progress = Mathf.Clamp(scoreCountElapsed / ScoreCountDuration, 0f, 1f);
		var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
		global.SetScore(Mathf.Lerp(scoreCountStart, scoreCountTarget, easedProgress));

		if (progress >= 1f)
		{
			global.SetScore(scoreCountTarget);
			FinishRoundCount();
		}
	}

	private void FinishRoundCount()
	{
		if (rounds <= 1)
		{
			global.ClearBoard();
			currentGPS = GamePlayStage.GameOver;
			global.SetStatus("Game over");
			GD.Print("Game over. Total points: " + points);
			return;
		}

		rounds--;
		currentGPS = GamePlayStage.ClearBoard;
	}

	private void StartNextRound()
	{
		global.ClearBoard();
		hand = HandsPerRound;
		global.StartNextRound();
		currentGPS = GamePlayStage.ChooseStartPosition;
	}
}