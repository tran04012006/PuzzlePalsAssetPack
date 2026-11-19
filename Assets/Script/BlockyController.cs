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

        holdBlockPos = transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsSpawned || !IsOwner || IsHost != leftArea)
            return;
        //Debug.Log("receiveBlock: " + receiveBlock);
        if (isGameOver == false && GameManager.Instance.isGameOver == true)
        {
            Win();
            isGameOver = true;
        }
        Direction();
        Movement();
        //Debug.Log("currentState: " + currentState);
        //Debug.Log("moveY: !!!!!!!!!!!!!! " + moveY);
        //rigidbody2D.linearVelocity = new Vector2(1, 0) * (moveX * moveSpeed);
        currentState.Update();
        
        if (receiveBlock != null && currentState == playerReceiveBlockCarryIdle)
        {
            ReceiveBlockAction?.Invoke();
            //BirdController
        }
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
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.position != holdBlockPos.position 
            && other.gameObject.CompareTag("Block"))
        {
            //Debug.Log("di ra khoi block !!!!!!!!!!!!!!@@@@@@@@@@@@");
            receiveBlock = null;
        }  
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if  (other.gameObject.CompareTag("Block"))
        {
            //Debug.Log("Dung vao block !!!!!!!!!!!!");
            receiveBlock = other.gameObject;
            //Debug.Log("receiveBlock = " + receiveBlock + " !!!!!!@@@@@@@@@@@@@@@");
        }    
    }

    

    public void ReceiveBlock()
    {
        if (receiveBlock == null)
            ChangeState(playerReceiveBlocks);
        else
        {
            ChangeState(playerReceiveBlockCarryIdle);
        }
    }

    public void Win()
    {
        if (leftArea == GameManager.Instance.leftIsWin)
            Instantiate(victoryEffect, (Vector2)gameObject.transform.position + new Vector2(0, 5), Quaternion.identity);
    }
}
