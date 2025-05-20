using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// using TuJuego.Inventario; 

public class CharacterEquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuración del Slot de Equipo")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.None;

    [Header("Componentes de UI")]
    [SerializeField] private Image itemIconImage;
    // [SerializeField] private TextMeshProUGUI slotNameText; 

    private ItemData _currentlyEquippedItemData;
    public ItemData CurrentlyEquippedItem => _currentlyEquippedItemData;

    void Awake()
    {
        if (itemIconImage == null)
        {
            Transform iconTransform = transform.Find("EquippedItem_Icon");
            if (iconTransform != null)
            {
                itemIconImage = iconTransform.GetComponent<Image>();
            }
            if (itemIconImage == null)
            {
                Debug.LogError("CharacterEquipmentSlotUI: 'itemIconImage' no asignado/encontrado en " + gameObject.name, this);
            }
        }
        ClearDisplay();
    }

    public void DisplayEquippedItem(ItemData itemData)
    {
        _currentlyEquippedItemData = itemData;

        if (itemIconImage == null) return;

        if (_currentlyEquippedItemData != null)
        {
            itemIconImage.sprite = _currentlyEquippedItemData.icon;
            itemIconImage.enabled = true;
        }
        else
        {
            ClearDisplay();
        }
    }

    public void ClearDisplay()
    {
        _currentlyEquippedItemData = null;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CharacterEquipmentSlotUI: Clic detectado en slot: " + slotType + ", Item: " + (_currentlyEquippedItemData != null ? _currentlyEquippedItemData.itemName : "Vacío"));

        // --- NUEVA COMPROBACIÓN DE NULIDAD ---
        if (EquipmentScreenManager.Instance == null)
        {
            Debug.LogError("CharacterEquipmentSlotUI: EquipmentScreenManager.Instance ES NULL. No se puede llamar a OnCharacterEquipmentSlotClicked.");
            return; // Salir si la instancia no existe
        }
        // --- FIN NUEVA COMPROBACIÓN ---

        // Notificar al EquipmentScreenManager que este slot fue clickeado.
        EquipmentScreenManager.Instance.OnCharacterEquipmentSlotClicked(slotType, _currentlyEquippedItemData, this.GetComponent<RectTransform>());
    }

    public EquipmentSlot GetSlotType()
    {
        return slotType;
    }

    public void SetSlotType(EquipmentSlot type)
    {
        slotType = type;
    }
}
