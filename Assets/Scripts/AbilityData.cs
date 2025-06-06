using UnityEngine;
using System.Collections.Generic;
using TopDown; // Asegúrate de que Character sea accesible

// Los enums de AbilityData sí están bien aquí.

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
/// Crea assets de este tipo desde el menú: Assets > Create > TuJuego > Habilidades > AbilityData
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

    [Header("Efectos y Objetivos")]
    [Tooltip("El tipo principal de efecto que produce esta habilidad (Daño, Curación, Buff, etc.).")]
    public AbilityEffectType effectType = AbilityEffectType.Damage;

    [Tooltip("A quién o quiénes afecta esta habilidad.")]
    public AbilityTargetType targetType = AbilityTargetType.SingleEnemy;

    [Tooltip("Potencia base de la habilidad (ej: cantidad de daño, cantidad de curación, etc.).")]
    public float power = 10f;

    // --- CAMPOS PARA ANIMACIÓN DE HABILIDAD ESPECÍFICA ---
    [Header("Animación y Efectos de Combate")]
    [Tooltip("Nombre del parámetro Trigger en el Animator Controller del personaje que lanza esta habilidad. Si está vacío, se podría usar un trigger genérico como 'AttackTrigger'.")]
    public string animationTriggerName;

    [Tooltip("Duración aproximada de la animación de esta habilidad en segundos. Si es 0 o negativo, CombatManager usará una duración por defecto.")]
    public float animationDuration = 0.8f; // Valor por defecto, ajústalo por habilidad

    [Tooltip("Referencia a un Prefab de efecto visual (VFX) que se instanciará en el objetivo o en el lanzador. Si la habilidad lanza un proyectil, este sería el prefab del proyectil.")]
    public GameObject vfxPrefab;
    // --- MODIFICADO: Campo de sfxClip renombrado y añadido sfxImpactClip ---
    [Tooltip("Sonido que se reproduce cuando se LANZA la habilidad.")]
    public AudioClip launchSound;
    [Tooltip("Sonido que se reproduce cuando la habilidad IMPACTA (si es proyectil) o en el objetivo (si es efecto directo).")]
    public AudioClip impactSound;
    // --- FIN MODIFICACIÓN ---


    // El método ExecuteEffect se mantiene como lo tenías, ya que la lógica de aplicar
    // el efecto real (daño, curación) y disparar la animación se maneja en CombatManager.cs
    // para este prototipo. Si quisieras que cada AbilityData tuviera una lógica de efecto
    // completamente única, este método se expandiría mucho o usarías clases derivadas.
    public virtual void ExecuteEffect(Character caster, List<Character> targets)
    {
        if (caster == null)
        {
            Debug.LogWarning("ExecuteEffect: Caster es null para " + abilityName);
            return;
        }

        // La deducción de MP y la lógica principal de efectos ahora están en CombatManager.ExecuteSkill.
        // Este método podría usarse en el futuro para efectos muy específicos de la habilidad
        // que no son cubiertos por la lógica genérica del CombatManager.
        // Por ahora, solo un log para indicar que se llamó.
        Debug.Log($"AbilityData.ExecuteEffect llamada para {abilityName} por {caster.characterName}. El efecto principal es manejado por CombatManager.");
    }
}
