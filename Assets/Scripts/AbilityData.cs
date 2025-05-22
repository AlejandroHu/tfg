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

    [Header("Animaciones y Efectos Visuales (Placeholders)")]
    [Tooltip("Nombre o referencia a la animación que se reproducirá cuando el personaje use esta habilidad.")]
    public string casterAnimationTrigger; // Ej: "CastSpell", "UseSkill"
    [Tooltip("Referencia a un Prefab de efecto visual (VFX) que se instanciará en el objetivo o en el lanzador.")]
    public GameObject vfxPrefab;
    [Tooltip("Referencia a un AudioClip para el sonido de la habilidad.")]
    public AudioClip sfxClip;


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
        if (caster == null || targets == null || targets.Count == 0)
        {
            Debug.LogWarning("ExecuteEffect: Lanzador o lista de objetivos no válidos para " + abilityName);
            return;
        }

        // Comprobar y gastar coste de MP (si lo tiene)
        if (mpCost > 0)
        {
            if (!caster.SpendMana(mpCost)) // Asume que Character.cs tiene un método SpendMana(int cost) que devuelve bool
            {
                Debug.Log(caster.characterName + " no tiene suficiente MP para usar " + abilityName);
                // Aquí podrías notificar a la UI o al sistema de combate que falló por falta de MP.
                return; // No ejecutar el efecto si no se puede pagar el coste.
            }
        }

        Debug.Log(caster.characterName + " usa " + abilityName + "!");
        // Aquí iría la lógica para reproducir animación del lanzador, VFX, SFX.

        foreach (Character target in targets)
        {
            if (target == null) continue;

            switch (effectType)
            {
                case AbilityEffectType.Damage:
                    // Lógica de daño muy simple. Un sistema real consideraría stats del lanzador y del objetivo.
                    // int damageDealt = Mathf.RoundToInt(power + (caster.Attack * 0.5f) - target.Defense); // Ejemplo de fórmula
                    int damageDealt = Mathf.Max(1, Mathf.RoundToInt(power)); // Daño base por ahora
                    Debug.Log(target.characterName + " recibe " + damageDealt + " de daño de " + abilityName);
                    target.TakeDamage(damageDealt); // Asume que Character.cs tiene TakeDamage(int amount)
                    break;
                case AbilityEffectType.Heal:
                    int amountHealed = Mathf.RoundToInt(power);
                    target.Heal(amountHealed); // Asume que Character.cs tiene Heal(int amount)
                    break;
                case AbilityEffectType.RestoreMP:
                    target.RestoreMana(Mathf.RoundToInt(power)); // Asume que Character.cs tiene RestoreMana(int amount)
                    break;
                // Implementar otros tipos de efectos (Buff, Debuff, StatusEffect) aquí...
                default:
                    Debug.LogWarning("Efecto de habilidad no implementado para: " + effectType.ToString());
                    break;
            }
        }
    }
}

// } // Fin del namespace si lo usas
