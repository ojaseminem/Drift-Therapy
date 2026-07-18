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

    /// <summary>Which skin is equipped on one owned vehicle. Keyed by vehicleId.</summary>
    [Serializable]
    public class EquippedSkinState
    {
        public string vehicleId;
        public string skinId;
    }

    /// <summary>Which attachment is equipped in one (vehicleId, slotId) mount slot.</summary>
    [Serializable]
    public class EquippedAttachmentState
    {
        public string vehicleId;
        public string slotId;
        public string attachmentId;
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

        // ── Vehicle skins (colour variants, keyed "vehicleId:skinId") ───────
        public List<string> ownedSkinKeys = new List<string>();
        public List<EquippedSkinState> equippedSkins = new List<EquippedSkinState>();

        // ── Cosmetic attachments (spoilers, underglow, ...) ─────────────────
        public List<string> ownedAttachmentIds = new List<string>();
        public List<EquippedAttachmentState> equippedAttachments = new List<EquippedAttachmentState>();

        // ── Booster/trail VFX skins ──────────────────────────────────────────
        public List<string> ownedBoosterIds = new List<string>();
        public string equippedBoosterId = "";

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

        // ── Play Games achievements (local tracking, mirrors server state) ──
        public List<string> unlockedAchievementIds = new List<string>();

        // ── Ads / lifetime stat counters ─────────────────────────────────────
        public int totalRunsCompleted;
        public float lifetimeDistanceMeters;
        public int totalFenceScreeches;
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

        [Tooltip("All purchasable Drift Coins/Gems packs in the shop catalog.")]
        [SerializeField] CurrencyPackDef[] currencyPacks;

        [Tooltip("All purchasable cosmetic attachments (spoilers, underglow, ...) in the store catalog.")]
        [SerializeField] CosmeticAttachmentDef[] attachments;

        [Tooltip("All purchasable boost-trail color skins in the store catalog.")]
        [SerializeField] BoosterSkinDef[] boosters;

        public PlayerData Data { get; private set; }
        public IReadOnlyList<VehicleDef> Vehicles => vehicles;
        public IReadOnlyList<MissionDef> Missions => missions;
        public IReadOnlyList<CurrencyPackDef> CurrencyPacks => currencyPacks;
        public IReadOnlyList<CosmeticAttachmentDef> Attachments => attachments;
        public IReadOnlyList<BoosterSkinDef> Boosters => boosters;

        /// <summary>Raised whenever wallet / profile / ownership changes.</summary>
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            GrantWelcomeBonusIfNeeded();

            PlatformServices.Init();
        }

        /// <summary>One-time welcome grant so the garage/economy is usable from the start.</summary>
        void GrantWelcomeBonusIfNeeded()
        {
            if (Data.welcomed) return;
            Data.welcomed = true;
            Data.coins += 1500;
            Data.gems += 20;
            Save();
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

        /// <summary>
        /// Wipes the save file (PlayerPrefs) and, if a GameApp is already loaded
        /// this session, resets it back to a fresh-install state in place —
        /// used by the "Drift Therapy/Player Data/Clear Data" editor menu.
        /// </summary>
        public static void ClearSavedData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            if (Instance != null)
            {
                Instance.Data = new PlayerData();
                Instance.EnsureDefaults();
                Instance.GrantWelcomeBonusIfNeeded();
                Instance.Changed?.Invoke();
            }
        }

        void EnsureDefaults()
        {
            if (Data.ownedVehicleIds == null) Data.ownedVehicleIds = new List<string>();
            if (Data.ownedSkinKeys == null) Data.ownedSkinKeys = new List<string>();
            if (Data.equippedSkins == null) Data.equippedSkins = new List<EquippedSkinState>();
            if (Data.ownedAttachmentIds == null) Data.ownedAttachmentIds = new List<string>();
            if (Data.equippedAttachments == null) Data.equippedAttachments = new List<EquippedAttachmentState>();
            if (Data.ownedBoosterIds == null) Data.ownedBoosterIds = new List<string>();
            if (Data.equippedBoosterId == null) Data.equippedBoosterId = "";
            if (Data.unlockedAchievementIds == null) Data.unlockedAchievementIds = new List<string>();

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

            // Grant each vehicle's default skin.
            if (vehicles != null)
                foreach (var v in vehicles)
                {
                    if (v == null || v.skins == null) continue;
                    foreach (var s in v.skins)
                        if (s != null && s.ownedByDefault && !Data.ownedSkinKeys.Contains(SkinKey(v.id, s.id)))
                            Data.ownedSkinKeys.Add(SkinKey(v.id, s.id));
                }

            // Grant any default-owned attachments/boosters.
            if (attachments != null)
                foreach (var a in attachments)
                    if (a != null && a.ownedByDefault && !Data.ownedAttachmentIds.Contains(a.id))
                        Data.ownedAttachmentIds.Add(a.id);

            if (boosters != null)
            {
                foreach (var b in boosters)
                    if (b != null && b.ownedByDefault && !Data.ownedBoosterIds.Contains(b.id))
                        Data.ownedBoosterIds.Add(b.id);

                if (string.IsNullOrEmpty(Data.equippedBoosterId))
                {
                    foreach (var b in boosters)
                        if (b != null && b.ownedByDefault) { Data.equippedBoosterId = b.id; break; }
                }
            }

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

        public bool TryBuy(VehicleDef v) => TryBuy(v, useGems: false);

        /// <summary>Buys with coins (useGems: false) or gems (useGems: true). Returns false if already owned or insufficient balance.</summary>
        public bool TryBuy(VehicleDef v, bool useGems)
        {
            if (v == null || Owns(v.id)) return false;
            bool spent = useGems ? TrySpendGems(v.gemPrice) : TrySpendCoins(v.price);
            if (!spent) return false;
            Data.ownedVehicleIds.Add(v.id);
            Save();
            Changed?.Invoke();

            if (Data.ownedVehicleIds.Count >= 5) TryUnlockAchievement(AchievementIds.GarageCollector);
            if (vehicles != null && vehicles.Length > 0 && Data.ownedVehicleIds.Count >= vehicles.Length) TryUnlockAchievement(AchievementIds.FullHouse);

            return true;
        }

        public void Select(string id)
        {
            if (!Owns(id)) return;
            Data.selectedVehicleId = id;
            Save();
            Changed?.Invoke();
        }

        // ── Vehicle skins ────────────────────────────────────────────────────
        static string SkinKey(string vehicleId, string skinId) => vehicleId + ":" + skinId;

        static VehicleSkinDef DefaultSkin(VehicleDef v)
        {
            if (v == null || v.skins == null || v.skins.Length == 0) return null;
            foreach (var s in v.skins) if (s != null && s.ownedByDefault) return s;
            return v.skins[0];
        }

        public bool OwnsSkin(string vehicleId, string skinId) => Data.ownedSkinKeys.Contains(SkinKey(vehicleId, skinId));

        /// <summary>The id of the skin currently equipped on <paramref name="vehicleId"/>, falling back to its default skin.</summary>
        public string GetEquippedSkinId(string vehicleId)
        {
            for (int i = 0; i < Data.equippedSkins.Count; i++)
                if (Data.equippedSkins[i].vehicleId == vehicleId) return Data.equippedSkins[i].skinId;
            var def = DefaultSkin(GetVehicle(vehicleId));
            return def != null ? def.id : null;
        }

        /// <summary>The colour to render for a vehicle right now — its equipped skin, or bodyColor if it has no skins defined.</summary>
        public Color GetEquippedColor(string vehicleId)
        {
            var v = GetVehicle(vehicleId);
            if (v == null) return Color.white;
            string skinId = GetEquippedSkinId(vehicleId);
            if (v.skins != null)
                foreach (var s in v.skins)
                    if (s != null && s.id == skinId) return s.color;
            return v.bodyColor;
        }

        /// <summary>Buys a skin with coins (useGems: false) or gems (useGems: true). Returns false if already owned or insufficient balance.</summary>
        public bool TryBuySkin(VehicleDef v, VehicleSkinDef skin, bool useGems)
        {
            if (v == null || skin == null || OwnsSkin(v.id, skin.id)) return false;
            bool spent = useGems ? TrySpendGems(skin.gemPrice) : TrySpendCoins(skin.price);
            if (!spent) return false;
            Data.ownedSkinKeys.Add(SkinKey(v.id, skin.id));
            Save();
            Changed?.Invoke();
            if (Data.ownedSkinKeys.Count >= 5) TryUnlockAchievement(AchievementIds.Fashionista);
            return true;
        }

        public void SelectSkin(string vehicleId, string skinId)
        {
            if (!OwnsSkin(vehicleId, skinId)) return;
            for (int i = 0; i < Data.equippedSkins.Count; i++)
            {
                if (Data.equippedSkins[i].vehicleId == vehicleId)
                {
                    Data.equippedSkins[i].skinId = skinId;
                    Save();
                    Changed?.Invoke();
                    return;
                }
            }
            Data.equippedSkins.Add(new EquippedSkinState { vehicleId = vehicleId, skinId = skinId });
            Save();
            Changed?.Invoke();
        }

        // ── Cosmetic attachments (spoilers, underglow, ...) ──────────────────
        public CosmeticAttachmentDef GetAttachmentDef(string id)
        {
            if (attachments == null || string.IsNullOrEmpty(id)) return null;
            foreach (var a in attachments) if (a != null && a.id == id) return a;
            return null;
        }

        public bool OwnsAttachment(string id) => Data.ownedAttachmentIds.Contains(id);

        /// <summary>Buys an attachment with coins (useGems: false) or gems (useGems: true). Returns false if already owned or insufficient balance.</summary>
        public bool TryBuyAttachment(CosmeticAttachmentDef def, bool useGems)
        {
            if (def == null || OwnsAttachment(def.id)) return false;
            bool spent = useGems ? TrySpendGems(def.gemPrice) : TrySpendCoins(def.price);
            if (!spent) return false;
            Data.ownedAttachmentIds.Add(def.id);
            Save();
            Changed?.Invoke();
            if (Data.ownedAttachmentIds.Count >= 3) TryUnlockAchievement(AchievementIds.Tuner);
            return true;
        }

        /// <summary>The attachment id equipped in <paramref name="slotId"/> on <paramref name="vehicleId"/>, or null if that slot is empty.</summary>
        public string GetEquippedAttachmentId(string vehicleId, string slotId)
        {
            for (int i = 0; i < Data.equippedAttachments.Count; i++)
            {
                var e = Data.equippedAttachments[i];
                if (e.vehicleId == vehicleId && e.slotId == slotId) return e.attachmentId;
            }
            return null;
        }

        /// <summary>Equips an owned attachment into its slot on a vehicle (replacing whatever was there).</summary>
        public void EquipAttachment(string vehicleId, string slotId, string attachmentId)
        {
            if (!OwnsAttachment(attachmentId)) return;
            for (int i = 0; i < Data.equippedAttachments.Count; i++)
            {
                var e = Data.equippedAttachments[i];
                if (e.vehicleId == vehicleId && e.slotId == slotId)
                {
                    e.attachmentId = attachmentId;
                    Save();
                    Changed?.Invoke();
                    return;
                }
            }
            Data.equippedAttachments.Add(new EquippedAttachmentState { vehicleId = vehicleId, slotId = slotId, attachmentId = attachmentId });
            Save();
            Changed?.Invoke();
        }

        /// <summary>Clears whatever is equipped in a slot (the vehicle goes back to bare for that slot).</summary>
        public void UnequipAttachmentSlot(string vehicleId, string slotId)
        {
            for (int i = 0; i < Data.equippedAttachments.Count; i++)
            {
                if (Data.equippedAttachments[i].vehicleId == vehicleId && Data.equippedAttachments[i].slotId == slotId)
                {
                    Data.equippedAttachments.RemoveAt(i);
                    Save();
                    Changed?.Invoke();
                    return;
                }
            }
        }

        // ── Booster/trail VFX skins ───────────────────────────────────────────
        public BoosterSkinDef GetBoosterDef(string id)
        {
            if (boosters == null || string.IsNullOrEmpty(id)) return null;
            foreach (var b in boosters) if (b != null && b.id == id) return b;
            return null;
        }

        public bool OwnsBooster(string id) => Data.ownedBoosterIds.Contains(id);

        /// <summary>Buys a booster skin with coins (useGems: false) or gems (useGems: true). Returns false if already owned or insufficient balance.</summary>
        public bool TryBuyBooster(BoosterSkinDef def, bool useGems)
        {
            if (def == null || OwnsBooster(def.id)) return false;
            bool spent = useGems ? TrySpendGems(def.gemPrice) : TrySpendCoins(def.price);
            if (!spent) return false;
            Data.ownedBoosterIds.Add(def.id);
            Save();
            Changed?.Invoke();
            if (Data.ownedBoosterIds.Count >= 3) TryUnlockAchievement(AchievementIds.Trailblazer);
            return true;
        }

        public void SelectBooster(string id)
        {
            if (!OwnsBooster(id)) return;
            Data.equippedBoosterId = id;
            Save();
            Changed?.Invoke();
        }

        /// <summary>The trail color to use right now, or null if no booster is equipped (caller should keep its own default).</summary>
        public Color? GetEquippedBoosterColor()
        {
            var def = GetBoosterDef(Data.equippedBoosterId);
            return def != null ? def.trailColor : (Color?)null;
        }

        // ── Currency packs (IAP) ─────────────────────────────────────────────
        public CurrencyPackDef GetCurrencyPack(string productId)
        {
            if (currencyPacks == null) return null;
            foreach (var p in currencyPacks) if (p != null && p.productId == productId) return p;
            return null;
        }

        /// <summary>Grants a purchased pack's reward and persists immediately. Called by IAPService.ProcessPurchase.</summary>
        public void GrantCurrencyPack(CurrencyPackDef pack)
        {
            if (pack == null) return;
            if (pack.currencyType == CurrencyType.Gems) AddGems(pack.amount);
            else AddCoins(pack.amount);
            Save();
            TryUnlockAchievement(AchievementIds.FirstPurchase);
        }

        // ── Wallet ───────────────────────────────────────────────────────────
        public void AddCoins(int amount)
        {
            Data.coins = Mathf.Max(0, Data.coins + amount);
            Changed?.Invoke();
            if (Data.coins >= 10000) TryUnlockAchievement(AchievementIds.CoinBaron);
        }

        public void AddGems(int amount)
        {
            Data.gems = Mathf.Max(0, Data.gems + amount);
            Changed?.Invoke();
            if (Data.gems >= 100) TryUnlockAchievement(AchievementIds.GemHunter);
        }
        public void AddKeys(int amount)  { Data.keys  = Mathf.Max(0, Data.keys  + amount); Changed?.Invoke(); }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            Changed?.Invoke();
            return true;
        }

        public bool TrySpendGems(int amount)
        {
            if (amount < 0 || Data.gems < amount) return false;
            Data.gems -= amount;
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
            if (owned)
            {
                TryUnlockAchievement(AchievementIds.Supporter);
                TryUnlockAchievement(AchievementIds.FirstPurchase);
            }
        }

        // ── Achievements ─────────────────────────────────────────────────────
        public bool HasUnlockedAchievement(string achievementId) => Data.unlockedAchievementIds.Contains(achievementId);

        /// <summary>Unlocks an achievement exactly once (locally deduped — safe to call
        /// repeatedly), persists, and forwards to Play Games.</summary>
        public void TryUnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || Data.unlockedAchievementIds.Contains(achievementId)) return;
            Data.unlockedAchievementIds.Add(achievementId);
            Save();
            PlatformServices.PlayGames.UnlockAchievement(achievementId);
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

        // ── Ads ──────────────────────────────────────────────────────────────
        /// <summary>Ad-free grace period: no interstitial/rewarded is ever shown for a
        /// player's first 10 completed runs.</summary>
        public bool AdsUnlocked => Data.totalRunsCompleted > 10;

        /// <summary>Records a fence scrape (not a crash) toward the Wall Hugger achievement.</summary>
        public void IncrementFenceScreech()
        {
            Data.totalFenceScreeches++;
            Save();
            if (Data.totalFenceScreeches >= 10) TryUnlockAchievement(AchievementIds.WallHugger);
        }

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

            Data.totalRunsCompleted++;
            Data.lifetimeDistanceMeters += Mathf.Max(0f, distanceMeters);
            Save();

            TryUnlockAchievement(AchievementIds.FirstDrift);
            if (distanceMeters >= 2000f) TryUnlockAchievement(AchievementIds.SpeedDemon);
            if (distanceMeters >= 5000f) TryUnlockAchievement(AchievementIds.EndlessHorizon);
            if (Data.lifetimeDistanceMeters >= 1000f) TryUnlockAchievement(AchievementIds.FirstMile);
            if (Data.lifetimeDistanceMeters >= 50000f) TryUnlockAchievement(AchievementIds.Marathoner);
            if (Data.lifetimeDistanceMeters >= 100000f) TryUnlockAchievement(AchievementIds.RoadLegend);
            if (Data.totalRunsCompleted >= 200) TryUnlockAchievement(AchievementIds.VeteranDrifter);
            if (Data.loginStreak >= 7) TryUnlockAchievement(AchievementIds.DailyDevotee);
            if (Data.loginStreak >= 30) TryUnlockAchievement(AchievementIds.StreakMaster);

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

            TryUnlockAchievement(def.achievementId);

            Save();
            Changed?.Invoke();
            return true;
        }
    }
}
