using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using TopDown; // Asumiendo que PlayerMovement y Character están aquí

// Asegúrate de que el namespace de tus otras clases sea correcto si los usas
// using TuJuego.Inventario; 
// using TuJuego.Personajes; 

public class EquipmentScreenManager : MonoBehaviour
{
    public static EquipmentScreenManager Instance { get; private set; }

    [Header("Paneles Principales de la UI")]
    [SerializeField] private GameObject equipmentScreenPanel;
    [SerializeField] private TextMeshProUGUI screenTitleText;

    [Header("Panel Izquierdo - Detalles del Personaje")]
    [SerializeField] private GameObject characterDisplayPanel;
    [SerializeField] private Image characterSpriteImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI characterStatsTextDisplay;

    [Header("Panel Izquierdo - Slots de Equipamiento del Personaje")]
    [SerializeField] private Transform characterEquipmentSlotsContainer;
    // [SerializeField] private GameObject characterEquipmentSlotUIPrefab; 

    [Header("Panel Izquierdo - Selección de Miembros de la Party")]
    [Tooltip("Transform padre donde se instanciarán los iconos/botones para seleccionar miembros de la party.")]
    [SerializeField] private Transform partyMemberSelectionContainer;
    [Tooltip("Prefab para un icono/botón de selección de miembro de la party.")]
    [SerializeField] private GameObject partyMemberSelectIconPrefab;

    [Header("Panel Derecho - Inventario del Jugador")]
    [SerializeField] private GameObject inventoryDisplayPanel_EquipmentScreen;
    [SerializeField] private GameObject inventorySlotUIPrefab_ForEquipScreen;

    [Header("Panel de Info/Acciones del Ítem")]
    [Tooltip("Panel para mostrar detalles y acciones del ítem (equipado o del inventario).")]
    [SerializeField] private GameObject itemInfoActionPanel;
    [SerializeField] private RectTransform itemInfoActionPanelRect;
    [SerializeField] private TextMeshProUGUI itemNameText_InfoPanel;
    [SerializeField] private TextMeshProUGUI itemDescriptionText_InfoPanel;
    [SerializeField] private TextMeshProUGUI itemStatsText_InfoPanel;
    [SerializeField] private Button useButton_InfoPanel;
    [SerializeField] private Button equipButton_InfoPanel;
    [SerializeField] private Button unequipButton_InfoPanel;
    [SerializeField] private Button discardButton_InfoPanel;
    [SerializeField] private Button closeInfoButton_InfoPanel;

    [Header("Configuración de Posición del Panel de Info")]
    [SerializeField] private float infoPanelOffsetX = 10f;
    [SerializeField] private float infoPanelOffsetY = 0f;

    [Header("Input")]
    [SerializeField] private KeyCode toggleEquipmentScreenKey = KeyCode.U;

    private Character _currentlyDisplayedCharacter;
    private InventorySlot _currentlySelectedItemFromInventory;
    private ItemData _currentlySelectedItemFromEquipment;
    private EquipmentSlot _currentlySelectedEquipmentSlot;

