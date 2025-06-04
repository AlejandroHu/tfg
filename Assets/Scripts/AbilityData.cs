using System.Collections.Generic;
using UnityEngine;

// Puedes poner esto en un namespace si estás organizando así tu código
// namespace TuJuego.Habilidades
// {

/// <summary>
/// Define los posibles tipos de objetivos para una habilidad.
/// </summary>
public enum AbilityTargetType
{
    None,           // No requiere un objetivo específico (ej: un buff personal)
    Self,           // Se aplica al propio lanzador
    SingleAlly,     // Un solo aliado
    AllAllies,      // Todos los aliados
    SingleEnemy,    // Un solo enemigo
    AllEnemies      // Todos los enemigos
}

/// <summary>
/// Define los posibles tipos o categorías de una habilidad.
/// </summary>
public enum AbilityEffectType
{
    Damage,         // Causa daño
    Heal,           // Restaura HP
    RestoreMP,      // Restaura MP
    Buff,           // Aplica un estado beneficioso (mejora de stats)
    Debuff,         // Aplica un estado perjudicial (empeoramiento de stats)
    StatusEffect,   // Aplica un estado alterado (veneno, parálisis, etc.)
    CureStatus,     // Cura un estado alterado
    Special         // Otro tipo de efecto especial
}

/// <summary>
/// ScriptableObject para definir los datos base de cada habilidad o magia en el juego.
/// Crea assets de este tipo desde el menú: Assets > Create > TuJuego > Habilidad (o similar)
/// </summary>
[CreateAssetMenu(fileName = "NewAbilityData", menuName = "TuJuego/Crear Habilidad")]
public class AbilityData : ScriptableObject
{
    [Header("Información General de la Habilidad")]
    [Tooltip("Identificador único para esta habilidad (ej: 'fireball_lvl1', 'minor_heal').")]
    public string abilityID;

    [Tooltip("Nombre de la habilidad tal como se mostrará al jugador.")]
    public string abilityName = "Nueva Habilidad";

    [Tooltip("Icono de la habilidad para mostrar en menús o en la UI de combate.")]
    public Sprite icon;

    [Tooltip("Descripción detallada de lo que hace la habilidad. Se mostrará en la UI.")]
    [TextArea(3, 6)]
    public string description = "Descripción de la habilidad.";

    [Header("Costes y Requisitos")]
    [Tooltip("Coste de Puntos de Maná (MP) para usar esta habilidad. Poner 0 si no tiene coste de MP.")]
    public int mpCost = 0;
    // Podrías añadir otros costes aquí (ej: TP, objetos consumibles necesarios, etc.)
    // Podrías añadir requisitos de nivel o clase aquí también.

    [Header("Efectos y Objetivos")]
    [Tooltip("El tipo principal de efecto que produce esta habilidad (Daño, Curación, Buff, etc.).")]
    public AbilityEffectType effectType = AbilityEffectType.Damage;

    [Tooltip("A quién o quiénes afecta esta habilidad.")]
    public AbilityTargetType targetType = AbilityTargetType.SingleEnemy;

    [Tooltip("Potencia base de la habilidad (ej: cantidad de daño, cantidad de curación, porcentaje de buff/debuff). La fórmula de daño/curación final puede usar esto junto con los stats del lanzador/objetivo.")]
    public float power = 10f;

    // Podrías añadir más campos para efectos específicos:
    // Ejemplo: Si es un Buff/Debuff
    // public StatType statToModify; // Necesitarías un enum StatType (HP, MP, Attack, Defense, etc.)
    // public float buffDebuffDuration;
    // public bool isPercentageBuff; // Si el 'power' es un porcentaje o un valor fijo

    // Ejemplo: Si aplica un StatusEffect
    // public StatusEffectData statusEffectToApply; // Referencia a otro ScriptableObject StatusEffectData
    // public float statusEffectChance; // Probabilidad de aplicar el estado (0 a 1)
    // public int statusEffectDurationInTurns;

    // --- CAMPOS PARA ANIMACIÓN DE HABILIDAD ESPECÍFICA ---
    [Header("Animación y Efectos de Combate")]
    [Tooltip("Nombre del parámetro Trigger en el Animator Controller del personaje que lanza esta habilidad. Si está vacío, se podría usar un trigger genérico como 'AttackTrigger'.")]
    public string animationTriggerName; // Renombrado desde casterAnimationTrigger

    [Tooltip("Duración aproximada de la animación de esta habilidad en segundos. Si es 0 o negativo, CombatManager usará una duración por defecto.")]
    public float animationDuration = 0.8f; // Valor por defecto, ajústalo por habilidad

    [Tooltip("Referencia a un Prefab de efecto visual (VFX) que se instanciará en el objetivo o en el lanzador.")]
    public GameObject vfxPrefab; // Ya lo tenías

    [Tooltip("Referencia a un AudioClip para el sonido de la habilidad.")]
    public AudioClip sfxClip; // Ya lo tenías


    // --- MÉTODOS (Lógica de la Habilidad) ---
    // La lógica de "ejecutar" la habilidad podría estar aquí o en un sistema de combate.
    // Por ahora, este ScriptableObject es principalmente para datos.

    /// <summary>
    /// (Ejemplo) Intenta ejecutar el efecto de esta habilidad sobre uno o más objetivos.
    /// Esta es una implementación muy básica. Un sistema de combate real sería más complejo.
    /// </summary>
    /// <param name="caster">El personaje que lanza la habilidad.</param>
    /// <param name="targets">La lista de personajes objetivo.</param>
    public virtual void ExecuteEffect(Character caster, List<Character> targets)
    {
        // La lógica principal de aplicar daño, curación, etc., ahora reside en CombatManager.ExecuteSkill
        // para mantener el control centralizado de los Combatant y la UI.
        // Este método se conserva por si se quieren añadir efectos únicos directamente en el AbilityData
        // o si se refactoriza el sistema más adelante.

        if (caster != null)
        {
            Debug.Log($"AbilityData.ExecuteEffect: Habilidad '{abilityName}' llamada por '{caster.characterName}'. El efecto principal es manejado por CombatManager.");
        }
        else
        {
            Debug.LogWarning($"AbilityData.ExecuteEffect: Habilidad '{abilityName}' llamada sin un caster válido.");
        }
    }
}

// } // Fin del namespace si lo usas
