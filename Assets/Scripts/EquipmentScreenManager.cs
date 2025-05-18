using UnityEngine;
using UnityEngine.UI; // Para Image, Button, etc.
using TMPro;          // Para TextMeshProUGUI
using System.Collections.Generic;
using System.Text;
// using TuJuego.Inventario; // Descomenta si tus clases de inventario y personaje están en un namespace
// using TuJuego.Personajes; // Descomenta si tu clase Character está en este namespace

public class EquipmentScreenManager : MonoBehaviour
{
    // --- Singleton Pattern (Opcional, pero útil si se accede desde varios sitios) ---
    public static EquipmentScreenManager Instance { get; private set; }

    [Header("Paneles Principales de la UI")]
    [Tooltip("El GameObject raíz de toda la pantalla de equipamiento.")]
    [SerializeField] private GameObject equipmentScreenPanel; // El panel que se activa/desactiva
    [Tooltip("Opcional: TextMeshProUGUI para el título principal de esta pantalla (ej: 'Equipamiento').")]
    [SerializeField] private TextMeshProUGUI screenTitleText;

    [Header("Panel Izquierdo - Detalles del Personaje")]
    [Tooltip("Panel que muestra la información del personaje seleccionado.")]
    [SerializeField] private GameObject characterDisplayPanel;
    [Tooltip("Image para mostrar el sprite del personaje seleccionado.")]
    [SerializeField] private Image characterSpriteImage;
    [Tooltip("Texto para el nombre del personaje seleccionado.")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [Tooltip("Texto para el nivel del personaje seleccionado.")]
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [Tooltip("Contenedor o TextMeshProUGUI para mostrar los stats detallados del personaje.")]
    [SerializeField] private TextMeshProUGUI characterStatsTextDisplay;

    [Header("Panel Izquierdo - Slots de Equipamiento del Personaje")]
    [Tooltip("Transform padre donde se instanciarán los UI de los slots de equipo del personaje (MainHand, Head, Body, Feet).")]
    [SerializeField] private Transform characterEquipmentSlotsContainer;
    [Tooltip("Prefab para un slot de equipamiento individual del personaje (mostrará el ítem equipado).")]
    [SerializeField] private GameObject characterEquipmentSlotUIPrefab;

    [Header("Panel Izquierdo - Selección de Miembros de la Party")]
    [Tooltip("Transform padre donde se instanciarán los iconos/botones de selección de miembros de la party.")]
    [SerializeField] private Transform partyMemberSelectionContainer;
    [Tooltip("Prefab para un icono/botón de selección de miembro de la party.")]
    [SerializeField] private GameObject partyMemberSelectIconPrefab;

    [Header("Panel Derecho - Inventario del Jugador")]
    [Tooltip("GameObject que actúa como panel y contenedor de los slots del inventario (debe tener GridLayoutGroup).")]
    [SerializeField] private GameObject inventoryDisplayPanel_EquipmentScreen;
    [Tooltip("Prefab para un slot de inventario individual (el mismo que usa InventoryUIManager).")] // NUEVA VARIABLE
    [SerializeField] private GameObject inventorySlotUIPrefab_EquipmentScreen; // NUEVA VARIABLE

    [Header("Input")]
    [Tooltip("Tecla para abrir/cerrar la pantalla de equipamiento.")]
    [SerializeField] private KeyCode toggleEquipmentScreenKey = KeyCode.U;

    private Character _currentlyDisplayedCharacter;
    // --- LÍNEAS COMENTADAS TEMPORALMENTE HASTA QUE SE CREEN LOS SCRIPTS ---
    // private List<PartyMemberSelectIconUI> _partyMemberIconUIs = new List<PartyMemberSelectIconUI>(); 
    // private Dictionary<EquipmentSlot, CharacterEquipmentSlotUI> _characterEquipmentSlotUIs = new Dictionary<EquipmentSlot, CharacterEquipmentSlotUI>(); 
    private List<InventorySlotUI> _inventorySlotUIs_EquipmentScreen = new List<InventorySlotUI>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); 

