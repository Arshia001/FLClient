using FLGameLogic;
using Network;
using Network.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class GameRepository : SingletonBehaviour<GameRepository>
{
    Dictionary<Guid, SimplifiedGameInfo> simpleGameInfoes = new Dictionary<Guid, SimplifiedGameInfo>();
    readonly Dictionary<Guid, FullGameInfo> games = new Dictionary<Guid, FullGameInfo>();


    public IEnumerable<SimplifiedGameInfo> SimpleGameInfoes =>
        games.Values
        .Select(Simplify)
        .Concat(
            simpleGameInfoes
            .Where(g => !games.ContainsKey(g.Key))
            .Select(g => g.Value)
        );

    public SimplifiedGameInfo InProgressGame
    {
        get
        {
            var now = DateTime.Now;
            return SimpleGameInfoes
                .Where(g => g.MyTurnEndTime.HasValue && g.MyTurnEndTime.Value > now)
                .FirstOrDefault();
        }
    }

    protected override bool IsGlobal => true;


    public async Task RefreshSimpleInfoes()
    {
        this.games.Clear();

        var games = await ConnectionManager.Instance.EndPoint<GameEndPoint>().GetAllGames();
        simpleGameInfoes = games.ToDictionary(g => g.GameID, g => new SimplifiedGameInfo(g));
    }

    public SimplifiedGameInfo GetSimplifiedGameInfo(Guid gameID)
    {
        if (games.TryGetValue(gameID, out FullGameInfo gameInfo))
            return Simplify(gameInfo);

        if (simpleGameInfoes.TryGetValue(gameID, out SimplifiedGameInfo result))
            return result;

        return null;
    }

    SimplifiedGameInfo Simplify(FullGameInfo game)
    {
        return new SimplifiedGameInfo
        (
            gameID: game.GameID,
            gameState: game.GameLogic.Expired ? GameState.Expired : game.GameState,
            myScore: game.GameLogic.GetNumRoundsWon(0),
            theirScore: game.GameLogic.GetNumRoundsWon(1),
            myTurn: game.GameLogic.Turn == 0,
            otherPlayerName: game.OpponentInfo?.Name,
            otherPlayerAvatar: game.OpponentInfo?.Avatar,
            winnerOfExpiredGame: game.GameLogic.Expired && game.GameLogic.ExpiredFor == GameManager.Them,
            expiryTime: game.GameLogic.ExpiryTime,
            myTurnEndTime: game.GameLogic.IsTurnInProgress(GameManager.Me) ? game.GameLogic.GetTurnEndTime(GameManager.Me) : default(DateTime?)
        );
    }

    public FullGameInfo TryGetCachedFullGameInfo(Guid gameID)
    {
        if (games.TryGetValue(gameID, out var result))
            return result;

        return null;
    }

    public async Task<FullGameInfo> GetFullGameInfo(Guid gameID)
    {
        if (games.TryGetValue(gameID, out var result))
            return result;

        if (!simpleGameInfoes.ContainsKey(gameID))
            return null;

        var game = await ConnectionManager.Instance.EndPoint<GameEndPoint>().GetGameInfo(gameID);

        if (game == null)
        {
            Debug.LogError($"Failed to load game {gameID} from server");
            return null;
        }

        return TryRegisterExistingGame(gameID, game);
    }

    public FullGameInfo TryRegisterGame(Guid gameID, PlayerInfoDTO opponentInfo, GameLogicClient gameLogic, bool[] haveRoundAnswers, TimeSpan? expiryTimeRemaining, uint timeExtensionsUsed, bool rewardPending)
    {
        if (games.ContainsKey(gameID))
            return null;

        var result = new FullGameInfo(gameID, opponentInfo, gameLogic, haveRoundAnswers, expiryTimeRemaining, timeExtensionsUsed, rewardPending);
        games.Add(gameID, result);
        simpleGameInfoes.Remove(gameID);
        return result;
    }

    FullGameInfo TryRegisterExistingGame(Guid gameID, GameInfoDTO game)
    {
        IEnumerable<IEnumerable<WordScorePair>> TransformWordScorePairDTOs(IEnumerable<IEnumerable<WordScorePairDTO>> words) =>
            words.Select(ws => ws.Select(w => (WordScorePair)w));

        var myTurnEndTime = game.MyTurnTimeRemaining.HasValue ? DateTime.Now + game.MyTurnTimeRemaining.Value : default(DateTime?);

        var logic = GameLogicClient.CreateFromState(game.NumRounds, game.Categories,
            new[] { TransformWordScorePairDTOs(game.MyWordsPlayed), TransformWordScorePairDTOs(game.TheirWordsPlayed) },
            new[] { myTurnEndTime, default }, game.MyTurnFirst ? GameManager.Me : GameManager.Them,
            game.Expired, game.ExpiredForMe ? GameManager.Me : GameManager.Them, game.ExpiryTimeRemaining);

        // Compensate for further rounds played by opponent for which we don't have the data yet
        for (uint i = (uint)logic.NumTurnsTakenBy(GameManager.Them); i < game.NumTurnsTakenByOpponent; ++i)
            logic.RegisterTurnTakenWithUnknownPlays(GameManager.Them, i);

        var result = TryRegisterGame(gameID, game.OtherPlayerInfo, logic, game.HaveCategoryAnswers.ToArray(), game.ExpiryTimeRemaining, game.RoundTimeExtensions, !game.RewardClaimed);

        if (result == null)
            Debug.LogError("Failed to register game just after fetching from server");

        return result;
    }

    public async Task RemoveOldGames()
    {
        var gold = await ConnectionManager.Instance.EndPoint<GameEndPoint>().ClearGameHistory();

        if (gold.HasValue)
        {
            TransientData.Instance.Gold.Value = gold.Value;
            SoundEffectManager.Play(SoundEffect.GainCoins);
        }

        var toRemove = new List<Guid>();
        foreach (var game in games.Values)
            if (game.GameState.GameHasEnded())
                toRemove.Add(game.GameID);

        foreach (var game in simpleGameInfoes.Values)
            if (game.GameState.GameHasEnded())
                toRemove.Add(game.GameID);

        foreach (var id in toRemove)
        {
            simpleGameInfoes.Remove(id);
            games.Remove(id);
        }
    }

    public class SimplifiedGameInfo
    {
        public SimplifiedGameInfo(SimplifiedGameInfoDTO gameInfo)
        {
            GameID = gameInfo.GameID;
            GameState = gameInfo.GameState;
            OtherPlayerName = gameInfo.OtherPlayerName;
            OtherPlayerAvatar = gameInfo.OtherPlayerAvatar;
            MyTurn = gameInfo.MyTurn;
            MyScore = gameInfo.MyScore;
            TheirScore = gameInfo.TheirScore;
            WinnerOfExpiredGame = gameInfo.WinnerOfExpiredGame;
            ExpiryTime = gameInfo.ExpiryTimeRemaining.HasValue ? DateTime.Now + gameInfo.ExpiryTimeRemaining.Value : default(DateTime?);
            MyTurnEndTime = gameInfo.MyTurnTimeRemaining.HasValue ? DateTime.Now + gameInfo.MyTurnTimeRemaining.Value : default(DateTime?);
            RewardPending = !gameInfo.RewardClaimed;
        }

        public SimplifiedGameInfo(Guid gameID, GameState gameState, string otherPlayerName, AvatarDTO otherPlayerAvatar,
            bool myTurn, byte myScore, byte theirScore, bool winnerOfExpiredGame, DateTime? expiryTime,
            DateTime? myTurnEndTime)
        {
            GameID = gameID;
            GameState = gameState;
            OtherPlayerName = otherPlayerName;
            OtherPlayerAvatar = otherPlayerAvatar;
            MyTurn = myTurn;
            MyScore = myScore;
            TheirScore = theirScore;
            WinnerOfExpiredGame = winnerOfExpiredGame;
            ExpiryTime = expiryTime;
            MyTurnEndTime = myTurnEndTime;
        }

        public Guid GameID { get; }
        public GameState GameState { get; }
        public string OtherPlayerName { get; }
        public AvatarDTO OtherPlayerAvatar { get; }
        public bool MyTurn { get; }
        public byte MyScore { get; }
        public byte TheirScore { get; }
        public bool WinnerOfExpiredGame { get; }
        public DateTime? ExpiryTime { get; }
        public DateTime? MyTurnEndTime { get; }
        public bool RewardPending { get; }
    }
}
