using UnityEngine;
using UnityEngine.UI; // Para Image, Button, etc.
using TMPro;          // Para TextMeshProUGUI
using System.Collections.Generic;
using System.Text;
using TopDown;
// using TuJuego.Inventario; // Descomenta si tus clases de inventario y personaje están en un namespace
// using TuJuego.Personajes; // Descomenta si tu clase Character está en este namespace

public class EquipmentScreenManager : MonoBehaviour
{
    // --- Singleton Pattern (Opcional, pero útil si se accede desde varios sitios) ---
    public static EquipmentScreenManager Instance { get; private set; }

    [Header("Paneles Principales de la UI")]
    [Tooltip("El GameObject raíz de toda la pantalla de equipamiento.")]
    [SerializeField] private GameObject equipmentScreenPanel;
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
    [Tooltip("Transform padre donde se encuentran los UI de los slots de equipo del personaje (MainHand, Head, Body, Feet).")] // MODIFICADO Tooltip
    [SerializeField] private Transform characterEquipmentSlotsContainer;
    // [Tooltip("Prefab para un slot de equipamiento individual del personaje (mostrará el ítem equipado).")] // Comentado si los slots ya están en escena
    // [SerializeField] private GameObject characterEquipmentSlotUIPrefab; // Comentado si los slots ya están en escena

    [Header("Panel Izquierdo - Selección de Miembros de la Party")]
    [Tooltip("Transform padre donde se instanciarán los iconos/botones de selección de miembros de la party.")]
    [SerializeField] private Transform partyMemberSelectionContainer;
    [Tooltip("Prefab para un icono/botón de selección de miembro de la party.")]
    [SerializeField] private GameObject partyMemberSelectIconPrefab;

    [Header("Panel Derecho - Inventario del Jugador")]
    [Tooltip("GameObject que actúa como panel y contenedor de los slots del inventario (debe tener GridLayoutGroup).")]
    [SerializeField] private GameObject inventoryDisplayPanel_EquipmentScreen;
    [Tooltip("Prefab para un slot de inventario individual (el mismo que usa InventoryUIManager).")]
    [SerializeField] private GameObject inventorySlotUIPrefab_ForEquipScreen;

    [Header("Input")]
    [Tooltip("Tecla para abrir/cerrar la pantalla de equipamiento.")]
    [SerializeField] private KeyCode toggleEquipmentScreenKey = KeyCode.U;

    private Character _currentlyDisplayedCharacter;
    // private List<PartyMemberSelectIconUI> _partyMemberIconUIs = new List<PartyMemberSelectIconUI>(); 

    // --- DICCIONARIO PARA LOS SLOTS DE EQUIPO DEL PERSONAJE (DESCOMENTADO) ---
    private Dictionary<EquipmentSlot, CharacterEquipmentSlotUI> _characterEquipmentSlotUIs = new Dictionary<EquipmentSlot, CharacterEquipmentSlotUI>();

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
        if (equipmentScreenPanel == null) Debug.LogError("ESM: 'equipmentScreenPanel' no asignado.", this);
        if (characterDisplayPanel == null) Debug.LogError("ESM: 'characterDisplayPanel' no asignado.", this);
        if (characterSpriteImage == null) Debug.LogError("ESM: 'characterSpriteImage' no asignado.", this);
        if (characterNameText == null) Debug.LogError("ESM: 'characterNameText' no asignado.", this);
        if (characterLevelText == null) Debug.LogError("ESM: 'characterLevelText' no asignado.", this);
        if (characterStatsTextDisplay == null) Debug.LogError("ESM: 'characterStatsTextDisplay' no asignado.", this);
        if (characterEquipmentSlotsContainer == null) Debug.LogError("ESM: 'characterEquipmentSlotsContainer' no asignado.", this);
        // if (characterEquipmentSlotUIPrefab == null) Debug.LogError("ESM: 'characterEquipmentSlotUIPrefab' no asignado.", this); // Comentado si no se usa para instanciar
        if (partyMemberSelectionContainer == null) Debug.LogError("ESM: 'partyMemberSelectionContainer' no asignado.", this);
        if (partyMemberSelectIconPrefab == null) Debug.LogError("ESM: 'partyMemberSelectIconPrefab' no asignado.", this);

