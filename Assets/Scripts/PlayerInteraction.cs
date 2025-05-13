using System.Collections;
using System.Collections.Generic;
using TopDown;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("La tecla que el jugador presionará para iniciar la interacción.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [Tooltip("NUEVA: La tecla que el jugador presionará para avanzar en el diálogo.")]
    [SerializeField] private KeyCode advanceDialogueKey = KeyCode.Space;
    [Tooltip("La distancia máxima a la que el jugador puede interactuar con un NPC.")]
    [SerializeField] private float interactionDistance = 1.0f;
    [Tooltip("La LayerMask para filtrar qué objetos puede detectar el raycast (ej: solo NPCs).")]
    [SerializeField] private LayerMask interactableLayer;
    [Tooltip("Pequeño desplazamiento hacia adelante para el origen del raycast, para evitar golpear al propio jugador.")]
    [SerializeField] private float raycastOriginOffset = 0.2f; // Añadido para mejorar el raycast

    private PlayerMovement playerMovement;
    private NPCDialogue currentActiveDialogue; // NUEVA: Para guardar la referencia al diálogo activo

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
        if (Input.GetKeyDown(interactionKey))
        {
            // Si no hay un diálogo activo, intentar iniciar uno.
            // Si ya hay uno, esta tecla (E) NO avanzará el diálogo (para eso está advanceDialogueKey).
            if (currentActiveDialogue == null || !currentActiveDialogue.IsDialogueActive())
            {
                TryInteract();
            }
        }
        else if (Input.GetKeyDown(advanceDialogueKey)) // Usar la nueva tecla para avanzar
        {
            if (currentActiveDialogue != null && currentActiveDialogue.IsDialogueActive())
            {
                currentActiveDialogue.AdvanceDialogue();
                // Comprobar si el diálogo terminó después de avanzar para limpiar la referencia
                if (!currentActiveDialogue.IsDialogueActive())
                {
                    currentActiveDialogue = null;
                }
            }
        }
    }

    private void TryInteract()
    {
        if (playerMovement == null) return;

        Vector2 interactionDirection = playerMovement.LastFacingVector;

        if (interactionDirection == Vector2.zero)
        {
            Debug.LogWarning("PlayerInteraction: LastFacingVector del jugador es (0,0). Usando Vector2.down como fallback.");
            interactionDirection = Vector2.down;
        }

        // Ajustar el origen del Raycast para que comience un poco delante del jugador
        Vector2 raycastOrigin = (Vector2)transform.position + (interactionDirection * raycastOriginOffset);

        Debug.DrawRay(raycastOrigin, interactionDirection * interactionDistance, Color.green, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, interactionDirection, interactionDistance, interactableLayer);

        if (hit.collider != null)
        {
            // Evitar interactuar consigo mismo si el jugador estuviera accidentalmente en la interactableLayer
            if (hit.collider.gameObject == this.gameObject)
            {
                Debug.Log("PlayerInteraction: Raycast golpeó al propio jugador.");
                return;
            }

            Debug.Log("PlayerInteraction: Raycast golpeó a " + hit.collider.name);

            NPCDialogue npcDialogue = hit.collider.GetComponent<NPCDialogue>();
            if (npcDialogue != null)
            {
                if (!npcDialogue.IsDialogueActive())
                {
                    Debug.Log("PlayerInteraction: Iniciando diálogo con " + hit.collider.name);
                    npcDialogue.StartDialogue(this.transform);
                    currentActiveDialogue = npcDialogue; // Guardar la referencia al diálogo que se acaba de iniciar
                }
                // No hacemos nada si ya está en diálogo, ya que E solo inicia.
            }
            else
            {
                Debug.Log("PlayerInteraction: Objeto golpeado no tiene NPCDialogue: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("PlayerInteraction: Raycast no golpeó nada interactuable.");
        }
    }
}
