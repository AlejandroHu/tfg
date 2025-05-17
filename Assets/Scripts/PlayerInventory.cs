// Puedes poner esto en el mismo namespace que ItemData.cs y Character.cs si estás usando uno.
// namespace TuJuego.Inventario
// {

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un slot (ranura) individual dentro del inventario.
/// Contiene un tipo de objeto (ItemData) y la cantidad de ese objeto.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    [Tooltip("El tipo de objeto (ScriptableObject ItemData) en este slot.")]
    public ItemData item;
    [Tooltip("La cantidad de este objeto en el slot.")]
    public int quantity;

    public InventorySlot(ItemData itemData, int amount)
    {
        item = itemData;
        quantity = amount;
    }

    public void AddQuantity(int amountToAdd)
    {
        quantity += amountToAdd;
        if (item != null && item.isStackable && quantity > item.maxStackSize)
        {
            quantity = item.maxStackSize;
        }
    }

    public void RemoveQuantity(int amountToRemove)
    {
        quantity -= amountToRemove;
    }

    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }
}

/// <summary>
/// Gestiona el inventario del jugador, incluyendo la adición, eliminación y búsqueda de objetos.
/// Implementa el patrón Singleton para un acceso global fácil.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Configuración del Inventario")]
    [Tooltip("Número máximo de slots diferentes que puede tener el inventario.")]
    [SerializeField] private int maxInventorySlots = 20;

    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

    // --- Implementación del Patrón Singleton ---
    // 'Instance' es una propiedad estática que permitirá acceder a la única instancia de PlayerInventory.
    // 'static' significa que pertenece a la clase, no a una instancia específica.
    // '{ get; private set; }' significa que se puede leer desde cualquier script (get),
    // pero solo se puede modificar desde dentro de esta clase (private set).
    public static PlayerInventory Instance { get; private set; }

    // Awake se llama cuando la instancia del script se carga.
    void Awake()
    {
        // Lógica del Singleton:
        // Si ya existe una instancia de PlayerInventory (Instance != null)
        // y esa instancia no es esta misma instancia actual (Instance != this)...
        if (Instance != null && Instance != this)
        {
            // ...entonces ya hay un PlayerInventory en la escena. Destruimos este GameObject
            // para evitar duplicados y asegurar que solo haya una instancia.
            Debug.LogWarning("PlayerInventory: Se encontró otra instancia. Destruyendo este GameObject.");
            Destroy(gameObject);
            return; // Salimos de Awake para no ejecutar el resto.
        }
        // Si no había otra instancia, o si esta es la primera,
        // asignamos esta instancia actual a la propiedad estática 'Instance'.
        Instance = this;

        
        DontDestroyOnLoad(gameObject); 
    }
    // --- Fin Implementación del Patrón Singleton ---

    /// <summary>
    /// Intenta añadir un objeto (ItemData) al inventario.
    /// </summary>
    public bool AddItem(ItemData itemToAdd, int quantityToAdd)
    {
        if (itemToAdd == null || quantityToAdd <= 0)
        {
            Debug.LogWarning("PlayerInventory: Intento de añadir un objeto nulo o cantidad cero/negativa.");
            return false;
        }

        if (itemToAdd.isStackable)
        {
            foreach (InventorySlot slot in inventorySlots)
            {
                if (slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
                {
                    int canAdd = itemToAdd.maxStackSize - slot.quantity;
                    int amountToAddInThisSlot = Mathf.Min(quantityToAdd, canAdd);

                    slot.AddQuantity(amountToAddInThisSlot);
                    quantityToAdd -= amountToAddInThisSlot;

                    Debug.Log($"PlayerInventory: Añadidos {amountToAddInThisSlot} de {itemToAdd.itemName} al slot existente. Quedan por añadir: {quantityToAdd}");

                    if (quantityToAdd <= 0) return true;
                }
            }
        }

        while (quantityToAdd > 0)
        {
            if (inventorySlots.Count < maxInventorySlots)
            {
                int amountForNewSlot = itemToAdd.isStackable ? Mathf.Min(quantityToAdd, itemToAdd.maxStackSize) : 1;

                InventorySlot newSlot = new InventorySlot(itemToAdd, amountForNewSlot);
                inventorySlots.Add(newSlot);
                quantityToAdd -= amountForNewSlot;
                Debug.Log($"PlayerInventory: Añadido {amountForNewSlot} de {itemToAdd.itemName} a un nuevo slot. Quedan por añadir: {quantityToAdd}");

                if (!itemToAdd.isStackable && quantityToAdd > 0)
                {
                    continue;
                }
            }
            else
            {
                Debug.LogWarning("PlayerInventory: Inventario lleno. No se pudo añadir todo de " + itemToAdd.itemName + ". Quedaron: " + quantityToAdd);
                return quantityToAdd < (itemToAdd.isStackable ? itemToAdd.maxStackSize : 1);
            }
            if (quantityToAdd <= 0) return true;
        }
        return true;
    }

    /// <summary>
    /// Intenta quitar una cantidad de un objeto específico del inventario.
    /// </summary>
    public bool RemoveItem(ItemData itemToRemove, int quantityToRemove)
    {
        if (itemToRemove == null || quantityToRemove <= 0)
        {
            Debug.LogWarning("PlayerInventory: Intento de quitar un objeto nulo o cantidad cero/negativa.");
            return false;
        }

        int initialQuantityToRemove = quantityToRemove;

        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = inventorySlots[i];
            if (slot.item == itemToRemove)
            {
                if (slot.quantity >= quantityToRemove)
                {
                    slot.RemoveQuantity(quantityToRemove);
                    if (slot.quantity <= 0)
                    {
                        inventorySlots.RemoveAt(i);
                    }
                    Debug.Log($"PlayerInventory: Quitados {quantityToRemove} de {itemToRemove.itemName}.");
                    return true;
                }
                else
                {
                    quantityToRemove -= slot.quantity;
                    inventorySlots.RemoveAt(i);
                    Debug.Log($"PlayerInventory: Quitado slot completo de {itemToRemove.itemName}. Quedan por quitar: {quantityToRemove}");
                }
            }
            if (quantityToRemove <= 0) break;
        }

        if (quantityToRemove > 0)
        {
            Debug.LogWarning($"PlayerInventory: No se pudo quitar la cantidad completa de {itemToRemove.itemName}. Faltaron: {quantityToRemove} de {initialQuantityToRemove} solicitados.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Comprueba si el jugador tiene una cierta cantidad de un objeto.
    /// </summary>
    public bool HasItem(ItemData itemToCheck, int quantityRequired = 1)
    {
        if (itemToCheck == null || quantityRequired <= 0) return false;
        int count = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.item == itemToCheck)
            {
                count += slot.quantity;
            }
        }
        return count >= quantityRequired;
    }
}

// } // Fin del namespace (si lo usas)

