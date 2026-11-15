using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class BlockArea : MonoBehaviour
{
    public Dictionary<Vector2Int, BlockController> blockList = new Dictionary<Vector2Int, BlockController>();
    private Dictionary<Vector2Int, bool> visited;
    public GameObject blockMatched;

    
    public void AddBlock(Vector2Int gridPos, BlockController block)
    {
        Debug.Log("So luong ptu truoc khi add trong blockList: " + blockList.Count);
        blockList[gridPos] = block;
        Debug.Log("So luong ptu sau khi add trong blockList: " + blockList.Count);
        
        CheckMatch(gridPos);
    }

    void CheckMatch(Vector2Int gridPos)
    {
        //tim xem blockName nay co tao ra 4 hang / cot giong nhau ko
        Debug.Log("bat dau FindConnectedBlock!!!!!!!!!!!");
        Dictionary<BlockController, Vector2Int> result = FindConnectedBlock(gridPos);
        Debug.Log("----------------------------------------- " + result.Count);

        Debug.Log("result.Count: !!!!!!!!!!!************* " + result.Count);
        
        if (result.Count >= 4)
        {
            Debug.Log("Da du 4");
            RemoveBlock(result);
        }

        else
        {
            Debug.Log("moi co " + result.Count);
            result.Clear();
            Debug.Log("clear result. Result = " + result.Count);
        }
    }
    //tham chieu, tham tri
    
    void DFS(ref Dictionary<Vector2Int, bool> visited, Vector2Int startPos, 
        ref Dictionary<BlockController, Vector2Int> result, string target)
    {
        //Debug.Lo_blockAreag("kiem tra " + startPos);
        visited[startPos] = true;
        Check(startPos, ref result, target);
        //Debug.Log("ben trai no la: " + blockList.ContainsKey(startPos + Vector2Int.left*2));
        //Debug.Log("ben phai no la: " + blockList.ContainsKey(startPos + Vector2Int.right*2));
        //Debug.Log("ben tren no la: " + blockList.ContainsKey(startPos + Vector2Int.up*2));
        //Debug.Log("ben duoi no la: " + blockList.ContainsKey(startPos + Vector2Int.down*2));

        if (blockList.ContainsKey(startPos + Vector2Int.left*2) == true && 
            visited.ContainsKey(startPos + Vector2Int.left*2) == false)
        {
            DFS(ref visited, startPos + Vector2Int.left*2, ref result, target);
        }
        
        if (blockList.ContainsKey(startPos + Vector2Int.right*2) == true && 
            visited.ContainsKey(startPos + Vector2Int.right*2) == false)
        {
            DFS(ref visited, startPos + Vector2Int.right*2, ref result, target);
        }
        
        if (blockList.ContainsKey(startPos + Vector2Int.up*2) == true && 
            visited.ContainsKey(startPos + Vector2Int.up*2) == false)
        {
            DFS(ref visited, startPos + Vector2Int.up*2, ref result, target);
        }
        
        if (blockList.ContainsKey(startPos + Vector2Int.down*2) == true && 
            visited.ContainsKey(startPos + Vector2Int.down*2) == false)
        {
            Debug.Log("di xuong ben duoi !!!!!!!!!!!!!!!!!!!!!!!!!!");
            DFS(ref visited, startPos + Vector2Int.down*2, ref result, target);
        }
    }
    
    Dictionary<BlockController, Vector2Int> FindConnectedBlock(Vector2Int gridPos)
    {
        visited = new Dictionary<Vector2Int, bool>();
        Dictionary<BlockController, Vector2Int> result = new Dictionary<BlockController, Vector2Int>();

        string target = blockList[gridPos]._blockData.name;
        DFS(ref visited, gridPos, ref result, target);
        visited.Clear();
        return result;
    }

    void Check(Vector2Int pos, ref Dictionary<BlockController, Vector2Int> result, string target)
    {
        Debug.Log("blockList: ");
        foreach (var item in blockList)
        {
            //Debug.Log(item);
        }

        BlockData blockData = blockList[pos]._blockData;
        
        if (blockData.name == target)
        {
            result.Add(blockList[pos], pos);
            //Debug.Log("them " + pos + " vao result");
        }
    }

    void RemoveBlock(Dictionary<BlockController, Vector2Int>result)
    {
        foreach (var item in result)
        {
            //Debug.Log("goi " + item.Value);
            GameObject blockMatchedTmp = Instantiate(blockMatched, item.Key.transform.position, Quaternion.identity);
            Destroy(item.Key.gameObject, 0.4f);
            Destroy(blockMatchedTmp, 0.5f);
            blockList.Remove(item.Value);
        }
        result.Clear();
        Debug.Log("clear result. Result = " + result.Count);
    }
}

