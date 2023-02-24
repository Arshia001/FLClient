#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define WITH_TAPSELL
#endif

#if WITH_TAPSELL
#pragma warning disable CS0414
#endif

using System;
using System.Threading;
using System.Threading.Tasks;
using GameAnalyticsSDK;
using Network;
using TapsellPlusSDK;
using UnityEngine;

public class AdRepository : SingletonBehaviour<AdRepository>
{
#if WITH_TAPSELL
    const string TapsellSecret = "lobilrnmqsneaitqhsesfjprkscssqgimedcorfhepsbpmheqfrlrposajfdedaonobprh";
#endif

    public enum AdZone
    {
        SubjectSelection,
        GameOverview,
        ExitConfirmation,
        CoinReward,
        GetCategoryAnswers
    }

    const string SubjectSelectionZoneID = "5e3c09fe57eacf0001696e4f";
    const string GameOverviewZoneID = "5e3c0a1657eacf0001696e50";
    const string ExitConfirmationZoneID = "5e3c0a258a02410001446d8a";
    const string CoinRewardZoneID = "5e3c0a3e57eacf0001696e51";
    const string GetCategoryAnswersZoneID = "5e7f0ea80f2eb80001aa7712";

    [SerializeField] bool debug_EditorAdsAvailable = true;

    protected override bool IsGlobal => true;

    public bool AdsEnabled { get; set; } = true;

    abstract class ZoneData
    {
        public string ZoneID { get; private set; }

        public ZoneData(string zoneID) => ZoneID = zoneID;

        public abstract bool IsAdAvailable();
    }

    class VideoZoneData : ZoneData
    {
        public bool AdAvailable { get; set; }
        public TaskCompletionSource<bool> ShowAdTcs { get; set; }

        public VideoZoneData(string zoneID) : base(zoneID) { }

        public override bool IsAdAvailable() => AdAvailable;
    }

    class NativeBannerZoneData : ZoneData
    {
        public TapsellPlusNativeBannerAd Ad { get; set; }

        public NativeBannerZoneData(string zoneID) : base(zoneID) { }

        public override bool IsAdAvailable() => Ad != null;
    }


    VideoZoneData CoinRewardZone;
    VideoZoneData GetCategoryAnswersZone;
    NativeBannerZoneData GameOverviewZone;
    NativeBannerZoneData SubjectSelectionZone;
    NativeBannerZoneData ExitConfirmationZone;


    ZoneData GetZone(string zoneID)
    {
        switch (zoneID)
        {
            case SubjectSelectionZoneID:
                return SubjectSelectionZone;
            case GameOverviewZoneID:
                return GameOverviewZone;
            case ExitConfirmationZoneID:
                return ExitConfirmationZone;
            case CoinRewardZoneID:
                return CoinRewardZone;
            case GetCategoryAnswersZoneID:
                return GetCategoryAnswersZone;
            default:
                throw new Exception($"Unknown ad zone ID {zoneID}");
        }
    }

    AdZone GetZoneName(string zoneID)
    {
        switch (zoneID)
        {
            case SubjectSelectionZoneID:
                return AdZone.SubjectSelection;
            case GameOverviewZoneID:
                return AdZone.GameOverview;
            case ExitConfirmationZoneID:
                return AdZone.ExitConfirmation;
            case CoinRewardZoneID:
                return AdZone.CoinReward;
            case GetCategoryAnswersZoneID:
                return AdZone.GetCategoryAnswers;
            default:
                throw new Exception($"Unknown ad zone ID {zoneID}");
        }
    }

    ZoneData GetZone(AdZone zone)
    {
        switch (zone)
        {
            case AdZone.SubjectSelection:
                return SubjectSelectionZone;
            case AdZone.GameOverview:
                return GameOverviewZone;
            case AdZone.ExitConfirmation:
                return ExitConfirmationZone;
            case AdZone.CoinReward:
                return CoinRewardZone;
            case AdZone.GetCategoryAnswers:
                return GetCategoryAnswersZone;
            default:
                throw new Exception($"Unknown ad zone {zone}");
        }
    }

    ChangeNotifier<VideoAdTrackerInfo> GetVideoAdTracker(AdZone zone)
    {
        switch (zone)
        {
            case AdZone.CoinReward:
                return TransientData.Instance.CoinRewardVideoTracker;
            case AdZone.GetCategoryAnswers:
                return TransientData.Instance.GetCategoryAnswersVideoTracker;
            default:
                throw new Exception($"Unknown ad zone or not a video zone {zone}");
        }
    }

    void Start()
    {
#if WITH_TAPSELL
        TapsellPlus.initialize(TapsellSecret);
#endif

        SubjectSelectionZone = new NativeBannerZoneData(SubjectSelectionZoneID);
        GameOverviewZone = new NativeBannerZoneData(GameOverviewZoneID);
        ExitConfirmationZone = new NativeBannerZoneData(ExitConfirmationZoneID);
        CoinRewardZone = new VideoZoneData(CoinRewardZoneID);
        GetCategoryAnswersZone = new VideoZoneData(GetCategoryAnswersZoneID);


#if WITH_TAPSELL
        RequestVideoAd(CoinRewardZone);
        RequestVideoAd(GetCategoryAnswersZone);

        RequestNativeBannerAd(SubjectSelectionZone);
        RequestNativeBannerAd(GameOverviewZone);
        RequestNativeBannerAd(ExitConfirmationZone);
#endif
    }

    public bool IsAdAvailable(AdZone adZone, bool logToAnalytics = false)
    {
        if (!AdsEnabled)
            return false;

        var zone = GetZone(adZone);

        if (zone is VideoZoneData && !GetVideoAdTracker(adZone).Value.CanWatchNow())
            return false;

#if WITH_TAPSELL
        var result = zone.IsAdAvailable();

        if (logToAnalytics)
        {
            if (result)
                LogAdClickedToAnalytics(adZone);
            else
                LogAdUnavailableToAnalytics(adZone);
        }

        return result;
#else
        // To facilitate testing ad-related stuff in the editor
        return debug_EditorAdsAvailable;
#endif
    }

