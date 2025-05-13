using System.Collections;
using System.Collections.Generic;
using TopDown;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("La tecla que el jugador presionará para interactuar.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [Tooltip("La distancia máxima a la que el jugador puede interactuar con un NPC.")]
    [SerializeField] private float interactionDistance = 1.0f; // Ajusta esto
    [Tooltip("La LayerMask para filtrar qué objetos puede detectar el raycast (ej: solo NPCs).")]
    [SerializeField] private LayerMask interactableLayer;

    private PlayerMovement playerMovement; // Referencia al script de movimiento del jugador

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerInteraction: No se encontró el componente PlayerMovement en el jugador.", this);
            enabled = false; // Deshabilitar si no puede obtener la dirección
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (playerMovement == null) return;

        // Obtener la dirección en la que mira el jugador desde PlayerMovement.cs
        Vector2 interactionDirection = playerMovement.LastFacingVector;

        // Si por alguna razón LastFacingVector fuera (0,0) (aunque no debería serlo con la lógica actual),
        // podríamos poner un fallback, pero es mejor asegurar que PlayerMovement siempre tenga una dirección válida.
        if (interactionDirection == Vector2.zero)
        {
            // Esto no debería pasar si PlayerMovement está bien inicializado y actualiza LastFacingVector.
            // Podrías usar un valor por defecto si ocurre, ej: Vector2.down
            Debug.LogWarning("PlayerInteraction: LastFacingVector del jugador es (0,0). Usando Vector2.down como fallback.");
            interactionDirection = Vector2.down;
        }

        // Origen del Raycast: Podría ser el centro del jugador, o un punto ligeramente adelantado.
        // Para un juego top-down, el centro suele estar bien.
        Vector2 raycastOrigin = transform.position;
        // Opcional: un pequeño offset para que no empiece exactamente en el centro si el pivote está ahí
        // raycastOrigin += interactionDirection * 0.1f; 

        Debug.DrawRay(raycastOrigin, interactionDirection * interactionDistance, Color.green, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, interactionDirection, interactionDistance, interactableLayer);

        if (hit.collider != null)
        {
            Debug.Log("PlayerInteraction: Raycast golpeó a " + hit.collider.name);

            NPCDialogue npcDialogue = hit.collider.GetComponent<NPCDialogue>();
            if (npcDialogue != null)
            {
                if (!npcDialogue.IsDialogueActive())
                {
                    Debug.Log("PlayerInteraction: Iniciando diálogo con " + hit.collider.name);
                    npcDialogue.StartDialogue(this.transform);
                }
                else
                {
                    Debug.Log("PlayerInteraction: NPC ya está en diálogo.");
                }
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

    // El método GetPlayerFacingDirection() ya no es necesario,
    // ya que obtenemos la dirección directamente de playerMovement.LastFacingVector
}
