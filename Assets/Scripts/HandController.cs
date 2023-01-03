using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


[Serializable]
public struct Gesture
{
    public enum Type
    {
        VICTORY = 0,
        OKHAND  = 1,
        FIST    = 2,
        METAL   = 3,
        MAX     = 4
    }
    
    public string name;
    public List<Vector3> fingerData;
    // public UnityEvent OnRecognized;
    public Type type;
    public HandController.Side side;
}

struct RecognizedGesture
{
    public bool found;
    public HandController.Side side;
    public Gesture gesture;
}

public class HandController : MonoBehaviour
{
    public enum Side
    {
        LEFT  = 0,
        RIGHT = 1
    }
    
    [SerializeField] private OVRSkeleton m_leftSkeleton;
    [SerializeField] private OVRSkeleton m_rightSkeleton;
    
    [SerializeField] private float m_recognizeThreshold = 0.1f;
    
    // debug purpose
    [SerializeField] private List<Gesture> m_leftDbgGesture;
    [SerializeField] private List<Gesture> m_rightDbgGesture;
    
    private Gesture? m_lastLeftGesture;
    private Gesture? m_lastRightGesture;
    
    #region EVENTS

    public delegate void OnNewGestureHandler(Gesture? p_newGesture, Gesture? p_oldGesture);

    public event OnNewGestureHandler OnNewLeftGesture;
    public event OnNewGestureHandler OnNewRightGesture;
    #endregion

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Keypad1)) DbgSave(Side.LEFT);
        if (Input.GetKeyDown(KeyCode.Keypad3)) DbgSave(Side.RIGHT);
        if (Input.GetKeyDown(KeyCode.Space)) GameObject.FindObjectOfType<NoteManager>().StartGame();
        #endif
        
        RecognizedGesture l_leftResult = Recognize(Side.LEFT);
        RecognizedGesture l_rightResult = Recognize(Side.RIGHT);

        CheckResult(l_leftResult);
        CheckResult(l_rightResult);
    }

    private void CheckResult(RecognizedGesture p_recognizedGesture)
    {
        Gesture? l_testGesture = p_recognizedGesture.side == Side.LEFT ? m_lastLeftGesture : m_lastRightGesture;
        
        if (p_recognizedGesture.found)
        {
            if (!p_recognizedGesture.gesture.Equals(l_testGesture))
            {
                if (p_recognizedGesture.side == Side.LEFT) 
                    OnNewLeftGesture?.Invoke(p_recognizedGesture.gesture, l_testGesture);
                else 
                    OnNewRightGesture?.Invoke(p_recognizedGesture.gesture, l_testGesture);

                if (p_recognizedGesture.side == Side.LEFT) m_lastLeftGesture = p_recognizedGesture.gesture;
                else m_lastRightGesture = p_recognizedGesture.gesture;
            }
        }
        else
        {
            if (l_testGesture.HasValue)
            {
                if (p_recognizedGesture.side == Side.LEFT)
                    OnNewLeftGesture?.Invoke(null, l_testGesture);
                else
                    OnNewRightGesture?.Invoke(null, l_testGesture);
            }
            
            if (p_recognizedGesture.side == Side.LEFT) m_lastLeftGesture = null;
            else m_lastRightGesture = null;
        }
    }

    private void DbgSave(HandController.Side p_side)
    {
        Gesture l_gesture = new Gesture();
        l_gesture.name = $"{(p_side == Side.LEFT ? "Left" : "Right")}_NewGesture";

        List<Vector3> l_data = new List<Vector3>();

        OVRSkeleton l_skeleton = p_side == Side.LEFT ? m_leftSkeleton : m_rightSkeleton;
        
        foreach (OVRBone l_bone in l_skeleton.Bones)
            l_data.Add(l_skeleton.transform.InverseTransformPoint(l_bone.Transform.position));

        l_gesture.fingerData = l_data;

        if (p_side == Side.LEFT) m_leftDbgGesture.Add(l_gesture);
        else m_rightDbgGesture.Add(l_gesture);
    }

    private RecognizedGesture Recognize(HandController.Side p_side)
    {
        RecognizedGesture l_result = new RecognizedGesture();
        l_result.side = p_side;
        l_result.gesture = new Gesture();
        l_result.gesture.side = p_side;

        float l_currentMin = Mathf.Infinity;

        foreach (Gesture l_testGesture in (p_side == Side.LEFT ? m_leftDbgGesture : m_rightDbgGesture))
        {
            float l_totalDist = 0.0f;
            bool l_discarded = false;

            OVRSkeleton l_skeleton = p_side == Side.LEFT ? m_leftSkeleton : m_rightSkeleton;
            
            for (int i = 0; i < l_skeleton.Bones.Count; i++)
            {
                Vector3 l_currentData =
                    l_skeleton.transform.InverseTransformPoint((l_skeleton.Bones[i].Transform.position));
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