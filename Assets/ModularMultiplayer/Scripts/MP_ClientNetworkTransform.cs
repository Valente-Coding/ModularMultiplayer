using Unity.Netcode.Components;
using UnityEngine;

public class MP_ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
