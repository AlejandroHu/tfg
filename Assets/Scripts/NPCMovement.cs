using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
   
    [Header("Movement")] 
    [SerializeField] private float moveSpeed = 2f; // Velocidad de movimiento del NPC

    // --- VARIABLES INTERNAS DE MOVIMIENTO ---
    private Vector2 currentMovementVector; // Almacena la dirección y magnitud del movimiento actual del NPC. Se pone a (0,0) cuando está quieto.
    private Vector3 startPosition;         // Posición inicial del NPC, usada como referencia para el patrón de patrulla.

    [Header("Animations")] 
    [SerializeField] private Animator anim; // Referencia al componente Animator del NPC 
    private string lastDirectionAnimKey = "Down"; // Almacena la última dirección como un string ("Up", "Down", "Left", "Right") para construir nombres de animación.

    private Rigidbody2D rb; // Referencia al componente Rigidbody2D del NPC, usado para el movimiento físico.

    [Header("Movement Pattern")] 
    [SerializeField] private float moveDistance = 1.28f; // Distancia que el NPC se moverá desde su startPosition en la patrolDirection.
    [SerializeField] private float waitTime = 1f;       // Tiempo que el NPC esperará al llegar al final de un tramo de patrulla o a su startPosition.

    [Header("Movement Direction")] 
    [SerializeField] private Vector2 patrolDirection = Vector2.right; // Dirección base en la que el NPC realizará su patrulla (ej: Vector2.right para moverse a la derecha).

   
    private Coroutine movementCoroutine; // Referencia a la corrutina de movimiento principal (MovePattern), para poder detenerla si es necesario.

    // Bandera pública (solo lectura externa) para saber si el movimiento del NPC está pausado por un sistema externo (como el diálogo).
    public bool IsExternallyPaused { get; private set; } = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();         // Obtiene el Rigidbody2D del mismo GameObject.
        startPosition = transform.position;     // Guarda la posición inicial del NPC.

        // Obtener el Animator, buscando en un hijo "CharacterSprite" si no está en el mismo GameObject.
        // (Asumiendo que el Animator podría estar en un hijo llamado "CharacterSprite" según tu estructura)
        if (anim == null)
        {
            Transform characterSprite = transform.Find("CharacterSprite"); // Nombre exacto del hijo
            if (characterSprite != null)
            {
                anim = characterSprite.GetComponent<Animator>();
            }
            else
            {
                anim = GetComponent<Animator>(); // Fallback al mismo GameObject
            }
        }
        if (anim == null)
        {
            Debug.LogError("NPCMovement: Animator no encontrado en " + gameObject.name + " o su hijo 'CharacterSprite'. Las animaciones no funcionarán.", this);
            enabled = false; // Deshabilitar script si falta el Animator
        }
    }

    private void Start()
    {
        // Asegurarse de tener las referencias (aunque Awake ya debería haberlas obtenido).
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Establece la dirección de animación inicial basada en la patrolDirection configurada.
        if (patrolDirection.normalized != Vector2.zero)
        {
            UpdateLastDirectionAnimKey(patrolDirection.normalized);
        }

        currentMovementVector = Vector2.zero; // El NPC empieza quieto.
        HandleAnimations();                   // Reproduce la animación de Idle inicial.

        // Inicia la corrutina de patrulla solo si no está pausado externamente desde el principio.
        if (!IsExternallyPaused)
        {
            StartPatrol();
        }
    }


    // Método para manejar qué animación se debe reproducir.
    private void HandleAnimations()
    {
        if (anim == null) return; // Si no hay Animator, no hacer nada.

        // Determina el prefijo del nombre de la animación ("Idle" o "Walking").
        string statePrefix = (currentMovementVector == Vector2.zero) ? "Idle" : "Walking";
        // Construye el nombre completo de la animación (ej: "IdleDown", "WalkingRight").
        string fullAnimationName = statePrefix + lastDirectionAnimKey;

        // Comprueba si la animación actual ya es la que se quiere reproducir.
        // Esto evita reiniciar la animación en cada frame si ya está corriendo, lo cual es bueno.
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(fullAnimationName))
        {
            anim.Play(fullAnimationName); // Reproduce la animación.
        }
    }

    // Actualiza la variable lastDirectionAnimKey ("Up", "Down", "Left", "Right")
    // basándose en el vector de dirección de movimiento.
    private void UpdateLastDirectionAnimKey(Vector2 direction)
    {
        // Si no hay dirección (o es muy pequeña), no cambiar la última dirección conocida.
        if (direction.sqrMagnitude < 0.01f) return;

        // Compara la magnitud de X e Y para determinar si el movimiento es más horizontal o vertical.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) // Movimiento horizontal predomina.
        {
            if (direction.x > 0) lastDirectionAnimKey = "Right";
            else lastDirectionAnimKey = "Left";
        }
        else // Movimiento vertical predomina (o son iguales, se prioriza Y).
        {
            if (direction.y > 0) lastDirectionAnimKey = "Up";
            else lastDirectionAnimKey = "Down";
        }
    }

    // Inicia la corrutina principal del patrón de movimiento.
    private void StartPatrol()
    {
        // Si ya hay una corrutina de movimiento, la detiene antes de iniciar una nueva.
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        // Inicia la corrutina MovePattern y guarda una referencia a ella.
        movementCoroutine = StartCoroutine(MovePattern());
    }

 
    // Corrutina que define el patrón de movimiento de patrulla (A -> B -> A -> esperar).
    private IEnumerator MovePattern()
    {
        while (true) // Bucle infinito para que el patrón se repita.
        {
            // Si está pausado externamente (ej: por diálogo), espera en este punto.
            if (IsExternallyPaused)
            {
                yield return null; // Espera un frame y vuelve a comprobar.
                continue;          // Salta el resto de la iteración del bucle.
            }

            // Calcula el punto B de la patrulla (startPosition + patrolDirection * moveDistance).
            Vector3 patternTargetPosition = startPosition + new Vector3(patrolDirection.x * moveDistance, patrolDirection.y * moveDistance, 0);
            // Inicia la sub-corrutina para moverse hacia el punto B.
            yield return StartCoroutine(MoveToPosition(patternTargetPosition));

            // Comprueba de nuevo si se pausó externamente después de completar el movimiento.
            if (IsExternallyPaused) { yield return null; continue; }

            // Inicia la sub-corrutina para moverse de regreso al punto A (startPosition).
            yield return StartCoroutine(MoveToPosition(startPosition));

            if (IsExternallyPaused) { yield return null; continue; }

            // Al llegar a startPosition, asegurarse de que esté en Idle.
            currentMovementVector = Vector2.zero;
            HandleAnimations();
            // Espera el tiempo definido en waitTime antes de repetir el patrón.
            yield return new WaitForSeconds(waitTime);
        }
    }

    // Corrutina para mover el NPC a una posición objetivo específica.
    private IEnumerator MoveToPosition(Vector3 target)
    {
        // Calcula la dirección normalizada hacia el objetivo.
        Vector2 directionToTarget = (target - transform.position).normalized;

        if (directionToTarget != Vector2.zero) // Solo si hay que moverse (no está ya en el target).
        {
            currentMovementVector = directionToTarget;        // Actualiza el vector de movimiento actual.
            UpdateLastDirectionAnimKey(currentMovementVector); // Actualiza la dirección para la animación.
            HandleAnimations();                               // Pone la animación de "Walking".
        }
        else // Si ya está en el target.
        {
            currentMovementVector = Vector2.zero; // Asegurar que está quieto.
            HandleAnimations();                   // Poner animación de "Idle".
            yield break;                          // Termina esta corrutina.
        }

        // Mientras no haya llegado al objetivo (con un pequeño margen de error de 0.05f).
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            // Si está pausado externamente (ej: por diálogo) mientras se mueve.
            if (IsExternallyPaused)
            {
                currentMovementVector = Vector2.zero; // Detener el vector de movimiento.
                HandleAnimations();                   // Poner en Idle (mirando en la última dirección).

                // Bucle de espera hasta que se quite la pausa externa.
                while (IsExternallyPaused)
                {
                    yield return null;
                }
                // Al quitarse la pausa, reanudar el movimiento hacia el target.
                directionToTarget = (target - transform.position).normalized; // Recalcular dirección.
                if (directionToTarget == Vector2.zero) // Si ya llegó mientras estaba pausado.
                {
                    HandleAnimations(); // Poner en Idle.
                    yield break;        // Terminar esta corrutina.
                }
                currentMovementVector = directionToTarget;        // Reanudar vector de movimiento.
                UpdateLastDirectionAnimKey(currentMovementVector); // Actualizar dirección de animación.
                HandleAnimations();                               // Poner en Walking.
            }

            // Determina la dirección actual en la que el NPC intenta moverse.
            Vector2 currentFacingDirection = currentMovementVector.normalized;
            // Si por alguna razón se detuvo pero no ha llegado, recalcular la dirección para el raycast.
            if (currentFacingDirection == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f)
            {
                currentFacingDirection = (target - transform.position).normalized;
            }

            // Bucle de espera si hay un obstáculo (jugador) en el camino Y no está pausado externamente.
            while (IsObstacleInPath(currentFacingDirection) && !IsExternallyPaused)
            {
                // Si se estaba moviendo cuando detectó el obstáculo, detenerse.
                if (currentMovementVector != Vector2.zero)
                {
                    currentMovementVector = Vector2.zero; // Detener vector de movimiento.
                    HandleAnimations();                   // Poner en Idle.
                }
                yield return null; // Esperar al siguiente frame y volver a comprobar IsObstacleInPath.
            }

            // Si estaba detenido por un obstáculo y ahora el camino está libre Y no está pausado externamente.
            if (currentMovementVector == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f && !IsExternallyPaused)
            {
                directionToTarget = (target - transform.position).normalized; // Recalcular dirección.
                if (directionToTarget != Vector2.zero) // Solo si hay una dirección válida.
                {
                    currentMovementVector = directionToTarget;        // Reanudar vector de movimiento.
                    UpdateLastDirectionAnimKey(currentMovementVector); // Actualizar dirección de animación.
                    HandleAnimations();                               // Poner en Walking.
                }
            }

            // Mover el NPC si no está pausado externamente y tiene una dirección de movimiento.
            if (!IsExternallyPaused && currentMovementVector != Vector2.zero)
            {
                // Calcula la nueva posición usando MoveTowards para un movimiento suave a velocidad constante.
                Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                rb.MovePosition(newPosition); // Mueve el Rigidbody2D a la nueva posición.
            }

            yield return null; // Espera al siguiente frame.
        }

        // Al llegar al objetivo.
        rb.MovePosition(target);              // Asegura que esté exactamente en la posición target.
        currentMovementVector = Vector2.zero; // Detener vector de movimiento.
        HandleAnimations();                   // Poner en Idle.
    }

    // Método para detectar si hay un obstáculo (jugador) en la dirección de movimiento.
    private bool IsObstacleInPath(Vector2 direction)
    {
        if (direction == Vector2.zero) return false; // Si no se mueve, no hay obstáculo en el camino.

        float checkDistance = 0.3f; // Distancia del rayo. AJUSTA ESTO según el tamaño de tus personajes/tiles.
        // Origen del rayo, con un pequeño offset para evitar que choque con el propio collider del NPC.
        Vector2 raycastOrigin = (Vector2)transform.position + direction * 0.4f; // Offset del origen. AJUSTA ESTO.

        // Lanza el rayo.
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, checkDistance);
        // Dibuja el rayo en la vista de Escena para depuración (solo visible en el Editor).
        Debug.DrawRay(raycastOrigin, direction * checkDistance, Color.red);

        if (hit.collider != null) // Si el rayo golpeó algo.
        {
            // Comprueba si el objeto golpeado tiene el tag "Player".
            if (hit.collider.CompareTag("Player"))
            {
                // Log para depuración, puedes comentarlo o quitarlo en la versión final.
                Debug.Log(gameObject.name + " detectó un obstáculo: " + hit.collider.name + " con tag: " + hit.collider.tag);
                return true; // Hay un obstáculo (el jugador).
            }
        }
        return false; // No hay obstáculo (o lo que golpeó no es el jugador).
    }


    /// <summary>
    /// Pausa o reanuda el patrón de movimiento del NPC.
    /// </summary>
    /// <param name="shouldPause">True para pausar, False para reanudar.</param>
    public void SetMovementPaused(bool shouldPause)
    {
        IsExternallyPaused = shouldPause; // Actualiza la bandera de pausa externa.

        if (shouldPause) // Si se debe pausar.
        {
            // Las corrutinas activas (MovePattern, MoveToPosition) detectarán IsExternallyPaused
            // y se pondrán en un estado de espera o se detendrán y pondrán al NPC en Idle.
            // Forzamos el estado Idle aquí también para asegurar.
            currentMovementVector = Vector2.zero;
            HandleAnimations();
        }
        else // Si se debe reanudar.
        {
            // Si la corrutina principal de patrulla no estaba activa (porque se detuvo completamente
            // o es la primera vez después de Start y estaba pausado), la (re)iniciamos.
            if (movementCoroutine == null && gameObject.activeInHierarchy && enabled)
            {
                StartPatrol();
            }
            // Si la corrutina MoveToPosition estaba en su bucle `while(IsExternallyPaused)`,
            // ahora saldrá de él y continuará su lógica de movimiento.
        }
    }

    /// <summary>
    /// Hace que el NPC se ponga en estado Idle y mire hacia un objetivo específico.
    /// Se asume que el movimiento ya ha sido pausado si es necesario por el sistema que llama a este método.
    /// </summary>
    /// <param name="targetToFace">El Transform del objeto hacia el que el NPC debe mirar.</param>
    public void SetIdleAndFaceTarget(Transform targetToFace)
    {
        currentMovementVector = Vector2.zero; // Asegurar estado Idle.

        if (targetToFace != null) // Si se proporcionó un objetivo.
        {
            // Calcula la dirección hacia el objetivo.
            Vector2 directionToTarget = (targetToFace.position - transform.position).normalized;
            // Actualiza la clave de dirección para la animación.
            UpdateLastDirectionAnimKey(directionToTarget);
        }
        // Si targetToFace es null, se quedará en Idle en su última dirección conocida (lastDirectionAnimKey no cambia).

        HandleAnimations(); // Reproducir la animación "Idle" + la nueva (o actual) lastDirectionAnimKey.
    }
}
