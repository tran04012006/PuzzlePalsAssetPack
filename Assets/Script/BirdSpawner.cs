using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    public GameObject birdPrefab;
    private List<GameObject> birdList = new List<GameObject>();
    private int maxBird = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnBird()
    {
        birdList.RemoveAll(item => item == null);

        if (birdList.Count >= maxBird)
        {
            return;
        }
        
        GameObject birdTmp = Instantiate(birdPrefab, gameObject.transform.position, Quaternion.identity);
        Debug.Log("De ra con chim " + birdTmp.name);
        birdList.Add(birdTmp);
    }
}
