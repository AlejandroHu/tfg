using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text; // Necesario para StringBuilder

public class NPCDialogue : MonoBehaviour
{
    [Header("Contenido del Diálogo")] 
    [Tooltip("Las líneas de diálogo que dirá este NPC. Cada elemento del array es una línea completa que puede paginarse.")]
    [TextArea(3, 10)] // Atributo para hacer el campo de texto más grande en el Inspector, facilitando la edición de multilíneas.
    public string[] dialogueLines; // Array público para almacenar todas las líneas de diálogo del NPC.

    [Tooltip("Opcional: Nombre del NPC que se mostraría en la UI de diálogo.")]
    public string npcName; 

    [Header("UI del Diálogo")] 
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
    
    [Tooltip("Opcional: AudioSource para el sonido de tipeo. Arrastra un AudioSource aquí.")]
    [SerializeField] private AudioSource typingSoundAudioSource; // Para reproducir sonido al tipear.
    [Tooltip("Opcional: AudioClip para el sonido de cada caracter. Arrastra un AudioClip aquí.")]
    [SerializeField] private AudioClip typingSoundClip; // El clip de audio específico para el tipeo.

   
    private Animator _animator; // Referencia al Animator del NPC (o de su sprite hijo).
    private NPCMovement _npcMovement; // Referencia al script de movimiento del NPC (si lo tiene).
    private DynamicIdleBehavior _dynamicIdleBehavior; // Referencia al script de idle dinámico (si lo tiene).

    
    private bool _isDialogueActive = false; // Bandera para saber si un diálogo está actualmente en curso.
    private int _currentDialogueLineIndex = 0; // Índice de la línea de diálogo actual dentro del array dialogueLines.
    private int _currentCharacterIndexInLine = 0; // Índice del carácter desde donde empezar a mostrar el segmento actual de la línea _fullCurrentLineText.
    private string _fullCurrentLineText; // Almacena el texto completo de la línea de diálogo actual que se está paginando.

