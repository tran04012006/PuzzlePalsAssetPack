using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public bool left = true;
    public int scoreLeft;
    public int scoreRight;

    public GameObject winEffect;

    public bool isGameOver;
    public bool leftIsWin;

    private struct MatchState :
        INetworkSerializable,
        IEquatable<MatchState>
    {
        public int leftScore;
        public int rightScore;

        public bool gameOver;
        public bool leftWins;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref leftScore);
            serializer.SerializeValue(ref rightScore);
            serializer.SerializeValue(ref gameOver);
            serializer.SerializeValue(ref leftWins);
        }

        public bool Equals(MatchState other)
        {
            return leftScore == other.leftScore &&
                   rightScore == other.rightScore &&
                   gameOver == other.gameOver &&
                   leftWins == other.leftWins;
        }
    }

    private NetworkVariable<MatchState> networkState =
        new NetworkVariable<MatchState>();

    private void Awake()
    {
        Instance = this;

        scoreLeft = 0;
        scoreRight = 0;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += onSceneLoad;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= onSceneLoad;
    }

    public override void OnNetworkSpawn()
    {
        networkState.OnValueChanged += OnStateChanged;

        if (IsServer)
        {
            PublishState();
        }
        else
        {
            ApplyState(networkState.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        networkState.OnValueChanged -= OnStateChanged;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (scoreLeft >= 20)
        {
            leftIsWin = true;
            isGameOver = true;
        }
        else if (scoreRight >= 20)
        {
            leftIsWin = false;
            isGameOver = true;
        }

        PublishState();
    }

    public void addScore(int score, bool left)
    {
        if (!IsSpawned || !IsServer)
            return;

        if (left)
        {
            scoreLeft += score;
        }
        else
        {
            scoreRight += score;
        }

        PublishState();
    }

    private void PublishState()
    {
        networkState.Value = new MatchState
        {
            leftScore = scoreLeft,
            rightScore = scoreRight,
            gameOver = isGameOver,
            leftWins = leftIsWin
        };
    }

    private void OnStateChanged(
        MatchState previous,
        MatchState next)
    {
        if (!IsServer)
        {
            ApplyState(next);
        }
    }

    private void ApplyState(MatchState state)
    {
        scoreLeft = state.leftScore;
        scoreRight = state.rightScore;
        leftIsWin = state.leftWins;
        isGameOver = state.gameOver;
    }

    public void PlayBlockMatched(
        Vector3 position,
        bool leftArea)
    {
        if (IsSpawned && IsServer)
        {
            PlayBlockMatchedRpc(position, leftArea);
        }
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    private void PlayBlockMatchedRpc(
        Vector3 position,
        bool leftArea)
    {
        BlockArea[] areas =
            FindObjectsByType<BlockArea>(
                FindObjectsSortMode.None
            );

        foreach (BlockArea area in areas)
        {
            if (area.left != leftArea ||
                area.blockMatched == null)
            {
                continue;
            }

            GameObject effect = Instantiate(
                area.blockMatched,
                position,
                Quaternion.identity
            );

            Destroy(effect, 0.5f);
            return;
        }
    }

    public void ReachCheckPoint(int checkPoint)
    {
    }

    private void onSceneLoad(
        Scene scene,
        LoadSceneMode loadSceneMode)
    {
    }
}