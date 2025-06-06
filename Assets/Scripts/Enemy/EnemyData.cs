using UnityEngine;
using System.Collections.Generic; // Necesario para List

// Asegúrate de que el namespace de AbilityData sea accesible si lo usas
// using TuJuego.Habilidades; // Si AbilityData.cs está en este namespace

/// <summary>
/// ScriptableObject para definir los datos base de cada tipo de enemigo en el juego.
/// Crea assets de este tipo desde el menú: Assets > Create > TuJuego > Datos de Enemigo (o similar)
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "TuJuego/Crear Datos de Enemigo")]
public class EnemyData : ScriptableObject
{
    [Header("Información General del Enemigo")]
    [Tooltip("Identificador único para este tipo de enemigo (ej: 'goblin_warrior', 'slime_blue').")]
    public string enemyID;

    [Tooltip("Nombre del enemigo tal como se mostrará al jugador (ej: 'Limo', 'Guerrero Goblin').")]
    public string enemyName = "Nuevo Enemigo";

    [Tooltip("Sprite que se usará para este enemigo en la pantalla de combate (vista 3/4, 48x48 píxeles).")]
    public Sprite battleSprite; // El sprite de 48x48 para el combate

    // --- NUEVO: Animator Controller para el combate específico del enemigo ---
    [Tooltip("Animator Controller a usar para este tipo de enemigo en combate. Debe contener estados como Idle_Combat, AttackTrigger, HitTrigger, DefeatTrigger, etc.")]
    public RuntimeAnimatorController combatAnimatorController;

    // Podrías añadir aquí un 'explorationSprite' si el sprite en el mapa es diferente al del combate,
    // aunque el GameObject en el mapa ya tendrá su propio SpriteRenderer.

    [Header("Estadísticas de Combate Base")]
    [Tooltip("Puntos de Vida (HP) máximos del enemigo.")]
    public int maxHP = 50;
    [Tooltip("Ataque base del enemigo.")]
    public int baseAttack = 8;
    [Tooltip("Defensa base del enemigo.")]
    public int baseDefense = 3;
    [Tooltip("Ataque Mágico base del enemigo, si aplica.")]
    public int baseMagicAttack = 0;
    [Tooltip("Defensa Mágica base del enemigo, si aplica.")]
    public int baseMagicDefense = 0;
    [Tooltip("Velocidad base del enemigo, para determinar el orden de turno.")]
    public int baseSpeed = 5;
    // Podrías añadir más stats como Evasión, Puntería, resistencias elementales, etc.

    [Header("Comportamiento y Habilidades")]
    [Tooltip("Si está marcado, este enemigo usará una IA de jefe (usará habilidades).")]
    public bool isBoss = false; // --- NUEVO: Bandera para identificar jefes ---
    [Tooltip("Lista de habilidades que este enemigo puede usar si es un jefe.")]
    public List<AbilityData> abilities = new List<AbilityData>();

    [Header("Recompensas al Ser Derrotado")]
    [Tooltip("Cantidad de Puntos de Experiencia (XP) que el jugador obtiene al derrotar a este enemigo.")]
    public int xpReward = 10;

    // --- NUEVO: Campos para Sonidos de Combate ---
    [Header("Sonidos de Combate")]
    [Tooltip("Sonido que se reproduce cuando este enemigo realiza un ataque básico.")]
    public AudioClip basicAttackSound;
    [Tooltip("Sonido que se reproduce cuando este enemigo recibe un golpe.")]
    public AudioClip takeHitSound;
    [Tooltip("Sonido que se reproduce cuando este enemigo es derrotado.")]
    public AudioClip defeatSound;
    // --- FIN NUEVO ---

    // Para el loot (objetos que deja caer), podrías tener una lista más compleja.
    // Ejemplo simple:
    [Tooltip("Opcional: ItemData del objeto que este enemigo podría dejar caer.")]
    public ItemData itemDrop;
    [Tooltip("Probabilidad (0.0 a 1.0) de que este enemigo deje caer el 'itemDrop'.")]
    [Range(0f, 1f)]
    public float itemDropChance = 0.1f; // 10% de probabilidad

    // Ejemplo más avanzado para múltiples drops con diferentes probabilidades:
    // public List<ItemDropInfo> potentialDrops = new List<ItemDropInfo>();
    // [System.Serializable]
    // public class ItemDropInfo {
    //    public ItemData item;
    //    [Range(0f, 1f)] public float chance;
    //    public int minQuantity = 1;
    //    public int maxQuantity = 1;
    // }

    // Podrías añadir aquí campos para resistencias/debilidades elementales,
    // tipo de enemigo (para fortalezas/debilidades), etc.
}
