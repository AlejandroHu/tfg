using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        private Vector2 movementDirection; // Esta se vuelve (0,0) cuando no hay input
        private Vector2 currentInput;

        [Header("Animations")]
        [SerializeField] private Animator anim;
        private string lastDirectionString = "Down"; // Renombrada para claridad

        // NUEVA PROPIEDAD PÚBLICA para que otros scripts sepan hacia dónde mira el jugador
        public Vector2 LastFacingVector { get; private set; } = Vector2.down; // Inicializar con un valor por defecto

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (anim == null) // Si no se asignó en el Inspector, intentar obtenerlo
            {
                // Asumiendo que el Animator está en el mismo GameObject o en un hijo "charactersprite"
                Transform characterSprite = transform.Find("charactersprite");
                if (characterSprite != null)
                {
                    anim = characterSprite.GetComponent<Animator>();
                }
                else
                {
                    anim = GetComponent<Animator>();
                }
            }

            // Establecer la dirección de "mirada" inicial basada en lastDirectionString
            UpdateLastFacingVectorFromString(lastDirectionString);
        }

        private void Update()
        {
            HandleAnimations();
        }

        private void FixedUpdate()
        {
            rb.velocity = movementDirection * moveSpeed;
        }

        private void HandleAnimations()
        {
            if (anim == null) return;

            string animationName = "";

            if (movementDirection == Vector2.zero) // O podrías usar currentInput.sqrMagnitude < 0.01f
            {
                animationName = "Idle";
            }
            else
            {
                animationName = "Walking";
            }
            // Usar lastDirectionString que retiene la última dirección de mirada
            anim.Play(animationName + lastDirectionString);
        }

        // Renombrada para claridad, ya que actualiza lastDirectionString y LastFacingVector
        private Vector2 ProcessInputToDirection(Vector2 input)
        {
            Vector2 calculatedDirection = Vector2.zero;

            if (Mathf.Abs(input.x) > 0.01f || Mathf.Abs(input.y) > 0.01f) // Si hay algún input significativo
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y)) // Priorizar movimiento horizontal
                {
                    if (input.x > 0.01f)
                    {
                        lastDirectionString = "Right";
                        calculatedDirection = Vector2.right;
                        LastFacingVector = Vector2.right;
                    }
                    else if (input.x < -0.01f)
                    {
                        lastDirectionString = "Left";
                        calculatedDirection = Vector2.left;
                        LastFacingVector = Vector2.left;
                    }
                }
                else // Priorizar movimiento vertical (o si son iguales)
                {
                    if (input.y > 0.01f)
                    {
                        lastDirectionString = "Up";
                        calculatedDirection = Vector2.up;
                        LastFacingVector = Vector2.up;
                    }
                    else if (input.y < -0.01f)
                    {
                        lastDirectionString = "Down";
                        calculatedDirection = Vector2.down;
                        LastFacingVector = Vector2.down;
                    }
                }
            }
            // Si no hay input (input.x e input.y son casi cero),
            // calculatedDirection será Vector2.zero.
            // lastDirectionString y LastFacingVector NO se actualizan aquí,
            // por lo que retienen la última dirección en la que el jugador miraba.

            return calculatedDirection;
        }

        // Helper para inicializar LastFacingVector en Awake
        private void UpdateLastFacingVectorFromString(string directionString)
        {
            switch (directionString)
            {
                case "Up": LastFacingVector = Vector2.up; break;
                case "Down": LastFacingVector = Vector2.down; break;
                case "Left": LastFacingVector = Vector2.left; break;
                case "Right": LastFacingVector = Vector2.right; break;
                default: LastFacingVector = Vector2.down; break; // Fallback
            }
        }


        // Método llamado por el Player Input component
        private void OnMove(InputValue value)
        {
            currentInput = value.Get<Vector2>().normalized; // Normalizar para evitar movimiento diagonal más rápido
            movementDirection = ProcessInputToDirection(currentInput);
        }
    }
}