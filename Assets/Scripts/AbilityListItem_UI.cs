using UnityEngine;
using UnityEngine.UI;       // Necesario para Button
using TMPro;                // Necesario para TextMeshProUGUI
using UnityEngine.EventSystems; // Necesario para IPointerClickHandler (si quieres clic en todo el item y no solo un botón)
using System;               // Necesario para Action (si usaras un callback más genérico)

// Asegúrate de que el namespace de AbilityData y CharacterStatsScreenManager sea accesible
// using TuJuego.Habilidades; // Si AbilityData.cs está en este namespace
// using TuJuego.UI; // Si CharacterStatsScreenManager.cs está en este namespace

public class AbilityListItem_UI : MonoBehaviour, IPointerClickHandler
{
    [Header("Componentes de UI del Item de Habilidad")]
    [Tooltip("Texto para mostrar el nombre de la habilidad.")]
    [SerializeField] private TextMeshProUGUI abilityNameText;

    [Tooltip("Opcional: Imagen para mostrar el icono de la habilidad.")]
    [SerializeField] private Image abilityIconImage;

    [Tooltip("Opcional: Texto para mostrar el coste de MP de la habilidad.")]
    [SerializeField] private TextMeshProUGUI mpCostText;

    [Tooltip("Opcional: El componente Button de este item de lista, si se usa uno.")]
    [SerializeField] private Button itemButton;

    private AbilityData _representedAbility; // La habilidad que este UI representa

    // --- NUEVA PROPIEDAD PÚBLICA ---
    /// <summary>
    /// Devuelve los datos de la habilidad que este ítem de UI está representando.
    /// </summary>
    public AbilityData CurrentAbilityData => _representedAbility;

    void Awake()
    {
        if (abilityNameText == null)
        {
            abilityNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (abilityNameText == null)
                Debug.LogError("AbilityListItem_UI: 'abilityNameText' no asignado/encontrado en " + gameObject.name, this);
        }
        if (abilityIconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            if (images.Length > 1 && images[0].gameObject != this.gameObject) abilityIconImage = images[0]; // Intenta tomar la primera imagen hija
            else if (images.Length > 1 && images[1].gameObject != this.gameObject) abilityIconImage = images[1]; // O la segunda si la primera es el fondo
            // Es mejor asignarlo manualmente en el Inspector.
        }

        if (itemButton == null)
        {
            itemButton = GetComponent<Button>();
        }

        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// Configura este elemento de UI con los datos de una habilidad específica.
    /// </summary>
    public void SetupAbilityItem(AbilityData abilityData)
    {
        _representedAbility = abilityData;

        if (_representedAbility == null)
        {
            if (abilityNameText != null) abilityNameText.text = "---";
            if (abilityIconImage != null) abilityIconImage.enabled = false;
            if (mpCostText != null) mpCostText.text = "";
            if (itemButton != null) itemButton.interactable = false;
            return;
        }

        if (abilityNameText != null)
        {
            abilityNameText.text = _representedAbility.abilityName;
        }

        if (abilityIconImage != null)
        {
            if (_representedAbility.icon != null)
            {
                abilityIconImage.sprite = _representedAbility.icon;
                abilityIconImage.enabled = true;
            }
            else
            {
                abilityIconImage.enabled = false;
            }
        }

        if (mpCostText != null)
        {
            if (_representedAbility.mpCost > 0)
            {
                mpCostText.text = "MP: " + _representedAbility.mpCost.ToString();
                mpCostText.gameObject.SetActive(true);
            }
            else
            {
                mpCostText.gameObject.SetActive(false);
            }
        }

        if (itemButton != null)
        {
            itemButton.interactable = true;
        }
    }

    private void OnItemClicked()
    {
        HandleSelection();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (_representedAbility != null && CharacterStatsScreenManager.Instance != null)
        {
            // Debug.Log("AbilityListItem_UI: Clic en habilidad: " + _representedAbility.abilityName); // Puedes descomentar para depurar
            CharacterStatsScreenManager.Instance.OnAbilityListItemClicked(_representedAbility);
        }
        else
        {
            if (_representedAbility == null) Debug.LogWarning("AbilityListItem_UI: _representedAbility es null al hacer clic.");
            if (CharacterStatsScreenManager.Instance == null) Debug.LogWarning("AbilityListItem_UI: CharacterStatsScreenManager.Instance es null al hacer clic.");
        }
    }
}
