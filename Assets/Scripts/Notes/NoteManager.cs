using System;
using System.Collections.Generic;
using System.IO;
using Notes;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NoteManager : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private Vector3 m_extent = Vector3.zero;
    [SerializeField] private float m_baseStamina = 100.0f;
    [SerializeField] private float m_staminaLoss = 10.0f;
    [SerializeField] private float m_staminaGain = 2.0f;
    [SerializeField] private float m_speedGain = 2.0f;

    [Header("Components")]
    [SerializeField] private NoteBehaviour m_notePrefab;
    [SerializeField] private ParticleSystem m_completeParticles;

    [Header("ComponentsRef")] 
    [SerializeField] private NoteDeadZone m_deadZoneRef;
    [SerializeField] private Transform m_noteHolder;
    [SerializeField] private Transform m_particleHolder;
    [SerializeField] private Image m_progressImage;
    [SerializeField] private TextMeshProUGUI m_scoreText;

    private bool m_started = false;

    private List<Tuple<Gesture.Type, Gesture.Type>> m_noteList;
    private Queue<NoteBehaviour> m_currentLeftNotes;
    private Queue<NoteBehaviour> m_currentRightNotes;
    private bool m_playing = false;

    private string m_currentMapName = String.Empty;
    private int m_currentSongBpm = 0;
    private int m_currentSongTickAmount = 0;
    private int m_currentTick = 0;
    private float m_tickTimer = 0.0f;
    private float m_totalMapTime = 0.0f;
    private float m_mapTimer = 0.0f;
    private float m_currentStamina;
    private NoteMapStats m_currentMapStats;
    private float m_currentScore = 0.0f;

    public NoteMapStats m_lastMapStats { get; private set; }

    private void Awake()
    {
        m_currentLeftNotes = new Queue<NoteBehaviour>();
        m_currentRightNotes = new Queue<NoteBehaviour>();
        
        Reset();
    }

    private void Reset()
    {
        m_progressImage.fillAmount = 0;
        m_scoreText.text = String.Empty;

        while (m_currentLeftNotes.Count > 0)
        {
            Destroy(m_currentLeftNotes.Dequeue().gameObject);
        }
        
        while (m_currentRightNotes.Count > 0)
        {
            Destroy(m_currentRightNotes.Dequeue().gameObject);
        }

        m_playing = false;
        m_currentLeftNotes.Clear();
        m_currentRightNotes.Clear();

        m_currentStamina = m_baseStamina;

        LoadMap();
    }

    private void LoadMap()
    {
        string l_path = "./Assets/Resources/map_test0.txt";

        StreamReader l_reader = new StreamReader(l_path);

        string l_header = l_reader.ReadLine();
        Debug.Log(l_header);
        string[] l_parsedHeader = l_header.Split(",");
        m_currentSongBpm = Int32.Parse(l_parsedHeader[0]);
        m_currentSongTickAmount = Int32.Parse(l_parsedHeader[1]);
        m_totalMapTime = m_currentSongTickAmount * (60.0f / m_currentSongBpm);

        m_noteList = new List<Tuple<Gesture.Type, Gesture.Type>>();

        for (int i = 0; i < m_currentSongTickAmount; i++)
        {
            char[] l_line = l_reader.ReadLine().ToCharArray();
            m_noteList.Add(new Tuple<Gesture.Type, Gesture.Type>(
                l_line[0] == '-' ? Gesture.Type.MAX : (Gesture.Type)Char.GetNumericValue(l_line[0]),
                l_line[1] == '-' ? Gesture.Type.MAX : (Gesture.Type)Char.GetNumericValue(l_line[1])
            ));
        }

        Debug.LogFormat("{0} {1} {2}", m_noteList.Count, m_currentSongTickAmount, m_currentSongBpm);
        m_currentTick = 0;
        m_mapTimer = 0.0f;
        
        m_currentMapStats.failHits = 0;
        m_currentMapStats.successHits = 0;
        m_currentMapName = Path.GetFileNameWithoutExtension(l_path);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position, m_extent);
    }

    public void ProcessInput(Gesture? p_newInput, Gesture? p_oldInput)
    {
        if (!m_playing) return;

        if (!p_newInput.HasValue) return;
        
        var l_queue = p_newInput.Value.side == HandController.Side.LEFT ? m_currentLeftNotes : m_currentRightNotes;
        
        if (l_queue.Count > 0)
        {
            l_queue.Peek().GiveInput(p_newInput.Value);
        }
    }
    
    public void StartGame()
    {
        Reset();
        m_scoreText.text = $"{m_currentScore:F0}";
        m_playing = true;
    }
    
    private void FixedUpdate()
    {
        if (!m_playing || !GameDirector.InPlayMode) return;

        m_tickTimer += Time.fixedDeltaTime * m_speedGain;
        m_mapTimer += Time.fixedDeltaTime * m_speedGain;

        m_progressImage.fillAmount = m_mapTimer / m_totalMapTime;

        if (m_tickTimer > (60.0f / m_currentSongBpm))
        {
            Tick();
            m_tickTimer = 0.0f;
            m_currentTick++;

            if (m_currentTick >= m_currentSongTickAmount)
            {
                EndGame();
                OnMapEnd?.Invoke(true);
            }
        }
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
            l_leftNote.ExpectedSide = HandController.Side.LEFT;
            l_leftNote.Speed = m_speedGain;
            
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
            l_rightNote.ExpectedSide = HandController.Side.RIGHT;
            l_rightNote.Speed = m_speedGain;
            
            m_currentRightNotes.Enqueue(l_rightNote);
        }
    }

    public void EndGame()
    {
        m_currentMapStats.Record(m_currentMapName);
        m_lastMapStats = m_currentMapStats;
        Reset();
    }
    
    public void NotifySuccess(NoteBehaviour p_note)
    {
        if (!m_playing) return;
        
        var l_queue = p_note.ExpectedSide == HandController.Side.LEFT ? m_currentLeftNotes : m_currentRightNotes;
        
        if (p_note == l_queue.Peek())
        {
            l_queue.Dequeue();

            ParticleSystem l_particle = Instantiate(m_completeParticles, p_note.transform.position, quaternion.identity,
                m_particleHolder);
            ParticleSystem.MainModule l_mainModule = l_particle.main;
            l_mainModule.startColor = Color.green;
            l_particle.Play();

            Destroy(l_particle.gameObject,
                Mathf.Max(l_mainModule.duration, l_mainModule.startLifetime.constantMax) + 0.1f);
            
            UpdateScoreAndStamina(true, p_note.ComputeScore());
        }
        else Debug.LogWarning("NoteManager::NotifySuccess: sent success notification with a note that is not the top queue");
    }

    public void NotifyFailure(NoteBehaviour p_note)
    {
        if (!m_playing) return;

        var l_queue = p_note.ExpectedSide == HandController.Side.LEFT ? m_currentLeftNotes : m_currentRightNotes;
        
        if (p_note == l_queue.Peek())
        {
            l_queue.Dequeue();

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

    private void UpdateScoreAndStamina(bool p_success, float p_scoreGain = 0.0f)
    {
        if (p_success)
        {
            m_currentStamina = Mathf.Min(m_currentStamina + m_staminaGain, m_baseStamina);
            m_currentMapStats.successHits++;
            m_currentScore += p_scoreGain;
        }
        else
        {
            m_currentStamina -= m_staminaLoss;
            m_currentMapStats.failHits++;
        }

        m_scoreText.text = $"{m_currentScore:F0}";

        if (m_currentStamina <= 0.0f)
        {
            EndGame();
            OnMapEnd?.Invoke(false);
        }
    }
    
    #region EVENTS

    public delegate void OnMapEndHandler(bool p_success);

    public event OnMapEndHandler OnMapEnd;

    #endregion
}