using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class MaterialDissolve : MonoBehaviour
{
    [SerializeField] private Texture m_customTexture;
    [SerializeField] private float m_duration = 0.5f;
    [SerializeField] private bool m_appearOnAwake = true;
    
    private Material m_material;
    private float m_currentDissolve;
    private bool m_playing;
    
    private static readonly float Margin = 0.05f;
    
    private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
    private static readonly int BaseTexture = Shader.PropertyToID("_BaseTexture");

    public Texture CustomTexture
    {
        get => m_material.GetTexture(BaseTexture);
        set
        {
            m_customTexture = value;
            m_material.SetTexture(BaseTexture, m_customTexture);
        }
    }
    
    private void Awake()
    {
        m_material = GetComponent<MeshRenderer>().material;
        m_material.SetFloat(Dissolve, 1.0f);
        if (m_customTexture != null) m_material.SetTexture(BaseTexture, m_customTexture);
        m_currentDissolve = 0.0f;
        m_playing = m_appearOnAwake;
    }

    public void Play()
    {
        m_currentDissolve = 0.0f;
        m_playing = true;
    }

    private void Update()
    {
        if (m_playing && m_currentDissolve < (m_duration + Margin))
        {
            float l_mappedValue = 1.0f - (m_currentDissolve * (1.0f / m_duration));
            m_material.SetFloat(Dissolve, l_mappedValue);

            m_currentDissolve += Time.deltaTime;

            if (m_currentDissolve >= m_duration + Margin) m_playing = false;
        }
    }
}
