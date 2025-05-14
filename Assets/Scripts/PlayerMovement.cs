using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown
{
    public class PlayerMovement : MonoBehaviour
    {
        // --- VARIABLES CONFIGURABLES DESDE EL INSPECTOR ---
        [Header("Movement")] // Encabezado para organizar las variables en el Inspector de Unity
        [SerializeField] private float moveSpeed = 5f; // Velocidad a la que se moverá el jugador. [SerializeField] la hace visible en el Inspector.

        // --- VARIABLES INTERNAS DE MOVIMIENTO ---
        private Vector2 movementDirection; // Vector que almacena la dirección actual de movimiento (ej: (0,1) para arriba). Se pone a (0,0) si no hay input.
        private Vector2 currentInput;      // Vector que almacena el input crudo del jugador (ej: desde un joystick o WASD), normalizado.

        [Header("Animations")] // Encabezado para las variables de animación en el Inspector
        [SerializeField] private Animator anim; // Referencia al componente Animator del jugador (o de su sprite hijo).
        private string lastDirectionString = "Down"; // Almacena la última dirección como un string ("Up", "Down", "Left", "Right") para construir nombres de animación.

        // Propiedad pública para que otros scripts (como PlayerInteraction) sepan hacia dónde está "mirando" el jugador.
        // { get; private set; } significa que se puede leer desde fuera, pero solo se puede modificar dentro de esta clase.
        // Se inicializa a Vector2.down por defecto.
        public Vector2 LastFacingVector { get; private set; } = Vector2.down;

        private Rigidbody2D rb; // Referencia al componente Rigidbody2D del jugador, usado para aplicar el movimiento físico.

        // Bandera para controlar si el script debe procesar el input del jugador.
        // Útil para pausar el movimiento del jugador durante diálogos, cinemáticas, etc.
        private bool _canProcessInput = true;

  
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>(); // Obtiene el componente Rigidbody2D adjunto a este GameObject.

            // Si el Animator no fue asignado en el Inspector, intenta encontrarlo.
            if (anim == null)
            {
                // Busca un GameObject hijo llamado "charactersprite" (asegúrate de que el nombre coincida).
                Transform characterSprite = transform.Find("charactersprite");
                if (characterSprite != null)
                {
                    // Si lo encuentra, obtiene el Animator de ese hijo.
                    anim = characterSprite.GetComponent<Animator>();
                }
                else
                {
                    // Si no hay hijo "charactersprite", intenta obtener el Animator del mismo GameObject.
                    anim = GetComponent<Animator>();
                }
            }
            // Si después de intentar encontrarlo, sigue siendo null, muestra un error.
            if (anim == null)
            {
                Debug.LogError("PlayerMovement: Animator no encontrado en " + gameObject.name + " o su hijo 'charactersprite'. Las animaciones no funcionarán.", this);
            }

            // Establece la dirección de "mirada" inicial (LastFacingVector) basada en el valor inicial de lastDirectionString.
            UpdateLastFacingVectorFromString(lastDirectionString);
        }

     
        private void Update()
        {
            // Llama al método que maneja las animaciones en cada frame.
            HandleAnimations();
        }

       
        private void FixedUpdate()
        {
            if (_canProcessInput) // Solo aplica movimiento si el input está habilitado.
            {
                // Establece la velocidad del Rigidbody2D.
                // movementDirection es el vector de dirección calculado (normalizado), y moveSpeed es la magnitud.
                rb.velocity = movementDirection * moveSpeed;
            }
            else // Si el input está deshabilitado
            {
                rb.velocity = Vector2.zero; // Detiene al jugador completamente.
            }
        }



        // Método para manejar qué animación se debe reproducir.
        private void HandleAnimations()
        {
            if (anim == null) return; // Si no hay Animator, no hacer nada.

            string animationName = ""; // String para construir el nombre de la animación.

            // Determina si la animación debe ser "Idle" o "Walking".
            // Se basa en si movementDirection es (0,0), lo cual ocurre si no hay input o si _canProcessInput es false.
            if (movementDirection == Vector2.zero)
            {
                animationName = "Idle";
            }
            else
            {
                animationName = "Walking";
            }
            // Concatena el estado ("Idle" o "Walking") con la última dirección ("Up", "Down", etc.)
            // para obtener el nombre completo del clip de animación (ej: "IdleDown", "WalkingRight").
            anim.Play(animationName + lastDirectionString);
        }

        // Procesa el vector de input crudo y lo convierte en una dirección de movimiento cardinal (arriba, abajo, izquierda, derecha).
        // También actualiza lastDirectionString y LastFacingVector.
        private Vector2 ProcessInputToDirection(Vector2 input)
        {
            Vector2 calculatedDirection = Vector2.zero; // Dirección calculada, empieza en cero.

            // Solo procesar si hay un input significativo (mayor que un pequeño umbral para evitar "drift" del joystick).
            if (Mathf.Abs(input.x) > 0.01f || Mathf.Abs(input.y) > 0.01f)
            {
                // Prioriza el movimiento horizontal si el input en X es mayor que en Y.
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    if (input.x > 0.01f) // Movimiento a la derecha
                    {
                        lastDirectionString = "Right";
                        calculatedDirection = Vector2.right; // Vector (1, 0)
                        LastFacingVector = Vector2.right;    // Actualiza la dirección de "mirada"
                    }
                    else if (input.x < -0.01f) // Movimiento a la izquierda
                    {
                        lastDirectionString = "Left";
                        calculatedDirection = Vector2.left;  // Vector (-1, 0)
                        LastFacingVector = Vector2.left;
                    }
                }
                else // Prioriza el movimiento vertical (o si los inputs X e Y son iguales).
                {
                    if (input.y > 0.01f) // Movimiento hacia arriba
                    {
                        lastDirectionString = "Up";
                        calculatedDirection = Vector2.up;    // Vector (0, 1)
                        LastFacingVector = Vector2.up;
                    }
                    else if (input.y < -0.01f) // Movimiento hacia abajo
                    {
                        lastDirectionString = "Down";
                        calculatedDirection = Vector2.down;  // Vector (0, -1)
                        LastFacingVector = Vector2.down;
                    }
                }
            }
            // Si no hay input significativo, calculatedDirection permanece Vector2.zero.
            // lastDirectionString y LastFacingVector NO se actualizan si no hay input,
            // así que retienen la última dirección en la que el jugador miraba.
            return calculatedDirection; // Devuelve la dirección calculada.
        }

        // Método auxiliar para inicializar LastFacingVector en Awake a partir de la lastDirectionString inicial.
        private void UpdateLastFacingVectorFromString(string directionString)
        {
            switch (directionString) // Compara el string de dirección
            {
                case "Up": LastFacingVector = Vector2.up; break;
                case "Down": LastFacingVector = Vector2.down; break;
                case "Left": LastFacingVector = Vector2.left; break;
                case "Right": LastFacingVector = Vector2.right; break;
                default: LastFacingVector = Vector2.down; break; // Dirección por defecto si el string no coincide.
            }
        }

        // --- MÉTODO DE EVENTO DEL INPUT SYSTEM ---

        // Este método es llamado automáticamente por el componente "Player Input" de Unity
        // cuando se detecta una acción de input mapeada a "Move" (o como la hayas llamado en tus Input Actions).
        // El parámetro 'value' contiene el valor del input (ej: un Vector2 de un joystick o WASD).
        private void OnMove(InputValue value)
        {
            if (!_canProcessInput) // Si el input está deshabilitado por otro script (ej: durante diálogo)
            {
                currentInput = Vector2.zero;      // Poner el input actual a cero.
                movementDirection = Vector2.zero; // Poner la dirección de movimiento a cero.
                // La velocidad se pondrá a cero en FixedUpdate.
                return; // No procesar más este input.
            }

            // Obtiene el Vector2 del input y lo normaliza (para que el movimiento diagonal no sea más rápido).
            currentInput = value.Get<Vector2>().normalized;
            // Procesa el input normalizado para obtener la dirección de movimiento cardinal y actualizar la dirección de "mirada".
            movementDirection = ProcessInputToDirection(currentInput);
        }

        // --- MÉTODO PÚBLICO PARA CONTROL EXTERNO ---

        // Método público que puede ser llamado por otros scripts (como PlayerInteraction)
        // para habilitar o deshabilitar el procesamiento del input del jugador.
        public void SetCanProcessInput(bool canProcess)
        {
            _canProcessInput = canProcess; // Actualiza la bandera.
            if (!canProcess) // Si se está deshabilitando el input
            {
                // Detener inmediatamente el movimiento.
                movementDirection = Vector2.zero;
                currentInput = Vector2.zero;
                // rb.velocity = Vector2.zero; // Opcional: forzar la velocidad a cero aquí también, aunque FixedUpdate lo hará.
            }
            // Al re-habilitar (_canProcessInput = true), no es necesario hacer nada especial aquí.
            // El método OnMove tomará el siguiente input del jugador cuando ocurra.
        }
    }
}