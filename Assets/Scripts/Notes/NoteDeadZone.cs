using System;
using UnityEngine;

public class NoteDeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider p_other)
    {
        Debug.Log($"Trigger with {p_other.tag}");
        if (p_other.CompareTag("Note"))
        {
            NoteBehaviour l_note = p_other.GetComponent<NoteBehaviour>();
            if (l_note == null)
            {
                Debug.LogWarning("Object with Note's tag has no NoteBehaviour object.");
                return;
            }

            l_note.Kill();
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider l_collider = GetComponent<BoxCollider>();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + l_collider.center, l_collider.size);
    }
}
