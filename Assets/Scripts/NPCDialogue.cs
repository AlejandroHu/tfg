using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text; // Necesario para StringBuilder

public class NPCDialogue : MonoBehaviour
{
    [Header("Contenido del Diálogo")]
    [Tooltip("Las líneas de diálogo que dirá este NPC.")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    [Tooltip("Opcional: Nombre del NPC que se mostraría en la UI de diálogo.")]
    public string npcName;

    [Header("UI del Diálogo")]
    [Tooltip("Arrastra aquí el GameObject del panel principal de la caja de diálogo.")]
    [SerializeField] private GameObject dialogueBoxPanel;
    [Tooltip("Arrastra aquí el componente TextMeshProUGUI donde se mostrará el texto del diálogo.")]
    [SerializeField] private TextMeshProUGUI dialogueTextTMP;
    [Tooltip("Opcional: Arrastra aquí el TextMeshProUGUI para el nombre del NPC.")]
    [SerializeField] private TextMeshProUGUI npcNameTextTMP;
    [Tooltip("Opcional: GameObject para el indicador de 'continuar' (ej. una flecha).")]
    [SerializeField] private GameObject continueIndicator;


    [Header("Efecto Máquina de Escribir y Paginación")]
    [Tooltip("Velocidad a la que aparecen los caracteres (caracteres por segundo). 0 para instantáneo.")]
    [SerializeField] private float typewriterSpeed = 20f;
    [Tooltip("Número máximo aproximado de caracteres a mostrar por página. Ajusta según tu caja y fuente.")]
    [SerializeField] private int maxCharsPerPage = 100; // ¡DEBES AJUSTAR ESTO!
    [Tooltip("Opcional: AudioSource para el sonido de tipeo.")]
    [SerializeField] private AudioSource typingSoundAudioSource;
    [Tooltip("Opcional: AudioClip para el sonido de cada caracter.")]
    [SerializeField] private AudioClip typingSoundClip;

    // Referencias a otros componentes del NPC
    private Animator _animator;
    private NPCMovement _npcMovement;
    private DynamicIdleBehavior _dynamicIdleBehavior;

    private bool _isDialogueActive = false;
    private int _currentDialogueLineIndex = 0; // Índice de la línea actual en dialogueLines
    private int _currentSegmentStartIndex = 0; // Índice de inicio del segmento actual DENTRO de la línea actual
    private string _fullCurrentLineText; // El texto completo de la línea actual que se está paginando

    private Transform _playerTransform;
    private Coroutine _typewriterCoroutine;
    private bool _isLineCurrentlyTyping = false; // Si la página actual se está escribiendo
    private bool _isCurrentPageFinishedDisplaying = false; // Si la página actual ya se mostró completa (tipeada o saltada)


    void Awake()
    {
        Transform characterSprite = transform.Find("CharacterSprite");
        if (characterSprite != null)
        {
            _animator = characterSprite.GetComponent<Animator>();
        }
        else
        {
            _animator = GetComponent<Animator>();
        }

        _npcMovement = GetComponent<NPCMovement>();
        _dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();

        if (_animator == null)
        {
            Debug.LogWarning("NPCDialogue: No se encontró Animator en " + gameObject.name + " o su hijo 'CharacterSprite'.", this);
        }

        if (dialogueBoxPanel == null || dialogueTextTMP == null)
        {
            Debug.LogError("NPCDialogue: Referencias de UI no asignadas en el Inspector para " + gameObject.name, this);
            enabled = false;
            return;
        }
        dialogueBoxPanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    public void StartDialogue(Transform playerInitiating)
    {
        if (_isDialogueActive) return;

        _isDialogueActive = true;
        _playerTransform = playerInitiating;
        _currentDialogueLineIndex = 0;
        _currentSegmentStartIndex = 0; // Resetear para la primera línea
        _isCurrentPageFinishedDisplaying = false;


        // Controlar comportamiento del NPC
        if (_npcMovement != null)
        {
            _npcMovement.SetMovementPaused(true);
            _npcMovement.SetIdleAndFaceTarget(_playerTransform);
        }
        else if (_dynamicIdleBehavior != null)
        {
            _dynamicIdleBehavior.FocusOnTarget(_playerTransform);
        }
        else if (_animator != null)
        {
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            string directionKey = "Down";
            if (directionToPlayer.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.y))
                    directionKey = directionToPlayer.x > 0 ? "Right" : "Left";
                else
                    directionKey = directionToPlayer.y > 0 ? "Up" : "Down";
            }
            string animationName = "Idle" + directionKey;
            if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
                _animator.Play(animationName);
        }

        // Mostrar UI y primera línea/página
        dialogueBoxPanel.SetActive(true);
        if (npcNameTextTMP != null)
        {
            npcNameTextTMP.text = string.IsNullOrEmpty(npcName) ? "" : npcName;
            npcNameTextTMP.gameObject.SetActive(!string.IsNullOrEmpty(npcName));
        }

        ProcessCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!_isDialogueActive) return;