    private Transform _playerTransform; // Referencia al Transform del jugador que inició el diálogo (para que el NPC sepa a quién mirar).
    private Coroutine _typewriterCoroutine; // Referencia a la corrutina del efecto máquina de escribir, para poder detenerla.
    private bool _isSegmentCurrentlyTyping = false; // Bandera que indica si el segmento actual de texto se está mostrando con el efecto máquina de escribir.
    private bool _isCurrentSegmentFullyDisplayed = false; // Bandera que indica si el segmento actual ya se ha mostrado completamente (ya sea tipeado o saltado).
    private float _dialogueTextRectHeight = 0f; // Almacena la altura del RectTransform del campo de texto, usada para la paginación.

   
    void Awake()
    {
        
        // Asegúrate de que el nombre "CharacterSprite" coincida con el de tu GameObject hijo.
        Transform characterSprite = transform.Find("CharacterSprite");
        if (characterSprite != null)
            _animator = characterSprite.GetComponent<Animator>();
        else // Si no lo encuentra en el hijo, lo busca en el mismo GameObject.
            _animator = GetComponent<Animator>();

        // Obtiene otros componentes del NPC. Pueden ser null si el NPC no los tiene.
        _npcMovement = GetComponent<NPCMovement>();
        _dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();

        // Comprobación de seguridad para el Animator.
        if (_animator == null)
        {
            Debug.LogWarning("NPCDialogue: No se encontró Animator en " + gameObject.name + " o su hijo 'CharacterSprite'. El NPC podría no girarse correctamente.", this);
        }

        // Comprobaciones de seguridad para las referencias de UI. Si faltan, el script se deshabilita.
        if (dialogueBoxPanel == null || dialogueTextTMP == null)
        {
            Debug.LogError("NPCDialogue: Referencias de UI (Panel o Texto) no asignadas para " + gameObject.name + " en el Inspector. El diálogo no funcionará.", this);
            enabled = false; // Deshabilitar este script.
            return;
        }

        // Comprobación para el RectTransform del texto.
        if (dialogueTextTMP.rectTransform == null)
        {
            Debug.LogError("NPCDialogue: El TextMeshProUGUI asignado a dialogueTextTMP no tiene un RectTransform. Esto es muy inusual.", this);
            enabled = false;
            return;
        }
        // Guarda la altura del área de texto. Esta altura es crucial para la paginación.
        // Se obtiene del RectTransform del TextMeshProUGUI que asignaste en el Inspector.
        _dialogueTextRectHeight = dialogueTextTMP.rectTransform.rect.height;
        if (_dialogueTextRectHeight <= 0)
        { // Si la altura es 0 o negativa, la paginación fallará.
            Debug.LogWarning("NPCDialogue: La altura del RectTransform de dialogueTextTMP es 0 o negativa para " + gameObject.name + ". La paginación podría no funcionar correctamente. Asegúrate de que tenga una altura definida en el Editor.", this);
        }

        // Asegurarse de que la UI del diálogo esté oculta al inicio del juego.
        dialogueBoxPanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    /// <summary>
    /// Inicia la secuencia de diálogo con este NPC.
    /// Este método es público para ser llamado por otros scripts (como PlayerInteraction).
    /// </summary>
    /// <param name="playerInitiating">El Transform del jugador que inicia el diálogo.</param>
    public void StartDialogue(Transform playerInitiating)
    {
        if (_isDialogueActive) return; // Si ya hay un diálogo activo, no iniciar otro.

        _isDialogueActive = true; // Marcar el diálogo como activo.
        _playerTransform = playerInitiating; // Guardar quién inició el diálogo.
        _currentDialogueLineIndex = 0; // Empezar desde la primera línea de diálogo.
        _currentCharacterIndexInLine = 0; // Empezar desde el inicio de esa línea.
        _isCurrentSegmentFullyDisplayed = false; // La primera página aún no se ha mostrado.
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador al inicio.

        // --- Controlar el comportamiento del NPC (pausar movimiento, girar) ---
        if (_npcMovement != null) // Si el NPC tiene script de movimiento.
        {
            _npcMovement.SetMovementPaused(true); // Pausar su movimiento.
            _npcMovement.SetIdleAndFaceTarget(_playerTransform); // Hacer que mire al jugador.
        }
        else if (_dynamicIdleBehavior != null) // Si es estático pero con idle dinámico.
        {
            _dynamicIdleBehavior.FocusOnTarget(_playerTransform); // Pausar idle dinámico y mirar al jugador.
        }
        else if (_animator != null) // Si es completamente estático con solo Animator.
        {
            // Lógica simple para girar al NPC estático hacia el jugador.
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            string directionKey = "Down"; // Dirección por defecto.
            if (directionToPlayer.sqrMagnitude > 0.01f) // Solo si hay una dirección clara.
            {
                if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.y))
                    directionKey = directionToPlayer.x > 0 ? "Right" : "Left";
                else
                    directionKey = directionToPlayer.y > 0 ? "Up" : "Down";
            }
            string animationName = "Idle" + directionKey;
            if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
                _animator.Play(animationName); // Reproducir animación de Idle en la dirección del jugador.
        }

        // --- Mostrar la UI del diálogo ---
        dialogueBoxPanel.SetActive(true); // Hacer visible la caja de diálogo.
        if (npcNameTextTMP != null) // Si hay un campo para el nombre del NPC.
        {
            npcNameTextTMP.text = string.IsNullOrEmpty(npcName) ? "" : npcName; // Poner el nombre.
            npcNameTextTMP.gameObject.SetActive(!string.IsNullOrEmpty(npcName)); // Mostrar/ocultar según si hay nombre.
        }

