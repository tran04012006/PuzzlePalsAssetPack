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
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    public void UpdateScore(int score)
    {
        //cap nhat
        Debug.Log("cong diem!!");
        if (left == true)
        {
            GameManager.Instance.addScore(score,true);
            Debug.Log("cap nhat " + scoreText.name);
            Debug.Log("GameManager.Instance.scoreLeft: " + GameManager.Instance.scoreLeft);
            scoreText.text = "Score: " + GameManager.Instance.scoreLeft;
            Debug.Log("GameManager.Instance.scoreLeft: " + GameManager.Instance.scoreLeft);
        }
        else
        {
            GameManager.Instance.addScore(score,false);
            scoreText.text = "Score: " + GameManager.Instance.scoreRight;
        }
        //Debug.Log("GameManager.Instance.score moi = " + GameManager.Instance.score);
        //Debug.Log("score = " + score);
    }
}