// Es buena práctica poner tus scripts dentro de un namespace para organizar mejor tu código.
// Puedes cambiar "TuJuego" por el nombre de tu proyecto o un namespace más específico.
// namespace TuJuego.Inventario
// {

using UnityEngine;
using System.Collections.Generic; // Necesario si vas a usar Listas para restricciones, etc.

/// <summary>
/// Define los diferentes tipos de objetos que pueden existir en el juego.
/// </summary>
public enum ItemType
{
    Weapon,     // Armas que se pueden equipar.
    Armor,      // Piezas de armadura que se pueden equipar (Casco, Pechera, Botas).
    Consumable, // Objetos que se usan y generalmente se gastan (pociones, comida, etc.).
    Quest       // Objetos específicos de misión, a menudo necesarios para progresar en la historia, pueden ser únicos y no tener otro uso.
}

/// <summary>
/// Define las ranuras de equipamiento donde un objeto puede ser equipado por un personaje.
/// </summary>
public enum EquipmentSlot
{
    None,       // El objeto no es equipable o no ocupa un slot específico.
    MainHand,   // Para el arma principal del personaje.
    Head,       // Para cascos, sombreros, etc.
    Body,       // Para armaduras de cuerpo, pecheras, ropas.
    Feet        // Para botas, calzado.
}

