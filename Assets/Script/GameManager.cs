using System;
using System.IO;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool left = true;
    public int scoreLeft;
    public int scoreRight;

    //score: diem player dat duoc
    public bool isGameOver = false;
    private bool scoreAtStart = false;
    
    //khai bao action, bao khi diem thay doi

    private BlockyController playerController;
    
    void Awake()
    {
        Debug.Log("goi awake cua gm");
        Instance = this;
        scoreLeft = 0;
        scoreRight = 0;
    }
    
    private void OnEnable()
    {
        Debug.Log("dang ki su kien thay doi score");
        SceneManager.sceneLoaded += onSceneLoad;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= onSceneLoad;
    }

    private void Update()
    {
    }

    public void ReachCheckPoint(int checkPoint)
    {
        //SoundManager.Instance.currentSound = SoundManager.SoundID.ReachCheckpoint;
        //luu lai du lieu
        //SaveCheckPoint(level[checkPoint], checkPoint, hp, this.score);
        //chuan bi chuyen sang scene tiep theo
    }

    void onSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log("onSceneLoad");
    }
    
    public void addScore(int score, bool left)
    {
        //tang score
        Debug.Log("cong " + score);
        if (left == true)
        {
            scoreLeft += score;
            //thong bao diem thay doi
            Debug.Log("scoreLeft: " + scoreLeft);
        }
        else
        {
            scoreRight += score;
            //thong bao diem thay doi
            Debug.Log("scoreRight: " + scoreRight);
        }
        
    }
    /*
    public void minusScore(int score)
    {
        this.score -= score;
        if (this.score <= 0)
            this.score = 0;
        onScoreChange?.Invoke(this.score);
        if (this.score <= 0)
            gameOver();
        
    }
    */
    public void gameOver()
    {
        isGameOver = true;
        //instantiate gameover prefab hoac dung game
    }
}