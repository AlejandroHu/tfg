using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TopDown; // Asumiendo que PlayerMovement y Character están aquí
// using TuJuego.Personajes; // Si Character.cs está aquí
// using TuJuego.Enemigos;   // Si EnemyData.cs está aquí

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
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Posiciones de Combate (Dentro de la Arena de Combate)")]
    [SerializeField] private List<Transform> partySpawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();

    // --- NUEVAS REFERENCIAS PARA EL HUD DE LA PARTY ---
    [Header("HUD de Combate - Estado de la Party")]
    [Tooltip("Transform padre (con HorizontalLayoutGroup) donde se instanciarán los UI de estado de cada miembro de la party.")]
    [SerializeField] private Transform partyStatusAreaContainer;
    [Tooltip("Prefab para mostrar el estado de un miembro de la party (debe tener PartyMemberCombatStatusUI.cs).")]
    [SerializeField] private GameObject partyMemberStatusUIPrefab;

    // --- Variables Internas ---
    private List<Character> currentPlayerParty;
    private List<EnemyData> currentEnemyGroup;
    private PlayerMovement playerMovementController;
    private EnemyEncounter _activeEncounter;

    private List<GameObject> _partyCombatSprites = new List<GameObject>();
    private List<GameObject> _enemyCombatSprites = new List<GameObject>();
    // --- NUEVA LISTA PARA LOS UI DE ESTADO DE LA PARTY ---
    private List<PartyMemberCombatStatusUI> _partyStatusUIs = new List<PartyMemberCombatStatusUI>();


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
            fadePanelCanvasGroup.alpha = 0f;
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'fadePanelCanvasGroup' no asignado.", this);
        }

        if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);
        if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
        if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);

        if (explorationCamera != null) explorationCamera.Priority = 10;
        if (combatCamera != null) combatCamera.Priority = 9;

        // --- NUEVAS VALIDACIONES ---
        if (partyStatusAreaContainer == null) Debug.LogError("CombatManager: 'partyStatusAreaContainer' no asignado. No se podrá mostrar el estado de la party.", this);
        if (partyMemberStatusUIPrefab == null) Debug.LogError("CombatManager: 'partyMemberStatusUIPrefab' no asignado. No se podrá mostrar el estado de la party.", this);
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
            fadePanelCanvasGroup.gameObject.SetActive(true);
            fadePanelCanvasGroup.alpha = 0f;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                fadePanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            fadePanelCanvasGroup.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        // --- Configuración/Restauración MIENTRAS la pantalla está en negro ---
        if (startingCombat)
        {
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSpriteRenderer)) playerSpriteRenderer.enabled = false;

            SetupCombatants();
            PopulatePartyStatusUI(); // --- LLAMADA PARA POBLAR EL HUD DE LA PARTY ---

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
        }
        else // Terminando el combate
        {
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSpriteRenderer)) playerSpriteRenderer.enabled = true;

            CleanupCombatants();
            CleanupPartyStatusUI(); // --- LLAMADA PARA LIMPIAR EL HUD DE LA PARTY ---

            if (explorationCamera != null) explorationCamera.Priority = 10;
            if (combatCamera != null) combatCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);

            if (playerWon && _activeEncounter != null) _activeEncounter.MarkAsDefeated();
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

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
            fadePanelCanvasGroup.gameObject.SetActive(false);
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
            // Debug.Log("CombatManager: Combate listo para empezar la lógica de turnos.");
        }
    }

    private void SetupCombatants()
    {
        CleanupCombatants();
        // Posicionar Party
        for (int i = 0; i < currentPlayerParty.Count; i++)
        {
            if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerParty[i] != null)
            {
                GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + currentPlayerParty[i].characterName);
                SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                sr.sprite = currentPlayerParty[i].portraitSprite; // O un campo battleSprite si lo tienes en Character.cs
                sr.sortingLayerName = "Characters_Combat"; // Asegúrate de tener este Sorting Layer
                // Aquí deberías ajustar el tamaño/escala del sprite si es necesario
                // Ejemplo: partyMemberSpriteGO.transform.localScale = new Vector3(1.5f, 1.5f, 1f); // Si tus sprites de 48x48 necesitan ser escalados
                partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);
                _partyCombatSprites.Add(partyMemberSpriteGO);
            }
        }

        // Posicionar Enemigos
        for (int i = 0; i < currentEnemyGroup.Count; i++)
        {
            if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null && currentEnemyGroup[i] != null)
            {
                GameObject enemySpriteGO = new GameObject("EnemyCombatSprite_" + currentEnemyGroup[i].enemyName);
                SpriteRenderer sr = enemySpriteGO.AddComponent<SpriteRenderer>();
                sr.sprite = currentEnemyGroup[i].battleSprite;
                sr.sortingLayerName = "Characters_Combat";
                // Ajustar escala si es necesario
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

    // --- NUEVOS MÉTODOS PARA EL HUD DE ESTADO DE LA PARTY ---
    private void PopulatePartyStatusUI()
    {
        if (partyStatusAreaContainer == null || partyMemberStatusUIPrefab == null)
        {
            Debug.LogError("CombatManager: Faltan referencias para poblar el PartyStatusUI (partyStatusAreaContainer o partyMemberStatusUIPrefab).");
            return;
        }

        // Limpiar UIs antiguos
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        _partyStatusUIs.Clear();

        if (currentPlayerParty == null || currentPlayerParty.Count == 0)
        {
            Debug.Log("CombatManager: No hay party para mostrar en el HUD.");
            return;
        }

        // Crear un UI de estado por cada miembro de la party
        foreach (Character partyMember in currentPlayerParty)
        {
            if (partyMember == null)
            {
                Debug.LogWarning("CombatManager: Se encontró un miembro nulo en currentPlayerParty al poblar el HUD.");
                continue;
            }

            GameObject statusGO = Instantiate(partyMemberStatusUIPrefab, partyStatusAreaContainer);
            PartyMemberCombatStatusUI statusUI = statusGO.GetComponent<PartyMemberCombatStatusUI>();
            if (statusUI != null)
            {
                statusUI.SetupStatus(partyMember);
                _partyStatusUIs.Add(statusUI);
            }
            else
            {
                Debug.LogError("CombatManager: El prefab 'partyMemberStatusUIPrefab' no tiene el componente PartyMemberCombatStatusUI.");
                Destroy(statusGO); // Limpiar si el prefab está mal configurado
            }
        }
    }

    private void CleanupPartyStatusUI()
    {
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        _partyStatusUIs.Clear();
    }

    // (Opcional) Método para actualizar el HUD de la party durante el combate (ej: después de un ataque)
    public void UpdatePartyStatusHUD()
    {
        if (_partyStatusUIs == null) return; // Comprobación adicional

        foreach (PartyMemberCombatStatusUI statusUI in _partyStatusUIs)
        {
            // Comprobar si statusUI y su gameObject aún existen antes de llamar a UpdateUIElements
            if (statusUI != null && statusUI.gameObject != null && statusUI.gameObject.activeInHierarchy)
            {
                statusUI.UpdateUIElements(); // Asume que PartyMemberCombatStatusUI tiene este método
            }
        }
    }
    // --- FIN MÉTODOS HUD PARTY ---

    void Update()
    {
        if (!isCombatActive) return;
        if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
        else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
        // Ejemplo de cómo podrías actualizar el HUD si algo cambia (ej: un personaje recibe daño)
        // if (Input.GetKeyDown(KeyCode.H)) 
        // {
        //    if(currentPlayerParty != null && currentPlayerParty.Count > 0 && currentPlayerParty[0] != null)
        //    {
        //        currentPlayerParty[0].TakeDamage(10); // Personaje 0 recibe 10 de daño
        //        UpdatePartyStatusHUD(); // Refrescar el HUD
        //    }
        // }
    }
}
