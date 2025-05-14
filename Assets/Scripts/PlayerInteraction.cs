using System.Collections;
using System.Collections.Generic;
using TopDown;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")] // Encabezado para organizar en el Inspector
    [Tooltip("La tecla que el jugador presionará para iniciar la interacción con NPCs.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E; // Tecla para iniciar el diálogo.
    [Tooltip("La tecla que el jugador presionará para avanzar en el diálogo actual.")]
    [SerializeField] private KeyCode advanceDialogueKey = KeyCode.Space; // Tecla para pasar las líneas de diálogo.
    [Tooltip("La distancia máxima (en unidades de Unity) a la que el jugador puede interactuar con un NPC.")]
    [SerializeField] private float interactionDistance = 1.0f; // Rango de interacción.
    [Tooltip("La LayerMask para filtrar qué objetos puede detectar el raycast (ej: solo la capa 'NPC' o 'Interactable').")]
    [SerializeField] private LayerMask interactableLayer; // Define qué capas contienen objetos con los que se puede interactuar.
    [Tooltip("Pequeño desplazamiento hacia adelante para el origen del raycast, para evitar que el rayo golpee al propio jugador.")]
    [SerializeField] private float raycastOriginOffset = 0.2f; // Offset para el inicio del rayo.

    private PlayerMovement playerMovement; // Referencia al script PlayerMovement del jugador (para obtener la dirección y controlar el input).
    private NPCDialogue currentActiveDialogue; // Referencia al script NPCDialogue del NPC con el que se está hablando actualmente. Null si no hay diálogo activo.

  
    void Awake()
    {
        // Obtiene el componente PlayerMovement adjunto a este mismo GameObject (el jugador).
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null) // Si no se encuentra el script PlayerMovement.
        {
            Debug.LogError("PlayerInteraction: No se encontró el componente PlayerMovement en el jugador. El control de input y la dirección de interacción no funcionarán.", this);
            enabled = false; // Deshabilitar este script para evitar errores continuos.
        }
    }

 
    void Update()
    {
        // Comprueba si hay un diálogo activo.
        if (currentActiveDialogue != null && currentActiveDialogue.IsDialogueActive())
        {
            // Si hay un diálogo activo, solo escuchar la tecla para avanzar el diálogo.
            if (Input.GetKeyDown(advanceDialogueKey))
            {
                currentActiveDialogue.AdvanceDialogue(); // Llama al método para avanzar en el NPCDialogue.

                // Comprobar si el diálogo terminó DESPUÉS de intentar avanzar.
                if (!currentActiveDialogue.IsDialogueActive())
                {
                    Debug.Log("PlayerInteraction: Diálogo terminado (detectado por PlayerInteraction). Habilitando input del jugador.");
                    playerMovement.SetCanProcessInput(true); // RE-HABILITAR el input del jugador a través de PlayerMovement.
                    currentActiveDialogue = null; // Limpiar la referencia, ya no hay diálogo activo.
                }
            }
        }
        else // Si NO hay un diálogo activo.
        {
            // Escuchar la tecla para intentar iniciar una nueva interacción/diálogo.
            if (Input.GetKeyDown(interactionKey))
            {
                TryInteract();
            }
        }
    }

    // Método para intentar iniciar una interacción con un NPC.
    private void TryInteract()
    {
        if (playerMovement == null) return; // No hacer nada si no hay referencia a PlayerMovement.

        // Obtener la última dirección en la que el jugador estaba "mirando" desde el script PlayerMovement.
        Vector2 interactionDirection = playerMovement.LastFacingVector;

        // Fallback por si LastFacingVector fuera (0,0) (aunque no debería pasar con la lógica actual de PlayerMovement).
        if (interactionDirection == Vector2.zero)
        {
            Debug.LogWarning("PlayerInteraction: LastFacingVector del jugador es (0,0). Usando Vector2.down como fallback para el raycast de interacción.");
            interactionDirection = Vector2.down;
        }

        // Calcular el origen del Raycast: posición del jugador + un pequeño offset en la dirección de interacción.
        // Esto ayuda a que el rayo comience ligeramente delante del jugador y no dentro de su propio collider.
        Vector2 raycastOrigin = (Vector2)transform.position + (interactionDirection * raycastOriginOffset);

        // Dibuja el rayo en la vista de Escena para depuración (visible solo en el Editor).
        // Color azul para distinguirlo de otros rayos (ej: el del NPCMovement).
        Debug.DrawRay(raycastOrigin, interactionDirection * interactionDistance, Color.blue, 0.5f);

        // Lanza un Raycast 2D.
        // - raycastOrigin: Punto de inicio del rayo.
        // - interactionDirection: Dirección del rayo.
        // - interactionDistance: Longitud máxima del rayo.
        // - interactableLayer: Máscara de capas para filtrar qué objetos puede golpear el rayo.
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, interactionDirection, interactionDistance, interactableLayer);

        if (hit.collider != null) // Si el rayo golpeó algún collider en la capa especificada.
        {
            // Comprobación para evitar que el jugador interactúe consigo mismo si accidentalmente está en la interactableLayer.
            if (hit.collider.gameObject == this.gameObject)
            {
               
                return; // No hacer nada más.
            }



            // Intenta obtener el componente NPCDialogue del objeto golpeado.
            NPCDialogue npcDialogue = hit.collider.GetComponent<NPCDialogue>();
            if (npcDialogue != null) // Si el objeto golpeado tiene un script NPCDialogue.
            {
                // Solo iniciar un nuevo diálogo si el NPC no está ya en uno.
                // (NPCDialogue.StartDialogue también tiene esta comprobación, pero es una buena práctica verificar aquí también).
                if (!npcDialogue.IsDialogueActive())
                {
                    Debug.Log("PlayerInteraction: Iniciando diálogo con " + hit.collider.name + ". Deshabilitando input del jugador.");
                    npcDialogue.StartDialogue(this.transform); // Llama al método para iniciar el diálogo en el NPC, pasando el Transform del jugador.
                    currentActiveDialogue = npcDialogue;       // Guarda la referencia a este diálogo como el activo.
                    playerMovement.SetCanProcessInput(false);  // DESHABILITAR el input del jugador a través de PlayerMovement.
                }

            }

        }

    }
}
