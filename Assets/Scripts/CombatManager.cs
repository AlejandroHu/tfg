using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TopDown;

// Clase Combatant (CON LA CORRECCIÓN PARA LA SINCRONIZACIÓN DE HP)
public class Combatant
{
    public Character characterData;
    public EnemyData enemyData;
    public GameObject combatSpriteGO;
    public int speed;
    public bool isPlayerCharacter;
    // 'currentHP' y 'maxHP' en Combatant ahora sirven principalmente para enemigos 
    // o como un reflejo temporal para personajes si se decide gestionarlo así.
    // Para personajes, la fuente de verdad de currentHP/MaxHP es characterData.
    public int currentHP;
    public int maxHP;
    public bool isDefeated = false;
    public EnemyCombatStatusUI enemyStatusUI;
    public bool isDefending = false;
    private int defenseBonusWhileDefending = 5;

    public Combatant(Character character, GameObject spriteGO)
    {
        characterData = character;
        enemyData = null;
        combatSpriteGO = spriteGO;
        speed = character.Speed;
        isPlayerCharacter = true;
        // Inicializar desde el Character. Sus valores son la fuente de verdad.
        this.currentHP = character.currentHP;
        this.maxHP = character.MaxHP;
        isDefeated = (this.currentHP <= 0);
        isDefending = false;
        Debug.Log($"Combatant CREADO para JUGADOR: {GetName()}, HP Inicial: {this.currentHP}/{this.maxHP} (Desde Character: {character.currentHP}/{character.MaxHP})");
    }

    public Combatant(EnemyData enemy, GameObject spriteGO, EnemyCombatStatusUI statusUIScript = null)
    {
        characterData = null;
        enemyData = enemy;
        combatSpriteGO = spriteGO;
        speed = enemy.baseSpeed;
        isPlayerCharacter = false;
        currentHP = enemy.maxHP;
        maxHP = enemy.maxHP;
        isDefeated = false;
        enemyStatusUI = statusUIScript;
        isDefending = false;
    }

    public string GetName() { return isPlayerCharacter ? characterData.characterName : enemyData.enemyName; }

    public int GetCurrentHP()
    {
        // Para personajes, siempre leer del characterData que es la fuente de verdad.
        // Para enemigos, leer del currentHP local del Combatant.
        return isPlayerCharacter && characterData != null ? characterData.currentHP : this.currentHP;
    }

    public int GetMaxHP()
    {
        return isPlayerCharacter && characterData != null ? characterData.MaxHP : this.maxHP;
    }

    public int GetAttack() { return isPlayerCharacter ? (characterData != null ? characterData.Attack : 0) : (enemyData != null ? enemyData.baseAttack : 0); }
    public int GetDefense()
    {
        int baseDef = isPlayerCharacter ? (characterData != null ? characterData.Defense : 0) : (enemyData != null ? enemyData.baseDefense : 0);
        if (isDefending) return baseDef + defenseBonusWhileDefending;
        return baseDef;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDefeated) return;

        int hpBeforeDamage = GetCurrentHP(); // Usar GetCurrentHP() para leer el valor correcto

        int newHP = hpBeforeDamage - damageAmount;
        if (newHP < 0) newHP = 0;

        // Actualizar la fuente de verdad y la copia local
        if (isPlayerCharacter && characterData != null)
        {
            characterData.currentHP = newHP; // ACTUALIZA EL CHARACTER ORIGINAL
            this.currentHP = newHP;          // Sincroniza la copia local del Combatant
        }
        else
        {
            this.currentHP = newHP; // Para enemigos, Combatant.currentHP es la fuente
        }

        Debug.Log($"{GetName()} recibe {damageAmount} de daño. HP antes: {hpBeforeDamage}, HP después: {GetCurrentHP()}. Vida restante: {GetCurrentHP()}/{GetMaxHP()}");

        if (GetCurrentHP() <= 0)
        {
            isDefeated = true;
            Debug.Log($"{GetName()} ha sido derrotado!");
            if (combatSpriteGO != null) combatSpriteGO.SetActive(false);
            if (enemyStatusUI != null && !isPlayerCharacter) enemyStatusUI.gameObject.SetActive(false); // Ocultar solo si es enemigo
        }

