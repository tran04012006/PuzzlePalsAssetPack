using UnityEngine;
using UnityEngine.UI;

public class BlockyController : MonoBehaviour
{
    [Header("Component")]
    public Animator _animator;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rigidbody2D;
    
    
    [Header("Movement")]
    public bool isRight;
    public float moveX, moveY;
    public float moveSpeed = 5f;


    [Header("PlayerState")] 
    private IPlayerStatePattern currentState;
    public PlayerIdle _playerIdle;
    public PlayerWalk playerWalk;
    public PlayerReceiveBlocks playerReceiveBlocks;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();

        isRight = true;
        
        _playerIdle = new PlayerIdle(this);
        playerWalk = new PlayerWalk(this);
        playerReceiveBlocks = new PlayerReceiveBlocks(this);

        ChangeState(_playerIdle);
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Direction();
        //Debug.Log("currentState: !!!!!!!!!!!!!! " + currentState);
        Debug.Log("moveY: !!!!!!!!!!!!!! " + moveY);

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

    void Movement()
    {
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");
    }

    void Direction()
    {
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
}
