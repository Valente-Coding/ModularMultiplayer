using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MP3D_SetPlayerCamera : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!IsOwner || !IsSpawned) return;

        Camera.main.AddComponent<MP3D_CameraController>().SetTarget(transform);
    }
}