        // Actualizar HUDs
        if (isPlayerCharacter && CombatManager.Instance != null)
        {
            CombatManager.Instance.UpdatePartyStatusHUD();
        }
        else if (!isPlayerCharacter && enemyStatusUI != null)
        {
            enemyStatusUI.UpdateHPDisplay(); // Actualizar su propia barra de HP
        }
    }

    public void StartDefending()
    {
        isDefending = true;
        Debug.Log($"{GetName()} adopta una postura defensiva. Defensa aumentada.");
    }

    public void StopDefending()
    {
        if (isDefending) Debug.Log($"{GetName()} ya no está en postura defensiva.");
        isDefending = false;
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
    private bool isSelectingSkill = false; // <-- DECLARACIÓN DE LA VARIABLE
    private bool isSelectingTargetForSkill = false;
    private AbilityData _selectedAbility = null;
    private bool isSelectingItem = false;
    private bool isSelectingTargetForItem = false;
    private ItemData _selectedItemData = null;


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
    [Header("HUD de Combate - Selección de Habilidades")]
    [SerializeField] private GameObject skillSelectionPanel;
    [SerializeField] private Transform skillListContainer;
    [SerializeField] private GameObject abilityListItemPrefab;
    [SerializeField] private Button closeSkillSelectionButton;
    [Header("HUD de Combate - Selección de Objetos")]
    [Tooltip("El Panel que contiene la lista de objetos usables en combate.")]
    [SerializeField] private GameObject itemSelectionPanel_Combat;
    [Tooltip("El Transform 'Content' del ScrollView donde se instanciarán los ítems de objeto.")]
    [SerializeField] private Transform itemListContainer_Combat;
    [Tooltip("Prefab para un ítem de objeto en la lista (debe tener CombatItemListItem_UI.cs).")]
    [SerializeField] private GameObject combatItemListItemPrefab;
    [Tooltip("Botón dentro del ItemSelectionPanel para volver al menú de acciones principal.")]
    [SerializeField] private Button closeItemSelectionButton;
    [Header("HUD de Combate - Estado de Enemigos")]
    [SerializeField] private GameObject enemyStatusUIPrefab;
    [SerializeField] private float enemyHPBarOffsetY = 0.7f;
    [Header("Capa de los Combatientes")]
    [SerializeField] private LayerMask combatantLayerMask;

    // --- NUEVA VARIABLE PARA LA PROBABILIDAD DE HUIR ---
    [Header("Mecánicas de Huida")]
    [Tooltip("Probabilidad de éxito al intentar huir (0.0 a 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float fleeSuccessChance = 0.7f; // 70% de probabilidad por defecto

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
    private List<AbilityListItem_UI> _currentSkillListUIs = new List<AbilityListItem_UI>();
    private List<CombatItemListItem_UI> _currentCombatItemListUIs = new List<CombatItemListItem_UI>();


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
        if (enemyStatusUIPrefab == null) Debug.LogError("CombatManager: 'enemyStatusUIPrefab' no asignado.", this);

        if (actionMenuPanel == null) Debug.LogError("CombatManager: 'actionMenuPanel' no asignado.", this);
        else actionMenuPanel.SetActive(false);

        if (skillSelectionPanel == null) Debug.LogError("CombatManager: 'skillSelectionPanel' no asignado.", this);
        else skillSelectionPanel.SetActive(false);
        if (skillListContainer == null) Debug.LogError("CombatManager: 'skillListContainer' no asignado.", this);
        if (abilityListItemPrefab == null) Debug.LogError("CombatManager: 'abilityListItemPrefab' no asignado.", this);

        // --- NUEVAS VALIDACIONES Y LISTENERS PARA OBJETOS ---
        if (itemSelectionPanel_Combat == null) Debug.LogError("CombatManager: 'itemSelectionPanel_Combat' no asignado.", this);
        else itemSelectionPanel_Combat.SetActive(false);
        if (itemListContainer_Combat == null) Debug.LogError("CombatManager: 'itemListContainer_Combat' no asignado.", this);
        if (combatItemListItemPrefab == null) Debug.LogError("CombatManager: 'combatItemListItemPrefab' no asignado.", this);
        if (closeItemSelectionButton != null) closeItemSelectionButton.onClick.AddListener(CloseItemSelectionPanel);
        else Debug.LogWarning("CombatManager: 'closeItemSelectionButton' (para panel de ítems) no asignado.", this);

        if (itemsButton != null) itemsButton.onClick.AddListener(OnItemsButtonClicked);
        else Debug.LogWarning("CombatManager: 'itemsButton' no asignado.", this);
        // --- FIN NUEVAS VALIDACIONES Y LISTENERS ---


        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClicked);
        if (defendButton != null) defendButton.onClick.AddListener(OnDefendButtonClicked);
        else Debug.LogWarning("CombatManager: 'defendButton' no asignado.", this);
        if (skillsButton != null) skillsButton.onClick.AddListener(OnSkillsButtonClicked);
        else Debug.LogWarning("CombatManager: 'skillsButton' no asignado.", this);
        if (fleeButton != null) fleeButton.onClick.AddListener(OnFleeButtonClicked); // --- AÑADIR LISTENER ---
        else Debug.LogWarning("CombatManager: 'fleeButton' no asignado.", this);
        if (closeSkillSelectionButton != null)
        {
            closeSkillSelectionButton.onClick.AddListener(CloseSkillSelectionPanel);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'closeSkillSelectionButton' no asignado en el panel de habilidades. No se podrá cerrar con ese botón.", this);
        }
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

        isCombatActive = true;
        this.currentPlayerPartyData = new List<Character>(playerParty);
        this.currentEnemyGroupData = new List<EnemyData>(enemyGroup);
        this._activeEncounter = encounterReference;
        StartCoroutine(CombatTransitionCoroutine(true));
    }

    public void EndCombat(bool playerWon)
    {
        if (!isCombatActive && !isSelectingTargetForAttack && !isSelectingTargetForSkill && _activeEncounter == null)
        {
            Debug.LogWarning("CombatManager: EndCombat llamado cuando el combate no parece estar activo o ya está terminando.");
        }
        isCombatActive = false;
        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        _selectedAbility = null;
        isSelectingItem = false; // Resetear estado de selección de ítem
        isSelectingTargetForItem = false;
        _selectedItemData = null;

        StartCoroutine(CombatTransitionCoroutine(false, playerWon));
    }

    private IEnumerator CombatTransitionCoroutine(bool startingCombat, bool playerWon = false)
    {
        if (playerMovementController != null) playerMovementController.SetCanMove(false);

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
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false); // Asegurar que esté oculto
            if (explorationRootGameObject != null) explorationRootGameObject.SetActive(false);
            if (currentCombatArenaGameObject != null) currentCombatArenaGameObject.SetActive(true);
            if (playerMovementController != null && playerMovementController.TryGetComponent<SpriteRenderer>(out SpriteRenderer pr)) pr.enabled = false;

            SetupCombatantsAndPrepareTurnOrder();
            PopulatePartyStatusUI();

            if (combatCamera != null) combatCamera.Priority = 11;
            if (explorationCamera != null) explorationCamera.Priority = 9;
            if (combatScreenUIPanel != null) combatScreenUIPanel.SetActive(true);
        }
        else
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
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
        }
        else
        {
            Debug.Log("CombatManager: Combate listo. Iniciando primer turno.");
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
                    // --- AÑADIR COLLIDER AL PERSONAJE DE LA PARTY ---
                    BoxCollider2D partyCol = partyMemberSpriteGO.AddComponent<BoxCollider2D>();
                    // partyCol.isTrigger = true; // Opcional, para raycast no es estrictamente necesario que sea trigger si está en la LayerMask
                    if (sr.sprite != null)
                    {
                        // Ajustar tamaño al 80% del sprite, por ejemplo, o al tamaño que consideres adecuado
                        partyCol.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);
                    }
                    else
                    {
                        partyCol.size = new Vector2(0.5f, 0.5f); // Tamaño por defecto si no hay sprite
                    }
                    // (Opcional pero RECOMENDADO) Asignar una capa específica para los miembros de la party
                    // partyMemberSpriteGO.layer = LayerMask.NameToLayer("PlayerPartyInCombat"); // Crea esta capa en Unity
                    // --- FIN COLLIDER PARTY ---
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
                    if (sr.sprite != null) col.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);
                    else col.size = new Vector2(0.5f, 0.5f);
                    enemySpriteGO.layer = LayerMask.NameToLayer("EnemiesInCombat");
                    _enemyCombatSpriteGOs.Add(enemySpriteGO);
                    EnemyCombatStatusUI statusUIInstance = null;
                    if (enemyStatusUIPrefab != null)
                    {
                        GameObject enemyStatusUIGO = Instantiate(enemyStatusUIPrefab, enemySpriteGO.transform);
                        enemyStatusUIGO.transform.localPosition = new Vector3(0, enemyHPBarOffsetY, 0);
                        statusUIInstance = enemyStatusUIGO.GetComponent<EnemyCombatStatusUI>();
                        if (statusUIInstance == null) Debug.LogError("El prefab enemyStatusUIPrefab no tiene el script EnemyCombatStatusUI.", enemyStatusUIPrefab);
                    }
                    Combatant enemyCombatant = new Combatant(currentEnemyGroupData[i], enemySpriteGO, statusUIInstance);
                    _combatants.Add(enemyCombatant);
                    if (statusUIInstance != null) statusUIInstance.SetupEnemyStatus(enemyCombatant);
                }
            }
        }
        _combatants = _combatants.OrderByDescending(c => c.speed).ToList();
        _currentCombatantIndex = -1;
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
        if (CheckCombatEndConditions()) return;

        _currentCombatantIndex++;
        if (_currentCombatantIndex >= _combatants.Count)
        {
            _currentCombatantIndex = 0;
            Debug.Log("CombatManager: ----- Nueva Ronda de Combate Iniciada -----");
            foreach (Combatant combatant in _combatants)
            {
                if (combatant.isDefending) combatant.StopDefending();
            }
            UpdatePartyStatusHUD();
        }

        if (_combatants.Count == 0) { EndCombat(false); return; }
        _activeCombatant = _combatants[_currentCombatantIndex];

        if (_activeCombatant.isDefeated)
        {
            NextTurn();
            return;
        }
        StartTurnForActiveCombatant();
    }

    private void StartTurnForActiveCombatant()
    {
        if (_activeCombatant == null || _activeCombatant.isDefeated) { NextTurn(); return; }
        Debug.Log($"CombatManager: Iniciando turno para {_activeCombatant.GetName()} (HP: {_activeCombatant.GetCurrentHP()}/{_activeCombatant.GetMaxHP()}, Defensa: {_activeCombatant.GetDefense()}, Defendiendo: {_activeCombatant.isDefending})");

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
        yield return new WaitForSeconds(1.5f);

        if (enemy.isDefeated) { NextTurn(); yield break; }
        if (enemy.isDefending) enemy.StopDefending();

        List<Combatant> livingPlayerCombatants = _combatants.Where(c => c.isPlayerCharacter && !c.isDefeated).ToList();

        if (livingPlayerCombatants.Count > 0)
        {
            Combatant target = livingPlayerCombatants[Random.Range(0, livingPlayerCombatants.Count)];
            Debug.Log($"{enemy.GetName()} ataca a {target.GetName()}! (Defensa del objetivo: {target.GetDefense()})");

            int damage = Mathf.Max(1, enemy.GetAttack() - target.GetDefense());
            target.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"{enemy.GetName()} no tiene objetivos vivos en la party.");
        }

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

    public void OnDefendButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter || isSelectingTargetForAttack) return;
        _activeCombatant.StartDefending();
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.3f));
    }

    public void OnSkillsButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill) return;
        Debug.Log($"{_activeCombatant.GetName()} seleccionó HABILIDADES.");
        isSelectingSkill = true;
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        PopulateSkillListForActiveCombatant();
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(true);
    }

    private void PopulateSkillListForActiveCombatant()
    {
        if (skillListContainer == null || abilityListItemPrefab == null || _activeCombatant == null || _activeCombatant.characterData == null)
        {
            if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
            return;
        }
        foreach (Transform child in skillListContainer) Destroy(child.gameObject);
        _currentSkillListUIs.Clear();
        List<AbilityData> knownAbilities = _activeCombatant.characterData.knownAbilities;
        if (knownAbilities == null || knownAbilities.Count == 0) return;

        foreach (AbilityData ability in knownAbilities)
        {
            if (ability == null) continue;
            GameObject listItemGO = Instantiate(abilityListItemPrefab, skillListContainer);
            AbilityListItem_UI listItemUI = listItemGO.GetComponent<AbilityListItem_UI>();
            if (listItemUI != null)
            {
                listItemUI.SetupAbilityItem(ability, this.OnSkillSelectedFromList);
                _currentSkillListUIs.Add(listItemUI);
            }
            else Destroy(listItemGO);
        }
    }

    public void OnSkillSelectedFromList(AbilityData ability)
    {
        if (!isSelectingSkill || ability == null || _activeCombatant == null) return;
        _selectedAbility = ability;
        if (_activeCombatant.characterData.currentMP < _selectedAbility.mpCost)
        {
            CloseSkillSelectionPanel();
            return;
        }
        isSelectingSkill = false;
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        switch (_selectedAbility.targetType)
        {
            case AbilityTargetType.Self:
            case AbilityTargetType.AllAllies:
            case AbilityTargetType.AllEnemies:
                ExecuteSkill(_activeCombatant, null, _selectedAbility);
                break;
            case AbilityTargetType.SingleAlly:
            case AbilityTargetType.SingleEnemy:
                isSelectingTargetForSkill = true;
                attackerForTargetSelection = _activeCombatant;
                Debug.Log($"Por favor, selecciona un objetivo para la habilidad: {_selectedAbility.abilityName} ({_selectedAbility.targetType})");
                break;
            default:
                NextTurn();
                break;
        }
    }

    public void CloseSkillSelectionPanel()
    {
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        _selectedAbility = null;
        attackerForTargetSelection = null; // Asegurarse de resetear esto también por si acaso
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        if (actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
        }
    }

    private void ExecuteAttack(Combatant attacker, Combatant target)
    {
        if (attacker == null || target == null || target.isDefeated)
        {
            isSelectingTargetForAttack = false;
            if (attacker != null && attacker.isPlayerCharacter && actionMenuPanel != null) actionMenuPanel.SetActive(true);
            return;
        }
        Debug.Log($"{attacker.GetName()} ataca a {target.GetName()}! (Defensa del objetivo: {target.GetDefense()})");

        int damage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
        target.TakeDamage(damage); // <--- ESTA LLAMADA ES LA IMPORTANTE

        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.5f));
    }

    private void ExecuteSkill(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        if (attacker == null || skill == null) return;
        if (attacker.isPlayerCharacter == false) { NextTurn(); return; }

        Debug.Log($"{attacker.GetName()} usa la habilidad '{skill.abilityName}'" + (directTarget != null ? $" sobre {directTarget.GetName()}" : ""));

        if (skill.mpCost > 0)
        {
            if (!attacker.characterData.SpendMana(skill.mpCost))
            {
                NextTurn();
                return;
            }
            UpdatePartyStatusHUD();
        }

        List<Combatant> actualTargets = new List<Combatant>();
        switch (skill.targetType)
        {
            case AbilityTargetType.Self:
                if (attacker != null && !attacker.isDefeated) actualTargets.Add(attacker);
                break;
            case AbilityTargetType.SingleAlly:
                if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                break;
            case AbilityTargetType.AllAllies:
                actualTargets.AddRange(_combatants.Where(c => c.isPlayerCharacter && !c.isDefeated));
                break;
            case AbilityTargetType.SingleEnemy:
                if (directTarget != null && !directTarget.isPlayerCharacter && !directTarget.isDefeated) actualTargets.Add(directTarget);
                break;
            case AbilityTargetType.AllEnemies:
                actualTargets.AddRange(_combatants.Where(c => !c.isPlayerCharacter && !c.isDefeated));
                break;
        }

        if (actualTargets.Count == 0)
        {
            Debug.LogWarning($"No se encontraron objetivos válidos para la habilidad {skill.abilityName}.");
            NextTurn();
            return;
        }

        Debug.Log($"Ejecutando efecto de {skill.abilityName} sobre {actualTargets.Count} objetivo(s). Tipo: {skill.effectType}, Potencia: {skill.power}");
        foreach (Combatant t in actualTargets)
        {
            if (skill.effectType == AbilityEffectType.Damage)
            {
                int damage = Mathf.Max(1, (int)skill.power + attacker.GetAttack() / 2 - t.GetDefense());
                t.TakeDamage(damage);
            }
            else if (skill.effectType == AbilityEffectType.Heal && t.isPlayerCharacter && t.characterData != null)
            {
                t.characterData.Heal((int)skill.power);
                UpdatePartyStatusHUD();
            }
        }

        isSelectingTargetForSkill = false;
        _selectedAbility = null;
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
        bool allEnemiesDefeated = _combatants.Where(c => !c.isPlayerCharacter && c.enemyData != null).All(e => e.isDefeated);
        if (allEnemiesDefeated && _combatants.Any(c => !c.isPlayerCharacter && c.enemyData != null))
        {
            EndCombat(true);
            return true;
        }
        bool allPlayersDefeated = _combatants.Where(c => c.isPlayerCharacter && c.characterData != null).All(p => p.isDefeated);
        if (allPlayersDefeated && _combatants.Any(c => c.isPlayerCharacter && c.characterData != null))
        {
            EndCombat(false);
            return true;
        }
        return false;
    }

    // --- MANEJADORES Y LÓGICA PARA LA ACCIÓN "OBJETOS" ---
    public void OnItemsButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill ||
            isSelectingItem || isSelectingTargetForItem) // Comprobar todos los estados de selección
        {
            return;
        }

        Debug.Log($"{_activeCombatant.GetName()} seleccionó OBJETOS.");
        isSelectingItem = true;

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        PopulateCombatItemList();
        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(true);
    }

    private void PopulateCombatItemList()
    {
        if (itemListContainer_Combat == null || combatItemListItemPrefab == null || PlayerInventory.Instance == null)
        {
            Debug.LogError("CombatManager: No se puede poblar la lista de objetos. Faltan referencias.");
            if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
            return;
        }

        foreach (Transform child in itemListContainer_Combat) Destroy(child.gameObject);
        _currentCombatItemListUIs.Clear();

        // Filtrar por objetos que sean consumibles y tengan un efecto de uso definido
        var usableItems = PlayerInventory.Instance.inventorySlots
            .Where(slot => slot.item != null && slot.item.isConsumable &&
                           (slot.item.hpToRestore > 0 || slot.item.mpToRestore > 0 /*|| otros efectos de itemData.Use()*/ ))
            .ToList();

        if (usableItems.Count == 0)
        {
            Debug.Log("No hay objetos usables en combate en el inventario.");
            // (FUTURO: Mostrar mensaje en la UI de objetos "Sin objetos usables")
            // Considerar llamar a CloseItemSelectionPanel() para volver al menú de acción si está vacío
            // CloseItemSelectionPanel(); 
            return;
        }

        foreach (InventorySlot invSlot in usableItems)
        {
            GameObject listItemGO = Instantiate(combatItemListItemPrefab, itemListContainer_Combat);
            CombatItemListItem_UI listItemUI = listItemGO.GetComponent<CombatItemListItem_UI>();
            if (listItemUI != null)
            {
                listItemUI.SetupItem(invSlot.item, invSlot.quantity, this.OnCombatItemSelected);
                _currentCombatItemListUIs.Add(listItemUI);
            }
            else
            {
                Debug.LogError("CombatManager: El prefab 'combatItemListItemPrefab' no tiene el componente CombatItemListItem_UI.", this);
                Destroy(listItemGO);
            }
        }
    }

    public void OnCombatItemSelected(ItemData selectedItem)
    {
        if (!isSelectingItem || selectedItem == null || _activeCombatant == null || _activeCombatant.characterData == null) return;

        Debug.Log($"{_activeCombatant.GetName()} seleccionó el objeto: {selectedItem.itemName}");
        _selectedItemData = selectedItem;
        isSelectingItem = false;
        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);

        // Determinar si el objeto necesita selección de objetivo
        // Por ahora, asumimos que los consumibles de HP/MP se pueden usar en aliados/self
        if (selectedItem.itemType == ItemType.Consumable && (selectedItem.hpToRestore > 0 || selectedItem.mpToRestore > 0))
        {
            isSelectingTargetForItem = true;
            attackerForTargetSelection = _activeCombatant; // El personaje que usa el objeto
            Debug.Log($"Por favor, selecciona un objetivo para el objeto: {selectedItem.itemName} (Aliado).");
        }
        // (Añadir 'else if' para objetos de ataque a enemigos si los tienes, ej: bombas)
        // else if (selectedItem.itemType == ItemType.Bomb_Damage_Enemy) { 
        //     isSelectingTargetForItem = true; 
        //     attackerForTargetSelection = _activeCombatant;
        //     Debug.Log($"Por favor, selecciona un objetivo para el objeto: {selectedItem.itemName} (Enemigo).");
        // }
        else
        {
            // Si el objeto no necesita objetivo explícito o se usa sobre sí mismo por defecto
            ExecuteItem(_activeCombatant, _activeCombatant, _selectedItemData);
        }
    }

    public void CloseItemSelectionPanel()
    {
        isSelectingItem = false;
        isSelectingTargetForItem = false;
        _selectedItemData = null;
        // attackerForTargetSelection no se resetea aquí necesariamente, podría ser útil si se cancela la selección de objetivo

        if (itemSelectionPanel_Combat != null) itemSelectionPanel_Combat.SetActive(false);
        if (actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
        }
    }

    private void ExecuteItem(Combatant caster, Combatant directTarget, ItemData item)
    {
        if (caster == null || item == null)
        {
            Debug.LogError("ExecuteItem: Lanzador u objeto nulos.");
            ResetSelectionStatesAndPassTurn();
            return;
        }
        if (caster.isPlayerCharacter == false || caster.characterData == null)
        {
            Debug.LogWarning("ExecuteItem: Solo los jugadores pueden usar objetos desde este flujo por ahora.");
            ResetSelectionStatesAndPassTurn();
            return;
        }

        Combatant actualTarget = null;

        // Lógica de objetivo para ítems (simplificada)
        if (item.itemType == ItemType.Consumable && (item.hpToRestore > 0 || item.mpToRestore > 0))
        {
            if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated)
            {
                actualTarget = directTarget;
            }
            else if (caster != null && !caster.isDefeated)
            {
                actualTarget = caster;
                Debug.Log($"El objeto {item.itemName} se usará sobre {caster.GetName()} (objetivo por defecto).");
            }
        }
        // (Añadir lógica para otros tipos de ítems y sus objetivos)


        if (actualTarget == null || actualTarget.characterData == null)
        {
            Debug.LogWarning($"No se pudo aplicar {item.itemName}, objetivo ({directTarget?.GetName()}) no válido o no es personaje de la party.");
            ResetSelectionStatesAndPassTurn(true); // Reabrir menú de acciones
            return;
        }

        Debug.Log($"{caster.GetName()} usa el objeto '{item.itemName}' sobre {actualTarget.GetName()}");

        bool itemUsedSuccessfully = item.Use(actualTarget.characterData);

        if (itemUsedSuccessfully)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveItem(item, 1);
            }
            UpdatePartyStatusHUD();
        }
        else
        {
            Debug.LogWarning($"{item.itemName} no pudo ser usado sobre {actualTarget.GetName()} (ej: HP/MP ya al máximo).");
            // No consumir el turno si el uso no fue "efectivo"
            ResetSelectionStatesAndPassTurn(true); // Reabrir menú de acciones
            return;
        }

        ResetSelectionStatesAndPassTurn();
    }

    private void ResetSelectionStatesAndPassTurn(bool reOpenActionMenu = false)
    {
        isSelectingItem = false;
        isSelectingTargetForItem = false;
        isSelectingSkill = false;
        isSelectingTargetForSkill = false;
        isSelectingTargetForAttack = false;
        _selectedItemData = null;
        _selectedAbility = null;
        attackerForTargetSelection = null;

        if (reOpenActionMenu && actionMenuPanel != null && _activeCombatant != null && _activeCombatant.isPlayerCharacter)
        {
            actionMenuPanel.SetActive(true);
        }
        else
        {
            StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.5f));
        }
    }



    void Update()
    {
        if (isCombatActive)
        {
            if (isSelectingTargetForAttack && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("EnemiesInCombat"));

                if (hit.collider != null)
                {
                    Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject && !c.isPlayerCharacter);
                    if (targetCombatant != null)
                    {
                        ExecuteAttack(attackerForTargetSelection, targetCombatant);
                    }
                }
            }
            else if (isSelectingTargetForSkill && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);

                if (hit.collider != null)
                {
                    Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject);

                    if (targetCombatant != null && _selectedAbility != null)
                    {
                        bool isValidTargetType = false;
                        switch (_selectedAbility.targetType)
                        {
                            case AbilityTargetType.SingleEnemy:
                                if (!targetCombatant.isPlayerCharacter) isValidTargetType = true;
                                break;
                            case AbilityTargetType.SingleAlly:
                                if (targetCombatant.isPlayerCharacter) isValidTargetType = true;
                                break;
                        }

                        if (isValidTargetType)
                        {
                            ExecuteSkill(attackerForTargetSelection, targetCombatant, _selectedAbility);
                        }
                        else
                        {
                            Debug.LogWarning($"CombatManager: Objetivo '{targetCombatant.GetName()}' NO es válido para la habilidad '{_selectedAbility.abilityName}' (Tipo esperado: {_selectedAbility.targetType}, Tipo real: {(targetCombatant.isPlayerCharacter ? "Aliado" : "Enemigo")}).");
                            CloseSkillSelectionPanel();
                        }
                    }
                }
            }
            else if (isSelectingTargetForItem && Input.GetMouseButtonDown(0))
            {
                if (Camera.main == null) { Debug.LogError("Camera.main es NULL."); return; }
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);

                if (hit.collider != null)
                {
                    Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject);

                    if (targetCombatant != null && _selectedItemData != null)
                    {
                        bool isValidTarget = false;
                        if (_selectedItemData.itemType == ItemType.Consumable && (_selectedItemData.hpToRestore > 0 || _selectedItemData.mpToRestore > 0))
                        {
                            if (targetCombatant.isPlayerCharacter) isValidTarget = true;
                        }
                        // (Añadir validación para otros tipos de ítems)

                        if (isValidTarget)
                        {
                            ExecuteItem(attackerForTargetSelection, targetCombatant, _selectedItemData);
                        }
                        else
                        {
                            Debug.LogWarning($"Objetivo '{targetCombatant.GetName()}' NO es válido para el objeto '{_selectedItemData.itemName}'.");
                            CloseItemSelectionPanel();
                        }
                    }
                }
            }
            else if (!isSelectingTargetForAttack && !isSelectingTargetForSkill && !isSelectingTargetForItem)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
                else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
            }
        }
    }
    public void OnFleeButtonClicked()
    {
        if (!isCombatActive || _activeCombatant == null || !_activeCombatant.isPlayerCharacter ||
            isSelectingTargetForAttack || isSelectingSkill || isSelectingTargetForSkill ||
            isSelectingItem || isSelectingTargetForItem)
        {
            return; // No hacer nada si no es el momento adecuado
        }

        Debug.Log($"{_activeCombatant.GetName()} intenta HUIR.");

        if (actionMenuPanel != null)
        {
            actionMenuPanel.SetActive(false); // Ocultar menú inmediatamente
        }

        // Comprobar si se puede huir de este encuentro específico
        if (_activeEncounter != null && !_activeEncounter.canFleeFromThisEncounter)
        {
            Debug.Log("¡No se puede huir de este combate (Jefe)!");
            // (FUTURO: Mostrar mensaje en UI "¡No puedes huir de este enemigo!")
            // El personaje pierde el turno
            StartCoroutine(ShowMessageAndEndPlayerTurn("¡No puedes huir!", 1.5f));
            return;
        }

        // Calcular si la huida tiene éxito
        if (Random.value < fleeSuccessChance) // Random.value devuelve un float entre 0.0 (inclusive) y 1.0 (inclusive)
        {
            Debug.Log("¡Huida exitosa!");
            // (FUTURO: Mostrar mensaje en UI "¡Escapaste con éxito!")
            // Podrías añadir una pequeña pausa antes de llamar a EndCombat
            // StartCoroutine(DelayedEndCombat(false, 0.5f)); 
            EndCombat(false); // Terminar el combate, el jugador no "gana"
        }
        else
        {
            Debug.Log("¡La huida falló!");
            // (FUTURO: Mostrar mensaje en UI "La huida falló...")
            // El personaje pierde el turno
            StartCoroutine(ShowMessageAndEndPlayerTurn("¡La huida falló!", 1.5f));
        }
    }
    private IEnumerator ShowMessageAndEndPlayerTurn(string message, float delay)
    {
        // (FUTURO: Aquí mostrarías 'message' en una UI temporal de feedback)
        Debug.Log("Mensaje de Combate: " + message);
        yield return new WaitForSeconds(delay);
        NextTurn();
    }
    // (Opcional) Corrutina para un pequeño delay antes de terminar el combate al huir
    // private IEnumerator DelayedEndCombat(bool playerWon, float delay)
    // {
    //    yield return new WaitForSeconds(delay);
    //    EndCombat(playerWon);
    // }

}
