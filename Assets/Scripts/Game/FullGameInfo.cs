using FLGameLogic;
using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullGameInfo
{
    public Guid GameID { get; }
    public PlayerInfoDTO OpponentInfo { get; set; }
    public GameLogicClient GameLogic { get; }
    public uint TimeExtensionsUsed { get; set; }
    public bool[] RoundRated { get; }
    public bool[] HaveRoundAnswers { get; }
    public bool RewardPending { get; set; }

    public FullGameInfo(Guid gameID, PlayerInfoDTO opponentInfo, GameLogicClient gameLogic, bool[] haveRoundAnswers, TimeSpan? expiryTimeRemaining, uint timeExtensionsUsed, bool rewardPending)
    {
        GameID = gameID;
        OpponentInfo = opponentInfo;
        GameLogic = gameLogic;
        TimeExtensionsUsed = timeExtensionsUsed;
        RoundRated = new bool[gameLogic.NumRounds];
        HaveRoundAnswers = new bool[gameLogic.NumRounds];
        RewardPending = rewardPending;
        if (haveRoundAnswers != null)
            Array.Copy(haveRoundAnswers, 0, HaveRoundAnswers, 0, haveRoundAnswers.Length);

        UpdateExpiryTime(expiryTimeRemaining);
    }

    public GameState GameState
    {
        get
        {
            if (OpponentInfo == null)
                return GameState.WaitingForSecondPlayer;
            else if (GameLogic.Finished)
                return GameState.Finished;
            else if (GameLogic.Expired)
                return GameState.Expired;
            else
                return GameState.InProgress;
        }
    }

    public void UpdateExpiryTime(TimeSpan? remainingTime) =>
        GameLogic.SetExpiryTime(remainingTime.HasValue ? DateTime.Now + remainingTime.Value : default(DateTime?));
}
