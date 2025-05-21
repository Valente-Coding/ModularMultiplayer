using Unity.Netcode.Components;
using UnityEngine;

public class MP_ClientNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
