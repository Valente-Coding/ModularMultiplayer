using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class MinMax 
{
    public float Min = 0f;
    public float Max = 0f;

    public MinMax(float p_Min, float p_Max)
    {
        Min = p_Min;
        Max = p_Max;
    }
}

public class MP2D_CameraController : NetworkBehaviour
{
    private class PointInSpace
    {
        public Vector2 Position;
        public float Time;
    }

    [Header("Default Camera Settings")]
    [SerializeField] private Vector2 m_DefaultOffset;
    [SerializeField] private float m_DefaultZoom = 5f;

    [Header("Camera Settings")]
    [SerializeField] private MinMax m_MinMaxZoom = new MinMax(2f, 10f);
    [SerializeField] private float m_ZoomSpeed = 1f;
    [SerializeField] private float m_Stiffness = 0.125f;
    [SerializeField] private float m_Delay = 1f;


    private Transform m_PlayerCamera;
    private Vector2 m_CameraOffset;
    private float m_CameraZoom;
    private Queue<PointInSpace> m_PointsInSpace = new Queue<PointInSpace>();

    private void Start()
    {
        if (!IsOwner) return;

        m_PlayerCamera = FindFirstObjectByType<Camera>().transform;
        m_CameraOffset = m_DefaultOffset;
        m_CameraZoom = m_DefaultZoom;
    }

    private void Update()
    {
        if (!IsOwner) return;

        m_PointsInSpace.Enqueue(new PointInSpace() { Position = transform.position, Time = Time.time });
        m_CameraZoom = Mathf.Clamp(m_CameraZoom + (-Input.mouseScrollDelta.y * m_ZoomSpeed), m_MinMaxZoom.Min, m_MinMaxZoom.Max);
    }


    void LateUpdate()
    {
        while (m_PointsInSpace.Count > 0 && m_PointsInSpace.Peek().Time <= Time.time - m_Delay + Mathf.Epsilon)
        {
            m_PointsInSpace.Dequeue();
        }

        if(m_PointsInSpace.Count > 0){
            Vector2 l_DesiredPosition = m_PointsInSpace.Peek().Position + m_CameraOffset;

            Vector2 l_SmoothedPosition = Vector2.Lerp(m_PlayerCamera.position, l_DesiredPosition, m_Stiffness * Time.deltaTime);
            m_PlayerCamera.position = new Vector3(l_SmoothedPosition.x, l_SmoothedPosition.y, -m_CameraZoom);
        }

    }
}
