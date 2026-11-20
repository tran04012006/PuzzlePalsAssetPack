using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D))]
public class BirdController : NetworkBehaviour
{
    public float distance = 4f;
    public float moveSpeed = 4f;
    public float lifeTime = 4f;

    public bool leftArea = true;
    public BlockData Blocktmp;

    [Header("Network Block")]
    public BlockController blockPrefab;
    public Transform blockHoldPoint;

    private Rigidbody2D birdRigidbody;
    private BlockController carriedBlock;

    private Vector2 startPos;
    private bool isFlyingUp;

    private void Awake()
    {
        birdRigidbody = GetComponent<Rigidbody2D>();
        birdRigidbody.simulated = false;
    }

    public override void OnNetworkSpawn()
    {
        birdRigidbody.simulated = IsServer;

        if (!IsServer)
            return;

        startPos = transform.position;
        StartCoroutine(MoveDown());
    }

    public void SpawnCarriedBlock(
        BlockArea area,
        Grid grid,
        BirdSpawner spawner)
    {
        if (!IsSpawned || !IsServer || carriedBlock != null)
            return;

        if (blockPrefab == null ||
            blockHoldPoint == null ||
            !blockPrefab.HasValidBlockData())
        {
            Debug.LogError(
                "Gán NetworkBlock, Block Hold Point và BlockData trước khi chạy.",
                this
            );

            return;
        }

        int index = Random.Range(0, blockPrefab.BlockTypeCount);

        BlockController block = Instantiate(
            blockPrefab,
            blockHoldPoint.position,
            blockHoldPoint.rotation
        );

        block.InitializeServer(index, area, grid, spawner, this);
        block.NetworkObject.Spawn();

        if (!block.NetworkObject.TrySetParent(NetworkObject, true))
        {
            block.NetworkObject.Despawn();
            return;
        }

        carriedBlock = block;
        Blocktmp = block._blockData;
    }

    private IEnumerator MoveDown()
    {
        while (!isFlyingUp &&
               startPos.y - transform.position.y < distance)
        {
            birdRigidbody.linearVelocity =
                Vector2.down * moveSpeed;

            yield return new WaitForSeconds(0.1f);
        }

        if (!isFlyingUp)
        {
            birdRigidbody.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || !isFlyingUp)
            return;

        birdRigidbody.linearVelocity =
            Vector2.up * moveSpeed;
    }

    public BlockController TryGiveBlockServer(
        BlockyController player)
    {
        if (!IsSpawned ||
            !IsServer ||
            isFlyingUp ||
            player == null)
        {
            return null;
        }

        if (carriedBlock == null || !carriedBlock.IsSpawned)
            return null;

        if (player.leftArea != leftArea ||
            player.holdBlockPos == null)
        {
            return null;
        }

        BlockController block = carriedBlock;

        // Khi đổi parent, BlockController sẽ đặt vị trí tay
        // trong OnNetworkObjectParentChanged.
        if (!block.NetworkObject.TrySetParent(
                player.NetworkObject,
                true))
        {
            return null;
        }
        // Đồng bộ trạng thái chính xác một lần khi nhận.
        block.SyncHeldPositionServer();
        Debug.Log("block o vi tri: " + block.transform.position);
        carriedBlock = null;
        Blocktmp = null;

        FlyUp();

        return block;
    }

    public void FlyUp()
    {
        if (!IsSpawned || !IsServer || isFlyingUp)
            return;

        isFlyingUp = true;
        StartCoroutine(DespawnAfterLifetime());
    }

    private IEnumerator DespawnAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);

        if (IsSpawned && IsServer)
        {
            NetworkObject.Despawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();

        if (IsServer &&
            carriedBlock != null &&
            carriedBlock.IsSpawned)
        {
            carriedBlock.NetworkObject.Despawn();
        }
    }
}