using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Necesario para Action (eventos)

// Asegúrate de que el namespace de Character sea accesible
// using TuJuego.Personajes; 
// using TopDown; 

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Configuración Inicial de la Party")]
    [Tooltip("Arrastra aquí los GameObjects de los personajes que comenzarán en la party. Deben tener el script Character.cs.")]
    [SerializeField] private List<GameObject> initialPartyMemberGameObjects = new List<GameObject>();

    private List<Character> _currentPartyMembers = new List<Character>();
    public List<Character> CurrentPartyMembers => new List<Character>(_currentPartyMembers); // Devuelve una copia para evitar modificaciones externas directas

    private Character _selectedMenuCharacter;
    public Character SelectedMenuCharacter => _selectedMenuCharacter;

    // Eventos para notificar cambios
    public static event Action OnPartyRosterChanged; // Se dispara cuando un miembro se añade o quita
    public static event Action<Character> OnSelectedMenuCharacterChanged; // Se dispara cuando el personaje seleccionado para menús cambia

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PartyManager: Instancia duplicada destruida.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeParty();
    }

    private void InitializeParty()
    {
        _currentPartyMembers.Clear();
        foreach (GameObject memberGO in initialPartyMemberGameObjects)
        {
            if (memberGO != null)
            {
                Character characterComponent = memberGO.GetComponent<Character>();
                if (characterComponent != null)
                {
                    if (!_currentPartyMembers.Contains(characterComponent))
                    {
                        _currentPartyMembers.Add(characterComponent);
                        // Asegurar que los personajes iniciales también sean DontDestroyOnLoad
                        // (Asumiendo que Character.cs ya tiene DontDestroyOnLoad en su Awake)
                    }
                }
                else
                {
                    Debug.LogWarning($"PartyManager: GameObject '{memberGO.name}' no tiene componente Character.", memberGO);
                }
            }
        }
        Debug.Log("PartyManager: Party inicializada con " + _currentPartyMembers.Count + " miembros.");

        // Seleccionar el primer miembro por defecto si la party no está vacía
        if (_currentPartyMembers.Count > 0)
        {
            SetSelectedMenuCharacter(_currentPartyMembers[0]);
        }
    }

    /// <summary>
    /// Añade un nuevo personaje a la party si aún no está presente.
    /// </summary>
    /// <param name="newMember">El componente Character del nuevo miembro.</param>
    /// <returns>True si el personaje fue añadido, False si ya estaba o es nulo.</returns>
    public bool AddCharacterToParty(Character newMember)
    {
        if (newMember == null)
        {
            Debug.LogWarning("PartyManager: Intento de añadir un miembro nulo a la party.");
            return false;
        }
        if (_currentPartyMembers.Contains(newMember))
        {
            Debug.LogWarning($"PartyManager: {newMember.characterName} ya está en la party.");
            return false;
        }

        _currentPartyMembers.Add(newMember);
        // Asegurar que el nuevo miembro también sea persistente
        // (Asumiendo que Character.cs ya tiene DontDestroyOnLoad en su Awake,
        // o si se instancia un prefab que lo tenga)
        // DontDestroyOnLoad(newMember.gameObject); // Solo si el Character no lo hace ya por sí mismo

        Debug.Log($"PartyManager: {newMember.characterName} añadido a la party. Miembros totales: {_currentPartyMembers.Count}");
        OnPartyRosterChanged?.Invoke(); // Notificar que la lista de miembros ha cambiado

        // Si es el primer miembro añadido y no había ninguno seleccionado, seleccionarlo
        if (_selectedMenuCharacter == null && _currentPartyMembers.Count == 1)
        {
            SetSelectedMenuCharacter(newMember);
        }
        return true;
    }

    // NOTA: Según tu feedback, no implementaremos RemoveCharacterFromParty ni reordenación.

    /// <summary>
    /// Establece el personaje que está actualmente seleccionado en los menús de party/equipo.
    /// </summary>
    /// <param name="character">El personaje a seleccionar.</param>
    public void SetSelectedMenuCharacter(Character character)
    {
        if (character == null)
        {
            // Debug.LogWarning("PartyManager: Intento de seleccionar un personaje nulo para menús.");
            // _selectedMenuCharacter = null; // Podrías permitir deseleccionar
            // OnSelectedMenuCharacterChanged?.Invoke(null);
            return;
        }

        if (_currentPartyMembers.Contains(character))
        {
            if (_selectedMenuCharacter != character) // Solo cambiar y notificar si es diferente
            {
                _selectedMenuCharacter = character;
                Debug.Log("PartyManager: Personaje seleccionado para menús: " + _selectedMenuCharacter.characterName);
                OnSelectedMenuCharacterChanged?.Invoke(_selectedMenuCharacter);
            }
        }
        else
        {
            Debug.LogWarning($"PartyManager: Se intentó seleccionar a '{character.characterName}' para menús, pero no está en la party actual.");
        }
    }

    /// <summary>
    /// Obtiene el primer miembro de la party. Útil para un personaje por defecto.
    /// </summary>
    /// <returns>El primer Character en la party, o null si la party está vacía.</returns>
    public Character GetFirstPartyMember()
    {
        if (_currentPartyMembers.Count > 0)
        {
            return _currentPartyMembers[0];
        }
        return null;
    }
}
