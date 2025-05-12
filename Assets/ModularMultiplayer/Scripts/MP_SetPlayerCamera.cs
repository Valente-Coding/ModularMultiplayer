using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MP_SetPlayerCamera : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!IsOwner || !IsSpawned) return;

        Camera.main.AddComponent<MP_CameraController>().SetTarget(transform);
    }
}
