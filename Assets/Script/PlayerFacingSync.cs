using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(SpriteRenderer))]
public class PlayerFacingSync : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;

    private NetworkVariable<bool> networkFlipX = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        networkFlipX.OnValueChanged += HandleFacingChanged;

        if (IsOwner)
        {
            networkFlipX.Value = spriteRenderer.flipX;
        }

        ApplyFacing(networkFlipX.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkFlipX.OnValueChanged -= HandleFacingChanged;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (networkFlipX.Value != spriteRenderer.flipX)
        {
            networkFlipX.Value = spriteRenderer.flipX;
        }
    }

    private void HandleFacingChanged(bool previousValue, bool newValue)
    {
        ApplyFacing(newValue);
    }

    private void ApplyFacing(bool flipX)
    {
        spriteRenderer.flipX = flipX;
    }
}