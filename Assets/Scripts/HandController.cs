using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [SerializeField] private TextMeshProUGUI m_distText;
    [SerializeField] private Transform m_leftHand;
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private OVRSkeleton m_leftSkeleton;
    [SerializeField] private OVRSkeleton m_rightSkeleton;
    
    [SerializeField] private float m_recognizeThreshold = 0.1f;
    [SerializeField] private float m_submitThreshold = 0.15f;
    [SerializeField] private float m_inputDelay = 0.04f;
    
    // debug purpose
    [SerializeField] private List<Gesture> m_leftDbgGesture;
    [SerializeField] private List<Gesture> m_rightDbgGesture;

    private float m_leftInputTimer = 0.0f;
    private float m_rightInputTimer = 0.0f;
    private Vector3 m_lastLeftPos;
    private Vector3 m_lastRightPos;
    private Gesture? m_lastLeftGesture;
    private Gesture? m_lastRightGesture;

    public float m_dbgMaxLeft = float.MinValue, m_dbgMaxRight = float.MinValue;

    #region EVENTS

    public delegate void OnNewGestureHandler(Gesture? p_newGesture, Gesture? p_oldGesture);

    public event OnNewGestureHandler OnNewLeftGesture;
    public event OnNewGestureHandler OnNewRightGesture;
    #endregion

    void Start()
    {
        m_lastLeftPos = m_leftHand.position;
        m_lastRightPos = m_rightHand.position;
    }
    
    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Keypad1)) DbgSave(Side.LEFT);
        if (Input.GetKeyDown(KeyCode.Keypad3)) DbgSave(Side.RIGHT);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameDirector.CurrentState = GameDirector.GameState.PLAYING;
            FindObjectOfType<NoteManager>().StartGame();
        }
        #endif

        if (m_leftInputTimer > 0.0f) m_leftInputTimer -= Time.deltaTime;
        if (m_rightInputTimer > 0.0f) m_rightInputTimer -= Time.deltaTime;
        
        Vector3 l_leftPos = m_leftHand.position;
        Vector3 l_rightPos = m_rightHand.position;
        RecognizedGesture l_leftResult = Recognize(Side.LEFT);
        RecognizedGesture l_rightResult = Recognize(Side.RIGHT);

        float l_leftDist = ((l_leftPos - m_lastLeftPos).sqrMagnitude) / Time.deltaTime;
        if (l_leftDist >= m_submitThreshold && m_leftInputTimer <= 0.0f)
        {
            Debug.Log(l_leftDist);
            m_dbgMaxLeft = l_leftDist;
            CheckResult(l_leftResult);
            m_leftInputTimer = m_inputDelay;
        }

        float l_rightDist = ((l_rightPos - m_lastRightPos).sqrMagnitude) / Time.deltaTime;
        if (l_rightDist >= m_submitThreshold && m_rightInputTimer <= 0.0f)
        {
            Debug.Log(l_rightDist);
            m_dbgMaxRight = l_rightDist;
            CheckResult(l_rightResult);
            m_rightInputTimer = m_inputDelay;
        }

        m_distText.text = $"{l_leftDist:F4} | {l_rightDist:F4}";
        m_lastLeftPos = l_leftPos;
        m_lastRightPos = l_rightPos;
    }

    private void CheckResult(RecognizedGesture p_recognizedGesture)
    {
        Gesture? l_testGesture = p_recognizedGesture.side == Side.LEFT ? m_lastLeftGesture : m_lastRightGesture;
        
        if (p_recognizedGesture.found)
        {
                if (p_recognizedGesture.side == Side.LEFT) 
                    OnNewLeftGesture?.Invoke(p_recognizedGesture.gesture, l_testGesture);
                else 
                    OnNewRightGesture?.Invoke(p_recognizedGesture.gesture, l_testGesture);

                if (p_recognizedGesture.side == Side.LEFT) m_lastLeftGesture = p_recognizedGesture.gesture;
                else m_lastRightGesture = p_recognizedGesture.gesture;
            /*if (!p_recognizedGesture.gesture.Equals(l_testGesture))
            {
            }*/
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