using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using TopDown;
using TMPro; // Necesario para TextMeshProUGUI en ShowFloatingText


// Clase Combatant (CON LA CORRECCIÓN PARA LA SINCRONIZACIÓN DE HP)
public class Combatant
{
    public Character characterData;
    public EnemyData enemyData;
    public GameObject combatSpriteGO;
    public int speed;
    public bool isPlayerCharacter;
    public Animator animator; // Referencia al Animator del sprite de combate
    public int currentHP;
    public int maxHP;
    public bool isDefeated = false;
    public EnemyCombatStatusUI enemyStatusUI;
    public bool isDefending = false;
    private int defenseBonusWhileDefending = 5;
    private SpriteRenderer _spriteRenderer; // Guardar referencia para cambiar color
    private Color _originalSpriteColor;    // Para restaurar el color original

    public Combatant(Character character, GameObject spriteGO)
    {
        characterData = character;
        enemyData = null;
        combatSpriteGO = spriteGO;
        if (spriteGO != null)
        {
            animator = spriteGO.GetComponent<Animator>();
            _spriteRenderer = spriteGO.GetComponent<SpriteRenderer>(); // Obtener SpriteRenderer
            if (_spriteRenderer != null) _originalSpriteColor = _spriteRenderer.color; // Guardar color original
            else Debug.LogWarning($"Combatant {character.characterName} no tiene SpriteRenderer en su combatSpriteGO.");
        }
        speed = character.Speed;
        isPlayerCharacter = true;
        // Inicializar desde el Character. Sus valores son la fuente de verdad.
        this.currentHP = character.currentHP;
        this.maxHP = character.MaxHP;
        isDefeated = (this.currentHP <= 0);
        if (isDefeated && combatSpriteGO != null) combatSpriteGO.SetActive(false); // Desactivar si empieza derrotado
        isDefending = false;
        Debug.Log($"Combatant CREADO para JUGADOR: {GetName()}, HP Inicial: {this.currentHP}/{this.maxHP} (Desde Character: {character.currentHP}/{character.MaxHP})");
    }

