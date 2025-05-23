using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    private Character _representedCharacter;
    public Character RepresentedCharacter => _representedCharacter; // Para que otros puedan saber a quién representa

    void Awake()
    {
        if (characterPortraitImage == null)
        {
            Transform portraitTransform = transform.Find("CharacterPortrait_Image");
            if (portraitTransform != null) characterPortraitImage = portraitTransform.GetComponent<Image>();
            if (characterPortraitImage == null)
                Debug.LogError("PartyMemberSelectIconUI: 'characterPortraitImage' no asignado/encontrado en " + gameObject.name, this);
        }

        if (selectionButton == null)
        {
            selectionButton = GetComponent<Button>();
            if (selectionButton == null)
                Debug.LogError("PartyMemberSelectIconUI: 'selectionButton' no asignado y no se encontró en " + gameObject.name, this);
        }

        if (selectionButton != null)
        {
            selectionButton.onClick.AddListener(OnIconButtonClicked);
        }
    }

    /// <summary>
    /// Configura este icono de UI con los datos de un personaje específico.
    /// </summary>
    public void SetupIcon(Character characterData)
    {
        _representedCharacter = characterData;

        if (_representedCharacter == null)
        {
            if (characterPortraitImage != null) characterPortraitImage.enabled = false;
            if (selectionButton != null) selectionButton.interactable = false;
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
                characterPortraitImage.enabled = false;
            }
        }

        if (selectionButton != null)
        {
            selectionButton.interactable = true;
        }
    }

    /// <summary>
    /// Se llama cuando se hace clic en el botón de este icono.
    /// Ahora llama a PartyManager para establecer el personaje seleccionado.
    /// </summary>
    private void OnIconButtonClicked()
    {
        if (_representedCharacter != null && PartyManager.Instance != null)
        {
            Debug.Log("PartyMemberSelectIconUI: Clic en icono de personaje: " + _representedCharacter.characterName + ". Llamando a PartyManager.SetSelectedMenuCharacter.");
            // Notificar al PartyManager que este personaje fue seleccionado.
            PartyManager.Instance.SetSelectedMenuCharacter(_representedCharacter);
        }
        else
        {
            if (_representedCharacter == null) Debug.LogWarning("PartyMemberSelectIconUI: _representedCharacter es null al hacer clic.");
            if (PartyManager.Instance == null) Debug.LogWarning("PartyMemberSelectIconUI: PartyManager.Instance es null al hacer clic.");
        }
    }
}