        // Validaciones básicas de referencias
        if (equipmentScreenPanel == null) Debug.LogError("EquipmentScreenManager: 'equipmentScreenPanel' no asignado.", this);
        if (characterDisplayPanel == null) Debug.LogError("EquipmentScreenManager: 'characterDisplayPanel' no asignado.", this);
        if (characterSpriteImage == null) Debug.LogError("EquipmentScreenManager: 'characterSpriteImage' no asignado.", this);
        if (characterNameText == null) Debug.LogError("EquipmentScreenManager: 'characterNameText' no asignado.", this);
        if (characterLevelText == null) Debug.LogError("EquipmentScreenManager: 'characterLevelText' no asignado.", this);
        if (characterStatsTextDisplay == null) Debug.LogError("EquipmentScreenManager: 'characterStatsTextDisplay' no asignado.", this);
        if (characterEquipmentSlotsContainer == null) Debug.LogError("EquipmentScreenManager: 'characterEquipmentSlotsContainer' no asignado.", this);
        if (characterEquipmentSlotUIPrefab == null) Debug.LogError("EquipmentScreenManager: 'characterEquipmentSlotUIPrefab' no asignado.", this);
        if (partyMemberSelectionContainer == null) Debug.LogError("EquipmentScreenManager: 'partyMemberSelectionContainer' no asignado.", this);
        if (partyMemberSelectIconPrefab == null) Debug.LogError("EquipmentScreenManager: 'partyMemberSelectIconPrefab' no asignado.", this);

        if (inventoryDisplayPanel_EquipmentScreen == null)
            Debug.LogError("EquipmentScreenManager: 'inventoryDisplayPanel_EquipmentScreen' no asignado.", this);
        else if (inventoryDisplayPanel_EquipmentScreen.GetComponent<GridLayoutGroup>() == null)
            Debug.LogError("EquipmentScreenManager: 'inventoryDisplayPanel_EquipmentScreen' (" + inventoryDisplayPanel_EquipmentScreen.name + ") NO tiene un componente GridLayoutGroup.", this);

