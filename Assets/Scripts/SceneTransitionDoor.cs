using UnityEngine;

// Asegúrate de que este script esté en un GameObject que tenga un Collider2D
// y que esté en la capa que tu PlayerInteraction puede detectar.
public class SceneTransitionDoor : MonoBehaviour
{
    [Header("Configuración de la Transición")]
    [Tooltip("El nombre exacto de la escena a la que esta puerta lleva. Debe estar añadida en Build Settings.")]
    [SerializeField] private string sceneToLoad;

    [Tooltip("La posición (coordenadas X, Y) donde aparecerá el jugador en la nueva escena.")]
    [SerializeField] private Vector2 playerSpawnPosition;

    /// <summary>
    /// Este método público será llamado por el script de interacción del jugador.
    /// </summary>
    public void Transition()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"La puerta '{gameObject.name}' no tiene una 'Scene To Load' asignada.", this);
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"Transición iniciada: Cargando escena '{sceneToLoad}' y moviendo al jugador a {playerSpawnPosition}");
            SceneTransitionManager.Instance.LoadScene(sceneToLoad, playerSpawnPosition);
        }
        else
        {
            Debug.LogError("No se encontró una instancia de SceneTransitionManager en la escena. Asegúrate de que el GameObject que lo contiene esté en tu escena de inicio.");
        }
    }
}
