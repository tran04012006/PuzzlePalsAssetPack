using UnityEngine;

public class PlayerVictory : IPlayerStatePattern
{
    private BlockyController _blockyController;
    private Animator _animator;
    private Rigidbody2D _rigidbody2D;

    private float duration;
    
    //constructor
    public PlayerVictory(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
        
        _animator = _blockyController._animator;
        _rigidbody2D = _blockyController.rigidbody2D;
    }

    public void Enter()
    {
        _animator.SetInteger("Status", 6);
    }

    public void Update()
    {
        
    }

    public void Exit()
    {
        
    }
}
