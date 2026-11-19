using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class LanPlayerAssignment : MonoBehaviour
{
    [SerializeField] private NetworkObject leftPlayer;
    [SerializeField] private NetworkObject rightPlayer;
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        leftPlayer.DontDestroyWithOwner = true;
        rightPlayer.DontDestroyWithOwner = true;
    }

    private void OnEnable()
    {
        networkManager.OnClientConnectedCallback += AssignPlayer;
        networkManager.OnClientDisconnectCallback += ReleasePlayer;
    }

    private void OnDisable()
    {
        networkManager.OnClientConnectedCallback -= AssignPlayer;
        networkManager.OnClientDisconnectCallback -= ReleasePlayer;
    }

    private void AssignPlayer(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        if (clientId == NetworkManager.ServerClientId)
        {
            leftPlayer.ChangeOwnership(clientId);

            Debug.Log(
                $"Host {clientId} được giao nhân vật trái.",
                leftPlayer
            );

            return;
        }

        if (rightPlayer.OwnerClientId != NetworkManager.ServerClientId)
            return;

        rightPlayer.ChangeOwnership(clientId);

        Debug.Log(
            $"Client {clientId} được giao nhân vật phải.",
            rightPlayer
        );
    }

    private void ReleasePlayer(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        if (rightPlayer.IsSpawned &&
            rightPlayer.OwnerClientId == clientId)
        {
            rightPlayer.RemoveOwnership();

            Debug.Log(
                "Client đã rời phòng. Sân phải đang chờ người chơi.",
                rightPlayer
            );
        }
    }
}