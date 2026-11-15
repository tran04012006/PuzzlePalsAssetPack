using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class BirdController : MonoBehaviour
{
    private Vector2 startPos;
    private Rigidbody2D _rigidbody2D;

    public float distance = 4f;

    public float moveSpeed = 4f;

    public GameObject blocky;
    private bool isFlyingUp = false;
    private BlockyController blockyController;
    public float lifeTime = 4f; 
    
    [Header("Block")]
    public BlockData[] blockDataArray;
    private int currentIndexBlock;
    private GameObject currentBlock;
    public BlockData Blocktmp;

    private void OnEnable()
    {
        blocky = GameObject.FindGameObjectWithTag("Player");
        blockyController = blocky.GetComponent<BlockyController>();
        
        blockyController.ReceiveBlockAction += FlyUp;
        
        currentBlock = transform.GetChild(0).gameObject;
        TakeBlock();
    }

    private void OnDisable()
    {
        blockyController.ReceiveBlockAction -= FlyUp;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //x: -25,29 -> -6,41
        //y: 15.56
        startPos.x = Random.Range(-25.29f, -6.41f);
        startPos.y = 15.56f;
        transform.position = startPos;

        _rigidbody2D = GetComponent<Rigidbody2D>();
        //xuat  hien block duoi chan bird
        
        //cho chim bay xuong 4f
        StartCoroutine(MoveDown());
    }

    // Update is called once per frame
    void Update()
    {
        if (isFlyingUp == true)
        {
            _rigidbody2D.linearVelocity = new Vector2(0, 1) * moveSpeed;
            Destroy(gameObject,lifeTime);
        }
    }

    IEnumerator MoveDown()
    {
        while (startPos.y - transform.position.y < distance)
        {
            _rigidbody2D.linearVelocity = new Vector2(0, -1) * moveSpeed;
            yield return new WaitForSeconds(0.1f);
        }
        _rigidbody2D.linearVelocity = Vector2.zero;
    }

    void FlyUp()
    {
        isFlyingUp = true;
    }

    void TakeBlock()
    {
        currentIndexBlock = Random.Range(0, blockDataArray.Length - 1);
        Blocktmp = blockDataArray[currentIndexBlock];
        SpriteRenderer sp = currentBlock.GetComponent<SpriteRenderer>();
        if (sp != null)
            sp.sprite = Blocktmp.s;
    }
    
}
