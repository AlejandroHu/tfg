using UnityEngine;
using UnityEngine.UI;       // Necesario para el componente Image
using TMPro;                // Necesario para TextMeshProUGUI (si decides añadir texto al slot)
using UnityEngine.EventSystems; // Necesario para detectar clics (IPointerClickHandler)

// Asegúrate de que el namespace de ItemData y EquipmentSlot sea accesible
// Si los tienes en un namespace como "TuJuego.Inventario", deberías añadir:
// using TuJuego.Inventario; 

public class CharacterEquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuración del Slot de Equipo")]
    [Tooltip("El tipo de slot de equipamiento que este UI representa (ej: Head, Body). Esto se DEBE configurar en el Inspector para cada instancia de slot en la escena.")]
    [SerializeField] private EquipmentSlot slotType = EquipmentSlot.None;

    [Header("Componentes de UI")]
    [Tooltip("La Image que mostrará el icono del objeto equipado. Debe ser un hijo de este GameObject.")]
    [SerializeField] private Image itemIconImage;
    // [Tooltip("Opcional: TextMeshProUGUI para el nombre del slot, si quieres mostrarlo (ej: 'Casco').")]
    // [SerializeField] private TextMeshProUGUI slotNameText; 

    private ItemData _currentlyEquippedItemData; // El ItemData del objeto actualmente equipado en este slot

    // Awake se llama cuando la instancia del script se carga.
    void Awake()
    {
        // Es una buena práctica obtener la referencia al componente Image del icono si no está asignada en el Inspector.
        if (itemIconImage == null)
        {
            // Intenta encontrarlo como un hijo llamado "EquippedItem_Icon".
            // ¡Asegúrate de que el nombre "EquippedItem_Icon" coincida exactamente con el nombre de tu GameObject hijo en el Prefab!
            Transform iconTransform = transform.Find("EquippedItem_Icon");
            if (iconTransform != null)
            {
                itemIconImage = iconTransform.GetComponent<Image>();
            }
            // Si sigue siendo null después de buscar, muestra un error.
            if (itemIconImage == null)
            {
                Debug.LogError("CharacterEquipmentSlotUI: 'itemIconImage' no está asignado y no se encontró un hijo llamado 'EquippedItem_Icon' en " + gameObject.name + ". Por favor, asígnalo en el Prefab o en la instancia.", this);
            }
        }

        // Inicialmente, el slot podría estar vacío o mostrar un placeholder si lo deseas.
        ClearDisplay();
    }

    /// <summary>
    /// Actualiza la visualización de este slot de equipo con un objeto específico.
    /// Este método será llamado por EquipmentScreenManager.
    /// </summary>
    /// <param name="itemData">El ItemData del objeto a mostrar. Puede ser null si el slot está vacío.</param>
    public void DisplayEquippedItem(ItemData itemData)
    {
        _currentlyEquippedItemData = itemData; // Guarda la referencia al objeto equipado.

        if (itemIconImage == null) return; // Salir si no hay referencia a la imagen del icono para evitar errores.

        if (_currentlyEquippedItemData != null) // Si hay un objeto para mostrar.
        {
            itemIconImage.sprite = _currentlyEquippedItemData.icon; // Asigna el icono del objeto.
            itemIconImage.enabled = true; // Asegúrate de que la imagen del icono sea visible.
        }
        else // Si no hay objeto (slot vacío).
        {
            ClearDisplay(); // Llama a ClearDisplay para mostrar el slot como vacío.
        }
    }

    /// <summary>
    /// Limpia la visualización del slot, haciéndolo parecer vacío.
    /// </summary>
    public void ClearDisplay()
    {
        _currentlyEquippedItemData = null; // No hay objeto equipado.
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;  // Quita cualquier icono anterior.
            itemIconImage.enabled = false; // Oculta la imagen del icono (o podrías poner un sprite de "slot vacío" aquí).
        }
    }

    /// <summary>
    /// Se llama automáticamente por el sistema de Eventos de Unity cuando se hace clic 
    /// en este GameObject de UI (si el Canvas tiene un Graphic Raycaster y este objeto 
    /// tiene un componente UI que pueda recibir rayos, como una Image).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clic en Slot de Equipo del Personaje: " + slotType.ToString() +
                  " - Objeto actual: " + (_currentlyEquippedItemData != null ? _currentlyEquippedItemData.itemName : "Vacío"));

        // Comprobar si existe una instancia del EquipmentScreenManager.
        if (EquipmentScreenManager.Instance != null)
        {
            // Notificar al EquipmentScreenManager que este slot fue clickeado.
            // El manager decidirá qué hacer (ej: intentar desequipar el objeto actual,
            // o si hay un objeto del inventario seleccionado, intentar equiparlo aquí).
            // Esta línea está comentada porque el método OnCharacterEquipmentSlotClicked aún no está
            // completamente implementado en EquipmentScreenManager para manejar la lógica completa.
            // EquipmentScreenManager.Instance.OnCharacterEquipmentSlotClicked(slotType, _currentlyEquippedItemData); 

            // --- LÓGICA DE INTERACCIÓN TEMPORAL O FUTURA ---
            // Si hay un objeto equipado, podríamos intentar desequiparlo.
            // Si no hay objeto y hay algo seleccionado en el inventario, podríamos intentar equiparlo.
            // Esta lógica se manejará de forma más centralizada en EquipmentScreenManager.
            // Por ahora, el Debug.Log es suficiente para saber que el clic funciona.
        }
        else
        {
            Debug.LogWarning("CharacterEquipmentSlotUI: No se encontró la instancia de EquipmentScreenManager.");
        }
    }

    /// <summary>
    /// Permite al EquipmentScreenManager (u otros scripts) saber qué tipo de slot de equipo es este.
    /// </summary>
    public EquipmentSlot GetSlotType()
    {
        return slotType;
    }

    /// <summary>
    /// Permite al EquipmentScreenManager configurar el tipo de slot,
    /// especialmente si estos slots de UI se instancian dinámicamente por código.
    /// </summary>
    /// <param name="type">El EquipmentSlot que este UI representará.</param>
    public void SetSlotType(EquipmentSlot type)
    {
        slotType = type;
        // Opcional: Si tuvieras un TextMeshProUGUI para mostrar el nombre del slot (ej: "Casco")
        // if (slotNameText != null) slotNameText.text = slotType.ToString(); 
    }
}
