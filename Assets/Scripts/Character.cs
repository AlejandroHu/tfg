// Puedes poner esto en el mismo namespace que ItemData.cs y PlayerInventory.cs si estás usando uno.
// namespace TuJuego.Personajes
// {

using UnityEngine;
using System.Collections.Generic; // Necesario para Dictionary

public class Character : MonoBehaviour
{

    // Añade más si quieres ver otros stats totales
    [Header("Información Básica del Personaje")]
    public string characterName = "Personaje";
    public int level = 1;

    [Header("Stats Base del Personaje")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseMagicAttack = 8;
    public int baseMagicDefense = 4;
    public int baseSpeed = 10;
    // Puedes añadir más stats base según necesites

    // Stats actuales (pueden ser modificados por buffs/debuffs en el futuro)
    public int currentHP;
    public int currentMP;

    // --- Equipamiento ---
    // Un diccionario para almacenar qué ItemData está equipado en cada EquipmentSlot.
    // La clave es el EquipmentSlot (ej: Head, Body), y el valor es el ItemData equipado.
    public Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

    // --- Stats Totales (calculados con el equipo) ---
    // Estas son propiedades de solo lectura que calculan el stat total.
    public int MaxHP => baseMaxHP + GetEquipmentBonus(item => item.maxHpBonus);
    public int MaxMP => baseMaxMP + GetEquipmentBonus(item => item.maxMpBonus);
    public int Attack => baseAttack + GetEquipmentBonus(item => item.attackBonus);
    public int Defense => baseDefense + GetEquipmentBonus(item => item.defenseBonus);
    public int MagicAttack => baseMagicAttack + GetEquipmentBonus(item => item.magicAttackBonus);
    public int MagicDefense => baseMagicDefense + GetEquipmentBonus(item => item.magicDefenseBonus);
    public int Speed => baseSpeed + GetEquipmentBonus(item => item.speedBonus);


    void Awake()
    {
        // Inicializar el diccionario de equipamiento con todas las ranuras vacías (null).
        InitializeEquipmentSlots();

        // Inicializar HP y MP al máximo al despertar (o cargar desde datos guardados).
        // Ahora usan las propiedades MaxHP y MaxMP que consideran los bonos del equipo.
        currentHP = MaxHP;
        currentMP = MaxMP;
    }

    void Start()
    {
        // Podrías querer recalcular stats aquí si el equipo se asigna en Awake de otro script
        // o si se carga desde un guardado.
        // RecalculateStats(); // Ejemplo de un método que podrías tener
    }
    // Dentro de la clase Character


    // Inicializa el diccionario de equipamiento.
    private void InitializeEquipmentSlots()
    {
        // Itera por todos los valores del enum EquipmentSlot.
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            // Añade cada slot al diccionario, inicialmente sin ningún objeto equipado (valor null).
            // Se excluye EquipmentSlot.None ya que no es una ranura real de equipamiento.
            if (slot != EquipmentSlot.None)
            {
                equippedItems[slot] = null;
            }
        }
    }

    /// <summary>
    /// Calcula la bonificación total de un stat específico proveniente de todos los objetos equipados.
    /// </summary>
    /// <param name="statSelector">Una función que toma un ItemData y devuelve el valor del bono de stat deseado.</param>
    /// <returns>La suma de las bonificaciones de ese stat de todos los objetos equipados.</returns>
    private int GetEquipmentBonus(System.Func<ItemData, int> statSelector)
    {
        int bonus = 0;
        // Itera por cada objeto equipado en el diccionario 'equippedItems'.
        foreach (ItemData item in equippedItems.Values)
        {
            if (item != null) // Si hay un objeto equipado en el slot.
            {
                bonus += statSelector(item); // Llama a la función 'statSelector' para obtener el bono de ese objeto y lo suma.
            }
        }
        return bonus;
    }


