using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text; // Necesario para StringBuilder

public class NPCDialogue : MonoBehaviour
{
    [Header("Contenido del Diálogo")] // Encabezado para organizar variables en el Inspector
    [Tooltip("Las líneas de diálogo que dirá este NPC. Cada elemento del array es una línea completa que puede paginarse.")]
    [TextArea(3, 10)] // Atributo para hacer el campo de texto más grande en el Inspector, facilitando la edición de multilíneas.
    public string[] dialogueLines; // Array público para almacenar todas las líneas de diálogo del NPC.

    [Tooltip("Opcional: Nombre del NPC que se mostraría en la UI de diálogo.")]
    public string npcName; // Nombre del NPC, puede estar vacío.

    [Header("UI del Diálogo")] // Encabezado para las referencias a la interfaz de usuario
    [Tooltip("Arrastra aquí el GameObject del panel principal de la caja de diálogo (el que tiene la Image de fondo).")]
    [SerializeField] private GameObject dialogueBoxPanel; // Referencia al GameObject que contiene toda la UI del diálogo.
    [Tooltip("Arrastra aquí el componente TextMeshProUGUI donde se mostrará el texto del diálogo.")]
    [SerializeField] private TextMeshProUGUI dialogueTextTMP; // Referencia al componente de texto donde se escribe el diálogo.
    [Tooltip("Opcional: Arrastra aquí el TextMeshProUGUI para el nombre del NPC.")]
    [SerializeField] private TextMeshProUGUI npcNameTextTMP;   // Referencia al texto para el nombre del NPC (opcional).
    [Tooltip("Opcional: GameObject para el indicador de 'continuar' (ej. una flecha 'v').")]
    [SerializeField] private GameObject continueIndicator; // Referencia al GameObject del indicador visual para continuar.

    [Header("Efecto Máquina de Escribir y Paginación")] // Encabezado para configuración del efecto de texto
    [Tooltip("Velocidad a la que aparecen los caracteres (caracteres por segundo). Si es 0, el texto aparece instantáneamente.")]
    [SerializeField] private float typewriterSpeed = 20f; // Velocidad del efecto de máquina de escribir.

    // --- SECCIÓN DE SONIDOS DEL DIÁLOGO ---
    [Header("Sonidos del Diálogo")] // Encabezado para la configuración de sonidos.
    [Tooltip("AudioSource para reproducir todos los sonidos del diálogo (tipeo, avanzar página).")]
    [SerializeField] private AudioSource dialogueAudioSource; // ÚNICA referencia al AudioSource.

    [Tooltip("Opcional: AudioClip para el sonido de cada caracter al tipear.")]
    [SerializeField] private AudioClip typingSoundClip; // El archivo de sonido para el efecto de máquina de escribir.
    [Tooltip("AudioClip para el sonido al mostrar una nueva página/línea o al avanzar en el diálogo.")]
    [SerializeField] private AudioClip nextPageSoundClip; // El archivo de sonido para cuando aparece una nueva "página" de texto.

    // --- REFERENCIAS INTERNAS Y VARIABLES DE ESTADO ---
    private Animator _animator;
    private NPCMovement _npcMovement;
    private DynamicIdleBehavior _dynamicIdleBehavior;

    private bool _isDialogueActive = false;
    private int _currentDialogueLineIndex = 0;
    private int _currentCharacterIndexInLine = 0;
    private string _fullCurrentLineText;

    private Transform _playerTransform;
    private Coroutine _typewriterCoroutine;
    private bool _isSegmentCurrentlyTyping = false;
    private bool _isCurrentSegmentFullyDisplayed = false;
    private float _dialogueTextRectHeight = 0f;

