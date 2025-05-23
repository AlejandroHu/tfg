using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TopDown;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Estado del Combate")]
    [SerializeField] private bool isCombatActive = false;
    public bool IsCombatActive => isCombatActive;

    [Header("Configuración de Escena y UI")]
    [Tooltip("GameObject raíz que contiene todos los elementos de la exploración.")]
    [SerializeField] private GameObject explorationRootGameObject;
    [Tooltip("GameObject raíz que contiene la arena de combate actual.")]
    [SerializeField] private GameObject currentCombatArenaGameObject;
    [Tooltip("Cámara virtual de Cinemachine para la exploración.")]
    [SerializeField] private CinemachineVirtualCamera explorationCamera;
    [Tooltip("Cámara virtual de Cinemachine para el combate.")]
    [SerializeField] private CinemachineVirtualCamera combatCamera;
    [Tooltip("GameObject raíz del panel de UI para la pantalla de combate.")]
    [SerializeField] private GameObject combatScreenUIPanel;
    [Tooltip("Referencia al CanvasGroup del panel de fundido (fade).")]
    [SerializeField] private CanvasGroup fadePanelCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f; // Un poco más rápido puede sentirse mejor

    [Header("Posiciones de Combate (Dentro de la Arena de Combate)")]
    [SerializeField] private List<Transform> partySpawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();

    private List<Character> currentPlayerParty;
    private List<EnemyData> currentEnemyGroup;
    private PlayerMovement playerMovementController;
    private EnemyEncounter _activeEncounter;

    private List<GameObject> _partyCombatSprites = new List<GameObject>();
    private List<GameObject> _enemyCombatSprites = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        playerMovementController = FindObjectOfType<PlayerMovement>();
        if (playerMovementController == null) Debug.LogWarning("CombatManager: No se encontró PlayerMovement.", this);

        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.alpha = 0f; // Asegurarse de que esté transparente al inicio
            fadePanelCanvasGroup.gameObject.SetActive(false); // Y desactivado
        }
        else
        {
            Debug.LogWarning("CombatManager: 'fadePanelCanvasGroup' no asignado. El fundido no funcionará.", this);
        }

        if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);
        if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
        if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true); // Asegurar que la exploración esté activa

        if (explorationCamera != null) explorationCamera.Priority = 10;
        if (combatCamera != null) combatCamera.Priority = 9;
    }

    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup, EnemyEncounter encounterReference)
    {
        if (isCombatActive) return;
        if (playerParty == null || playerParty.Count == 0) { Debug.LogError("CombatManager: Party vacía."); return; }
        if (enemyGroup == null || enemyGroup.Count == 0) { Debug.LogError("CombatManager: Grupo de enemigos vacío."); return; }
        if (encounterReference == null) { Debug.LogError("CombatManager: Referencia a EnemyEncounter nula."); return; }

        this.currentPlayerParty = new List<Character>(playerParty);
        this.currentEnemyGroup = new List<EnemyData>(enemyGroup);
        this._activeEncounter = encounterReference;

        StartCoroutine(CombatTransitionCoroutine(true));
    }

    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive) return;
        StartCoroutine(CombatTransitionCoroutine(false, playerWon));
    }

    private IEnumerator CombatTransitionCoroutine(bool startingCombat, bool playerWon = false)
    {
        isCombatActive = startingCombat;
        if (playerMovementController != null) playerMovementController.SetCanMove(false);

        // Fade Out
        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.gameObject.SetActive(true); // Activar el panel antes de empezar el fade
            float timer = 0f;
            while (timer < fadeDuration)
            {
                fadePanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            fadePanelCanvasGroup.alpha = 1f; // Asegurar opacidad completa
        }
        else
        {
            yield return new WaitForSeconds(0.1f); // Pequeña pausa si no hay fade panel
        }

        // --- Configuración/Restauración MIENTRAS la pantalla está en negro ---
        if (startingCombat)
        {
            Debug.Log("--- ¡COMBATE INICIADO! (Configurando escena en negro) ---");
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);

            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSpriteRenderer))
            {
                playerSpriteRenderer.enabled = false;
            }

            SetupCombatants();

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
        }
        else // Terminando el combate
        {
            Debug.Log("--- COMBATE FINALIZADO (Restaurando escena en negro) --- ¿Jugador ganó?: " + playerWon);
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);

            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSpriteRenderer))
            {
                playerSpriteRenderer.enabled = true;
            }

            CleanupCombatants();

            if (explorationCamera != null) explorationCamera.Priority = 10;
            if (combatCamera != null) combatCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);

            if (playerWon && _activeEncounter != null)
            {
                _activeEncounter.MarkAsDefeated();
            }
        }
        // --- Fin Configuración/Restauración ---

        // Esperar un frame extra puede ayudar a que los SetActive se procesen antes del Fade In
        yield return null;

        // Fade In
        if (fadePanelCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                fadePanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            fadePanelCanvasGroup.alpha = 0f;
            fadePanelCanvasGroup.gameObject.SetActive(false); // Desactivar el panel cuando es transparente
        }

        if (!startingCombat)
        {
            if (playerMovementController != null) playerMovementController.SetCanMove(true);
            this.currentPlayerParty = null;
            this.currentEnemyGroup = null;
            this._activeEncounter = null;
            isCombatActive = false;
        }
        else
        {
            Debug.Log("CombatManager: Combate listo para empezar la lógica de turnos.");
            // Aquí iniciarías la lógica de turnos
        }
    }

    private void SetupCombatants()
    {
        CleanupCombatants();
        for (int i = 0; i < currentPlayerParty.Count; i++)
        {
            if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerParty[i] != null)
            {
                GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + currentPlayerParty[i].characterName);
                SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                sr.sprite = currentPlayerParty[i].portraitSprite;
                sr.sortingLayerName = "Characters_Combat";
                partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);
                _partyCombatSprites.Add(partyMemberSpriteGO);
            }
        }
        for (int i = 0; i < currentEnemyGroup.Count; i++)
        {
            if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null && currentEnemyGroup[i] != null)
            {
                GameObject enemySpriteGO = new GameObject("EnemyCombatSprite_" + currentEnemyGroup[i].enemyName);
                SpriteRenderer sr = enemySpriteGO.AddComponent<SpriteRenderer>();
                sr.sprite = currentEnemyGroup[i].battleSprite;
                sr.sortingLayerName = "Characters_Combat";
                enemySpriteGO.transform.position = enemySpawnPoints[i].position;
                if (currentCombatArenaGameObject != null) enemySpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);
                _enemyCombatSprites.Add(enemySpriteGO);
            }
        }
    }

    private void CleanupCombatants()
    {
        foreach (GameObject go in _partyCombatSprites) Destroy(go);
        _partyCombatSprites.Clear();
        foreach (GameObject go in _enemyCombatSprites) Destroy(go);
        _enemyCombatSprites.Clear();
    }

    void Update()
    {
        if (!isCombatActive) return;
        if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
    }
}
