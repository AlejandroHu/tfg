using UnityEngine;
using System.Collections.Generic; // Necesario para List y Dictionary

// Puedes poner esto en un namespace si estás organizando así tu código
// namespace TuJuego.Personajes
// {

// Asegúrate de que ItemData, EquipmentSlot y AbilityData (si está en otro namespace) sean accesibles
// using TuJuego.Inventario; // Si ItemData y EquipmentSlot están aquí
// using TuJuego.Habilidades; // Si AbilityData está aquí

public class Character : MonoBehaviour
{
    [Header("Información Básica del Personaje")]
    public string characterName = "PersonajeDePrueba";
    public int level = 1;
    [Tooltip("Sprite del retrato del personaje para mostrar en la UI (menús, party, etc.).")]
    public Sprite portraitSprite;

    [Header("Experiencia y Progresión")]
    [Tooltip("Puntos de experiencia actuales del personaje.")]
    public int currentXP = 0;
    [Tooltip("Puntos de experiencia necesarios para alcanzar el siguiente nivel.")]
    public int experienceToNextLevel = 100;

    // --- NUEVO: Lista de Habilidades Conocidas ---
    [Header("Habilidades del Personaje")]
    [Tooltip("Lista de las habilidades que este personaje conoce y puede usar en combate.")]
    public List<AbilityData> knownAbilities = new List<AbilityData>();

    [Header("Stats Base del Personaje")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseMagicAttack = 8;
    public int baseMagicDefense = 4;
    public int baseSpeed = 10;

    // Stats actuales
    public int currentHP;
    public int currentMP;

    [Header("Equipo Inicial de Prueba (Opcional)")]
    [SerializeField] private ItemData initialHeadEquipment;
    [SerializeField] private ItemData initialMainHandEquipment;
    [SerializeField] private ItemData initialBodyEquipment;
    [SerializeField] private ItemData initialFeetEquipment;

    public Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

    // Propiedades para los stats totales (que consideran el equipo)
    public int MaxHP => GetStatValueWithEquipment(baseMaxHP, item => item.maxHpBonus);
    public int MaxMP => GetStatValueWithEquipment(baseMaxMP, item => item.maxMpBonus);
    public int Attack => GetStatValueWithEquipment(baseAttack, item => item.attackBonus);
    public int Defense => GetStatValueWithEquipment(baseDefense, item => item.defenseBonus);
    public int MagicAttack => GetStatValueWithEquipment(baseMagicAttack, item => item.magicAttackBonus);
    public int MagicDefense => GetStatValueWithEquipment(baseMagicDefense, item => item.magicDefenseBonus);
    public int Speed => GetStatValueWithEquipment(baseSpeed, item => item.speedBonus);


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeEquipmentSlots();

        if (initialHeadEquipment != null) EquipItemInitially(initialHeadEquipment);
        if (initialMainHandEquipment != null) EquipItemInitially(initialMainHandEquipment);
        if (initialBodyEquipment != null) EquipItemInitially(initialBodyEquipment);
        if (initialFeetEquipment != null) EquipItemInitially(initialFeetEquipment);

        currentHP = MaxHP;
        currentMP = MaxMP;
    }

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

    private void EquipItemInitially(ItemData itemToEquip)
    {
        if (itemToEquip != null && itemToEquip.isEquipable && itemToEquip.equipmentSlot != EquipmentSlot.None)
        {
            equippedItems[itemToEquip.equipmentSlot] = itemToEquip;
        }
    }

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

    public bool EquipItem(ItemData itemToEquip, PlayerInventory inventory)
    {
        if (itemToEquip == null || !itemToEquip.isEquipable || itemToEquip.equipmentSlot == EquipmentSlot.None)
        {
            Debug.LogWarning(characterName + ": Intento de equipar un objeto no válido o no equipable: " + (itemToEquip != null ? itemToEquip.itemName : "NULL"));
            return false;
        }
        EquipmentSlot slotToEquipIn = itemToEquip.equipmentSlot;
        ItemData previouslyEquippedItem = null;
        if (equippedItems.TryGetValue(slotToEquipIn, out previouslyEquippedItem) && previouslyEquippedItem != null)
        {
            previouslyEquippedItem.OnUnequip(this);
            if (inventory != null) inventory.AddItem(previouslyEquippedItem, 1);
            else Debug.LogWarning("PlayerInventory no proporcionado al desequipar " + previouslyEquippedItem.itemName);
        }
        equippedItems[slotToEquipIn] = itemToEquip;
        itemToEquip.OnEquip(this);
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

    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (currentHP >= MaxHP) return false;
        currentHP += amount;
        if (currentHP > MaxHP) currentHP = MaxHP;
        return true;
    }

    public bool RestoreMana(int amount)
    {
        if (amount <= 0) return false;
        if (currentMP >= MaxMP) return false;
        currentMP += amount;
        if (currentMP > MaxMP) currentMP = MaxMP;
        return true;
    }

    // Método de ejemplo para recibir daño (para poder bajar el HP para las pruebas)
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
        // Debug.Log(characterName + " recibió " + amount + " de daño. HP actual: " + currentHP + "/" + MaxHP); // Para depuración
        if (currentHP == 0)
        {
            Debug.Log(characterName + " ha sido derrotado.");
            // Lógica de muerte aquí
        }
    }

    // Gasta una cantidad de MP del personaje.
    public bool SpendMana(int cost)
    {
        if (cost < 0) return false;
        if (currentMP >= cost)
        {
            currentMP -= cost;
            return true;
        }
        return false;
    }

    public void GainXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;
        Debug.Log(characterName + " ganó " + amount + " XP. XP actual: " + currentXP);
    }

    // private void CheckForLevelUp()
    // {
    //    if (currentXP >= experienceToNextLevel)
    //    {
    //        level++;
    //        currentXP -= experienceToNextLevel; // O currentXP = 0; si la XP se resetea
    //        experienceToNextLevel = CalculateNextLevelXP(level); // Necesitarías una función para esto
    //        // Incrementar stats base, etc.
    //        // Disparar evento OnLevelUp
    //        Debug.Log(characterName + " subió al Nivel " + level + "!");
    //    }
    // }

    // private int CalculateNextLevelXP(int currentLevel)
    // {
    //    // Fórmula de ejemplo para la XP necesaria
    //    return Mathf.FloorToInt(100 * Mathf.Pow(currentLevel, 1.5f));
    // }
}

// } // Fin del namespace (si lo usas)
