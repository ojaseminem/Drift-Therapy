using System;
using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Device-local top-10 "TOP RUNS" list, backed by
    /// <c>GameApp.Data.leaderboardEntries</c>. Sorted-insert (O(n), no LINQ
    /// re-sort) so a submit — a rare, once-per-run event — stays cheap and
    /// allocation-light.
    /// </summary>
    public class LocalLeaderboardService : ILeaderboardService
    {
        const int MaxEntries = 10;

        public bool IsOnline => false;

        public void Submit(float distanceMeters, string vehicleId)
        {
            var app = GameApp.Instance;
            if (app == null) return;

            var list = app.Data.leaderboardEntries;

            int insertAt = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (distanceMeters > list[i].distanceMeters)
                {
                    insertAt = i;
                    break;
                }
            }

            if (insertAt >= MaxEntries) return; // wouldn't make the top list at all

            list.Insert(insertAt, new LeaderboardEntry
            {
                distanceMeters = distanceMeters,
                dateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                vehicleId = vehicleId,
            });

            while (list.Count > MaxEntries)
            {
                list.RemoveAt(list.Count - 1);
            }

            app.Save();
        }

        public IReadOnlyList<LeaderboardEntry> GetTop(int count)
        {
            var app = GameApp.Instance;
            if (app == null) return Array.Empty<LeaderboardEntry>();

            var list = app.Data.leaderboardEntries;
            if (count >= list.Count) return list;
            return list.GetRange(0, Mathf.Max(0, count));
        }
    }
}
