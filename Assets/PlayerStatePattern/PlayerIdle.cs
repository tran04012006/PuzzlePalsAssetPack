using UnityEngine;

public class PlayerIdle : IPlayerStatePattern
{
    private BlockyController _blockyController;
    private Animator _animator;
    
    //constructor
    public PlayerIdle(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
        _animator = _blockyController._animator;
    }
    public void Enter()
    {
        _animator.SetInteger("Status", 0);
    }

    public void Update()
    {
        if (_blockyController.moveX != 0)
        {
            _blockyController.ChangeState(_blockyController.playerWalk);
        }

        if (Mathf.Approximately(_blockyController.moveY, 1))
        {
            //do tay len
            _blockyController.ReceiveBlock();
        }

        if (_blockyController.isGameOver == true && _blockyController.leftArea == GameManager.Instance.leftIsWin)
        {
            _blockyController.ChangeState(_blockyController.playerVictory);
        }
        if (_blockyController.isGameOver == true && _blockyController.leftArea != GameManager.Instance.leftIsWin)
        {
            _blockyController.ChangeState(_blockyController.playerLose);
        }
    }


    public void Exit()
    {
        
    }
}
