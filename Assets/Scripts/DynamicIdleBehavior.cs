using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicIdleBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("El componente Animator del NPC.")]
    [SerializeField] private Animator anim;
    [Tooltip("Tiempo mínimo que el NPC pasará mirando en una dirección antes de cambiar.")]
    [SerializeField] private float minLookTime = 2f;
    [Tooltip("Tiempo máximo que el NPC pasará mirando en una dirección antes de cambiar.")]
    [SerializeField] private float maxLookTime = 5f;

    [System.Serializable]
    public struct AllowedDirections
    {
        public bool lookUp;
        public bool lookDown;
        public bool lookLeft;
        public bool lookRight;
    }
    [Tooltip("Define en qué direcciones puede mirar este NPC.")]
    [SerializeField]
    private AllowedDirections allowedLookDirections =
        new AllowedDirections { lookUp = true, lookDown = true, lookLeft = true, lookRight = true }; // Por defecto, todas

    [Tooltip("Dirección inicial en la que mirará el NPC al empezar. Debe ser 'Up', 'Down', 'Left', o 'Right'.")]
    [SerializeField] private string initialDirection = "Down";

    private List<string> _possibleDirectionKeys = new List<string>();
    private string _currentDirectionKey;
    private Coroutine _idleCycleCoroutine;
    private bool _isExternalControlActive = false; // NUEVA: Bandera para control externo

    void Awake()
    {
        if (anim == null)
        {
            // Si tu Animator está en un GameObject hijo llamado "charactersprite"
            Transform characterSprite = transform.Find("charactersprite");
            if (characterSprite != null)
            {
                anim = characterSprite.GetComponent<Animator>();
            }
            else
            {
                // Si no, intenta obtenerlo del mismo GameObject
                anim = GetComponent<Animator>();
            }
        }

        if (anim == null)
        {
            Debug.LogError("DynamicIdleBehavior: No se encontró un componente Animator en " + gameObject.name + " o en su hijo 'charactersprite'.", this);
            enabled = false; // Deshabilitar el script si no hay animator
            return;
        }
        _currentDirectionKey = initialDirection; // Establecer la dirección inicial
    }

    void Start()
    {
        BuildPossibleDirectionsList();

        if (_possibleDirectionKeys.Count > 0)
        {
            if (!_possibleDirectionKeys.Contains(_currentDirectionKey) && _possibleDirectionKeys.Count > 0)
            {
                _currentDirectionKey = _possibleDirectionKeys[0];
                Debug.LogWarning($"DynamicIdleBehavior: initialDirection '{initialDirection}' no está en las permitidas. Usando '{_currentDirectionKey}'.", this);
            }

            PlayIdleAnimation(_currentDirectionKey);
            // Iniciar el ciclo solo si no está controlado externamente desde el inicio
            if (!_isExternalControlActive)
            {
                StartIdleCycle();
            }
        }
        else
        {
            Debug.LogWarning("DynamicIdleBehavior: No hay direcciones permitidas configuradas para " + gameObject.name + ". El NPC permanecerá en su pose inicial.", this);
            PlayIdleAnimation(_currentDirectionKey);
        }
    }

    void OnEnable()
    {
        // Si el script se reactiva y no está bajo control externo, reanudar.
        if (!_isExternalControlActive && anim != null && anim.isInitialized && _possibleDirectionKeys.Count > 0)
        {
            PlayIdleAnimation(_currentDirectionKey); // Restaurar la última pose conocida
            StartIdleCycle();
        }
    }

    void OnDisable()
    {
        StopIdleCycle();
    }

    void BuildPossibleDirectionsList()
    {
        _possibleDirectionKeys.Clear();
        if (allowedLookDirections.lookUp) _possibleDirectionKeys.Add("Up");
        if (allowedLookDirections.lookDown) _possibleDirectionKeys.Add("Down");
        if (allowedLookDirections.lookLeft) _possibleDirectionKeys.Add("Left");
        if (allowedLookDirections.lookRight) _possibleDirectionKeys.Add("Right");

        if (_possibleDirectionKeys.Count == 0)
        {
            if (!string.IsNullOrEmpty(initialDirection) && (initialDirection == "Up" || initialDirection == "Down" || initialDirection == "Left" || initialDirection == "Right"))
            {
                _possibleDirectionKeys.Add(initialDirection);
            }
            else
            {
                _possibleDirectionKeys.Add("Down"); // Fallback absoluto
            }
            Debug.LogWarning("DynamicIdleBehavior: No se especificaron direcciones en allowedLookDirections. Usando: " + _possibleDirectionKeys[0], this);
        }
    }

    void PlayIdleAnimation(string directionKey)
    {
        if (anim == null || string.IsNullOrEmpty(directionKey)) return;

        _currentDirectionKey = directionKey;
        string animationName = "Idle" + _currentDirectionKey;

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            anim.Play(animationName);
        }
    }

    IEnumerator IdleCycleRoutine()
    {
        while (true)
        {
            // Esperar el tiempo aleatorio
            float waitTime = Random.Range(minLookTime, maxLookTime);
            yield return new WaitForSeconds(waitTime);

            // Si está bajo control externo (ej: diálogo), esperar aquí hasta que se libere
            while (_isExternalControlActive)
            {
                yield return null; // Espera un frame y vuelve a comprobar
            }

            // Si, después de la espera y de que ya no esté bajo control externo, aún hay direcciones posibles...
            if (_possibleDirectionKeys.Count > 0)
            {
                string newDirectionKey = _currentDirectionKey;
                if (_possibleDirectionKeys.Count > 1)
                {
                    int attempts = 0;
                    while (newDirectionKey == _currentDirectionKey && attempts < _possibleDirectionKeys.Count * 2)
                    {
                        newDirectionKey = _possibleDirectionKeys[Random.Range(0, _possibleDirectionKeys.Count)];
                        attempts++;
                    }
                }
                else
                {
                    newDirectionKey = _possibleDirectionKeys[0];
                }
                PlayIdleAnimation(newDirectionKey);
            }
        }
    }

    private void StartIdleCycle()
    {
        StopIdleCycle();
        if (this.enabled && gameObject.activeInHierarchy && anim != null && anim.isInitialized && _possibleDirectionKeys.Count > 0)
        {
            _idleCycleCoroutine = StartCoroutine(IdleCycleRoutine());
        }
    }

    private void StopIdleCycle()
    {
        if (_idleCycleCoroutine != null)
        {
            StopCoroutine(_idleCycleCoroutine);
            _idleCycleCoroutine = null;
        }
    }

    // ---------- NUEVOS MÉTODOS PÚBLICOS PARA EL SISTEMA DE DIÁLOGO ----------

    /// <summary>
    /// Pausa el ciclo de idle dinámico y hace que el NPC mire a un objetivo específico.
    /// </summary>
    /// <param name="targetToFace">El Transform del objeto hacia el que el NPC debe mirar.</param>
    public void FocusOnTarget(Transform targetToFace)
    {
        _isExternalControlActive = true;
        // Opcional: Detener la corrutina explícitamente para una respuesta más inmediata
        // si no quieres que termine su WaitForSeconds actual.
        // StopIdleCycle(); 

        if (anim != null && targetToFace != null)
        {
            Vector2 directionToTarget = (targetToFace.position - transform.position).normalized;
            string newDirectionKey = _currentDirectionKey; // Mantener actual si no hay dirección clara

            if (directionToTarget.sqrMagnitude > 0.01f) // Solo calcular si hay una dirección válida
            {
                if (Mathf.Abs(directionToTarget.x) > Mathf.Abs(directionToTarget.y))
                {
                    newDirectionKey = directionToTarget.x > 0 ? "Right" : "Left";
                }
                else
                {
                    newDirectionKey = directionToTarget.y > 0 ? "Up" : "Down";
                }
            }
            PlayIdleAnimation(newDirectionKey); // Esto actualiza _currentDirectionKey y reproduce la animación
        }
        else if (anim != null) // Si no hay target, al menos asegurar que esté en su pose actual
        {
            PlayIdleAnimation(_currentDirectionKey);
        }
    }

    /// <summary>
    /// Reanuda el ciclo de idle dinámico. El NPC volverá a mirar aleatoriamente después de su próximo ciclo de espera.
    /// </summary>
    public void ResumeDynamicIdle()
    {
        _isExternalControlActive = false;
        // No es estrictamente necesario reiniciar la corrutina aquí si solo usas la bandera,
        // ya que el bucle en IdleCycleRoutine detectará que _isExternalControlActive es false y continuará.
        // Sin embargo, si detuviste explícitamente la corrutina en FocusOnTarget, necesitarías reiniciarla:
        // if (_idleCycleCoroutine == null && this.enabled && gameObject.activeInHierarchy)
        // {
        //     PlayIdleAnimation(_currentDirectionKey); // Asegurar que está en la última pose
        //     StartIdleCycle();
        // }
        // Por ahora, la corrutina existente se desbloqueará.
        // Si quieres que el NPC se quede mirando al jugador un poco más antes de cambiar,
        // el comportamiento actual (la corrutina continúa su waitTime) lo hace.
        // Si quieres que inmediatamente después de ResumeDynamicIdle pueda cambiar de dirección
        // tras un nuevo Random.Range(minLookTime, maxLookTime), entonces sí, StartIdleCycle() aquí.
        // Vamos a optar por la reactivacion del ciclo para que el timer se reinicie:
        if (this.enabled && gameObject.activeInHierarchy)
        {
            // Opcional: Podrías querer que se quede mirando al jugador un poco más,
            // en ese caso, no llames a StartIdleCycle() aquí y deja que la corrutina existente
            // termine su WaitForSeconds actual.
            // Si quieres que el timer para el siguiente cambio aleatorio se reinicie AHORA:
            PlayIdleAnimation(_currentDirectionKey); // Asegura la pose actual
            StartIdleCycle(); // Reinicia el ciclo y su temporizador
        }
    }
}
