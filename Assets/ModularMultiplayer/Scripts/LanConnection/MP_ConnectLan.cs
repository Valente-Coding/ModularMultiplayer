using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MP_ConnectLan : MonoBehaviour
{

    void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () => {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            Debug.Log($"Server started successfully on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
        };
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void StopAll()
    {
        NetworkManager.Singleton.Shutdown();
    }
}
