using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    private Vector2 currentMovementVector; // Indica si se está moviendo y en qué dirección general
    private Vector3 startPosition;

    [Header("Animations")]
    [SerializeField] private Animator anim;
    private string lastDirectionAnimKey = "Down"; // Para la clave de animación (Up, Down, Left, Right)

    private Rigidbody2D rb;

    [Header("Movement Pattern")]
    [SerializeField] private float moveDistance = 1.28f; // Ajusta según tu PixelsPerUnit o escala deseada
    [SerializeField] private float waitTime = 1f;

    [Header("Movement Direction")]
    [SerializeField] private Vector2 patrolDirection = Vector2.right; // Dirección base del patrón

    private Coroutine movementCoroutine;
    // Añadimos una bandera para saber si está pausado por un sistema externo (como el diálogo)
    public bool IsExternallyPaused { get; private set; } = false;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        if (anim == null) anim = GetComponent<Animator>(); // Asegurarse de tener el animator
        if (rb == null) rb = GetComponent<Rigidbody2D>(); // Asegurarse de tener el Rigidbody

        // Establecer la dirección inicial para la animación basada en la patrulla
        if (patrolDirection.normalized != Vector2.zero)
        {
            UpdateLastDirectionAnimKey(patrolDirection.normalized);
        }

        currentMovementVector = Vector2.zero; // Empezar en Idle
        HandleAnimations(); // Poner animación inicial (Idle + lastDirectionAnimKey)

        // Iniciar la patrulla solo si no está pausado externamente al inicio
        if (!IsExternallyPaused)
        {
            StartPatrol();
        }
    }

    private void HandleAnimations()
    {
        if (anim == null) return;

        string statePrefix = (currentMovementVector == Vector2.zero) ? "Idle" : "Walking";
        string fullAnimationName = statePrefix + lastDirectionAnimKey;

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(fullAnimationName))
        {
            anim.Play(fullAnimationName);
        }
    }

    private void UpdateLastDirectionAnimKey(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0) lastDirectionAnimKey = "Right";
            else lastDirectionAnimKey = "Left";
        }
        else
        {
            if (direction.y > 0) lastDirectionAnimKey = "Up";
            else lastDirectionAnimKey = "Down";
        }
    }

    private void StartPatrol()
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MovePattern());
    }

    private void StopPatrol()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        // Al detener la patrulla, asegurar que quede en Idle
        currentMovementVector = Vector2.zero;
        HandleAnimations();
    }

    private IEnumerator MovePattern()
    {
        while (true)
        {
            if (IsExternallyPaused) // Si está pausado por diálogo, esperar
            {
                yield return null;
                continue;
            }

            Vector3 patternTargetPosition = startPosition + new Vector3(patrolDirection.x * moveDistance, patrolDirection.y * moveDistance, 0);
            yield return StartCoroutine(MoveToPosition(patternTargetPosition));

            if (IsExternallyPaused) { yield return null; continue; } // Comprobar de nuevo después de moverse

            yield return StartCoroutine(MoveToPosition(startPosition));

            if (IsExternallyPaused) { yield return null; continue; } // Comprobar de nuevo después de moverse

            currentMovementVector = Vector2.zero;
            HandleAnimations(); // Asegurar Idle en startPosition
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        Vector2 directionToTarget = (target - transform.position).normalized;

        if (directionToTarget != Vector2.zero)
        {
            currentMovementVector = directionToTarget;
            UpdateLastDirectionAnimKey(currentMovementVector);
            HandleAnimations();
        }
        else
        {
            currentMovementVector = Vector2.zero;
            HandleAnimations();
            yield break;
        }

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            if (IsExternallyPaused) // Si se pausa por diálogo mientras se mueve a una posición
            {
                // Mantener currentMovementVector y lastDirectionAnimKey como estaban, pero HandleAnimations pondrá Idle
                currentMovementVector = Vector2.zero;
                HandleAnimations(); // Poner en Idle mirando en la última dirección de movimiento

                // Esperar hasta que se quite la pausa externa
                while (IsExternallyPaused)
                {
                    yield return null;
                }
                // Al quitarse la pausa, reanudar el movimiento hacia el target
                directionToTarget = (target - transform.position).normalized; // Recalcular por si acaso
                if (directionToTarget == Vector2.zero)
                { // Si ya llegó mientras estaba pausado
                    HandleAnimations(); // Idle
                    yield break;
                }
                currentMovementVector = directionToTarget;
                UpdateLastDirectionAnimKey(currentMovementVector);
                HandleAnimations(); // Caminar de nuevo
            }


            Vector2 currentFacingDirection = currentMovementVector.normalized;
            if (currentFacingDirection == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f)
            {
                currentFacingDirection = (target - transform.position).normalized;
            }

            while (IsObstacleInPath(currentFacingDirection))
            {
                if (IsExternallyPaused)
                { // Si el diálogo empieza MIENTRAS está bloqueado por obstáculo
                    currentMovementVector = Vector2.zero;
                    HandleAnimations();
                    while (IsExternallyPaused) yield return null;
                    // Al salir de pausa por diálogo, necesita re-evaluar obstáculo y dirección
                    // La lógica externa del while de MoveToPosition se encargará.
                }

                if (currentMovementVector != Vector2.zero)
                {
                    currentMovementVector = Vector2.zero;
                    HandleAnimations();
                }
                yield return null;
            }

            if (currentMovementVector == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f && !IsExternallyPaused)
            {
                directionToTarget = (target - transform.position).normalized;
                currentMovementVector = directionToTarget;
                UpdateLastDirectionAnimKey(currentMovementVector);
                HandleAnimations();
            }

            Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            rb.MovePosition(newPosition);

            yield return null;
        }

        rb.MovePosition(target);
        currentMovementVector = Vector2.zero;
        HandleAnimations();
    }

    private bool IsObstacleInPath(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;

        float checkDistance = 0.5f;
        Vector2 raycastOrigin = (Vector2)transform.position + direction * 0.1f;

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, checkDistance);
        Debug.DrawRay(raycastOrigin, direction * checkDistance, Color.red);

        if (hit.collider != null)
        {
            // MODIFICADO: Ahora comprueba si el objeto golpeado tiene el tag "Player" O "Obstacle"
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log(gameObject.name + " detectó un obstáculo: " + hit.collider.name + " con tag: " + hit.collider.tag); // Log para depuración
                return true;
            }
        }
        return false;
    }

    // ---------- NUEVOS MÉTODOS PÚBLICOS PARA EL SISTEMA DE DIÁLOGO ----------

    /// <summary>
    /// Pausa o reanuda el patrón de movimiento del NPC.
    /// </summary>
    /// <param name="shouldPause">True para pausar, False para reanudar.</param>
    public void SetMovementPaused(bool shouldPause)
    {
        IsExternallyPaused = shouldPause;

        if (shouldPause)
        {
            // Si estaba en medio de un movimiento específico (MoveToPosition),
            // la comprobación de IsExternallyPaused dentro de esa corrutina lo pondrá en Idle.
            // Si estaba en el waitTime de MovePattern, la comprobación también lo detendrá.
            // Nos aseguramos de que esté en Idle aquí también.
            currentMovementVector = Vector2.zero;
            HandleAnimations();
        }
        else
        {
            // Si no hay una corrutina de movimiento activa (porque se detuvo completamente
            // o porque es la primera vez después de Start y estaba pausado), la iniciamos.
            // La corrutina MovePattern ya maneja su propio bucle y reinicio interno de MoveToPosition.
            // Al quitar la pausa, la corrutina MovePattern o MoveToPosition debería reanudarse.
            // Si la corrutina principal 'movementCoroutine' fue detenida, necesitaríamos reiniciarla.
            // Por simplicidad, si se quita la pausa, y la corrutina no existe, la iniciamos.
            // Las comprobaciones internas de IsExternallyPaused manejarán la reanudación del estado lógico.

            // Si la corrutina principal (MovePattern) se detuvo explícitamente:
            if (movementCoroutine == null && gameObject.activeInHierarchy && enabled)
            {
                StartPatrol(); // Reinicia el patrón de patrulla si estaba completamente detenido.
            }
            // Si solo se pausó con IsExternallyPaused, las corrutinas existentes continuarán
            // y saldrán de sus bucles `while(IsExternallyPaused)`
        }
    }

    /// <summary>
    /// Hace que el NPC se ponga en estado Idle y mire hacia un objetivo específico.
    /// Asume que el movimiento ya ha sido pausado si es necesario.
    /// </summary>
    /// <param name="targetToFace">El Transform del objeto hacia el que el NPC debe mirar.</param>
    public void SetIdleAndFaceTarget(Transform targetToFace)
    {
        currentMovementVector = Vector2.zero; // Asegurar estado Idle

        if (targetToFace != null)
        {
            Vector2 directionToTarget = (targetToFace.position - transform.position).normalized;
            UpdateLastDirectionAnimKey(directionToTarget);
        }
        // Si targetToFace es null, se quedará en Idle en su última dirección (lastDirectionAnimKey no cambia)

        HandleAnimations(); // Reproducir la animación "Idle" + la nueva (o actual) lastDirectionAnimKey
    }
}
