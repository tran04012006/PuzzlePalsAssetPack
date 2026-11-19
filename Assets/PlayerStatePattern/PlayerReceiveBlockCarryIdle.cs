using UnityEngine;

public class PlayerReceiveBlockCarryIdle : IPlayerStatePattern
{
    private BlockyController _blockyController;

    public PlayerReceiveBlockCarryIdle(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
    }
    
    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 5);
    }

    public void Update()
    {
        if (_blockyController.receiveBlock == null)
            return;
        _blockyController.receiveBlock.transform.SetParent(_blockyController.gameObject.transform);
        _blockyController.receiveBlock.transform.position = _blockyController.holdBlockPos.transform.position;

        if (_blockyController.moveX != 0)
        {
            _blockyController.ChangeState(_blockyController.playerReceiveBlockRun);
        }

        if (Mathf.Approximately(_blockyController.moveY, -1))
        {
            _blockyController.ChangeState(_blockyController.playerThrow);
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
