using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f; // Velocidad de movimiento del NPC
    private Vector2 movementDirection;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    [Header("Animations")]
    [SerializeField] private Animator anim;
    private string lastDirection = "Down";

    private Rigidbody2D rb;

    [Header("Movement Pattern")]
    [SerializeField] private float moveDistance = 64f; // Cuánto se mueve el NPC (2 tiles de 32x32)
    [SerializeField] private float waitTime = 1f; // Tiempo de espera al llegar a la posición

    [Header("Movement Direction")]
    [SerializeField] private Vector2 moveDirection = Vector2.right; // Dirección del movimiento (por defecto a la derecha)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        targetPosition = startPosition;
    }

    private void Start()
    {
        StartCoroutine(MovePattern()); // Iniciar el patrón de movimiento
    }

    private void FixedUpdate()
    {
        rb.velocity = movementDirection * moveSpeed; // Mover al NPC
    }

    private void HandleAnimations()
    {
        if (anim == null) return;

        string animationName = movementDirection == Vector2.zero ? "Idle" : "Walking";
        anim.Play(animationName + lastDirection); // Animación en función de la dirección
    }

    private IEnumerator MovePattern()
    {
        while (true)
        {
            // Determinar la dirección del movimiento
            targetPosition = startPosition + new Vector3(moveDirection.x * moveDistance, moveDirection.y * moveDistance, 0); // Mover en la dirección seleccionada
            yield return StartCoroutine(MoveToPosition(targetPosition));

            // Volver a la posición inicial
            yield return StartCoroutine(MoveToPosition(startPosition));

            yield return new WaitForSeconds(waitTime); // Espera antes de repetir
        }
    }

    private IEnumerator MoveToPosition(Vector3 target)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
        float startTime = Time.time;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            float journeyLength = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = journeyLength / distance;
            Vector3 newPosition = Vector3.Lerp(start, target, fractionOfJourney);

            rb.MovePosition(newPosition); 

            // Actualiza animaciones
            movementDirection = (target - transform.position).normalized;
            SetDirectionAnimation(movementDirection);

            yield return null;
        }

        rb.MovePosition(target); // Asegura posición final
        movementDirection = Vector2.zero;
        HandleAnimations();
    }


    private void SetDirectionAnimation(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        if (direction.y > 0.1f)
            lastDirection = "Up";
        else if (direction.y < -0.1f)
            lastDirection = "Down";
        else if (direction.x > 0.1f)
            lastDirection = "Right";
        else if (direction.x < -0.1f)
            lastDirection = "Left";

        HandleAnimations(); // Actualiza la animación
    }
}
