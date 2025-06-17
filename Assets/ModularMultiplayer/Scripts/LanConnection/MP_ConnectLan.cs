using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MP_ConnectLan : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_IPAddressText;

    void Start()
    {
        var transport = GameObject.FindFirstObjectByType<UnityTransport>();
        transport.ConnectionData.Address = GetAllLocalIPv4(NetworkInterfaceType.Ethernet).FirstOrDefault();

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            m_IPAddressText.text = GetAllLocalIPv4(NetworkInterfaceType.Ethernet).FirstOrDefault();
            Debug.Log($"Server started successfully on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
        };

        StartHost();
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient(TextMeshProUGUI p_IPAddressInput)
    {
        NetworkManager.Singleton.Shutdown();

        var transport = GameObject.FindFirstObjectByType<UnityTransport>();
        transport.ConnectionData.Address = p_IPAddressInput.text.Trim((char)8203);;
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


    public static string[] GetAllLocalIPv4(NetworkInterfaceType _type)
    {
        List<string> ipAddrList = new List<string>();
        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddrList.Add(ip.Address.ToString());
                    }
                }
            }
        }
        return ipAddrList.ToArray();
    }
}
