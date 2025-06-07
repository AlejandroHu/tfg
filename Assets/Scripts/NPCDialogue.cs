using UnityEngine;
using Ink.Runtime; // Asegúrate de tener Ink instalado en tu proyecto

// Asegúrate de que los otros scripts necesarios sean accesibles
 using TopDown; 

[RequireComponent(typeof(NPCMovement))] // Es bueno asegurar que también tenga el script de movimiento
public class NPCDialogue : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    [Tooltip("El archivo de diálogo (.ink.json) por defecto para este NPC si no hay una misión asociada.")]
    [SerializeField] private TextAsset inkJSON;

    [Header("Referencias")]
    [Tooltip("Referencia al script de movimiento del NPC en el mismo GameObject.")]
    [SerializeField] private NPCMovement npcMovement;

    // Referencia al transform del jugador, se buscará automáticamente
    private Transform playerTransform;

    private void Awake()
    {
        // Obtener el script de movimiento si no está asignado en el Inspector
        if (npcMovement == null)
        {
            npcMovement = GetComponent<NPCMovement>();
        }

        // Encontrar el transform del jugador para que el NPC pueda mirarlo
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"NPCDialogue en '{gameObject.name}': No se pudo encontrar el GameObject del jugador con el tag 'Player'. El NPC no podrá encararlo.");
        }
    }

    /// <summary>
    /// Inicia el diálogo por defecto asignado en el Inspector.
    /// Este método puede ser llamado por un sistema de interacción simple si el NPC no tiene misiones.
    /// </summary>
    public void StartDefaultDialogue()
    {
        // Llama al método principal de diálogo usando el TextAsset por defecto
        StartDialogue(this.inkJSON);
    }

    /// <summary>
    /// NUEVO MÉTODO: Inicia un diálogo usando un TextAsset específico.
    /// Este es el método que llamará QuestGiver.cs.
    /// </summary>
    /// <param name="dialogueToPlay">El archivo .ink.json a reproducir.</param>
    public void StartDialogue(TextAsset dialogueToPlay)
    {
        // Comprobar si hay un diálogo válido para iniciar
        if (dialogueToPlay == null)
        {
            Debug.LogWarning($"NPCDialogue en '{gameObject.name}' intentó iniciar un diálogo, pero el TextAsset es nulo.");
            return;
        }

        // No iniciar un nuevo diálogo si ya hay uno en curso
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying)
        {
            return;
        }

        // Pausar el movimiento del NPC y hacer que mire al jugador
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(true);
            if (playerTransform != null)
            {
                npcMovement.SetIdleAndFaceTarget(playerTransform);
            }
        }

        // Iniciar el diálogo a través del DialogueManager
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EnterDialogueMode(dialogueToPlay, this); // Pasamos 'this' para que el DialogueManager sepa quién está hablando
        }
        else
        {
            Debug.LogError("No se encontró una instancia de DialogueManager en la escena.");
        }
    }

    /// <summary>
    /// Este método será llamado por el DialogueManager cuando el diálogo termine.
    /// </summary>
    public void OnDialogueEnd()
    {
        // Reanudar el movimiento del NPC
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(false);
        }
    }
}
