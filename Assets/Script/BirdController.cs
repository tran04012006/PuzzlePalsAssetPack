using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D))]
public class BirdController : NetworkBehaviour
{
    public float distance = 4f;
    public float moveSpeed = 4f;
    public float lifeTime = 4f;

    [Header("Block")]
    public BlockData[] blockDataArray;
    public SpriteRenderer carriedBlockRenderer;

    public BlockData Blocktmp;
    public bool leftArea = true;

    private Rigidbody2D birdRigidbody;
    private float startY;
    private float flyingUpTime;
    private bool isFlyingDown;
    private bool isFlyingUp;

    private NetworkVariable<int> blockIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        birdRigidbody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        blockIndex.OnValueChanged += HandleBlockChanged;

        // Host mô phỏng chuyển động, Client hiển thị vị trí nhận được.
        birdRigidbody.simulated = IsServer;

        if (IsServer)
        {
            if (blockDataArray == null || blockDataArray.Length == 0)
            {
                Debug.LogError("Bird chưa được gán Block Data Array.", this);
                return;
            }

            blockIndex.Value = Random.Range(0, blockDataArray.Length);

            startY = transform.position.y;
            isFlyingDown = true;
        }

        ApplyBlock(blockIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        blockIndex.OnValueChanged -= HandleBlockChanged;
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
                birdRigidbody.linearVelocity = Vector2.down * moveSpeed;
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

    // Dùng từ Inspector của Host để kiểm tra vòng sinh Bird.
    [ContextMenu("Test Fly Up")]
    public void FlyUp()
    {
        if (!IsSpawned || !IsServer || isFlyingUp)
            return;

        isFlyingDown = false;
        isFlyingUp = true;
        flyingUpTime = 0f;
    }

    private void HandleBlockChanged(int previousValue, int newValue)
    {
        ApplyBlock(newValue);
    }

    private void ApplyBlock(int index)
    {
        if (blockDataArray == null ||
            index < 0 ||
            index >= blockDataArray.Length)
        {
            return;
        }

        Blocktmp = blockDataArray[index];
        carriedBlockRenderer.sprite = Blocktmp.s;
    }
}