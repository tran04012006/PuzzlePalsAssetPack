using System;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUpdate : MonoBehaviour
{
    private Text scoreText;
    public bool left = true;
    
    private void Awake()
    {
        scoreText = GetComponent<Text>();
        //scoreText.text = "...";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    public void UpdateScore(int score)
    {
        //cap nhat
        Debug.Log("cong diem!!");
        /*
        if (scoreText.text == null)
        {
            Debug.Log("ko thay scoreText.text :(((((((((((((((((");
            return;
        }
        */
        //Debug.Log("GameManager.Instance.score cu = " + GameManager.Instance.score);
        if (left == true)
        {
            GameManager.Instance.addScore(score,true);
            Debug.Log("cap nhat " + scoreText.name);
            Debug.Log("GameManager.Instance.scoreLeft: " + GameManager.Instance.scoreLeft);
            scoreText.text = ".....";
            Debug.Log("GameManager.Instance.scoreLeft: " + GameManager.Instance.scoreLeft);
        }
        else
        {
            GameManager.Instance.addScore(score,false);
            //scoreText.text = "Score: " + GameManager.Instance.scoreRight;
        }
        //Debug.Log("GameManager.Instance.score moi = " + GameManager.Instance.score);
        //Debug.Log("score = " + score);
    }
}