    void Awake()
    {
        // Obtener Animator del hijo "CharacterSprite" o del mismo GameObject
        Transform characterSprite = transform.Find("CharacterSprite");
        if (characterSprite != null)
            _animator = characterSprite.GetComponent<Animator>();
        else
            _animator = GetComponent<Animator>();

        // Obtener referencias a otros scripts de comportamiento del NPC
        _npcMovement = GetComponent<NPCMovement>();
        _dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();

        // Comprobación de seguridad para el Animator
        if (_animator == null)
        {
            Debug.LogWarning("NPCDialogue: No se encontró Animator en " + gameObject.name + " o su hijo 'CharacterSprite'. El NPC podría no girarse correctamente.", this);
        }

        // Comprobaciones de seguridad para las referencias de UI principales
        if (dialogueBoxPanel == null || dialogueTextTMP == null)
        {
            Debug.LogError("NPCDialogue: Referencias de UI (Panel o Texto) no asignadas para " + gameObject.name + " en el Inspector. El diálogo no funcionará.", this);
            enabled = false; // Deshabilitar este script si faltan referencias cruciales
            return;
        }

        // Comprobación para el RectTransform del componente de texto
        if (dialogueTextTMP.rectTransform == null)
        {
            Debug.LogError("NPCDialogue: El TextMeshProUGUI asignado a dialogueTextTMP no tiene un RectTransform. Esto es muy inusual.", this);
            enabled = false;
            return;
        }
        // Guardar la altura del área de texto definida en el Editor. Es crucial para la paginación.
        _dialogueTextRectHeight = dialogueTextTMP.rectTransform.rect.height;
        if (_dialogueTextRectHeight <= 0)
        { // Advertir si la altura es inválida
            Debug.LogWarning("NPCDialogue: La altura del RectTransform de dialogueTextTMP es 0 o negativa para " + gameObject.name + ". La paginación podría no funcionar. Asegúrate de que tenga una altura definida en el Editor.", this);
        }

        // Comprobación para el AudioSource: si hay AudioClips asignados pero no hay AudioSource, muestra un aviso.
        if ((typingSoundClip != null || nextPageSoundClip != null) && dialogueAudioSource == null)
        {
            Debug.LogWarning("NPCDialogue: Hay AudioClips asignados pero no hay un AudioSource en 'dialogueAudioSource' para " + gameObject.name + ". Los sonidos del diálogo no se reproducirán.", this);
        }

        // Asegurarse de que la UI del diálogo y el indicador de continuar estén ocultos al inicio del juego.
        dialogueBoxPanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    /// <summary>
    /// Inicia la secuencia de diálogo con este NPC.
    /// </summary>
    /// <param name="playerInitiating">El Transform del jugador que inicia el diálogo.</param>
    public void StartDialogue(Transform playerInitiating)
    {
        if (_isDialogueActive) return; // Evitar iniciar si ya hay un diálogo activo

        _isDialogueActive = true; // Marcar el diálogo como activo
        _playerTransform = playerInitiating; // Guardar referencia al jugador
        _currentDialogueLineIndex = 0; // Empezar desde la primera línea
        _currentCharacterIndexInLine = 0; // Empezar desde el inicio de la línea
        _isCurrentSegmentFullyDisplayed = false; // La primera página aún no se ha mostrado
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador

        // --- Controlar el comportamiento del NPC (pausar movimiento, girar) ---
        if (_npcMovement != null) // Si el NPC tiene script de movimiento
        {
            _npcMovement.SetMovementPaused(true); // Pausar su movimiento
            _npcMovement.SetIdleAndFaceTarget(_playerTransform); // Hacer que mire al jugador
        }
        else if (_dynamicIdleBehavior != null) // Si es estático pero con idle dinámico
        {
            _dynamicIdleBehavior.FocusOnTarget(_playerTransform); // Pausar idle dinámico y mirar al jugador
        }
        else if (_animator != null) // Si es completamente estático con solo Animator
        {
            // Lógica para girar al NPC estático hacia el jugador
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            string directionKey = "Down"; // Dirección por defecto
            if (directionToPlayer.sqrMagnitude > 0.01f) // Solo si hay una dirección clara
            {
                if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.y))
                    directionKey = directionToPlayer.x > 0 ? "Right" : "Left";
                else
                    directionKey = directionToPlayer.y > 0 ? "Up" : "Down";
            }
            string animationName = "Idle" + directionKey;
            if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
                _animator.Play(animationName); // Reproducir animación de Idle en la dirección del jugador
        }

        // --- Mostrar la UI del diálogo ---
        dialogueBoxPanel.SetActive(true); // Hacer visible la caja de diálogo

        if (npcNameTextTMP != null) // Si hay un campo para el nombre del NPC
        {
            npcNameTextTMP.text = string.IsNullOrEmpty(npcName) ? "" : npcName; // Poner el nombre
            npcNameTextTMP.gameObject.SetActive(!string.IsNullOrEmpty(npcName)); // Mostrar/ocultar según si hay nombre
        }

