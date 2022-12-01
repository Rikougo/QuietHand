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
    private NoteManager m_parent;
    private bool m_waitingForInput;
    private bool m_consumed = false;
    private float m_speed = 1.0f;

    public Texture victoryTexture;
    public Texture okTexture;
    public Texture fistTexture;

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

            switch (m_expectedInput)
            {
                case Gesture.Type.FIST:
                    m_dissolveMaterial.CustomTexture = fistTexture;
                    break;
                case Gesture.Type.GANG:
                    m_dissolveMaterial.CustomTexture = null;
                    break;
                case Gesture.Type.OKHAND:
                    m_dissolveMaterial.CustomTexture = okTexture;
                    break;
                case Gesture.Type.VICTORY:
                    m_dissolveMaterial.CustomTexture = victoryTexture;
                    break;
            }
        }
    }
    
    public float Speed {
        get => m_speed;
        set => m_speed = Mathf.Max(value, 1.0f);
    }

    private void FixedUpdate()
    {
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
}