using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;



[Serializable]
public struct Gesture
{
    public enum Type
    {
        VICTORY = 0,
        OKHAND  = 1,
        FIST    = 2,
        GANG    = 10,
        MAX     = 3
    }
    
    public string name;
    public List<Vector3> fingerData;
    public UnityEvent OnRecognized;
    public Type type;
}

struct RecognizedGesture
{
    public bool found;
    public Gesture gesture;
}

public class HandController : MonoBehaviour
{
    [SerializeField] private OVRHand m_leftHand;
    [SerializeField] private OVRHand m_rightHand;

    [SerializeField] private OVRSkeleton m_skeleton;
    // private List<OVRBone> m_fingerBones;
    
    [SerializeField] private float m_recognizeThreshold = 0.1f;
    [SerializeField] private List<Gesture> m_gesture;
    private Gesture? m_lastGesture;

    #region EVENTS

    public delegate void OnNewGestureHandler(Gesture? p_newGesture, Gesture? p_oldGesture);

    public event OnNewGestureHandler OnNewGesture;
    #endregion

    // Update is called once per frame
    void Update()
    {
        RecognizedGesture l_result = Recognize();
        if (l_result.found)
        {
            if (!l_result.gesture.Equals(m_lastGesture))
            {
                // Debug.Log($"New gesture {l_result.gesture.name}");
                OnNewGesture?.Invoke(l_result.gesture, m_lastGesture);
                m_lastGesture = l_result.gesture;
            }
        }
        else
        {
            if (m_lastGesture.HasValue) OnNewGesture?.Invoke(null, m_lastGesture);
            
            m_lastGesture = null;
        }
    }

    private void Save()
    {
        Gesture l_gesture = new Gesture();
        l_gesture.name = "New gesture";

        List<Vector3> l_data = new List<Vector3>();

        foreach (OVRBone l_bone in m_skeleton.Bones)
        {
            l_data.Add(m_skeleton.transform.InverseTransformPoint(l_bone.Transform.position));
        }

        l_gesture.fingerData = l_data;

        m_gesture.Add(l_gesture);
    }

    private RecognizedGesture Recognize()
    {
        RecognizedGesture l_result = new RecognizedGesture();
        l_result.gesture = new Gesture();

        float l_currentMin = Mathf.Infinity;

        foreach (Gesture l_testGesture in m_gesture)
        {
            float l_totalDist = 0.0f;
            bool l_discarded = false;

            for (int i = 0; i < m_skeleton.Bones.Count; i++)
            {
                Vector3 l_currentData =
                    m_skeleton.transform.InverseTransformPoint((m_skeleton.Bones[i].Transform.position));
                float l_dist = Vector3.Distance(l_currentData, l_testGesture.fingerData[i]);

                if (l_dist > m_recognizeThreshold)
                {
                    l_discarded = true;
                    break;
                }
            }

            if (!l_discarded && l_totalDist < l_currentMin)
            {
                l_currentMin = l_totalDist;
                l_result.gesture = l_testGesture;
                l_result.found = true;
            }
        }

        return l_result;
    }
}