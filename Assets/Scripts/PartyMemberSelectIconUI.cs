using UnityEngine;
using UnityEngine.UI; // Necesario para Image y Button
using TMPro;          // Necesario para TextMeshProUGUI (si quieres mostrar el nombre en el icono)

// Asegúrate de que el namespace de Character sea accesible
// using TuJuego.Personajes; // Si Character.cs está en este namespace
// using TopDown; // Si Character.cs está en el namespace TopDown

public class PartyMemberSelectIconUI : MonoBehaviour
{
    [Header("Componentes de UI del Icono")]
    [Tooltip("La Image que mostrará el retrato o icono del personaje.")]
    [SerializeField] private Image characterPortraitImage;

    [Tooltip("El Button que permitirá seleccionar este personaje.")]
    [SerializeField] private Button selectionButton;

    // (Opcional) Si quieres mostrar el nombre del personaje en el icono
    // [SerializeField] private TextMeshProUGUI characterNameText; 

    private Character _representedCharacter; // El personaje que este icono representa

    void Awake()
    {
        // Obtener referencias si no están asignadas (esto es un fallback)
        if (characterPortraitImage == null)
        {
            // Intenta encontrarlo como un hijo llamado "CharacterPortrait_Image" o similar
            // Ajusta el nombre si es diferente en tu prefab.
            Transform portraitTransform = transform.Find("CharacterPortrait_Image");
            if (portraitTransform != null) characterPortraitImage = portraitTransform.GetComponent<Image>();
            if (characterPortraitImage == null)
                Debug.LogError("PartyMemberSelectIconUI: 'characterPortraitImage' no asignado/encontrado en " + gameObject.name, this);
        }

        if (selectionButton == null)
        {
            selectionButton = GetComponent<Button>(); // Asume que el botón está en el mismo GameObject
            if (selectionButton == null)
                Debug.LogError("PartyMemberSelectIconUI: 'selectionButton' no asignado y no se encontró en " + gameObject.name, this);
        }

        // Configurar el listener del botón
        if (selectionButton != null)
        {
            selectionButton.onClick.AddListener(OnIconButtonClicked);
        }
    }

    /// <summary>
    /// Configura este icono de UI con los datos de un personaje específico.
    /// </summary>
    /// <param name="characterData">El personaje que este icono representará.</param>
    public void SetupIcon(Character characterData)
    {
        _representedCharacter = characterData;

        if (_representedCharacter == null)
        {
            // Ocultar o mostrar un estado vacío si no hay personaje
            if (characterPortraitImage != null) characterPortraitImage.enabled = false;
            // if (characterNameText != null) characterNameText.text = "";
            if (selectionButton != null) selectionButton.interactable = false;
            Debug.LogWarning("PartyMemberSelectIconUI: Se intentó configurar un icono con datos de personaje nulos.");
            return;
        }

        if (characterPortraitImage != null)
        {
            if (_representedCharacter.portraitSprite != null)
            {
                characterPortraitImage.sprite = _representedCharacter.portraitSprite;
                characterPortraitImage.enabled = true;
            }
            else
            {
                characterPortraitImage.enabled = false; // O un sprite por defecto de "sin retrato"
            }
        }

        // if (characterNameText != null)
        // {
        //    characterNameText.text = _representedCharacter.characterName;
        // }

        if (selectionButton != null)
        {
            selectionButton.interactable = true;
        }
    }

    /// <summary>
    /// Se llama cuando se hace clic en el botón de este icono.
    /// </summary>
    private void OnIconButtonClicked()
    {
        if (_representedCharacter != null && EquipmentScreenManager.Instance != null)
        {
            Debug.Log("PartyMemberSelectIconUI: Clic en icono de personaje: " + _representedCharacter.characterName);
            // Notificar al EquipmentScreenManager que este personaje fue seleccionado.
            EquipmentScreenManager.Instance.SelectCharacterForDisplay(_representedCharacter);
        }
        else
        {
            if (_representedCharacter == null) Debug.LogWarning("PartyMemberSelectIconUI: _representedCharacter es null al hacer clic.");
            if (EquipmentScreenManager.Instance == null) Debug.LogWarning("PartyMemberSelectIconUI: EquipmentScreenManager.Instance es null al hacer clic.");
        }
    }
}
