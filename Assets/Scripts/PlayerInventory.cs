using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Necesario para usar Action (eventos)

// Asegúrate de que el namespace coincida si estás usando uno
// namespace TuJuego.Inventario 
// {

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

public class PlayerInventory : MonoBehaviour
{
    [Header("Configuración del Inventario")]
    [Tooltip("Número máximo de slots diferentes que puede tener el inventario.")]
    // --- MODIFICADO: de private a public ---
    [SerializeField] public int maxInventorySlots = 24; // Ahora es público

    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

    public static event Action OnInventoryChanged;
    public static PlayerInventory Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PlayerInventory: Se encontró otra instancia. Destruyendo este GameObject.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
         DontDestroyOnLoad(gameObject); 
    }

    public bool AddItem(ItemData itemToAdd, int quantityToAdd)
    {
        if (itemToAdd == null || quantityToAdd <= 0)
        {
            Debug.LogWarning("PlayerInventory: Intento de añadir un objeto nulo o cantidad cero/negativa.");
            return false;
        }

        bool itemAddedSuccessfully = false;

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
                    itemAddedSuccessfully = true;

                    if (quantityToAdd <= 0) break;
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
                itemAddedSuccessfully = true;

                if (!itemToAdd.isStackable && quantityToAdd > 0)
                {
                    continue;
                }
            }
            else
            {
                Debug.LogWarning("PlayerInventory: Inventario lleno. No se pudo añadir todo de " + itemToAdd.itemName + ". Quedaron: " + quantityToAdd);
                if (itemAddedSuccessfully) OnInventoryChanged?.Invoke();
                return itemAddedSuccessfully;
            }
            if (quantityToAdd <= 0) break;
        }

        if (itemAddedSuccessfully)
        {
            OnInventoryChanged?.Invoke();
        }
        return itemAddedSuccessfully;
    }

    public bool RemoveItem(ItemData itemToRemove, int quantityToRemove)
    {
        if (itemToRemove == null || quantityToRemove <= 0)
        {
            Debug.LogWarning("PlayerInventory: Intento de quitar un objeto nulo o cantidad cero/negativa.");
            return false;
        }

        int initialQuantityToRemove = quantityToRemove;
        bool itemRemovedSuccessfully = false;
        bool actualRemovalHappened = false; // Para saber si realmente se quitó algo

        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = inventorySlots[i];
            if (slot.item == itemToRemove)
            {
                itemRemovedSuccessfully = true; // Marcamos que al menos encontramos el item
                int amountToRemoveFromThisSlot = 0;

                if (slot.quantity > quantityToRemove)
                {
                    amountToRemoveFromThisSlot = quantityToRemove;
                    slot.RemoveQuantity(quantityToRemove);
                    quantityToRemove = 0;
                }
                else
                {
                    amountToRemoveFromThisSlot = slot.quantity;
                    quantityToRemove -= slot.quantity;
                    inventorySlots.RemoveAt(i);
                }
                if (amountToRemoveFromThisSlot > 0) actualRemovalHappened = true;
            }
            if (quantityToRemove <= 0) break;
        }

        if (actualRemovalHappened) // Solo invocar si realmente se quitó algo
        {
            OnInventoryChanged?.Invoke();
            if (quantityToRemove > 0)
            {
                Debug.LogWarning($"PlayerInventory: No se pudo quitar la cantidad completa de {itemToRemove.itemName}. Faltaron: {quantityToRemove} de {initialQuantityToRemove} solicitados.");
                return false;
            }
            return true;
        }
        else if (itemRemovedSuccessfully && !actualRemovalHappened)
        {
            // Se encontró el item pero no se quitó nada (quizás quantityToRemove era mayor a lo que había)
            // Esto no debería pasar si la lógica de arriba es correcta y quantityToRemove es positivo.
            // Pero si RemoveQuantity no hace nada si el valor es mayor, podría pasar.
            // Asumiendo que RemoveQuantity siempre quita si hay algo.
        }
        else if (!itemRemovedSuccessfully)
        {
            Debug.LogWarning($"PlayerInventory: No se encontró el objeto {itemToRemove.itemName} para quitar.");
        }

        return false;
    }

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

// } // Fin del namespace si lo usas
