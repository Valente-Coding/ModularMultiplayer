using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public Vector3 Position;
        public float Time;
    }

    [Header("Default Camera Settings")]
    [SerializeField] private Vector3 m_DefaultOffset;

    [Header("Camera Settings")]
    [SerializeField] private MinMax m_MinMaxZoom = new MinMax(2f, 10f);
    [SerializeField] private float m_ZoomSpeed = 1f;
    [SerializeField] private float m_Stiffness = 0.125f;
    [SerializeField] private float m_Delay = 1f;


    private Transform m_PlayerCamera;
    private Vector3 m_
    private Queue<PointInSpace> m_PointsInSpace = new Queue<PointInSpace>();

    private void Start()
    {
        if (!IsOwner) return;

        m_PlayerCamera = FindFirstObjectByType<Camera>().transform;
    }

    private void Update()
    {
        if (!IsOwner) return;

        m_PointsInSpace.Enqueue(new PointInSpace() { Position = transform.position, Time = Time.time });
    }


    void LateUpdate()
    {
        // Move the camera to the position of the target X seconds ago
        while (m_PointsInSpace.Count > 0 && m_PointsInSpace.Peek().Time <= Time.time - m_Delay + Mathf.Epsilon)
        {
            m_PointsInSpace.Dequeue(); //Remove the position from the queue
        }

        if(m_PointsInSpace.Count > 0){
        // Calculate the desired position with the delay
            Vector3 desiredPosition = m_PointsInSpace.Peek().Position + new Vector3(m_DefaultOffset.x, m_DefaultOffset.y, -Input.mouseScrollDelta.y * m_ZoomSpeed);
            // Move the camera smoothly to the desired position
            Vector3 smoothedPosition = Vector3.Lerp(m_PlayerCamera.position, desiredPosition, m_Stiffness * Time.deltaTime);
            m_PlayerCamera.position = smoothedPosition;
        }

    }
}