/// <summary>
/// ScriptableObject para definir los datos base de cada tipo de objeto en el juego.
/// Puedes crear assets de este tipo desde el menú de Unity: Assets > Create > TuJuego > Inventory > ItemData
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "TuJuego/Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Información General del Objeto")]
    [Tooltip("Identificador único para este objeto (ej: 'wpn_iron_sword', 'quest_ancient_seal'). Útil para referencias internas, guardado/carga.")]
    public string itemID;

    [Tooltip("Nombre del objeto tal como se mostrará al jugador en la UI.")]
    public string itemName = "Nuevo Objeto";

    [Tooltip("Icono del objeto para el inventario y la UI. Idealmente 16x16 píxeles para mantener el estilo.")]
    public Sprite icon;

    [Tooltip("Descripción del objeto que se mostrará al jugador (puede ser multilínea para más detalle).")]
    [TextArea(3, 6)] // Hace el campo de texto más grande en el Inspector.
    public string description = "Descripción del objeto.";

    [Tooltip("El tipo de objeto (Arma, Armadura, Consumible, Quest). Determina su uso principal y cómo interactúa con otros sistemas.")]
    public ItemType itemType = ItemType.Consumable; // Valor por defecto, ajústalo al crear el asset.

    [Tooltip("¿Puede este objeto apilarse en un solo slot del inventario (ej: pociones)? Si no, cada uno ocupará un slot.")]
    public bool isStackable = false;

    [Tooltip("Si es apilable, ¿cuántos caben como máximo en un solo slot? Si no es apilable, este valor se ignora (o se considera 1).")]
    public int maxStackSize = 1;

    [Header("Detalles de Equipamiento")]
    [Tooltip("¿Es este objeto equipable por un personaje? (Marcado para Armas y Armaduras).")]
    public bool isEquipable = false;

    [Tooltip("Si es equipable, ¿en qué ranura se equipa? (Debe coincidir con los valores del enum EquipmentSlot).")]
    public EquipmentSlot equipmentSlot = EquipmentSlot.None;

    // --- Stats que el objeto otorga al ser equipado ---
    // Estos valores se sumarán a los stats base del personaje cuando el objeto esté equipado.
    [Space(10)] // Añade un pequeño espacio visual en el Inspector para organizar.
    [Tooltip("Bonificación al ataque físico que proporciona este objeto.")]
    public int attackBonus = 0;
    [Tooltip("Bonificación a la defensa física que proporciona este objeto.")]
    public int defenseBonus = 0;
    [Tooltip("Bonificación al ataque mágico que proporciona este objeto.")]
    public int magicAttackBonus = 0;
    [Tooltip("Bonificación a la defensa mágica que proporciona este objeto.")]
    public int magicDefenseBonus = 0;
    [Tooltip("Bonificación a la velocidad del personaje.")]
    public int speedBonus = 0;
    [Tooltip("Bonificación a los Puntos de Vida (HP) máximos del personaje.")]
    public int maxHpBonus = 0;
    [Tooltip("Bonificación a los Puntos de Maná (MP) máximos del personaje.")]
    public int maxMpBonus = 0;

    // --- Restricciones de Equipamiento (Ejemplos, puedes implementarlos si los necesitas) ---
    // [Tooltip("Lista de IDs de personajes que pueden equipar este objeto. Dejar vacío si no hay restricciones por personaje.")]
    // public List<string> equippableByCharacterIDs; 
    // [Tooltip("Lista de clases de personaje que pueden equipar este objeto. Dejar vacío si no hay restricciones por clase.")]
    // public List<CharacterClassEnum> equippableByClasses; // Necesitarías un enum CharacterClassEnum
    // [Tooltip("Nivel mínimo del personaje para poder equipar este objeto.")]
    // public int requiredLevel = 1;


    [Header("Detalles de Consumible")]
    [Tooltip("¿Es este objeto un consumible (se gasta/desaparece después de usarse)?")]
    public bool isConsumable = false;

    // --- Efectos del consumible ---
    [Space(10)]
    [Tooltip("Cantidad de Puntos de Vida (HP) que este objeto restaura al usarse.")]
    public int hpToRestore = 0;
    [Tooltip("Cantidad de Puntos de Maná (MP) que este objeto restaura al usarse.")]
    public int mpToRestore = 0;
    // Ejemplos de otros efectos para consumibles (necesitarían más estructura de datos):
    // public StatusEffectData statusEffectToCure; // Referencia a un ScriptableObject de efecto de estado a curar
    // public StatusEffectData statusEffectToApplyToTarget; // Para objetos que aplican efectos a enemigos
    // public BuffData buffToApplyToSelf; // Para objetos que dan buffs temporales


    // --- MÉTODOS (pueden ser virtuales para que clases de ítems más específicas los sobrescriban) ---

    /// <summary>
    /// Lógica para cuando el objeto es usado (ej: desde el inventario o en combate).
    /// Este método base es un placeholder; la lógica real dependerá del tipo de objeto.
    /// </summary>
    /// <param name="targetCharacter">El personaje sobre el que se usa el objeto (puede ser el propio jugador u otro). Puede ser null si el objeto no tiene un objetivo específico.</param>
    /// <returns>True si el objeto se consumió o usó con éxito, False en caso contrario.</returns>
    public virtual bool Use(Character targetCharacter) // Necesitarás una clase 'Character' definida en tu proyecto.
    {
        if (isConsumable)
        {
            Debug.Log("Usando " + itemName + (targetCharacter != null ? " en " + targetCharacter.characterName : ""));

            // Aquí iría la lógica específica del efecto del consumible.
            // Por ejemplo, si hpToRestore > 0 y targetCharacter != null:
            // targetCharacter.Heal(hpToRestore); // Asumiendo que Character tiene un método Heal().

            // Si se usa con éxito, el sistema de inventario debería quitar/reducir este objeto.
            return true; // Indicar que el objeto se consumió (o se usó).
        }
        Debug.Log(itemName + " no es un consumible o no se pudo usar en este contexto.");
        return false;
    }

    /// <summary>
    /// Lógica que se ejecuta cuando este objeto es equipado por un personaje.
    /// Los cambios de stats se suelen manejar en el sistema de equipamiento del personaje,
    /// pero este método podría usarse para efectos adicionales al equipar.
    /// </summary>
    /// <param name="characterEquipping">El personaje que está equipando este objeto.</param>
    public virtual void OnEquip(Character characterEquipping) // Necesitarás una clase 'Character'.
    {
        if (isEquipable)
        {
            Debug.Log(characterEquipping.characterName + " equipó " + itemName);
            // Aquí podrías aplicar efectos pasivos especiales que no sean solo stats, si los hubiera.
        }
    }

    /// <summary>
    /// Lógica que se ejecuta cuando este objeto es desequipado por un personaje.
    /// Los cambios de stats se suelen manejar en el sistema de equipamiento del personaje,
    /// pero este método podría usarse para quitar efectos adicionales.
    /// </summary>
    /// <param name="characterUnequipping">El personaje que está desequipando este objeto.</param>
    public virtual void OnUnequip(Character characterUnequipping) // Necesitarás una clase 'Character'.
    {
        if (isEquipable)
        {
            Debug.Log(characterUnequipping.characterName + " desequipó " + itemName);
            // Aquí podrías quitar efectos pasivos especiales si los hubiera.
        }
    }
}

// } // Fin del namespace (si lo usas)
