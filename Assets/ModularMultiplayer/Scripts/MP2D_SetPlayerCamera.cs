using UnityEngine;

[RequireComponent(typeof(SpringJoint2D))]
public class MP2D_SetPlayerCamera : MonoBehaviour
{
    private SpringJoint2D m_Spring2D;

    private void Start()
    {
        m_Spring2D = GetComponent<SpringJoint2D>();
    }

    public void ConnectToSpring(Rigidbody2D p_TargetRB)
    {
        m_Spring2D.connectedBody = p_TargetRB;
    }
}
