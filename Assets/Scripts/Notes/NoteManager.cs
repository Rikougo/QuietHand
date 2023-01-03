using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class NoteManager : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private Vector3 m_extent = Vector3.zero;

    [Header("Components")]
    [SerializeField] private NoteBehaviour m_notePrefab;
    // [SerializeField] private ParticleSystem m_spawnParticles;
    [SerializeField] private ParticleSystem m_completeParticles;

    [Header("ComponentsRef")] 
    [SerializeField] private NoteDeadZone m_deadZoneRef;
    [SerializeField] private Transform m_noteHolder;
    [SerializeField] private Transform m_particleHolder;
    private HandController m_handControllerRef;

    private bool m_started = false;

    private int m_currentSongBPM = 0;
    private int m_currentSongTickAmount = 0;
    private int m_currentTick = 0;
    private List<Tuple<Gesture.Type, Gesture.Type>> m_noteList;
    private Queue<NoteBehaviour> m_currentLeftNotes;
    private Queue<NoteBehaviour> m_currentRightNotes;
    private bool m_playing;

    private float m_tickTimer = 0.0f;
    /*private float m_spawnTime;
    private float m_currentSpawnRate;
    private float m_currentSpeed;
    private int m_currentCombo;*/

    private void Awake()
    {
        m_currentLeftNotes = new Queue<NoteBehaviour>();
        m_currentRightNotes = new Queue<NoteBehaviour>();
        
        Reset();
    }

    private void Reset()
    {
        while (m_currentLeftNotes.Count > 0)
        {
            Destroy(m_currentLeftNotes.Dequeue());
        }
        
        while (m_currentRightNotes.Count > 0)
        {
            Destroy(m_currentRightNotes.Dequeue());
        }

        m_playing = false;
        m_currentLeftNotes.Clear();
        m_currentRightNotes.Clear();

        LoadMap();
        /*m_currentSpawnRate = m_baseSpawnRate;
        m_currentSpeed = m_baseSpawnSpeed;
        m_spawnTime = m_currentSpawnRate;
        m_currentCombo = 0;*/
    }

    private void LoadMap()
    {
        string l_path = "./Assets/Resources/map_test0.txt";

        StreamReader l_reader = new StreamReader(l_path);

        string l_header = l_reader.ReadLine();
        Debug.Log(l_header);
        string[] l_parsedHeader = l_header.Split(",");
        m_currentSongBPM = Int32.Parse(l_parsedHeader[0]);
        m_currentSongTickAmount = Int32.Parse(l_parsedHeader[1]);

        m_noteList = new List<Tuple<Gesture.Type, Gesture.Type>>();

        for (int i = 0; i < m_currentSongTickAmount; i++)
        {
            char[] l_line = l_reader.ReadLine().ToCharArray();
            m_noteList.Add(new Tuple<Gesture.Type, Gesture.Type>(
                l_line[0] == '-' ? Gesture.Type.MAX : (Gesture.Type)Char.GetNumericValue(l_line[0]),
                l_line[1] == '-' ? Gesture.Type.MAX : (Gesture.Type)Char.GetNumericValue(l_line[1])
            ));
        }

        Debug.LogFormat("{0} {1} {2}", m_noteList.Count, m_currentSongTickAmount, m_currentSongBPM);
        m_currentTick = 0;
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
        m_handControllerRef.OnNewLeftGesture += ProcessInput;
        m_handControllerRef.OnNewRightGesture += ProcessInput;
    }

    private void Start()
    {
        m_started = true;

        OnStartOrEnable();
    }

    private void ProcessInput(Gesture? p_newInput, Gesture? p_oldInput)
    {
        if (!m_playing) return;
        
        if (m_currentLeftNotes.Count > 0 && p_newInput.HasValue)
        {
            m_currentLeftNotes.Peek().GiveInput(p_newInput.Value);
        }
    }

    private void OnEnable()
    {
        OnStartOrEnable();
    }

    private void OnDisable()
    {
        m_handControllerRef.OnNewLeftGesture -= ProcessInput;
        m_handControllerRef.OnNewRightGesture -= ProcessInput;
    }

    public void StartGame()
    {
        Reset();

        m_playing = true;
    }
    
    private void FixedUpdate()
    {
        if (!m_playing) return;

        m_tickTimer += Time.fixedDeltaTime;

        if (m_tickTimer > (60.0f / m_currentSongBPM))
        {
            Tick();
            m_tickTimer = 0.0f;
            m_currentTick++;

            if (m_currentTick >= m_currentSongTickAmount)
                EndGame();
        }
        /*if (m_spawnTime <= 0.0f) SpawnNote();
        m_spawnTime -= Time.fixedDeltaTime;*/
    }

    private void Tick()
    {
        Vector3 l_tPosition = transform.position;
        Vector3 l_leftPosition = (transform.position + Vector3.left * m_extent.x);
        Vector3 l_rightPosition = (transform.position + Vector3.right * m_extent.x);

        if (m_noteList[m_currentTick].Item1 != Gesture.Type.MAX)
        {
            NoteBehaviour l_leftNote = Instantiate(m_notePrefab, l_leftPosition, Quaternion.identity, m_noteHolder);
            Vector3 l_direction = m_deadZoneRef.transform.position - l_tPosition;
            l_direction.y = 0.0f;
            l_direction.Normalize();
            l_leftNote.transform.forward = l_direction;
            l_leftNote.Parent = this;
            l_leftNote.ExpectedInput = m_noteList[m_currentTick].Item1;
            
            m_currentLeftNotes.Enqueue(l_leftNote);
        }
        
        if (m_noteList[m_currentTick].Item2 != Gesture.Type.MAX)
        {
            NoteBehaviour l_rightNote = Instantiate(m_notePrefab, l_rightPosition, Quaternion.identity, m_noteHolder);
            Vector3 l_direction = m_deadZoneRef.transform.position - l_tPosition;
            l_direction.y = 0.0f;
            l_direction.Normalize();
            l_rightNote.transform.forward = l_direction;
            l_rightNote.Parent = this;
            l_rightNote.ExpectedInput = m_noteList[m_currentTick].Item2;
            
            m_currentLeftNotes.Enqueue(l_rightNote);
        }
    }

    public void EndGame()
    {
        Reset();
    }

    /*private void SpawnNote()
    {
        Vector3 l_tPosition = transform.position;
        Vector3 l_position = new Vector3(
            Random.Range(l_tPosition.x - m_extent.x, l_tPosition.x + m_extent.x),
            Random.Range(l_tPosition.y - m_extent.y, l_tPosition.y + m_extent.y),
            l_tPosition.z);
        NoteBehaviour l_note = Instantiate(m_notePrefab, l_position, Quaternion.identity, m_noteHolder);
        Vector3 l_direction = m_deadZoneRef.transform.position - l_tPosition;
        l_direction.y = 0.0f;
        l_direction.Normalize();
        l_note.transform.forward = l_direction;
        l_note.Parent = this;
        l_note.ExpectedInput = (Gesture.Type)Random.Range(0, (int)Gesture.Type.MAX);
        // l_note.Speed = m_currentSpeed;

        m_currentLeftNotes.Enqueue(l_note);

        ScaleDifficulty();
        
        // m_spawnTime = m_currentSpawnRate;
    }*/

    /*private void ScaleDifficulty()
    {
        /*m_currentSpeed = Mathf.Min(2.0f, m_currentSpeed + (m_currentSpeed * 0.05f));
        m_currentSpawnRate = Mathf.Max(0.75f, m_currentSpawnRate - (m_currentSpawnRate * 0.1f));#1#
    }*/

    public void NotifySuccess(NoteBehaviour p_note)
    {
        if (p_note == m_currentLeftNotes.Peek())
        {
            m_currentLeftNotes.Dequeue();

            ParticleSystem l_particle = Instantiate(m_completeParticles, p_note.transform.position, quaternion.identity,
                m_particleHolder);
            ParticleSystem.MainModule l_mainModule = l_particle.main;
            l_mainModule.startColor = Color.green;
            l_particle.Play();

            Destroy(l_particle.gameObject,
                Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);
            
            UpdateScoreAndStamina(true);
        }
        else Debug.LogWarning("NoteManager::NotifySuccess: sent success notification with a note that is not the top queue");
    }

    public void NotifyFailure(NoteBehaviour p_note)
    {
        if (p_note == m_currentLeftNotes.Peek())
        {
            m_currentLeftNotes.Dequeue();

            ParticleSystem l_particle = Instantiate(m_completeParticles, p_note.transform.position, quaternion.identity,
                m_particleHolder);
            ParticleSystem.MainModule l_mainModule = l_particle.main;
            l_mainModule.startColor = Color.red;
            l_particle.Play();

            Destroy(l_particle.gameObject,
                Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);

            UpdateScoreAndStamina(false);
        }
        else Debug.LogWarning("NoteManager::NotifyFailure: sent failure notification with a note that is not the top queue");
    }

    private void UpdateScoreAndStamina(bool p_success)
    {
        
    }
}