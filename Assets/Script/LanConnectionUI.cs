using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
public class LanConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_Text statusText;

    private const ushort Port = 7777;

    private NetworkManager networkManager;
    private UnityTransport transport;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
    }

    private void OnEnable()
    {
        networkManager.OnClientConnectedCallback += HandleConnected;
        networkManager.OnClientDisconnectCallback += HandleDisconnected;
    }

    private void OnDisable()
    {
        networkManager.OnClientConnectedCallback -= HandleConnected;
        networkManager.OnClientDisconnectCallback -= HandleDisconnected;
    }

    public void HostGame()
    {
        if (networkManager.IsListening)
            return;

        transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
        statusText.text = "Đang mở phòng...";

        if (!networkManager.StartHost())
            statusText.text = "Không thể mở phòng.";
    }

    public void JoinGame()
    {
        if (networkManager.IsListening)
            return;

        string hostIp = ipInput.text.Trim();

        if (!IPAddress.TryParse(hostIp, out IPAddress address) ||
            address.AddressFamily != AddressFamily.InterNetwork)
        {
            statusText.text = "Hãy nhập địa chỉ IPv4 của máy Host.";
            return;
        }

        transport.SetConnectionData(address.ToString(), Port);
        statusText.text = "Đang kết nối...";

        if (!networkManager.StartClient())
            statusText.text = "Không thể bắt đầu kết nối.";
    }

    private void HandleConnected(ulong clientId)
    {
        if (networkManager.IsHost)
        {
            int playerCount = networkManager.ConnectedClientsIds.Count;
            statusText.text = $"Đã kết nối: {playerCount}/2 người chơi";
        }
        else if (clientId == networkManager.LocalClientId)
        {
            statusText.text = "Đã kết nối với Host — 2/2 người chơi";
        }
    }

    private void HandleDisconnected(ulong clientId)
    {
        if (networkManager.IsHost)
        {
            statusText.text = "Người chơi đã rời phòng. Đang chờ kết nối...";
        }
        else
        {
            statusText.text = "Không kết nối được hoặc đã mất kết nối với Host.";
        }
    }
}