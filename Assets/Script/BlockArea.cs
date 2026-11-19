using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlockArea : MonoBehaviour
{
    public Dictionary<Vector2Int, BlockController> blockList =
        new Dictionary<Vector2Int, BlockController>();

    private Dictionary<Vector2Int, bool> visited;

    public GameObject blockMatched;
    public bool left = true;
    public ScoreUpdate scoreUpdate;

    private bool IsServer =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsServer;

    public void AddBlock(
        Vector2Int gridPos,
        BlockController block)
    {
        if (!IsServer)
            return;

        while (blockList.ContainsKey(gridPos))
        {
            blockList.Remove(gridPos);
        }

        blockList[gridPos] = block;

        CheckMatch(gridPos);
    }

    private void CheckMatch(Vector2Int gridPos)
    {
        Dictionary<BlockController, Vector2Int> result =
            FindConnectedBlock(gridPos);

        if (result.Count >= 4)
        {
            scoreUpdate.UpdateScore(result.Count);
            RemoveBlock(result);
        }
        else
        {
            result.Clear();
        }
    }

    private bool CanVisit(
        Vector2Int pos,
        Dictionary<Vector2Int, bool> visited,
        string target)
    {
        if (visited.ContainsKey(pos))
            return false;

        if (!blockList.TryGetValue(
                pos,
                out BlockController block))
        {
            return false;
        }

        if (block == null || block._blockData == null)
            return false;

        return block._blockData.name == target;
    }

    private void DFS(
        ref Dictionary<Vector2Int, bool> visited,
        Vector2Int startPos,
        ref Dictionary<BlockController, Vector2Int> result,
        string target)
    {
        visited[startPos] = true;
        Check(startPos, ref result, target);

        Vector2Int left =
            startPos + Vector2Int.left * 2;

        if (CanVisit(left, visited, target))
        {
            DFS(ref visited, left, ref result, target);
        }

        Vector2Int right =
            startPos + Vector2Int.right * 2;

        if (CanVisit(right, visited, target))
        {
            DFS(ref visited, right, ref result, target);
        }

        Vector2Int up =
            startPos + Vector2Int.up * 2;

        if (CanVisit(up, visited, target))
        {
            DFS(ref visited, up, ref result, target);
        }

        Vector2Int down =
            startPos + Vector2Int.down * 2;

        if (CanVisit(down, visited, target))
        {
            DFS(ref visited, down, ref result, target);
        }
    }

    private Dictionary<BlockController, Vector2Int>
        FindConnectedBlock(Vector2Int gridPos)
    {
        visited = new Dictionary<Vector2Int, bool>();

        Dictionary<BlockController, Vector2Int> result =
            new Dictionary<BlockController, Vector2Int>();

        string target = blockList[gridPos]._blockData.name;

        DFS(ref visited, gridPos, ref result, target);

        visited.Clear();
        return result;
    }

    private void Check(
        Vector2Int pos,
        ref Dictionary<BlockController, Vector2Int> result,
        string target)
    {
        BlockData blockData = blockList[pos]._blockData;

        if (blockData.name == target)
        {
            result[blockList[pos]] = pos;
        }
    }

    private void CheckUnsupportedBlocks()
    {
        var blocks =
            new List<KeyValuePair<Vector2Int, BlockController>>(
                blockList
            );

        blocks.Sort((a, b) => a.Key.y.CompareTo(b.Key.y));

        foreach (var item in blocks)
        {
            Vector2Int pos = item.Key;
            BlockController block = item.Value;

            if (block == null)
                continue;

            if (!block.HasBlockBelow(pos))
            {
                block.Fall();
            }
        }
    }

    private void RemoveBlock(
        Dictionary<BlockController, Vector2Int> result)
    {
        if (!IsServer)
            return;

        foreach (var item in result)
        {
            blockList.Remove(item.Value);

            BlockController block = item.Key;

            if (block == null || !block.IsSpawned)
                continue;

            GameManager.Instance.PlayBlockMatched(
                block.transform.position,
                left
            );

            block.disappear = true;
            block.GetComponent<Collider2D>().enabled = false;

            block.NetworkObject.Despawn();

            blockList.Remove(item.Value);
        }

        result.Clear();
        CheckUnsupportedBlocks();
    }
}