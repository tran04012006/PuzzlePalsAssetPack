using UnityEngine;

public class PlayerThrow : IPlayerStatePattern
{
    private readonly BlockyController blockyController;

    private const float Duration = 0.5f;
    private float elapsedTime;

    public PlayerThrow(BlockyController blockyController)
    {
        this.blockyController = blockyController;
    }

    public void Enter()
    {
        blockyController._animator.SetInteger("Status", 4);
        elapsedTime = 0f;
    }

    public void Update()
    {
        if (blockyController.isGameOver)
        {
            blockyController.ChangeState(blockyController.playerVictory);
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= Duration)
        {
            blockyController.ChangeState(blockyController._playerIdle);
        }
    }

    public void Exit()
    {
    }
}