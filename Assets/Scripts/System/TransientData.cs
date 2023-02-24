using Network;
using Network.Types;
using System;
using System.Collections.Generic;
using System.Linq;

class TransientData
{
    static TransientData instance;

    public static TransientData Instance => instance ?? (instance = new TransientData());

    bool initialized = false;

    public ConfigValuesDTO ConfigValues { get; set; }

    public ChangeNotifier<string> UserName { get; } = new ChangeNotifier<string>();
    public ChangeNotifier<string> EmailAddress { get; } = new ChangeNotifier<string>();
    public ChangeNotifier<uint> Level { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<uint> NotifiedLevel { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<uint> XP { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<uint> NextLevelXPThreshold { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<uint> Score { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<uint> Rank { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<ulong> Gold { get; } = new ChangeNotifier<ulong>();

    public ChangeNotifier<uint> CurrentNumRoundsWonForReward { get; } = new ChangeNotifier<uint>();
    public ChangeNotifier<DateTime> NextRoundWinRewardTime { get; } = new ChangeNotifier<DateTime>();

    public ChangeNotifier<DateTime?> UpgradedActiveGameLimitEndTime { get; } = new ChangeNotifier<DateTime?>();

    public ChangeNotifier<RegistrationStatus> RegistrationStatus { get; } = new ChangeNotifier<RegistrationStatus>();

    public ChangeNotifier<bool> NotificationsEnabled { get; } = new ChangeNotifier<bool>();
    public ChangeNotifier<bool?> CoinRewardVideoNotificationsEnabled { get; } = new ChangeNotifier<bool?>();

    public ChangeNotifier<ulong> TutorialProgress { get; } = new ChangeNotifier<ulong>();

    public Dictionary<(Statistics statistic, int parameter), ulong> StatisticsValues { get; } = new Dictionary<(Statistics statistic, int parameter), ulong>();

    public List<CoinGiftInfoDTO> CoinGifts { get; private set; }

    public IReadOnlyDictionary<AvatarPartDTO, AvatarPartConfigDTO> AvatarParts { get; private set; }
    public ChangeNotifier<AvatarDTO> Avatar { get; private set; } = new ChangeNotifier<AvatarDTO>();
    public HashSet<AvatarPartDTO> OwnedAvatarParts { get; private set; }

    public string InviteCode { get; private set; }
    public ChangeNotifier<bool> InviteCodeEntered { get; private set; } = new ChangeNotifier<bool>();

    public ChangeNotifier<VideoAdTrackerInfo> CoinRewardVideoTracker { get; } = new ChangeNotifier<VideoAdTrackerInfo>();
    public ChangeNotifier<VideoAdTrackerInfo> GetCategoryAnswersVideoTracker { get; } = new ChangeNotifier<VideoAdTrackerInfo>();

    public void Initialize()
    {
        if (initialized)
            return;

        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();

        ep.NumRoundsWonForRewardUpdated += OnNumRoundsWonForRewardUpdated;
        ep.StatisticUpdated += OnStatisticUpdated;
        ep.CoinGiftReceived += OnCoinGiftReceived;

        initialized = true;
    }

    private void OnCoinGiftReceived(CoinGiftInfoDTO gift)
    {
        if (!CoinGifts.Any(g => g.GiftID == gift.GiftID))
            CoinGifts.Add(gift);
    }

    public void InitializeData(OwnPlayerInfoDTO info, ConfigValuesDTO configData, VideoAdTrackerInfoDTO coinRewardVideo,
        VideoAdTrackerInfoDTO getCategoryAnswersVideo, IEnumerable<CoinGiftInfoDTO> coinGifts, IEnumerable<AvatarPartConfigDTO> avatarParts)
    {
        if (!initialized)
            throw new InvalidOperationException("Must initialize TransientData before initializing data");

        using (ChangeNotifier.BeginBatch())
        {
            ConfigValues = configData;
            UserName.Value = info.Name;
            EmailAddress.Value = info.Email;
            Level.Value = info.Level;
            XP.Value = info.XP;
            NextLevelXPThreshold.Value = info.NextLevelXPThreshold;
            Score.Value = info.Score;
            Rank.Value = info.Rank;
            Gold.Value = info.Gold;
            CurrentNumRoundsWonForReward.Value = info.CurrentNumRoundsWonForReward;
            NextRoundWinRewardTime.Value = DateTime.Now + info.NextRoundWinRewardTimeRemaining;
            UpgradedActiveGameLimitEndTime.Value = info.UpgradedActiveGameLimitTimeRemaining.HasValue ? DateTime.Now + info.UpgradedActiveGameLimitTimeRemaining.Value : default(DateTime?);
            NotificationsEnabled.Value = info.NotificationsEnabled;
            CoinRewardVideoNotificationsEnabled.Value = info.CoinRewardVideoNotificationsEnabled;
            RegistrationStatus.Value = info.RegistrationStatus;
            TutorialProgress.Value = info.TutorialProgress;
            Avatar.Value = info.Avatar;
            OwnedAvatarParts = new HashSet<AvatarPartDTO>(info.OwnedAvatarParts, new AvatarPartDTOEqualityComparer());
            InviteCode = info.InviteCode;
            InviteCodeEntered.Value = info.InviteCodeEntered;
            NotifiedLevel.Value = info.NotifiedLevel;

            CoinRewardVideoTracker.Value = new VideoAdTrackerInfo(coinRewardVideo);
            GetCategoryAnswersVideoTracker.Value = new VideoAdTrackerInfo(getCategoryAnswersVideo);

            CoinGifts = coinGifts.ToList();

            AvatarParts = avatarParts.ToDictionary(a => new AvatarPartDTO(a.PartType, a.ID), new AvatarPartDTOEqualityComparer());

            StatisticsValues.Clear();
            foreach (var stat in info.StatisticsValues)
                StatisticsValues[(stat.Statistic, stat.Parameter)] = stat.Value;
        }
    }

    private void OnStatisticUpdated(StatisticValueDTO stat) => StatisticsValues[(stat.Statistic, stat.Parameter)] = stat.Value;

    private void OnNumRoundsWonForRewardUpdated(uint totalRoundsWon) => CurrentNumRoundsWonForReward.Value = totalRoundsWon;

    public ulong GetStatisticValue(Statistics statistic) =>
        GetStatisticValue(statistic, 0);

    public ulong GetStatisticValue(Statistics statistic, int parameter) =>
        StatisticsValues.TryGetValue((statistic, parameter), out var value) ? value : 0UL;

    class AvatarPartDTOEqualityComparer : IEqualityComparer<AvatarPartDTO>
    {
        public bool Equals(AvatarPartDTO x, AvatarPartDTO y) => x.ID == y.ID && x.PartType == y.PartType;

        public int GetHashCode(AvatarPartDTO obj)
        {
            unchecked
            {
                var hash = 17;
                hash += hash * 31 + obj.ID.GetHashCode();
                hash += hash * 31 + obj.PartType.GetHashCode();
                return hash;
            }
        }
    }
}
