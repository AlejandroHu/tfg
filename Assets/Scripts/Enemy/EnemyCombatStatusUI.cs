using UnityEngine;
using UnityEngine.UI; // Necesario para Slider o Image
using TMPro;          // Opcional, si quieres mostrar texto de HP

// Asegúrate de que el namespace de Combatant sea accesible si lo defines en otro sitio
// using TuJuego.Combate;

/// <summary>
/// Gestiona la visualización de la barra de HP (y opcionalmente otros datos)
/// para un enemigo en combate. Este script irá en el prefab de la barra de HP del enemigo.
/// </summary>
public class EnemyCombatStatusUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Slider para representar la barra de HP del enemigo.")]
    [SerializeField] private Slider hpSlider;

    [Tooltip("Opcional: TextMeshProUGUI para mostrar el HP numérico (ej: '15/50 HP').")]
    [SerializeField] private TextMeshProUGUI hpText;

    // [Tooltip("Opcional: TextMeshProUGUI para mostrar el nombre del enemigo encima de la barra.")]
    // [SerializeField] private TextMeshProUGUI enemyNameText;

    private Combatant _linkedCombatant;

    void Awake()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponentInChildren<Slider>(); // Intenta encontrarlo si no está asignado
            if (hpSlider == null) Debug.LogError("EnemyCombatStatusUI: 'hpSlider' no asignado/encontrado en " + gameObject.name, this);
        }
        // Validaciones opcionales para hpText y enemyNameText
    }

    /// <summary>
    /// Configura esta UI de estado con los datos de un combatiente enemigo.
    /// </summary>
    public void SetupEnemyStatus(Combatant enemyCombatant)
    {
        if (enemyCombatant == null || enemyCombatant.isPlayerCharacter)
        {
            Debug.LogWarning("EnemyCombatStatusUI: Se intentó configurar con un combatiente no válido o que no es enemigo.", this);
            gameObject.SetActive(false);
            return;
        }

        _linkedCombatant = enemyCombatant;
        gameObject.SetActive(true);

        // if (enemyNameText != null && _linkedCombatant.enemyData != null)
        // {
        //     enemyNameText.text = _linkedCombatant.enemyData.enemyName;
        // }

        UpdateHPDisplay();
    }

    /// <summary>
    /// Actualiza la visualización del HP (barra y texto).
    /// </summary>
    public void UpdateHPDisplay()
    {
        if (_linkedCombatant == null || _linkedCombatant.isDefeated)
        {
            // Si el combatiente es nulo o derrotado, podrías ocultar la barra o mostrarla vacía.
            if (hpSlider != null) hpSlider.value = 0;
            if (hpText != null) hpText.text = "HP: 0/" + (_linkedCombatant != null ? _linkedCombatant.GetMaxHP().ToString() : "--");
            // gameObject.SetActive(false); // Ocultar si está derrotado
            return;
        }

        if (hpSlider != null)
        {
            if (_linkedCombatant.GetMaxHP() > 0)
            {
                hpSlider.value = (float)_linkedCombatant.GetCurrentHP() / _linkedCombatant.GetMaxHP();
            }
            else
            {
                hpSlider.value = 0;
            }
        }

        if (hpText != null)
        {
            hpText.text = "HP: " + _linkedCombatant.GetCurrentHP() + "/" + _linkedCombatant.GetMaxHP();
        }
    }

    // Podrías necesitar un método para que el CombatManager llame a este UpdateHPDisplay
    // externamente si el HP del combatiente cambia por otras razones que no sean TakeDamage en Combatant.
    // Pero si TakeDamage en Combatant ya tiene una forma de notificar (o si el CombatManager
    // llama a esto después de aplicar daño), podría no ser necesario.
}
