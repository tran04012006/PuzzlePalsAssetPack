using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;

    private BoxCollider2D _boxCollider2D;
    
    public BlockData _blockData;
    private SpriteRenderer _spriteRenderer;
    public BirdSpawner birdSpawner;
    public BirdController bird;
    public bool isFalling = false;
    private Vector3Int gridPos;
    [Header("Grid")] 
    public Grid _grid;
    public BlockArea _blockArea;
    public float speed = 4f;


    private void OnEnable()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _boxCollider2D.isTrigger = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bird = GetComponentInParent<BirdController>();
        _blockData = bird.Blocktmp;
    }
    

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("_rigidbody2D.linearVelocity.y = " + _rigidbody2D.linearVelocity.y + " !!!!!!!!!!!!!!!!!!");
        if (isFalling == true && transform.position.y <= 3f 
                              && Mathf.Approximately(_rigidbody2D.linearVelocity.y, 0)
            )
        {
            Debug.Log("chay PlaceBlock!!!!!!!!!!!!!!!!!!");
            PlaceBlock();
            isFalling = false;
        }
        /*
        if (HasBlockBelow((Vector2Int)gridPos) == false)
        {
            Debug.Log("HasBlockBelow = false");
            Fall();
        }
        */
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
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("BlockArea"))
        {
            //Debug.Log("Dung vao BlockArea !!!!!!! @@@@@@@@@@@@@@@");
            BirdSpawn();
            _rigidbody2D.bodyType = RigidbodyType2D.Static;
            return;
        }
        
        else if (other.gameObject.CompareTag("Block"))
        {
            BirdSpawn();
            _rigidbody2D.bodyType = RigidbodyType2D.Static;
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
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.bodyType = RigidbodyType2D.Static;

        gridPos = _grid.WorldToCell(transform.position);

        isFalling = false;

        _blockArea.AddBlock((Vector2Int)gridPos, this);
    }

    public void Fall()
    {
        //block roi xuong
        if (isFalling) return;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1f;
        isFalling = true;
    }

    public bool HasBlockBelow(Vector2Int currentPos)
    {
        Vector2Int blockBelow = currentPos + Vector2Int.down * 2;
        //neu dang duoc con chim cam thi ko roi
        if (transform.parent != null)
            return true;
        //neu dang o duoi day cua BlockArea thi se ko bi roi
        if (Mathf.Approximately(currentPos.y, -12))
        {
            Debug.Log("vat dang o duoi day!!!!!!!!!!!!!!");
            return true;
        }
        //neu co block o duoi thi ko bi roi
        if (_blockArea.blockList.ContainsKey(blockBelow) == true)
        {
            Debug.Log("co block ben duoi!!!!!!!!!!!!!!");
            return true;
        }
        
        return false;
    }
}
