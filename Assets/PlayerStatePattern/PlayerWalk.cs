using UnityEngine;

public class PlayerWalk : IPlayerStatePattern
{
    private BlockyController _blockyController;
    private Animator _animator;
    private Rigidbody2D _rigidbody2D;

    private float duration;
    
    //constructor
    public PlayerWalk(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
        
        _animator = _blockyController._animator;
        _rigidbody2D = _blockyController.rigidbody2D;
    }

    public void Enter()
    {
        _animator.SetInteger("Status", 1);
    }

    public void Update()
    {
        if (_blockyController.moveX == 0)
        {
            _blockyController.ChangeState(_blockyController._playerIdle);
        }
        
        if (Mathf.Approximately(_blockyController.moveY, 1))
        {
            //do tay len
            if (_blockyController.receiveBlock == null)
                _blockyController.ChangeState(_blockyController.playerReceiveBlocks);
            else
            {
                _blockyController.ChangeState(_blockyController.playerReceiveBlockCarryIdle);
            }
        }
        if (_blockyController.isGameOver == true)
        {
            _blockyController.ChangeState(_blockyController.playerVictory);
        }
    }

    public void Exit()
    {
        
    }
}
