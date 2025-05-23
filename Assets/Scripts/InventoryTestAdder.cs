using UnityEngine;

public class InventoryTestAdder : MonoBehaviour
{
    [Header("Objetos de Prueba")]
    [Tooltip("Arrastra aquí tu asset de ItemData para la 'Espada de Hierro' desde la ventana de Proyecto.")]
    public ItemData itemEspadaDeHierro;

    [Tooltip("Arrastra aquí tu asset de ItemData para la 'Espada de Acero' desde la ventana de Proyecto.")]
    public ItemData itemEspadaDeAcero;



    [Header("Teclas de Prueba")]
    [Tooltip("Tecla para añadir la Espada de Hierro.")]
    [SerializeField] private KeyCode addEspadaHierroKey = KeyCode.Alpha6; // Ejemplo: Tecla 6

    [Tooltip("Tecla para añadir la Espada de Acero.")]
    [SerializeField] private KeyCode addEspadaAceroKey = KeyCode.Alpha7;  // Ejemplo: Tecla 7

    // [Tooltip("Tecla para añadir Poción (si la tienes).")]
    // [SerializeField] private KeyCode addPocionKey = KeyCode.Alpha8;

    [Tooltip("Tecla para imprimir el contenido del inventario en la consola.")]
    [SerializeField] private KeyCode printInventoryKey = KeyCode.Alpha0;


    void Update()
    {
        // Añadir Espada de Hierro
        if (Input.GetKeyDown(addEspadaHierroKey))
        {
            if (itemEspadaDeHierro != null && PlayerInventory.Instance != null)
            {
                // Las armas no suelen apilarse, así que la cantidad suele ser 1.
                // PlayerInventory.AddItem se encargará de esto si itemEspadaDeHierro.isStackable es false.
                bool anadido = PlayerInventory.Instance.AddItem(itemEspadaDeHierro, 1);
                LogResultado(anadido, itemEspadaDeHierro.itemName, 1, "añadir");
            }
            else
            {
                if (itemEspadaDeHierro == null) Debug.LogWarning("InventoryTestAdder: 'itemEspadaDeHierro' no está asignado en el Inspector.");
                if (PlayerInventory.Instance == null) Debug.LogWarning("InventoryTestAdder: PlayerInventory.Instance no encontrado.");
            }
        }

        // Añadir Espada de Acero
        if (Input.GetKeyDown(addEspadaAceroKey))
        {
            if (itemEspadaDeAcero != null && PlayerInventory.Instance != null)
            {
                bool anadido = PlayerInventory.Instance.AddItem(itemEspadaDeAcero, 1);
                LogResultado(anadido, itemEspadaDeAcero.itemName, 1, "añadir");
            }
            else
            {
                if (itemEspadaDeAcero == null) Debug.LogWarning("InventoryTestAdder: 'itemEspadaDeAcero' no está asignado en el Inspector.");
                if (PlayerInventory.Instance == null) Debug.LogWarning("InventoryTestAdder: PlayerInventory.Instance no encontrado.");
            }
        }

        // Ejemplo para añadir poción 
        /*
        if (Input.GetKeyDown(addPocionKey)) 
        {
            if (itemPocion != null && PlayerInventory.Instance != null)
            {
                bool anadido = PlayerInventory.Instance.AddItem(itemPocion, cantidadPocion);
                LogResultado(anadido, itemPocion.itemName, cantidadPocion, "añadir");
            }
        }
        */

        // Imprimir Inventario
        if (Input.GetKeyDown(printInventoryKey))
        {
            PrintInventory();
        }
    }

    // Método auxiliar para mostrar el resultado de la operación en consola
    void LogResultado(bool exito, string itemName, int cantidad, string accion)
    {
        if (exito)
        {
            Debug.Log($"Se intentó {accion} {cantidad} de {itemName}. Éxito. Contenido actual del inventario:");
            PrintInventory(); // Imprimir inventario después de una acción exitosa
        }
        else
        {
            Debug.Log($"No se pudo {accion} {cantidad} de {itemName}. ¿Inventario lleno o no había suficientes para quitar?");
        }
    }

    // Método para imprimir el contenido actual del inventario en la consola
    void PrintInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            Debug.Log("--- Contenido del Inventario ---");
            if (PlayerInventory.Instance.inventorySlots.Count == 0)
            {
                Debug.Log("Inventario Vacío.");
            }
            foreach (var slot in PlayerInventory.Instance.inventorySlots)
            {
                // Asegurarse de que el slot y el item dentro del slot no sean nulos
                if (slot != null && slot.item != null)
                {
                    Debug.Log($"- {slot.item.itemName} x {slot.quantity}");
                }
                else if (slot == null)
                {
                    Debug.Log("- Slot nulo detectado en la lista (esto podría indicar un problema en cómo se añaden/quitan slots).");
                }
                
            }
            Debug.Log("-----------------------------");
        }
        else
        {
            Debug.Log("PlayerInventory.Instance no encontrado al intentar imprimir inventario.");
        }
    }
}
