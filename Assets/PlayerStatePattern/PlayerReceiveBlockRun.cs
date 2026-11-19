public class PlayerReceiveBlockRun : IPlayerStatePattern
{
    private readonly BlockyController blockyController;

    public PlayerReceiveBlockRun(BlockyController blockyController)
    {
        this.blockyController = blockyController;
    }

    public void Enter()
    {
        blockyController._animator.SetInteger("Status", 3);
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

        if (blockyController.moveX == 0)
        {
            blockyController.ChangeState(
                blockyController.playerReceiveBlockCarryIdle
            );
        }
    }

    public void Exit()
    {
    }
}