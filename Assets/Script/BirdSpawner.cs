using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;


public class BirdSpawner : MonoBehaviour
{
    public GameObject birdPrefab;
    public BlockArea blockArea;
    public Grid grid;
    
    private List<GameObject> birdList = new List<GameObject>();
    private int maxBird = 1;

    [Header("Spawn Position")]
    [SerializeField] private bool leftArea = true;
    [SerializeField] private float spawnY = 15.56f;

    [SerializeField] private Vector2 leftXRange =
        new Vector2(-25.29f, -6.41f);

    [SerializeField] private Vector2 rightXRange =
        new Vector2(6f, 24f);

    private NetworkObject currentBird;

    private bool initialBirdSpawned;
    private bool spawnRequested;

    private bool CanSpawn()
    {
        NetworkManager manager = NetworkManager.Singleton;

        return manager != null &&
               manager.IsListening &&
               manager.IsServer &&
               manager.ConnectedClientsIds.Count >= 2 &&
               (GameManager.Instance == null ||
                !GameManager.Instance.isGameOver);
    }

    private void Update()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null || !manager.IsListening)
        {
            currentBird = null;
            initialBirdSpawned = false;
            spawnRequested = false;
            return;
        }

        if (!CanSpawn())
            return;

        if (!initialBirdSpawned)
        {
            spawnRequested = true;
        }

        if (spawnRequested)
        {
            TrySpawnBird();
        }
    }

    public void SpawnBird()
    {
        if (!CanSpawn())
            return;
        birdList.RemoveAll(item => item == null);

        if (birdList.Count >= maxBird)
        {
            return;
        }
        
        spawnRequested = true;
        TrySpawnBird();
    }

    private void TrySpawnBird()
    {
        if (currentBird != null && currentBird.IsSpawned)
            return;

        Vector2 range = leftArea ? leftXRange : rightXRange;

        Vector3 position = new Vector3(
            Random.Range(range.x, range.y),
            spawnY,
            transform.position.z
        );
        Debug.Log("de chim");
        GameObject instance = Instantiate(
            birdPrefab,
            position,
            Quaternion.identity
        );
        birdList.Add(instance);
        currentBird = instance.GetComponent<NetworkObject>();
        currentBird.Spawn();

        instance.GetComponent<BirdController>().SpawnCarriedBlock(
            blockArea,
            grid,
            this
        );

        initialBirdSpawned = true;
        spawnRequested = false;
    }
}