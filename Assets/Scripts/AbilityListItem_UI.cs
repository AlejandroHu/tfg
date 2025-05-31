using UnityEngine;
using UnityEngine.UI;       // Necesario para Button
using TMPro;                // Necesario para TextMeshProUGUI
using UnityEngine.EventSystems; // Necesario para IPointerClickHandler
using System;

// Asegúrate de que el namespace de AbilityData y los Managers sea accesible
// using TuJuego.Habilidades; 
// using TuJuego.UI; 

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

    private AbilityData _representedAbility;
    public AbilityData CurrentAbilityData => _representedAbility;

    // --- NUEVO: Callback genérico para cuando se selecciona el ítem ---
    // El script que instancia este UI (CombatManager o CharacterStatsScreenManager)
    // asignará un método a esta acción.
    public Action<AbilityData> OnItemSelectedCallback;


    void Awake()
    {
        if (abilityNameText == null)
        {
            abilityNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (abilityNameText == null)
                Debug.LogError("AbilityListItem_UI: 'abilityNameText' no asignado/encontrado en " + gameObject.name, this);
        }
        // (Fallback para abilityIconImage y mpCostText si es necesario)

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
    /// Configura este elemento de UI con los datos de una habilidad específica
    /// y un callback para cuando se seleccione.
    /// </summary>
    public void SetupAbilityItem(AbilityData abilityData, Action<AbilityData> selectionCallback)
    {
        _representedAbility = abilityData;
        OnItemSelectedCallback = selectionCallback; // Guardar el callback

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
        if (_representedAbility != null && OnItemSelectedCallback != null)
        {
            // Debug.Log("AbilityListItem_UI: Clic en habilidad: " + _representedAbility.abilityName + ". Llamando al callback.");
            OnItemSelectedCallback.Invoke(_representedAbility); // Llamar al callback asignado
        }
        else
        {
            if (_representedAbility == null) Debug.LogWarning("AbilityListItem_UI: _representedAbility es null al hacer clic.");
            if (OnItemSelectedCallback == null) Debug.LogWarning("AbilityListItem_UI: OnItemSelectedCallback es null. ¿Se configuró desde el Manager?");
        }
    }
}
