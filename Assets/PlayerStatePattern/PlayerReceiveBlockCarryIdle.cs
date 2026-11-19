public class PlayerReceiveBlockCarryIdle : IPlayerStatePattern
{
    private readonly BlockyController blockyController;

    public PlayerReceiveBlockCarryIdle(BlockyController blockyController)
    {
        this.blockyController = blockyController;
    }

    public void Enter()
    {
        blockyController._animator.SetInteger("Status", 5);
    }

    public void Update()
    {
        if (blockyController.isGameOver)
        {
            blockyController.ChangeState(blockyController.playerVictory);
            return;
        }

        if (blockyController.receiveBlock == null)
        {
            blockyController.ChangeState(blockyController._playerIdle);
            return;
        }

        if (blockyController.moveX != 0)
        {
            blockyController.ChangeState(
                blockyController.playerReceiveBlockRun
            );
        }
    }

    public void Exit()
    {
    }
}