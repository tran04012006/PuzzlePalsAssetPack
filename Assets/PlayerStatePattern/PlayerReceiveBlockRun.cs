public class PlayerReceiveBlockRun : IPlayerStatePattern
{
    private BlockyController _blockyController;

    public PlayerReceiveBlockRun(
        BlockyController blockyController)
    {
        _blockyController = blockyController;
    }

    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 3);
    }

    public void Update()
    {
        if (_blockyController.receiveBlock != null)
        {
            _blockyController.HoldBlock();
        }

        if (_blockyController.moveX == 0f)
        {
            _blockyController.ChangeState(
                _blockyController.playerReceiveBlockCarryIdle
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