    public void LogAdClickedToAnalytics(AdZone adZone) =>
        GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.RewardedVideo, "tapsell", adZone.ToString(), GAAdError.Unknown);

    public void LogAdUnavailableToAnalytics(AdZone adZone) =>
        GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "tapsell", adZone.ToString(), GAAdError.NoFill);

    public Task<bool> StartRewardedVideoAd(AdZone adZone)
    {
        if (!IsAdAvailable(adZone, false) || !(GetZone(adZone) is VideoZoneData zone) || zone.ShowAdTcs != null)
            return Task.FromCanceled<bool>(new CancellationToken(true));

#if WITH_TAPSELL
        return MainThreadDispatcher.Instance.EnqueueTask(async () =>
        {
            ConnectionManager.Instance.DelayKeepAlive(TimeSpan.FromMinutes(15));

            TapsellPlus.showAd(zone.ZoneID, OnAdShown, OnAdClosed, OnAdRewarded, OnShowAdError);

            zone.AdAvailable = false;

            zone.ShowAdTcs = new TaskCompletionSource<bool>();

            var result = await zone.ShowAdTcs.Task;

            ConnectionManager.Instance.ResetKeepAlive();

            return result;
        });
#else
        var tracker = GetVideoAdTracker(adZone);
        tracker.Value = tracker.Value.WatchOnce();
        return Task.FromResult(true);
#endif
    }

    void OnAdShown(string zoneID)
    {
        Debug.Log($"Ad shown: {zoneID}");
    }

    void OnAdClosed(string zoneID)
    {
        Debug.Log($"Ad closed: {zoneID}");

        var zone = GetZone(zoneID) as VideoZoneData;
        RequestVideoAd(zone);

        if (zone.ShowAdTcs != null)
        {
            GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "tapsell", GetZoneName(zoneID).ToString(), GAAdError.InvalidRequest);

            zone.ShowAdTcs.SetResult(false);
            zone.ShowAdTcs = null;
        }
    }

    void OnAdRewarded(string zoneID)
    {
        Debug.Log($"Ad rewarded: {zoneID}");

        var zoneName = GetZoneName(zoneID);

        GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, "tapsell", zoneName.ToString(), GAAdError.Unknown);

        var zone = GetZone(zoneID) as VideoZoneData;
        RequestVideoAd(zone);

        zone.ShowAdTcs?.SetResult(true);
        zone.ShowAdTcs = null;

        var tracker = GetVideoAdTracker(zoneName);
        tracker.Value = tracker.Value.WatchOnce();
    }

    void OnShowAdError(TapsellError error)
    {
        Debug.LogError($"Failed to show ad for {error.zoneId} due to {error.message}");

        TaskExtensions.RunIgnoreAsync(async () =>
        {
            await Task.Delay(5000);

            var zone = GetZone(error.zoneId) as VideoZoneData;
            RequestVideoAd(zone);
        });
    }

    void RequestVideoAd(VideoZoneData zone) =>
        MainThreadDispatcher.Instance.Enqueue(() =>
            TapsellPlus.requestRewardedVideo(zone.ZoneID, OnVideoAdAvailable, OnRequestVideoAdError)
        );

    void OnVideoAdAvailable(string zoneID)
    {
        Debug.Log($"Video ad available: {zoneID}");

        (GetZone(zoneID) as VideoZoneData).AdAvailable = true;
    }

    void OnRequestVideoAdError(TapsellError error)
    {
        Debug.LogError($"Failed to request video ad for {error.zoneId} due to {error.message}");

        TaskExtensions.RunIgnoreAsync(async () =>
        {
            await Task.Delay(5000);

            var zone = GetZone(error.zoneId) as VideoZoneData;
            RequestVideoAd(zone);
        });
    }

    public TapsellPlusNativeBannerAd GetNativeBanner(AdZone adZone) => (GetZone(adZone) as NativeBannerZoneData)?.Ad;

    public void NativeBannerClicked(AdZone adZone)
    {
        GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Banner, "tapsell", adZone.ToString());
        var zone = GetZone(adZone) as NativeBannerZoneData;
        zone.Ad?.clicked();
        RequestNativeBannerAd(zone);
    }

    void RequestNativeBannerAd(NativeBannerZoneData zone)
    {
        GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.Banner, "tapsell", GetZoneName(zone.ZoneID).ToString());
        MainThreadDispatcher.Instance.Enqueue(() =>
            TapsellPlus.requestNativeBanner(this, zone.ZoneID, OnNativeBannerAdAvailable, OnNativeBannerAdError)
            );
    }

    void OnNativeBannerAdAvailable(TapsellPlusNativeBannerAd ad)
    {
        Debug.Log($"Native banner ad available: {ad.zoneId}");

        GameAnalytics.NewAdEvent(GAAdAction.Loaded, GAAdType.Banner, "tapsell", GetZoneName(ad.zoneId).ToString());
        (GetZone(ad.zoneId) as NativeBannerZoneData).Ad = ad;
    }

    void OnNativeBannerAdError(TapsellError error)
    {
        Debug.LogError($"Failed to request banner ad for {error.zoneId} due to {error.message}");
        GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Banner, "tapsell", GetZoneName(error.zoneId).ToString());

        TaskExtensions.RunIgnoreAsync(async () =>
        {
            await Task.Delay(5000);

            var zone = GetZone(error.zoneId) as NativeBannerZoneData;
            RequestNativeBannerAd(zone);
        });
    }
}
