using UnityEngine;
using System.Collections.Generic; // Necesario para List

// Asegúrate de que los namespaces de Character, EnemyData, PlayerMovement sean accesibles
// Si están en un namespace como "TopDown", necesitarás:
 using TopDown; 
// O los namespaces específicos si son diferentes:
// using TuJuego.Personajes; 
// using TuJuego.Enemigos;   

/// <summary>
/// Gestiona el inicio, el flujo y el final de las secuencias de combate.
/// </summary>
public class CombatManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static CombatManager Instance { get; private set; }

    [Header("Estado del Combate")]
    [Tooltip("Indica si una secuencia de combate está actualmente activa.")]
    [SerializeField] private bool isCombatActive = false;
    public bool IsCombatActive => isCombatActive; // Propiedad para leer el estado desde fuera

    // Referencias a los participantes del combate actual
    private List<Character> currentPlayerParty;
    private List<EnemyData> currentEnemyGroup;

    // Referencia al script de movimiento del jugador para pausarlo/reanudarlo
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
        // DontDestroyOnLoad(gameObject); // Considerar si este manager debe persistir entre escenas.
        // Si el combate siempre ocurre en la misma escena de exploración
        // y este manager está en un objeto de esa escena, podría no ser necesario.
        // Si tienes escenas de combate separadas, SÍ necesitará persistir.
    }

    void Start()
    {
        // Intentar encontrar el PlayerMovement automáticamente al inicio.
        // Esto asume que solo hay un PlayerMovement activo en la escena.
        playerMovementController = FindObjectOfType<PlayerMovement>();
        if (playerMovementController == null)
        {
            Debug.LogWarning("CombatManager: No se encontró el script PlayerMovement en la escena. La pausa del jugador podría no funcionar.", this);
        }
    }

    /// <summary>
    /// Inicia una secuencia de combate con la party del jugador y un grupo de enemigos.
    /// </summary>
    /// <param name="playerParty">La lista de componentes Character de los personajes del jugador que participarán.</param>
    /// <param name="enemyGroup">La lista de ScriptableObjects EnemyData para los enemigos de este encuentro.</param>
    /// <param name="encounterReference">La referencia al EnemyEncounter que inició el combate (para marcarlo como derrotado después).</param>
    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup, EnemyEncounter encounterReference)
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
            Debug.LogError("CombatManager: Se intentó iniciar combate sin enemigos en el enemyGroup.");
            return;
        }
        if (encounterReference == null)
        {
            Debug.LogError("CombatManager: Se intentó iniciar combate sin una referencia al EnemyEncounter.");
            return;
        }

        Debug.Log("--- ¡COMBATE INICIADO! ---");
        isCombatActive = true;

        // Guardar copias de las listas de participantes para este combate.
        this.currentPlayerParty = new List<Character>(playerParty);
        this.currentEnemyGroup = new List<EnemyData>(enemyGroup);
        // Guardar la referencia al encuentro para poder marcarlo como derrotado después.
        // private EnemyEncounter _activeEncounter = encounterReference; // Necesitarás declarar _activeEncounter

        // 1. Pausar al jugador (y potencialmente otros sistemas de exploración)
        if (playerMovementController != null)
        {
            playerMovementController.SetCanMove(false);
            Debug.Log("CombatManager: Movimiento del jugador DESACTIVADO.");
        }

        // 2. (FUTURO) Realizar transición visual a la "arena" de combate.
        //    Esto podría implicar activar/desactivar GameObjects, cambiar la cámara,
        //    cargar un fondo de batalla específico, etc.
        //    Ejemplo: UIManager.Instance.ShowBattleTransition();

        Debug.Log("Party del Jugador en combate:");
        foreach (Character member in currentPlayerParty)
        {
            if (member != null) Debug.Log("- " + member.characterName);
        }
        Debug.Log("Grupo de Enemigos en combate:");
        foreach (EnemyData enemy in currentEnemyGroup)
        {
            if (enemy != null) Debug.Log("- " + enemy.enemyName);
        }

        // 3. (FUTURO) Activar la Interfaz de Usuario (UI) de combate.
        //    Ejemplo: CombatUIManager.Instance.ShowCombatUI(currentPlayerParty, currentEnemyGroup);

        // 4. (FUTURO) Iniciar el sistema de turnos y la lógica del combate.
        //    Ejemplo: TurnBasedSystem.StartCombat(currentPlayerParty, currentEnemyGroup);
    }

    /// <summary>
    /// Termina la secuencia de combate actual.
    /// </summary>
    /// <param name="playerWon">True si el jugador ganó, false si perdió o huyó.</param>
    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive)
        {
            // Debug.LogWarning("CombatManager: Se intentó terminar un combate que no estaba activo."); // Puede ser muy verboso
            return;
        }

        Debug.Log("--- COMBATE FINALIZADO --- ¿Jugador ganó?: " + playerWon);

        // 1. (FUTURO) Procesar resultados del combate.
        if (playerWon)
        {
            // Dar recompensas (XP, objetos, etc.)
            // DistributeRewards();

            // Marcar el encuentro como derrotado (si es necesario)
            // if (_activeEncounter != null)
            // {
            //    _activeEncounter.MarkAsDefeated();
            // }
        }
        else
        {
            // Lógica de Game Over o penalización.
            // HandlePlayerDefeat();
        }

        // 2. (FUTURO) Realizar transición visual de vuelta a la exploración.
        //    Ejemplo: UIManager.Instance.HideBattleTransition();
        //    Desactivar UI de combate, reactivar UI de exploración.

        // 3. Reactivar el movimiento del jugador (y otros sistemas de exploración).
        if (playerMovementController != null)
        {
            playerMovementController.SetCanMove(true);
            Debug.Log("CombatManager: Movimiento del jugador REACTIVADO.");
        }

        // 4. Limpiar datos del combate actual.
        isCombatActive = false;
        currentPlayerParty = null;
        currentEnemyGroup = null;
        // _activeEncounter = null;
    }

    // Método de prueba para terminar el combate (ej: con una tecla).
    // Esto es útil durante el desarrollo antes de tener las condiciones de victoria/derrota implementadas.
    void Update()
    {
        if (!isCombatActive) return; // Solo procesar si el combate está activo.

        if (Input.GetKeyDown(KeyCode.Alpha0)) // Ejemplo: Tecla 0 para simular victoria del jugador.
        {
            Debug.Log("CombatManager: Forzando fin de combate (VICTORIA) con tecla 0.");
            EndCombat(true);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9)) // Ejemplo: Tecla 9 para simular derrota del jugador.
        {
            Debug.Log("CombatManager: Forzando fin de combate (DERROTA) con tecla 9.");
            EndCombat(false);
        }
    }
}