    /// <summary>
    /// Equipa un objeto al personaje en su ranura correspondiente.
    /// Si ya hay un objeto en esa ranura, se desequipa y se devuelve al inventario.
    /// </summary>
    /// <param name="itemToEquip">El ItemData del objeto a equipar.</param>
    /// <param name="inventory">Referencia al inventario del jugador para devolver objetos desequipados.</param>
    /// <returns>True si el objeto se equipó con éxito, False en caso contrario.</returns>
    public bool EquipItem(ItemData itemToEquip, PlayerInventory inventory)
    {
        if (itemToEquip == null || !itemToEquip.isEquipable || itemToEquip.equipmentSlot == EquipmentSlot.None)
        {
            Debug.LogWarning("Intento de equipar un objeto no válido o no equipable.");
            return false; // No es un objeto equipable válido.
        }

        // AQUÍ IRÍA LA LÓGICA DE RESTRICCIONES (ej: ¿puede este personaje usar esta clase de objeto?)
        // if (!CanThisCharacterEquip(itemToEquip)) {
        //     Debug.Log(characterName + " no puede equipar " + itemToEquip.itemName);
        //     return false;
        // }

        EquipmentSlot slotToEquipIn = itemToEquip.equipmentSlot;

        // Comprobar si ya hay algo equipado en esa ranura.
        ItemData previouslyEquippedItem = null;
        if (equippedItems.TryGetValue(slotToEquipIn, out previouslyEquippedItem) && previouslyEquippedItem != null)
        {
            // Si había algo, desequiparlo y añadirlo de nuevo al inventario.
            Debug.Log(characterName + " desequipó " + previouslyEquippedItem.itemName + " para equipar " + itemToEquip.itemName);
            previouslyEquippedItem.OnUnequip(this); // Llamar al método OnUnequip del objeto.
            if (inventory != null)
            {
                inventory.AddItem(previouslyEquippedItem, 1); // Añadir al inventario.
            }
            else
            {
                Debug.LogWarning("PlayerInventory no proporcionado. El objeto " + previouslyEquippedItem.itemName + " no pudo ser devuelto al inventario.");
            }
        }

        // Equipar el nuevo objeto.
        equippedItems[slotToEquipIn] = itemToEquip;
        itemToEquip.OnEquip(this); // Llamar al método OnEquip del nuevo objeto.
        Debug.Log(characterName + " equipó " + itemToEquip.itemName + " en " + slotToEquipIn);

        // Recalcular HP/MP actuales si los máximos cambiaron y el personaje estaba al máximo.
        // O simplemente asegurar que no excedan el nuevo máximo.
        RecalculateCurrentHPMPAfterEquipmentChange();

        // Aquí podrías disparar un evento OnCharacterStatsChanged o OnEquipmentChanged para que la UI se actualice.
        return true;
    }

    /// <summary>
    /// Desequipa un objeto de una ranura específica y lo devuelve al inventario.
    /// </summary>
    /// <param name="slotToUnequip">La ranura de la que se quiere desequipar el objeto.</param>
    /// <param name="inventory">Referencia al inventario del jugador.</param>
    /// <returns>El ItemData del objeto que fue desequipado, o null si no había nada.</returns>
    public ItemData UnequipItem(EquipmentSlot slotToUnequip, PlayerInventory inventory)
    {
        if (slotToUnequip == EquipmentSlot.None) return null;

        ItemData unequippedItem = null;
        if (equippedItems.TryGetValue(slotToUnequip, out unequippedItem) && unequippedItem != null)
        {
            unequippedItem.OnUnequip(this); // Llamar al método OnUnequip.
            equippedItems[slotToUnequip] = null; // Vaciar el slot en el personaje.

            if (inventory != null)
            {
                inventory.AddItem(unequippedItem, 1); // Añadir el objeto de vuelta al inventario.
            }
            else
            {
                Debug.LogWarning("PlayerInventory no proporcionado. El objeto " + unequippedItem.itemName + " no pudo ser devuelto al inventario.");
            }
            Debug.Log(characterName + " desequipó " + unequippedItem.itemName + " de " + slotToUnequip);

            RecalculateCurrentHPMPAfterEquipmentChange();
            // Disparar evento de cambio de stats/equipo.
            return unequippedItem;
        }
        return null; // No había nada en esa ranura.
    }

    // Método para ajustar currentHP y currentMP si los máximos cambian debido al equipo.
    private void RecalculateCurrentHPMPAfterEquipmentChange()
    {
        if (currentHP > MaxHP) currentHP = MaxHP;
        if (currentMP > MaxMP) currentMP = MaxMP;
        // Si el personaje estaba a 0 HP y un objeto le da MaxHP, no debería revivir automáticamente.
        // Esa lógica iría en otro lado (ej: si currentHP era 0, sigue siendo 0 a menos que se use un objeto de revivir).
    }


    // --- Métodos de Stats (ya los tenías) ---
    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (currentHP >= MaxHP) // Usar la propiedad MaxHP que incluye bonos
        {
            Debug.Log(characterName + " ya tiene el HP al máximo.");
            return false;
        }
        currentHP += amount;
        if (currentHP > MaxHP) currentHP = MaxHP;
        Debug.Log(characterName + " se curó por " + amount + " HP. HP actual: " + currentHP + "/" + MaxHP);
        return true;
    }

    public bool RestoreMana(int amount)
    {
        if (amount <= 0) return false;
        if (currentMP >= MaxMP) // Usar la propiedad MaxMP que incluye bonos
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
}

// } // Fin del namespace (si lo usas)
