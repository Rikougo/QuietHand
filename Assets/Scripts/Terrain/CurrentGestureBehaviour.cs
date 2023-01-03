using System;
using UnityEngine;

namespace Terrain
{
    public class CurrentGestureBehaviour : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private HandController.Side m_side;
        [Header("ComponentsRef")]
        [SerializeField] private HandController m_handControllerRef;
        [SerializeField] private SpriteRenderer m_spriteDisplay;

        public Sprite victorySprite;
        public Sprite okSprite;
        public Sprite fistSprite;
        public Sprite rockSprite;
        
        private bool m_started = false;
        
        private void OnStartOrEnable()
        {
            if (!m_started) return;

            if (m_side == HandController.Side.LEFT) m_handControllerRef.OnNewLeftGesture += UpdateGesture;
            else m_handControllerRef.OnNewRightGesture += UpdateGesture;
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
            if (m_side == HandController.Side.LEFT) m_handControllerRef.OnNewLeftGesture -= UpdateGesture;
            else m_handControllerRef.OnNewRightGesture -= UpdateGesture;
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
                    case Gesture.Type.OKHAND:
                        m_spriteDisplay.sprite = okSprite;
                        break;
                    case Gesture.Type.VICTORY:
                        m_spriteDisplay.sprite = victorySprite;
                        break;
                    case Gesture.Type.METAL:
                        m_spriteDisplay.sprite = rockSprite;
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