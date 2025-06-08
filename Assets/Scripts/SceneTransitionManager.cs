using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TopDown;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Configuración de Transición")]
    [Tooltip("El CanvasGroup del panel negro que se usará para el efecto de fundido.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("La duración del efecto de fundido en segundos.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;
    private Vector2 nextPlayerSpawnPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Asegurarse de que el panel de fundido esté totalmente transparente al inicio.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Inicia el proceso de transición a una nueva escena.
    /// </summary>
    /// <param name="sceneName">El nombre de la escena a cargar (debe estar en Build Settings).</param>
    /// <param name="spawnPosition">La posición donde aparecerá el jugador en la nueva escena.</param>
    public void LoadScene(string sceneName, Vector2 spawnPosition)
    {
        if (!isTransitioning)
        {
            nextPlayerSpawnPosition = spawnPosition;
            StartCoroutine(TransitionCoroutine(sceneName));
        }
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        // 1. Fade Out (fundido a negro)
        yield return StartCoroutine(Fade(1f));

        // 2. Cargar la nueva escena
        AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);

        // 3. Esperar a que la escena se cargue completamente
        while (!sceneLoadOperation.isDone)
        {
            yield return null;
        }

        // 4. Mover al jugador a la posición de spawn
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.transform.position = nextPlayerSpawnPosition;
            Debug.Log($"Jugador movido a la posición de spawn: {nextPlayerSpawnPosition}");
        }
        else
        {
            Debug.LogError("SceneTransitionManager: No se pudo encontrar al jugador en la nueva escena para posicionarlo.");
        }

        // 5. Fade In (fundido para mostrar la nueva escena)
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("Fade Canvas Group no está asignado en SceneTransitionManager. No se mostrará el efecto de fundido.");
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true; // Bloquear input durante el fundido
        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0)
        {
            fadeCanvasGroup.blocksRaycasts = false; // Desbloquear input al final
        }
    }
}
