using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("Contenido del Diálogo")]
    [Tooltip("Las líneas de diálogo que dirá este NPC.")]
    [TextArea(3, 10)] // Hace que el campo de texto sea más grande en el Inspector
    public string[] dialogueLines;

    [Tooltip("Opcional: Nombre del NPC que se mostraría en la UI de diálogo.")]
    public string npcName;

    // Referencias a otros componentes del NPC (pueden ser null)
    private Animator _animator;
    private NPCMovement _npcMovement;
    private DynamicIdleBehavior _dynamicIdleBehavior;

    private bool _isDialogueActive = false;
    private int _currentDialogueLine = 0;

    // Podríamos necesitar una referencia al Transform del jugador para saber a quién mirar
    private Transform _playerTransform;

    void Awake()
    {
        // Obtener referencias a los componentes del mismo GameObject
        // Es importante que el Animator esté en el hijo "charactersprite" según tu estructura
        Transform characterSprite = transform.Find("CharacterSprite");
        if (characterSprite != null)
        {
            _animator = characterSprite.GetComponent<Animator>();
        }
        else
        {
            _animator = GetComponent<Animator>(); // Fallback si no está en el hijo
        }

        _npcMovement = GetComponent<NPCMovement>();
        _dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();

        if (_animator == null)
        {
            Debug.LogWarning("NPCDialogue: No se encontró Animator en " + gameObject.name + " o su hijo 'charactersprite'. El NPC no podrá cambiar de dirección al hablar.", this);
        }
    }

    /// <summary>
    /// Inicia la secuencia de diálogo con este NPC.
    /// </summary>
    /// <param name="playerInitiating">El Transform del jugador que inicia el diálogo.</param>
    public void StartDialogue(Transform playerInitiating)
    {
        if (_isDialogueActive)
        {
            Debug.LogWarning("NPCDialogue: Se intentó iniciar un diálogo que ya estaba activo para " + gameObject.name, this);
            return; // Evitar iniciar si ya está en diálogo
        }

        _isDialogueActive = true;
        _playerTransform = playerInitiating;
        _currentDialogueLine = 0;

        Debug.Log("Diálogo iniciado con: " + (string.IsNullOrEmpty(npcName) ? gameObject.name : npcName));

        // 1. Controlar el comportamiento del NPC
        if (_npcMovement != null) // Si el NPC se mueve
        {
            _npcMovement.SetMovementPaused(true);
            _npcMovement.SetIdleAndFaceTarget(_playerTransform);
        }
        else if (_dynamicIdleBehavior != null) // Si es estático con idle dinámico
        {
            _dynamicIdleBehavior.FocusOnTarget(_playerTransform);
        }
        else if (_animator != null) // Si es completamente estático con solo Animator
        {
            // Lógica simple para girar (si no tiene DynamicIdleBehavior ni NPCMovement)
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            string directionKey = "Down"; // Default

            if (directionToPlayer.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.y))
                {
                    directionKey = directionToPlayer.x > 0 ? "Right" : "Left";
                }
                else
                {
                    directionKey = directionToPlayer.y > 0 ? "Up" : "Down";
                }
            }

            string animationName = "Idle" + directionKey;
            if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            {
                _animator.Play(animationName);
            }
        }

        // 2. Mostrar la primera línea de diálogo (o la UI de diálogo)
        ShowNextDialogueLine();
    }

    /// <summary>
    /// Avanza a la siguiente línea de diálogo o termina el diálogo si no hay más.
    /// Este método sería llamado por la UI de diálogo cuando el jugador presiona "continuar".
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!_isDialogueActive) return;

        _currentDialogueLine++;
        if (_currentDialogueLine < dialogueLines.Length)
        {
            ShowNextDialogueLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowNextDialogueLine()
    {
        if (_currentDialogueLine < dialogueLines.Length)
        {
            string lineToShow = dialogueLines[_currentDialogueLine];
            Debug.Log((string.IsNullOrEmpty(npcName) ? gameObject.name : npcName) + ": " + lineToShow);
            // AQUÍ es donde te comunicarías con tu sistema de UI para mostrar la línea
            // Ejemplo: UIManager.Instance.ShowDialogueText(npcName, lineToShow);
        }
    }

    /// <summary>
    /// Termina la secuencia de diálogo actual.
    /// </summary>
    public void EndDialogue()
    {
        if (!_isDialogueActive) return;

        _isDialogueActive = false;
        Debug.Log("Diálogo terminado con: " + (string.IsNullOrEmpty(npcName) ? gameObject.name : npcName));
        _playerTransform = null; // Limpiar referencia al jugador

        // AQUÍ es donde te comunicarías con tu sistema de UI para ocultar la caja de diálogo
        // Ejemplo: UIManager.Instance.HideDialogueBox();

        // Reanudar el comportamiento normal del NPC
        if (_npcMovement != null)
        {
            _npcMovement.SetMovementPaused(false);
        }
        else if (_dynamicIdleBehavior != null)
        {
            _dynamicIdleBehavior.ResumeDynamicIdle();
        }
        else if (_animator != null)
        {
            // Opcional: Si el NPC es completamente estático, podrías querer que vuelva
            // a su dirección original si la guardaste, o simplemente dejarlo como está.
            // Por ahora, no hacemos nada, se queda mirando donde estaba el jugador.
        }
    }

    // Opcional: para comprobar desde fuera si está en diálogo
    public bool IsDialogueActive()
    {
        return _isDialogueActive;
    }
}
