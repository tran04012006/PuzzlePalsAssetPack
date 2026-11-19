using Unity.Netcode;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    public GameObject birdPrefab;
    public BlockArea blockArea;

    [Header("Spawn Position")]
    [SerializeField] private bool leftArea = true;
    [SerializeField] private float spawnY = 15.56f;
    [SerializeField] private Vector2 leftXRange =
        new Vector2(-25.29f, -6.41f);
    [SerializeField] private Vector2 rightXRange =
        new Vector2(6f, 24f);

    private NetworkObject currentBird;

    private void Update()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null ||
            !manager.IsListening ||
            !manager.IsServer)
        {
            return;
        }

        if (manager.ConnectedClientsIds.Count < 2)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.isGameOver)
        {
            return;
        }

        SpawnBird();
    }

    public void SpawnBird()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null ||
            !manager.IsListening ||
            !manager.IsServer ||
            manager.ConnectedClientsIds.Count < 2)
        {
            return;
        }

        if (currentBird != null && currentBird.IsSpawned)
            return;

        Vector2 xRange = leftArea ? leftXRange : rightXRange;
        float spawnX = Random.Range(xRange.x, xRange.y);

        Vector3 spawnPosition = new Vector3(
            spawnX,
            spawnY,
            transform.position.z
        );

        GameObject birdInstance = Instantiate(
            birdPrefab,
            spawnPosition,
            Quaternion.identity
        );

        currentBird = birdInstance.GetComponent<NetworkObject>();
        currentBird.Spawn();
    }
}