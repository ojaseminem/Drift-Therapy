using System;
using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>Result of the most recent run, shown on the end screen.</summary>
    public struct RunResult
    {
        public int score;
        public int best;
        public int coins;
        public int xp;
        public bool isNewBest;
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
        public string selectedVehicleId = "";
        public List<string> ownedVehicleIds = new List<string>();
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool haptics = true;
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

        public PlayerData Data { get; private set; }
        public IReadOnlyList<VehicleDef> Vehicles => vehicles;

        /// <summary>Raised whenever wallet / profile / ownership changes.</summary>
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Persistence ──────────────────────────────────────────────────────
        void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            Data = string.IsNullOrEmpty(json) ? new PlayerData() : JsonUtility.FromJson<PlayerData>(json);
            EnsureDefaults();
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
        public void SubmitRun(int score, int coinsEarned, int xpEarned)
        {
            bool newBest = score > Data.highScore;
            if (newBest) Data.highScore = score;
            AddCoins(coinsEarned);
            AddXp(xpEarned);
            Save();
            LastRun = new RunResult
            {
                score = score, best = Data.highScore,
                coins = coinsEarned, xp = xpEarned, isNewBest = newBest
            };
        }
    }
}
