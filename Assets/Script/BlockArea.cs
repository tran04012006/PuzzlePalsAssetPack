using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
public class BlockArea : MonoBehaviour
{
    public Dictionary<Vector2Int, BlockController> blockList = new Dictionary<Vector2Int, BlockController>();
    private Dictionary<Vector2Int, bool> visited;
    public GameObject blockMatched;
    public bool left = true;
    public ScoreUpdate scoreUpdate;
    private int score = 0;
        
    public void AddBlock(Vector2Int gridPos, BlockController block)
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }
        while (blockList.ContainsKey(gridPos))
        {
            Debug.Log("da co !!!!!!!!! " + gridPos);
            blockList.Remove(gridPos);
        }
        //Debug.Log("So luong ptu truoc khi add trong blockList: " + blockList.Count);
        blockList[gridPos] = block;
        //Debug.Log("So luong ptu sau khi add trong blockList: " + blockList.Count);
        
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
            scoreUpdate.UpdateScore(result.Count);
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
    
    
    bool CanVisit(
        Vector2Int pos,
        Dictionary<Vector2Int, bool> visited,
        string target)
    {
        //vi tri nay da duoc visit chua
        if (visited.ContainsKey(pos))
            return false;
        //o nay co chua block ko
        if (!blockList.TryGetValue(pos, out BlockController block))
            return false;
        //kiem tra null
        if (block == null ||
            block._blockData == null ||
            block.isFalling ||
            block.disappear)
        {
            return false;
        }
        //neu giong target thi tra ve true
        return block._blockData.name == target;
    }

    
    void DFS(ref Dictionary<Vector2Int, bool> visited, Vector2Int startPos, 
        ref Dictionary<BlockController, Vector2Int> result, string target)
    {
        //Debug.Lo_blockAreag("kiem tra " + startPos);
        visited[startPos] = true;
        Check(startPos, ref result, target);
        //trai
        Vector2Int left = startPos + Vector2Int.left * 2;
        
        if (CanVisit(left, visited, target))
        {
            DFS(ref visited, left, ref result, target);
        }
        //phai
        Vector2Int right = startPos + Vector2Int.right * 2;

        if (CanVisit(right, visited, target))
        {
            DFS(ref visited, right, ref result, target);
        }
        //tren
        Vector2Int up = startPos + Vector2Int.up * 2;

        if (CanVisit(up, visited, target))
        {
            DFS(ref visited, up, ref result, target);
        }
        //duoi
        Vector2Int down = startPos + Vector2Int.down * 2;

        if (CanVisit(down, visited, target))
        {
            DFS(ref visited, down, ref result, target);
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
            result[blockList[pos]] = pos;
            //Debug.Log("them " + pos + " vao result");
        }
    }


    void CheckUnsupportedBlocks()
    {
        //duyet qua cac phan tu trong blockList
        var blocks = new List<KeyValuePair<Vector2Int, BlockController>>(blockList);
        //sap xep theo toa do y giam dan
        blocks.Sort((a, b) => a.Key.y.CompareTo(b.Key.y));
        foreach (var item in blocks)
        {
            Vector2Int pos = item.Key;
            BlockController block = item.Value;
            
            if (block == null)
                continue;

            if (!block.HasBlockBelow(pos))
            {
                // Cho block rơi
                block.Fall();
            }
        }
    }

    void RemoveBlock(Dictionary<BlockController, Vector2Int> result)
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        // Xóa các ô trước khi kiểm tra block mất chỗ đỡ.
        foreach (var item in result)
        {
            blockList.Remove(item.Value);
        }

        foreach (var item in result)
        {
            BlockController block = item.Key;

            if (block == null || !block.IsSpawned)
                continue;

            if (blockMatched != null)
            {
                GameObject effect = Instantiate(
                    blockMatched,
                    block.transform.position,
                    Quaternion.identity
                );

                Destroy(effect, 0.5f);
            }

            block.disappear = true;
            block.GetComponent<Collider2D>().enabled = false;
            block.NetworkObject.Despawn();
        }

        result.Clear();
        CheckUnsupportedBlocks();
    }
}