    // Lista para los UI de los iconos de selección de party
    private List<PartyMemberSelectIconUI> _partyMemberIconUIs = new List<PartyMemberSelectIconUI>();
    private Dictionary<EquipmentSlot, CharacterEquipmentSlotUI> _characterEquipmentSlotUIs = new Dictionary<EquipmentSlot, CharacterEquipmentSlotUI>();
    private List<InventorySlotUI> _inventorySlotUIs_EquipmentScreen = new List<InventorySlotUI>();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Validaciones
        if (equipmentScreenPanel == null) Debug.LogError("ESM: 'equipmentScreenPanel' no asignado.", this);
        if (partyMemberSelectionContainer == null) Debug.LogError("ESM: 'partyMemberSelectionContainer' no asignado.", this);
        if (partyMemberSelectIconPrefab == null) Debug.LogError("ESM: 'partyMemberSelectIconPrefab' no asignado. No se podrán crear los iconos de selección de party.", this);
        // ... (resto de tus validaciones que ya tenías)
        if (characterDisplayPanel == null) Debug.LogError("ESM: 'characterDisplayPanel' no asignado.", this);
        if (characterSpriteImage == null) Debug.LogError("ESM: 'characterSpriteImage' no asignado.", this);
        if (characterNameText == null) Debug.LogError("ESM: 'characterNameText' no asignado.", this);
        if (characterLevelText == null) Debug.LogError("ESM: 'characterLevelText' no asignado.", this);
        if (characterStatsTextDisplay == null) Debug.LogError("ESM: 'characterStatsTextDisplay' no asignado.", this);
        if (characterEquipmentSlotsContainer == null) Debug.LogError("ESM: 'characterEquipmentSlotsContainer' no asignado.", this);
        if (inventoryDisplayPanel_EquipmentScreen == null) Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' no asignado.", this);
        else if (inventoryDisplayPanel_EquipmentScreen.GetComponent<GridLayoutGroup>() == null) Debug.LogError("ESM: 'inventoryDisplayPanel_EquipmentScreen' NO tiene GridLayoutGroup.", this);
        if (inventorySlotUIPrefab_ForEquipScreen == null) Debug.LogError("ESM: 'inventorySlotUIPrefab_ForEquipScreen' no asignado.", this);
        if (itemInfoActionPanel != null)
        {
            if (itemInfoActionPanelRect == null) itemInfoActionPanelRect = itemInfoActionPanel.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("ESM: 'itemInfoActionPanel' NO ESTÁ ASIGNADO.", this);
        }
    }

    void OnEnable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged += HandlePlayerInventoryChanged;
        else
            Debug.LogWarning("ESM: PlayerInventory.Instance es null en OnEnable.");
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged -= HandlePlayerInventoryChanged;
    }

    void Start()
    {
        if (equipmentScreenPanel != null) equipmentScreenPanel.SetActive(false);
        if (itemInfoActionPanel != null) itemInfoActionPanel.SetActive(false);

        InitializeCharacterEquipmentSlots();
        InitializeInventoryForEquipmentScreen();
        // No llamamos a PopulatePartySelection aquí, se llamará al abrir la pantalla.

        // Listeners de botones del panel de información
        if (useButton_InfoPanel != null) useButton_InfoPanel.onClick.AddListener(OnUseItemClicked_InfoPanel);
        if (equipButton_InfoPanel != null) equipButton_InfoPanel.onClick.AddListener(OnEquipItemClicked_InfoPanel);
        if (unequipButton_InfoPanel != null) unequipButton_InfoPanel.onClick.AddListener(OnUnequipItemClicked_InfoPanel);
        if (discardButton_InfoPanel != null) discardButton_InfoPanel.onClick.AddListener(OnDiscardItemClicked_InfoPanel);
        if (closeInfoButton_InfoPanel != null) closeInfoButton_InfoPanel.onClick.AddListener(HideItemInfoActionPanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleEquipmentScreenKey)) ToggleEquipmentScreen();
        if (equipmentScreenPanel != null && equipmentScreenPanel.activeSelf &&
            itemInfoActionPanel != null && itemInfoActionPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            HideItemInfoActionPanel();
        }
    }

    public void ToggleEquipmentScreen()
    {
        if (equipmentScreenPanel == null) return;
        bool isActive = equipmentScreenPanel.activeSelf;
        equipmentScreenPanel.SetActive(!isActive);

        if (equipmentScreenPanel.activeSelf)
        {
            // Al abrir, refrescar la lista de miembros de la party (por si cambió)
            PopulatePartySelection();

            // Seleccionar el primer personaje de la party por defecto o el personaje jugador principal
            Character characterToDisplay = null;
            // --- LÓGICA DE PRUEBA PARA OBTENER PERSONAJE INICIAL ---
            // Si hay iconos de party y el primero es válido, intenta usar su personaje.
            // Sino, busca el PlayerMovement.
            if (_partyMemberIconUIs.Count > 0 && _partyMemberIconUIs[0] != null)
            {
                // Asumimos que PartyMemberSelectIconUI tiene una forma de exponer su _representedCharacter.
                // Si no, esta lógica necesitará ajustarse o depender de un PartyManager.
                // Por ahora, si SetupIcon fue llamado, _representedCharacter debería estar seteado en el iconUI.
                // Necesitaríamos una propiedad pública en PartyMemberSelectIconUI para acceder a _representedCharacter.
                // Ejemplo: public Character RepresentedCharacter => _representedCharacter;
                // characterToDisplay = _partyMemberIconUIs[0].RepresentedCharacter; // Si tuvieras esa propiedad

                // Solución temporal más simple: si hay party, y el jugador principal está en ella, lo seleccionamos.
                // Esto aún depende de cómo obtienes tu 'currentParty' en PopulatePartySelection.
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    characterToDisplay = pm.GetComponent<Character>();
                }
            }
            else // Si no hay iconos de party (o la lógica anterior falla)
            {
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    characterToDisplay = pm.GetComponent<Character>();
                }
            }
            // --- FIN LÓGICA DE PRUEBA ---

            SelectCharacterForDisplay(characterToDisplay);
            RefreshInventoryForEquipmentScreen();
            HideItemInfoActionPanel();
            Debug.Log("ESM: Pantalla de Equipamiento Abierta");
        }
        else
        {
            HideItemInfoActionPanel();
            Debug.Log("ESM: Pantalla de Equipamiento Cerrada");
        }
    }

    /// <summary>
    /// Crea o actualiza los iconos de selección para cada miembro de la party en la UI.
    /// </summary>
    private void PopulatePartySelection()
    {
        if (partyMemberSelectionContainer == null || partyMemberSelectIconPrefab == null)
        {
            Debug.LogError("ESM: PopulatePartySelection - Falta 'partyMemberSelectionContainer' o 'partyMemberSelectIconPrefab'. No se pueden crear los iconos de selección de party.");
            return;
        }

        // Limpiar iconos antiguos para evitar duplicados si se llama varias veces
        foreach (Transform child in partyMemberSelectionContainer)
        {
            Destroy(child.gameObject);
        }
        _partyMemberIconUIs.Clear(); // Limpiar la lista de referencias a los scripts UI

        // --- LÓGICA DE PRUEBA PARA OBTENER LA PARTY ---
        // Esta sección debe ser reemplazada por la lógica de tu PartyManager.cs cuando lo tengas.
        // Por ahora, crearemos una lista de prueba que solo contiene al personaje jugador principal.
        List<Character> currentPartyForDisplay = new List<Character>();
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            Character mainCharacter = playerMovement.GetComponent<Character>();
            if (mainCharacter != null)
            {
                currentPartyForDisplay.Add(mainCharacter);
                // Debug.Log("ESM: PopulatePartySelection - Añadido personaje principal a la party de prueba: " + mainCharacter.characterName);
            }
            else
            {
                Debug.LogWarning("ESM: PopulatePartySelection - El GameObject con PlayerMovement no tiene un componente Character.");
            }
        }
        else
        {
            Debug.LogWarning("ESM: PopulatePartySelection - No se encontró PlayerMovement en la escena para la party de prueba.");
        }

        // Ejemplo para añadir un segundo personaje de prueba (si lo tienes configurado en la escena con un script Character)
        // GameObject otherCharacterGO = GameObject.Find("NombreDeOtroPersonajeEnEscena"); // Cambia el nombre
        // if (otherCharacterGO != null) {
        //    Character otherChar = otherCharacterGO.GetComponent<Character>();
        //    if (otherChar != null && !currentPartyForDisplay.Contains(otherChar)) { // Evitar duplicados
        //        currentPartyForDisplay.Add(otherChar);
        //        Debug.Log("ESM: PopulatePartySelection - Añadido OTRO personaje a la party de prueba: " + otherChar.characterName);
        //    }
        // }
        // --- FIN LÓGICA DE PRUEBA ---

        // Cuando tengas tu PartyManager:
        // if (PartyManager.Instance == null || PartyManager.Instance.currentPartyMembers.Count == 0)
        // {
        //    Debug.LogWarning("ESM: PopulatePartySelection - No hay PartyManager o no hay miembros en la party.");
        //    SelectCharacterForDisplay(null); // Limpiar la UI del personaje si no hay party
        //    return;
        // }
        // List<Character> currentPartyForDisplay = PartyManager.Instance.currentPartyMembers;


        // Debug.Log("ESM: Poblando selección de party con " + currentPartyForDisplay.Count + " miembros.");
        foreach (Character member in currentPartyForDisplay)
        {
            if (member == null) continue;

            // Instanciar el prefab del icono de selección de party
            GameObject iconGO = Instantiate(partyMemberSelectIconPrefab, partyMemberSelectionContainer);
            iconGO.name = "PartyIcon_" + member.characterName; // Ponerle un nombre descriptivo

            // Obtener el script PartyMemberSelectIconUI del icono instanciado
            PartyMemberSelectIconUI iconUI = iconGO.GetComponent<PartyMemberSelectIconUI>();
            if (iconUI != null)
            {
                // Configurar el icono con los datos del personaje.
                // El método SetupIcon en PartyMemberSelectIconUI se encarga de:
                // 1. Guardar la referencia al 'member'.
                // 2. Actualizar su 'characterPortraitImage'.
                // 3. Configurar su botón para que llame a EquipmentScreenManager.Instance.SelectCharacterForDisplay(member).
                iconUI.SetupIcon(member);
                _partyMemberIconUIs.Add(iconUI); // Añadir a la lista para referencia futura si es necesario
            }
            else
            {
                Debug.LogError("ESM: El prefab 'partyMemberSelectIconPrefab' (" + partyMemberSelectIconPrefab.name + ") no tiene el componente PartyMemberSelectIconUI. Asegúrate de que el script esté en el prefab raíz.", this);
            }
        }
    }
    private void InitializeCharacterEquipmentSlots()
    {
        if (characterEquipmentSlotsContainer == null) return; // Salir si el contenedor no está asignado.
        _characterEquipmentSlotUIs.Clear(); // Limpiar el diccionario por si se llama varias veces.

        // Obtener todos los scripts CharacterEquipmentSlotUI que son hijos del contenedor.
        CharacterEquipmentSlotUI[] slotsInScene = characterEquipmentSlotsContainer.GetComponentsInChildren<CharacterEquipmentSlotUI>();

        foreach (CharacterEquipmentSlotUI slotUI in slotsInScene)
        {
            EquipmentSlot type = slotUI.GetSlotType(); // Obtener el tipo de slot configurado en el Inspector del slotUI.

            if (type != EquipmentSlot.None) // Asegurarse de que el slot tenga un tipo válido asignado.
            {
                if (!_characterEquipmentSlotUIs.ContainsKey(type)) // Si no hay ya un UI para este tipo de slot.
                {
                    _characterEquipmentSlotUIs.Add(type, slotUI); // Añadir al diccionario.
                    slotUI.ClearDisplay(); // Asegurar que empiece visualmente vacío.
                }
            }
        }
    }

    /// <summary>
    /// Establece el personaje cuyos datos se mostrarán en el panel izquierdo y actualiza la UI.
    /// </summary>
    /// <param name="characterToDisplay">El personaje a mostrar. Puede ser null para limpiar la UI.</param>
    public void SelectCharacterForDisplay(Character characterToDisplay)
    {
        _currentlyDisplayedCharacter = characterToDisplay; // Guardar el personaje actual.

        if (_currentlyDisplayedCharacter == null)
        { // Si no hay personaje para mostrar.
            // Limpiar los campos de la UI.
            if (characterNameText != null) characterNameText.text = "---";
            if (characterLevelText != null) characterLevelText.text = "Nvl: --";
            if (characterSpriteImage != null) { characterSpriteImage.sprite = null; characterSpriteImage.enabled = false; }
            if (characterStatsTextDisplay != null) characterStatsTextDisplay.text = "";
            ClearCharacterEquipmentSlotsDisplay(); // Limpiar los slots de equipo.
            Debug.LogWarning("ESM: SelectCharacterForDisplay - Personaje nulo. UI limpiada.");
            return;
        }
        Debug.Log("ESM: Mostrando equipo para: " + _currentlyDisplayedCharacter.characterName);

        // Actualizar los textos e imagen del personaje.
        if (characterNameText != null) characterNameText.text = _currentlyDisplayedCharacter.characterName;
        if (characterLevelText != null) characterLevelText.text = "Nvl: " + _currentlyDisplayedCharacter.level.ToString();
        if (characterSpriteImage != null)
        {
            characterSpriteImage.sprite = _currentlyDisplayedCharacter.portraitSprite; // Asignar el retrato del personaje.
            characterSpriteImage.enabled = (characterSpriteImage.sprite != null); // Mostrar solo si hay un sprite asignado.
        }

        UpdateCharacterStatsDisplay(); // Actualizar los stats mostrados.
        UpdateCharacterEquipmentSlotsDisplay(); // Actualizar los iconos del equipo.
        RefreshInventoryForEquipmentScreen(true); // Refrescar el inventario (el 'true' podría ser para filtrar en el futuro).
    }

    // Actualiza los textos de los stats del personaje actualmente mostrado.
    private void UpdateCharacterStatsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || characterStatsTextDisplay == null) return;
        StringBuilder statsBuilder = new StringBuilder(); // Usar StringBuilder para construir el string de stats eficientemente.
        // Asumimos que Character.cs tiene estas propiedades (MaxHP, MaxMP, Attack, Defense) que ya incluyen bonos de equipo.
        statsBuilder.AppendLine("HP: " + _currentlyDisplayedCharacter.currentHP + "/" + _currentlyDisplayedCharacter.MaxHP);
        statsBuilder.AppendLine("MP: " + _currentlyDisplayedCharacter.currentMP + "/" + _currentlyDisplayedCharacter.MaxMP);
        statsBuilder.AppendLine("Ataque: " + _currentlyDisplayedCharacter.Attack);
        statsBuilder.AppendLine("Defensa: " + _currentlyDisplayedCharacter.Defense);
        // Aquí podrías añadir más stats si los tienes definidos en tu clase Character.
        characterStatsTextDisplay.text = statsBuilder.ToString(); // Asignar el string construido al TextMeshPro.
    }

    // Limpia la visualización de todos los slots de equipo del personaje (los hace parecer vacíos).
    private void ClearCharacterEquipmentSlotsDisplay()
    {
        if (_characterEquipmentSlotUIs == null) return;
        foreach (CharacterEquipmentSlotUI slotUI in _characterEquipmentSlotUIs.Values)
        { // Itera por todos los UI de slot de equipo registrados.
            if (slotUI != null) slotUI.DisplayEquippedItem(null); // Llama al método del slot UI para mostrarlo como vacío.
        }
    }

    // Actualiza los iconos en los slots de equipo del personaje actualmente mostrado.
    private void UpdateCharacterEquipmentSlotsDisplay()
    {
        if (_currentlyDisplayedCharacter == null || _characterEquipmentSlotUIs == null) { ClearCharacterEquipmentSlotsDisplay(); return; }
        if (_currentlyDisplayedCharacter.equippedItems != null)
        { // Si el personaje tiene datos de equipo.
            // Iterar por cada tipo de slot de equipo definido en el enum EquipmentSlot.
            foreach (EquipmentSlot slotTypeKey in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slotTypeKey == EquipmentSlot.None) continue; // Ignorar el tipo 'None'.

                ItemData equippedItem = null;
                // Intentar obtener el ítem equipado por el personaje en este slotType.
                _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotTypeKey, out equippedItem);

                // Intentar obtener el componente UI para este slotType del diccionario.
                if (_characterEquipmentSlotUIs.TryGetValue(slotTypeKey, out CharacterEquipmentSlotUI slotUIComponent))
                {
                    if (slotUIComponent != null) slotUIComponent.DisplayEquippedItem(equippedItem); // Actualizar el slot UI.
                }
            }
        }
        else { ClearCharacterEquipmentSlotsDisplay(); } // Limpiar si no hay datos de equipo.
    }

    // Inicializa los slots de UI para la vista del inventario en esta pantalla.
    private void InitializeInventoryForEquipmentScreen()
    {
        if (inventoryDisplayPanel_EquipmentScreen == null || inventorySlotUIPrefab_ForEquipScreen == null) return;
        // El contenedor de los slots es el propio panel derecho que tiene el GridLayoutGroup.
        Transform containerTransform = inventoryDisplayPanel_EquipmentScreen.transform;

        // Limpiar slots antiguos para evitar duplicados si este método se llama más de una vez.
        foreach (Transform child in containerTransform) Destroy(child.gameObject);
        _inventorySlotUIs_EquipmentScreen.Clear(); // Limpiar la lista de referencias.

        // Determinar cuántos slots visuales crear. Idealmente, basado en PlayerInventory.maxInventorySlots.
        int slotsToDisplay = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 20; // Usar 20 como fallback.

        // Instanciar un prefab de slot por cada slot a mostrar.
        for (int i = 0; i < slotsToDisplay; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotUIPrefab_ForEquipScreen, containerTransform); // Instanciar como hijo del contenedor.
            slotGO.name = "InventorySlotUI_EquipScreen_" + i; // Ponerle un nombre descriptivo en la Jerarquía.
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>(); // Obtener el script del slot.
            if (slotUIComponent != null)
            {
                _inventorySlotUIs_EquipmentScreen.Add(slotUIComponent); // Añadir a la lista de slots de UI.
                // Suscribir el método HandleInventorySlotSelectionOnEquipScreen de ESTE manager al evento OnSlotClickedAction del slot UI.
                slotUIComponent.OnSlotClickedAction = HandleInventorySlotSelectionOnEquipScreen;
                slotUIComponent.ClearSlotDisplay(); // Asegurar que el slot se vea vacío inicialmente.
            }
            else Debug.LogError("ESM: El prefab 'inventorySlotUIPrefab_ForEquipScreen' no tiene el componente InventorySlotUI.", this);
        }
    }

    // Actualiza la visualización de los slots del inventario en esta pantalla.
    public void RefreshInventoryForEquipmentScreen(bool filter = false)
    { // El parámetro 'filter' no se usa aún.
        if (inventoryDisplayPanel_EquipmentScreen == null || PlayerInventory.Instance == null) return;
        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots; // Obtener los datos actuales del inventario del jugador.

        // Comprobar si el número de slots UI creados coincide con el máximo del inventario.
        int expectedSlots = PlayerInventory.Instance != null ? PlayerInventory.Instance.maxInventorySlots : 0;
        if (_inventorySlotUIs_EquipmentScreen.Count != expectedSlots && expectedSlots > 0)
        {
            InitializeInventoryForEquipmentScreen(); // Re-inicializar si es necesario.
        }

        // Iterar por todos los slots de UI que hemos creado para esta pantalla.
        for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
        {
            if (i < playerSlotsData.Count)
            { // Si hay un objeto en el inventario del jugador para este slot de UI.
                _inventorySlotUIs_EquipmentScreen[i].gameObject.SetActive(true); // Asegurar que el slot esté activo.
                _inventorySlotUIs_EquipmentScreen[i].UpdateSlotDisplay(playerSlotsData[i]); // Actualizar el slot de UI con los datos del objeto.
            }
            else
            { // Si no hay más objetos en el inventario para los slots de UI restantes.
                _inventorySlotUIs_EquipmentScreen[i].ClearSlotDisplay(); // Limpiar el slot de UI para que se vea vacío.
            }
        }
    }

    // --- MANEJO DE CLICS EN SLOTS ---

    // Método llamado por los InventorySlotUI del panel derecho (inventario) de ESTA pantalla.
    private void HandleInventorySlotSelectionOnEquipScreen(InventorySlot slotData, RectTransform slotRectTransform)
    {
        if (itemInfoActionPanel == null) return; // Si no hay panel de info, no hacer nada.

        // Si se hizo clic en un slot vacío del inventario.
        if (slotData == null || slotData.item == null)
        {
            HideItemInfoActionPanel(); // Ocultar el panel de info.
            _currentlySelectedItemFromInventory = null; // No hay ítem seleccionado.
            _currentlySelectedItemFromEquipment = null; // Asegurar que no haya selección de equipo.
            return;
        }

        // Si el panel de info ya está activo Y se hizo clic en el MISMO ítem del inventario que ya estaba seleccionado.
        if (itemInfoActionPanel.activeSelf && _currentlySelectedItemFromInventory == slotData && _currentlySelectedItemFromEquipment == null)
        {
            HideItemInfoActionPanel(); // Ocultar el panel (comportamiento de toggle).
            return;
        }

        _currentlySelectedItemFromInventory = slotData; // Guardar el slot del inventario que fue clickeado.
        _currentlySelectedItemFromEquipment = null;   // Limpiar cualquier selección previa de un ítem equipado.
        _currentlySelectedEquipmentSlot = EquipmentSlot.None; // No hay un slot de equipo del personaje seleccionado.
        DisplayItemInfoAndActions(slotData.item, slotRectTransform, false); // Mostrar info, 'false' porque viene del inventario.
    }

    // Método público llamado por los CharacterEquipmentSlotUI del panel izquierdo.
    public void OnCharacterEquipmentSlotClicked(EquipmentSlot slotType, ItemData equippedItemData, RectTransform slotRectTransform)
    {
        if (itemInfoActionPanel == null) return; // Si no hay panel de info, no hacer nada.
        Debug.Log("ESM: Clic en slot de equipo del personaje: " + slotType);

        _currentlySelectedItemFromInventory = null; // Limpiar cualquier selección previa del inventario.
        _currentlySelectedEquipmentSlot = slotType; // Guardar el slot de equipo del personaje que fue clickeado.

        // Obtener el ítem realmente equipado desde los datos del personaje actual.
        ItemData actualItemInSlot = null;
        if (_currentlyDisplayedCharacter != null && _currentlyDisplayedCharacter.equippedItems != null)
        {
            _currentlyDisplayedCharacter.equippedItems.TryGetValue(slotType, out actualItemInSlot);
        }

        if (actualItemInSlot != null) // Si hay un ítem equipado en este slot del personaje.
        {
            _currentlySelectedItemFromEquipment = actualItemInSlot; // Guardar el ítem que está equipado.
            // Mostrar el panel de información para este ítem equipado.
            // 'true' porque el ítem viene de un slot de equipo del personaje.
            DisplayItemInfoAndActions(actualItemInSlot, slotRectTransform, true);
        }
        else // Si el slot de equipo del personaje está vacío.
        {
            Debug.Log("ESM: Slot de equipo " + slotType + " está vacío.");
            _currentlySelectedItemFromEquipment = null; // No hay ítem de equipo seleccionado.

            // Si teníamos un ítem seleccionado previamente del inventario Y es compatible con este slot vacío,
            // intentar equiparlo directamente.
            if (_currentlySelectedItemFromInventory != null &&
                _currentlySelectedItemFromInventory.item != null &&
                _currentlySelectedItemFromInventory.item.isEquipable &&
                _currentlySelectedItemFromInventory.item.equipmentSlot == slotType)
            {
                Debug.Log("ESM: Slot de equipo vacío, intentando equipar ítem del inventario: " + _currentlySelectedItemFromInventory.item.itemName + " en slot " + slotType);
                PerformEquipAction(_currentlySelectedItemFromInventory.item, slotType);
            }
            else
            {
                // Si el slot está vacío y no hay nada del inventario para equipar, ocultar el panel de info.
                HideItemInfoActionPanel();
            }
        }
    }

    // Muestra el panel de información del ítem y configura sus botones.
    private void DisplayItemInfoAndActions(ItemData item, RectTransform clickedUITransform, bool isFromEquipmentSlot)
    {
        if (itemInfoActionPanel == null || item == null) { HideItemInfoActionPanel(); return; }

        // Poblar los campos de texto del panel de información.
        if (itemNameText_InfoPanel != null) itemNameText_InfoPanel.text = item.itemName;
        if (itemDescriptionText_InfoPanel != null) itemDescriptionText_InfoPanel.text = item.description;

        // Mostrar Stats si el objeto es equipable.
        if (itemStatsText_InfoPanel != null)
        {
            if (item.isEquipable)
            {
                StringBuilder statsBuilder = new StringBuilder();
                if (item.attackBonus != 0) statsBuilder.AppendLine("Ataque: " + item.attackBonus);
                if (item.defenseBonus != 0) statsBuilder.AppendLine("Defensa: " + item.defenseBonus);
                // Añadir más stats si los tienes en ItemData.cs
                itemStatsText_InfoPanel.text = statsBuilder.ToString();
                itemStatsText_InfoPanel.gameObject.SetActive(statsBuilder.Length > 0); // Mostrar solo si hay stats
            }
            else itemStatsText_InfoPanel.gameObject.SetActive(false); // Ocultar si no es equipable
        }

        // Configurar visibilidad de los botones de acción según el contexto.
        if (useButton_InfoPanel != null) useButton_InfoPanel.gameObject.SetActive(item.isConsumable && !isFromEquipmentSlot); // "Usar" solo para consumibles del inventario.
        if (equipButton_InfoPanel != null) equipButton_InfoPanel.gameObject.SetActive(item.isEquipable && !isFromEquipmentSlot); // "Equipar" solo para equipables del inventario.
        if (unequipButton_InfoPanel != null) unequipButton_InfoPanel.gameObject.SetActive(item.isEquipable && isFromEquipmentSlot); // "Desequipar" solo para ítems ya equipados.
        if (discardButton_InfoPanel != null) discardButton_InfoPanel.gameObject.SetActive(!isFromEquipmentSlot); // "Tirar" solo para ítems del inventario.

        // Posicionar el panel de información al lado del slot/UI clickeado.
        if (clickedUITransform != null && itemInfoActionPanelRect != null)
        {
            itemInfoActionPanel.SetActive(true); // Asegurar que esté activo antes de calcular layout.
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemInfoActionPanelRect); // Forzar actualización si tiene LayoutGroups internos.

            Vector3[] slotCorners = new Vector3[4];
            clickedUITransform.GetWorldCorners(slotCorners); // Obtiene las esquinas del slot en coordenadas mundiales.
            // slotCorners[2] es la esquina superior derecha del slot.
            Vector2 targetPositionForInfoPanel = new Vector2(
                slotCorners[2].x + infoPanelOffsetX, // Posicionar a la derecha del slot.
                slotCorners[2].y + infoPanelOffsetY  // Alinear verticalmente con la parte superior del slot (o ajustar con offset).
            );
            itemInfoActionPanelRect.position = targetPositionForInfoPanel; // Establece la posición mundial del pivote del panel de info.
        }
        else if (itemInfoActionPanel != null && !itemInfoActionPanel.activeSelf)
        { // Si solo se están refrescando datos y el panel estaba oculto.
            itemInfoActionPanel.SetActive(true); // Mostrar el panel.
        }
        // Si ya estaba activo y no hay clickedUITransform (ej: refrescando datos), no se reposiciona.
    }

    // Oculta el panel de información del ítem y limpia las selecciones actuales.
    public void HideItemInfoActionPanel()
    {
        if (itemInfoActionPanel != null) itemInfoActionPanel.SetActive(false);
        _currentlySelectedItemFromInventory = null;
        _currentlySelectedItemFromEquipment = null;
        _currentlySelectedEquipmentSlot = EquipmentSlot.None;
    }

    // --- MÉTODOS LLAMADOS POR LOS BOTONES DEL PANEL DE INFORMACIÓN ---
    // Estos métodos ahora son públicos para ser enlazados a los botones desde el Inspector.

    public void OnUseItemClicked_InfoPanel()
    {
        // DEBUG: Para confirmar que este método específico se está llamando
        Debug.Log("ESM: OnUseItemClicked_InfoPanel - MÉTODO LLAMADO."); 
        // Solo se puede usar un ítem si fue seleccionado del inventario.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null && _currentlySelectedItemFromInventory.item.isConsumable)
        {

            // Debug.Log("ESM: Botón Usar presionado para (del inventario): " + _currentlySelectedItemFromInventory.item.itemName); // Log más verboso
            if (_currentlyDisplayedCharacter == null) // Necesitamos un personaje para aplicar el efecto.
            {
                Debug.LogError("ESM: No hay personaje seleccionado (_currentlyDisplayedCharacter es null) para usar el objeto " + _currentlySelectedItemFromInventory.item.itemName, this);
                return;
            }
            bool itemWasUsedSuccessfully = _currentlySelectedItemFromInventory.item.Use(_currentlyDisplayedCharacter);
            if (itemWasUsedSuccessfully)
            {
                // Debug.Log(_currentlySelectedItemFromInventory.item.itemName + " fue usado con éxito en " + _currentlyDisplayedCharacter.characterName); // Log más verboso
                PlayerInventory.Instance.RemoveItem(_currentlySelectedItemFromInventory.item, 1);
                // El evento OnInventoryChanged se encargará de refrescar la UI del inventario.
                // HandlePlayerInventoryChanged también ocultará el panel de info si el ítem se agotó.
            }
            else
            {
                Debug.Log(_currentlySelectedItemFromInventory.item.itemName + " no se pudo usar o no tuvo efecto (ej: HP ya al máximo).");
            }
        }
        else
        {
            Debug.LogWarning("ESM: OnUseItemClicked_InfoPanel - No hay un objeto consumible seleccionado del inventario, o no hay personaje objetivo.");
        }
    }

    public void OnEquipItemClicked_InfoPanel()
    {
        // Este método se llama cuando se presiona "Equipar" en un ítem seleccionado DEL INVENTARIO.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null &&
            _currentlySelectedItemFromInventory.item.isEquipable && _currentlyDisplayedCharacter != null)
        {
            ItemData itemToEquip = _currentlySelectedItemFromInventory.item;
            // El slot objetivo es el slot natural del ítem (ej: una espada va a MainHand).
            EquipmentSlot targetSlot = itemToEquip.equipmentSlot;

            // Debug.Log("ESM: Botón Equipar (desde inventario) para: " + itemToEquip.itemName + " en " + _currentlyDisplayedCharacter.characterName + ", slot: " + targetSlot); // Log más verboso
            PerformEquipAction(itemToEquip, targetSlot); // Llama al método auxiliar para equipar.
        }
        // HideItemInfoActionPanel(); // PerformEquipAction ya oculta el panel.
    }

    public void OnUnequipItemClicked_InfoPanel()
    {
        // Este método se llama cuando se presiona "Desequipar" en un ítem que estaba en un SLOT DE EQUIPO del personaje.
        if (_currentlyDisplayedCharacter != null && _currentlySelectedEquipmentSlot != EquipmentSlot.None &&
            _currentlySelectedItemFromEquipment != null && // Usar el ítem que estaba seleccionado desde el equipo.
            PlayerInventory.Instance != null)
        {
            // Debug.Log("ESM: Botón Desequipar para slot: " + _currentlySelectedEquipmentSlot + " que tiene " + _currentlySelectedItemFromEquipment.itemName); // Log más verboso

            // Llama al método UnequipItem del personaje, pasándole el slot del que se quiere desequipar.
            ItemData unequippedItem = _currentlyDisplayedCharacter.UnequipItem(_currentlySelectedEquipmentSlot, PlayerInventory.Instance);

            if (unequippedItem != null) // Si se desequipó algo con éxito.
            {
                UpdateCharacterStatsDisplay(); // Actualizar los stats del personaje en la UI.
                UpdateCharacterEquipmentSlotsDisplay(); // Actualizar los slots de equipo en la UI.
                // El inventario (panel derecho) se refrescará automáticamente por el evento OnInventoryChanged
                // que se dispara cuando el objeto vuelve al PlayerInventory.
            }
        }
        HideItemInfoActionPanel(); // Siempre ocultar el panel de info después de la acción.
    }

    public void OnDiscardItemClicked_InfoPanel()
    {
        // Este método se llama cuando se presiona "Tirar" en un ítem seleccionado DEL INVENTARIO.
        if (_currentlySelectedItemFromInventory != null && _currentlySelectedItemFromInventory.item != null && PlayerInventory.Instance != null)
        {
            ItemData itemToDiscard = _currentlySelectedItemFromInventory.item;
            int quantityToDiscard = _currentlySelectedItemFromInventory.quantity; // Tirar todo el stack del slot.
            // Debug.Log("ESM: Botón Tirar (desde inventario) para: " + itemToDiscard.itemName + " x" + quantityToDiscard); // Log más verboso
            PlayerInventory.Instance.RemoveItem(itemToDiscard, quantityToDiscard);
            // El evento OnInventoryChanged de PlayerInventory debería refrescar la UI de slots.
            // HandlePlayerInventoryChanged también se encargará de ocultar el panel de info si el ítem desaparece.
        }
        // No permitir tirar ítems directamente equipados (deben desequiparse primero).
        else if (_currentlySelectedItemFromEquipment != null)
        {
            Debug.LogWarning("ESM: Intentando tirar un ítem que está actualmente equipado (" + _currentlySelectedItemFromEquipment.itemName + "). Primero debe ser desequipado del personaje.");
        }
        HideItemInfoActionPanel(); // Ocultar el panel de info después de intentar tirar.
    }

    // Método auxiliar para la acción de equipar un ítem en un slot específico.
    private void PerformEquipAction(ItemData itemToEquip, EquipmentSlot targetSlot)
    {
        if (itemToEquip == null || _currentlyDisplayedCharacter == null || PlayerInventory.Instance == null || targetSlot == EquipmentSlot.None)
        {
            Debug.LogError("ESM: PerformEquipAction - Faltan datos necesarios o targetSlot es None.");
            return;
        }
        // Comprobar si el ítem es para el slot correcto.
        if (itemToEquip.equipmentSlot != targetSlot)
        {
            Debug.LogWarning($"ESM: {itemToEquip.itemName} (slot natural: {itemToEquip.equipmentSlot}) no es para el slot objetivo {targetSlot}. No se equipará.");
            return;
        }

        // 1. Quitar el objeto del inventario (asumimos que se equipa una unidad).
        bool removedFromInv = PlayerInventory.Instance.RemoveItem(itemToEquip, 1);
        if (removedFromInv)
        {
            // 2. Equipar en el personaje. El método EquipItem en Character.cs se encarga de
            //    desequipar el objeto anterior (si lo hay) y devolverlo al inventario.
            bool equipped = _currentlyDisplayedCharacter.EquipItem(itemToEquip, PlayerInventory.Instance);
            if (equipped)
            {
                // Debug.Log("ESM: " + itemToEquip.itemName + " equipado con éxito en " + targetSlot); // Log más verboso
                UpdateCharacterStatsDisplay(); // Actualizar stats del personaje en la UI.
                UpdateCharacterEquipmentSlotsDisplay(); // Actualizar los slots de equipo del personaje en la UI.
            }
            else
            {
                // Si no se pudo equipar (ej: restricción de clase en Character.EquipItem, o algún otro error),
                // devolver el objeto que se quitó del inventario.
                Debug.LogWarning("ESM: No se pudo equipar " + itemToEquip.itemName + " (EquipItem en Character devolvió false). Devolviendo al inventario.");
                PlayerInventory.Instance.AddItem(itemToEquip, 1);
            }
        }
        else
        {
            Debug.LogError("ESM: No se pudo quitar " + itemToEquip.itemName + " del inventario para equipar. ¿Realmente estaba en el inventario?");
        }

        _currentlySelectedItemFromInventory = null; // Limpiar la selección del inventario después de intentar equipar.
        HideItemInfoActionPanel(); // Ocultar el panel de info después de la acción.
    }

    // Método llamado por el evento OnInventoryChanged de PlayerInventory.
    private void HandlePlayerInventoryChanged()
    {
        // Debug.Log("ESM: Evento OnInventoryChanged de PlayerInventory recibido."); // Log más verboso
        if (equipmentScreenPanel != null && equipmentScreenPanel.activeSelf) // Solo si esta pantalla está activa.
        {
            RefreshInventoryForEquipmentScreen(); // Refrescar la vista del inventario en esta pantalla.

            // Si un ítem estaba seleccionado en el panel de información (y venía del inventario)
            // y ese ítem ya no está (o su cantidad es cero) en el inventario, ocultar el panel de información.
            if (_currentlySelectedItemFromInventory != null &&
                (_currentlySelectedItemFromInventory.item == null ||
                 _currentlySelectedItemFromInventory.quantity <= 0 ||
                 (PlayerInventory.Instance != null && !PlayerInventory.Instance.HasItem(_currentlySelectedItemFromInventory.item, 1))))
            {
                HideItemInfoActionPanel();
            }
        }
    }

    // --- MÉTODO PÚBLICO PARA SER LLAMADO DESDE InventoryUIManager ---
    /// <summary>
    /// Abre la pantalla de equipamiento y opcionalmente preselecciona un ítem del inventario
    /// para mostrar su panel de información.
    /// </summary>
    /// <param name="itemToPreselect">El ItemData del objeto a preseleccionar (puede ser null).</param>
    public void OpenForEquipping(ItemData itemToPreselect)
    {
        if (equipmentScreenPanel == null)
        {
            Debug.LogError("ESM: equipmentScreenPanel no está asignado. No se puede abrir la pantalla de equipamiento.");
            return;
        }

        // Activar el panel principal de equipamiento si no lo está ya.
        if (!equipmentScreenPanel.activeSelf)
        {
            // Llamar a ToggleEquipmentScreen en lugar de SetActive directamente
            // para asegurar que toda la lógica de apertura se ejecute.
            ToggleEquipmentScreen();
            // Si ToggleEquipmentScreen ya llama a SelectCharacterForDisplay y RefreshInventory,
            // no necesitamos hacerlo de nuevo aquí inmediatamente.
        }
        else
        {
            // Si ya estaba activo, igual queremos seleccionar el personaje y refrescar el inventario
            // por si el contexto cambió.
            Character characterToDisplay = null;
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) characterToDisplay = pm.GetComponent<Character>();
            SelectCharacterForDisplay(characterToDisplay);
            RefreshInventoryForEquipmentScreen();
        }


        if (itemToPreselect != null)
        {
            // Buscar el slot de UI que contiene este ítem para simular un clic o resaltarlo
            // y mostrar su panel de información.
            bool itemFoundAndDisplayed = false;
            // Iterar por los slots de UI del inventario en ESTA pantalla.
            for (int i = 0; i < _inventorySlotUIs_EquipmentScreen.Count; i++)
            {
                InventorySlotUI slotUI = _inventorySlotUIs_EquipmentScreen[i];
                if (slotUI.CurrentSlotData != null && slotUI.CurrentSlotData.item == itemToPreselect)
                {
                    // Encontramos el slot que contiene el ítem a preseleccionar.
                    // Llamamos a la lógica que se ejecutaría si el jugador hiciera clic en este slot.
                    HandleInventorySlotSelectionOnEquipScreen(slotUI.CurrentSlotData, slotUI.GetComponent<RectTransform>());
                    itemFoundAndDisplayed = true;
                    break;
                }
            }
            if (!itemFoundAndDisplayed)
            {
                Debug.LogWarning("ESM: Ítem " + itemToPreselect.itemName + " no encontrado en la UI del inventario de la pantalla de equipo para preseleccionar.");
                HideItemInfoActionPanel(); // Ocultar si no se encontró el ítem.
            }
        }
        else
        {
            HideItemInfoActionPanel(); // Si no hay ítem para preseleccionar, asegurar que el panel de info esté oculto.
        }
        // Debug.Log("Pantalla de Equipamiento abierta para equipar: " + (itemToPreselect != null ? itemToPreselect.itemName : "Ningún ítem preseleccionado")); // Log más verboso
    }
}
