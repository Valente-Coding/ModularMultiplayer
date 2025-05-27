using Unity.Netcode;
using UnityEngine;

public class MP_ConnectLan : MonoBehaviour
{
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
