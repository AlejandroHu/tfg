using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TopDown;

// (Clase Combatant sin cambios)
public class Combatant
{
    public Character characterData;
    public EnemyData enemyData;
    public GameObject combatSpriteGO;
    public int speed;
    public bool isPlayerCharacter;
    public int currentHP;
    public int maxHP;
    public bool isDefeated = false;

    public Combatant(Character character, GameObject spriteGO)
    {
        characterData = character;
        enemyData = null;
        combatSpriteGO = spriteGO;
        speed = character.Speed;
        isPlayerCharacter = true;
        currentHP = character.currentHP;
        maxHP = character.MaxHP;
        isDefeated = (currentHP <= 0);
    }

    public Combatant(EnemyData enemy, GameObject spriteGO)
    {
        characterData = null;
        enemyData = enemy;
        combatSpriteGO = spriteGO;
        speed = enemy.baseSpeed;
        isPlayerCharacter = false;
        currentHP = enemy.maxHP;
        maxHP = enemy.maxHP;
        isDefeated = false;
    }

    public string GetName() { return isPlayerCharacter ? characterData.characterName : enemyData.enemyName; }
    public int GetCurrentHP() { return currentHP; }
    public int GetMaxHP() { return isPlayerCharacter ? characterData.MaxHP : enemyData.maxHP; }
    public int GetAttack() { return isPlayerCharacter ? (characterData != null ? characterData.Attack : 0) : (enemyData != null ? enemyData.baseAttack : 0); }
    public int GetDefense() { return isPlayerCharacter ? (characterData != null ? characterData.Defense : 0) : (enemyData != null ? enemyData.baseDefense : 0); }

    public void TakeDamage(int amount)
    {
        if (isDefeated) return;
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        Debug.Log($"{GetName()} recibe {amount} de daño. HP restante: {currentHP}/{GetMaxHP()}");
        if (currentHP <= 0)
        {
            isDefeated = true;
            Debug.Log($"{GetName()} ha sido derrotado!");
            if (combatSpriteGO != null) combatSpriteGO.SetActive(false);
        }
        if (isPlayerCharacter && CombatManager.Instance != null)
        {
            CombatManager.Instance.UpdatePartyStatusHUD();
        }
    }
}


