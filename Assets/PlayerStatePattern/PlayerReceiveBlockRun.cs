public class PlayerReceiveBlockRun : IPlayerStatePattern
{
    private BlockyController _blockyController;

    public PlayerReceiveBlockRun(BlockyController blockyController)
    {
        this._blockyController = blockyController;
    }
    
    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 3);
    }

    public void Update()
    {
        if (_blockyController.receiveBlock != null)
        {
            _blockyController.receiveBlock.transform.SetParent(_blockyController.gameObject.transform);
            _blockyController.receiveBlock.transform.position = _blockyController.holdBlockPos.transform.position;
        }

        if (_blockyController.moveX == 0)
        {
            _blockyController.ChangeState(_blockyController.playerReceiveBlockCarryIdle);
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
