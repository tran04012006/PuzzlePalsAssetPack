using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D))]
public class BirdController : NetworkBehaviour
{
    public float distance = 4f;
    public float moveSpeed = 4f;
    public float lifeTime = 4f;

    [Header("Network Block")]
    public BlockController blockPrefab;
    public Transform blockHoldPoint;

    public bool leftArea = true;
    public BlockData Blocktmp;

    private Rigidbody2D birdRigidbody;
    private BlockController carriedBlock;

    private float startY;
    private float flyingUpTime;
    private bool isFlyingDown;
    private bool isFlyingUp;

    private void Awake()
    {
        birdRigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        birdRigidbody.simulated = IsServer;

        if (IsServer)
        {
            startY = transform.position.y;
            isFlyingDown = true;
        }
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
                "Bird cần Block Prefab, Block Hold Point và danh sách BlockData hợp lệ.",
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
            Debug.LogError("Không thể gắn block vào Bird.", this);
            block.NetworkObject.Despawn();
            return;
        }

        carriedBlock = block;
        Blocktmp = block._blockData;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer &&
            carriedBlock != null &&
            carriedBlock.IsSpawned)
        {
            carriedBlock.NetworkObject.Despawn();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (isFlyingDown)
        {
            if (startY - birdRigidbody.position.y >= distance)
            {
                birdRigidbody.position = new Vector2(
                    birdRigidbody.position.x,
                    startY - distance
                );

                birdRigidbody.linearVelocity = Vector2.zero;
                isFlyingDown = false;
            }
            else
            {
                birdRigidbody.linearVelocity =
                    Vector2.down * moveSpeed;
            }
        }

        if (isFlyingUp)
        {
            birdRigidbody.linearVelocity = Vector2.up * moveSpeed;
            flyingUpTime += Time.fixedDeltaTime;

            if (flyingUpTime >= lifeTime)
            {
                NetworkObject.Despawn();
            }
        }
    }

    [ContextMenu("Test Fly Up")]
    public BlockController TryGiveBlockServer(
        BlockyController player,
        float receiveDistance)
    {
        if (!IsSpawned || !IsServer || isFlyingUp)
            return null;

        if (carriedBlock == null || !carriedBlock.IsSpawned)
            return null;

        if (player.leftArea != leftArea)
            return null;

        Vector2 blockPosition = carriedBlock.transform.position;
        Vector2 handPosition = player.holdBlockPos.position;

        if (Vector2.Distance(blockPosition, handPosition) > receiveDistance)
            return null;

        BlockController block = carriedBlock;

        if (!block.NetworkObject.TrySetParent(player.NetworkObject, true))
            return null;

        block.transform.position = player.holdBlockPos.position;

        // Bird đã bàn giao block nên không còn quản lý block này.
        carriedBlock = null;
        Blocktmp = null;

        FlyUp();

        return block;
    }
    public void FlyUp()
    {
        if (!IsSpawned || !IsServer || isFlyingUp)
            return;

        isFlyingDown = false;
        isFlyingUp = true;
        flyingUpTime = 0f;
    }
}