using UnityEngine;
using System.Collections.Generic; // Necesario para List

// Asegúrate de que el namespace de EnemyData (que crearemos pronto) sea accesible
// using TuJuego.Enemigos; // Si EnemyData.cs estará en este namespace

/// <summary>
/// Script para adjuntar a los GameObjects de enemigos visibles en el mapa de exploración.
/// Contiene la información del grupo de enemigos que se encontrarán en combate.
/// </summary>
public class EnemyEncounter : MonoBehaviour
{
    [Header("Configuración del Encuentro")]
    [Tooltip("Lista de los datos de los enemigos que participarán en este encuentro. Crearemos un ScriptableObject 'EnemyData' para esto.")]
    public List<EnemyData> enemyGroup = new List<EnemyData>(); // Usaremos un ScriptableObject EnemyData aquí

    // [Tooltip("Identificador único para este encuentro, por si necesitas rastrear si ya fue derrotado.")]
    // public string encounterID; 

    [Tooltip("¿Este grupo de enemigos desaparece permanentemente después de ser derrotado una vez?")]
    public bool defeatPermanently = false;

    [HideInInspector] // Se gestionará a través de un sistema de guardado o un manager de estado del mundo
    public bool isDefeated = false; // Para saber si este encuentro ya fue superado

    void Awake()
    {
        // Asegurarse de que el GameObject tenga un Collider2D para la detección.
        // Es recomendable que sea un Trigger para que el jugador no choque físicamente.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "' no tiene un Collider2D. No podrá ser detectado por el jugador.", this);
        }
        else
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "': Se recomienda que su Collider2D sea 'Is Trigger = true' para evitar colisiones físicas y facilitar la detección de interacción.", this);
            }
        }
    }

    /// <summary>
    /// (A implementar más adelante) Marca este encuentro como derrotado.
    /// Podría desactivar el GameObject o notificar a un sistema de gestión de encuentros.
    /// </summary>
    public void MarkAsDefeated()
    {
        isDefeated = true;
        if (defeatPermanently)
        {
            // Desactivar este GameObject para que el enemigo desaparezca del mapa
            gameObject.SetActive(false);
            Debug.Log("Encuentro con " + gameObject.name + " marcado como derrotado permanentemente y desactivado.");
            // Aquí podrías añadir lógica para guardar el estado de este encuentro (ej: usando encounterID)
        }
        else
        {
            // Si no es permanente, quizás solo se desactiva temporalmente o se maneja de otra forma (respawn).
            // Por ahora, podemos desactivarlo también.
            gameObject.SetActive(false);
            Debug.Log("Encuentro con " + gameObject.name + " marcado como derrotado y desactivado (temporalmente).");
        }
    }

    // Podrías añadir un método OnTriggerEnter2D aquí si quieres que el enemigo
    // detecte al jugador por sí mismo (ej: para enemigos agresivos que inician el combate).
    // O, puedes dejar que el script PlayerInteraction detecte a este EnemyEncounter.
    // Ejemplo:
    /*
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDefeated) return; // No iniciar combate si ya fue derrotado

        PlayerMovement player = other.GetComponent<PlayerMovement>(); // O PlayerInteraction
        if (player != null)
        {
            Debug.Log("Player ha entrado en contacto con enemigo: " + gameObject.name);
            // Aquí se llamaría al CombatManager para iniciar el combate
            if (CombatManager.Instance != null)
            {
                // Necesitamos la lista de personajes del jugador para pasarla.
                // Esto es un placeholder, idealmente el CombatManager la obtiene del PartyManager.
                // List<Character> playerParty = new List<Character>();
                // if (PartyManager.Instance != null) playerParty = PartyManager.Instance.CurrentPartyMembers;
                
                // CombatManager.Instance.StartCombat(playerParty, enemyGroup);
                // MarkAsDefeated(); // Marcar como derrotado después de iniciar el combate
            }
        }
    }
    */
}
