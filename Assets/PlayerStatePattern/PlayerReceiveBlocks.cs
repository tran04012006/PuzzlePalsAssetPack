using Unity.Collections;
using UnityEngine;

public class PlayerReceiveBlocksIdle : IPlayerStatePattern
{
    private BlockyController _blockyController;
    private float duration = 0.5f;
    private float time;
    
    public PlayerReceiveBlocksIdle(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
    }

    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 2);
        time = 0;
    }

    public void Update()
    {
        
        time = time + Time.deltaTime;
        if (time >= duration)
        {
            _blockyController.ChangeState(_blockyController._playerIdle);
        }
        
        if (_blockyController.moveX != 0)
        {
            _blockyController.ChangeState(_blockyController.playerReceiveBlockRun);
        }

        if (Mathf.Approximately(_blockyController.moveY, -1))
        {
            _blockyController.ChangeState(_blockyController.playerThrow);
        }
        
    }

    public void Exit()
    {
        
    }
    
}
