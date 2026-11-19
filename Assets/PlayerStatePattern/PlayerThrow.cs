using UnityEngine;

public class PlayerThrow : IPlayerStatePattern
{
    private BlockyController _blockyController;

    private float duration = 0.5f;
    private float time = 0f;

    public PlayerThrow(BlockyController blockyController)
    {
        _blockyController = blockyController;
    }

    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 4);

        time = 0f;
        _blockyController.ThrowBlock();
    }

    public void Update()
    {
        time += Time.deltaTime;

        if (duration - time <= 0.2f)
        {
            _blockyController.ChangeState(
                _blockyController._playerIdle
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