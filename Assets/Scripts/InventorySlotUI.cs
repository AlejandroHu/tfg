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

    // Acción pública para que el manager que instancia este slot pueda suscribir un método.
    // Pasará los datos del slot y el RectTransform del slot clickeado.
    public Action<InventorySlot, RectTransform> OnSlotClickedAction;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>(); // Obtener el RectTransform de este slot

        if (itemIconImage == null)
        {
            // Intenta encontrarlo si es un hijo con un nombre específico.
            // Asegúrate de que "ItemIcon_Image" coincida con el nombre de tu GameObject hijo en el Prefab.
            Transform iconTransform = transform.Find("ItemIcon_Image");
            if (iconTransform != null) itemIconImage = iconTransform.GetComponent<Image>();

            if (itemIconImage == null)
                Debug.LogError("InventorySlotUI: 'itemIconImage' no asignado y no se encontró en los hijos de " + gameObject.name + ". Por favor, asígnalo en el Prefab.", this);
        }

        if (quantityText == null)
        {
            // Asegúrate de que "Quantity_Text" coincida con el nombre de tu GameObject hijo en el Prefab.
            Transform quantityTransform = transform.Find("Quantity_Text");
            if (quantityTransform != null) quantityText = quantityTransform.GetComponent<TextMeshProUGUI>();

            if (quantityText == null)
                Debug.LogError("InventorySlotUI: 'quantityText' no asignado y no se encontró en los hijos de " + gameObject.name + ". Por favor, asígnalo en el Prefab.", this);
        }
    }

    public void UpdateSlotDisplay(InventorySlot slotData)
    {
        _currentSlotData = slotData; // Guardar los datos del slot que este UI representa.

        if (itemIconImage == null || quantityText == null) return; // Salir si las referencias de UI no están listas.

        if (_currentSlotData != null && _currentSlotData.item != null && _currentSlotData.quantity > 0)
        {
            // --- Hay un objeto en este slot ---
            itemIconImage.sprite = _currentSlotData.item.icon; // Asigna el icono del objeto.
            itemIconImage.enabled = true;              // Asegúrate de que la imagen del icono sea visible.

            if (_currentSlotData.item.isStackable && _currentSlotData.quantity > 1)
            {
                quantityText.text = "x" + _currentSlotData.quantity.ToString(); // Muestra la cantidad.
                quantityText.gameObject.SetActive(true);       // Asegúrate de que el objeto de texto sea visible.
            }
            else
            {
                quantityText.gameObject.SetActive(false);      // Oculta el texto de cantidad.
            }
        }
        else
        {
            // --- El slot está vacío ---
            itemIconImage.sprite = null;  // Quita cualquier icono anterior.
            itemIconImage.enabled = false; // Oculta la imagen del icono.
            quantityText.gameObject.SetActive(false); // Oculta el texto de cantidad.
        }
    }

    public void ClearSlotDisplay()
    {
        _currentSlotData = null; // Limpiar los datos del slot.
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
        // Si hay una acción suscrita (OnSlotClickedAction no es null), invocarla.
        // Le pasamos los datos del slot actual (_currentSlotData, que puede ser null si el slot está vacío)
        // y el RectTransform de este slot UI.
        OnSlotClickedAction?.Invoke(_currentSlotData, _rectTransform);
    }
}
