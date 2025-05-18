// Puedes poner esto en el mismo namespace que Character.cs si estás usando uno.
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
    Quest       // Objetos específicos de misión, a menudo necesarios para progresar en la historia.
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
    [TextArea(3, 6)]
    public string description = "Descripción del objeto.";

    [Tooltip("El tipo de objeto (Arma, Armadura, Consumible, Quest). Determina su uso principal y cómo interactúa con otros sistemas.")]
    public ItemType itemType = ItemType.Consumable;

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
    [Space(10)]
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

    [Header("Detalles de Consumible")]
    [Tooltip("¿Es este objeto un consumible (se gasta/desaparece después de usarse)?")]
    public bool isConsumable = false;

    [Space(10)]
    [Tooltip("Cantidad de Puntos de Vida (HP) que este objeto restaura al usarse.")]
    public int hpToRestore = 0;
    [Tooltip("Cantidad de Puntos de Maná (MP) que este objeto restaura al usarse.")]
    public int mpToRestore = 0;

    /// <summary>
    /// Lógica para cuando el objeto es usado.
    /// </summary>
    /// <param name="targetCharacter">El personaje sobre el que se usa el objeto. Puede ser null si el objeto no requiere un objetivo.</param>
    /// <returns>True si el objeto se consumió porque tuvo algún efecto, False en caso contrario.</returns>
    public virtual bool Use(Character targetCharacter)
    {
        // Comprobar si el objeto es realmente un consumible.
        if (!isConsumable)
        {
            Debug.Log(itemName + " no es un consumible y no puede ser usado de esta manera.");
            return false; // No se usó/consumió.
        }

        Debug.Log("Intentando usar " + itemName + (targetCharacter != null ? " en " + targetCharacter.characterName : " (sin objetivo específico)"));

        bool effectWasActuallyApplied = false; // Bandera para rastrear si algún efecto real ocurrió.

        // Solo intentar aplicar efectos si se proporcionó un personaje objetivo.
        if (targetCharacter != null)
        {
            // Intentar restaurar HP si el objeto tiene hpToRestore > 0.
            if (hpToRestore > 0)
            {
                if (targetCharacter.Heal(hpToRestore)) // El método Heal() ahora devuelve true si curó algo.
                {
                    effectWasActuallyApplied = true; // Marcar que un efecto se aplicó.
                }
            }

            // Intentar restaurar MP si el objeto tiene mpToRestore > 0.
            if (mpToRestore > 0)
            {
                if (targetCharacter.RestoreMana(mpToRestore)) // El método RestoreMana() ahora devuelve true si restauró algo.
                {
                    effectWasActuallyApplied = true; // Marcar que un efecto se aplicó.
                }
            }
            // Aquí podrías añadir lógica para otros efectos consumibles (curar estados, etc.)
            // y actualizar effectWasActuallyApplied si tienen éxito.
        }
        else if (hpToRestore > 0 || mpToRestore > 0) // Si el objeto tiene efectos de restauración pero no se dio un objetivo.
        {
            Debug.Log(itemName + " es un objeto de restauración pero no se especificó un objetivo (targetCharacter es null). No se puede usar.");
            return false; // No se puede usar sin objetivo si tiene efectos que lo requieren.
        }
        else
        {
            // Si es un consumible pero no tiene efectos de HP/MP y no se le dio objetivo,
            // podría ser un tipo de consumible diferente (ej: una llave que se gasta, una bengala).
            // Para esos casos, podrías querer que siempre devuelva true para que se consuma.
            // O añadir otra propiedad a ItemData para "seConsumeAlUsarIndependientementeDelEfecto".
            // Por ahora, si no tuvo efectos de restauración y no hubo objetivo, consideramos que no se "usó efectivamente"
            // a menos que añadas lógica específica para otros tipos de consumibles.
            Debug.Log(itemName + " es consumible pero no tuvo efectos de restauración o no se aplicaron (sin objetivo / objetivo ya al máximo).");
            // Si quieres que CUALQUIER consumible se gaste al "intentar" usarlo, incluso si no tuvo efecto,
            // podrías simplemente hacer 'return true;' aquí (después del Debug.Log).
            // Pero para que solo se gaste si HIZO algo, dependemos de effectWasActuallyApplied.
        }

        // El objeto se considera "usado con éxito" (y por lo tanto se debe consumir del inventario)
        // solo si realmente tuvo algún efecto.
        if (effectWasActuallyApplied)
        {
            Debug.Log(itemName + " fue usado con éxito y tuvo efecto.");
            return true;
        }
        else
        {
            // Si no se aplicó ningún efecto (ej: HP/MP ya estaban al máximo, o no había objetivo para un objeto que lo requería),
            // el objeto no se consume.
            Debug.Log(itemName + " se intentó usar, pero no tuvo ningún efecto. No se consumirá.");
            return false;
        }
    }

    // Método llamado cuando el objeto es equipado.
    public virtual void OnEquip(Character characterEquipping)
    {
        if (isEquipable)
        {
            Debug.Log((characterEquipping != null ? characterEquipping.characterName : "Alguien") + " equipó " + itemName);
            // Aquí podrías añadir lógica para efectos que se activan al equipar,
            // además de los bonus de stats que se manejarían en el sistema de equipamiento del personaje.
        }
    }

    // Método llamado cuando el objeto es desequipado.
    public virtual void OnUnequip(Character characterUnequipping)
    {
        if (isEquipable)
        {
            Debug.Log((characterUnequipping != null ? characterUnequipping.characterName : "Alguien") + " desequipó " + itemName);
            // Aquí podrías añadir lógica para quitar efectos que se activaron al equipar.
        }
    }
}

// } // Fin del namespace (si lo usas)
