using Unity.Netcode;
using UnityEngine;

[RequireComponent(
    typeof(NetworkObject),
    typeof(Rigidbody2D),
    typeof(BoxCollider2D)
)]
public class BlockController : NetworkBehaviour
{
    [Header("Network Block")]
    [SerializeField] private BlockData[] blockDataArray;

    public Rigidbody2D _rigidbody2D;
    public BlockData _blockData;

    public BirdSpawner birdSpawner;
    public BirdController bird;

    [Header("Grid")]
    public Grid _grid;
    public BlockArea _blockArea;

    public bool isFalling;
    public bool disappear;

    private BoxCollider2D blockCollider;
    private SpriteRenderer blockRenderer;

    private int initialBlockIndex = -1;

    private bool hasRegisteredPosition;
    private Vector2Int registeredPosition;

    private readonly ContactPoint2D[] landingContacts =
        new ContactPoint2D[8];

    private NetworkVariable<int> blockIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int BlockTypeCount => blockDataArray.Length;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        blockCollider = GetComponent<BoxCollider2D>();
        blockRenderer = GetComponent<SpriteRenderer>();

        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody2D.simulated = false;
        blockCollider.enabled = false;
    }

    public void InitializeServer(
        int index,
        BlockArea area,
        Grid grid,
        BirdSpawner spawner,
        BirdController sourceBird)
    {
        initialBlockIndex = index;

        _blockArea = area;
        _grid = grid;

        birdSpawner = spawner;
        bird = sourceBird;
    }

    public override void OnNetworkSpawn()
    {
        blockIndex.OnValueChanged += HandleBlockChanged;

        if (IsServer)
        {
            blockIndex.Value = initialBlockIndex;
        }

        ApplyBlock(blockIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        blockIndex.OnValueChanged -= HandleBlockChanged;

        if (IsServer)
        {
            UnregisterPosition();
        }
    }

    public bool HasValidBlockData()
    {
        if (blockDataArray == null || blockDataArray.Length == 0)
            return false;

        foreach (BlockData data in blockDataArray)
        {
            if (data == null)
                return false;
        }

        return true;
    }

    private void HandleBlockChanged(int previousValue, int newValue)
    {
        ApplyBlock(newValue);
    }

    private void ApplyBlock(int index)
    {
        if (blockDataArray == null ||
            index < 0 ||
            index >= blockDataArray.Length ||
            blockDataArray[index] == null)
        {
            Debug.LogError(
                $"Block '{name}' có BlockData chưa hợp lệ: {index}",
                this
            );

            return;
        }

        _blockData = blockDataArray[index];
        blockRenderer.sprite = _blockData.s;
    }

    public bool GoDown()
    {
        if (!IsSpawned || !IsServer || isFalling)
            return false;

        if (_grid == null || _blockArea == null)
        {
            Debug.LogError(
                "Block cần được gán Grid và BlockArea từ spawner.",
                this
            );

            return false;
        }

        Transform previousHolder = transform.parent;

        if (!NetworkObject.TryRemoveParent(true))
            return false;

        BeginFalling();

        // Block thả từ tay sẽ đi xuyên qua collider của người vừa cầm.
        if (previousHolder != null)
        {
            Collider2D[] holderColliders =
                previousHolder.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D holderCollider in holderColliders)
            {
                if (holderCollider != blockCollider)
                {
                    Physics2D.IgnoreCollision(
                        blockCollider,
                        holderCollider
                    );
                }
            }
        }

        return true;
    }

    private void BeginFalling()
    {
        UnregisterPosition();

        blockCollider.enabled = true;
        blockCollider.isTrigger = false;

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.constraints =
            RigidbodyConstraints2D.FreezePositionX |
            RigidbodyConstraints2D.FreezeRotation;

        _rigidbody2D.gravityScale = 1f;
        _rigidbody2D.simulated = true;
        _rigidbody2D.linearVelocity = Vector2.zero;

        isFalling = true;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsServer || disappear)
            return;

        if (transform.parent != null)
            return;

        bool hasSupport = HasSupportBelow();

        // Block đã đáp nhưng sau đó mất chỗ đỡ hoặc bắt đầu rơi.
        if (hasRegisteredPosition &&
            (!hasSupport ||
             Mathf.Abs(_rigidbody2D.linearVelocity.y) > 0.1f))
        {
            UnregisterPosition();
            isFalling = true;
        }

        if (!isFalling || !hasSupport)
            return;

        if (Mathf.Abs(_rigidbody2D.linearVelocity.y) > 0.1f)
            return;

        PlaceBlock();
    }

    private bool HasSupportBelow()
    {
        int contactCount = blockCollider.GetContacts(landingContacts);

        for (int i = 0; i < contactCount; i++)
        {
            if (landingContacts[i].normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private void PlaceBlock()
    {
        Vector2Int gridPosition =
            (Vector2Int)_grid.WorldToCell(transform.position);

        if (_blockArea.blockList.TryGetValue(
                gridPosition,
                out BlockController existingBlock) &&
            existingBlock != null &&
            existingBlock != this)
        {
            Debug.LogError(
                $"Ô {gridPosition} đã có block khác. " +
                "Kiểm tra Grid và kích thước block.",
                this
            );

            isFalling = false;
            return;
        }

        isFalling = false;
        registeredPosition = gridPosition;
        hasRegisteredPosition = true;

        _blockArea.AddBlock(gridPosition, this);
    }

    private void UnregisterPosition()
    {
        if (!hasRegisteredPosition || _blockArea == null)
            return;

        if (_blockArea.blockList.TryGetValue(
                registeredPosition,
                out BlockController registeredBlock) &&
            registeredBlock == this)
        {
            _blockArea.blockList.Remove(registeredPosition);
        }

        hasRegisteredPosition = false;
    }

    public bool HasBlockBelow(Vector2Int currentPosition)
    {
        if (transform.parent != null || currentPosition.y <= -12)
            return true;

        Vector2Int belowPosition =
            currentPosition + Vector2Int.down * 2;

        return _blockArea != null &&
               _blockArea.blockList.TryGetValue(
                   belowPosition,
                   out BlockController belowBlock) &&
               belowBlock != null &&
               !belowBlock.isFalling &&
               !belowBlock.disappear;
    }

    public void Fall()
    {
        if (!IsSpawned ||
            !IsServer ||
            isFalling ||
            transform.parent != null)
        {
            return;
        }

        BeginFalling();
    }
}