using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network.Types;

// If the game is kept open past midnight (server time), we will fall out of sync.
// However, we'd be more strict and show less ads, which is fine.
public class VideoAdTrackerInfo
{
    public VideoAdTrackerInfo(VideoAdTrackerInfoDTO trackerInfoDTO)
    {
        LastWatchedTime = trackerInfoDTO.TimeSinceLastWatched.HasValue ? DateTime.Now - trackerInfoDTO.TimeSinceLastWatched.Value : default(DateTime?);
        NumberWatchedToday = trackerInfoDTO.NumberWatchedToday;
        Interval = trackerInfoDTO.Interval;
        NumberPerDay = trackerInfoDTO.NumberPerDay;
    }

    VideoAdTrackerInfo(DateTime? lastWatchedTime, uint numberWatchedToday, TimeSpan interval, uint numberPerDay)
    {
        LastWatchedTime = lastWatchedTime;
        NumberWatchedToday = numberWatchedToday;
        Interval = interval;
        NumberPerDay = numberPerDay;
    }

    public DateTime? LastWatchedTime { get; }
    public uint NumberWatchedToday { get; }
    public TimeSpan Interval { get; }
    public uint NumberPerDay { get; }

    public bool CanWatchNow()
    {
        var now = DateTime.Now;

        if (Interval > TimeSpan.Zero &&
            LastWatchedTime.HasValue &&
            now - LastWatchedTime.Value < Interval)
            return false;

        if (NumberPerDay > 0 && NumberWatchedToday >= NumberPerDay)
            return false;

        return true;
    }

    public (bool mustWaitUntilTomorrow, TimeSpan? remainingTime) GetUnavailableReason()
    {
        var mustWait = NumberPerDay > 0 && NumberWatchedToday >= NumberPerDay;

        TimeSpan? remaining;
        if (Interval <= TimeSpan.Zero)
            remaining = default;
        else if (!LastWatchedTime.HasValue || DateTime.Now - LastWatchedTime.Value >= Interval)
            remaining = TimeSpan.Zero;
        else
            remaining = Interval - (DateTime.Now - LastWatchedTime.Value);

        return (mustWait, remaining);
    }

    public VideoAdTrackerInfo WatchOnce() =>
        new VideoAdTrackerInfo(
            DateTime.Now,
            NumberWatchedToday + 1,
            Interval, NumberPerDay
            );
}
