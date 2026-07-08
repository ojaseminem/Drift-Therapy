using System;
using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>Result of the most recent run, shown on the end screen.</summary>
    public struct RunResult
    {
        public float distanceMeters;
        public float bestDistanceMeters;
        public int score;
        public int best;
        public int coins;
        public int xp;
        public bool isNewBest;
    }

    /// <summary>Progress/claim state for one <see cref="MissionDef"/>, keyed by id.</summary>
    [Serializable]
    public class MissionState
    {
        public string id;
        public int progress;
        public bool claimed;
    }

    /// <summary>One saved leaderboard entry (see <see cref="LeaderboardEntry"/>).</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public float distanceMeters;
        public string dateUtc;
        public string vehicleId;
    }

    /// <summary>Persistent player save model (JSON via PlayerPrefs).</summary>
    [Serializable]
    public class PlayerData
    {
        public int coins;
        public int gems;
        public int keys;
        public int xp;
        public int level = 1;
        public int highScore;
        public float bestDistanceMeters;
        public string selectedVehicleId = "";
        public List<string> ownedVehicleIds = new List<string>();
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool haptics = true;
        public bool welcomed;
        public bool removeAdsOwned;
        public bool onboardingSeen;

        // ── Missions / trials / daily streak ────────────────────────────────
        public List<MissionState> missionStates = new List<MissionState>();
        public string dailyChallengeId = "";
        public int dailyChallengeProgress;
        public bool dailyChallengeClaimed;
        /// <summary>yyyy-MM-dd, device-local date the active daily was rolled.</summary>
        public string dailyChallengeDateUtc = "";
        public int loginStreak;
        public string lastLoginDateUtc = "";

        // ── Leaderboard (local top runs; see LeaderboardProvider) ───────────
        public List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Persistent meta-game service (wallet, profile/progression, vehicle ownership)
    /// shared across the Menu and Game scenes. Single instance, survives scene loads,
    /// raises <see cref="Changed"/> so UI can refresh. Save is JSON in PlayerPrefs.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameApp : MonoBehaviour
    {
        const string SaveKey = "dt_player_v1";

        public static GameApp Instance { get; private set; }

        [Tooltip("All vehicles in the catalog. The first is the default/free car.")]
        [SerializeField] VehicleDef[] vehicles;

        [Tooltip("All missions/trials/daily-pool entries in the catalog.")]
        [SerializeField] MissionDef[] missions;

        public PlayerData Data { get; private set; }
        public IReadOnlyList<VehicleDef> Vehicles => vehicles;
        public IReadOnlyList<MissionDef> Missions => missions;

        /// <summary>Raised whenever wallet / profile / ownership changes.</summary>
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();

            // One-time welcome grant so the garage/economy is usable from the start.
            if (!Data.welcomed)
            {
                Data.welcomed = true;
                Data.coins += 1500;
                Data.gems += 20;
                Save();
            }

            PlatformServices.Init();
        }

        // ── Persistence ──────────────────────────────────────────────────────
        void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            Data = string.IsNullOrEmpty(json) ? new PlayerData() : JsonUtility.FromJson<PlayerData>(json);
            EnsureDefaults();
            if (Data.bestDistanceMeters <= 0f && Data.highScore > 0)
            {
                Data.bestDistanceMeters = Data.highScore;
            }
        }

        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        void EnsureDefaults()
        {
            if (Data.ownedVehicleIds == null) Data.ownedVehicleIds = new List<string>();

            // Grant any default-owned vehicles.
            if (vehicles != null)
                foreach (var v in vehicles)
                    if (v != null && v.ownedByDefault && !Data.ownedVehicleIds.Contains(v.id))
                        Data.ownedVehicleIds.Add(v.id);

            // Always own at least the first catalog entry.
            if (Data.ownedVehicleIds.Count == 0 && vehicles != null && vehicles.Length > 0)
                Data.ownedVehicleIds.Add(vehicles[0].id);

            if (string.IsNullOrEmpty(Data.selectedVehicleId) ||
                !Data.ownedVehicleIds.Contains(Data.selectedVehicleId))
                Data.selectedVehicleId = Data.ownedVehicleIds.Count > 0 ? Data.ownedVehicleIds[0] : "";

            EnsureDailyChallenge();
        }

        /// <summary>
        /// Rolls a new daily challenge (from the <see cref="MissionScope.Daily"/>
        /// pool) whenever the device-local date has advanced past
        /// <see cref="PlayerData.dailyChallengeDateUtc"/>, and updates the login
        /// streak based on whether yesterday's daily was claimed. Device-clock
        /// based (no server authority) — acceptable for this game's scope.
        /// </summary>
        void EnsureDailyChallenge()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.dailyChallengeDateUtc == today)
            {
                return;
            }

            bool hadPreviousDaily = !string.IsNullOrEmpty(Data.dailyChallengeDateUtc);
            bool previousWasYesterday = hadPreviousDaily &&
                Data.dailyChallengeDateUtc == DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

            if (!hadPreviousDaily)
            {
                Data.loginStreak = 1;
            }
            else if (previousWasYesterday && Data.dailyChallengeClaimed)
            {
                Data.loginStreak++;
            }
            else if (!previousWasYesterday)
            {
                Data.loginStreak = 1;
            }
            // else: same-streak-window but unclaimed yesterday — streak resets to 0 (missed day).
            else
            {
                Data.loginStreak = 0;
            }

            var pool = new List<MissionDef>();
            if (missions != null)
                foreach (var m in missions)
                    if (m != null && m.scope == MissionScope.Daily)
                        pool.Add(m);

            Data.dailyChallengeId = pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)].id : "";
            Data.dailyChallengeProgress = 0;
            Data.dailyChallengeClaimed = false;
            Data.dailyChallengeDateUtc = today;
            Data.lastLoginDateUtc = today;
        }

        // ── Vehicles ─────────────────────────────────────────────────────────
        public VehicleDef GetVehicle(string id)
        {
            if (vehicles == null) return null;
            foreach (var v in vehicles) if (v != null && v.id == id) return v;
            return vehicles.Length > 0 ? vehicles[0] : null;
        }

        public VehicleDef Selected => GetVehicle(Data.selectedVehicleId);
        public bool Owns(string id) => Data.ownedVehicleIds.Contains(id);

        public bool TryBuy(VehicleDef v)
        {
            if (v == null || Owns(v.id)) return false;
            if (!TrySpendCoins(v.price)) return false;
            Data.ownedVehicleIds.Add(v.id);
            Save();
            Changed?.Invoke();
            return true;
        }

        public void Select(string id)
        {
            if (!Owns(id)) return;
            Data.selectedVehicleId = id;
            Save();
            Changed?.Invoke();
        }

        // ── Wallet ───────────────────────────────────────────────────────────
        public void AddCoins(int amount) { Data.coins = Mathf.Max(0, Data.coins + amount); Changed?.Invoke(); }
        public void AddGems(int amount)  { Data.gems  = Mathf.Max(0, Data.gems  + amount); Changed?.Invoke(); }
        public void AddKeys(int amount)  { Data.keys  = Mathf.Max(0, Data.keys  + amount); Changed?.Invoke(); }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            Changed?.Invoke();
            return true;
        }

        // ── Settings ─────────────────────────────────────────────────────────
        public void SetMusicVolume(float v) { Data.musicVolume = Mathf.Clamp01(v); Save(); Changed?.Invoke(); }
        public void SetSfxVolume(float v) { Data.sfxVolume = Mathf.Clamp01(v); Save(); Changed?.Invoke(); }
        public void SetHaptics(bool on) { Data.haptics = on; Save(); Changed?.Invoke(); }

        /// <summary>Called once, the first time the onboarding popup is dismissed.</summary>
        public void MarkOnboardingSeen()
        {
            if (Data.onboardingSeen) return;
            Data.onboardingSeen = true;
            Save();
            Changed?.Invoke();
        }

        /// <summary>Grants the remove-ads entitlement (called by IAPService on purchase/restore).</summary>
        public void SetRemoveAdsOwned(bool owned)
        {
            if (Data.removeAdsOwned == owned) return;
            Data.removeAdsOwned = owned;
            Save();
            Changed?.Invoke();
        }

        // ── Progression ──────────────────────────────────────────────────────
        public int XpForLevel(int level) => 100 + Mathf.Max(0, level - 1) * 60;
        public int XpInLevel  => Data.xp;
        public int XpToLevel  => XpForLevel(Data.level);

        public void AddXp(int amount)
        {
            Data.xp += Mathf.Max(0, amount);
            while (Data.xp >= XpForLevel(Data.level))
            {
                Data.xp -= XpForLevel(Data.level);
                Data.level++;
            }
            Changed?.Invoke();
        }

        /// <summary>Most recent finished run (for the end screen).</summary>
        public RunResult LastRun { get; private set; }

        /// <summary>Apply the results of a finished run, store the summary, and persist.</summary>
        public void SubmitRun(float distanceMeters, int coinsEarned, int xpEarned)
        {
            float previousBestDistance = Mathf.Max(0f, Data.bestDistanceMeters);
            bool newBest = distanceMeters > previousBestDistance;
            if (newBest)
            {
                Data.bestDistanceMeters = distanceMeters;
            }

            Data.highScore = Mathf.Max(Data.highScore, Mathf.RoundToInt(Data.bestDistanceMeters));
            AddCoins(coinsEarned);
            AddXp(xpEarned);
            Save();
            LastRun = new RunResult
            {
                distanceMeters = distanceMeters,
                bestDistanceMeters = Data.bestDistanceMeters,
                score = Mathf.RoundToInt(distanceMeters),
                best = Mathf.RoundToInt(Data.bestDistanceMeters),
                coins = coinsEarned, xp = xpEarned, isNewBest = newBest
            };
        }

        // ── Missions / trials / daily challenge ─────────────────────────────
        public MissionDef GetMissionDef(string id)
        {
            if (missions == null || string.IsNullOrEmpty(id)) return null;
            foreach (var m in missions) if (m != null && m.id == id) return m;
            return null;
        }

        MissionState GetOrCreateMissionState(string id)
        {
            for (int i = 0; i < Data.missionStates.Count; i++)
                if (Data.missionStates[i].id == id) return Data.missionStates[i];

            var state = new MissionState { id = id };
            Data.missionStates.Add(state);
            return state;
        }

        /// <summary>Current progress toward <paramref name="id"/>'s target (daily challenge or catalog mission).</summary>
        public int GetProgress(string id)
        {
            if (!string.IsNullOrEmpty(id) && id == Data.dailyChallengeId) return Data.dailyChallengeProgress;
            for (int i = 0; i < Data.missionStates.Count; i++)
                if (Data.missionStates[i].id == id) return Data.missionStates[i].progress;
            return 0;
        }

        public bool IsMissionClaimed(string id)
        {
            if (!string.IsNullOrEmpty(id) && id == Data.dailyChallengeId) return Data.dailyChallengeClaimed;
            for (int i = 0; i < Data.missionStates.Count; i++)
                if (Data.missionStates[i].id == id) return Data.missionStates[i].claimed;
            return false;
        }

        /// <summary>
        /// Increments every unclaimed <see cref="MissionScope.Cumulative"/> mission and
        /// the active daily challenge (if either tracks <paramref name="metric"/>), clamped
        /// at each definition's target. Cheap (loops the small mission catalog, no
        /// allocation) — safe to call once per run end.
        /// </summary>
        public void ReportMetric(MissionMetric metric, int amount)
        {
            if (amount == 0 || missions == null) return;
            bool changed = false;

            for (int i = 0; i < missions.Length; i++)
            {
                var def = missions[i];
                if (def == null || def.scope != MissionScope.Cumulative || def.metric != metric) continue;

                var state = GetOrCreateMissionState(def.id);
                if (state.claimed) continue;

                int next = Mathf.Min(def.targetValue, state.progress + amount);
                if (next == state.progress) continue;
                state.progress = next;
                changed = true;
            }

            if (!string.IsNullOrEmpty(Data.dailyChallengeId) && !Data.dailyChallengeClaimed)
            {
                var daily = GetMissionDef(Data.dailyChallengeId);
                if (daily != null && daily.metric == metric)
                {
                    int next = Mathf.Min(daily.targetValue, Data.dailyChallengeProgress + amount);
                    if (next != Data.dailyChallengeProgress)
                    {
                        Data.dailyChallengeProgress = next;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Save();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Updates every unclaimed <see cref="MissionScope.PerRunTrial"/> mission with the
        /// best (max) value seen so far, clamped at target. Call once per run end with the
        /// run's peak value for <paramref name="metric"/> — never per-frame.
        /// </summary>
        public void ReportRunTrialPeak(MissionMetric metric, int peakValueThisRun)
        {
            if (peakValueThisRun <= 0 || missions == null) return;
            bool changed = false;

            for (int i = 0; i < missions.Length; i++)
            {
                var def = missions[i];
                if (def == null || def.scope != MissionScope.PerRunTrial || def.metric != metric) continue;

                var state = GetOrCreateMissionState(def.id);
                if (state.claimed) continue;

                int next = Mathf.Min(def.targetValue, Mathf.Max(state.progress, peakValueThisRun));
                if (next == state.progress) continue;
                state.progress = next;
                changed = true;
            }

            if (changed)
            {
                Save();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Pays out a completed mission/trial/daily-challenge reward exactly once.
        /// Reads the claimed flag first, so a double-tap or reopened popup can't
        /// double-pay. Returns false if not found, not complete, or already claimed.
        /// </summary>
        public bool ClaimMission(string id)
        {
            var def = GetMissionDef(id);
            if (def == null) return false;

            bool isDaily = id == Data.dailyChallengeId;
            if (isDaily)
            {
                if (Data.dailyChallengeClaimed || Data.dailyChallengeProgress < def.targetValue) return false;
                Data.dailyChallengeClaimed = true;
            }
            else
            {
                var state = GetOrCreateMissionState(id);
                if (state.claimed || state.progress < def.targetValue) return false;
                state.claimed = true;
            }

            if (def.rewardCoins > 0) AddCoins(def.rewardCoins);
            if (def.rewardGems > 0) AddGems(def.rewardGems);
            if (def.rewardXp > 0) AddXp(def.rewardXp);

            if (!string.IsNullOrEmpty(def.achievementId))
            {
                // TODO(PlayGames): unlock via a real GPGS-backed PlatformServices.PlayGames
                // once installed. Keep Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync.
                PlatformServices.PlayGames.UnlockAchievement(def.achievementId);
            }

            Save();
            Changed?.Invoke();
            return true;
        }
    }
}
