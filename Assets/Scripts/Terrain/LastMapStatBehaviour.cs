using System;
using Notes;
using TMPro;
using UnityEngine;

namespace Terrain
{
    public class LastMapStatBehaviour : MonoBehaviour
    {
        [Header("ComponentsRef")] 
        [SerializeField] private TextMeshProUGUI m_nameText;
        [SerializeField] private TextMeshProUGUI m_hitText;
        [SerializeField] private TextMeshProUGUI m_failText;
        [SerializeField] private TextMeshProUGUI m_accText;

        public void UpdateTexts(NoteMapStats p_stats)
        {
            m_nameText.text = p_stats.mapName;
            m_hitText.text = $"{p_stats.successHits}";
            m_failText.text = $"{p_stats.failHits}";
            m_accText.text = $"{p_stats.accuracy * 100.0f:F1}";
        }
    }
}