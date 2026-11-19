using System.Collections;
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

    private int initialBlockIndex = -1;

    private NetworkVariable<int> blockIndex =
        new NetworkVariable<int>(-1);

    public int BlockTypeCount =>
        blockDataArray == null ? 0 : blockDataArray.Length;

    [Header("Component")]
    public Rigidbody2D _rigidbody2D;

    private BoxCollider2D _boxCollider2D;
    private SpriteRenderer _spriteRenderer;

    public BlockData _blockData;
    public BirdSpawner birdSpawner;
    public BirdController bird;

    [Header("State")]
    public bool isFalling = false;
    public bool disappear = false;

    private bool isFallingAfterMatch = false;
    private bool hasSupportContact;

    // Tránh nhiều callback cùng yêu cầu sinh Bird cho một lần thả.
    private bool requestedNextBird;

    [Header("Grid")]
    public Grid _grid;
    public BlockArea _blockArea;

    public float speed = 4f;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Giữ thiết lập ban đầu của code cũ.
        _rigidbody2D.simulated = true;
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _boxCollider2D.isTrigger = true;
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

        // Luôn bật Simulated trên cả hai máy.
        _rigidbody2D.simulated = true;

        // Ban đầu block được Bird giữ.
        // Client tiếp tục ở Kinematic khi Host thả block.
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;

        // Chỉ Host xử lý va chạm của block.
        _boxCollider2D.enabled = IsServer;

        if (IsServer)
        {
            blockIndex.Value = initialBlockIndex;
        }

        ApplyBlock(blockIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        blockIndex.OnValueChanged -= HandleBlockChanged;
        StopAllCoroutines();
    }

    public bool HasValidBlockData()
    {
        if (BlockTypeCount == 0)
            return false;

        foreach (BlockData data in blockDataArray)
        {
            if (data == null)
                return false;
        }

        return true;
    }

    private void HandleBlockChanged(
        int previousValue,
        int newValue)
    {
        ApplyBlock(newValue);
    }

    private void ApplyBlock(int index)
    {
        if (index < 0 ||
            index >= BlockTypeCount ||
            blockDataArray[index] == null)
        {
            Debug.LogError(
                $"BlockData index không hợp lệ: {index}",
                this
            );

            return;
        }

        _blockData = blockDataArray[index];
        _spriteRenderer.sprite = _blockData.s;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        // Giữ logic đáp xuống sau khi mất chỗ đỡ.
        if (isFallingAfterMatch)
        {
            if (hasSupportContact &&
                Mathf.Abs(_rigidbody2D.linearVelocity.y) < 0.1f)
            {
                isFallingAfterMatch = false;
                hasSupportContact = false;

                PlaceBlock();
                isFalling = false;
            }

            return;
        }

        // Giữ điều kiện đáp xuống của block được player thả.
        if (isFalling &&
            transform.position.y <= 3f &&
            Mathf.Approximately(_rigidbody2D.linearVelocity.y, 0f))
        {
            PlaceBlock();
            isFalling = false;
        }
    }

    public bool GoDown()
    {
        if (!IsSpawned || !IsServer || isFalling)
            return false;

        if (_grid == null || _blockArea == null)
            return false;

        // Thay SetParent(null) bằng thao tác tách parent qua network.
        // true giữ lại vị trí trong world khi tách.
        if (!NetworkObject.TryRemoveParent(true))
            return false;

        requestedNextBird = false;

        // Giữ logic thả cũ.
        // Không thay gravity, velocity hoặc constraints tại đây.
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        isFalling = true;

        return true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsSpawned || !IsServer)
            return;

        if (other.CompareTag("PlayerArea"))
        {
            SetTrigger(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSpawned || !IsServer)
            return;

        if (isFallingAfterMatch)
            return;

        if (other.CompareTag("BlockArea"))
        {
            BirdSpawn();
            return;
        }

        if (other.CompareTag("Block"))
        {
            BirdSpawn();
        }
    }

    private void CheckLandingContact(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                hasSupportContact = true;
                return;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!IsSpawned || !IsServer)
            return;

        if (isFallingAfterMatch)
        {
            CheckLandingContact(other);
        }
        else
        {
            if (other.gameObject.CompareTag("BlockArea"))
            {
                BirdSpawn();
                return;
            }

            if (other.gameObject.CompareTag("Block"))
            {
                BirdSpawn();
            }
        }
    }

    private void BirdSpawn()
    {
        if (requestedNextBird)
            return;

        requestedNextBird = true;

        birdSpawner.SpawnBird();
        StartCoroutine(SetTriggerBeforeBeingDestroyed());
    }

    private IEnumerator SetTriggerBeforeBeingDestroyed()
    {
        yield return new WaitForSeconds(0.4f);
        SetTrigger(false);
    }

    private void SetTrigger(bool newTrigger)
    {
        _boxCollider2D.isTrigger = newTrigger;
    }

    private void PlaceBlock()
    {
        Vector3Int gridPos =
            _grid.WorldToCell(transform.position);

        while (_blockArea.blockList.ContainsKey(
                   (Vector2Int)gridPos))
        {
            _blockArea.blockList.Remove(
                (Vector2Int)gridPos
            );
        }

        _blockArea.AddBlock((Vector2Int)gridPos, this);
    }

    public bool HasBlockBelow(Vector2Int currentPos)
    {
        if (transform.parent != null)
            return true;

        if (currentPos.y <= -12)
            return true;

        Vector2Int belowPos =
            currentPos + Vector2Int.down * 2;

        if (_blockArea != null &&
            _blockArea.blockList.ContainsKey(belowPos))
        {
            return true;
        }

        return false;
    }

    public void Fall()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (isFalling)
            return;

        // Giữ logic rơi sau khi ghép của code cũ.
        isFallingAfterMatch = true;
        hasSupportContact = false;

        _boxCollider2D.isTrigger = false;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1f;

        isFalling = true;
    }
}