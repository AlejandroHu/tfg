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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        // Establecer la dirección inicial para la animación basada en la patrulla
        if (patrolDirection.normalized != Vector2.zero)
        {
            UpdateLastDirectionAnimKey(patrolDirection.normalized);
        }
        // El currentMovementVector es (0,0) al inicio, así que HandleAnimations() pondrá Idle
        HandleAnimations(); // Poner animación inicial (Idle + lastDirectionAnimKey)

        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MovePattern());
    }

    private void HandleAnimations()
    {
        if (anim == null) return;

        string statePrefix = (currentMovementVector == Vector2.zero) ? "Idle" : "Walking";
        string fullAnimationName = statePrefix + lastDirectionAnimKey;

        // Para evitar reiniciar la animación si ya se está reproduciendo (opcional, pero bueno)
        // Esto es más útil si tus animaciones no son de loop perfecto o si Play() causa un pequeño "salto"
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(fullAnimationName))
        {
            anim.Play(fullAnimationName);
            // Para transiciones más suaves, podrías considerar anim.CrossFade en el futuro:
            // anim.CrossFade(fullAnimationName, 0.1f); // 0.1f es la duración de la transición
        }
    }

    private void UpdateLastDirectionAnimKey(Vector2 direction)
    {
        // Solo actualiza la dirección si el vector de dirección no es (casi) cero
        // Esto evita que lastDirectionAnimKey se pierda cuando el NPC se detiene.
        if (direction.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) // Movimiento horizontal predomina
        {
            if (direction.x > 0) lastDirectionAnimKey = "Right";
            else lastDirectionAnimKey = "Left";
        }
        else // Movimiento vertical predomina (o son iguales, se prioriza Y)
        {
            if (direction.y > 0) lastDirectionAnimKey = "Up";
            else lastDirectionAnimKey = "Down";
        }
    }

    private IEnumerator MovePattern()
    {
        while (true)
        {
            Vector3 patternTargetPosition = startPosition + new Vector3(patrolDirection.x * moveDistance, patrolDirection.y * moveDistance, 0);
            yield return StartCoroutine(MoveToPosition(patternTargetPosition));

            // Volver a la posición inicial
            yield return StartCoroutine(MoveToPosition(startPosition));

            // Al llegar a startPosition, ya se habrá puesto en Idle por MoveToPosition.
            // Así que solo esperamos.
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        Vector2 directionToTarget = (target - transform.position).normalized;

        if (directionToTarget != Vector2.zero) // Solo si hay que moverse
        {
            currentMovementVector = directionToTarget;
            UpdateLastDirectionAnimKey(currentMovementVector);
            HandleAnimations(); // Inicia animación de caminar
        }
        else
        {
            currentMovementVector = Vector2.zero; // Ya está en el target
            HandleAnimations();
            yield break; // Salir si ya está en el objetivo
        }


        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            Vector2 currentFacingDirection = currentMovementVector.normalized;
            if (currentFacingDirection == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f)
            {
                currentFacingDirection = (target - transform.position).normalized;
            }

            while (IsObstacleInPath(currentFacingDirection))
            {
                if (currentMovementVector != Vector2.zero) // Solo la primera vez que se detiene
                {
                    currentMovementVector = Vector2.zero;
                    HandleAnimations(); // Poner animación de Idle
                }
                yield return null;
            }

            if (currentMovementVector == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f) // Si estaba en Idle y obstáculo se fue
            {
                directionToTarget = (target - transform.position).normalized;
                currentMovementVector = directionToTarget;
                UpdateLastDirectionAnimKey(currentMovementVector); // Actualiza la dirección antes de animar
                HandleAnimations(); // Poner animación de caminar de nuevo
            }

            Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            rb.MovePosition(newPosition);

            // No es necesario actualizar la animación en cada frame de MoveTowards si ya está caminando en la dir correcta.
            // HandleAnimations(); // Podría ser demasiado si se llama cada frame

            yield return null;
        }

        rb.MovePosition(target);
        currentMovementVector = Vector2.zero;
        HandleAnimations(); // Animación de Idle al llegar
    }

    private bool IsObstacleInPath(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;

        float checkDistance = 0.5f; // AJUSTA ESTO
        Vector2 raycastOrigin = (Vector2)transform.position + direction * 0.1f;

        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, checkDistance);
        Debug.DrawRay(raycastOrigin, direction * checkDistance, Color.red);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
}
