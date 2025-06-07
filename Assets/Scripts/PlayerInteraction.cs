using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    void Update()
    {
        // Detectar si se pulsa la tecla de interacción (ej: E o Espacio)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Puedes usar un OverlapCircle para detectar NPCs cercanos,
        // o un Raycast, o un trigger de colisión.
        // Este es un ejemplo con OverlapCircle.

        float interactionRadius = 1f; // El radio de detección
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);

        foreach (Collider2D collider in colliders)
        {
            if (collider.TryGetComponent<NPCDialogue>(out NPCDialogue npcDialogue))
            {
                if (npcDialogue.TryGetComponent<QuestGiver>(out QuestGiver questGiver))
                {
                    // El QuestGiver decide qué diálogo mostrar
                    questGiver.StartQuestDialogue();
                }
                else
                {
                    // Iniciar el diálogo por defecto del NPC
                    npcDialogue.StartDefaultDialogue();
                }
                break; // Interactuar solo con el primer NPC encontrado
            }
        }
    }
}
