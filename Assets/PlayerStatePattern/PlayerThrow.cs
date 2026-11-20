using UnityEngine;

public class PlayerThrow : IPlayerStatePattern
{
    private BlockyController _blockyController;
    private float duration = 0.5f;
    private float time = 0f;
    
    public PlayerThrow(BlockyController _blockyController)
    {
        this._blockyController = _blockyController;
    }
    
    public void Enter()
    {
        _blockyController._animator.SetInteger("Status", 4);
        time = 0f;
        BlockController blockController = _blockyController.receiveBlock.GetComponent<BlockController>();
        blockController.GoDown();
        _blockyController.receiveBlock = null;
        //Debug.Log("_blockyController.receiveBlock = " + _blockyController.receiveBlock + "!!!!!!@@@@@@@@@@@@@@@");
    }

    public void Update()
    {
        time = time + Time.deltaTime;
        
        if (duration - time <= 0.2f)
        {
            _blockyController.ChangeState(_blockyController._playerIdle);
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