        if (inventoryDisplayPanel_EquipmentScreen == null)
            Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' no asignado.", this);
        else if (inventoryDisplayPanel_EquipmentScreen.GetComponent<GridLayoutGroup>() == null)
            Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' (" + inventoryDisplayPanel_EquipmentScreen.name + ") NO tiene un componente GridLayoutGroup.", this);

        if (inventorySlotUIPrefab_ForEquipScreen == null)
            Debug.LogError("ESM: 'inventorySlotUIPrefab_ForEquipScreen' no asignado.", this);
    }

    void Start()
    {
        if (equipmentScreenPanel != null)
        {
            equipmentScreenPanel.SetActive(false);
        }

        InitializeCharacterEquipmentSlots(); // Crear/configurar los slots de equipo del personaje
        InitializeInventoryForEquipmentScreen();
        // PopulatePartySelection(); // Esto lo haremos después
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
            // TODO: Obtener y mostrar el primer personaje de la party por defecto
            // (Necesitarás un PartyManager y un Character de prueba para esto)
            // Ejemplo:
            // if (PartyManager.Instance != null && PartyManager.Instance.currentPartyMembers.Count > 0)
            // { SelectCharacterForDisplay(PartyManager.Instance.currentPartyMembers[0]); }
            // else { 
            //    Debug.LogWarning("ESM: No hay PartyManager o no hay personajes. Necesitas un personaje para mostrar.");
            //    SelectCharacterForDisplay(null); // Limpiar UI si no hay personaje
            // }

            // --- PARA PRUEBAS INICIALES, PODRÍAS BUSCAR UN PERSONAJE EN LA ESCENA ---
            // Asume que tu jugador tiene el script Character.cs
            if (FindObjectOfType<PlayerMovement>() != null)
            {
                Character testChar = FindObjectOfType<PlayerMovement>().GetComponent<Character>();
                if (testChar != null) SelectCharacterForDisplay(testChar);
                else SelectCharacterForDisplay(null); // Limpiar si no se encuentra
            }
            else
            {
                SelectCharacterForDisplay(null); // Limpiar si no se encuentra
            }
            // --- FIN PRUEBAS INICIALES ---

            RefreshInventoryForEquipmentScreen();
            Debug.Log("Pantalla de Equipamiento Abierta");
        }
        else
        {
            Debug.Log("Pantalla de Equipamiento Cerrada");
        }
    }

    /// <summary>
    /// Inicializa los slots de UI para el equipamiento del personaje.
    /// Busca los componentes CharacterEquipmentSlotUI en los hijos del contenedor.
    /// </summary>
    private void InitializeCharacterEquipmentSlots()
    {
        if (characterEquipmentSlotsContainer == null)
        {
            Debug.LogError("ESM: 'characterEquipmentSlotsContainer' no está asignado. No se pueden inicializar los slots de equipo.");
            return;
        }
        _characterEquipmentSlotUIs.Clear(); // Limpiar por si se llama varias veces

        // Obtener todos los componentes CharacterEquipmentSlotUI que son hijos del contenedor
        CharacterEquipmentSlotUI[] slotsInScene = characterEquipmentSlotsContainer.GetComponentsInChildren<CharacterEquipmentSlotUI>();

        if (slotsInScene.Length == 0)
        {
            Debug.LogWarning("ESM: No se encontraron CharacterEquipmentSlotUI como hijos de " + characterEquipmentSlotsContainer.name + ". Asegúrate de que los slots estén ahí y tengan el script.", this);
            return;
        }

        Debug.Log("ESM: Encontrados " + slotsInScene.Length + " CharacterEquipmentSlotUI en la escena.");
        foreach (CharacterEquipmentSlotUI slotUI in slotsInScene)
        {
            EquipmentSlot type = slotUI.GetSlotType(); // El script del slot debe exponer su tipo
            if (type != EquipmentSlot.None && !_characterEquipmentSlotUIs.ContainsKey(type))
            {
                _characterEquipmentSlotUIs.Add(type, slotUI);
                Debug.Log("ESM: Registrado slot de UI para " + type.ToString());
                // Aquí podrías suscribirte a un evento OnClick de slotUI si lo tuviera
                // Ejemplo: slotUI.OnThisSlotClicked += HandleCharacterEquipmentSlotClicked;
            }
            else if (type == EquipmentSlot.None)
            {
                Debug.LogWarning("ESM: CharacterEquipmentSlotUI '" + slotUI.gameObject.name + "' tiene SlotType como None. No se registrará.", slotUI.gameObject);
            }
            else if (_characterEquipmentSlotUIs.ContainsKey(type))
            {
                Debug.LogWarning("ESM: Ya existe un CharacterEquipmentSlotUI registrado para el SlotType: " + type.ToString() + ". GameObject: " + slotUI.gameObject.name, slotUI.gameObject);
            }
        }
    }


    public void SelectCharacterForDisplay(Character characterToDisplay)
    {
        if (characterToDisplay == null)
        {
            Debug.LogWarning("ESM: SelectCharacterForDisplay - Se intentó mostrar un personaje nulo.");
            _currentlyDisplayedCharacter = null;
            if (characterNameText != null) characterNameText.text = "---";
            if (characterLevelText != null) characterLevelText.text = "Nvl: --";
            if (characterSpriteImage != null) { characterSpriteImage.sprite = null; characterSpriteImage.enabled = false; }
            if (characterStatsTextDisplay != null) characterStatsTextDisplay.text = "";
            ClearCharacterEquipmentSlotsDisplay();
            return;
        }

        _currentlyDisplayedCharacter = characterToDisplay;
        Debug.Log("ESM: Mostrando equipo para: " + _currentlyDisplayedCharacter.characterName);

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
        UpdateCharacterEquipmentSlotsDisplay(); // Llamar para mostrar el equipo actual
        RefreshInventoryForEquipmentScreen(true);
    }

    private void UpdateCharacterStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || characterStatsTextDisplay == null) return;
        StringBuilder statsBuilder = new StringBuilder();
        statsBuilder.AppendLine("HP: " + _currentlyDisplayedCharacter.currentHP + "/" + _currentlyDisplayedCharacter.MaxHP);
        statsBuilder.AppendLine("MP: " + _currentlyDisplayedCharacter.currentMP + "/" + _currentlyDisplayedCharacter.MaxMP);
        statsBuilder.AppendLine("Ataque: " + _currentlyDisplayedCharacter.Attack);
        statsBuilder.AppendLine("Defensa: " + _currentlyDisplayedCharacter.Defense);
        // ... (más stats)
        characterStatsTextDisplay.text = statsBuilder.ToString();
    }

    private void ClearCharacterEquipmentSlotsDisplay()
    {
        if (_characterEquipmentSlotUIs == null) return;
        // Iterar por el diccionario de UIs de slots de equipo y limpiarlos
        foreach (CharacterEquipmentSlotUI slotUI in _characterEquipmentSlotUIs.Values)
        {
            if (slotUI != null)
            {
                slotUI.DisplayEquippedItem(null); // Mostrar como vacío
            }
        }
    }

    private void UpdateCharacterEquipmentSlotsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || _characterEquipmentSlotUIs == null)
        {
            ClearCharacterEquipmentSlotsDisplay(); // Limpiar si no hay personaje
            return;
        }
        // Debug.Log("ESM: Actualizando UI de slots de equipo para: " + _currentlyDisplayedCharacter.characterName);

        if (_currentlyDisplayedCharacter.equippedItems != null)
        {
            // Iterar por cada tipo de slot de equipo definido en el enum
            foreach (EquipmentSlot slotType in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slotType == EquipmentSlot.None) continue; // Ignorar el tipo 'None'

                ItemData equippedItem = null;
                // Intentar obtener el ítem equipado por el personaje en este slotType
                _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotType, out equippedItem);

                // Intentar obtener el componente UI para este slotType del diccionario
                if (_characterEquipmentSlotUIs.TryGetValue(slotType, out CharacterEquipmentSlotUI slotUI))
                {
                    if (slotUI != null)
                    {
                        // Actualizar el slot UI con el ítem (que puede ser null si no hay nada equipado)
                        slotUI.DisplayEquippedItem(equippedItem);
                    }
                }
                else
                {
                    // Esto significa que InitializeCharacterEquipmentSlots no encontró/registró un UI para este slotType
                    // Podría ser normal si no tienes un UI para todos los EquipmentSlot (ej: si no usas OffHand)
                    // Pero para Head, Body, Feet, MainHand, deberías tenerlos.
                    // Debug.LogWarning("ESM: No se encontró UI para el EquipmentSlot: " + slotType + " al intentar actualizar display.");
                }
            }
        }
        else
        {
            Debug.LogWarning("ESM: _currentlyDisplayedCharacter.equippedItems es null para " + _currentlyDisplayedCharacter.characterName);
            ClearCharacterEquipmentSlotsDisplay(); // Limpiar la UI si no hay datos de equipo
        }
    }

    // --- Métodos para el inventario en esta pantalla (ya estaban) ---
    private void InitializeInventoryForEquipmentScreen()
    {
        if (inventoryDisplayPanel_EquipmentScreen == null || inventorySlotUIPrefab_ForEquipScreen == null) return;
        Transform containerTransform = inventoryDisplayPanel_EquipmentScreen.transform; // Usar el transform del panel que tiene el GridLayoutGroup

        foreach (Transform child in containerTransform)
            Destroy(child.gameObject);
        _inventorySlotUIs_EquipmentScreen.Clear();

        int slotsToDisplay = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 20;

        for (int i = 0; i < slotsToDisplay; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotUIPrefab_ForEquipScreen, containerTransform);
            slotGO.name = "InventorySlotUI_EquipScreen_" + i;
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                _inventorySlotUIs_EquipmentScreen.Add(slotUIComponent);
                // Suscribir el método de este manager al evento del slot
                // slotUIComponent.OnSlotClickedAction = HandleInventorySlotSelectionOnEquipScreen; // Se hará cuando InventorySlotUI esté listo
                slotUIComponent.ClearSlotDisplay();
            }
            else Debug.LogError("ESM: Prefab de slot de inventario no tiene InventorySlotUI.", this);
        }
        // Debug.Log("Inventario para la pantalla de equipamiento inicializado con " + slotsToDisplay + " slots.");
    }
    public void RefreshInventoryForEquipmentScreen(bool filter = false)
    {
        if (inventoryDisplayPanel_EquipmentScreen == null || PlayerInventory.Instance == null) return;
        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots;

        int expectedSlots = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 0;
        if (_inventorySlotUIs_EquipmentScreen.Count != expectedSlots && expectedSlots > 0)
        { // Solo reinicializar si se espera un número > 0
            Debug.LogWarning("ESM: El número de UI slots (" + _inventorySlotUIs_EquipmentScreen.Count +
                             ") no coincide con maxInventorySlots del jugador (" + expectedSlots +
                             "). Re-inicializando la UI del inventario para esta pantalla.");
            InitializeInventoryForEquipmentScreen();
        }

        for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
        {
            if (i < playerSlotsData.Count)
            {
                _inventorySlotUIs_EquipmentScreen[i].gameObject.SetActive(true);
                _inventorySlotUIs_EquipmentScreen[i].UpdateSlotDisplay(playerSlotsData[i]);
            }
            else
            {
                _inventorySlotUIs_EquipmentScreen[i].ClearSlotDisplay();
            }
        }
        // Debug.Log("Inventario para la pantalla de equipamiento refrescado.");
    }
    // ... (resto de los métodos como HandleInventorySlotSelectionOnEquipScreen, DisplayItemInfoAndActions_EquipScreen, HideItemInfoActionPanel_EquipScreen, y los On...Clicked para los botones del panel de info del inventario)
}
