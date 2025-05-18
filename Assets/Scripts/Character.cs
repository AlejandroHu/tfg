// Puedes poner esto en el mismo namespace que ItemData.cs si estás usando uno.
// namespace TuJuego.Personajes
// {

using UnityEngine;
using System.Collections.Generic; // Añadido por si se usa para equipamiento más adelante

/// <summary>
/// Clase base para representar a un personaje en el juego (jugador o NPC).
/// </summary>
public class Character : MonoBehaviour
{
    [Header("Información Básica del Personaje")]
    public string characterName = "Personaje";
    public int level = 1;
    [Tooltip("Sprite del retrato del personaje para mostrar en la UI (menús, party, etc.).")]
    public Sprite portraitSprite; // <--- VARIABLE AÑADIDA

    [Header("Stats Básicos")]
    public int maxHP = 100;
    public int currentHP;

    public int maxMP = 50;
    public int currentMP;

    // --- Equipamiento (Placeholder - Se desarrollará más adelante) ---
    // Un diccionario para almacenar qué ItemData está equipado en cada EquipmentSlot.
    public Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();


    // --- Stats Totales (Propiedades calculadas - Se desarrollarán más adelante) ---
    // Estas son propiedades de solo lectura que calcularán el stat total.
    // Por ahora, devuelven el base, pero se modificarán para incluir bonos de equipo.
    public int MaxHP => GetStatValueWithEquipment(baseMaxHP, item => item.maxHpBonus); // Ejemplo de cómo podría ser
    public int MaxMP => GetStatValueWithEquipment(baseMaxMP, item => item.maxMpBonus);
    public int Attack => GetStatValueWithEquipment(baseAttack, item => item.attackBonus);
    public int Defense => GetStatValueWithEquipment(baseDefense, item => item.defenseBonus);
    // Stats base (puedes moverlos arriba si prefieres)
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    // ... puedes añadir más stats base y propiedades totales ...


    void Awake()
    {
        InitializeEquipmentSlots(); // Asegurarse de que el diccionario esté listo
        currentHP = MaxHP; // Usar la propiedad que considera el equipo
        currentMP = MaxMP; // Usar la propiedad que considera el equipo
    }

    // Inicializa el diccionario de equipamiento.
    private void InitializeEquipmentSlots()
    {
        if (equippedItems == null)
        {
            equippedItems = new Dictionary<EquipmentSlot, ItemData>();
        }
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot != EquipmentSlot.None && !equippedItems.ContainsKey(slot))
            {
                equippedItems[slot] = null;
            }
        }
    }

    /// <summary>
    /// Helper para calcular un stat total incluyendo bonos de equipo.
    /// </summary>
    private int GetStatValueWithEquipment(int baseValue, System.Func<ItemData, int> statSelector)
    {
        int totalBonus = 0;
        if (equippedItems != null)
        {
            foreach (ItemData item in equippedItems.Values)
            {
                if (item != null)
                {
                    totalBonus += statSelector(item);
                }
            }
        }
        return baseValue + totalBonus;
    }


    /// <summary>
    /// Cura al personaje una cantidad específica de HP.
    /// </summary>
    /// <param name="amount">La cantidad de HP a restaurar.</param>
    /// <returns>True si el HP fue restaurado (no estaba ya al máximo), False en caso contrario.</returns>
    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (currentHP >= MaxHP)
        {
            Debug.Log(characterName + " ya tiene el HP al máximo.");
            return false;
        }
        currentHP += amount;
        if (currentHP > MaxHP) currentHP = MaxHP;
        Debug.Log(characterName + " se curó por " + amount + " HP. HP actual: " + currentHP + "/" + MaxHP);
        return true;
    }

    /// <summary>
    /// Restaura al personaje una cantidad específica de MP.
    /// </summary>
    /// <param name="amount">La cantidad de MP a restaurar.</param>
    /// <returns>True si el MP fue restaurado (no estaba ya al máximo), False en caso contrario.</returns>
    public bool RestoreMana(int amount)
    {
        if (amount <= 0) return false;
        if (currentMP >= MaxMP)
        {
            Debug.Log(characterName + " ya tiene el MP al máximo.");
            return false;
        }
        currentMP += amount;
        if (currentMP > MaxMP) currentMP = MaxMP;
        Debug.Log(characterName + " restauró " + amount + " MP. MP actual: " + currentMP + "/" + MaxMP);
        return true;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        Debug.Log(characterName + " recibió " + amount + " de daño. HP actual: " + currentHP + "/" + MaxHP);
        if (currentHP == 0)
        {
            Debug.Log(characterName + " ha sido derrotado.");
        }
    }

    public bool SpendMana(int cost)
    {
        if (cost < 0) return false;
        if (currentMP >= cost)
        {
            currentMP -= cost;
            Debug.Log(characterName + " gastó " + cost + " MP. MP restante: " + currentMP + "/" + MaxMP);
            return true;
        }
        else
        {
            Debug.Log(characterName + " no tiene suficiente MP para gastar " + cost + ". MP actual: " + currentMP);
            return false;
        }
    }

    // Métodos para equipar/desequipar (los que ya tenías en el documento de diseño)
    public bool EquipItem(ItemData itemToEquip, PlayerInventory inventory)
    {
        if (itemToEquip == null || !itemToEquip.isEquipable || itemToEquip.equipmentSlot == EquipmentSlot.None)
        {
            Debug.LogWarning("Intento de equipar un objeto no válido o no equipable.");
            return false;
        }

        EquipmentSlot slotToEquipIn = itemToEquip.equipmentSlot;
        ItemData previouslyEquippedItem = null;

        if (equippedItems.TryGetValue(slotToEquipIn, out previouslyEquippedItem) && previouslyEquippedItem != null)
        {
            Debug.Log(characterName + " desequipó " + previouslyEquippedItem.itemName + " para equipar " + itemToEquip.itemName);
            previouslyEquippedItem.OnUnequip(this);
            if (inventory != null) inventory.AddItem(previouslyEquippedItem, 1);
            else Debug.LogWarning("PlayerInventory no proporcionado al desequipar " + previouslyEquippedItem.itemName);
        }

        equippedItems[slotToEquipIn] = itemToEquip;
        itemToEquip.OnEquip(this);
        Debug.Log(characterName + " equipó " + itemToEquip.itemName + " en " + slotToEquipIn);
        RecalculateCurrentHPMPAfterEquipmentChange();
        return true;
    }

    public ItemData UnequipItem(EquipmentSlot slotToUnequip, PlayerInventory inventory)
    {
        if (slotToUnequip == EquipmentSlot.None) return null;
        ItemData unequippedItem = null;
        if (equippedItems.TryGetValue(slotToUnequip, out unequippedItem) && unequippedItem != null)
        {
            unequippedItem.OnUnequip(this);
            equippedItems[slotToUnequip] = null;
            if (inventory != null) inventory.AddItem(unequippedItem, 1);
            else Debug.LogWarning("PlayerInventory no proporcionado al desequipar " + unequippedItem.itemName);
            Debug.Log(characterName + " desequipó " + unequippedItem.itemName + " de " + slotToUnequip);
            RecalculateCurrentHPMPAfterEquipmentChange();
            return unequippedItem;
        }
        return null;
    }

    private void RecalculateCurrentHPMPAfterEquipmentChange()
    {
        if (currentHP > MaxHP) currentHP = MaxHP;
        if (currentMP > MaxMP) currentMP = MaxMP;
    }
}

// } // Fin del namespace (si lo usas)
