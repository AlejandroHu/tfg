using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using TopDown; // Asumiendo que Character.cs está aquí
// Asegúrate de que los namespaces de tus otras clases sean accesibles
// using TuJuego.Habilidades; // Si AbilityData.cs está en este namespace

public class CharacterStatsScreenManager : MonoBehaviour
{
    public static CharacterStatsScreenManager Instance { get; private set; }

    [Header("Paneles Principales de la UI")]
    [Tooltip("El GameObject raíz de toda la pantalla de estado/datos del personaje.")]
    [SerializeField] private GameObject characterStatsScreenPanel;
    [Tooltip("Opcional: TextMeshProUGUI para el título principal de esta pantalla (ej: 'Estado' o el nombre del personaje).")]
    [SerializeField] private TextMeshProUGUI screenTitleText_Stats;

    [Header("Información Principal del Personaje")]
    [Tooltip("Image para mostrar el sprite grande del personaje seleccionado.")]
    [SerializeField] private Image characterBigSpriteImage_StatsScreen;
    [Tooltip("Texto para el nombre del personaje.")]
    [SerializeField] private TextMeshProUGUI characterNameText_StatsScreen;
    [Tooltip("Texto para el nivel del personaje.")]
    [SerializeField] private TextMeshProUGUI characterLevelText_StatsScreen;

    [Header("Información de Experiencia (XP)")]
    [Tooltip("Texto para mostrar la XP actual del personaje (ej: 'XP: 500').")]
    [SerializeField] private TextMeshProUGUI currentXPText_StatsScreen;
    [Tooltip("Texto para mostrar la XP necesaria para el siguiente nivel (ej: 'Siguiente: 1200').")]
    [SerializeField] private TextMeshProUGUI nextLevelXPText_StatsScreen;
    [Tooltip("Opcional: Slider o Image (tipo Filled) para la barra de progreso de XP.")]
    [SerializeField] private Slider xpProgressBar_StatsScreen;

    [Header("Stats Detallados del Personaje")]
    [Tooltip("Contenedor padre de todos los elementos de texto de los stats individuales.")]
    [SerializeField] private GameObject detailedStatsContainer_StatsScreen;
    [SerializeField] private TextMeshProUGUI hpValueText;
    [SerializeField] private TextMeshProUGUI mpValueText;
    [SerializeField] private TextMeshProUGUI attackValueText;
    [SerializeField] private TextMeshProUGUI defenseValueText;
    [SerializeField] private TextMeshProUGUI magicAttackValueText;
    [SerializeField] private TextMeshProUGUI magicDefenseValueText;
    [SerializeField] private TextMeshProUGUI speedValueText;

    [Header("Panel de Habilidades/Magias")]
    [Tooltip("GameObject que contiene la lista de habilidades y su descripción.")]
    [SerializeField] private GameObject abilitiesPanel_StatsScreen;
    [Tooltip("Transform padre (Content de un ScrollView) donde se instanciarán los items de la lista de habilidades.")]
    [SerializeField] private Transform abilitiesListContainer_StatsScreen;
    [Tooltip("Prefab para un item individual en la lista de habilidades (debe tener AbilityListItem_UI.cs).")]
    [SerializeField] private GameObject abilityListItemUIPrefab;
    [Tooltip("TextMeshProUGUI para mostrar la descripción de la habilidad seleccionada.")]
    [SerializeField] private TextMeshProUGUI abilityDescriptionText_StatsScreen;

    [Header("Input y Control")]
    [Tooltip("Tecla para abrir/cerrar esta pantalla de estado (si se accede directamente).")]
    [SerializeField] private KeyCode toggleStatsScreenKey = KeyCode.C;
    [Tooltip("Botón para cerrar la pantalla de estado/datos.")]
    [SerializeField] private Button closeButton_StatsScreen;

