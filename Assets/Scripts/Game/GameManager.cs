using FLGameLogic;
using GameAnalyticsSDK;
using Network;
using Network.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    public const int Me = 0, Them = 1;

    MenuGroup<IGameMenu> menus;

    public event Action<Guid> GameUpdated;

    static IEnumerable<RoundDisplayInfo> GetRounds(FullGameInfo gameInfo)
    {
        var l = gameInfo.GameLogic;
        var myScores = l.GetPlayerScores(Me);
        var theirScores = l.GetPlayerScores(Them);
        for (int i = 0; i < l.NumRounds; ++i)
        {
            var iPlayed = l.PlayerFinishedTurn(Me, i);
            var theyPlayed = l.PlayerFinishedTurn(Them, i);

            RoundState state;
            if (iPlayed)
            {
                if (theyPlayed)
                    state = RoundState.Complete;
                else
                    state = RoundState.WaitingForThem;
            }
            else
            {
                if (l.Turn == Me)
                    state = RoundState.WaitingForMe;
                else
                    state = RoundState.WaitingForThem;
            }

            if (!iPlayed)
                yield return new RoundDisplayInfo
                {
                    gameInfo = gameInfo,
                    index = i,
                    category = null,
                    myScore = null,
                    theirScore = null,
                    myWords = Enumerable.Empty<WordScorePair>(),
                    theirWords = Enumerable.Empty<WordScorePair>(),
                    state = state,
                    roundRated = false,
                    haveAnswers = false
                };
            else
                yield return new RoundDisplayInfo
                {
                    gameInfo = gameInfo,
                    index = i,
                    category = l.Categories[i],
                    myScore = myScores[i],
                    theirScore = theyPlayed ? theirScores[i] : default(uint?),
                    myWords = l.GetPlayerAnswers(Me, i),
                    theirWords = l.GetPlayerAnswers(Them, i),
                    state = state,
                    roundRated = gameInfo.RoundRated[i],
                    haveAnswers = gameInfo.HaveRoundAnswers[i]
                };

            if (!iPlayed || !theyPlayed)
                yield break;
        }
    }


    GameEndPoint endPoint;

    FullGameInfo activeGame;


    protected override void Awake()
    {
        base.Awake();

        endPoint = ConnectionManager.Instance.EndPoint<GameEndPoint>();

        endPoint.OpponentJoined += OnOpponentJoined;
        endPoint.OpponentTurnEnded += OnOpponentTurnEnded;
        endPoint.GameEnded += OnGameEnded;
        endPoint.GameExpired += OnGameExpired;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        endPoint.OpponentJoined -= OnOpponentJoined;
        endPoint.OpponentTurnEnded -= OnOpponentTurnEnded;
        endPoint.GameEnded -= OnGameEnded;
        endPoint.GameExpired -= OnGameExpired;
    }

    private void OnGameExpired(Guid gameID, bool myWin, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold, bool hasReward) =>
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var td = TransientData.Instance;
            using (ChangeNotifier.BeginBatch())
            {
                td.Score.Value = myPlayerScore;
                td.Rank.Value = myPlayerRank;
                td.Level.Value = myLevel;
                td.XP.Value = myXP;
                td.Gold.Value = myGold;
            }

            var game = await GameRepository.Instance.GetFullGameInfo(gameID);

            if (game == null)
            {
                Debug.LogError($"Received event for unknown game with ID {gameID}");
                return;
            }

            game.GameLogic.Expire(myWin ? Them : Me);
            game.RewardPending = hasReward;
            OnGameInfoUpdated(game);
        });

    void Start()
    {
        menus = new MenuGroup<IGameMenu>(gameObject);
        TaskExtensions.RunIgnoreAsync(menus.Hide);
    }

    void OnGameInfoUpdated(FullGameInfo gameInfo)
    {
        // we could compare ID's, but this should be equivalent
        if (activeGame == gameInfo && menus.Menu<OverviewUI>().IsVisible)
            ShowGameOverview(gameInfo);

        GameUpdated?.Invoke(gameInfo.GameID);
    }

    private void OnOpponentJoined(Guid gameID, PlayerInfoDTO opponentInfo, TimeSpan? expiryTimeRemaining) =>
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var game = await GameRepository.Instance.GetFullGameInfo(gameID);

            if (game == null)
            {
                Debug.LogError($"Received event for unknown game with ID {gameID}");
                return;
            }

            game.OpponentInfo = opponentInfo;
            game.UpdateExpiryTime(expiryTimeRemaining);

            //?? notify player?

            OnGameInfoUpdated(game);
        });

    private void OnOpponentTurnEnded(Guid gameID, byte roundNumber, IEnumerable<WordScorePairDTO> wordsPlayed, TimeSpan? expiryTimeRemaining) =>
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var game = await GameRepository.Instance.GetFullGameInfo(gameID);

            if (game == null)
            {
                Debug.LogError($"Received event for unknown game with ID {gameID}");
                return;
            }

            if (wordsPlayed == null)
            {
                if (!game.GameLogic.RegisterTurnTakenWithUnknownPlays(Them, roundNumber))
                    Debug.LogError($"Failed to register opponent turn with unknown plays for game {gameID}");
            }
            else
            {
                if (!game.GameLogic.RegisterFullTurn(Them, roundNumber, wordsPlayed.Select(w => (WordScorePair)w)))
                    Debug.LogError($"Failed to register opponent turn for game {gameID}");
            }

            game.UpdateExpiryTime(expiryTimeRemaining);

            //?? notify player?

            OnGameInfoUpdated(game);
        });

    void OnGameEnded(Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold, bool hasReward) =>
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var td = TransientData.Instance;
            using (ChangeNotifier.BeginBatch())
            {
                td.Score.Value = myPlayerScore;
                td.Rank.Value = myPlayerRank;
                td.Level.Value = myLevel;
                td.XP.Value = myXP;
                td.Gold.Value = myGold;
            }

            var game = await GameRepository.Instance.GetFullGameInfo(gameID);

            if (game == null)
            {
                Debug.LogError($"Received event for unknown game with ID {gameID}");
                return;
            }

            game.RewardPending = hasReward;
            OnGameInfoUpdated(game);

            //?? notify player?
            //?? show end game UI if active game?
        });

    public void StartNew() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        using (LoadingIndicator.Show(true))
        {
            try
            {
                var (gameID, opponentInfo, numRounds, myTurnFirst, expiryTimeRemaining) = await endPoint.NewGame();

                var logic = new GameLogicClient(numRounds, myTurnFirst ? Me : Them);
                if (!myTurnFirst)
                    logic.RegisterTurnTakenWithUnknownPlays(Them, 0);

                activeGame = GameRepository.Instance.TryRegisterGame(gameID, opponentInfo, logic, null, expiryTimeRemaining, 0, false);
                if (activeGame == null)
                {
                    Debug.LogError("Failed to register new game");
                    BackToMenu();
                }

                ShowGameOverview(activeGame);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start new game due to {ex}");
                BackToMenu();
            }
        }
    });

    public void ContinueGame(Guid gameID) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var gr = GameRepository.Instance;
        using (LoadingIndicator.Show(true))
        {
            var gameInfo = await gr.GetFullGameInfo(gameID);

            if (gameInfo == null)
            {
                BackToMenu();
                return;
            }

            activeGame = gameInfo;

            ShowGameOverview(gameInfo);
        }
    });

    public void StartRound() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        try
        {
            if (activeGame.GameLogic.Turn != Me)
            {
                Debug.LogError("Request to start round when it's not my turn");
                return;
            }

            if (activeGame.GameLogic.IsTurnInProgress(Me))
            {
                await ResumeRound();
                return;
            }

            using (LoadingIndicator.Show(true))
            {
                var (category, haveAnswers, roundTime, mustChooseGroup, groups) = await endPoint.StartRound(activeGame.GameID);
                if (mustChooseGroup)
                    ShowGroupChoices(groups, (int)TransientData.Instance.ConfigValues.RefreshGroupsAllowedPerRound);
                else
                    StartRoundImpl(category, haveAnswers.Value, roundTime.Value);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start round due to {ex}");
        }
    });

    void ShowGroupChoices(IReadOnlyList<GroupInfoDTO> groups, int numRemainingRefreshes) =>
        TaskExtensions.RunIgnoreAsync(() => menus.ShowCustom<GroupSelectionUI>(g => g.Show(groups[0], groups[1], groups[2], numRemainingRefreshes)));

    public int GetTimeExtensionsUsed() => (int)activeGame.TimeExtensionsUsed;

    public void IncreaseRoundTime()
    {
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var (gold, time) = await endPoint.IncreaseRoundTime(activeGame.GameID);

            if (time.HasValue)
            {
                menus.Menu<RoundUI>().ExtendRemainingRoundTime(time.Value);
                ++activeGame.TimeExtensionsUsed;
            }

            menus.Menu<RoundUI>().PowerUpRequestComplete();

            if (gold.HasValue)
            {
                TransientData.Instance.Gold.Value = gold.Value;
                GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "gold", TransientData.Instance.Gold.Value - gold.Value, "powerup", "time");
            }
        });
    }

    public void RevealWord()
    {
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var (gold, word, score) = await endPoint.RevealWord(activeGame.GameID);

            if (word != null && score.HasValue)
            {
                activeGame.GameLogic.RegisterPlayedWord(Me, word, score.Value);
                menus.Menu<RoundUI>().RevealWord(word, score.Value);
            }

            menus.Menu<RoundUI>().PowerUpRequestComplete();

            if (gold.HasValue)
            {
                GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "gold", TransientData.Instance.Gold.Value - gold.Value, "powerup", "reveal word");
                TransientData.Instance.Gold.Value = gold.Value;
            }
        });
    }

    public void RefreshGroupChoices(int remainingRefreshes) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        using (LoadingIndicator.Show(true))
        {
            var (newGroups, totalGold) = await endPoint.RefreshGroups(activeGame.GameID);
            if (newGroups != null)
            {
                TransientData.Instance.Gold.Value = totalGold;
                if (newGroups.Count >= 3)
                    ShowGroupChoices(newGroups, remainingRefreshes - 1);
            }
        }
    });

    public void ChooseGroup(int id) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        try
        {
            GameAnalytics.NewDesignEvent("group chosen: " + id.ToString());
            using (LoadingIndicator.Show(true))
            {
                var (category, haveAnswers, roundTime) = await endPoint.ChooseGroup(activeGame.GameID, (ushort)id);
                StartRoundImpl(category, haveAnswers, roundTime);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to set group due to {ex}");
        }
    });

    void StartRoundImpl(string category, bool haveAnswers, TimeSpan roundTime)
    {
        var roundIndex = activeGame.GameLogic.NumTurnsTakenByIncludingCurrent(Me);
        if (!activeGame.GameLogic.SetCategory(roundIndex, category))
        {
            Debug.LogError("Failed to set category");
            //?? handle
            return;
        }

        activeGame.HaveRoundAnswers[roundIndex] = haveAnswers;
        var roundResult = activeGame.GameLogic.StartRound(Me, roundTime);

        if (!roundResult.IsSuccess())
        {
            Debug.LogError($"Failed to start round, result is {roundResult}");
            //?? handle
            return;
        }

        TaskExtensions.RunIgnoreAsync(() => menus.ShowCustom<RoundUI>(r => r.StartRound(category, roundTime)));
    }

    public async Task<(string actualWord, int score, bool duplicate)> PlayWord(string word)
    {
        try
        {
            var (wordScore, corrected) = await endPoint.PlayWord(activeGame.GameID, word);
            var actualWord = string.IsNullOrEmpty(corrected) ? word : corrected;

            bool duplicate = !activeGame.GameLogic.RegisterPlayedWord(Me, actualWord, wordScore);

            return (actualWord, wordScore, duplicate);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to play word due to {ex}");
            return (null, 0, false);
        }
    }

    public void EndRound() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        try
        {
            activeGame.GameLogic.ForceEndTurn(Me);

            var turn = activeGame.GameLogic.NumTurnsTakenBy(Me) - 1;

            try
            {
                var subject = activeGame.GameLogic.Categories[turn];
                var answers = activeGame.GameLogic.GetPlayerAnswers(Me)[turn];

                GameAnalytics.NewDesignEvent("category:" + LatinIDGenerator.ToLatinIdentifier(subject) + ":answer_count", answers.Count);
                GameAnalytics.NewDesignEvent("category:" + LatinIDGenerator.ToLatinIdentifier(subject) + ":score", answers.Sum(a => a.score));
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to register round data with analytics service");
                Debug.LogException(ex);
            }

            using (LoadingIndicator.Show(true))
            {
                var (opponentWords, expiryTimeRemaining) = await endPoint.EndRound(activeGame.GameID);

                if (opponentWords != null)
                    activeGame.GameLogic.RegisterFullTurn(Them, (uint)activeGame.GameLogic.NumTurnsTakenBy(Me) - 1, opponentWords.Select(w => (WordScorePair)w));

                activeGame.UpdateExpiryTime(expiryTimeRemaining);
            }

            ShowGameOverview(activeGame);
            menus.Menu<OverviewUI>().ShowRoundDetails(turn);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to end round due to {ex}");
        }
    });

    void ShowGameOverview(FullGameInfo gameInfo)
    {
        TaskExtensions.RunIgnoreAsync(() =>
            menus.ShowCustom<OverviewUI>(o => o.ShowGame(
                gameInfo.OpponentInfo?.Name ?? "حریف شانسی",
                gameInfo.OpponentInfo?.Avatar,
                gameInfo.OpponentInfo?.Level ?? 0u,
                gameInfo.GameLogic.GetNumRoundsWon(Me),
                gameInfo.GameLogic.GetNumRoundsWon(Them),
                GetRounds(gameInfo),
                gameInfo.GameLogic.Finished,
                gameInfo.GameLogic.Expired,
                gameInfo.GameLogic.ExpiredFor == Me
            )));
    }

    public void RateRound(FullGameInfo gameInfo, int index, bool up)
    {
        activeGame.RoundRated[index] = true;
        endPoint.Vote(gameInfo.GameLogic.Categories[index], up);
        GameAnalytics.NewDesignEvent("category:" + LatinIDGenerator.ToLatinIdentifier(gameInfo.GameLogic.Categories[index]) + ":rating", up ? 1 : 0);
    }

    public async Task<IEnumerable<string>> GetRoundAnswers(FullGameInfo gameInfo, int index, bool byVideoAd)
    {
        IReadOnlyList<string> result;

        var category = gameInfo.GameLogic.Categories[index];

        if (byVideoAd)
        {
            result = await endPoint.GetAnswersByVideoAd(category);
        }
        else
        {
            ulong? gold;
            (result, gold) = await endPoint.GetAnswers(category);

            if (gold != null)
            {
                GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "gold", TransientData.Instance.Gold.Value - gold.Value, "powerup", "round answers");
                TransientData.Instance.Gold.Value = gold.Value;
            }
        }

        if (result != null && result.Any())
            activeGame.HaveRoundAnswers[index] = true;

        return result ?? Array.Empty<string>();
    }

    public async Task<bool> ResumeInProgressGameIfAny()
    {
        var game = GameRepository.Instance.InProgressGame;
        if (game != null)
        {
            var gr = GameRepository.Instance;
            using (LoadingIndicator.Show(true))
            {
                var gameInfo = await gr.GetFullGameInfo(game.GameID);

                if (gameInfo == null || !gameInfo.GameLogic.IsTurnInProgress(Me))
                    return false;

                activeGame = gameInfo;

                await ResumeRound();

                return true;
            }
        }

        return false;
    }

    Task ResumeRound()
    {
        var roundIndex = activeGame.GameLogic.NumTurnsTakenByIncludingCurrent(Me);
        var gl = activeGame.GameLogic;
        return menus.ShowCustom<RoundUI>(r => 
            r.ResumeRound(
                gl.Categories[roundIndex], 
                gl.GetTurnEndTime(Me) - DateTime.Now,
                gl.GetPlayerAnswers(Me, roundIndex))
        );
    }

    public async Task<bool> ClaimReward(Guid gameID)
    {
        var game = await GameRepository.Instance.GetFullGameInfo(gameID);

        if (game == null)
        {
            Debug.LogError($"Cannot claim reward for unknown game with ID {gameID}");
            return false;
        }

        var ep = ConnectionManager.Instance.EndPoint<GameEndPoint>();
        var gold = await ep.ClaimGameReward(gameID);
        if (gold.HasValue)
        {
            SoundEffectManager.Play(SoundEffect.GainCoins);
            TransientData.Instance.Gold.Value = gold.Value;
            game.RewardPending = false;
            return true;
        }

        return false;
    }

    public void BackToMenu() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        await menus.Hide();
        await MenuManager.Instance.Show<MainMenu>();
    });

    public TMenu Menu<TMenu>() where TMenu : MonoBehaviour, IGameMenu => menus.Menu<TMenu>();
}