public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Estado del Combate")]
    [SerializeField] private bool isCombatActive = false;
    public bool IsCombatActive => isCombatActive;
    private bool isSelectingTargetForAttack = false;
    private Combatant attackerForTargetSelection;

    [Header("Configuración de Escena y UI")]
    [SerializeField] private GameObject explorationRootGameObject;
    [SerializeField] private GameObject currentCombatArenaGameObject;
    [SerializeField] private CinemachineVirtualCamera explorationCamera;
    [SerializeField] private CinemachineVirtualCamera combatCamera;
    [SerializeField] private GameObject combatScreenUIPanel;
    [SerializeField] private CanvasGroup fadePanelCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;
    [Header("Posiciones de Combate")]
    [SerializeField] private List<Transform> partySpawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();
    [Header("HUD de Combate - Estado de la Party")]
    [SerializeField] private Transform partyStatusAreaContainer;
    [SerializeField] private GameObject partyMemberStatusUIPrefab;
    [Header("HUD de Combate - Menú de Acciones")]
    [SerializeField] private GameObject actionMenuPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button itemsButton;
    [SerializeField] private Button fleeButton;
    [Header("Capa de los Combatientes")]
    [SerializeField] private LayerMask combatantLayerMask;

    private List<Character> currentPlayerPartyData;
    private List<EnemyData> currentEnemyGroupData;
    private PlayerMovement playerMovementController;
    private EnemyEncounter _activeEncounter;
    private List<GameObject> _partyCombatSpriteGOs = new List<GameObject>();
    private List<GameObject> _enemyCombatSpriteGOs = new List<GameObject>();
    private List<PartyMemberCombatStatusUI> _partyStatusUIs = new List<PartyMemberCombatStatusUI>();
    private List<Combatant> _combatants = new List<Combatant>();
    private int _currentCombatantIndex = -1;
    private Combatant _activeCombatant;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("CombatManager: Instancia DUPLICADA. Destruyendo este: " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("CombatManager: Awake - Instancia configurada.");
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

        if (partyStatusAreaContainer == null) Debug.LogError("CombatManager: 'partyStatusAreaContainer' no asignado.", this);
        if (partyMemberStatusUIPrefab == null) Debug.LogError("CombatManager: 'partyMemberStatusUIPrefab' no asignado.", this);

        if (actionMenuPanel == null) Debug.LogError("CombatManager: 'actionMenuPanel' no asignado.", this);
        else actionMenuPanel.SetActive(false);

        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClicked);
        // (Añadir listeners para otros botones cuando se implementen sus acciones)
    }

    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup, EnemyEncounter encounterReference)
    {
        if (isCombatActive)
        {
            Debug.LogWarning("CombatManager: StartCombat llamado pero isCombatActive ya es true.");
            return;
        }
        if (playerParty == null || playerParty.Count == 0) { Debug.LogError("CombatManager: Party vacía al iniciar combate."); return; }
        if (enemyGroup == null || enemyGroup.Count == 0) { Debug.LogError("CombatManager: Grupo de enemigos vacío al iniciar combate."); return; }
        if (encounterReference == null) { Debug.LogError("CombatManager: Referencia a EnemyEncounter nula al iniciar combate."); return; }

        Debug.Log("CombatManager: StartCombat - INICIANDO. Estableciendo isCombatActive a true.");
        isCombatActive = true; // Establecer aquí antes de la corrutina

        this.currentPlayerPartyData = new List<Character>(playerParty);
        this.currentEnemyGroupData = new List<EnemyData>(enemyGroup);
        this._activeEncounter = encounterReference;

        StartCoroutine(CombatTransitionCoroutine(true));
    }

    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive && !isSelectingTargetForAttack)
        {
            Debug.LogWarning("CombatManager: EndCombat llamado pero isCombatActive ya era false y no se estaba seleccionando objetivo.");
            // return; // Permitir que se ejecute para limpiar la UI por si acaso
        }
        Debug.Log("CombatManager: EndCombat - INICIANDO. Estableciendo isCombatActive a false.");
        isCombatActive = false;
        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;

        StartCoroutine(CombatTransitionCoroutine(false, playerWon));
    }

    private IEnumerator CombatTransitionCoroutine(bool startingCombat, bool playerWon = false)
    {
        Debug.Log($"CombatManager: CombatTransitionCoroutine - startingCombat: {startingCombat}, isCombatActive actual: {isCombatActive}");
        // isCombatActive ya debería estar establecido por StartCombat o EndCombat ANTES de llamar a esta corrutina.

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

        // Configuración/Restauración MIENTRAS la pantalla está en negro
        if (startingCombat)
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = false;

            SetupCombatantsAndPrepareTurnOrder();
            PopulatePartyStatusUI();

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
        }
        else // Terminando el combate
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = true;

            CleanupCombatants();
            CleanupPartyStatusUI();

            if (explorationCamera != null) explorationCamera.Priority = 10;
            if (combatCamera != null) combatCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);

            if (playerWon && _activeEncounter != null)
            {
                _activeEncounter.MarkAsDefeated();
            }
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
            this.currentPlayerPartyData = null;
            this.currentEnemyGroupData = null;
            this._activeEncounter = null;
            // isCombatActive ya se estableció a false en EndCombat()
            Debug.Log("CombatManager: CombatTransitionCoroutine - Fin de EndCombat. isCombatActive: " + isCombatActive);
        }
        else // Al iniciar el combate
        {
            // isCombatActive ya se estableció a true en StartCombat()
            Debug.Log("CombatManager: CombatTransitionCoroutine - Fin de StartCombat. isCombatActive: " + isCombatActive + ". Llamando a NextTurn.");
            NextTurn();
        }
    }

    private void SetupCombatantsAndPrepareTurnOrder()
    {
        CleanupCombatants();
        _combatants.Clear();

        if (currentPlayerPartyData != null)
        {
            for (int i = 0; i < currentPlayerPartyData.Count; i++)
            {
                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerPartyData[i] != null)
                {
                    GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + currentPlayerPartyData[i].characterName);
                    partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);

                    SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = currentPlayerPartyData[i].portraitSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    _partyCombatSpriteGOs.Add(partyMemberSpriteGO);
                    _combatants.Add(new Combatant(currentPlayerPartyData[i], partyMemberSpriteGO));
                }
            }
        }

        if (currentEnemyGroupData != null)
        {
            for (int i = 0; i < currentEnemyGroupData.Count; i++)
            {
                if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null && currentEnemyGroupData[i] != null)
                {
                    GameObject enemySpriteGO = new GameObject("EnemyCombatSprite_" + currentEnemyGroupData[i].enemyName);
                    enemySpriteGO.transform.position = enemySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) enemySpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);

                    SpriteRenderer sr = enemySpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = currentEnemyGroupData[i].battleSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    BoxCollider2D col = enemySpriteGO.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    if (sr.sprite != null)
                    {
                        col.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);
                        // float yOffset = -sr.sprite.bounds.extents.y * 0.2f; 
                        // col.offset = new Vector2(0, yOffset); 
                    }
                    else col.size = new Vector2(0.5f, 0.5f);

                    enemySpriteGO.layer = LayerMask.NameToLayer("EnemiesInCombat");

                    _enemyCombatSpriteGOs.Add(enemySpriteGO);
                    _combatants.Add(new Combatant(currentEnemyGroupData[i], enemySpriteGO));
                }
            }
        }

        _combatants = _combatants.OrderByDescending(c => c.speed).ToList();
        _currentCombatantIndex = -1;

        Debug.Log("CombatManager: Lista de combatientes preparada. Total: " + _combatants.Count);
    }

    private void CleanupCombatants()
    {
        foreach (GameObject go in _partyCombatSpriteGOs) if (go != null) Destroy(go);
        _partyCombatSpriteGOs.Clear();
        foreach (GameObject go in _enemyCombatSpriteGOs) if (go != null) Destroy(go);
        _enemyCombatSpriteGOs.Clear();
    }

    private void PopulatePartyStatusUI()
    {
        if (partyStatusAreaContainer == null || partyMemberStatusUIPrefab == null) return;
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs) if (ui != null) Destroy(ui.gameObject);
        _partyStatusUIs.Clear();
        if (currentPlayerPartyData == null || currentPlayerPartyData.Count == 0) return;

        foreach (Character partyMember in currentPlayerPartyData)
        {
            if (partyMember == null) continue;
            GameObject statusGO = Instantiate(partyMemberStatusUIPrefab, partyStatusAreaContainer);
            PartyMemberCombatStatusUI statusUI = statusGO.GetComponent<PartyMemberCombatStatusUI>();
            if (statusUI != null)
            {
                statusUI.SetupStatus(partyMember);
                _partyStatusUIs.Add(statusUI);
            }
            else Destroy(statusGO);
        }
    }

    private void CleanupPartyStatusUI()
    {
        foreach (PartyMemberCombatStatusUI ui in _partyStatusUIs) if (ui != null) Destroy(ui.gameObject);
        _partyStatusUIs.Clear();
    }

    public void UpdatePartyStatusHUD()
    {
        if (_partyStatusUIs == null) return;
        foreach (PartyMemberCombatStatusUI statusUI in _partyStatusUIs)
        {
            if (statusUI != null && statusUI.gameObject != null && statusUI.gameObject.activeInHierarchy)
            {
                statusUI.UpdateUIElements();
            }
        }
    }

    private void NextTurn()
    {
        Debug.Log($"CombatManager: NextTurn() - INICIO. isCombatActive: {isCombatActive}");
        if (!isCombatActive)
        {
            Debug.LogWarning("CombatManager: NextTurn() - Saliendo porque isCombatActive es false.");
            return;
        }

        if (CheckCombatEndConditions())
        {
            Debug.Log("CombatManager: NextTurn() - Saliendo porque CheckCombatEndConditions() devolvió true (combate terminado).");
            return;
        }

        _currentCombatantIndex++;
        if (_currentCombatantIndex >= _combatants.Count)
        {
            _currentCombatantIndex = 0;
            Debug.Log("CombatManager: ----- Nueva Ronda de Combate Iniciada -----");
        }

        if (_combatants.Count == 0)
        {
            Debug.LogError("CombatManager: No hay combatientes para el siguiente turno.");
            EndCombat(false);
            return;
        }
        _activeCombatant = _combatants[_currentCombatantIndex];

        if (_activeCombatant.isDefeated)
        {
            Debug.Log($"CombatManager: {_activeCombatant.GetName()} está derrotado. Saltando turno.");
            NextTurn();
            return;
        }

        StartTurnForActiveCombatant();
    }

    private void StartTurnForActiveCombatant()
    {
        Debug.Log($"CombatManager: StartTurnForActiveCombatant() - INICIO para {_activeCombatant?.GetName()}. isCombatActive: {isCombatActive}");
        if (_activeCombatant == null || _activeCombatant.isDefeated)
        {
            Debug.LogWarning($"CombatManager: StartTurnForActiveCombatant - _activeCombatant es nulo o derrotado. Intentando NextTurn.");
            NextTurn();
            return;
        }

        Debug.Log($"CombatManager: Iniciando turno para {_activeCombatant.GetName()} (HP: {_activeCombatant.GetCurrentHP()}/{_activeCombatant.GetMaxHP()})");

        if (_activeCombatant.isPlayerCharacter)
        {
            if (actionMenuPanel != null)
            {
                Debug.Log("CombatManager: Activando actionMenuPanel para jugador.");
                actionMenuPanel.SetActive(true);
            }
            else Debug.LogError("CombatManager: actionMenuPanel es NULO al intentar activarlo para jugador.");
        }
        else
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            StartCoroutine(EnemyTurnCoroutine(_activeCombatant));
        }
    }

    private IEnumerator EnemyTurnCoroutine(Combatant enemy)
    {
        Debug.Log($"CombatManager: {enemy.GetName()} está pensando...");
        yield return new WaitForSeconds(1.5f);

        if (enemy.isDefeated)
        {
            NextTurn();
            yield break;
        }

        List<Combatant> livingPlayerCombatants = _combatants.Where(c => c.isPlayerCharacter && !c.isDefeated).ToList();

        if (livingPlayerCombatants.Count > 0)
        {
            Combatant target = livingPlayerCombatants[Random.Range(0, livingPlayerCombatants.Count)];
            Debug.Log($"{enemy.GetName()} ataca a {target.GetName()}!");

            int damage = Mathf.Max(1, enemy.GetAttack() - target.GetDefense());
            target.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"{enemy.GetName()} no tiene objetivos vivos en la party.");
        }

        Debug.Log($"CombatManager: Turno de {enemy.GetName()} finalizado.");
        yield return new WaitForSeconds(0.5f);
        NextTurn();
    }

    public void OnAttackButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter || isSelectingTargetForAttack) return;

        Debug.Log($"{_activeCombatant.GetName()} seleccionó ATACAR. Por favor, selecciona un objetivo enemigo.");
        isSelectingTargetForAttack = true;
        attackerForTargetSelection = _activeCombatant;

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
    }

    private void ExecuteAttack(Combatant attacker, Combatant target)
    {
        if (attacker == null || target == null || target.isDefeated)
        {
            Debug.LogWarning("ExecuteAttack: Atacante, objetivo no válido o ya derrotado.");
            isSelectingTargetForAttack = false;
            if (attacker != null && attacker.isPlayerCharacter && actionMenuPanel != null) actionMenuPanel.SetActive(true);
            return;
        }

        Debug.Log($"{attacker.GetName()} ataca a {target.GetName()}!");

        int damage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
        target.TakeDamage(damage);

        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;

        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.5f));
    }

    private IEnumerator EndPlayerActionAndProceedToNextTurn(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextTurn();
    }

    private bool CheckCombatEndConditions()
    {
        bool allEnemiesDefeated = _combatants.Where(c => !c.isPlayerCharacter).All(e => e.isDefeated);
        if (allEnemiesDefeated && _combatants.Any(c => !c.isPlayerCharacter))
        {
            Debug.Log("CombatManager: ¡Todos los enemigos derrotados! El jugador gana.");
            EndCombat(true);
            return true;
        }

        bool allPlayersDefeated = _combatants.Where(c => c.isPlayerCharacter).All(p => p.isDefeated);
        if (allPlayersDefeated && _combatants.Any(c => c.isPlayerCharacter))
        {
            Debug.Log("CombatManager: ¡Toda la party derrotada! Game Over.");
            EndCombat(false);
            return true;
        }
        return false;
    }

    void Update()
    {
        if (isCombatActive)
        {
            if (isSelectingTargetForAttack && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);
                if (hit.collider != null)
                {
                    Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject && !c.isPlayerCharacter);
                    if (targetCombatant != null)
                    {
                        ExecuteAttack(attackerForTargetSelection, targetCombatant);
                    }
                }
            }

            if (!isSelectingTargetForAttack)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
                else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
            }
        }
    }
}
