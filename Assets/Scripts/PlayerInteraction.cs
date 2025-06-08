using System.Collections;
using System.Collections.Generic;
using TopDown; // Asegúrate de que PlayerMovement y NPCDialogue estén accesibles
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("La tecla que el jugador presionará para iniciar la interacción con NPCs.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [Tooltip("La distancia máxima (en unidades de Unity) a la que el jugador puede interactuar con un NPC.")]
    [SerializeField] private float interactionDistance = 1.0f;
    [Tooltip("La LayerMask para filtrar qué objetos puede detectar el raycast (ej: solo la capa 'NPC' o 'Interactable').")]
    [SerializeField] private LayerMask interactableLayer;
    [Tooltip("Pequeño desplazamiento hacia adelante para el origen del raycast, para evitar que el rayo golpee al propio jugador.")]
    [SerializeField] private float raycastOriginOffset = 0.2f;

    // Referencia al script PlayerMovement del jugador
    private PlayerMovement playerMovement;

    // Ya no necesitamos 'currentActiveDialogue' ni la lógica de 'advanceDialogueKey' aquí.
    // El DialogueManager se encargará de todo eso.

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: No se encontró el componente PlayerMovement en el jugador.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // La responsabilidad de este script ahora es mucho más simple.
        // Solo debe detectar la pulsación de la tecla de interacción,
        // pero únicamente si no hay un diálogo ya en curso.

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying)
        {
            // Si el diálogo está activo, no hacemos nada. El DialogueManager maneja el input.
            return;
        }

        // Si no hay diálogo, escuchar la tecla para intentar iniciar una nueva interacción.
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// Intenta iniciar una interacción con un NPC en la dirección en la que mira el jugador.
    /// </summary>
    private void TryInteract()
    {
        if (playerMovement == null) return;

        Vector2 interactionDirection = playerMovement.LastFacingVector;

        if (interactionDirection == Vector2.zero)
        {
            // Fallback por si el jugador está quieto y no tiene una última dirección guardada
            interactionDirection = Vector2.down;
        }

        Vector2 raycastOrigin = (Vector2)transform.position + (interactionDirection * raycastOriginOffset);

        Debug.DrawRay(raycastOrigin, interactionDirection * interactionDistance, Color.blue, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, interactionDirection, interactionDistance, interactableLayer);

        if (hit.collider != null)
        {
            if (hit.collider.gameObject == this.gameObject) return;
            // Intentar obtener el componente NPCDialogue del objeto golpeado.
            if (hit.collider.TryGetComponent<SceneTransitionDoor>(out SceneTransitionDoor door))
            {
                Debug.Log($"Interactuando con puerta: {hit.collider.name}");
                door.Transition();
            }
            else if (hit.collider.TryGetComponent<NPCDialogue>(out NPCDialogue npcDialogue))
            {
                // --- NUEVA LÓGICA INTELIGENTE ---
                // Primero, comprobar si el NPC tiene un QuestGiver.
                if (npcDialogue.TryGetComponent<QuestGiver>(out QuestGiver questGiver))
                {
                    // Si lo tiene, el QuestGiver decide qué diálogo mostrar.
                    Debug.Log($"Interactuando con QuestGiver: {hit.collider.name}");
                    questGiver.StartQuestDialogue();
                }
                else
                {
                    // Si no, iniciar el diálogo por defecto del NPC.
                    Debug.Log($"Interactuando con NPC (diálogo por defecto): {hit.collider.name}");
                    npcDialogue.StartDefaultDialogue();
                }
            }
        }
    }


}