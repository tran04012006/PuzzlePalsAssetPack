using UnityEngine;

public class PlayerReceiveBlockCarryIdle : IPlayerStatePattern
{
    private BlockyController _blockyController;

    public PlayerReceiveBlockCarryIdle(
        BlockyController blockyController)
    {
        _blockyController = blockyController;
    }

    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 5);
    }

    public void Update()
    {
        if (_blockyController.receiveBlock == null)
            return;

        _blockyController.HoldBlock();

        if (_blockyController.moveX != 0f)
        {
            _blockyController.ChangeState(
                _blockyController.playerReceiveBlockRun
            );
        }

        if (Mathf.Approximately(
                _blockyController.moveY,
                -1f))
        {
            _blockyController.ChangeState(
                _blockyController.playerThrow
            );
        }

        if (_blockyController.isGameOver)
        {
            _blockyController.ChangeState(
                _blockyController.playerVictory
            );
        }
    }

    public void Exit()
    {
    }
}