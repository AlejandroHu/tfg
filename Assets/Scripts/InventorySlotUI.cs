using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System; // Necesario para System.Action

// Asegúrate de que el namespace de ItemData y InventorySlot sea accesible
// using TuJuego.Inventario;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Componentes de UI del Slot")]
    [Tooltip("Arrastra aquí la Image que mostrará el icono del objeto.")]
    [SerializeField] private Image itemIconImage;
    [Tooltip("Arrastra aquí el TextMeshProUGUI que mostrará la cantidad del objeto.")]
    [SerializeField] private TextMeshProUGUI quantityText;

    private InventorySlot _currentSlotData;
    public InventorySlot CurrentSlotData => _currentSlotData;
    private RectTransform _rectTransform;

    public Action<InventorySlot, RectTransform> OnSlotClickedAction;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (itemIconImage == null)
        {
            Transform iconTransform = transform.Find("ItemIcon_Image");
            if (iconTransform != null) itemIconImage = iconTransform.GetComponent<Image>();
            if (itemIconImage == null) Debug.LogError("InventorySlotUI (" + gameObject.name + "): 'itemIconImage' no asignado/encontrado.", this);
        }
        if (quantityText == null)
        {
            Transform quantityTransform = transform.Find("Quantity_Text");
            if (quantityTransform != null) quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();
            if (quantityText == null) Debug.LogError("InventorySlotUI (" + gameObject.name + "): 'quantityText' no asignado/encontrado.", this);
        }
        // --- DEBUG: Para ver cuándo se crea cada slot UI y su estado inicial ---
        // Debug.Log($"InventorySlotUI ({gameObject.name}): Awake. _currentSlotData es {( (_currentSlotData == null) ? "NULL" : (_currentSlotData.item != null ? _currentSlotData.item.itemName : "ITEM NULL") )}");
    }

    public void UpdateSlotDisplay(InventorySlot slotData)
    {
        string gameObjectName = gameObject.name;
        // --- DEBUG: Qué datos está recibiendo este slot UI para mostrar ---
        if (slotData != null && slotData.item != null)
        {
            Debug.Log($"InventorySlotUI ({gameObjectName}): UpdateSlotDisplay RECIBIENDO Item: {slotData.item.itemName}, Cantidad: {slotData.quantity}, SlotData HashCode: {slotData.GetHashCode()}");
        }
        else if (slotData != null && slotData.item == null)
        {
            Debug.Log($"InventorySlotUI ({gameObjectName}): UpdateSlotDisplay RECIBIENDO slotData con item NULL, Cantidad: {slotData.quantity}, SlotData HashCode: {slotData.GetHashCode()}");
        }
        else
        {
            Debug.Log($"InventorySlotUI ({gameObjectName}): UpdateSlotDisplay RECIBIENDO slotData NULL (limpiando).");
        }

        _currentSlotData = slotData; // Asignación de la referencia

        // --- DEBUG: Qué datos tiene _currentSlotData INMEDIATAMENTE DESPUÉS de la asignación ---
        if (_currentSlotData != null && _currentSlotData.item != null)
        {
            Debug.Log($"InventorySlotUI ({gameObjectName}): UpdateSlotDisplay ASIGNADO. _currentSlotData.item: {_currentSlotData.item.itemName}, Cantidad: {_currentSlotData.quantity}, _currentSlotData HashCode: {_currentSlotData.GetHashCode()}");
        }
        else
        {
            Debug.Log($"InventorySlotUI ({gameObjectName}): UpdateSlotDisplay ASIGNADO. _currentSlotData ahora es NULL.");
        }

        // Actualizar la UI visual
        if (itemIconImage == null || quantityText == null) return;

        if (_currentSlotData != null && _currentSlotData.item != null && _currentSlotData.quantity > 0)
        {
            itemIconImage.sprite = _currentSlotData.item.icon;
            itemIconImage.enabled = true;
            if (_currentSlotData.item.isStackable && _currentSlotData.quantity > 1)
            {
                quantityText.text = "x" + _currentSlotData.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
            quantityText.gameObject.SetActive(false);
        }
    }

    public void ClearSlotDisplay()
    {
        Debug.Log($"InventorySlotUI ({gameObject.name}): ClearSlotDisplay llamado.");

        _currentSlotData = null;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
            quantityText.text = "";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        string itemNameOnClick = "N/A";
        int quantityOnClick = 0;
        int slotDataHashCodeOnClick = 0;

        if (_currentSlotData != null && _currentSlotData.item != null)
        {
            itemNameOnClick = _currentSlotData.item.itemName;
            quantityOnClick = _currentSlotData.quantity;
            slotDataHashCodeOnClick = _currentSlotData.GetHashCode();
        }
        else if (_currentSlotData != null && _currentSlotData.item == null)
        {
            itemNameOnClick = "ITEM ES NULL";
            quantityOnClick = _currentSlotData.quantity;
            slotDataHashCodeOnClick = _currentSlotData.GetHashCode();
        }
        else
        {
            itemNameOnClick = "SLOTDATA ES NULL";
        }
        // --- DEBUG: Qué datos tiene _currentSlotData en el momento del clic y su HashCode ---
        Debug.Log($"InventorySlotUI ({gameObject.name}): OnPointerClick. _currentSlotData.item es: {itemNameOnClick}, Cantidad: {quantityOnClick}, _currentSlotData HashCode: {slotDataHashCodeOnClick}");

        OnSlotClickedAction?.Invoke(_currentSlotData, _rectTransform);
    }
}