        // Procesar la primera línea de diálogo para mostrar su primer segmento/página
        ProcessCurrentLineAndDisplaySegment();
    }

    /// <summary>
    /// Avanza el diálogo. Se llama cuando el jugador presiona la tecla de "continuar".
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!_isDialogueActive) return; // No hacer nada si no hay diálogo activo

        if (_isSegmentCurrentlyTyping && _typewriterCoroutine != null) // Si la página actual se está tipeando
        {
            // Detener el efecto de máquina de escribir
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;

            // Mostrar el segmento completo que se estaba tipeando Y ACTUALIZAR EL ÍNDICE
            dialogueTextTMP.text = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out _currentCharacterIndexInLine);

            _isSegmentCurrentlyTyping = false; // El tipeo ha terminado
            _isCurrentSegmentFullyDisplayed = true; // La página está ahora completamente visible
            UpdateContinueIndicator(); // Actualizar el indicador de "continuar"

            // Detener el sonido de tipeo si es un sonido en loop y estaba activo.
            if (dialogueAudioSource != null && typingSoundClip != null && dialogueAudioSource.isPlaying)
                dialogueAudioSource.Stop();
        }
        else // Si la página actual ya se mostró completamente (o se saltó el tipeo)
        {
            // Actualizar _currentCharacterIndexInLine con el final del segmento que se acaba de mostrar.
            string previousSegment_unused = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out _currentCharacterIndexInLine);

            if (_currentCharacterIndexInLine < _fullCurrentLineText.Length) // Si hay más texto en la MISMA línea
            {
                DisplayNextSegment(); // Mostrar la siguiente página de la línea actual
            }
            else // Se terminó la línea actual, pasar a la siguiente línea del array dialogueLines
            {
                _currentDialogueLineIndex++; // Incrementar el índice de la línea de diálogo
                _currentCharacterIndexInLine = 0; // Resetear el índice de caracteres para la nueva línea
                _isCurrentSegmentFullyDisplayed = false; // La nueva página/línea aún no se ha mostrado

                if (_currentDialogueLineIndex < dialogueLines.Length) // Si hay más líneas de diálogo
                {
                    ProcessCurrentLineAndDisplaySegment(); // Procesar la nueva línea
                }
                else // Si no hay más líneas
                {
                    EndDialogue(); // Terminar el diálogo
                }
            }
        }
    }

    // Carga la línea de diálogo actual del array y prepara la visualización de su primer segmento.
    private void ProcessCurrentLineAndDisplaySegment()
    {
        if (_currentDialogueLineIndex < dialogueLines.Length) // Si el índice es válido
        {
            _fullCurrentLineText = dialogueLines[_currentDialogueLineIndex]; // Obtener el texto completo de la línea
            _currentCharacterIndexInLine = 0; // Empezar desde el inicio de esta línea
            DisplayNextSegment(); // Mostrar el primer (o siguiente) segmento
        }
        else // Si ya no hay más líneas
        {
            EndDialogue();
        }
    }

    // Se encarga de obtener el segmento de texto que cabe y de iniciar su visualización (tipeada o instantánea).
    private void DisplayNextSegment()
    {
        _isCurrentSegmentFullyDisplayed = false; // Marcar que la nueva página aún no está completa
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador mientras se prepara/tipea

        // Si la línea está vacía o ya hemos mostrado toda la línea
        if (string.IsNullOrEmpty(_fullCurrentLineText) || _currentCharacterIndexInLine >= _fullCurrentLineText.Length)
        {
            dialogueTextTMP.text = ""; // Limpiar el texto
            _isSegmentCurrentlyTyping = false; // No hay nada que tipear
            _isCurrentSegmentFullyDisplayed = true; // Considerar una página vacía como "completa"
            UpdateContinueIndicator(); // Actualizar indicador
            return;
        }

        // Obtener el segmento de texto que cabe en la página actual
        string segmentToDisplay = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out int tempEndIndexOfThisSegment_unused);

        // --- REPRODUCCIÓN DEL SONIDO DE AVANZAR PÁGINA/LÍNEA ---
        // Se reproduce aquí, justo antes de mostrar el nuevo segmento de texto.
        if (segmentToDisplay.Length > 0) // Solo reproducir si hay texto que mostrar en este segmento
        {
            PlaySound(nextPageSoundClip); // Llama al método auxiliar para reproducir el sonido
        }

        if (typewriterSpeed > 0 && segmentToDisplay.Length > 0) // Si hay velocidad de tipeo y texto que mostrar
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine); // Detener tipeo anterior
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(segmentToDisplay)); // Iniciar nuevo tipeo
        }
        else // Si el tipeo es instantáneo (velocidad 0) o el segmento está vacío
        {
            dialogueTextTMP.text = segmentToDisplay; // Mostrar texto instantáneamente
            _isSegmentCurrentlyTyping = false; // No hay tipeo
            _isCurrentSegmentFullyDisplayed = true; // La página está completa
            UpdateContinueIndicator(); // Actualizar indicador
        }
    }

    /// <summary>
    /// Calcula el segmento de texto de 'fullText' (empezando en 'startIndex') que cabe en la caja de diálogo.
    /// Intenta cortar por palabras para evitar partirlas.
    /// </summary>
    private string GetSegmentThatFits(string fullText, int startIndex, out int segmentEndIndexInFullText)
    {
        segmentEndIndexInFullText = startIndex;
        if (startIndex >= fullText.Length) return "";

        dialogueTextTMP.text = "";
        StringBuilder currentSegment = new StringBuilder();
        int lastPotentialBreakPointInSegment = -1;
        int lastPotentialBreakPointInFullText = startIndex;

        for (int i = startIndex; i < fullText.Length; i++)
        {
            currentSegment.Append(fullText[i]);
            dialogueTextTMP.text = currentSegment.ToString();
            dialogueTextTMP.ForceMeshUpdate(true);

            if (dialogueTextTMP.preferredHeight > _dialogueTextRectHeight)
            {
                if (lastPotentialBreakPointInSegment != -1)
                {
                    currentSegment.Length = lastPotentialBreakPointInSegment;
                    segmentEndIndexInFullText = lastPotentialBreakPointInFullText;
                }
                else
                {
                    currentSegment.Length--;
                    segmentEndIndexInFullText = i;
                }
                return currentSegment.ToString().TrimEnd();
            }

            if (char.IsWhiteSpace(fullText[i]))
            {
                lastPotentialBreakPointInSegment = currentSegment.Length - 1;
                lastPotentialBreakPointInFullText = i + 1;
            }
        }
        segmentEndIndexInFullText = fullText.Length;
        return currentSegment.ToString();
    }


    // Corrutina para el efecto de máquina de escribir para un segmento de texto.
    private IEnumerator TypewriterEffect(string segment)
    {
        _isSegmentCurrentlyTyping = true; // Marcar que el tipeo está en curso
        _isCurrentSegmentFullyDisplayed = false; // La página aún no está completamente visible
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador mientras se tipea

        dialogueTextTMP.text = ""; // Limpiar el texto al inicio del tipeo
        float delay = 1f / typewriterSpeed; // Calcular el retraso entre caracteres

        // Iterar por cada carácter del segmento
        for (int i = 0; i < segment.Length; i++)
        {
            dialogueTextTMP.text += segment[i]; // Añadir el carácter al texto visible

            // --- REPRODUCCIÓN DEL SONIDO DE TIPEO ---
            PlaySound(typingSoundClip); // Llama al método auxiliar para reproducir el sonido de tipeo

            yield return new WaitForSeconds(delay); // Esperar antes de mostrar el siguiente carácter
        }
        // El tipeo del segmento ha terminado
        _isSegmentCurrentlyTyping = false; // Marcar que el tipeo ha finalizado
        _isCurrentSegmentFullyDisplayed = true; // Marcar que la página está ahora completamente visible
        _typewriterCoroutine = null; // Limpiar la referencia a la corrutina
        UpdateContinueIndicator(); // Actualizar el indicador de "continuar" (probablemente para mostrarlo)
    }

    // Actualiza la visibilidad del indicador de "continuar".
    private void UpdateContinueIndicator()
    {
        if (continueIndicator == null) return;

        GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out int currentSegmentEndIndexForIndicator);

        bool hasMoreTextInCurrentLine = currentSegmentEndIndexForIndicator < _fullCurrentLineText.Length;
        bool hasMoreLinesInDialogue = _currentDialogueLineIndex < dialogueLines.Length - 1;

        if (!_isSegmentCurrentlyTyping && _isCurrentSegmentFullyDisplayed && (hasMoreTextInCurrentLine || hasMoreLinesInDialogue))
        {
            continueIndicator.SetActive(true);
        }
        else
        {
            continueIndicator.SetActive(false);
        }
    }

    // Termina la secuencia de diálogo.
    public void EndDialogue()
    {
        if (!_isDialogueActive) return;

        _isDialogueActive = false;
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        _isSegmentCurrentlyTyping = false;
        _isCurrentSegmentFullyDisplayed = false;

        dialogueTextTMP.text = "";
        dialogueBoxPanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Reanudar comportamiento del NPC.
        if (_npcMovement != null) _npcMovement.SetMovementPaused(false);
        else if (_dynamicIdleBehavior != null) _dynamicIdleBehavior.ResumeDynamicIdle();
    }

    // --- MÉTODO PlaySound DEFINIDO AQUÍ ---
    /// <summary>
    /// Reproduce un AudioClip usando el AudioSource asignado (dialogueAudioSource).
    /// </summary>
    /// <param name="clip">El AudioClip a reproducir.</param>
    private void PlaySound(AudioClip clip)
    {
        // Solo reproducir si hay un AudioSource y un AudioClip válidos.
        // Utiliza 'dialogueAudioSource' como el AudioSource principal para los sonidos de este script.
        if (dialogueAudioSource != null && clip != null)
        {
            // PlayOneShot permite que varios sonidos se reproduzcan sin cortarse entre sí,
            // lo cual es ideal para el sonido de tipeo rápido.
            dialogueAudioSource.PlayOneShot(clip);
        }
    }

    // Devuelve si el diálogo está activo.
    public bool IsDialogueActive()
    {
        return _isDialogueActive;
    }
}
