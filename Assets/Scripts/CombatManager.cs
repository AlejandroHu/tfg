using System.Collections;
using System.Collections.Generic;
using TopDown;
using UnityEngine;
// Asegúrate de que los namespaces de Character, EnemyData, PlayerMovement sean accesibles
// using TuJuego.Personajes; // Si Character.cs está aquí
// using TuJuego.Enemigos;   // Si EnemyData.cs está aquí
// using TopDown;          // Si PlayerMovement.cs está aquí

public class CombatManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static CombatManager Instance { get; private set; }

    [Header("Estado del Combate")]
    [SerializeField] private bool isCombatActive = false;

    // Referencias a los participantes del combate actual
    private List<Character> currentPlayerParty;
    private List<EnemyData> currentEnemyGroup;

    // Referencia al script de movimiento del jugador para pausarlo
    private PlayerMovement playerMovementController;

    void Awake()
    {
        // Lógica del Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CombatManager: Se encontró otra instancia. Destruyendo este GameObject.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Considera si este manager debe persistir entre escenas
        // o si cada escena de exploración con combate tendrá el suyo.
        // Si el combate ocurre en la misma escena, podría no ser necesario
        // si el CombatManager está en un objeto que no se destruye con la escena principal.
        // Si tienes escenas de combate separadas, SÍ necesitará persistir o pasar datos.
    }

    void Start()
    {
        // Intentar encontrar el PlayerMovement automáticamente
        // Esto asume que solo hay un PlayerMovement activo en la escena.
        playerMovementController = FindObjectOfType<PlayerMovement>();
        if (playerMovementController == null)
        {
            Debug.LogWarning("CombatManager: No se encontró el script PlayerMovement en la escena.");
        }
    }

    /// <summary>
    /// Inicia una secuencia de combate.
    /// </summary>
    /// <param name="playerParty">La lista de personajes del jugador que participarán.</param>
    /// <param name="enemyGroup">La lista de datos de los enemigos para este encuentro.</param>
    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup)
    {
        if (isCombatActive)
        {
            Debug.LogWarning("CombatManager: Se intentó iniciar un combate mientras otro ya estaba activo.");
            return;
        }
        if (playerParty == null || playerParty.Count == 0)
        {
            Debug.LogError("CombatManager: Se intentó iniciar combate sin personajes en la party del jugador.");
            return;
        }
        if (enemyGroup == null || enemyGroup.Count == 0)
        {
            Debug.LogError("CombatManager: Se intentó iniciar combate sin enemigos.");
            return;
        }

        isCombatActive = true;
        this.currentPlayerParty = new List<Character>(playerParty); // Copiar la lista
        this.currentEnemyGroup = new List<EnemyData>(enemyGroup);   // Copiar la lista

        // 1. Pausar al jugador (y quizás a otros NPCs en el mapa)
        if (playerMovementController != null)
        {
            //playerMovementController.SetCanMove(false); // Asume que PlayerMovement tiene un método SetCanMove
            Debug.Log("CombatManager: Movimiento del jugador DESACTIVADO.");
        }

        // 2. (Futuro) Realizar transición visual a la "arena" de combate
        //    Esto podría implicar activar/desactivar GameObjects, cambiar la cámara, etc.
        Debug.Log("--- ¡COMBATE INICIADO! ---");
        Debug.Log("Party del Jugador:");
        foreach (Character member in currentPlayerParty)
        {
            Debug.Log("- " + member.characterName);
        }
        Debug.Log("Grupo de Enemigos:");
        foreach (EnemyData enemy in currentEnemyGroup)
        {
            Debug.Log("- " + enemy.enemyName);
        }

        // 3. (Futuro) Activar la UI de combate
        //    UIManager.Instance.ShowCombatUI(currentPlayerParty, currentEnemyGroup);

        // 4. (Futuro) Iniciar el sistema de turnos
        //    TurnSystem.StartCombat(currentPlayerParty, currentEnemyGroup);
    }

    /// <summary>
    /// Termina la secuencia de combate actual.
    /// </summary>
    /// <param name="playerWon">True si el jugador ganó, false si perdió o huyó.</param>
    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive) return;

        Debug.Log("--- COMBATE FINALIZADO --- ¿Jugador ganó?: " + playerWon);

        // 1. (Futuro) Dar recompensas si el jugador ganó (XP, items)
        if (playerWon)
        {
            // Distribuir XP, generar loot, etc.
        }

        // 2. (Futuro) Realizar transición visual de vuelta a la exploración
        //    Desactivar UI de combate, reactivar UI de exploración.

        // 3. Reactivar el movimiento del jugador
        if (playerMovementController != null)
        {
            //playerMovementController.SetCanMove(true); // Asume que PlayerMovement tiene un método SetCanMove
            Debug.Log("CombatManager: Movimiento del jugador REACTIVADO.");
        }

        // 4. Limpiar datos del combate actual
        isCombatActive = false;
        currentPlayerParty = null;
        currentEnemyGroup = null;

        // 5. (Futuro) Actualizar el estado del EnemyEncounter (ej: marcar como derrotado)
        //    Esto podría hacerse antes, justo después de la victoria.
    }

    // Método de prueba para terminar el combate (ej: con una tecla)
    void Update()
    {
        if (isCombatActive && Input.GetKeyDown(KeyCode.Alpha0)) // Ejemplo: Tecla 0 para terminar combate (ganando)
        {
            EndCombat(true);
        }
        else if (isCombatActive && Input.GetKeyDown(KeyCode.Alpha9)) // Ejemplo: Tecla 9 para terminar combate (perdiendo)
        {
            EndCombat(false);
        }
    }
}
