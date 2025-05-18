using UnityEngine;
using UnityEngine.UI;       // Necesario para el componente Image
using TMPro;                // Necesario para TextMeshProUGUI
using UnityEngine.EventSystems; // NECESARIO para detectar clics en UI (IPointerClickHandler)

// Puedes poner esto en un namespace si estás organizando así tu código
// namespace TuJuego.UI.Inventario
// {

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Componentes de UI del Slot")]
    [Tooltip("Arrastra aquí la Image que mostrará el icono del objeto.")]
    [SerializeField] private Image itemIconImage;

    [Tooltip("Arrastra aquí el TextMeshProUGUI que mostrará la cantidad del objeto.")]
    [SerializeField] private TextMeshProUGUI quantityText;

    private InventorySlot _currentSlotData;
    public InventorySlot CurrentSlotData => _currentSlotData;

    private RectTransform _rectTransform; // Guardar referencia a nuestro RectTransform

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>(); // Obtener el RectTransform de este slot

        if (itemIconImage == null)
        {
            Transform iconTransform = transform.Find("ItemIcon_Image"); // Usa el nombre exacto de tu GameObject hijo
            if (iconTransform != null) itemIconImage = iconTransform.GetComponent<Image>();
            if (itemIconImage == null)
                Debug.LogError("InventorySlotUI: 'itemIconImage' no asignado/encontrado en " + gameObject.name + ". Por favor, asígnalo en el Prefab.", this);
        }

        if (quantityText == null)
        {
            Transform quantityTransform = transform.Find("Quantity_Text"); // Usa el nombre exacto de tu GameObject hijo
            if (quantityTransform != null) quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();
            if (quantityText == null)
                Debug.LogError("InventorySlotUI: 'quantityText' no asignado/encontrado en " + gameObject.name + ". Por favor, asígnalo en el Prefab.", this);
        }
    }

    public void UpdateSlotDisplay(InventorySlot slotData)
    {
        _currentSlotData = slotData;

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

    // --- MÉTODO DE LA INTERFAZ IPointerClickHandler ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance == null)
        {
            Debug.LogError("InventorySlotUI: InventoryUIManager.Instance es null. Asegúrate de que esté en la escena y se inicialice correctamente.");
            return;
        }

        // Llamar al método en InventoryUIManager pasando AMBOS parámetros:
        // el RectTransform de este slot y los datos del slot (_currentSlotData, que puede ser null si el slot está vacío).
        InventoryUIManager.Instance.OnInventorySlotClicked(_rectTransform, _currentSlotData);
    }
}
// } // Fin del namespace si lo usas
