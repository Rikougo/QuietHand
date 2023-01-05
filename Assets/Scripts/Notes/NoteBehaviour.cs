using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
public class NoteBehaviour : MonoBehaviour
{
    [SerializeField] private MaterialDissolve m_dissolveMaterial;
    [SerializeField] private Gesture.Type m_expectedInput;
    private HandController.Side m_expectedSide;
    private NoteManager m_parent;
    private bool m_waitingForInput;
    private bool m_consumed = false;
    private float m_speed = 1.0f;
    private float m_timeAlive = 0.0f;

    [Header("Score")] 
    public float timeDeadZone = 0.25f;
    public float maxTime = 2.0f;
    public float maxScoreGain = 200.0f;
    public float minScoreGain = 50.0f;
    
    [Header("Left textures")]
    public Texture victoryTexture;
    public Texture okTexture;
    public Texture fistTexture;
    public Texture rockTexture;

    [Header("Right textures")]
    public Texture rVictoryTexture;
    public Texture rOkTexture;
    public Texture rFistTexture;
    public Texture rRockTexture;

    public NoteManager Parent
    {
        get => m_parent;
        set => m_parent = value;
    }

    public Gesture.Type ExpectedInput
    {
        get => m_expectedInput;
        set
        {
            m_expectedInput = value;
            UpdateTexture();
        }
    }

    public HandController.Side ExpectedSide
    {
        get => m_expectedSide;
        set
        {
            m_expectedSide = value;
            UpdateTexture();
        }
    }

    public float Speed
    {
        get => m_speed;
        set => m_speed = Mathf.Max(value, 1.0f);
    }

    public float TimeAlive => m_timeAlive;

    private void FixedUpdate()
    {
        m_timeAlive += Time.fixedDeltaTime;
        transform.position += transform.forward * (m_speed * Time.fixedDeltaTime);
    }

    public void GiveInput(Gesture p_gesture)
    {
        if (m_consumed) return;

        m_consumed = true;

        if (p_gesture.type == m_expectedInput) m_parent.NotifySuccess(this);
        else m_parent.NotifyFailure(this);

        Destroy(this.gameObject);
    }

    public void Kill()
    {
        if (m_consumed) return;

        m_consumed = true;

        m_parent.NotifyFailure(this);

        Destroy(this.gameObject);
    }

    private void UpdateTexture()
    {
        switch (m_expectedInput)
        {
            case Gesture.Type.FIST:
                m_dissolveMaterial.CustomTexture = 
                    m_expectedSide == HandController.Side.LEFT ? fistTexture : rFistTexture;
                break;
            case Gesture.Type.OKHAND:
                m_dissolveMaterial.CustomTexture = 
                    m_expectedSide == HandController.Side.LEFT ? okTexture : rOkTexture;
                break;
            case Gesture.Type.VICTORY:
                m_dissolveMaterial.CustomTexture = 
                    m_expectedSide == HandController.Side.LEFT ? victoryTexture : rVictoryTexture;
                break;
            case Gesture.Type.METAL:
                m_dissolveMaterial.CustomTexture = 
                    m_expectedSide == HandController.Side.LEFT ? rockTexture : rRockTexture;
                break;
        }
    }

    public float ComputeScore()
    {
        return Mathf.Lerp(maxScoreGain, minScoreGain, Mathf.Max(0.0f, m_timeAlive - timeDeadZone) / maxTime);
    }
}