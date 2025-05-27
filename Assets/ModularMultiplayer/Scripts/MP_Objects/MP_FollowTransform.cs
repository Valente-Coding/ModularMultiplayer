using UnityEngine;

public class MP_FollowTransform : MonoBehaviour
{
    private Transform m_TargetTransform;
    private Vector3 m_Offset;

    public void SetTarget(Transform target, Vector3 offset)
    {
        m_TargetTransform = target;
        m_Offset = offset;
    }

    private void Update()
    {
        if (m_TargetTransform != null)
        {
            transform.position = m_TargetTransform.position + m_Offset;
            transform.rotation = m_TargetTransform.rotation;
        }
    }
}
