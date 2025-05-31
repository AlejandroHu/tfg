using UnityEngine;
using UnityEngine.UI;       // Para Slider e Image
using TMPro;                // Para TextMeshProUGUI

// Asegúrate de que el namespace de Character.cs sea accesible
// using TuJuego.Personajes; // Si Character.cs está en este namespace
// using TopDown; // Si Character.cs está en el namespace TopDown

/// <summary>
/// Gestiona la actualización de los elementos de UI para mostrar el estado
/// de un miembro de la party durante el combate.
/// Este script debe estar en el Prefab 'PartyMember_CombatStatus_UI_Prefab'.
/// </summary>
public class PartyMemberCombatStatusUI : MonoBehaviour
{
    [Header("Referencias de UI del Personaje")]
    [Tooltip("Texto para mostrar el nombre del personaje.")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Tooltip("Texto para mostrar el HP actual y máximo del personaje (ej: 'HP: 100/120').")]
    [SerializeField] private TextMeshProUGUI hpText;
    [Tooltip("Slider para representar visualmente la barra de HP.")]
    [SerializeField] private Slider hpSlider;

    [Tooltip("Texto para mostrar el MP actual y máximo del personaje (ej: 'MP: 50/70').")]
    [SerializeField] private TextMeshProUGUI mpText;
    [Tooltip("Slider para representar visualmente la barra de MP.")]
    [SerializeField] private Slider mpSlider;

    [Tooltip("Opcional: Imagen para mostrar un pequeño retrato del personaje.")]
    [SerializeField] private Image portraitImage;

    private Character _linkedCharacter; // El personaje cuyos datos se muestran

    void Awake()
    {
        // Validaciones básicas de referencias (puedes hacerlas más robustas si quieres)
        if (characterNameText == null) Debug.LogWarning("PartyMemberCombatStatusUI: 'characterNameText' no asignado en " + gameObject.name, this);
        if (hpText == null) Debug.LogWarning("PartyMemberCombatStatusUI: 'hpText' no asignado en " + gameObject.name, this);
        if (hpSlider == null) Debug.LogWarning("PartyMemberCombatStatusUI: 'hpSlider' no asignado en " + gameObject.name, this);
        // (Valida mpText y mpSlider si los vas a usar siempre)
        // if (portraitImage == null) Debug.LogWarning("PartyMemberCombatStatusUI: 'portraitImage' no asignado en " + gameObject.name, this); // Es opcional
    }

    /// <summary>
    /// Configura y actualiza esta UI de estado con los datos de un personaje específico.
    /// </summary>
    /// <param name="characterToDisplay">El personaje cuyos datos se mostrarán.</param>
    public void SetupStatus(Character characterToDisplay)
    {
        if (characterToDisplay == null)
        {
            Debug.LogWarning("PartyMemberCombatStatusUI: Se intentó configurar con un personaje nulo.", this);
            gameObject.SetActive(false); // Ocultar este slot si no hay personaje
            return;
        }

        _linkedCharacter = characterToDisplay;
        gameObject.SetActive(true); // Asegurarse de que esté activo

        UpdateUIElements();

        // (Opcional) Suscribirse a eventos de cambio de stats del _linkedCharacter si los tuviera,
        // para actualizar la UI en tiempo real sin que el CombatManager tenga que llamar a UpdateStatus_HP_MP.
        // Ejemplo: _linkedCharacter.OnStatsChanged += UpdateUIElements;
        // (Recordar desuscribirse en OnDisable o cuando el personaje cambie)
    }

    /// <summary>
    /// Actualiza todos los elementos de la UI basados en el _linkedCharacter.
    /// </summary>
    public void UpdateUIElements()
    {
        if (_linkedCharacter == null)
        {
            Debug.LogWarning("PartyMemberCombatStatusUI (" + gameObject.name + "): UpdateUIElements llamado pero _linkedCharacter es NULL.");
            //Debug.LogWarning("PartyMemberCombatStatusUI: No hay personaje vinculado para actualizar UI en " + gameObject.name);
            // Podrías poner valores por defecto o dejarlo como está si se va a desactivar.
            if (characterNameText != null) characterNameText.text = "---";
            if (hpText != null) hpText.text = "HP: --/--";
            if (hpSlider != null) hpSlider.value = 0;
            if (mpText != null) mpText.text = "MP: --/--";
            if (mpSlider != null) mpSlider.value = 0;
            if (portraitImage != null) portraitImage.enabled = false;
            return;
        }
        Debug.Log($"PartyMemberCombatStatusUI ({gameObject.name}): UpdateUIElements para '{_linkedCharacter.characterName}'. HP Actual Leído: {_linkedCharacter.currentHP}, MaxHP: {_linkedCharacter.MaxHP}");

        if (characterNameText != null)
        {
            characterNameText.text = _linkedCharacter.characterName;
        }

        if (hpText != null)
        {
            hpText.text = "HP: " + _linkedCharacter.currentHP + "/" + _linkedCharacter.MaxHP;
        }
        if (hpSlider != null)
        {
            if (_linkedCharacter.MaxHP > 0)
                hpSlider.value = (float)_linkedCharacter.currentHP / _linkedCharacter.MaxHP;
            else
                hpSlider.value = 0;
        }

        if (mpText != null)
        {
            mpText.text = "MP: " + _linkedCharacter.currentMP + "/" + _linkedCharacter.MaxMP;
        }
        if (mpSlider != null)
        {
            if (_linkedCharacter.MaxMP > 0)
                mpSlider.value = (float)_linkedCharacter.currentMP / _linkedCharacter.MaxMP;
            else
                mpSlider.value = 0;
        }

        if (portraitImage != null)
        {
            if (_linkedCharacter.portraitSprite != null)
            {
                portraitImage.sprite = _linkedCharacter.portraitSprite;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }
        }
    }

    // (Opcional) Si te desuscribes de eventos, hazlo aquí
    // void OnDisable()
    // {
    //    if (_linkedCharacter != null)
    //    {
    //        // _linkedCharacter.OnStatsChanged -= UpdateUIElements;
    //    }
    // }
}
