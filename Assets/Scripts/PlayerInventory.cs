// Puedes poner esto en el mismo namespace que ItemData.cs y Character.cs si estás usando uno.
// namespace TuJuego.Inventario
// {

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
    [SerializeField] private int maxInventorySlots = 20;

    public List<InventorySlot> inventorySlots = new List<InventorySlot>();

    // --- Evento para notificar cambios en el inventario ---
    // 'public static event Action' define un evento al que otros scripts pueden suscribirse.
    // 'static' significa que el evento pertenece a la clase PlayerInventory, no a una instancia.
    // 'Action' es un delegado que no toma parámetros y no devuelve nada.
    public static event Action OnInventoryChanged;

    // --- Implementación del Patrón Singleton ---
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
        // DontDestroyOnLoad(gameObject); // Descomenta si quieres que persista entre escenas
    }

    public bool AddItem(ItemData itemToAdd, int quantityToAdd)
    {
        if (itemToAdd == null || quantityToAdd <= 0)
        {
            Debug.LogWarning("PlayerInventory: Intento de añadir un objeto nulo o cantidad cero/negativa.");
            return false;
        }

        bool itemAddedSuccessfully = false; // Bandera para saber si se añadió algo

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

                    if (quantityToAdd <= 0) break; // Si ya se añadió todo en este tipo de slot
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
                // Si se añadió algo antes de llenarse, itemAddedSuccessfully será true.
                // Si no se pudo añadir nada, itemAddedSuccessfully será false.
                if (itemAddedSuccessfully) OnInventoryChanged?.Invoke(); // Notificar si algo se añadió antes de llenarse
                return itemAddedSuccessfully;
            }
            if (quantityToAdd <= 0) break; // Salir del while si ya se añadió todo
        }

        // Si se añadió o modificó algún objeto, disparar el evento.
        if (itemAddedSuccessfully)
        {
            OnInventoryChanged?.Invoke(); // El '?' es un operador null-conditional: solo invoca si OnInventoryChanged no es null (es decir, si hay suscriptores).
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

        for (int i = inventorySlots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = inventorySlots[i];
            if (slot.item == itemToRemove)
            {
                itemRemovedSuccessfully = true; // Marcamos que al menos encontramos el item
                if (slot.quantity > quantityToRemove) // Si este slot tiene más de lo que necesitamos quitar
                {
                    slot.RemoveQuantity(quantityToRemove);
                    quantityToRemove = 0; // Ya quitamos todo lo necesario
                }
                else // Este slot tiene igual o menos de lo que necesitamos quitar
                {
                    quantityToRemove -= slot.quantity; // Quitamos lo que tiene el slot
                    inventorySlots.RemoveAt(i); // Quitar el slot porque se vació (o quitamos todo lo que tenía)
                }
            }
            if (quantityToRemove <= 0) break;
        }

        if (itemRemovedSuccessfully && initialQuantityToRemove > quantityToRemove) // Si se quitó al menos una parte de lo solicitado
        {
            OnInventoryChanged?.Invoke(); // Disparar el evento
            if (quantityToRemove > 0)
            { // Si no se pudo quitar todo
                Debug.LogWarning($"PlayerInventory: No se pudo quitar la cantidad completa de {itemToRemove.itemName}. Faltaron: {quantityToRemove} de {initialQuantityToRemove} solicitados.");
                return false; // No se completó la operación como se esperaba
            }
            return true; // Se quitó la cantidad solicitada o todo lo que había.
        }
        else if (!itemRemovedSuccessfully)
        {
            Debug.LogWarning($"PlayerInventory: No se encontró el objeto {itemToRemove.itemName} para quitar.");
        }

        return false; // No se encontró el objeto o no se pudo quitar la cantidad solicitada
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
