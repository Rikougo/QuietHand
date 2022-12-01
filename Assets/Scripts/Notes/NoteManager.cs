using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class NoteManager : MonoBehaviour
{
    [Header("SpawnSettings")] 
    [SerializeField] private Vector3 m_extent;

    //! note spawned per second
    [SerializeField] private float m_baseSpawnRate;
    [SerializeField] private float m_baseSpawnSpeed;
    [SerializeField] private NoteBehaviour m_notePrefab;
    [SerializeField] private ParticleSystem m_spawnParticles;
    [SerializeField] private ParticleSystem m_completeParticles;

    [Header("ComponentsRef")] 
    [SerializeField] private NoteDeadZone m_deadZoneRef;
    [SerializeField] private Transform m_noteHolder;
    [SerializeField] private Transform m_particleHolder;
    private HandController m_handControllerRef;

    private Queue<NoteBehaviour> m_currentNotes;
    private bool m_started = false;

    
    private float m_spawnTime = 0.0f;
    private float m_currentSpawnRate;
    private float m_currentSpeed = 1.0f;

    private void Awake()
    {
        Reset();
    }

    public void Reset()
    {
        while (m_currentNotes?.Count > 0)
        {
            Destroy(m_currentNotes.Dequeue());
        }

        m_currentNotes = new Queue<NoteBehaviour>();
        
        m_currentSpawnRate = m_baseSpawnRate;
        m_currentSpeed = m_baseSpawnSpeed;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position, m_extent);
    }
    
    private void OnStartOrEnable()
    {
        if (!m_started) return;

        m_handControllerRef = FindObjectOfType<HandController>();
        m_handControllerRef.OnNewGesture += ProcessInput;
    }

    private void Start()
    {
        m_started = true;

        OnStartOrEnable();
    }

    private void ProcessInput(Gesture? p_newInput, Gesture? p_oldInput)
    {
        if (m_currentNotes.Count > 0 && p_newInput.HasValue)
        {
            m_currentNotes.Peek().GiveInput(p_newInput.Value);
        }
    }

    private void OnEnable()
    {
        OnStartOrEnable();
    }

    private void OnDisable()
    {
        m_handControllerRef.OnNewGesture -= ProcessInput;
    }

    private void FixedUpdate()
    {
        if (m_spawnTime <= 0.0f)
        {
            Vector3 l_tPosition = transform.position;
            Vector3 l_position = new Vector3(
                Random.Range(l_tPosition.x - m_extent.x, l_tPosition.x + m_extent.x), 
                Random.Range(l_tPosition.y - m_extent.y, l_tPosition.y + m_extent.y), 
                l_tPosition.z);
            NoteBehaviour l_note = Instantiate(m_notePrefab, l_position, Quaternion.identity, m_noteHolder);
            Vector3 l_direction = m_deadZoneRef.transform.position - l_tPosition;
            l_direction.y = 0.0f; l_direction.Normalize();
            l_note.transform.forward = l_direction;
            l_note.Parent = this;
            l_note.ExpectedInput = (Gesture.Type)Random.Range(0, (int)Gesture.Type.MAX);
            l_note.Speed = m_currentSpeed;

            /*ParticleSystem l_particles = 
                Instantiate(m_spawnParticles, l_note.transform.position, Quaternion.identity, m_particleHolder);
            l_particles.Play();

            ParticleSystem.MainModule l_mainModule = l_particles.main;
            Destroy(l_particles.gameObject, Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);*/
            
            m_currentNotes.Enqueue(l_note);

            m_currentSpeed = Mathf.Min(2.0f, m_currentSpeed + (m_currentSpeed * 0.05f));
            m_currentSpawnRate = Mathf.Max(0.75f, m_currentSpawnRate - (m_currentSpawnRate * 0.1f));
            m_spawnTime = m_currentSpawnRate;
        }

        m_spawnTime -= Time.fixedDeltaTime;
    }

    public void NotifySuccess(NoteBehaviour p_note)
    {
        if (p_note == m_currentNotes.Peek())
        {
            m_currentNotes.Dequeue();

            ParticleSystem l_particle = Instantiate(m_completeParticles, p_note.transform.position, quaternion.identity, m_particleHolder);
            ParticleSystem.MainModule l_mainModule = l_particle.main;
            l_mainModule.startColor = Color.green;
            l_particle.Play();
            
            Destroy(l_particle.gameObject, Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);
        }
    }
    
    public void NotifyFailure(NoteBehaviour p_note) {
        if (p_note == m_currentNotes.Peek())
        {
            m_currentNotes.Dequeue();
            
            ParticleSystem l_particle = Instantiate(m_completeParticles, p_note.transform.position, quaternion.identity, m_particleHolder);
            ParticleSystem.MainModule l_mainModule = l_particle.main;
            l_mainModule.startColor = Color.red;
            l_particle.Play();
            
            Destroy(l_particle.gameObject, Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);
        }
    }
}