    public Combatant(EnemyData enemy, GameObject spriteGO, EnemyCombatStatusUI statusUIScript = null)
    {
        characterData = null;
        enemyData = enemy;
        combatSpriteGO = spriteGO;
        if (spriteGO != null)
        {
            animator = spriteGO.GetComponent<Animator>();
            _spriteRenderer = spriteGO.GetComponent<SpriteRenderer>(); // Obtener SpriteRenderer
            if (_spriteRenderer != null) _originalSpriteColor = _spriteRenderer.color; // Guardar color original
            else Debug.LogWarning($"Combatant {enemy.enemyName} no tiene SpriteRenderer en su combatSpriteGO.");
        }
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
        if (isDefeated || damageAmount <= 0) return;
        // --- REPRODUCIR SONIDO DE RECIBIR DAÑO ---
        // Se reproduce antes de cualquier espera o efecto visual para que sea inmediato
        if (CombatManager.Instance != null && damageAmount > 0)
        {
            AudioClip hitSound = isPlayerCharacter ? characterData.takeHitSound : enemyData.takeHitSound;
            CombatManager.Instance.PlaySoundEffect(hitSound);
        }

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
        // Mostrar texto flotante para el daño
        if (CombatManager.Instance != null && combatSpriteGO != null)
        {
            if (damageAmount > 0) CombatManager.Instance.ShowFloatingText("-" + damageAmount.ToString(), combatSpriteGO.transform.position, Color.red, this);
            if (animator != null) animator.SetTrigger("HitTrigger");
            if (_spriteRenderer != null && damageAmount > 0) CombatManager.Instance.StartCoroutine(FlashFeedbackCoroutine(_spriteRenderer, Color.red, _originalSpriteColor, 0.1f, 2));
        }
        Debug.Log($"{GetName()} recibe {damageAmount} de daño. HP restante: {GetCurrentHP()}/{GetMaxHP()}");

        if (GetCurrentHP() <= 0)
        {
            HandleDefeat(); // Llamar al nuevo método para manejar la derrota
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

    private void HandleDefeat()
    {
        if (isDefeated) return;
        isDefeated = true;
        Debug.Log($"{GetName()} ha sido derrotado!");
        // --- REPRODUCIR SONIDO DE DERROTA ---
        if (CombatManager.Instance != null)
        {
            AudioClip defeatSound = isPlayerCharacter ? null : enemyData.defeatSound; // Asumimos que solo enemigos tienen sonido de derrota
            CombatManager.Instance.PlaySoundEffect(defeatSound);
        }
        // --- FIN SONIDO ---
        if (enemyStatusUI != null && !isPlayerCharacter) enemyStatusUI.gameObject.SetActive(false);
        if (combatSpriteGO != null && CombatManager.Instance != null)
        {
            float preFadeDelay = 0f;
            string defeatTrigger = "DefeatTrigger"; // Trigger por defecto para derrota

            if (isPlayerCharacter)
            {
                if (animator != null) animator.SetTrigger("HitTrigger"); // Jugador usa HitTrigger al ser derrotado
                preFadeDelay = 0.3f;
            }
            else
            {
                if (animator != null)
                {
                    animator.SetTrigger(defeatTrigger);
                    preFadeDelay = CombatManager.Instance.GetEnemyDefeatAnimDuration();
                }
            }
            CombatManager.Instance.StartCoroutine(DefeatSequenceCoroutine(preFadeDelay, 0.5f));
        }
    }

    private IEnumerator DefeatSequenceCoroutine(float preFadeDelay, float fadeDuration)
    {
        if (preFadeDelay > 0.01f) yield return new WaitForSeconds(preFadeDelay);
        if (_spriteRenderer != null)
        {
            float timer = 0f; Color startColor = _spriteRenderer.color;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, timer / fadeDuration));
                yield return null;
            }
            _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }
        if (combatSpriteGO != null) combatSpriteGO.SetActive(false);
        yield break;
    }

    public static IEnumerator FlashFeedbackCoroutine(SpriteRenderer sr, Color flashColor, Color originalColor, float flashDuration, int flashCount)
    {
        if (sr == null) yield break;

        for (int i = 0; i < flashCount; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
            if (i < flashCount - 1) // No esperar después del último flash si se restaura inmediatamente
            {
                yield return new WaitForSeconds(flashDuration);
            }
        }
        // Asegurarse de que el color final sea el original
        sr.color = originalColor;
    }


    public void ApplyHeal(int healAmount)
    {
        if (isDefeated || healAmount <= 0) return;

        int hpBeforeHeal = GetCurrentHP();
        int actualHealedAmount = 0;

        if (isPlayerCharacter && characterData != null)
        {
            if (characterData.Heal(healAmount)) // Character.Heal devuelve true si curó algo
            {
                actualHealedAmount = characterData.currentHP - hpBeforeHeal;
                this.currentHP = characterData.currentHP; // Sincronizar
            }
        }
        else // Para enemigos (o si characterData fuera null)
        {
            int maxHealable = GetMaxHP() - hpBeforeHeal;
            if (maxHealable > 0)
            {
                actualHealedAmount = Mathf.Min(healAmount, maxHealable);
                this.currentHP += actualHealedAmount;
                if (this.currentHP > GetMaxHP()) this.currentHP = GetMaxHP();
            }
        }

        if (actualHealedAmount > 0)
        {
            if (CombatManager.Instance != null && combatSpriteGO != null)
            {
                CombatManager.Instance.ShowFloatingText("+" + actualHealedAmount.ToString(), combatSpriteGO.transform.position, Color.green, this);
                if (_spriteRenderer != null) // Parpadeo verde para curación
                {
                    CombatManager.Instance.StartCoroutine(FlashFeedbackCoroutine(_spriteRenderer, Color.green, _originalSpriteColor, 0.1f, 2));
                }
            }
            Debug.Log($"{GetName()} se cura {actualHealedAmount}. HP actual: {GetCurrentHP()}/{GetMaxHP()}");
        }
        else
        {
            Debug.Log($"{GetName()} intentó curarse {healAmount} pero no tuvo efecto (HP ya al máximo o no se pudo curar).");
        }


        if (isPlayerCharacter && CombatManager.Instance != null) CombatManager.Instance.UpdatePartyStatusHUD();
        else if (!isPlayerCharacter && enemyStatusUI != null) enemyStatusUI.UpdateHPDisplay();
    }

    public void ApplyManaRestore(int manaAmount)
    {
        if (isDefeated || manaAmount <= 0) return;

        int actualRestoredAmount = 0;

        if (isPlayerCharacter && characterData != null)
        {
            int mpBeforeRestore = characterData.currentMP;
            if (characterData.RestoreMana(manaAmount)) // Character.RestoreMana devuelve true si restauró algo
            {
                actualRestoredAmount = characterData.currentMP - mpBeforeRestore;
            }
        }
        // (Podrías añadir lógica para enemigos si usan MP y pueden restaurarlo)

        if (actualRestoredAmount > 0)
        {
            if (CombatManager.Instance != null && combatSpriteGO != null)
            {
                CombatManager.Instance.ShowFloatingText("+" + actualRestoredAmount.ToString() + " MP", combatSpriteGO.transform.position, Color.blue, this);
            }
            Debug.Log($"{GetName()} restaura {actualRestoredAmount} MP. MP actual: {characterData.currentMP}/{characterData.MaxMP}");
            CombatManager.Instance.UpdatePartyStatusHUD(); // Asume que esto actualiza el MP en la UI
        }
        else
        {
            Debug.Log($"{GetName()} intentó restaurar {manaAmount} MP pero no tuvo efecto.");
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
    [Header("Audio")] // --- NUEVA SECCIÓN ---
    [Tooltip("AudioSource para reproducir los efectos de sonido del combate.")]
    [SerializeField] private AudioSource sfxAudioSource;
    // ... (Variables [SerializeField] existentes) ...
    [Header("HUD de Combate - Información de Ronda/Turno")] // --- NUEVA SECCIÓN ---
    [Tooltip("Elemento TextMeshProUGUI para mostrar el título de la ronda (ej: 'RONDA 1').")]
    [SerializeField] private TextMeshProUGUI roundTitleText;
    [Tooltip("Duración en segundos que el título de la ronda permanecerá visible.")]
    [SerializeField] private float roundTitleDisplayDuration = 1.5f;
    // --- FIN NUEVA SECCIÓN ---

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

    // --- NUEVAS VARIABLES PARA TEXTO FLOTANTE ---
    [Header("Feedback de Combate")]
    [Tooltip("Prefab para el texto flotante de daño/curación (debe tener FloatingCombatText.cs).")]
    [SerializeField] private GameObject floatingTextPrefab;
    [Tooltip("Transform padre para los textos flotantes. Si es null, se instanciarán como hijos del combatiente (para World Space Canvas en el prefab del texto). Si se asigna un Canvas ScreenSpace, se intentará convertir la posición.")]
    [SerializeField] private Transform floatingTextCanvasTransform;
    [SerializeField] private float floatingTextDefaultFontSize = 20f;
    [SerializeField] private float floatingTextYOffset = 0.6f;
    // --- NUEVO: Prefab para el VFX de golpe directo ---
    [Tooltip("Prefab del efecto visual (slash/hit) para ataques directos y habilidades sin proyectil.")]
    [SerializeField] private GameObject directHitVFXPrefab;
    [Tooltip("Offset Y para el VFX de golpe directo sobre el pivote del objetivo.")]
    [SerializeField] private float directHitVFX_Y_Offset = 0.3f; // Ajusta según el tamaño de tus sprites y el pivote del VFX
                                                                 // --- FIN NUEVO ---

    [Header("Animaciones de Combate")]
    // --- ELIMINADAS LAS REFERENCIAS GENÉRICAS DE ANIMATOR CONTROLLER ---
    // [SerializeField] private RuntimeAnimatorController playerCombatAnimatorController; 
    // [SerializeField] private RuntimeAnimatorController defaultEnemyCombatAnimatorController;
    // --- REINTRODUCIDAS LAS REFERENCIAS GENÉRICAS COMO FALLBACK ---
    [Tooltip("Animator Controller por defecto para los personajes de la party si su Character.cs no tiene uno asignado.")]
    [SerializeField] private RuntimeAnimatorController playerFallbackAnimatorController;
    [Tooltip("Animator Controller por defecto para los enemigos si su EnemyData.cs no tiene uno asignado.")]
    [SerializeField] private RuntimeAnimatorController defaultEnemyFallbackAnimatorController;
    // --- FIN REINTRODUCCIÓN ---
    [Tooltip("Duración por defecto para animaciones de ataque/habilidad del jugador si la habilidad no especifica una o el personaje no tiene controller específico.")]
    [SerializeField] private float playerGenericAnimationDuration = 0.6f;
    [Tooltip("Duración de la animación de ataque del enemigo si su EnemyData no especifica una o no tiene controller.")]
    [SerializeField] private float enemyAttackAnimationDuration = 0.8f;
    [SerializeField] private float enemyDefeatAnimationBaseDuration = 1.0f;
    // --- NUEVA VARIABLE PARA DELAY DE IMPACTO DE PROYECTILES DE HABILIDAD ---
    [Tooltip("Tiempo de espera adicional DESPUÉS de la animación del lanzador para que los proyectiles de habilidad impacten y sus efectos se procesen.")]
    [SerializeField] private float skillProjectileImpactDelay = 0.75f; // Ajusta este valor según sea necesario



    // --- FIN NUEVO ---

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
    private int _currentRoundNumber = 0; // --- NUEVO: Contador de rondas ---
    private Coroutine _roundTitleCoroutine; // Para manejar la corrutina del título


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
        if (floatingTextPrefab == null) Debug.LogError("CombatManager: 'floatingTextPrefab' no asignado. No se mostrará texto flotante.", this);
        // floatingTextCanvasTransform es opcional, así que un LogWarning está bien si está vacío.
        if (floatingTextCanvasTransform == null) Debug.LogWarning("CombatManager: 'floatingTextCanvasTransform' no asignado. Los textos flotantes se instanciarán como hijos del combatiente (asume World Space Canvas en prefab) o en la raíz de la escena.", this);
        if (directHitVFXPrefab == null) Debug.LogWarning("CombatManager: 'directHitVFXPrefab' no asignado. No se mostrará VFX para golpes directos.", this);
        if (roundTitleText != null)
        {
            roundTitleText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CombatManager: 'roundTitleText' no asignado. No se mostrará el título de la ronda.");
        }
        if (sfxAudioSource == null) Debug.LogWarning("CombatManager: 'sfxAudioSource' no asignado. No se reproducirán efectos de sonido.", this);

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
    // --- NUEVO: Método para Reproducir Sonidos ---
    public void PlaySoundEffect(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip);
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
        _currentRoundNumber = 0; // Resetear al iniciar combate
        if (roundTitleText != null) roundTitleText.gameObject.SetActive(false);

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
        if (roundTitleText != null) roundTitleText.gameObject.SetActive(false);
        if (_roundTitleCoroutine != null)
        {
            StopCoroutine(_roundTitleCoroutine);
            _roundTitleCoroutine = null;
        }

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

        // Añadir personajes de la party
        if (currentPlayerPartyData != null)
        {
            for (int i = 0; i < currentPlayerPartyData.Count; i++)
            {
                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && currentPlayerPartyData[i] != null)
                {
                    Character character = currentPlayerPartyData[i];
                    GameObject partyMemberSpriteGO = new GameObject("PartyCombatSprite_" + character.characterName);
                    partyMemberSpriteGO.transform.position = partySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) partyMemberSpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);

                    SpriteRenderer sr = partyMemberSpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = character.portraitSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    Animator pAnim = partyMemberSpriteGO.AddComponent<Animator>();
                    // --- USAR ANIMATOR CONTROLLER DEL CHARACTER.CS ---
                    // --- USAR ANIMATOR CONTROLLER DEL CHARACTER.CS O EL FALLBACK ---
                    if (character.combatAnimatorController != null)
                    {
                        pAnim.runtimeAnimatorController = character.combatAnimatorController;
                    }
                    else if (playerFallbackAnimatorController != null) // Usar el fallback si el específico no está
                    {
                        pAnim.runtimeAnimatorController = playerFallbackAnimatorController;
                        Debug.LogWarning($"Personaje {character.characterName} no tiene un Combat Animator Controller específico. Usando fallback del CombatManager.");
                    }
                    else
                    {
                        Debug.LogError($"Personaje {character.characterName} no tiene Combat Animator Controller específico NI hay un Fallback asignado en CombatManager.");
                    }
                    // --- FIN USO ANIMATOR ---

                    BoxCollider2D partyCol = partyMemberSpriteGO.AddComponent<BoxCollider2D>();
                    if (sr.sprite != null) partyCol.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);
                    else partyCol.size = new Vector2(0.5f, 0.5f);

                    partyMemberSpriteGO.AddComponent<CombatSpriteEventHandler>();

                    _partyCombatSpriteGOs.Add(partyMemberSpriteGO);
                    _combatants.Add(new Combatant(character, partyMemberSpriteGO));
                }
            }
        }

        // Añadir enemigos
        if (currentEnemyGroupData != null)
        {
            for (int i = 0; i < currentEnemyGroupData.Count; i++)
            {
                if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null && currentEnemyGroupData[i] != null)
                {
                    EnemyData enemy = currentEnemyGroupData[i];
                    GameObject enemySpriteGO = new GameObject("EnemyCombatSprite_" + enemy.enemyName);
                    enemySpriteGO.transform.position = enemySpawnPoints[i].position;
                    if (currentCombatArenaGameObject != null) enemySpriteGO.transform.SetParent(currentCombatArenaGameObject.transform);
                    SpriteRenderer sr = enemySpriteGO.AddComponent<SpriteRenderer>();
                    sr.sprite = enemy.battleSprite;
                    sr.sortingLayerName = "Characters_Combat";

                    Animator eAnim = enemySpriteGO.AddComponent<Animator>();
                    // --- USAR ANIMATOR CONTROLLER DEL ENEMYDATA.CS ---
                    if (enemy.combatAnimatorController != null)
                    {
                        eAnim.runtimeAnimatorController = enemy.combatAnimatorController;
                    }
                    else
                    {
                        Debug.LogWarning($"Enemigo {enemy.enemyName} no tiene un Combat Animator Controller asignado en su EnemyData asset.");
                        // Opcional: Asignar un controller por defecto si no se encontró uno específico
                        // if (defaultEnemyCombatAnimatorController_DEPRECATED != null) eAnim.runtimeAnimatorController = defaultEnemyCombatAnimatorController_DEPRECATED;
                    }
                    // --- FIN USO ANIMATOR DEL ENEMYDATA ---

                    BoxCollider2D col = enemySpriteGO.AddComponent<BoxCollider2D>();

                    col.isTrigger = true;

                    if (sr.sprite != null) col.size = new Vector2(sr.sprite.bounds.size.x * 0.8f, sr.sprite.bounds.size.y * 0.8f);

                    else col.size = new Vector2(0.5f, 0.5f);

                    enemySpriteGO.layer = LayerMask.NameToLayer("EnemiesInCombat");

                    _enemyCombatSpriteGOs.Add(enemySpriteGO);
                    // --- AÑADIR CombatSpriteEventHandler A ENEMIGOS (si fueran a lanzar proyectiles por evento) --
                    enemySpriteGO.AddComponent<CombatSpriteEventHandler>();

                    // --- FIN ---

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
        Debug.Log($"[{Time.frameCount}] NextTurn: INICIO. isCombatActive: {isCombatActive}"); // Log de tu versión
        if (!isCombatActive)
        {
            Debug.LogWarning($"[{Time.frameCount}] NextTurn: Combate NO activo. Retornando.");
            return;
        }

        if (CheckCombatEndConditions()) // CheckCombatEndConditions llama a EndCombat si se cumplen
        {
            Debug.Log($"[{Time.frameCount}] NextTurn: CheckCombatEndConditions() devolvió true (combate terminado). Retornando.");
            return; // El combate ha terminado
        }

        Debug.Log($"[{Time.frameCount}] NextTurn: El combate continúa. Buscando siguiente combatiente.");

        _currentCombatantIndex++;
        if (_currentCombatantIndex >= _combatants.Count)
        {
            _currentCombatantIndex = 0;
            _currentRoundNumber++; // Incrementar el número de ronda
            Debug.Log($"[{Time.frameCount}] NextTurn: ----- NUEVA RONDA ({_currentRoundNumber}) -----");

            // --- MOSTRAR TÍTULO DE RONDA ---
            if (roundTitleText != null)
            {
                if (_roundTitleCoroutine != null) StopCoroutine(_roundTitleCoroutine); // Detener corrutina anterior si existe
                _roundTitleCoroutine = StartCoroutine(ShowRoundTitleCoroutine($"RONDA {_currentRoundNumber}"));
            }
            // --- FIN MOSTRAR TÍTULO ---

            foreach (Combatant combatant in _combatants.Where(c => c != null)) // Añadido check de null por seguridad
            {
                if (combatant.isDefending) combatant.StopDefending();
            }
            UpdatePartyStatusHUD();
        }
        else if (_currentCombatantIndex == 0 && _currentRoundNumber == 0) // Condición para el primer turno del combate
        {
            _currentRoundNumber = 1; // La primera ronda es la 1
            Debug.Log($"[{Time.frameCount}] NextTurn: ----- INICIO COMBATE - RONDA {_currentRoundNumber} -----");
            if (roundTitleText != null)
            {
                if (_roundTitleCoroutine != null) StopCoroutine(_roundTitleCoroutine);
                _roundTitleCoroutine = StartCoroutine(ShowRoundTitleCoroutine($"RONDA {_currentRoundNumber}"));
            }
        }

        // Asegurarse de que _combatants no sea nulo o vacío después de la lógica de ronda
        if (_combatants == null || _combatants.Count == 0)
        {
            Debug.LogError($"[{Time.frameCount}] NextTurn: Lista de combatientes vacía o nula inesperadamente DESPUÉS de procesar ronda. Terminando combate.");
            EndCombat(false);
            return;
        }
        // Asegurarse de que el índice es válido después de potencialmente resetearlo
        if (_currentCombatantIndex < 0 || _currentCombatantIndex >= _combatants.Count)
        {
            Debug.LogError($"[{Time.frameCount}] NextTurn: Índice de combatiente inválido ({_currentCombatantIndex}) DESPUÉS de procesar ronda. Reseteando a 0.");
            _currentCombatantIndex = 0;
            if (_combatants.Count == 0) { EndCombat(false); return; } // Doble check por si acaso
        }

        _activeCombatant = _combatants[_currentCombatantIndex];

        if (_activeCombatant == null)
        { // Comprobar si _activeCombatant es nulo
            Debug.LogError($"[{Time.frameCount}] NextTurn: _activeCombatant es null en el índice {_currentCombatantIndex} ANTES de comprobar isDefeated. Saltando turno e investigando.");
            NextTurn(); // Intentar con el siguiente, pero esto podría indicar un problema más profundo
            return;
        }

        if (_activeCombatant.isDefeated)
        {
            Debug.Log($"[{Time.frameCount}] NextTurn: {_activeCombatant.GetName()} está derrotado. Saltando turno.");
            NextTurn();
            return;
        }

        Debug.Log($"[{Time.frameCount}] NextTurn: FIN. Llamando a StartTurnForActiveCombatant() para {_activeCombatant.GetName()}.");
        StartTurnForActiveCombatant();
    }

    private void StartTurnForActiveCombatant()
    {
        if (_activeCombatant == null || _activeCombatant.isDefeated) { NextTurn(); return; }
        Debug.Log($"CombatManager: Iniciando turno para {_activeCombatant.GetName()} (HP: {_activeCombatant.GetCurrentHP()}/{_activeCombatant.GetMaxHP()}, Defensa: {_activeCombatant.GetDefense()}, Defendiendo: {_activeCombatant.isDefending})");
        // Doble check para asegurar que el combate no terminó justo antes
        if (!isCombatActive)
        {
            Debug.LogWarning($"[{Time.frameCount}] StartTurnForActiveCombatant: El combate terminó antes de iniciar el turno para {_activeCombatant.GetName()}. No se mostrará el menú de acción.");
            return;
        }
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
            // --- ACTIVAR ANIMACIÓN DE ATAQUE DEL ENEMIGO ---
            // --- REPRODUCIR SONIDO DE ATAQUE DEL ENEMIGO ---
            PlaySoundEffect(enemy.enemyData.basicAttackSound);
            // --- FIN SONIDO ---
            if (enemy.animator != null)
            {
                Debug.Log($"{enemy.GetName()} activando AttackTrigger.");
                enemy.animator.SetTrigger("AttackTrigger");
                // Podrías añadir una espera aquí si la animación es larga
                yield return new WaitForSeconds(enemyAttackAnimationDuration); // O la duración exacta de tu clip de animación

            }
            // --- FIN ANIMACIÓN ---
            Debug.Log($"{enemy.GetName()} ataca a {target.GetName()}! (Defensa del objetivo: {target.GetDefense()})");
            // --- INSTANCIAR VFX DE GOLPE DIRECTO DEL ENEMIGO EN EL OBJETIVO (JUGADOR) ---
            if (directHitVFXPrefab != null && target.combatSpriteGO != null)
            {
                Vector3 vfxPosition = target.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0);
                Instantiate(directHitVFXPrefab, vfxPosition, Quaternion.identity);
                Debug.Log($"[{Time.frameCount}] EnemyTurnCoroutine: Instanciado directHitVFXPrefab en {target.GetName()} por ataque de {enemy.GetName()}");
            }
            // --- FIN INSTANCIAR VFX ---

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

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        StartCoroutine(PerformAttackSequence(attacker, target));
    }

    private IEnumerator PerformAttackSequence(Combatant attacker, Combatant target)
    {
        Debug.Log($"[{Time.frameCount}] {attacker.GetName()} inicia secuencia de ATAQUE BÁSICO sobre {target.GetName()}!");

        float animationDuration = playerGenericAnimationDuration;
        if (attacker.isPlayerCharacter && attacker.characterData != null && attacker.characterData.combatAnimatorController != null)
        {
            // Aquí podrías obtener una duración específica del ataque básico si la tuvieras en Character.cs o su AnimatorController
            // Por ahora, se usa playerGenericAnimationDuration para todos los ataques básicos del jugador.
        }

        // Preparar el EventHandler para el posible lanzamiento del proyectil por evento de animación
        CombatSpriteEventHandler eventHandler = null;
        if (attacker.combatSpriteGO != null)
        {
            eventHandler = attacker.combatSpriteGO.GetComponent<CombatSpriteEventHandler>();
        }

        GameObject projectilePrefabFromCharacter = null;
        if (attacker.isPlayerCharacter && attacker.characterData != null)
        {
            projectilePrefabFromCharacter = attacker.characterData.basicAttackProjectilePrefab; // Obtener de Character.cs
        }

        bool isBasicAttackWithProjectile = projectilePrefabFromCharacter != null && projectilePrefabFromCharacter.GetComponent<Projectile>() != null;

        if (isBasicAttackWithProjectile && eventHandler != null)
        {
            int basicAttackDamage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
            eventHandler.SetupForBasicAttackProjectileLaunch(attacker, target, projectilePrefabFromCharacter, basicAttackDamage);
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: EventHandler configurado para proyectil de ataque básico '{projectilePrefabFromCharacter.name}'.");
        }
        else if (isBasicAttackWithProjectile && eventHandler == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] {attacker.GetName()}: CombatSpriteEventHandler no encontrado. Proyectil de ataque básico no se lanzará por evento. Aplicando daño directo.");
            isBasicAttackWithProjectile = false; // Forzar daño directo si el handler es nulo
        }
        else if (attacker.isPlayerCharacter && projectilePrefabFromCharacter != null && projectilePrefabFromCharacter.GetComponent<Projectile>() == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] {attacker.GetName()}: El 'basicAttackProjectilePrefab' ({projectilePrefabFromCharacter.name}) no tiene script Projectile. Aplicando daño directo.");
            isBasicAttackWithProjectile = false; // Forzar daño directo
        }
        // --- REPRODUCIR SONIDO DE ATAQUE BÁSICO DEL JUGADOR ---
        if (attacker.isPlayerCharacter && attacker.characterData != null)
        {
            PlaySoundEffect(attacker.characterData.basicAttackSound);
        }
        // --- FIN SONIDO ---


        // Disparar Animación de Ataque del Atacante
        if (attacker.animator != null)
        {
            attacker.animator.SetTrigger("AttackTrigger");
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()} activando AttackTrigger para ataque básico. Esperando {animationDuration}s.");
            yield return new WaitForSeconds(animationDuration);
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()} terminó espera de animación de ataque básico.");
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Si NO fue un proyectil lanzado por evento (o no se pudo configurar/no existe), aplicar daño directo.
        // Esto cubrirá al aventurero (si su characterData.basicAttackProjectilePrefab es null)
        // y a los enemigos (que no usan esta lógica de proyectil para ataque básico por ahora).
        if (!isBasicAttackWithProjectile)
        {
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: No se lanzó proyectil (o no era aplicable/válido), llamando a ApplyDirectDamage.");
            ApplyDirectDamage(attacker, target);
        }
        else
        {
            Debug.Log($"[{Time.frameCount}] {attacker.GetName()}: Ataque básico es de tipo proyectil. El daño se aplicará al impacto del proyectil (lanzado por evento de animación).");
        }

        isSelectingTargetForAttack = false;
        attackerForTargetSelection = null;
        StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.2f));
    }

    private void ApplyDirectDamage(Combatant attacker, Combatant target)
    {
        if (target.isDefeated) return;
        Debug.Log($"[{Time.frameCount}] ApplyDirectDamage: Aplicando daño de {attacker.GetName()} a {target.GetName()}. Defensa: {target.GetDefense()}, Ataque: {attacker.GetAttack()}");
        // --- INSTANCIAR VFX DE GOLPE DIRECTO EN EL OBJETIVO ---
        if (directHitVFXPrefab != null && target.combatSpriteGO != null)
        {
            Vector3 vfxPosition = target.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0);
            Instantiate(directHitVFXPrefab, vfxPosition, Quaternion.identity);
            Debug.Log($"[{Time.frameCount}] Instanciado directHitVFXPrefab en {target.GetName()}");
        }
        // --- FIN INSTANCIAR VFX ---

        int damage = Mathf.Max(1, attacker.GetAttack() - target.GetDefense());
        target.TakeDamage(damage);
    }

    private void ExecuteSkill(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        if (attacker == null || skill == null) { ResetSelectionStatesAndPassTurn(true); return; }
        if (attacker.isPlayerCharacter == false || attacker.characterData == null) { ResetSelectionStatesAndPassTurn(true); return; }

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);

        StartCoroutine(PerformSkillSequence(attacker, directTarget, skill));
    }

    private IEnumerator PerformSkillSequence(Combatant attacker, Combatant directTarget, AbilityData skill)
    {
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Iniciando para '{skill.abilityName}' por '{attacker.GetName()}'.");

        if (skill.mpCost > 0)
        {
            if (!attacker.characterData.SpendMana(skill.mpCost))
            {
                Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: MP insuficiente para '{skill.abilityName}'.");
                ResetSelectionStatesAndPassTurn(true);
                yield break;
            }
            UpdatePartyStatusHUD();
        }
        // --- REPRODUCIR SONIDO DE LANZAMIENTO DE HABILIDAD ---
        PlaySoundEffect(skill.launchSound);
        // --- FIN SONIDO ---

        float casterAnimationDuration = playerGenericAnimationDuration;
        CombatSpriteEventHandler eventHandler = null;
        if (attacker.combatSpriteGO != null) eventHandler = attacker.combatSpriteGO.GetComponent<CombatSpriteEventHandler>();

        List<Combatant> actualTargets = DetermineActualTargets(skill.targetType, attacker, directTarget);

        // Determinar si la habilidad va a lanzar un proyectil a través de un evento de animación
        bool isSkillAProjectileLaunchedByEvent = skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() != null && eventHandler != null;

        if (isSkillAProjectileLaunchedByEvent)
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Configurando EventHandler para proyectil de '{skill.abilityName}'.");
            eventHandler.SetupForSkillProjectileLaunch(attacker, actualTargets, skill, skill.vfxPrefab);
        }
        else if (skill.vfxPrefab != null && skill.vfxPrefab.GetComponent<Projectile>() != null && eventHandler == null)
        {
            Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: CombatSpriteEventHandler no encontrado en {attacker.GetName()} para lanzar proyectil de {skill.abilityName}. Se intentará aplicar efecto directo si es posible.");
        }

        // Disparar Animación de Lanzamiento del Personaje
        if (attacker.animator != null)
        {
            string triggerName = "AttackTrigger";
            if (!string.IsNullOrEmpty(skill.animationTriggerName)) triggerName = skill.animationTriggerName;
            attacker.animator.SetTrigger(triggerName);
            if (skill.animationDuration > 0.01f) casterAnimationDuration = skill.animationDuration;
        }
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Animación del lanzador '{attacker.animator?.GetCurrentAnimatorClipInfo(0)[0].clip.name}' activada, esperando {casterAnimationDuration}s.");
        yield return new WaitForSeconds(casterAnimationDuration);
        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de espera de animación del lanzador para '{skill.abilityName}'.");

        // --- AÑADIR ESPERA ADICIONAL SI FUE UN PROYECTIL LANZADO POR EVENTO ---
        if (isSkillAProjectileLaunchedByEvent)
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Habilidad '{skill.abilityName}' es de tipo proyectil. Esperando {skillProjectileImpactDelay}s adicionales para impacto de proyectil(es).");
            yield return new WaitForSeconds(skillProjectileImpactDelay); // Esperar que los proyectiles impacten
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de espera adicional para impacto de proyectil de habilidad.");
        }
        else // Si NO es un proyectil lanzado por evento, aplicar efectos directos ahora
        {
            Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Aplicando efecto directo para '{skill.abilityName}'.");
            if (actualTargets.Count == 0 && skill.targetType != AbilityTargetType.None && skill.targetType != AbilityTargetType.Self)
            {
                Debug.LogWarning($"[{Time.frameCount}] PerformSkillSequence: No se encontraron objetivos válidos para efecto directo de {skill.abilityName}.");
            }
            else
            {
                if (skill.targetType == AbilityTargetType.Self && actualTargets.Count == 0 && attacker != null && !attacker.isDefeated) actualTargets.Add(attacker);

                Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Aplicando efecto directo a {actualTargets.Count} objetivo(s).");
                foreach (Combatant t in actualTargets)
                {
                    if (t.isDefeated && skill.effectType != AbilityEffectType.Special) continue;

                    if (directHitVFXPrefab != null && t.combatSpriteGO != null && skill.effectType == AbilityEffectType.Damage)
                    {
                        Instantiate(directHitVFXPrefab, t.combatSpriteGO.transform.position + new Vector3(0, directHitVFX_Y_Offset, 0), Quaternion.identity);
                    }
                    // --- REPRODUCIR SONIDO DE IMPACTO DE HABILIDAD DIRECTA ---
                    PlaySoundEffect(skill.impactSound);
                    // --- FIN SONIDO ---

                    if (skill.effectType == AbilityEffectType.Damage)
                    {
                        int damage = Mathf.Max(1, (int)skill.power + (attacker.characterData.MagicAttack / 2) - t.GetDefense());
                        t.TakeDamage(damage);
                    }
                    else if (skill.effectType == AbilityEffectType.Heal) { t.ApplyHeal((int)skill.power); }
                    else if (skill.effectType == AbilityEffectType.RestoreMP) { if (t.isPlayerCharacter && t.characterData != null) t.ApplyManaRestore((int)skill.power); }
                }
            }
        }

        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Esperando fin de frame ANTES de ResetSelectionStatesAndPassTurn para '{skill.abilityName}'.");
        yield return null;

        Debug.Log($"[{Time.frameCount}] PerformSkillSequence: Fin de la secuencia para '{skill.abilityName}'. Llamando a ResetSelectionStatesAndPassTurn.");
        ResetSelectionStatesAndPassTurn();
    }

    private IEnumerator EndPlayerActionAndProceedToNextTurn(float delay)
    {
        Debug.Log($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Esperando {delay}s.");
        if (delay > 0) yield return new WaitForSeconds(delay);
        else yield return null; // Esperar al menos un frame si el delay es 0

        Debug.Log($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Fin de espera. Llamando a NextTurn(). isCombatActive: {isCombatActive}");
        if (isCombatActive) // Solo llamar a NextTurn si el combate sigue activo (ej: no terminó por CheckCombatEndConditions)
        {
            NextTurn();
        }
        else
        {
            Debug.LogWarning($"[{Time.frameCount}] CombatManager: EndPlayerActionAndProceedToNextTurn - Combate NO está activo, no se llama a NextTurn().");
        }
    }

    private bool CheckCombatEndConditions()
    {
        Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Verificando si el combate ha terminado.");
        if (_combatants == null || _combatants.Count == 0) { EndCombat(false); return true; }

        bool allEnemiesDefeated = _combatants.Where(c => c != null && !c.isPlayerCharacter).All(e => e.isDefeated);
        int enemyCount = _combatants.Count(c => c != null && !c.isPlayerCharacter);
        // foreach (Combatant enemy in _combatants.Where(c => !c.isPlayerCharacter && c != null))
        // { Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions - Enemigo: {enemy.GetName()}, HP: {enemy.GetCurrentHP()}, Derrotado: {enemy.isDefeated}"); }
        // Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Enemigos totales: {enemyCount}. allEnemiesDefeated: {allEnemiesDefeated}");

        if (enemyCount > 0 && allEnemiesDefeated)
        {
            Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: ¡Todos los enemigos derrotados! Jugador gana.");
            EndCombat(true);
            return true;
        }

        bool allPlayersDefeated = _combatants.Where(c => c != null && c.isPlayerCharacter).All(p => p.isDefeated);
        int playerCount = _combatants.Count(c => c != null && c.isPlayerCharacter);
        // Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: Jugadores totales: {playerCount}. allPlayersDefeated: {allPlayersDefeated}");

        if (playerCount > 0 && allPlayersDefeated)
        {
            Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: ¡Todos los jugadores derrotados! Jugador pierde.");
            EndCombat(false);
            return true;
        }

        Debug.Log($"[{Time.frameCount}] CheckCombatEndConditions: El combate continúa.");
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
        if (caster == null || item == null) { ResetSelectionStatesAndPassTurn(); return; }
        if (caster.isPlayerCharacter == false || caster.characterData == null) { ResetSelectionStatesAndPassTurn(); return; }

        if (actionMenuPanel != null) actionMenuPanel.SetActive(false); // Ocultar menú

        StartCoroutine(PerformItemUseSequence(caster, directTarget, item));
    }

    private IEnumerator PerformItemUseSequence(Combatant caster, Combatant directTarget, ItemData item)
    {
        Combatant actualTarget = DetermineItemTarget(item, caster, directTarget);
        if (actualTarget == null || (actualTarget.isPlayerCharacter && actualTarget.characterData == null))
        {
            Debug.LogWarning($"No se pudo aplicar {item.itemName}, objetivo no válido.");
            ResetSelectionStatesAndPassTurn(true);
            yield break;
        }

        Debug.Log($"{caster.GetName()} usa el objeto '{item.itemName}' sobre {actualTarget.GetName()}");
        // (FUTURO: Animación de usar objeto para el caster)
        // if (caster.animator != null) caster.animator.SetTrigger("UseItemTrigger");
        // yield return new WaitForSeconds(playerItemAnimationDuration); // Necesitarías esta variable

        bool itemUsedSuccessfully = false;
        if (item.Use(actualTarget.characterData))
        {
            itemUsedSuccessfully = true;
            if (item.hpToRestore > 0 && actualTarget.isPlayerCharacter) { ShowFloatingText("+" + item.hpToRestore, actualTarget.combatSpriteGO.transform.position, Color.green, actualTarget); }
            if (item.mpToRestore > 0 && actualTarget.isPlayerCharacter) { ShowFloatingText("+" + item.mpToRestore + " MP", actualTarget.combatSpriteGO.transform.position, Color.blue, actualTarget); }
        }

        if (itemUsedSuccessfully)
        {
            if (PlayerInventory.Instance != null) PlayerInventory.Instance.RemoveItem(item, 1);
            UpdatePartyStatusHUD();
        }
        else Debug.LogWarning($"{item.itemName} no pudo ser usado sobre {actualTarget.GetName()}.");

        ResetSelectionStatesAndPassTurn(!itemUsedSuccessfully);
    }

    public void ShowFloatingText(string text, Vector3 worldPosition, Color textColor, Combatant targetCombatant = null)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("CombatManager: floatingTextPrefab no asignado. No se puede mostrar texto flotante.");
            return;
        }

        GameObject textGO;
        Vector3 targetPosition = worldPosition; // Posición base del sprite del objetivo

        // Ajustar la posición Y para que el texto aparezca encima del pivote del sprite del objetivo
        if (targetCombatant != null && targetCombatant.combatSpriteGO != null)
        {
            SpriteRenderer sr = targetCombatant.combatSpriteGO.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // El offset se aplica sobre el centro del sprite si el pivote es central,
                // o sobre la base si el pivote está en los pies.
                // floatingTextYOffset debería ser en unidades del mundo.
                targetPosition = targetCombatant.combatSpriteGO.transform.position + new Vector3(0, floatingTextYOffset, 0);
            }
        }
        else
        { // Si no hay combatiente específico, usar la worldPosition directamente con el offset
            targetPosition = worldPosition + new Vector3(0, floatingTextYOffset, 0);
        }


        // Decidir el padre y la posición del texto flotante
        if (floatingTextCanvasTransform != null) // Si se asignó un Canvas padre (Screen Space)
        {
            textGO = Instantiate(floatingTextPrefab, floatingTextCanvasTransform);
            Canvas canvas = floatingTextCanvasTransform.GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector2 screenPoint = Camera.main.WorldToScreenPoint(targetPosition);
                    textGO.transform.position = screenPoint;
                }
                else // ScreenSpaceCamera o WorldSpace (si el canvas padre es WorldSpace)
                {
                    textGO.transform.position = targetPosition;
                }
            }
            else
            { // Fallback si no hay canvas o cámara
                textGO.transform.position = targetPosition;
            }
        }
        else if (targetCombatant != null && targetCombatant.combatSpriteGO != null &&
                 floatingTextPrefab.GetComponent<Canvas>() != null &&
                 floatingTextPrefab.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace)
        {
            // Si el PREFAB de texto flotante tiene su PROPIO Canvas en World Space, hacerlo hijo del target
            textGO = Instantiate(floatingTextPrefab, targetCombatant.combatSpriteGO.transform);
            // El localPosition se ajusta para que el pivote del texto quede en el offset Y deseado
            // Asumiendo que el pivote del texto flotante está en su centro.
            textGO.transform.localPosition = new Vector3(0, floatingTextYOffset, 0);
        }
        else // Fallback: instanciar en el mundo sin padre específico (requiere que el prefab tenga su propio Canvas WorldSpace)
        {
            textGO = Instantiate(floatingTextPrefab, targetPosition, Quaternion.identity);
        }


        FloatingCombatText floatingTextScript = textGO.GetComponent<FloatingCombatText>();
        if (floatingTextScript != null)
        {
            floatingTextScript.Init(text, textColor, floatingTextDefaultFontSize);
        }
        else
        {
            Debug.LogError("El prefab 'floatingTextPrefab' no tiene el script FloatingCombatText.", this);
            Destroy(textGO);
        }
    }

    private void ResetSelectionStatesAndPassTurn(bool reOpenActionMenu = false)
    {
        Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn INICIO. Reabrir Menú: {reOpenActionMenu}");
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
            Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn - Menú de acción reabierto.");
        }
        else
        {
            // El delay aquí debe ser corto, solo para permitir que el frame actual termine si es necesario.
            // Si la acción ya incluyó una espera para animación, este puede ser muy corto.
            StartCoroutine(EndPlayerActionAndProceedToNextTurn(0.1f));
        }
        Debug.Log($"[{Time.frameCount}] CombatManager: ResetSelectionStatesAndPassTurn FIN.");
    }



    void Update()
    {
        if (!isCombatActive) return;

        // --- Lógica de Selección de Objetivo para ATAQUE ---
        if (isSelectingTargetForAttack && Input.GetMouseButtonDown(0))
        {
            // Debug.Log("CombatManager: Clic detectado mientras isSelectingTargetForAttack es true.");
            if (Camera.main == null)
            {
                Debug.LogError("CombatManager: Camera.main es NULL. Asegúrate de que tu cámara de combate esté tageada como 'MainCamera'.");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("EnemiesInCombat"));

            if (hit.collider != null)
            {
                // Debug.Log($"CombatManager: Raycast (ataque) golpeó a '{hit.collider.gameObject.name}' en la capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                Combatant targetCombatant = _combatants.FirstOrDefault(c => !c.isDefeated && c.combatSpriteGO == hit.collider.gameObject && !c.isPlayerCharacter);

                if (targetCombatant != null)
                {
                    Debug.Log("CombatManager: Objetivo enemigo válido encontrado para ATAQUE: " + targetCombatant.GetName());
                    // --- CORRECCIÓN: Desactivar la selección ANTES de ejecutar la acción ---
                    isSelectingTargetForAttack = false;
                    ExecuteAttack(attackerForTargetSelection, targetCombatant);
                    // attackerForTargetSelection se resetea dentro de PerformAttackSequence o su corolario
                }
                else
                {
                    Debug.LogWarning("CombatManager: Clic en un objeto con collider en la capa de enemigos, pero no es un combatiente enemigo válido/vivo.");
                    // No resetear isSelectingTargetForAttack aquí para permitir otro intento si el jugador quiere
                    // Opcional: Podrías querer que el jugador pueda cancelar la selección aquí
                    // ResetSelectionStatesAndPassTurn(true); // Esto volvería al menú de acción
                }
            }
            // else { Debug.Log("Ataque: Clic en el vacío."); } // Opcional: cancelar si se hace clic en el vacío
        }
        // --- Lógica de Selección de Objetivo para HABILIDAD ---
        else if (isSelectingTargetForSkill && Input.GetMouseButtonDown(0))
        {
            // Debug.Log("CombatManager: Clic detectado mientras isSelectingTargetForSkill es true.");
            if (Camera.main == null) { /* ... */ return; }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, combatantLayerMask);

            if (hit.collider != null)
            {
                // Debug.Log($"CombatManager: Raycast (habilidad) golpeó a '{hit.collider.gameObject.name}' en la capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
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
                        Debug.Log($"CombatManager: Objetivo '{targetCombatant.GetName()}' válido para la habilidad '{_selectedAbility.abilityName}'.");
                        // --- CORRECCIÓN: Desactivar la selección ANTES de ejecutar la habilidad ---
                        isSelectingTargetForSkill = false;
                        ExecuteSkill(attackerForTargetSelection, targetCombatant, _selectedAbility);
                        // attackerForTargetSelection y _selectedAbility se resetean en ResetSelectionStatesAndPassTurn
                    }
                    else
                    {
                        Debug.LogWarning($"CombatManager: Objetivo '{targetCombatant.GetName()}' NO es válido para la habilidad '{_selectedAbility.abilityName}'.");
                        // Considerar no cerrar el panel de skills inmediatamente, sino dar feedback y permitir reintentar o cancelar.
                        // Por ahora, la lógica de ResetSelectionStatesAndPassTurn(true) se llamará si ExecuteSkill no procede.
                        // Si se quiere cancelar explícitamente aquí:
                        // ResetSelectionStatesAndPassTurn(true); // Vuelve al menú de acción
                    }
                }
                // else { Debug.LogWarning("CombatManager: Clic en un objeto con collider, pero no es un combatiente válido/vivo o no hay habilidad seleccionada."); }
            }
            // else { Debug.Log("Habilidad: Clic en el vacío."); } // Opcional
        }
        // --- Lógica de Selección de Objetivo para OBJETOS ---
        else if (isSelectingTargetForItem && Input.GetMouseButtonDown(0))
        {
            // ... (lógica similar, poner isSelectingTargetForItem = false; antes de ExecuteItem) ...
            if (Camera.main == null) { return; }
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
                        // --- CORRECCIÓN: Desactivar la selección ANTES de ejecutar el ítem ---
                        isSelectingTargetForItem = false;
                        ExecuteItem(attackerForTargetSelection, targetCombatant, _selectedItemData);
                    }
                    else
                    {
                        Debug.LogWarning($"Objetivo '{targetCombatant.GetName()}' NO es válido para el objeto '{_selectedItemData.itemName}'.");
                        ResetSelectionStatesAndPassTurn(true);
                    }
                }
            }
        }
        // Teclas de depuración para terminar combate (solo si no estamos seleccionando nada)
        else if (!isSelectingTargetForAttack && !isSelectingTargetForSkill && !isSelectingTargetForItem)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) EndCombat(true);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) EndCombat(false);
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
    private Combatant DetermineItemTarget(ItemData item, Combatant caster, Combatant directTarget)
    {
        // Lógica simplificada: si es consumible de HP/MP, el objetivo es un aliado o el lanzador.
        if (item.itemType == ItemType.Consumable && (item.hpToRestore > 0 || item.mpToRestore > 0))
        {
            if (directTarget != null && directTarget.isPlayerCharacter && !directTarget.isDefeated) return directTarget;
            if (caster != null && !caster.isDefeated && caster.isPlayerCharacter) return caster;
        }
        // (Añadir lógica para otros tipos de ítems, ej: bombas que apuntan a enemigos)
        // else if (item.itemType == ItemType.Bomb_Damage_Enemy_Etc) {
        //    if (directTarget != null && !directTarget.isPlayerCharacter && !directTarget.isDefeated) return directTarget;
        // }
        return null;
    }

    private List<Combatant> DetermineActualTargets(AbilityTargetType targetType, Combatant attacker, Combatant directTarget)
    {
        List<Combatant> actualTargets = new List<Combatant>();
        if (attacker == null) return actualTargets; // Necesitamos un atacante

        switch (targetType)
        {
            case AbilityTargetType.Self:
                if (!attacker.isDefeated) actualTargets.Add(attacker);
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
            case AbilityTargetType.None:
                // Para habilidades que no tienen un objetivo específico (ej: un buff de área que no se selecciona)
                // O podría aplicarse a todos, o a un grupo predefinido.
                // Si es para todos los aliados, se debería usar AllAllies.
                // Si es para todos los enemigos, AllEnemies.
                // Si es para el propio lanzador, Self.
                // Si es "None" y tiene un efecto, la habilidad misma debe saber a quién aplicarlo o cómo.
                // Por ahora, si es None, no se añaden objetivos aquí.
                break;
        }
        return actualTargets;
    }
    public float GetEnemyDefeatAnimDuration()
    {
        // Podrías hacer esto más dinámico si los enemigos tienen diferentes duraciones de animación de derrota
        // almacenadas en su EnemyData, por ejemplo.
        return enemyAttackAnimationDuration; // Reutilizando la duración del ataque por ahora, o usa enemyDefeatAnimationBaseDuration
    }
    private IEnumerator ShowRoundTitleCoroutine(string title)
    {
        if (roundTitleText == null) yield break;

        roundTitleText.text = title;
        roundTitleText.gameObject.SetActive(true);

        // Opcional: Podrías añadir un pequeño efecto de fade in aquí si quieres
        // CanvasGroup titleCG = roundTitleText.GetComponent<CanvasGroup>(); // Si le añades un CanvasGroup
        // if (titleCG != null) { /* ... lógica de fade in ... */ }

        yield return new WaitForSeconds(roundTitleDisplayDuration);

        // Opcional: Efecto de Fade Out para el título
        // if (titleCG != null) { /* ... lógica de fade out ... */ }
        // else roundTitleText.gameObject.SetActive(false); 
        roundTitleText.gameObject.SetActive(false); // Simple ocultación por ahora

        _roundTitleCoroutine = null; // Limpiar referencia
    }

}
