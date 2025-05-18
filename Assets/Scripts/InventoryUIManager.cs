using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
// using TuJuego.Inventario; // Descomenta si tus clases de inventario están en este namespace
// using TopDown; // Si Character.cs está en el namespace TopDown

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("Panel Principal del Inventario")]
    [SerializeField] private GameObject inventoryPanelRoot;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private int maxSlotsToDisplay = 24;
    [SerializeField] private KeyCode toggleInventoryKey = KeyCode.I;

    [Header("Panel de Detalles/Acciones del Objeto")]
    [SerializeField] private GameObject itemInfoActionPanel;
    [SerializeField] private RectTransform itemInfoActionPanelRect;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemStatsText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button discardButton;
    [Tooltip("Opcional: Botón específico para cerrar el panel de información del ítem.")]
    [SerializeField] private Button closeInfoButton;

    [Header("Configuración de Posición del Panel de Info")]
    [SerializeField] private float infoPanelOffsetX = 10f;
    [SerializeField] private float infoPanelOffsetY = 0f;

    [Header("Referencias del Jugador")]
    [Tooltip("Arrastra aquí el GameObject de tu personaje jugador principal (el que tiene el script Character.cs).")]
    [SerializeField] private GameObject playerGameObject;
    private Character _playerCharacterComponent;

    private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();
    private InventorySlot _currentlySelectedSlotData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Validaciones
        if (inventoryPanelRoot == null) Debug.LogError("InventoryUIManager: 'inventoryPanelRoot' no asignado.", this);
        if (slotsContainer == null) Debug.LogError("InventoryUIManager: 'slotsContainer' no asignado.", this);
        if (inventorySlotPrefab == null) Debug.LogError("InventoryUIManager: 'inventorySlotPrefab' no asignado.", this);

        if (itemInfoActionPanel != null)
        {
            if (itemInfoActionPanelRect == null) itemInfoActionPanelRect = itemInfoActionPanel.GetComponent<RectTransform>();
            if (itemInfoActionPanelRect == null) Debug.LogError("InventoryUIManager: 'itemInfoActionPanel' (" + itemInfoActionPanel.name + ") no tiene un RectTransform o no está asignado a 'itemInfoActionPanelRect'.", this);
        }
        else
        {
            Debug.LogWarning("InventoryUIManager: 'itemInfoActionPanel' no asignado.", this);
        }
        if (itemNameText == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'itemNameText' no asignado.", this);
        if (itemDescriptionText == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'itemDescriptionText' no asignado.", this);
        if (itemStatsText == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'itemStatsText' no asignado.", this);
        if (useButton == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'useButton' no asignado.", this);
        if (equipButton == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'equipButton' no asignado.", this);
        if (discardButton == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'discardButton' no asignado.", this);
        if (closeInfoButton == null && itemInfoActionPanel != null) Debug.LogWarning("InventoryUIManager: 'closeInfoButton' no asignado.", this);

        if (playerGameObject != null)
        {
            _playerCharacterComponent = playerGameObject.GetComponent<Character>();
            if (_playerCharacterComponent == null)
            {
                Debug.LogError("InventoryUIManager: El 'playerGameObject' asignado no tiene un componente 'Character'. Las acciones 'Usar' y 'Equipar' podrían fallar.", this);
            }
        }
        else
        {
            Debug.LogWarning("InventoryUIManager: 'playerGameObject' no está asignado en el Inspector. Las acciones 'Usar' y 'Equipar' podrían no funcionar correctamente si requieren un personaje objetivo.", this);
        }
    }

    void OnEnable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged += HandleInventoryChanged;
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    void Start()
    {
        if (inventoryPanelRoot != null) inventoryPanelRoot.SetActive(false);
        if (itemInfoActionPanel != null) itemInfoActionPanel.SetActive(false);

        InitializeInventoryUI();

        if (useButton != null) useButton.onClick.AddListener(OnUseButtonClicked);
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipButtonClicked);
        if (discardButton != null) discardButton.onClick.AddListener(OnDiscardButtonClicked);
        if (closeInfoButton != null) closeInfoButton.onClick.AddListener(HideItemInfoActionPanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            ToggleInventoryPanel();
        }
        if (inventoryPanelRoot != null && inventoryPanelRoot.activeSelf &&
            itemInfoActionPanel != null && itemInfoActionPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            HideItemInfoActionPanel();
        }
    }

    private void InitializeInventoryUI()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        uiSlots.Clear();
        for (int i = 0; i < maxSlotsToDisplay; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsContainer);
            slotGO.name = "InventorySlotUI_" + i;
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                uiSlots.Add(slotUIComponent);
                slotUIComponent.ClearSlotDisplay();
            }
            else
                Debug.LogError("InventoryUIManager: El prefab 'inventorySlotPrefab' no tiene el componente InventorySlotUI.", this);
        }
    }

    private void HandleInventoryChanged()
    {
        if (inventoryPanelRoot != null && inventoryPanelRoot.activeSelf)
        {
            RefreshInventoryDisplay();
            if (_currentlySelectedSlotData != null &&
                (_currentlySelectedSlotData.item == null || _currentlySelectedSlotData.quantity <= 0 ||
                 (PlayerInventory.Instance != null && !PlayerInventory.Instance.HasItem(_currentlySelectedSlotData.item, 1))))
            {
                HideItemInfoActionPanel();
            }
            else if (_currentlySelectedSlotData != null && itemInfoActionPanel != null && itemInfoActionPanel.activeSelf)
            {
                DisplayItemInfoAndActions(_currentlySelectedSlotData, null);
            }
        }
    }

    public void RefreshInventoryDisplay()
    {
        if (PlayerInventory.Instance == null) return;
        List<InventorySlot> playerSlotsData = PlayerInventory.Instance.inventorySlots;
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < playerSlotsData.Count)
                uiSlots[i].UpdateSlotDisplay(playerSlotsData[i]);
            else
                uiSlots[i].ClearSlotDisplay();
        }
    }

    public void ToggleInventoryPanel()
    {
        if (inventoryPanelRoot == null) return;
        bool currentVisibility = inventoryPanelRoot.activeSelf;
        inventoryPanelRoot.SetActive(!currentVisibility);
        if (inventoryPanelRoot.activeSelf)
        {
            RefreshInventoryDisplay();
            HideItemInfoActionPanel();
        }
        else
        {
            HideItemInfoActionPanel();
        }
    }

    public void OnInventorySlotClicked(RectTransform clickedSlotRectTransform, InventorySlot clickedSlotData)
    {
        if (itemInfoActionPanel == null) return;

        if (clickedSlotData == null || clickedSlotData.item == null)
        {
            HideItemInfoActionPanel();
            _currentlySelectedSlotData = null;
            return;
        }

        if (itemInfoActionPanel.activeSelf && _currentlySelectedSlotData == clickedSlotData)
        {
            HideItemInfoActionPanel();
            return;
        }

        _currentlySelectedSlotData = clickedSlotData;
        DisplayItemInfoAndActions(clickedSlotData, clickedSlotRectTransform);
    }

    private void DisplayItemInfoAndActions(InventorySlot slotData, RectTransform clickedSlotUITransform)
    {
        if (itemInfoActionPanel == null || slotData == null || slotData.item == null)
        {
            HideItemInfoActionPanel();
            return;
        }
        ItemData item = slotData.item;
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        if (itemStatsText != null)
        {
            if (item.isEquipable)
            {
                StringBuilder statsBuilder = new StringBuilder();
                if (item.attackBonus != 0) statsBuilder.AppendLine("Ataque: " + item.attackBonus);
                if (item.defenseBonus != 0) statsBuilder.AppendLine("Defensa: " + item.defenseBonus);
                if (item.magicAttackBonus != 0) statsBuilder.AppendLine("Ata. Mág: " + item.magicAttackBonus);
                if (item.magicDefenseBonus != 0) statsBuilder.AppendLine("Def. Mág: " + item.magicDefenseBonus);
                if (item.speedBonus != 0) statsBuilder.AppendLine("Velocidad: " + item.speedBonus);
                if (item.maxHpBonus != 0) statsBuilder.AppendLine("HP Max: +" + item.maxHpBonus);
                if (item.maxMpBonus != 0) statsBuilder.AppendLine("MP Max: +" + item.maxMpBonus);
                itemStatsText.text = statsBuilder.ToString();
                itemStatsText.gameObject.SetActive(statsBuilder.Length > 0);
            }
            else
                itemStatsText.gameObject.SetActive(false);
        }
        if (useButton != null) useButton.gameObject.SetActive(item.isConsumable);
        if (equipButton != null) equipButton.gameObject.SetActive(item.isEquipable);
        if (discardButton != null) discardButton.gameObject.SetActive(true);

        if (clickedSlotUITransform != null && itemInfoActionPanelRect != null)
        {
            itemInfoActionPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemInfoActionPanelRect);
            Vector3[] slotCorners = new Vector3[4];
            clickedSlotUITransform.GetWorldCorners(slotCorners);
            Vector2 targetPositionForInfoPanel = new Vector2(
                slotCorners[2].x + infoPanelOffsetX,
                slotCorners[2].y + infoPanelOffsetY
            );
            itemInfoActionPanelRect.position = targetPositionForInfoPanel;
        }
        else if (itemInfoActionPanel != null && !itemInfoActionPanel.activeSelf)
        {
            itemInfoActionPanel.SetActive(true);
        }
    }

    public void HideItemInfoActionPanel()
    {
        if (itemInfoActionPanel != null)
        {
            itemInfoActionPanel.SetActive(false);
        }
        _currentlySelectedSlotData = null;
    }

    public void OnUseButtonClicked()
    {
        if (_currentlySelectedSlotData != null && _currentlySelectedSlotData.item != null && _currentlySelectedSlotData.item.isConsumable)
        {
            Debug.Log("Botón Usar presionado para: " + _currentlySelectedSlotData.item.itemName);
            if (_playerCharacterComponent == null)
            {
                Debug.LogError("InventoryUIManager: _playerCharacterComponent no está asignado. No se puede usar el objeto.", this);
                return;
            }
            bool itemWasUsedSuccessfully = _currentlySelectedSlotData.item.Use(_playerCharacterComponent);
            if (itemWasUsedSuccessfully)
            {
                Debug.Log(_currentlySelectedSlotData.item.itemName + " fue usado con éxito.");
                PlayerInventory.Instance.RemoveItem(_currentlySelectedSlotData.item, 1);
            }
            else
            {
                Debug.Log(_currentlySelectedSlotData.item.itemName + " no se pudo usar o no tuvo efecto.");
            }
        }
        else
        {
            Debug.LogWarning("OnUseButtonClicked: No hay un objeto consumible seleccionado o _playerCharacterComponent no está asignado.");
        }
    }

    public void OnEquipButtonClicked()
    {
        if (_currentlySelectedSlotData != null && _currentlySelectedSlotData.item != null && _currentlySelectedSlotData.item.isEquipable)
        {
            Debug.Log("Botón Equipar presionado para: " + _currentlySelectedSlotData.item.itemName);

            if (_playerCharacterComponent == null)
            {
                Debug.LogError("InventoryUIManager: _playerCharacterComponent no está asignado. No se puede equipar el objeto.", this);
                HideItemInfoActionPanel();
                return;
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("InventoryUIManager: PlayerInventory.Instance no encontrado. No se puede proceder con el equipamiento.", this);
                HideItemInfoActionPanel();
                return;
            }

            ItemData itemToEquip = _currentlySelectedSlotData.item;
            bool removedFromInventory = PlayerInventory.Instance.RemoveItem(itemToEquip, 1);

            if (removedFromInventory)
            {
                bool equippedSuccessfully = _playerCharacterComponent.EquipItem(itemToEquip, PlayerInventory.Instance);
                if (equippedSuccessfully)
                {
                    Debug.Log(itemToEquip.itemName + " equipado en " + _playerCharacterComponent.characterName);
                }
                else
                {
                    Debug.LogWarning(itemToEquip.itemName + " no se pudo equipar en " + _playerCharacterComponent.characterName + ". Devolviendo al inventario.");
                    PlayerInventory.Instance.AddItem(itemToEquip, 1);
                }
            }
            else
            {
                Debug.LogError("InventoryUIManager: No se pudo quitar " + itemToEquip.itemName + " del inventario antes de equipar.");
            }
            HideItemInfoActionPanel();
        }
        else
        {
            Debug.LogWarning("OnEquipButtonClicked: No hay un objeto equipable seleccionado.");
        }
    }

    public void OnDiscardButtonClicked()
    {
        // --- Comprobaciones específicas para el descarte ---
        if (_currentlySelectedSlotData == null)
        {
            Debug.LogError("OnDiscardButtonClicked: _currentlySelectedSlotData ES NULL al entrar al método. No se puede tirar.");
            return;
        }
        if (_currentlySelectedSlotData.item == null)
        {
            Debug.LogError("OnDiscardButtonClicked: _currentlySelectedSlotData.item ES NULL. No hay objeto que tirar.");
            return;
        }
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("OnDiscardButtonClicked: PlayerInventory.Instance ES NULL. No se puede acceder al inventario para tirar.");
            return;
        }
        // --- Fin Comprobaciones ---

        ItemData itemToDiscard = _currentlySelectedSlotData.item;
        int quantityToDiscard = _currentlySelectedSlotData.quantity;

        Debug.Log("Botón Tirar presionado para: " + itemToDiscard.itemName + " x" + quantityToDiscard);

        bool removedSuccessfully = PlayerInventory.Instance.RemoveItem(itemToDiscard, quantityToDiscard);

        if (removedSuccessfully)
        {
            Debug.Log(quantityToDiscard + " de " + itemToDiscard.itemName + " tirados/eliminados del inventario.");
        }
        else
        {
            Debug.LogError("InventoryUIManager: Error al intentar tirar " + itemToDiscard.itemName + ". El objeto no pudo ser eliminado del inventario (quizás la cantidad cambió inesperadamente o ya no existía).");
        }

        HideItemInfoActionPanel();
    }
}

