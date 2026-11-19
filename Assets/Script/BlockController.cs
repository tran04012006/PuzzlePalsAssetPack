using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
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

    private NetworkVariable<int> blockIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
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
    
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Block đang được Bird mang sẽ đi theo Transform của Bird.
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody2D.simulated = false;
        _boxCollider2D.enabled = false;
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
                $"Block '{name}' có chỉ số hoặc Block Data Array chưa hợp lệ: {index}",
                this
            );

            return;
        }

        _blockData = blockDataArray[index];
        _spriteRenderer.sprite = _blockData.s;
    }

    public int BlockTypeCount => blockDataArray.Length;

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
    
    // Update is called once per frame
    void Update()
    {
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
        
        //Debug.Log("_rigidbody2D.linearVelocity.y = " + _rigidbody2D.linearVelocity.y + " !!!!!!!!!!!!!!!!!!");
        if (isFalling == true && transform.position.y <= 3f 
                              && Mathf.Approximately(_rigidbody2D.linearVelocity.y, 0)
            )
        {
            Debug.Log("chay PlaceBlock!!!!!!!!!!!!!!!!!!");
            //_rigidbody2D.bodyType = RigidbodyType2D.Static;
            PlaceBlock();
            isFalling = false;
        }
    }

    public void GoDown()
    {
        gameObject.transform.SetParent(null);
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        isFalling = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerArea"))
        {
            SetTrigger(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFallingAfterMatch)
            return;
        
        //xu li truong hop block dua vao phia bien
        if (other.gameObject.CompareTag("BlockArea"))
        {
            //Debug.Log("Time.time: " + Time.time);
            //Debug.Log("Dung vao BlockArea !!!!!!! @@@@@@@@@@@@@@@");
            //StartCoroutine(SetTriggerCoroutine());
            BirdSpawn();
            return;
        }
        
        else if (other.gameObject.CompareTag("Block"))
        {
            //StartCoroutine(SetTriggerCoroutine());
            BirdSpawn();
        }
    }

    void CheckLandingContact(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            //neu y > 0.5 tuc la phap tuyen huong len, tuc la other o ben duoi
            //loai bo cac tiep xuc o hai ben vat
            if (contact.normal.y > 0.5f)
            {
                hasSupportContact = true;
                return;
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        //neu block nay roi xuong vi bi mat cho do
        if (isFallingAfterMatch)
        {
            CheckLandingContact(other);
        }
        else
        {
            //neu block nay duoc player tha xuong
            if (other.gameObject.CompareTag("BlockArea"))
            {
                //Debug.Log("Dung vao BlockArea !!!!!!! @@@@@@@@@@@@@@@");
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
        //Debug.Log("chay SetTrigger");
        _boxCollider2D.isTrigger = newTrigger;
    }

    void PlaceBlock()
    {
        //_rigidbody2D.bodyType = RigidbodyType2D.Static;
        //_rigidbody2D.linearVelocity = new Vector2(0, -1 * speed);
        //doi tu world position thanh grid position
        Vector3Int gridPos = _grid.WorldToCell(transform.position);
        //Debug.Log("_rigidbody2D.linearVelocity.y = " + _rigidbody2D.linearVelocity.y + " !!!!!!!!!!!!!!!!!!");
        Debug.Log("gridPos: x = " + gridPos.x + ", y = " + gridPos.y + " !!!!!!!!!");
        //Debug.Log("goi AddBlock@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        while (_blockArea.blockList.ContainsKey((Vector2Int)gridPos))
        {
            Debug.Log("da co !!!!!!!!! " + gridPos);
            _blockArea.blockList.Remove((Vector2Int)gridPos);
        }
        _blockArea.AddBlock((Vector2Int)gridPos, this);
        //Debug.Log("kich thuoc cua blockList: " + _blockArea.blockList.Count);
    }

    public bool HasBlockBelow(Vector2Int currentPos)
    {
        // Block còn được chim giữ thì không rơi
        if (transform.parent != null)
            return true;

        // Block ở hàng đáy
        if (currentPos.y <= -12)
            return true;

        // Tìm block ngay bên dưới
        Vector2Int belowPos = currentPos + Vector2Int.down * 2;

        if (_blockArea != null &&
            _blockArea.blockList.ContainsKey(belowPos))
        {
            return true;
        }
        return false;
    }

    public void Fall()
    {
        if (isFalling)
            return;
        isFallingAfterMatch = true;
        hasSupportContact = false; //ben duoi co vat do chua
        _boxCollider2D.isTrigger = false;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1f;
        isFalling = true;
    }
}
