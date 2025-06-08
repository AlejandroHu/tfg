using System.Collections;
using System.Collections.Generic;
// using UnityEditor; // Comentado ya que no debería estar en builds finales. Descomenta solo si usas algo específico del editor aquí.
using UnityEngine;
using UnityEngine.InputSystem;

// Asumimos que PartyManager, EnemyEncounter, Character, EnemyData están en el namespace global o también en TopDown.
// Si están en otros namespaces, necesitarás añadir los 'using' correspondientes.
// Ejemplo: using TuJuego.Party; using TuJuego.Enemigos;

namespace TopDown
{
    public class PlayerMovement : MonoBehaviour
    {
        public static PlayerMovement Instance { get; private set; }
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        private Vector2 movementDirection;
        private Vector2 currentInput;

        [Header("Animations")]
        [SerializeField] private Animator anim;
        private string lastDirectionString = "Down";
        public Vector2 LastFacingVector { get; private set; } = Vector2.down;

        private Rigidbody2D rb;
        private bool _canMove = true;


        private void Awake()
        {
            // --- INICIO DE LA LÓGICA DE PERSISTENCIA ---
            //if (Instance != null && Instance != this)
            //{
                // Si ya existe una instancia del jugador (porque hemos vuelto a la escena principal),
                // destruimos esta nueva instancia duplicada para evitar tener dos jugadores.
               // Destroy(gameObject);
               // return;
            //}
           // Instance = this;
            //transform.SetParent(null);
            //DontDestroyOnLoad(gameObject); // ¡LA LÍNEA MÁS IMPORTANTE! No destruir este objeto al cargar una nueva escena.
            // --- FIN DE LA LÓGICA DE PERSISTENCIA ---
            rb = GetComponent<Rigidbody2D>();

            if (anim == null)
            {
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
            if (anim == null)
            {
                Debug.LogError("PlayerMovement: Animator no encontrado en " + gameObject.name + " o su hijo 'charactersprite'.", this);
            }

            UpdateLastFacingVectorFromString(lastDirectionString);
        }

        private void Update()
        {
            HandleAnimations();

            // Ejemplo de cómo ganar XP (puedes moverlo o quitarlo cuando tengas un sistema de recompensas)
            if (Input.GetKeyDown(KeyCode.X))
            {
                Character playerChar = GetComponent<Character>();
                if (playerChar != null)
                {
                    playerChar.GainXP(50);
                }
            }
        }

        private void FixedUpdate()
        {
            if (_canMove)
            {
                rb.velocity = movementDirection * moveSpeed;
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
        }

        private void HandleAnimations()
        {
            if (anim == null) return;

            string animationName = "";

            if (movementDirection == Vector2.zero)
            {
                animationName = "Idle";
            }
            else
            {
                animationName = "Walking";
            }
            // Asegurarse de que el Animator tenga los clips con estos nombres exactos
            // (ej: "IdleDown", "WalkingRight", etc.)
            try
            {
                anim.Play(animationName + lastDirectionString);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayerMovement: Error al intentar reproducir animación '{animationName + lastDirectionString}'. ¿Existe el clip? Error: {e.Message}", anim);
            }
        }

        private Vector2 ProcessInputToDirection(Vector2 input)
        {
            Vector2 calculatedDirection = Vector2.zero;
            if (Mathf.Abs(input.x) > 0.01f || Mathf.Abs(input.y) > 0.01f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
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
                else
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
            return calculatedDirection;
        }

        private void UpdateLastFacingVectorFromString(string directionString)
        {
            switch (directionString)
            {
                case "Up": LastFacingVector = Vector2.up; break;
                case "Down": LastFacingVector = Vector2.down; break;
                case "Left": LastFacingVector = Vector2.left; break;
                case "Right": LastFacingVector = Vector2.right; break;
                default: LastFacingVector = Vector2.down; break;
            }
        }

        private void OnMove(InputValue value)
        {
            if (!_canMove)
            {
                currentInput = Vector2.zero;
                movementDirection = Vector2.zero;
                return;
            }
            currentInput = value.Get<Vector2>().normalized;
            movementDirection = ProcessInputToDirection(currentInput);
        }

        public void SetCanMove(bool movementAllowed)
        {
            _canMove = movementAllowed;
            Debug.Log("PlayerMovement: SetCanMove llamado con: " + _canMove);

            if (!_canMove)
            {
                movementDirection = Vector2.zero;
                currentInput = Vector2.zero;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
                // HandleAnimations se llamará en el siguiente Update y debería poner la animación de Idle
            }
        }

        // --- NUEVO: LÓGICA PARA DETECTAR ENEMIGOS E INICIAR COMBATE ---
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_canMove) return; // No iniciar combate si el jugador ya está en un estado que no permite movimiento (ej: ya en combate o diálogo)

            Debug.Log("PlayerMovement: OnTriggerEnter2D con: " + other.gameObject.name);

            EnemyEncounter encounter = other.GetComponent<EnemyEncounter>();
            if (encounter != null)
            {
                Debug.Log("PlayerMovement: Es un encuentro con enemigo: " + other.gameObject.name);

                if (encounter.isDefeated)
                {
                    Debug.Log("PlayerMovement: Este enemigo (" + other.gameObject.name + ") ya fue derrotado.");
                    return;
                }

                // Verificar si los Singletons existen antes de usarlos
                if (PartyManager.Instance == null)
                {
                    Debug.LogError("PlayerMovement: PartyManager.Instance es NULL. No se puede obtener la party para el combate.");
                    return;
                }
                if (CombatManager.Instance == null)
                {
                    Debug.LogError("PlayerMovement: CombatManager.Instance es NULL. No se puede iniciar el combate.");
                    return;
                }

                List<Character> playerParty = PartyManager.Instance.CurrentPartyMembers;
                if (playerParty == null || playerParty.Count == 0)
                {
                    Debug.LogError("PlayerMovement: La lista de CurrentPartyMembers del PartyManager está vacía o es nula.");
                    // Podrías intentar añadir el personaje actual como fallback si es necesario
                    // Character selfChar = GetComponent<Character>();
                    // if (selfChar != null) playerParty = new List<Character>() { selfChar };
                    // else return;
                    return;
                }

                Debug.Log("PlayerMovement: Llamando a CombatManager.Instance.StartCombat para el encuentro con " + other.gameObject.name);
                CombatManager.Instance.StartCombat(playerParty, encounter.enemyGroup, encounter);

                // La lógica para marcar el 'encounter' como derrotado se manejará en CombatManager.EndCombat
                // después de que el jugador gane la batalla.
            }
            // else
            // {
            //    Debug.Log("PlayerMovement: No es un enemigo con EnemyEncounter: " + other.gameObject.name);
            // }
        }
    }
}
