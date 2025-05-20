using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Asegúrate de que el namespace de Character sea accesible
// using TuJuego.Personajes; // Si Character.cs está en este namespace
// using TopDown; // Si Character.cs está en el namespace TopDown


public class PartyManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PartyManager Instance { get; private set; }

    [Header("Miembros de la Party")]
    [Tooltip("Lista de los GameObjects de los personajes que están actualmente en la party. Deben tener el script Character.cs.")]
    [SerializeField] private List<GameObject> partyMemberGameObjects = new List<GameObject>();

    // Lista interna de los componentes Character de los miembros de la party
    private List<Character> _currentPartyMembers = new List<Character>();
    public List<Character> CurrentPartyMembers => _currentPartyMembers; // Propiedad pública de solo lectura

    // (Opcional) Para mantener un registro del personaje actualmente seleccionado en los menús
    // private Character _selectedMenuCharacter;
    // public Character SelectedMenuCharacter => _selectedMenuCharacter;

    void Awake()
    {
        // Lógica del Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("PartyManager: Se encontró otra instancia. Destruyendo este GameObject.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
         DontDestroyOnLoad(gameObject); // Si este manager debe persistir entre escenas

        InitializeParty();
    }

    /// <summary>
    /// Inicializa la lista _currentPartyMembers a partir de los GameObjects asignados en el Inspector.
    /// </summary>
    private void InitializeParty()
    {
        _currentPartyMembers.Clear();
        foreach (GameObject memberGO in partyMemberGameObjects)
        {
            if (memberGO != null)
            {
                Character characterComponent = memberGO.GetComponent<Character>();
                if (characterComponent != null)
                {
                    if (!_currentPartyMembers.Contains(characterComponent)) // Evitar duplicados
                    {
                        _currentPartyMembers.Add(characterComponent);
                    }
                }
                else
                {
                    Debug.LogWarning("PartyManager: El GameObject '" + memberGO.name + "' asignado a partyMemberGameObjects no tiene un componente Character.", memberGO);
                }
            }
        }
        Debug.Log("PartyManager: Party inicializada con " + _currentPartyMembers.Count + " miembros.");

        // Opcional: Seleccionar el primer miembro por defecto para los menús
        // if (_currentPartyMembers.Count > 0)
        // {
        //    SetSelectedMenuCharacter(_currentPartyMembers[0]);
        // }
    }

    /// <summary>
    /// (Opcional) Establece el personaje que está actualmente seleccionado en los menús de party/equipo.
    /// </summary>
    // public void SetSelectedMenuCharacter(Character character)
    // {
    //     if (_currentPartyMembers.Contains(character))
    //     {
    //         _selectedMenuCharacter = character;
    //         Debug.Log("PartyManager: Personaje seleccionado para menús: " + character.characterName);
    //         // Aquí podrías disparar un evento si otras UIs necesitan saber que el personaje seleccionado cambió
    //         // OnSelectedMenuCharacterChanged?.Invoke(_selectedMenuCharacter);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("PartyManager: Se intentó seleccionar un personaje que no está en la party actual: " + character.characterName);
    //     }
    // }

    // Más adelante podrías añadir métodos para:
    // AddCharacterToParty(Character newMember)
    // RemoveCharacterFromParty(Character memberToRemove)
    // GetPartyMemberByIndex(int index)
    // etc.
}