        // NUEVA VALIDACIÓN
        if (inventorySlotUIPrefab_EquipmentScreen == null)
            Debug.LogError("EquipmentScreenManager: 'inventorySlotUIPrefab_EquipmentScreen' no asignado. No se podrán crear los slots del inventario en esta pantalla.", this);

    }

    void Start()
    {
        if (equipmentScreenPanel != null)
        {
            equipmentScreenPanel.SetActive(false);
        }

        // Las siguientes líneas se implementarán cuando tengamos los scripts y prefabs correspondientes
        // PopulatePartySelection();
        // InitializeCharacterEquipmentSlots();
        InitializeInventoryForEquipmentScreen(); // Descomentado para que se ejecute
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleEquipmentScreenKey))
        {
            ToggleEquipmentScreen();
        }
        if (equipmentScreenPanel != null && equipmentScreenPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEquipmentScreen();
        }
    }

    public void ToggleEquipmentScreen()
    {
        if (equipmentScreenPanel == null) return;
        bool isActive = equipmentScreenPanel.activeSelf;
        equipmentScreenPanel.SetActive(!isActive);

        if (equipmentScreenPanel.activeSelf)
        {
            // TODO: Seleccionar el primer personaje de la party por defecto y mostrar su info
            // if (PartyManager.Instance != null && PartyManager.Instance.currentPartyMembers.Count > 0)
            // { SelectCharacterForDisplay(PartyManager.Instance.currentPartyMembers[0]); }
            // else { SelectCharacterForDisplay(null); }
            RefreshInventoryForEquipmentScreen(); // Llamar para poblar/refrescar el inventario
            Debug.Log("Pantalla de Equipamiento Abierta");
        }
        else
        {
            Debug.Log("Pantalla de Equipamiento Cerrada");
        }
    }

    public void SelectCharacterForDisplay(Character characterToDisplay)
    {
        if (characterToDisplay == null)
        {
            Debug.LogWarning("SelectCharacterForDisplay: Se intentó mostrar un personaje nulo.");
            if (characterNameText != null) characterNameText.text = "---";
            if (characterLevelText != null) characterLevelText.text = "Nvl: --";
            if (characterSpriteImage != null) { characterSpriteImage.sprite = null; characterSpriteImage.enabled = false; }
            if (characterStatsTextDisplay != null) characterStatsTextDisplay.text = "";
            ClearCharacterEquipmentSlotsDisplay();
            return;
        }

        _currentlyDisplayedCharacter = characterToDisplay;
        Debug.Log("Mostrando equipo para: " + _currentlyDisplayedCharacter.characterName);

        if (characterNameText != null) characterNameText.text = _currentlyDisplayedCharacter.characterName;
        if (characterLevelText != null) characterLevelText.text = "Nvl: " + _currentlyDisplayedCharacter.level.ToString();

        if (characterSpriteImage != null)
        {
            if (_currentlyDisplayedCharacter.portraitSprite != null)
            {
                characterSpriteImage.sprite = _currentlyDisplayedCharacter.portraitSprite;
                characterSpriteImage.enabled = true;
            }
            else
            {
                characterSpriteImage.sprite = null;
                characterSpriteImage.enabled = false;
            }
        }

        UpdateCharacterStatsDisplay();
        UpdateCharacterEquipmentSlotsDisplay();
        RefreshInventoryForEquipmentScreen(true); // Refrescar inventario, quizás filtrando
    }

    private void UpdateCharacterStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || characterStatsTextDisplay == null) return;
        StringBuilder statsBuilder = new StringBuilder();
        statsBuilder.AppendLine("HP: " + _currentlyDisplayedCharacter.currentHP + "/" + _currentlyDisplayedCharacter.MaxHP);
        statsBuilder.AppendLine("MP: " + _currentlyDisplayedCharacter.currentMP + "/" + _currentlyDisplayedCharacter.MaxMP);
        statsBuilder.AppendLine("Ataque: " + _currentlyDisplayedCharacter.Attack);
        statsBuilder.AppendLine("Defensa: " + _currentlyDisplayedCharacter.Defense);
        characterStatsTextDisplay.text = statsBuilder.ToString();
    }

    private void ClearCharacterEquipmentSlotsDisplay()
    {
        if (characterEquipmentSlotsContainer != null)
        {
            // Lógica futura para limpiar los slots de equipo
        }
    }

    private void UpdateCharacterEquipmentSlotsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || characterEquipmentSlotsContainer == null) return;
        // Debug.Log("Actualizando UI de slots de equipo para: " + _currentlyDisplayedCharacter.characterName); // Menos verboso
        if (_currentlyDisplayedCharacter.equippedItems != null)
        {
            foreach (EquipmentSlot slotType in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slotType == EquipmentSlot.None) continue;
                ItemData equippedItem = null;
                _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotType, out equippedItem);
                // Debug.Log("Slot: " + slotType + " - Item: " + (equippedItem != null ? equippedItem.itemName : "Vacío")); // Menos verboso
                // TODO: Actualizar la UI del slot de equipo específico (CharacterEquipmentSlotUI)
            }
        }
        else
        {
            Debug.LogWarning("_currentlyDisplayedCharacter.equippedItems es null para " + _currentlyDisplayedCharacter.characterName);
        }
    }

    // --- MÉTODO PARA INICIALIZAR EL INVENTARIO EN ESTA PANTALLA ---
    private void InitializeInventoryForEquipmentScreen()
    {
        // Comprobar si las referencias necesarias están asignadas
        if (inventoryDisplayPanel_EquipmentScreen == null || inventorySlotUIPrefab_EquipmentScreen == null)
        {
            Debug.LogError("EquipmentScreenManager: Falta 'inventoryDisplayPanel_EquipmentScreen' o 'inventorySlotUIPrefab_EquipmentScreen'. No se pueden crear los slots del inventario.");
            return;
        }

        // Limpiar slots antiguos para evitar duplicados si se llama varias veces
        foreach (Transform child in inventoryDisplayPanel_EquipmentScreen.transform)
        {
            Destroy(child.gameObject);
        }
        _inventorySlotUIs_EquipmentScreen.Clear();

        // Determinar cuántos slots mostrar. Podría ser un número fijo o basado en PlayerInventory.maxInventorySlots
        // Por ahora, usaremos un número fijo como en InventoryUIManager, o podríamos tomarlo de PlayerInventory.
        int slotsToDisplay = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 20; // Ejemplo, ajusta según necesidad

        for (int i = 0; i < slotsToDisplay; i++)
        {
            // Instanciar el prefab del slot de inventario como hijo del contenedor del panel derecho
            GameObject slotGO = Instantiate(inventorySlotUIPrefab_EquipmentScreen, inventoryDisplayPanel_EquipmentScreen.transform);
            slotGO.name = "InventorySlotUI_EquipScreen_" + i;
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                _inventorySlotUIs_EquipmentScreen.Add(slotUIComponent);
                slotUIComponent.ClearSlotDisplay(); // Asegurar que empiecen vacíos visualmente
            }
            else
            {
                Debug.LogError("EquipmentScreenManager: El prefab 'inventorySlotUIPrefab_EquipmentScreen' no tiene el componente InventorySlotUI.", this);
            }
        }
        // Después de crear los slots, refrescar su contenido
        // RefreshInventoryForEquipmentScreen(); // Se llamará cuando se abra la pantalla o cambie el personaje
        Debug.Log("Inventario para la pantalla de equipamiento inicializado con " + slotsToDisplay + " slots.");
    }

    // --- MÉTODO PARA REFESCAR EL INVENTARIO EN ESTA PANTALLA ---
    public void RefreshInventoryForEquipmentScreen(bool filterForCurrentCharacterAndSlot = false)
    {
        if (inventoryDisplayPanel_EquipmentScreen == null || PlayerInventory.Instance == null) return;

        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots;

        // Asegurarse de que tenemos la cantidad correcta de UI slots (si el número de slots de UI es dinámico)
        // Si _inventorySlotUIs_EquipmentScreen.Count es fijo (igual a maxSlotsToDisplay de PlayerInventory), no es necesario
        if (_inventorySlotUIs_EquipmentScreen.Count != PlayerInventory.Instance.maxInventorySlots)
        {
            // Podríamos llamar a InitializeInventoryForEquipmentScreen() pero sería destructivo.
            // Es mejor que Initialize cree el número correcto de slots basado en PlayerInventory.maxInventorySlots
            Debug.LogWarning("El número de UI slots no coincide con maxInventorySlots. Re-inicializando...");
            InitializeInventoryForEquipmentScreen(); // Esto limpiará y recreará los slots
                                                     // Es importante que PlayerInventory.Instance.maxInventorySlots sea el número de slots visuales que quieres
        }


        for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
        {
            if (i < playerSlotsData.Count) // Si hay datos de un objeto para este slot de UI
            {
                // TODO: Lógica de filtrado si filterForCurrentCharacterAndSlot es true
                // Por ahora, muestra todos los objetos.
                _inventorySlotUIs_EquipmentScreen[i].gameObject.SetActive(true); // Asegurar que esté activo
                _inventorySlotUIs_EquipmentScreen[i].UpdateSlotDisplay(playerSlotsData[i]);
            }
            else // Si no hay más objetos en el inventario para los slots de UI restantes
            {
                _inventorySlotUIs_EquipmentScreen[i].ClearSlotDisplay();
                // Opcional: podrías desactivar los slots vacíos si no quieres que se vean
                // _inventorySlotUIs_EquipmentScreen[i].gameObject.SetActive(false); 
            }
        }
        Debug.Log("Inventario para la pantalla de equipamiento refrescado.");
    }
}