        if (_isLineCurrentlyTyping && _typewriterCoroutine != null)
        {
            // Si la página actual se está escribiendo, mostrarla completa instantáneamente
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
            // Mostrar el segmento que se estaba tipeando
            int charsToDisplay = Mathf.Min(maxCharsPerPage, _fullCurrentLineText.Length - _currentSegmentStartIndex);
            dialogueTextTMP.text = _fullCurrentLineText.Substring(_currentSegmentStartIndex, charsToDisplay);

            _isLineCurrentlyTyping = false;
            _isCurrentPageFinishedDisplaying = true;
            UpdateContinueIndicator();

            if (typingSoundAudioSource != null && typingSoundAudioSource.isPlaying)
            {
                typingSoundAudioSource.Stop();
            }
        }
        else // La página actual ya se mostró completa (o se saltó el tipeo)
        {
            _currentSegmentStartIndex += maxCharsPerPage; // Avanzar al inicio del siguiente segmento

            if (_currentSegmentStartIndex < _fullCurrentLineText.Length) // Si hay más segmentos en la MISMA línea
            {
                DisplaySegment();
            }
            else // Se terminó la línea actual, pasar a la siguiente línea del array dialogueLines
            {
                _currentDialogueLineIndex++;
                _currentSegmentStartIndex = 0; // Resetear para la nueva línea
                _isCurrentPageFinishedDisplaying = false;

                if (_currentDialogueLineIndex < dialogueLines.Length)
                {
                    ProcessCurrentLine();
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    private void ProcessCurrentLine()
    {
        if (_currentDialogueLineIndex < dialogueLines.Length)
        {
            _fullCurrentLineText = dialogueLines[_currentDialogueLineIndex];
            _currentSegmentStartIndex = 0; // Siempre empezar desde el inicio de la nueva línea
            DisplaySegment();
        }
        else
        {
            EndDialogue();
        }
    }

    private void DisplaySegment()
    {
        if (string.IsNullOrEmpty(_fullCurrentLineText))
        {
            // Si la línea está vacía, podría ser un error o una pausa intencional.
            // Por ahora, la trataremos como si estuviera "completa" para avanzar.
            _isLineCurrentlyTyping = false;
            _isCurrentPageFinishedDisplaying = true;
            dialogueTextTMP.text = "";
            UpdateContinueIndicator();
            return;
        }

        int charsToDisplayCount = Mathf.Min(maxCharsPerPage, _fullCurrentLineText.Length - _currentSegmentStartIndex);
        string segmentToDisplay = _fullCurrentLineText.Substring(_currentSegmentStartIndex, charsToDisplayCount);

        if (typewriterSpeed > 0)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(segmentToDisplay));
        }
        else
        {
            dialogueTextTMP.text = segmentToDisplay;
            _isLineCurrentlyTyping = false;
            _isCurrentPageFinishedDisplaying = true;
            UpdateContinueIndicator();
        }
    }

    private IEnumerator TypewriterEffect(string segment)
    {
        _isLineCurrentlyTyping = true;
        _isCurrentPageFinishedDisplaying = false;
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar mientras se tipea

        dialogueTextTMP.text = "";
        float delay = 1f / typewriterSpeed;
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < segment.Length; i++)
        {
            sb.Append(segment[i]);
            dialogueTextTMP.text = sb.ToString(); // Actualizar con StringBuilder es más eficiente para muchos cambios

            if (typingSoundAudioSource != null && typingSoundClip != null)
            {
                typingSoundAudioSource.PlayOneShot(typingSoundClip);
            }
            yield return new WaitForSeconds(delay);
        }
        _isLineCurrentlyTyping = false;
        _isCurrentPageFinishedDisplaying = true;
        _typewriterCoroutine = null;
        UpdateContinueIndicator();
    }

    private void UpdateContinueIndicator()
    {
        if (continueIndicator == null) return;

        bool hasMoreSegmentsInLine = _currentSegmentStartIndex + maxCharsPerPage < _fullCurrentLineText.Length;
        bool hasMoreLinesInDialogue = _currentDialogueLineIndex < dialogueLines.Length - 1;

        // Mostrar indicador si:
        // 1. La página actual está terminada de mostrar Y
        // 2. (Hay más segmentos en la línea actual O hay más líneas en el diálogo)
        if (_isCurrentPageFinishedDisplaying && (hasMoreSegmentsInLine || hasMoreLinesInDialogue))
        {
            continueIndicator.SetActive(true);
        }
        else
        {
            continueIndicator.SetActive(false);
        }
    }


    public void EndDialogue()
    {
        if (!_isDialogueActive) return;

        _isDialogueActive = false;
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        _isLineCurrentlyTyping = false;
        _isCurrentPageFinishedDisplaying = false;

        dialogueBoxPanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);


        if (_npcMovement != null)
            _npcMovement.SetMovementPaused(false);
        else if (_dynamicIdleBehavior != null)
            _dynamicIdleBehavior.ResumeDynamicIdle();
    }

    public bool IsDialogueActive()
    {
        return _isDialogueActive;
    }
}
