using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    private Vector2 currentMovementVector;
    private Vector3 startPosition;

    [Header("Animations")]
    [SerializeField] private Animator anim;
    private string lastDirectionAnimKey = "Down";

    private Rigidbody2D rb;

    [Header("Movement Pattern")]
    [SerializeField] private float moveDistance = 1.28f;
    [SerializeField] private float waitTime = 1f;

    [Header("Movement Direction")]
    [SerializeField] private Vector2 patrolDirection = Vector2.right;
    [SerializeField] private LayerMask detectionLayerMask;


    private Coroutine movementCoroutine;
    public bool IsExternallyPaused { get; private set; } = false;

    private bool _wasInitialized = false; // Para asegurar que Start() se complete antes de OnEnable

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        if (anim == null)
        {
            Transform characterSprite = transform.Find("CharacterSprite"); // Nombre exacto del hijo
            if (characterSprite != null)
            {
                anim = characterSprite.GetComponent<Animator>();
            }
            else
            {
                anim = GetComponent<Animator>();
            }
        }
        if (anim == null)
        {
            Debug.LogError("NPCMovement: Animator no encontrado en " + gameObject.name + " o su hijo 'CharacterSprite'. Las animaciones no funcionarán.", this);
            enabled = false;
        }
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - Awake completado.");
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (patrolDirection.normalized != Vector2.zero)
        {
            UpdateLastDirectionAnimKey(patrolDirection.normalized);
        }

        currentMovementVector = Vector2.zero;
        HandleAnimations();

        _wasInitialized = true; // Marcar como inicializado
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - Start completado. _wasInitialized = true.");

        if (!IsExternallyPaused && gameObject.activeInHierarchy && enabled)
        {
            Debug.Log($"[{Time.frameCount}] {gameObject.name} - Start: Llamando a StartPatrol() porque no está pausado externamente.");
            StartPatrol();
        }
        else if (IsExternallyPaused)
        {
            Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - Start: No se inicia patrulla porque IsExternallyPaused es true al final de Start().");
        }
    }

    void OnEnable()
    {
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - OnEnable llamado. _wasInitialized: {_wasInitialized}, IsExternallyPaused: {IsExternallyPaused}, movementCoroutine: {(movementCoroutine == null ? "NULL" : "ACTIVO")}");
        // Si _wasInitialized es true (Start ya se ejecutó) y no está pausado, y la corrutina es null (pudo ser detenida por OnDisable)
        // entonces reiniciar patrulla. Esto maneja la reactivación después de un combate.
        if (_wasInitialized && !IsExternallyPaused && movementCoroutine == null && gameObject.activeInHierarchy && enabled)
        {
            Debug.Log($"[{Time.frameCount}] {gameObject.name} - OnEnable: Condiciones cumplidas para reiniciar patrulla (ej: post-combate), llamando a StartPatrol().");
            StartPatrol();
        }
        else
        {
            if (!_wasInitialized) Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - OnEnable: No se inicia patrulla porque _wasInitialized es false (Start aún no se ha completado).");
            if (IsExternallyPaused) Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - OnEnable: No se inicia patrulla porque IsExternallyPaused es true.");
            if (movementCoroutine != null) Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - OnEnable: No se inicia patrulla porque movementCoroutine ya está activo.");
        }
    }

    void OnDisable()
    {
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - OnDisable llamado. Deteniendo corrutina si existe.");
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
            Debug.Log($"[{Time.frameCount}] {gameObject.name} - OnDisable: movementCoroutine puesto a NULL.");
        }
        currentMovementVector = Vector2.zero;
        // Es arriesgado llamar a HandleAnimations aquí si el objeto se está destruyendo o el animador no es válido
        // if (anim != null && gameObject.activeInHierarchy) 
        // {
        //     HandleAnimations();
        // }
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
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - StartPatrol() llamado. IsExternallyPaused: {IsExternallyPaused}");
        if (IsExternallyPaused)
        {
            Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - StartPatrol: No se inicia porque IsExternallyPaused es true.");
            return; // No iniciar si está pausado externamente
        }

        if (movementCoroutine != null)
        {
            Debug.Log($"[{Time.frameCount}] {gameObject.name} - StartPatrol: Deteniendo corrutina existente antes de iniciar una nueva.");
            StopCoroutine(movementCoroutine);
        }
        movementCoroutine = StartCoroutine(MovePattern());
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - StartPatrol: Nueva corrutina MovePattern iniciada. movementCoroutine is {(movementCoroutine == null ? "NULL" : "ACTIVO")}.");
    }

    private IEnumerator MovePattern()
    {
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - MovePattern: Corrutina iniciada.");
        while (true)
        {
            if (IsExternallyPaused)
            {
                Debug.Log($"[{Time.frameCount}] {gameObject.name} - MovePattern: Pausado externamente, esperando.");
                currentMovementVector = Vector2.zero; // Asegurar que esté quieto mientras está pausado en este bucle
                HandleAnimations();
                yield return new WaitUntil(() => !IsExternallyPaused); // Esperar hasta que la pausa se quite
                Debug.Log($"[{Time.frameCount}] {gameObject.name} - MovePattern: Reanudado después de pausa externa.");
            }

            Vector3 patternTargetPosition = startPosition + new Vector3(patrolDirection.x * moveDistance, patrolDirection.y * moveDistance, 0);
            yield return StartCoroutine(MoveToPosition(patternTargetPosition));

            if (IsExternallyPaused) { /*Debug.Log("Pausado en MovePattern post B");*/ yield return new WaitUntil(() => !IsExternallyPaused); /*Debug.Log("Reanudado en MovePattern post B");*/ }

            yield return StartCoroutine(MoveToPosition(startPosition));

            if (IsExternallyPaused) { /*Debug.Log("Pausado en MovePattern post A");*/ yield return new WaitUntil(() => !IsExternallyPaused); /*Debug.Log("Reanudado en MovePattern post A");*/ }

            currentMovementVector = Vector2.zero;
            HandleAnimations();
            Debug.Log($"[{Time.frameCount}] {gameObject.name} - MovePattern: Esperando {waitTime}s.");
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        // Debug.Log($"[{Time.frameCount}] {gameObject.name} - MoveToPosition: Moviéndose a {target}. IsExternallyPaused: {IsExternallyPaused}");

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
            if (IsExternallyPaused)
            {
                currentMovementVector = Vector2.zero; HandleAnimations();
                yield return new WaitUntil(() => !IsExternallyPaused);
                directionToTarget = (target - transform.position).normalized;
                if (directionToTarget == Vector2.zero) { HandleAnimations(); yield break; }
                currentMovementVector = directionToTarget; UpdateLastDirectionAnimKey(currentMovementVector); HandleAnimations();
            }
            Vector2 currentFacingDirection = currentMovementVector.normalized;
            if (currentFacingDirection == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f)
            {
                currentFacingDirection = (target - transform.position).normalized;
            }
            while (IsObstacleInPath(currentFacingDirection) && !IsExternallyPaused)
            {
                if (currentMovementVector != Vector2.zero) { currentMovementVector = Vector2.zero; HandleAnimations(); }
                yield return null;
            }
            if (currentMovementVector == Vector2.zero && Vector3.Distance(transform.position, target) > 0.05f && !IsExternallyPaused)
            {
                directionToTarget = (target - transform.position).normalized;
                if (directionToTarget != Vector2.zero)
                {
                    currentMovementVector = directionToTarget; UpdateLastDirectionAnimKey(currentMovementVector); HandleAnimations();
                }
            }
            if (!IsExternallyPaused && currentMovementVector != Vector2.zero)
            {
                Vector3 newPosition = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                rb.MovePosition(newPosition);
            }
            yield return null;
        }
        rb.MovePosition(target);
        currentMovementVector = Vector2.zero;
        HandleAnimations();
    }

    private bool IsObstacleInPath(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;
        float checkDistance = 0.3f;
        Vector2 raycastOrigin = (Vector2)transform.position + direction * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, direction, checkDistance, detectionLayerMask);
        Debug.DrawRay(raycastOrigin, direction * checkDistance, Color.red);
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log(gameObject.name + " detectó un obstáculo: " + hit.collider.name + " con tag: " + hit.collider.tag);
                return true;
            }
        }
        return false;
    }

    public void SetMovementPaused(bool shouldPause)
    {
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetMovementPaused({shouldPause}) llamado. IsExternallyPaused ANTES: {IsExternallyPaused}, movementCoroutine: {(movementCoroutine == null ? "NULL" : "ACTIVO")}");
        IsExternallyPaused = shouldPause;

        if (shouldPause)
        {
            currentMovementVector = Vector2.zero;
            HandleAnimations();
        }
        else
        {
            if (gameObject.activeInHierarchy && enabled)
            {
                // Si se está reanudando Y la corrutina no está activa (pudo ser detenida por OnDisable o nunca inició correctamente)
                // Y _wasInitialized es true (Start ya se ejecutó)
                if (movementCoroutine == null && _wasInitialized)
                {
                    Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetMovementPaused(false): movementCoroutine era NULL y _wasInitialized es true, llamando a StartPatrol().");
                    StartPatrol();
                }
                else if (movementCoroutine != null)
                {
                    Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetMovementPaused(false): movementCoroutine NO era NULL. La corrutina debería reanudarse sola.");
                }
                else if (!_wasInitialized)
                {
                    Debug.LogWarning($"[{Time.frameCount}] {gameObject.name} - SetMovementPaused(false): No se inicia patrulla porque _wasInitialized es false.");
                }
            }
        }
        Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetMovementPaused({shouldPause}) - IsExternallyPaused DESPUÉS: {IsExternallyPaused}, movementCoroutine: {(movementCoroutine == null ? "NULL" : "ACTIVO")}");
    }

    public void SetIdleAndFaceTarget(Transform targetToFace)
    {
        currentMovementVector = Vector2.zero;
        if (targetToFace != null)
        {
            Vector2 directionToTarget = (targetToFace.position - transform.position).normalized;
            UpdateLastDirectionAnimKey(directionToTarget);
        }
        HandleAnimations();
    }
}
