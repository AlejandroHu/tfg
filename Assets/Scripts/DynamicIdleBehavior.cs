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

    void Awake()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        if (anim == null)
        {
            Debug.LogError("DynamicIdleBehavior: No se encontró un componente Animator en " + gameObject.name, this);
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
            // Asegurar que la dirección inicial sea válida o elegir una de las permitidas
            if (!_possibleDirectionKeys.Contains(_currentDirectionKey) && _possibleDirectionKeys.Count > 0)
            {
                _currentDirectionKey = _possibleDirectionKeys[0]; // Usar la primera permitida como fallback
                Debug.LogWarning($"DynamicIdleBehavior: initialDirection '{initialDirection}' no está en las permitidas. Usando '{_currentDirectionKey}'.", this);
            }

            PlayIdleAnimation(_currentDirectionKey); // Reproducir la animación idle inicial
            StartIdleCycle(); // Iniciar el ciclo de cambio de dirección
        }
        else
        {
            // Esto no debería ocurrir si BuildPossibleDirectionsList tiene un fallback, pero por si acaso.
            Debug.LogWarning("DynamicIdleBehavior: No hay direcciones permitidas configuradas para " + gameObject.name + ". El NPC permanecerá en su pose inicial.", this);
            PlayIdleAnimation(_currentDirectionKey); // Intentar reproducir la pose inicial
        }
    }

    void OnEnable()
    {
        // Si el script se reactiva (y no es la primera vez, que lo maneja Start),
        // y el Animator está listo, reanudar el ciclo.
        if (anim != null && anim.isInitialized && _possibleDirectionKeys.Count > 0)
        {
            PlayIdleAnimation(_currentDirectionKey); // Restaurar la última pose conocida
            StartIdleCycle();
        }
    }

    void OnDisable()
    {
        // Detener la corrutina si el objeto se desactiva para evitar errores.
        StopIdleCycle();
    }

    void BuildPossibleDirectionsList()
    {
        _possibleDirectionKeys.Clear();
        if (allowedLookDirections.lookUp) _possibleDirectionKeys.Add("Up");
        if (allowedLookDirections.lookDown) _possibleDirectionKeys.Add("Down");
        if (allowedLookDirections.lookLeft) _possibleDirectionKeys.Add("Left");
        if (allowedLookDirections.lookRight) _possibleDirectionKeys.Add("Right");

        // Si el usuario no marcó ninguna dirección, por defecto usamos la initialDirection o "Down"
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

        // Comprobación opcional para ver si el estado de animación existe antes de intentar reproducirlo
        // (requiere que los nombres de estado en el Animator coincidan exactamente con "IdleUp", "IdleDown", etc.)
        // bool stateExists = false;
        // for (int i = 0; i < anim.layerCount; i++) {
        //    if (anim.HasState(i, Animator.StringToHash(animationName))) {
        //        stateExists = true;
        //        break;
        //    }
        // }
        // if(!stateExists) {
        //    Debug.LogWarning($"DynamicIdleBehavior: Animation state '{animationName}' not found in Animator on {gameObject.name}", this);
        //    return;
        // }

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            anim.Play(animationName);
        }
    }

    IEnumerator IdleCycleRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minLookTime, maxLookTime);
            yield return new WaitForSeconds(waitTime);

            if (_possibleDirectionKeys.Count > 0)
            {
                string newDirectionKey = _currentDirectionKey;
                // Intentar elegir una dirección diferente a la actual, si hay más de una opción
                if (_possibleDirectionKeys.Count > 1)
                {
                    int attempts = 0; // Para evitar un bucle infinito si algo va mal
                    while (newDirectionKey == _currentDirectionKey && attempts < _possibleDirectionKeys.Count * 2)
                    {
                        newDirectionKey = _possibleDirectionKeys[Random.Range(0, _possibleDirectionKeys.Count)];
                        attempts++;
                    }
                }
                else
                {
                    newDirectionKey = _possibleDirectionKeys[0]; // Solo una dirección posible
                }
                PlayIdleAnimation(newDirectionKey);
            }
        }
    }

    private void StartIdleCycle()
    {
        StopIdleCycle(); // Detener cualquier ciclo anterior primero
        // Solo iniciar si el componente está activo y el animator está listo
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
}
