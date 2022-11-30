using System;
using UnityEngine;

namespace Terrain
{
    public class CurrentGestureBehaviour : MonoBehaviour
    {
        [Header("ComponentsRef")]
        [SerializeField] private HandController m_handControllerRef;
        [SerializeField] private SpriteRenderer m_spriteDisplay;

        public Sprite victorySprite;
        public Sprite okSprite;
        public Sprite fistSprite;
        public Sprite gangSprite;

        private bool m_started = false;
        
        private void OnStartOrEnable()
        {
            if (!m_started) return;

            m_handControllerRef.OnNewGesture += UpdateGesture;
        }
        
        private void Start()
        {
            m_started = true;
            
            OnStartOrEnable();
        }

        private void OnEnable()
        {
            OnStartOrEnable();
        }

        private void OnDisable()
        {
            m_handControllerRef.OnNewGesture -= UpdateGesture;
        }

        private void UpdateGesture(Gesture? p_newGesture, Gesture? p_oldGesture)
        {
            if (p_newGesture.HasValue)
            {
                switch (p_newGesture.Value.type)
                {
                    case Gesture.Type.FIST:
                        m_spriteDisplay.sprite = fistSprite;
                        break;
                    case Gesture.Type.GANG:
                        m_spriteDisplay.sprite = gangSprite;
                        break;
                    case Gesture.Type.OKHAND:
                        m_spriteDisplay.sprite = okSprite;
                        break;
                    case Gesture.Type.VICTORY:
                        m_spriteDisplay.sprite = victorySprite;
                        break;
                }
            }
            else
            {
                m_spriteDisplay.sprite = null;
            }
        }
    }
}