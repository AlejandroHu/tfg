using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Para Action

// Asegúrate de que ItemData y CombatManager (si lo llamas directamente) sean accesibles
// using TuJuego.Inventario;
// using TuJuego.Combate;

public class CombatItemListItem_UI : MonoBehaviour
{
    [Header("Componentes de UI")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemQuantityText; // Para mostrar la cantidad
    [SerializeField] private Button itemButton;

    private ItemData _representedItemData;
    private int _itemQuantity;

    public Action<ItemData> OnItemSelectedCallback; // Callback al CombatManager

    void Awake()
    {
        if (itemButton == null) itemButton = GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
        else
        {
            Debug.LogError("CombatItemListItem_UI: No se encontró el componente Button.", this);
        }

        // Validaciones opcionales para los textos e imagen
        if (itemNameText == null) Debug.LogWarning("CombatItemListItem_UI: itemNameText no asignado.", this);
        if (itemIconImage == null) Debug.LogWarning("CombatItemListItem_UI: itemIconImage no asignado.", this);
        if (itemQuantityText == null) Debug.LogWarning("CombatItemListItem_UI: itemQuantityText no asignado.", this);
    }

    public void SetupItem(ItemData itemData, int quantity, Action<ItemData> callback)
    {
        _representedItemData = itemData;
        _itemQuantity = quantity;
        OnItemSelectedCallback = callback;

        if (_representedItemData == null)
        {
            if (itemNameText != null) itemNameText.text = "---";
            if (itemIconImage != null) itemIconImage.enabled = false;
            if (itemQuantityText != null) itemQuantityText.text = "";
            if (itemButton != null) itemButton.interactable = false;
            return;
        }

        if (itemNameText != null)
        {
            itemNameText.text = _representedItemData.itemName;
        }

        if (itemIconImage != null)
        {
            if (_representedItemData.icon != null)
            {
                itemIconImage.sprite = _representedItemData.icon;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.enabled = false;
            }
        }

        if (itemQuantityText != null)
        {
            itemQuantityText.text = "x" + _itemQuantity.ToString();
            itemQuantityText.gameObject.SetActive(true);
        }

        if (itemButton != null)
        {
            // Podrías deshabilitar el botón si la cantidad es 0, aunque el inventario no debería mostrarlo.
            itemButton.interactable = (_itemQuantity > 0);
        }
    }

    private void OnItemClicked()
    {
        if (_representedItemData != null && OnItemSelectedCallback != null && _itemQuantity > 0)
        {
            OnItemSelectedCallback.Invoke(_representedItemData);
        }
        else
        {
            Debug.LogWarning("CombatItemListItem_UI: No se puede seleccionar el ítem (datos nulos, callback nulo o cantidad cero).");
        }
    }
}
