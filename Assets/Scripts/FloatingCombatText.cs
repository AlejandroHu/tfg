using UnityEngine;
using TMPro; // Necesario para TextMeshProUGUI

public class FloatingCombatText : MonoBehaviour
{
    [Header("Configuración de Animación")]
    [SerializeField] private float moveSpeed = 1.0f;      // Velocidad a la que el texto se mueve hacia arriba
    [SerializeField] private float fadeOutTime = 1.0f;    // Tiempo que tarda en desvanecerse completamente
    [SerializeField] private float lifetime = 1.5f;       // Tiempo total de vida del texto antes de autodestruirse
    [SerializeField] private Vector3 moveDirection = Vector3.up; // Dirección del movimiento

    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private float fadeTimer;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>(); // Asume que el TextMeshProUGUI es un hijo o está en este mismo GO
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }
        if (textMesh == null)
        {
            Debug.LogError("FloatingCombatText: No se encontró el componente TextMeshProUGUI.", this);
            Destroy(gameObject); // Destruir si no hay texto que mostrar
            return;
        }
        originalColor = textMesh.color;
        fadeTimer = fadeOutTime;

        // Destruir el objeto después de su tiempo de vida
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Mover el texto
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

        // Desvanecer el texto
        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            if (fadeTimer < 0) fadeTimer = 0;

            float alpha = Mathf.Clamp01(fadeTimer / fadeOutTime);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
    }

    /// <summary>
    /// Inicializa el texto flotante.
    /// </summary>
    /// <param name="textToShow">El texto a mostrar (ej: "15", "+20 HP").</param>
    /// <param name="textColor">El color del texto.</param>
    /// <param name="fontSize">Opcional: tamaño de la fuente.</param>
    public void Init(string textToShow, Color textColor, float? fontSize = null)
    {
        if (textMesh == null) return;

        textMesh.text = textToShow;
        textMesh.color = textColor;
        originalColor = textColor; // Guardar el color original para el desvanecimiento con alfa

        if (fontSize.HasValue)
        {
            textMesh.fontSize = fontSize.Value;
        }
    }
}
