using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Spawns the player's selected vehicle prefab at a fixed spawn point and wires
    /// it into the scripts that used to reference a static scene "PlayerCar" object
    /// directly. <see cref="DefaultExecutionOrderAttribute"/>(-100) guarantees this
    /// component's <see cref="Awake"/> runs before every other script's Awake() —
    /// this is what makes the wiring safe: consumers that read player/vehicle state
    /// inside their own Awake() (e.g. GameController resolving VehicleHealth) get a
    /// fully-wired reference already, instead of racing this spawner.
    ///
    /// Falls back to <see cref="fallbackPrefab"/> if there's no GameApp instance or
    /// no vehicle prefab assigned (e.g. isolated scene testing).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlayerVehicleSpawner : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] GameObject fallbackPrefab;
        [SerializeField] GameController gameController;
        [SerializeField] TrafficSensor trafficSensor;
        [SerializeField] GameHudUI hud;
        [SerializeField] RoadSegmentPool road;
        [SerializeField] TrafficDirector trafficDirector;
        [SerializeField] CollectibleSpawner collectibleSpawner;

        public Transform SpawnedPlayer { get; private set; }
        public HyperDriftCarController SpawnedCarController { get; private set; }
        public VehicleHealth SpawnedVehicleHealth { get; private set; }

        void Awake()
        {
            var def = GameApp.Instance != null ? GameApp.Instance.Selected : null;
            var prefab = (def != null && def.prefab != null) ? def.prefab : fallbackPrefab;
            if (prefab == null)
            {
                Debug.LogError("[PlayerVehicleSpawner] No vehicle prefab resolved (no GameApp.Selected.prefab and no fallbackPrefab set).");
                return;
            }

            Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint ? spawnPoint.rotation : transform.rotation;
            var instance = Instantiate(prefab, pos, rot);
            instance.name = "PlayerCar";

            SpawnedPlayer = instance.transform;
            SpawnedCarController = instance.GetComponent<HyperDriftCarController>();
            SpawnedVehicleHealth = instance.GetComponent<VehicleHealth>();

            if (gameController != null) gameController.SetPlayer(SpawnedPlayer, SpawnedVehicleHealth, SpawnedCarController);
            if (trafficSensor != null) trafficSensor.SetPlayer(SpawnedPlayer, SpawnedCarController);
            if (hud != null) hud.SetPlayer(SpawnedPlayer);
            // RoadSegmentPool/TrafficDirector/CollectibleSpawner all read `player` from
            // their own Start()/Update(), which run after every Awake() — safe given
            // the execution-order guarantee above, but found the hard way: an earlier
            // pass of this migration missed these three and RoadSegmentPool failed
            // loudly ("Player reference missing") the first time the static PlayerCar
            // was actually removed from the scene.
            if (road != null) road.SetPlayer(SpawnedPlayer);
            if (trafficDirector != null) trafficDirector.SetPlayer(SpawnedPlayer);
            if (collectibleSpawner != null) collectibleSpawner.SetPlayer(SpawnedPlayer, SpawnedVehicleHealth);
            // DriftFollowCamera needs no explicit wiring — its existing
            // ResolveTargetIfNeeded() fallback (FindGameObjectWithTag("Player") in
            // LateUpdate()) already resolves this correctly, and the execution-order
            // guarantee above ensures the spawn always happens before the first
            // LateUpdate call.
        }
    }
}
