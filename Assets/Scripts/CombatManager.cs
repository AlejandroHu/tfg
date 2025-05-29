using UnityEngine;
using UnityEngine.UI; // Necesario para Button
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Necesario para OrderBy
using Cinemachine;
using TopDown; // Asumiendo que PlayerMovement y Character están aquí

// Clase simple para representar a cualquier participante en el combate
public class Combatant
{
    public Character characterData; // Si es un personaje del jugador
    public EnemyData enemyData;     // Si es un enemigo
    public GameObject combatSpriteGO; // El GameObject del sprite en la arena de combate
    public int speed;
    public bool isPlayerCharacter;
    public int currentHP;
    public int maxHP;
    // (Añadir más datos necesarios como currentMP, maxMP, etc.)

    // Constructor para personajes del jugador
    public Combatant(Character character, GameObject spriteGO)
    {
        characterData = character;
        enemyData = null; // No es un enemigo
        combatSpriteGO = spriteGO;
        speed = character.Speed; // Asume que Character tiene una propiedad Speed
        isPlayerCharacter = true;
        currentHP = character.currentHP; // Iniciar con HP actual del personaje
        maxHP = character.MaxHP;
    }

    // Constructor para enemigos
    public Combatant(EnemyData enemy, GameObject spriteGO)
    {
        characterData = null; // No es un personaje del jugador
        enemyData = enemy;
        combatSpriteGO = spriteGO;
        speed = enemy.baseSpeed; // Asume que EnemyData tiene baseSpeed
        isPlayerCharacter = false;
        currentHP = enemy.maxHP; // Los enemigos empiezan con HP al máximo
        maxHP = enemy.maxHP;
    }

    public string GetName()
    {
        return isPlayerCharacter ? characterData.characterName : enemyData.enemyName;
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }

