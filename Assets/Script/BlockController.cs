using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
[RequireComponent(
    typeof(NetworkObject),
    typeof(Rigidbody2D),
    typeof(BoxCollider2D)
)]
public class BlockController : NetworkBehaviour
{
    public Rigidbody2D _rigidbody2D;

    private BoxCollider2D _boxCollider2D;

    public BlockData _blockData;
    private SpriteRenderer _spriteRenderer;

    public BirdSpawner birdSpawner;
    public BirdController bird;

    public bool isFalling = false;

    private bool isFallingAfterMatch = false;
    private bool hasSupportContact;

    [Header("Grid")]
    public Grid _grid;
    public BlockArea _blockArea;

    public float speed = 4f;
    public bool disappear = false;
    private NetworkTransform blockNetworkTransform;
    // NETWORK: danh sách loại block giống nhau trên hai máy.
    [Header("Network Block")]
    [SerializeField] private BlockData[] blockDataArray;

    private int initialBlockIndex = -1;

    private NetworkVariable<int> blockIndex =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public int BlockTypeCount =>
        blockDataArray == null ? 0 : blockDataArray.Length;

    private void OnEnable()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Giữ trạng thái ban đầu của bạn.
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _boxCollider2D.isTrigger = true;

        // Giữ Simulated luôn bật.
        _rigidbody2D.simulated = true;
        blockNetworkTransform = GetComponent<NetworkTransform>();
    }
    
    // đặt vị trí tay ngay trong quá trình đổi parent.
    public override void OnNetworkObjectParentChanged(
        NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        if (!IsServer || parentNetworkObject == null)
            return;

        BlockyController player =
            parentNetworkObject.GetComponent<BlockyController>();

        // Khi parent là Bird thì giữ cách xử lý hiện tại.
        if (player == null || player.holdBlockPos == null)
            return;

        // Chuyển vị trí world của tay thành tọa độ
        // tương đối với GameObject gốc của player.
        transform.localPosition =
            player.transform.InverseTransformPoint(
                player.holdBlockPos.position
            );
    }
        
    // đồng bộ tức thời một lần sau khi nhận block.
    public void SyncHeldPositionServer()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (blockNetworkTransform == null)
        {
            Debug.LogError(
                "NetworkBlock cần NetworkTransform.",
                this
            );

            return;
        }

        // Truyền tọa độ đúng theo không gian
        // mà NetworkTransform đang sử dụng.
        Vector3 position = blockNetworkTransform.InLocalSpace
            ? transform.localPosition
            : transform.position;

        Quaternion rotation = blockNetworkTransform.InLocalSpace
            ? transform.localRotation
            : transform.rotation;

        blockNetworkTransform.Teleport(
            position,
            rotation,
            transform.localScale
        );
    }

    // NETWORK:
    // Bird gọi hàm này trước khi Spawn block.
    // Các tham chiếu scene chỉ cần được gán trên Host.
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

        _rigidbody2D.simulated = true;

        // Block bắt đầu ở trạng thái được Bird giữ.
        // Client giữ Kinematic và nhận vị trí từ Host.
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
        // Client có thể đang chờ chỉ số do Host gửi.
        if (index == -1)
            return;

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

    void Update()
    {
        // NETWORK
        if (!IsSpawned || !IsServer)
            return;
        if (isFallingAfterMatch == true)
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
        
        if (isFalling == true &&
            transform.position.y <= 3f &&
            Mathf.Abs(_rigidbody2D.linearVelocity.y) < 0.05f)
        {
            Debug.Log("vi tri y cua block < 3: " + transform.position.y);
            Debug.Log("du dieu kien de placeblock");
            PlaceBlock();

            isFalling = false;
        }

       
    }

    public bool GoDown()
    {
        // NETWORK: việc thả được thực hiện trên Host.
        if (!IsSpawned || !IsServer)
            return false;

        // NETWORK: thay cho transform.SetParent(null).
        // true giữ vị trí world khi tách khỏi player.
        if (!NetworkObject.TryRemoveParent(true))
            return false;

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        isFalling = true;

        return true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // NETWORK
        if (!IsSpawned || !IsServer)
            return;

        if (other.CompareTag("PlayerArea"))
        {
            SetTrigger(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // NETWORK
        if (!IsSpawned || !IsServer)
            return;

        if (isFallingAfterMatch)
            return;

        // Giữ cách xử lý block đụng vùng đỡ.
        if (other.gameObject.CompareTag("BlockArea"))
        {
            BirdSpawn();
            return;
        }
        else if (other.gameObject.CompareTag("Block"))
        {
            BirdSpawn();
        }
    }

    void CheckLandingContact(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            // Vật đỡ nằm bên dưới.
            if (contact.normal.y > 0.5f)
            {
                hasSupportContact = true;
                return;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // NETWORK
        if (!IsSpawned || !IsServer)
            return;

        // Block rơi do mất chỗ đỡ.
        if (isFallingAfterMatch)
        {
            CheckLandingContact(other);
        }
        else
        {
            // Block được player thả xuống.
            if (other.gameObject.CompareTag("BlockArea"))
            {
                BirdSpawn();
                return;
            }
            else if (other.gameObject.CompareTag("Block"))
            {
                BirdSpawn();
            }
        }
    }

    void BirdSpawn()
    {
        birdSpawner.SpawnBird();

        StartCoroutine(SetTriggerBeforeBeingDestroyed());
    }

    IEnumerator SetTriggerBeforeBeingDestroyed()
    {
        yield return new WaitForSeconds(0.4f);

        SetTrigger(false);
    }

    void SetTrigger(bool newTrigger)
    {
        _boxCollider2D.isTrigger = newTrigger;
    }

    void PlaceBlock()
    {
        Debug.Log("chay PlaceBlock!!!!!!!!!!!!!!!!!!");
        Vector3Int gridPos =
            _grid.WorldToCell(transform.position);

        Debug.Log(
            "gridPos: x = " + gridPos.x +
            ", y = " + gridPos.y +
            " !!!!!!!!!"
        );

        while (_blockArea.blockList.ContainsKey(
                   (Vector2Int)gridPos))
        {
            Debug.Log("da co !!!!!!!!! " + gridPos);

            _blockArea.blockList.Remove(
                (Vector2Int)gridPos
            );
        }

        _blockArea.AddBlock(
            (Vector2Int)gridPos,
            this
        );
    }

    public bool HasBlockBelow(Vector2Int currentPos)
    {
        // Block còn được giữ thì không rơi.
        if (transform.parent != null)
            return true;

        // Block ở hàng đáy.
        if (currentPos.y <= -12)
            return true;

        // Tìm block ngay bên dưới.
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
        // NETWORK
        if (!IsSpawned || !IsServer)
            return;

        if (isFalling)
            return;

        isFallingAfterMatch = true;
        hasSupportContact = false;

        _boxCollider2D.isTrigger = false;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1f;

        isFalling = true;
    }
}