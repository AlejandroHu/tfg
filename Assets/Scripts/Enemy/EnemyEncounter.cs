using UnityEngine;
using System.Collections.Generic; // Necesario para List

// Asegúrate de que el namespace de EnemyData (que crearemos pronto) sea accesible
// using TuJuego.Enemigos; // Si EnemyData.cs estará en este namespace

/// <summary>
/// Script para adjuntar a los GameObjects de enemigos visibles en el mapa de exploración.
/// Contiene la información del grupo de enemigos que se encontrarán en combate.
/// </summary>
public class EnemyEncounter : MonoBehaviour
{
    [Header("Configuración del Encuentro")]
    [Tooltip("Lista de los datos de los enemigos que participarán en este encuentro. Asigna aquí tus assets de EnemyData.")]
    public List<EnemyData> enemyGroup = new List<EnemyData>();

    // [Tooltip("Identificador único para este encuentro, por si necesitas rastrear si ya fue derrotado.")]
    // public string encounterID; 

    [Tooltip("¿Este grupo de enemigos desaparece permanentemente después de ser derrotado una vez?")]
    public bool defeatPermanently = false;

    [HideInInspector]
    public bool isDefeated = false;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "' no tiene un Collider2D. No podrá ser detectado por el jugador.", this);
        }
        else
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning("EnemyEncounter en '" + gameObject.name + "': Se recomienda que su Collider2D sea 'Is Trigger = true' para facilitar la detección de interacción por el jugador.", this);
            }
        }
    }

    public void MarkAsDefeated()
    {
        isDefeated = true;
        if (defeatPermanently)
        {
            gameObject.SetActive(false);
            Debug.Log("Encuentro con " + gameObject.name + " marcado como derrotado permanentemente y desactivado.");
        }
        else
        {
            gameObject.SetActive(false);
            Debug.Log("Encuentro con " + gameObject.name + " marcado como derrotado y desactivado (temporalmente).");
        }
    }
}
