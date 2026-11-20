using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BlockyController : NetworkBehaviour
{
    [Header("Action")]
    public Action ReceiveBlockAction;

    [Header("Component")]
    public Animator _animator;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rigidbody2D;

    [Header("Movement")]
    public bool isRight;
    public float moveX;
    public float moveY;
    public float moveSpeed = 5f;
    public float cellSize;

    private Vector2 direction;

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

    [Header("Control")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode receiveKey = KeyCode.W;
    public KeyCode throwKey = KeyCode.S;

    [Header("Victory")]
    public bool isGameOver = false;

    [Header("Effect")]
    public GameObject victoryEffect;

    public bool leftArea = true;

    private const ulong NoBlock = ulong.MaxValue;

    private NetworkVariable<ulong> networkReceiveBlockId =
        new NetworkVariable<ulong>(NoBlock);

    private BlockController serverHeldBlock;

    private bool holdRequestPending;
    private ulong confirmedHeldId = NoBlock;
    private ulong pendingThrownId = NoBlock;

    private void Awake()
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

        playerReceiveBlockCarryIdle =
            new PlayerReceiveBlockCarryIdle(this);

        playerVictory = new PlayerVictory(this);

        ChangeState(_playerIdle);

        if (holdBlockPos == null && transform.childCount > 0)
        {
            holdBlockPos = transform.GetChild(0);
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        RefreshReceiveBlock();

        if (!isGameOver &&
            GameManager.Instance != null &&
            GameManager.Instance.isGameOver)
        {
            Win();
            isGameOver = true;
        }

        if (!IsOwner || IsHost != leftArea)
            return;

        if (IsServer &&
            NetworkManager.ConnectedClientsIds.Count < 2)
        {
            return;
        }

        Direction();
        Movement();
        currentState.Update();

        if (receiveBlock != null &&
            currentState == playerReceiveBlockCarryIdle)
        {
            ReceiveBlockAction?.Invoke();
        }
    }

    private void LateUpdate()
    {
        if (!IsSpawned ||
            !IsServer ||
            serverHeldBlock == null)
        {
            return;
        }

        if (!serverHeldBlock.IsSpawned)
        {
            serverHeldBlock = null;
            networkReceiveBlockId.Value = NoBlock;
            return;
        }

        if (serverHeldBlock.transform.parent == transform &&
            holdBlockPos != null)
        {
            serverHeldBlock.transform.position =
                holdBlockPos.position;
        }
    }

    public override void OnNetworkDespawn()
    {
        receiveBlock = null;
        serverHeldBlock = null;

        holdRequestPending = false;
        confirmedHeldId = NoBlock;
        pendingThrownId = NoBlock;
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
        {
            direction = Vector2.left;
        }

        if (Input.GetKeyDown(rightKey))
        {
            direction = Vector2.right;
        }

        transform.position +=
            (Vector3)(direction * cellSize);

        direction = Vector2.zero;
    }

    private void Direction()
    {
        moveX = 0f;

        if (Input.GetKeyDown(leftKey))
        {
            moveX = -1f;
        }
        else if (Input.GetKeyDown(rightKey))
        {
            moveX = 1f;
        }

        moveY = 0f;

        if (Input.GetKeyDown(receiveKey))
        {
            moveY = 1f;
        }

        if (Input.GetKeyDown(throwKey))
        {
            moveY = -1f;
        }

        if (moveX == 1f)
        {
            isRight = true;
            spriteRenderer.flipX = false;
        }

        if (moveX == -1f)
        {
            isRight = false;
            spriteRenderer.flipX = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSpawned ||
            !IsServer ||
            serverHeldBlock != null)
        {
            return;
        }

        if (!other.CompareTag("Block"))
            return;

        BlockController block =
            other.GetComponent<BlockController>();

        if (block == null ||
            !block.IsSpawned ||
            block.isFalling)
        {
            return;
        }

        BirdController source =
            block.GetComponentInParent<BirdController>();

        if (source == null || source.leftArea != leftArea)
            return;

        networkReceiveBlockId.Value =
            block.NetworkObjectId;

        RefreshReceiveBlock();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsSpawned ||
            !IsServer ||
            serverHeldBlock != null)
        {
            return;
        }

        if (!other.CompareTag("Block") ||
            holdBlockPos == null)
        {
            return;
        }

        BlockController block =
            other.GetComponent<BlockController>();

        if (block != null &&
            block.IsSpawned &&
            block.NetworkObjectId == networkReceiveBlockId.Value &&
            other.transform.position != holdBlockPos.position)
        {
            networkReceiveBlockId.Value = NoBlock;
            RefreshReceiveBlock();
        }
    }

    private void RefreshReceiveBlock()
    {
        ulong id = networkReceiveBlockId.Value;

        if (id != pendingThrownId)
        {
            pendingThrownId = NoBlock;
        }

        if (id != confirmedHeldId)
        {
            confirmedHeldId = NoBlock;
        }

        receiveBlock = null;

        if (id == NoBlock || id == pendingThrownId)
            return;

        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                id,
                out NetworkObject blockObject))
        {
            receiveBlock = blockObject.gameObject;
        }
    }

    public void ReceiveBlock()
    {
        if (receiveBlock == null)
        {
            ChangeState(playerReceiveBlocks);
        }
        else
        {
            ChangeState(playerReceiveBlockCarryIdle);
        }
    }

    public void HoldBlock()
    {
        if (!IsSpawned ||
            !IsOwner ||
            receiveBlock == null ||
            holdRequestPending)
        {
            return;
        }

        NetworkObject blockObject =
            receiveBlock.GetComponent<NetworkObject>();

        if (blockObject == null ||
            !blockObject.IsSpawned ||
            confirmedHeldId == blockObject.NetworkObjectId)
        {
            return;
        }

        holdRequestPending = true;

        RequestHoldBlockRpc(blockObject.NetworkObjectId);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void RequestHoldBlockRpc(
        ulong blockId,
        RpcParams rpcParams = default)
    {
        bool success = false;

        if (rpcParams.Receive.SenderClientId == OwnerClientId &&
            NetworkManager.ConnectedClientsIds.Count >= 2 &&
            (GameManager.Instance == null ||
             !GameManager.Instance.isGameOver) &&
            blockId == networkReceiveBlockId.Value)
        {
            if (serverHeldBlock != null &&
                serverHeldBlock.NetworkObjectId == blockId)
            {
                success = true;
            }
            else if (
                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    blockId,
                    out NetworkObject blockObject))
            {
                BirdController source =
                    blockObject.GetComponentInParent<BirdController>();

                if (source != null)
                {
                    serverHeldBlock =
                        source.TryGiveBlockServer(this);

                    success = serverHeldBlock != null;
                }
            }
        }

        HoldResultRpc(blockId, success);
    }

    [Rpc(SendTo.Owner, RequireOwnership = false)]
    private void HoldResultRpc(ulong blockId, bool success)
    {
        holdRequestPending = false;

        if (success)
        {
            confirmedHeldId = blockId;
        }
    }

    public void ThrowBlock()
    {
        if (!IsSpawned ||
            !IsOwner ||
            receiveBlock == null ||
            holdBlockPos == null ||
            pendingThrownId != NoBlock)
        {
            return;
        }

        NetworkObject blockObject =
            receiveBlock.GetComponent<NetworkObject>();

        if (blockObject == null || !blockObject.IsSpawned)
            return;

        // chụp vị trí tay tại thời điểm bấm S.
        Vector3 releasePosition = holdBlockPos.position;

        pendingThrownId = blockObject.NetworkObjectId;
        receiveBlock = null;

        // gửi ID và vị trí thả trong cùng yêu cầu.
        RequestThrowBlockRpc(
            pendingThrownId,
            releasePosition
        );
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void RequestThrowBlockRpc(
        ulong blockId,
        Vector3 releasePosition,
        RpcParams rpcParams = default)
    {
        bool success = false;

        if (rpcParams.Receive.SenderClientId == OwnerClientId &&
            NetworkManager.ConnectedClientsIds.Count >= 2 &&
            (GameManager.Instance == null ||
             !GameManager.Instance.isGameOver) &&
            serverHeldBlock != null &&
            serverHeldBlock.IsSpawned &&
            serverHeldBlock.NetworkObjectId == blockId &&
            serverHeldBlock.transform.parent == transform &&
            IsFinitePosition(releasePosition))
        {
            BlockController block = serverHeldBlock;
            Rigidbody2D body = block._rigidbody2D;

            Vector3 previousPosition =
                block.transform.position;

            // Host dùng vị trí được gửi cùng yêu cầu thả.
            block.transform.position = releasePosition;

            body.position = new Vector2(
                releasePosition.x,
                releasePosition.y
            );

            // GoDown tách parent, chuyển Dynamic và bật isFalling.
            success = block.GoDown();

            if (success)
            {
                serverHeldBlock = null;
                networkReceiveBlockId.Value = NoBlock;

                // MỚI: đồng bộ chính xác vị trí sau khi tách parent.
                NetworkTransform blockTransform =
                    block.GetComponent<NetworkTransform>();

                if (blockTransform != null)
                {
                    // Block đã không còn parent nên local position
                    // và world position trùng nhau.
                    blockTransform.Teleport(
                        block.transform.position,
                        block.transform.rotation,
                        block.transform.localScale
                    );
                }
            }
            else
            {
                // Khôi phục nếu GoDown không thành công.
                block.transform.position = previousPosition;

                body.position = new Vector2(
                    previousPosition.x,
                    previousPosition.y
                );
            }
        }

        ThrowResultRpc(success);
    }

    private static bool IsFinitePosition(Vector3 position)
    {
        return !float.IsNaN(position.x) &&
               !float.IsInfinity(position.x) &&
               !float.IsNaN(position.y) &&
               !float.IsInfinity(position.y) &&
               !float.IsNaN(position.z) &&
               !float.IsInfinity(position.z);
    }

    [Rpc(SendTo.Owner, RequireOwnership = false)]
    private void ThrowResultRpc(bool success)
    {
        if (!success)
        {
            pendingThrownId = NoBlock;
        }
    }

    public void Win()
    {
        if (victoryEffect != null &&
            leftArea == GameManager.Instance.leftIsWin)
        {
            Instantiate(
                victoryEffect,
                (Vector2)transform.position + new Vector2(0, 5),
                Quaternion.identity
            );
        }
    }
}