    private Character _currentlyDisplayedCharacter;
    private List<AbilityListItem_UI> _abilityListItemUIs = new List<AbilityListItem_UI>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (characterStatsScreenPanel == null) Debug.LogError("CSSM: 'characterStatsScreenPanel' no asignado.", this);
        if (abilitiesListContainer_StatsScreen == null) Debug.LogWarning("CSSM: 'abilitiesListContainer_StatsScreen' no asignado.", this);
        if (abilityListItemUIPrefab == null) Debug.LogWarning("CSSM: 'abilityListItemUIPrefab' no asignado.", this);
        if (abilityDescriptionText_StatsScreen == null) Debug.LogWarning("CSSM: 'abilityDescriptionText_StatsScreen' no asignado.", this);
        if (characterBigSpriteImage_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterBigSpriteImage_StatsScreen' no asignado.", this);
        if (characterNameText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterNameText_StatsScreen' no asignado.", this);
        if (characterLevelText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'characterLevelText_StatsScreen' no asignado.", this);
        if (currentXPText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'currentXPText_StatsScreen' no asignado.", this);
        if (nextLevelXPText_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'nextLevelXPText_StatsScreen' no asignado.", this);
        if (detailedStatsContainer_StatsScreen == null) Debug.LogWarning("CharacterStatsScreenManager: 'detailedStatsContainer_StatsScreen' no asignado.", this);
        if (hpValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'hpValueText' no asignado.", this);
        if (mpValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'mpValueText' no asignado.", this);
        if (attackValueText == null) Debug.LogWarning("CharacterStatsScreenManager: 'attackValueText' no asignado.", this);
    }

    void OnEnable()
    {
        Character.OnCharacterLevelUp += HandleCharacterLevelUp;
    }

    void OnDisable()
    {
        Character.OnCharacterLevelUp -= HandleCharacterLevelUp;
    }

    void Start()
    {
        if (characterStatsScreenPanel != null)
        {
            characterStatsScreenPanel.SetActive(false);
        }
        if (closeButton_StatsScreen != null) closeButton_StatsScreen.onClick.AddListener(HideScreen);
        else Debug.LogWarning("CSSM: 'closeButton_StatsScreen' no asignado. No se podrá cerrar la pantalla con botón.", this);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleStatsScreenKey))
        {
            if (characterStatsScreenPanel != null && characterStatsScreenPanel.activeSelf)
            {
                HideScreen();
            }
            else
            {
                Character charToOpenWith = null;
                if (PartyManager.Instance != null && PartyManager.Instance.CurrentPartyMembers.Count > 0)
                {
                    charToOpenWith = PartyManager.Instance.SelectedMenuCharacter ?? PartyManager.Instance.CurrentPartyMembers[0];
                    if (PartyManager.Instance.SelectedMenuCharacter == null && charToOpenWith != null)
                    {
                        PartyManager.Instance.SetSelectedMenuCharacter(charToOpenWith); // Asegurar que PartyManager lo sepa
                    }
                }
                else
                {
                    PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                    if (pm != null) charToOpenWith = pm.GetComponent<Character>();
                }
                if (charToOpenWith != null) ShowScreen(charToOpenWith);
                else Debug.Log("CSSM: Tecla para abrir Stats, pero no hay personaje para mostrar.");
            }
        }
    }

    public void ShowScreen(Character characterToShow)
    {
        if (characterStatsScreenPanel == null) return;
        if (characterToShow == null) { HideScreen(); return; }

        _currentlyDisplayedCharacter = characterToShow;
        characterStatsScreenPanel.SetActive(true);
        UpdateAllCharacterInfo();
    }

    public void HideScreen()
    {
        if (characterStatsScreenPanel != null) characterStatsScreenPanel.SetActive(false);
        _currentlyDisplayedCharacter = null;
    }

    private void UpdateAllCharacterInfo()
    {
        if (_currentlyDisplayedCharacter == null)
        {
            // Limpiar UI si no hay personaje
            if (characterNameText_StatsScreen != null) characterNameText_StatsScreen.text = "---";
            // ... (limpiar otros campos como en la versión anterior del canvas) ...
            return;
        }

        if (screenTitleText_Stats != null) screenTitleText_Stats.text = "Estado de " + _currentlyDisplayedCharacter.characterName;
        if (characterBigSpriteImage_StatsScreen != null)
        {
            characterBigSpriteImage_StatsScreen.sprite = _currentlyDisplayedCharacter.portraitSprite;
            characterBigSpriteImage_StatsScreen.enabled = (_currentlyDisplayedCharacter.portraitSprite != null);
        }
        if (characterNameText_StatsScreen != null) characterNameText_StatsScreen.text = _currentlyDisplayedCharacter.characterName;
        if (characterLevelText_StatsScreen != null) characterLevelText_StatsScreen.text = "Nivel: " + _currentlyDisplayedCharacter.level.ToString();
        if (currentXPText_StatsScreen != null) currentXPText_StatsScreen.text = "XP: " + _currentlyDisplayedCharacter.currentXP.ToString();
        if (nextLevelXPText_StatsScreen != null) nextLevelXPText_StatsScreen.text = "Siguiente: " + _currentlyDisplayedCharacter.experienceToNextLevel.ToString();
        if (xpProgressBar_StatsScreen != null && _currentlyDisplayedCharacter.experienceToNextLevel > 0)
        {
            xpProgressBar_StatsScreen.value = (float)_currentlyDisplayedCharacter.currentXP / _currentlyDisplayedCharacter.experienceToNextLevel;
        }
        else if (xpProgressBar_StatsScreen != null) { xpProgressBar_StatsScreen.value = 0; }

        UpdateDetailedStatsDisplay();
        PopulateAbilitiesList();
    }

    private void UpdateDetailedStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null) return;
        if (hpValueText != null) hpValueText.text = _currentlyDisplayedCharacter.currentHP.ToString() + " / " + _currentlyDisplayedCharacter.MaxHP.ToString();
        if (mpValueText != null) mpValueText.text = _currentlyDisplayedCharacter.currentMP.ToString() + " / " + _currentlyDisplayedCharacter.MaxMP.ToString();
        if (attackValueText != null) attackValueText.text = _currentlyDisplayedCharacter.Attack.ToString();
        if (defenseValueText != null) defenseValueText.text = _currentlyDisplayedCharacter.Defense.ToString();
        if (magicAttackValueText != null) magicAttackValueText.text = _currentlyDisplayedCharacter.MagicAttack.ToString();
        if (magicDefenseValueText != null) magicDefenseValueText.text = _currentlyDisplayedCharacter.MagicDefense.ToString();
        if (speedValueText != null) speedValueText.text = _currentlyDisplayedCharacter.Speed.ToString();
    }

    private void PopulateAbilitiesList()
    {
        if (abilitiesListContainer_StatsScreen == null || abilityListItemUIPrefab == null || _currentlyDisplayedCharacter == null)
        {
            if (abilitiesListContainer_StatsScreen != null)
            {
                foreach (Transform child in abilitiesListContainer_StatsScreen) Destroy(child.gameObject);
                _abilityListItemUIs.Clear();
            }
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "";
            return;
        }

        foreach (Transform child in abilitiesListContainer_StatsScreen) Destroy(child.gameObject);
        _abilityListItemUIs.Clear();

        if (_currentlyDisplayedCharacter.knownAbilities == null || _currentlyDisplayedCharacter.knownAbilities.Count == 0)
        {
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "Sin habilidades conocidas.";
            return;
        }

        foreach (AbilityData ability in _currentlyDisplayedCharacter.knownAbilities)
        {
            if (ability == null) continue;
            GameObject listItemGO = Instantiate(abilityListItemUIPrefab, abilitiesListContainer_StatsScreen);
            AbilityListItem_UI listItemUI = listItemGO.GetComponent<AbilityListItem_UI>();
            if (listItemUI != null)
            {
                // --- MODIFICADO: Pasar el método OnAbilityListItemClicked de ESTE manager como callback ---
                listItemUI.SetupAbilityItem(ability, this.OnAbilityListItemClicked);
                _abilityListItemUIs.Add(listItemUI);
            }
            else
            {
                Debug.LogError("CSSM: El prefab 'abilityListItemUIPrefab' no tiene el componente AbilityListItem_UI.", this);
                Destroy(listItemGO);
            }
        }

        if (_abilityListItemUIs.Count > 0 && _abilityListItemUIs[0].CurrentAbilityData != null)
        {
            OnAbilityListItemClicked(_abilityListItemUIs[0].CurrentAbilityData);
        }
        else if (abilityDescriptionText_StatsScreen != null)
        {
            abilityDescriptionText_StatsScreen.text = "";
        }
    }

    public void OnAbilityListItemClicked(AbilityData selectedAbility)
    {
        if (selectedAbility == null)
        {
            if (abilityDescriptionText_StatsScreen != null) abilityDescriptionText_StatsScreen.text = "";
            return;
        }

        if (abilityDescriptionText_StatsScreen != null)
        {
            StringBuilder descBuilder = new StringBuilder();
            descBuilder.AppendLine("<b>" + selectedAbility.abilityName + "</b>");
            if (selectedAbility.mpCost > 0)
            {
                descBuilder.AppendLine("Coste MP: " + selectedAbility.mpCost);
            }
            descBuilder.AppendLine("<i>" + selectedAbility.effectType.ToString() + " - " + selectedAbility.targetType.ToString() + "</i>");
            descBuilder.AppendLine("--------------------");
            descBuilder.AppendLine(selectedAbility.description);
            abilityDescriptionText_StatsScreen.text = descBuilder.ToString();
        }
    }

    private void HandleCharacterLevelUp(Character characterWhoLeveledUp)
    {
        if (characterStatsScreenPanel != null && characterStatsScreenPanel.activeSelf &&
            _currentlyDisplayedCharacter == characterWhoLeveledUp)
        {
            UpdateAllCharacterInfo();
        }
    }
}
