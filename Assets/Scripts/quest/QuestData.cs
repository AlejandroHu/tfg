using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Define el estado actual de una misión para el jugador.
/// </summary>
public enum QuestStatus
{
    Inactive,   // La misión no ha sido aceptada todavía.
    Active,     // La misión está en curso.
    Completed,  // Los objetivos se han cumplido, pero no se ha entregado.
    Claimed     // La misión ha sido entregada y las recompensas recibidas.
}

/// <summary>
/// Define el tipo de objetivo para una misión.
/// </summary>
public enum GoalType
{
    Kill,   // Objetivo de matar a un número de enemigos de un tipo específico.
    Collect // Objetivo de recolectar un número de objetos de un tipo específico.
    // (Se pueden añadir más tipos, como: Talk, Reach, Escort...)
}

/// <summary>
/// Clase serializable que define un único objetivo dentro de una misión.
/// Al ser [System.Serializable], podremos editarla en el Inspector de QuestData.
/// </summary>
[System.Serializable]
public class QuestGoal
{
    [Tooltip("El tipo de este objetivo (Matar, Recolectar, etc.).")]
    public GoalType goalType;

    [Tooltip("El ID del objetivo (ej: el 'enemyID' del enemigo a matar, o el 'itemID' del objeto a recolectar).")]
    public string requiredID;

    [Tooltip("La cantidad necesaria para completar este objetivo.")]
    public int requiredAmount;

    // El progreso actual se manejará en el QuestManager, no aquí,
    // ya que este es solo el ScriptableObject de datos.
}

/// <summary>
/// ScriptableObject que sirve como plantilla para cada misión del juego.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "TuJuego/Crear Misión (Quest)")]
public class QuestData : ScriptableObject
{
    [Header("Información General")]
    [Tooltip("ID único de la misión (ej: 'main_quest_01', 'kill_slimes_01').")]
    public string questID;
    [Tooltip("Título de la misión que verá el jugador.")]
    public string title;
    [Tooltip("Descripción detallada de la misión para el diario de misiones.")]
    [TextArea(4, 8)]
    public string description;

    [Header("Objetivos de la Misión")]
    [Tooltip("Lista de todos los objetivos que se deben cumplir para completar la misión.")]
    public List<QuestGoal> goals = new List<QuestGoal>();

    [Header("Recompensas")]
    [Tooltip("Cantidad de Puntos de Experiencia (XP) que se obtienen al completar la misión.")]
    public int experienceReward;
    // public int goldReward; // Podrías añadir oro
    [Tooltip("Lista de objetos que se obtienen como recompensa.")]
    public List<ItemData> itemRewards = new List<ItemData>();
    // (Para las recompensas de objetos, necesitarás una forma de especificar la cantidad de cada uno si es variable)

    // El estado actual de la misión (Active, Completed, etc.) lo gestionará el QuestManager,
    // ya que es específico del progreso del jugador, no un dato de la misión en sí.
}