    public int GetMaxHP()
    {
        return isPlayerCharacter ? characterData.MaxHP : enemyData.maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        Debug.Log($"{GetName()} recibe {amount} de daño. HP restante: {currentHP}");

        // Actualizar el HUD de la party si el objetivo es un personaje del jugador
        if (isPlayerCharacter && CombatManager.Instance != null)
        {
            CombatManager.Instance.UpdatePartyStatusHUD();
        }
        // (FUTURO: Lógica para actualizar HUD de enemigos si el objetivo es un enemigo)
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
    [Tooltip("La LayerMask que deben tener los colliders de los enemigos para ser seleccionables.")]
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

        if (partyStatusAreaContainer == null) Debug.LogError("CombatManager: 'partyStatusAreaContainer' no asignado.", this);
        if (partyMemberStatusUIPrefab == null) Debug.LogError("CombatManager: 'partyMemberStatusUIPrefab' no asignado.", this);

        if (actionMenuPanel == null) Debug.LogError("CombatManager: 'actionMenuPanel' no asignado.", this);
        else actionMenuPanel.SetActive(false);

        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClicked);
        // (Añadir listeners para otros botones cuando se implementen sus acciones)
    }

    public void StartCombat(List<Character> playerParty, List<EnemyData> enemyGroup, EnemyEncounter encounterReference)
    {
        if (isCombatActive) return;
        if (playerParty == null || playerParty.Count == 0) { Debug.LogError("CombatManager: Party vacía al iniciar combate."); return; }
        if (enemyGroup == null || enemyGroup.Count == 0) { Debug.LogError("CombatManager: Grupo de enemigos vacío al iniciar combate."); return; }
        if (encounterReference == null) { Debug.LogError("CombatManager: Referencia a EnemyEncounter nula al iniciar combate."); return; }

        this.currentPlayerPartyData = new List<Character>(playerParty);
        this.currentEnemyGroupData = new List<EnemyData>(enemyGroup);
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

        if (startingCombat)
        {
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = false;

            SetupCombatantsAndPrepareTurnOrder();
            PopulatePartyStatusUI();

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        }
        else
        {
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(true);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(false);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = true;

            CleanupCombatants();
            CleanupPartyStatusUI();

            if (explorationCamera != null) explorationCamera.Priority = 10;
            if (combatCamera != null) combatCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(false);

            if (playerWon && _activeEncounter != null) _activeEncounter.MarkAsDefeated();
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

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
            isCombatActive = false;
        }
        else
        {
            isCombatActive = true;
            Debug.Log("CombatManager: Combate listo. Iniciando primer turno.");
            NextTurn();
        }
    }

    private void SetupCombatantsAndPrepareTurnOrder()
    {
        CleanupCombatants();
        _combatants.Clear();

        // Añadir personajes de la party
        for (int i = 0; i < currentPlayerPartyData.Count; i++)
        {
            if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerPartyData[i] != null)
            {
                GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + currentPlayerPartyData[i].characterName);
                partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);

                SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                sr.sprite = currentPlayerPartyData[i].portraitSprite; // O un campo battleSprite si lo tienes
                sr.sortingLayerName = "Characters_Combat";
                // (Añadir BoxCollider2D para selección de aliados si es necesario)
                // partyMemberSpriteGO.layer = LayerMask.NameToLayer("PlayerParty"); // Capa para aliados

                _partyCombatSpriteGOs.Add(partyMemberSpriteGO);
                _combatants.Add(new Combatant(currentPlayerPartyData[i], partyMemberSpriteGO));
            }
        }

        // Añadir enemigos
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
                col.isTrigger = true; // O false si prefieres colisión física para el raycast
                                      // Ajustar col.size si es necesario

                // Asignar la capa para que el Raycast lo detecte
                enemySpriteGO.layer = LayerMask.NameToLayer("EnemiesInCombat"); // Asegúrate de que esta capa exista y esté en combatantLayerMask

                _enemyCombatSpriteGOs.Add(enemySpriteGO);
                _combatants.Add(new Combatant(currentEnemyGroupData[i], enemySpriteGO));
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
        if (!isCombatActive) return;

        // (FUTURO: Comprobar condiciones de victoria/derrota aquí antes de cada turno)
        // if (IsPartyDefeated() || IsEnemyGroupDefeated()) { /* ... EndCombat ... */ return; }

        _currentCombatantIndex++;
        if (_currentCombatantIndex >= _combatants.Count)
        {
            _currentCombatantIndex = 0;
            Debug.Log("CombatManager: Nueva Ronda de Combate Iniciada.");
        }

        if (_combatants.Count == 0)
        {
            Debug.LogError("CombatManager: No hay combatientes para el siguiente turno.");
            EndCombat(false);
            return;
        }
        _activeCombatant = _combatants[_currentCombatantIndex];
        StartTurnForActiveCombatant();
    }

    private void StartTurnForActiveCombatant()
    {
        if (_activeCombatant == null)
        {
            Debug.LogError("CombatManager: _activeCombatant es nulo. Saltando turno.");
            NextTurn();
            return;
        }

        Debug.Log($"CombatManager: Iniciando turno para {_activeCombatant.GetName()} (HP: {_activeCombatant.GetCurrentHP()}/{_activeCombatant.GetMaxHP()})");

        if (_activeCombatant.isPlayerCharacter)
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(true);
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
        yield return new WaitForSeconds(1.0f);

        if (currentPlayerPartyData.Count > 0)
        {
            List<Character> livingPartyMembers = currentPlayerPartyData.Where(p => p.currentHP > 0).ToList();
            if (livingPartyMembers.Count > 0)
            {
                Character targetCharacter = livingPartyMembers[Random.Range(0, livingPartyMembers.Count)];
                Debug.Log($"{enemy.GetName()} ataca a {targetCharacter.characterName}!");
                // (FUTURO: Aplicar daño real)
                // targetCharacter.TakeDamage(enemy.enemyData.baseAttack); 
                // UpdatePartyStatusHUD(); 
            }
            else
            {
                Debug.Log($"{enemy.GetName()} no tiene objetivos vivos en la party.");
            }
        }

        Debug.Log($"CombatManager: Turno de {enemy.GetName()} finalizado.");
        NextTurn();
    }

    public void OnAttackButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter || isSelectingTargetForAttack) return;

        Debug.Log($"{_activeCombatant.GetName()} seleccionó ATACAR. Por favor, selecciona un objetivo.");
        isSelectingTargetForAttack = true;
        attackerForTargetSelection = _activeCombatant;

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
    }

    private void ExecuteAttack(Combatant attacker, Combatant target)
    {
        if (attacker == null || target == null) return;

        Debug.Log($"{attacker.GetName()} ataca a {target.GetName()}!");
        int damage = 0;
        if (attacker.isPlayerCharacter && attacker.characterData != null)
        {
            damage = attacker.characterData.Attack;
        }
        else if (!attacker.isPlayerCharacter && attacker.enemyData != null)
        {
            damage = attacker.enemyData.baseAttack;
        }
        target.TakeDamage(damage);

        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        NextTurn();
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
                    // Debug.Log("Clic detectado en: " + hit.collider.gameObject.name);
                    Combatant targetCombatant = _combatants.FirstOrDefault(c => c.combatSpriteGO == hit.collider.gameObject && !c.isPlayerCharacter);

                    if (targetCombatant != null)
                    {
                        Debug.Log("Objetivo seleccionado: " + targetCombatant.GetName());
                        ExecuteAttack(attackerForTargetSelection, targetCombatant);
                    }
                    // else Debug.Log("Clic en un objeto, pero no es un enemigo válido.");
                }
                // else Debug.Log("Clic en el vacío, no se seleccionó objetivo.");
            }

            if (!isSelectingTargetForAttack)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
                else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
            }
        }
    }
}