        // Procesar la primera línea de diálogo para mostrar su primer segmento/página.
        ProcessCurrentLineAndDisplaySegment();
    }

    /// <summary>
    /// Avanza el diálogo. Se llama cuando el jugador presiona la tecla de "continuar".
    /// Puede completar el tipeo de la página actual, mostrar la siguiente página de la misma línea,
    /// o pasar a la siguiente línea del diálogo.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!_isDialogueActive) return; // No hacer nada si no hay diálogo activo.

        if (_isSegmentCurrentlyTyping && _typewriterCoroutine != null) // Si la página actual se está tipeando.
        {
            // Detener el efecto de máquina de escribir.
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;

            // Mostrar el segmento completo que se estaba tipeando.
            // GetSegmentThatFits también actualiza el out parameter _currentCharacterIndexInLine
            // para que apunte al inicio del *siguiente* segmento, si este fuera el caso al saltar.
            dialogueTextTMP.text = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out _currentCharacterIndexInLine);

            _isSegmentCurrentlyTyping = false; // El tipeo ha terminado.
            _isCurrentSegmentFullyDisplayed = true; // La página está ahora completamente visible.
            UpdateContinueIndicator(); // Actualizar el indicador de "continuar".
            if (typingSoundAudioSource != null && typingSoundAudioSource.isPlaying) // Detener sonido de tipeo si estaba activo.
                typingSoundAudioSource.Stop();
        }
        else // Si la página actual ya se mostró completamente (o se saltó el tipeo).
        {
            // _currentCharacterIndexInLine ya debería apuntar al inicio del siguiente segmento
            // debido a la llamada a GetSegmentThatFits al saltar el tipeo, o la llamada
            // anterior a GetSegmentThatFits para actualizar el indicador.
            // Si no se saltó el tipeo, _currentCharacterIndexInLine aún apunta al inicio del segmento que se acaba de mostrar.
            // Necesitamos recalcular dónde terminó el segmento actual para avanzar _currentCharacterIndexInLine correctamente.
            string previousSegment_unused = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out _currentCharacterIndexInLine);


            if (_currentCharacterIndexInLine < _fullCurrentLineText.Length) // Si hay más texto en la MISMA línea.
            {
                DisplayNextSegment(); // Mostrar la siguiente página de la línea actual.
            }
            else // Se terminó la línea actual, pasar a la siguiente línea del array dialogueLines.
            {
                _currentDialogueLineIndex++; // Incrementar el índice de la línea de diálogo.
                _currentCharacterIndexInLine = 0; // Resetear el índice de caracteres para la nueva línea.
                _isCurrentSegmentFullyDisplayed = false; // La nueva página/línea aún no se ha mostrado.

                if (_currentDialogueLineIndex < dialogueLines.Length) // Si hay más líneas de diálogo.
                {
                    ProcessCurrentLineAndDisplaySegment(); // Procesar la nueva línea.
                }
                else // Si no hay más líneas.
                {
                    EndDialogue(); // Terminar el diálogo.
                }
            }
        }
    }

    // Carga la línea de diálogo actual del array y prepara la visualización de su primer segmento.
    private void ProcessCurrentLineAndDisplaySegment()
    {
        if (_currentDialogueLineIndex < dialogueLines.Length) // Si el índice es válido.
        {
            _fullCurrentLineText = dialogueLines[_currentDialogueLineIndex]; // Obtener el texto completo de la línea.
            _currentCharacterIndexInLine = 0; // Empezar desde el inicio de esta línea.
            DisplayNextSegment(); // Mostrar el primer (o siguiente) segmento.
        }
        else // Si ya no hay más líneas (esto es una salvaguarda, AdvanceDialogue debería llamar a EndDialogue).
        {
            EndDialogue();
        }
    }

    // Se encarga de obtener el segmento de texto que cabe y de iniciar su visualización (tipeada o instantánea).
    private void DisplayNextSegment()
    {
        _isCurrentSegmentFullyDisplayed = false; // Marcar que la nueva página aún no está completa.
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador mientras se prepara/tipea.

        // Si la línea está vacía o ya hemos mostrado toda la línea.
        if (string.IsNullOrEmpty(_fullCurrentLineText) || _currentCharacterIndexInLine >= _fullCurrentLineText.Length)
        {
            dialogueTextTMP.text = ""; // Limpiar el texto.
            _isSegmentCurrentlyTyping = false; // No hay nada que tipear.
            _isCurrentSegmentFullyDisplayed = true; // Considerar una página vacía como "completa".
            UpdateContinueIndicator(); // Actualizar indicador (probablemente lo ocultará o indicará siguiente línea).
            return;
        }

        // Obtener el segmento de texto que cabe en la página actual.
        // _currentCharacterIndexInLine ya apunta al inicio del segmento que queremos mostrar.
        // El out parameter de GetSegmentThatFits (tercer argumento) no se usa para actualizar _currentCharacterIndexInLine aquí,
        // esa actualización para el *siguiente* segmento ocurre en AdvanceDialogue.
        string segmentToDisplay = GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out int tempEndIndexOfThisSegment_unused);

        if (typewriterSpeed > 0 && segmentToDisplay.Length > 0) // Si hay velocidad de tipeo y texto que mostrar.
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine); // Detener tipeo anterior.
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(segmentToDisplay)); // Iniciar nuevo tipeo.
        }
        else // Si el tipeo es instantáneo (velocidad 0) o el segmento está vacío.
        {
            dialogueTextTMP.text = segmentToDisplay; // Mostrar texto instantáneamente.
            _isSegmentCurrentlyTyping = false; // No hay tipeo.
            _isCurrentSegmentFullyDisplayed = true; // La página está completa.
            UpdateContinueIndicator(); // Actualizar indicador.
        }
    }

    /// <summary>
    /// Calcula el segmento de texto de 'fullText' (empezando en 'startIndex') que cabe en la caja de diálogo.
    /// Intenta cortar por palabras para evitar partirlas.
    /// </summary>
    /// <param name="fullText">El texto completo de la línea de diálogo actual.</param>
    /// <param name="startIndex">El índice en 'fullText' desde donde empezar a calcular el segmento.</param>
    /// <param name="segmentEndIndexInFullText">Salida: el índice en 'fullText' donde termina este segmento (y donde empezaría el siguiente).</param>
    /// <returns>La cadena de texto que cabe en la página actual.</returns>
    private string GetSegmentThatFits(string fullText, int startIndex, out int segmentEndIndexInFullText)
    {
        segmentEndIndexInFullText = startIndex; // Inicializar el índice de fin.
        if (startIndex >= fullText.Length) return ""; // Si ya estamos al final del texto, no hay segmento.

        dialogueTextTMP.text = ""; // Limpiar el campo de texto para una nueva medición.
        StringBuilder currentSegmentBeingBuilt = new StringBuilder(); // Para construir el segmento actual.

        // Variables para recordar el último punto de corte bueno (un espacio) que aún cabía.
        int lastGoodBreakPointInSegmentBuilder = -1; // Índice dentro de currentSegmentBeingBuilt.
        int lastGoodBreakPointInFullText = startIndex; // Índice correspondiente en fullText.

        // Iterar por el texto desde startIndex.
        for (int i = startIndex; i < fullText.Length; i++)
        {
            currentSegmentBeingBuilt.Append(fullText[i]); // Añadir el carácter actual al segmento en construcción.
            dialogueTextTMP.text = currentSegmentBeingBuilt.ToString(); // Poner el segmento actual en el TextMeshPro.
            dialogueTextTMP.ForceMeshUpdate(true); // Forzar a TextMeshPro a recalcular sus dimensiones.

            // Comprobar si la altura preferida del texto actual excede la altura permitida de la caja.
            if (dialogueTextTMP.preferredHeight > _dialogueTextRectHeight)
            {
                // --- Desbordamiento detectado ---
                if (lastGoodBreakPointInSegmentBuilder != -1) // Si encontramos un espacio antes del desbordamiento.
                {
                    // Cortar en el último espacio bueno encontrado.
                    // 'lastGoodBreakPointInSegmentBuilder' es el índice del espacio dentro de 'currentSegmentBeingBuilt'.
                    currentSegmentBeingBuilt.Length = lastGoodBreakPointInSegmentBuilder;
                    segmentEndIndexInFullText = lastGoodBreakPointInFullText; // El siguiente segmento empezará después de este espacio.
                }
                else // No hubo espacios antes (o la primera palabra ya es demasiado larga y desborda).
                {
                    // Cortar justo antes del carácter que causó el desbordamiento.
                    currentSegmentBeingBuilt.Length--; // Quitar el último carácter que no cupo.
                    segmentEndIndexInFullText = i;     // El carácter 'i' es el primero que no cupo.
                }
                return currentSegmentBeingBuilt.ToString().TrimEnd(); // Devolver el segmento que cabe (TrimEnd por si cortamos en espacio).
            }

            // Si no hubo desbordamiento, y el carácter actual es un espacio,
            // actualizamos el 'lastGoodBreakPoint' porque es un buen lugar para cortar si es necesario.
            if (char.IsWhiteSpace(fullText[i]))
            {
                lastGoodBreakPointInSegmentBuilder = currentSegmentBeingBuilt.Length - 1; // El espacio está al final de currentSegmentBeingBuilt.
                lastGoodBreakPointInFullText = i + 1; // El siguiente segmento empezaría DESPUÉS de este espacio.
            }
        }

        // Si el bucle termina, significa que todo el texto restante desde startIndex cupo en la caja.
        segmentEndIndexInFullText = fullText.Length; // El final del segmento es el final del texto.
        return currentSegmentBeingBuilt.ToString(); // Devolver el segmento completo.
    }


    // Corrutina para el efecto de máquina de escribir para un segmento de texto.
    private IEnumerator TypewriterEffect(string segment)
    {
        _isSegmentCurrentlyTyping = true; // Marcar que el tipeo está en curso.
        _isCurrentSegmentFullyDisplayed = false; // La página aún no está completamente visible.
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar indicador mientras se tipea.

        dialogueTextTMP.text = ""; // Limpiar el texto al inicio del tipeo.
        float delay = 1f / typewriterSpeed; // Calcular el retraso entre caracteres.

        // Iterar por cada carácter del segmento.
        for (int i = 0; i < segment.Length; i++)
        {
            dialogueTextTMP.text += segment[i]; // Añadir el carácter al texto visible.

            // Reproducir sonido de tipeo si está configurado.
            if (typingSoundAudioSource != null && typingSoundClip != null)
                typingSoundAudioSource.PlayOneShot(typingSoundClip);

            yield return new WaitForSeconds(delay); // Esperar antes de mostrar el siguiente carácter.
        }
        // El tipeo del segmento ha terminado.
        _isSegmentCurrentlyTyping = false; // Marcar que el tipeo ha finalizado.
        _isCurrentSegmentFullyDisplayed = true; // Marcar que la página está ahora completamente visible.
        _typewriterCoroutine = null; // Limpiar la referencia a la corrutina.
        UpdateContinueIndicator(); // Actualizar el indicador de "continuar" (probablemente para mostrarlo).
    }

    // Actualiza la visibilidad del indicador de "continuar".
    private void UpdateContinueIndicator()
    {
        if (continueIndicator == null) return; // Si no hay indicador asignado, no hacer nada.

        // Para saber si hay más texto, llamamos a GetSegmentThatFits desde la posición actual (_currentCharacterIndexInLine).
        // El 'out currentSegmentEndIndexForIndicator' nos dirá dónde terminaría el segmento actual que se está mostrando
        // o que se acaba de mostrar.
        GetSegmentThatFits(_fullCurrentLineText, _currentCharacterIndexInLine, out int currentSegmentEndIndexForIndicator);

        // Hay más texto en la línea actual si el final del segmento actual es antes del final de la línea completa.
        bool hasMoreTextInCurrentLine = currentSegmentEndIndexForIndicator < _fullCurrentLineText.Length;
        // Hay más líneas en el diálogo si el índice de la línea actual no es el de la última línea.
        bool hasMoreLinesInDialogue = _currentDialogueLineIndex < dialogueLines.Length - 1;

        // Mostrar el indicador si:
        // 1. El tipeo de la página actual NO está en curso.
        // 2. Y la página actual se ha mostrado completamente.
        // 3. Y (hay más texto en la línea actual O hay más líneas en todo el diálogo).
        if (!_isSegmentCurrentlyTyping && _isCurrentSegmentFullyDisplayed && (hasMoreTextInCurrentLine || hasMoreLinesInDialogue))
        {
            continueIndicator.SetActive(true);
        }
        else
        {
            continueIndicator.SetActive(false);
        }
    }

    // Termina la secuencia de diálogo actual.
    public void EndDialogue()
    {
        if (!_isDialogueActive) return; // No hacer nada si el diálogo no estaba activo.

        _isDialogueActive = false; // Marcar el diálogo como inactivo.
        if (_typewriterCoroutine != null) // Si había un tipeo en curso, detenerlo.
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
        _isSegmentCurrentlyTyping = false; // Asegurar que las banderas de tipeo estén reseteadas.
        _isCurrentSegmentFullyDisplayed = false;

        dialogueTextTMP.text = ""; // Limpiar el texto de la caja de diálogo.
        dialogueBoxPanel.SetActive(false); // Ocultar la caja de diálogo.
        if (continueIndicator != null) continueIndicator.SetActive(false); // Ocultar el indicador.

        // Reanudar el comportamiento normal del NPC.
        if (_npcMovement != null) _npcMovement.SetMovementPaused(false); // Reanudar movimiento si lo tiene.
        else if (_dynamicIdleBehavior != null) _dynamicIdleBehavior.ResumeDynamicIdle(); // Reanudar idle dinámico si lo tiene.
    }

    // Método público para comprobar desde otros scripts si el diálogo está activo.
    public bool IsDialogueActive()
    {
        return _isDialogueActive;
    }
}
