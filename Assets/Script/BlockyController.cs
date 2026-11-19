using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class BlockyController : NetworkBehaviour
{
    /*
     * status = 0: idle
     * status = 1: walk
     * status = 2: receive block idle
     * status = 3: receive block run
     * status = 4: throw
     * status = 5: receive block carry idle
     */
    [Header("Action")] public Action ReceiveBlockAction;
    
    [Header("Component")]
    public Animator _animator;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rigidbody2D;
    
    
    [Header("Movement")]
    public bool isRight;
    public float moveX, moveY;
    public float moveSpeed = 5f;
    public float cellSize;
    Vector2 direction;

    [Header("PlayerState")] 
    private IPlayerStatePattern currentState;
    public PlayerIdle _playerIdle;
    public PlayerWalk playerWalk;
    public PlayerReceiveBlocksIdle playerReceiveBlocks;
    public PlayerReceiveBlockRun playerReceiveBlockRun;
    public PlayerThrow playerThrow;
    public PlayerReceiveBlockCarryIdle playerReceiveBlockCarryIdle;
    public PlayerVictory playerVictory;

    [Header("Receive block")] 
    public GameObject receiveBlock;
    public Transform holdBlockPos;
    //public GameObject bird;
    [Header("Control")] public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode receiveKey = KeyCode.W;
    public KeyCode throwKey = KeyCode.S;

    [Header("Victory")] public bool isGameOver = false;

    [Header("Effect")] public GameObject victoryEffect;

    public bool leftArea = true;
    
    [Header("Network Receive")]
    [SerializeField, Min(0.1f)] private float receiveDistance = 2f;

    private const ulong NoHeldBlock = ulong.MaxValue;

    private NetworkVariable<ulong> heldBlockId = new NetworkVariable<ulong>(
        NoHeldBlock,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<uint> throwSequence = new NetworkVariable<uint>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private uint lastThrowSequence;
    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void RequestReceiveBlockRpc(RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (NetworkManager.ConnectedClientsIds.Count < 2)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.isGameOver)
        {
            return;
        }

        if (heldBlockId.Value != NoHeldBlock)
            return;

        BirdController[] birds = FindObjectsByType<BirdController>(
            FindObjectsSortMode.None
        );

        foreach (BirdController bird in birds)
        {
            BlockController block = bird.TryGiveBlockServer(
                this,
                receiveDistance
            );

            if (block == null)
                continue;

            heldBlockId.Value = block.NetworkObjectId;
            return;
        }

        Debug.Log(
            $"'{name}' chưa nhận được block. Kiểm tra vị trí và khoảng cách nhận.",
            this
        );
    }
    public override void OnNetworkSpawn()
    {
        lastThrowSequence = throwSequence.Value;
    }

    public void ThrowBlock()
    {
        if (!IsSpawned || !IsOwner || receiveBlock == null)
            return;

        RequestThrowBlockRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void RequestThrowBlockRpc(RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (NetworkManager.ConnectedClientsIds.Count < 2)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.isGameOver)
        {
            return;
        }

        if (heldBlockId.Value == NoHeldBlock)
            return;

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                heldBlockId.Value,
                out NetworkObject blockObject))
        {
            heldBlockId.Value = NoHeldBlock;
            return;
        }

        BlockController block = blockObject.GetComponent<BlockController>();

        if (block == null || block.transform.parent != transform)
            return;

        if (!block.GoDown())
            return;

        heldBlockId.Value = NoHeldBlock;

        // Thông báo một lần thả thành công để Owner chạy animation.
        throwSequence.Value = unchecked(throwSequence.Value + 1);
    }
    private void RefreshHeldBlock()
    {
        GameObject nextBlock = null;

        if (heldBlockId.Value != NoHeldBlock &&
            NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                heldBlockId.Value,
                out NetworkObject blockObject))
        {
            nextBlock = blockObject.gameObject;
        }

        if (receiveBlock == nextBlock)
            return;

        bool wasHoldingBlock = receiveBlock != null;
        receiveBlock = nextBlock;

        if (!IsOwner)
            return;

        if (receiveBlock != null)
        {
            ChangeState(playerReceiveBlockCarryIdle);
        }
        else if (wasHoldingBlock && currentState != playerThrow)
        {
            ChangeState(_playerIdle);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();

        isRight = true;

        receiveBlock = null;
        
        _playerIdle = new PlayerIdle(this);
        playerWalk = new PlayerWalk(this);
        playerReceiveBlocks = new PlayerReceiveBlocksIdle(this);
        playerReceiveBlockRun = new PlayerReceiveBlockRun(this);
        playerThrow = new PlayerThrow(this);
        playerReceiveBlockCarryIdle = new PlayerReceiveBlockCarryIdle(this);
        playerVictory = new PlayerVictory(this);
        ChangeState(_playerIdle);

        if (holdBlockPos == null)
        {
            holdBlockPos = transform.GetChild(0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsSpawned)
            return;

        RefreshHeldBlock();

        if (!IsOwner || IsHost != leftArea)
            return;

        if (lastThrowSequence != throwSequence.Value)
        {
            lastThrowSequence = throwSequence.Value;
            ChangeState(playerThrow);
        }

        if (!isGameOver &&
            GameManager.Instance != null &&
            GameManager.Instance.isGameOver)
        {
            Win();
            isGameOver = true;
        }

        Direction();
        Movement();

        if (Mathf.Approximately(moveY, -1f) &&
            receiveBlock != null &&
            currentState != playerThrow)
        {
            ThrowBlock();
        }

        currentState.Update();
    }
     

    public void ChangeState(IPlayerStatePattern newState)
    {
        if (currentState == newState)
            return;
        
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Movement()
    {
        if (Input.GetKeyDown(leftKey))
            direction = Vector2.left;

        if (Input.GetKeyDown(rightKey))
            direction = Vector2.right;
        
        //Debug.Log("Movement!!!!!!!!!!!");
        transform.position += (Vector3)(direction * cellSize);
        direction = Vector2.zero;
    }

    void Direction()
    {
        //gia tri moveX
        moveX = 0f;
        if (Input.GetKeyDown(leftKey))
        {
            moveX = -1f;
        }
        else if (Input.GetKeyDown(rightKey))
        {
            moveX = 1f;
        }
        //gia tri moveY
        moveY = 0f;
        if (Input.GetKeyDown(receiveKey))
        {
            moveY = 1f;
        }
        if (Input.GetKeyDown(throwKey))
        {
            moveY = -1f;
        }
        //quyet dinh sprite
        if (moveX == 1)
        {
            //di sang phai
            isRight = true;
            spriteRenderer.flipX = false;
        }
        if (moveX == -1)
        {
            isRight = false;
            spriteRenderer.flipX = true;
        }
    }
    
    public void ReceiveBlock()
    {
        if (!IsSpawned ||
            !IsOwner ||
            receiveBlock != null ||
            currentState == playerThrow)
        {
            return;
        }

        ChangeState(playerReceiveBlocks);
        RequestReceiveBlockRpc();
    }

    public void Win()
    {
        if (leftArea == GameManager.Instance.leftIsWin)
            Instantiate(victoryEffect, (Vector2)gameObject.transform.position + new Vector2(0, 5), Quaternion.identity);
    }
}
