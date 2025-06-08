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
    public List<Character> CurrentPartyMembers => new List<Character>(_currentPartyMembers);

    private Character _selectedMenuCharacter;
    public Character SelectedMenuCharacter => _selectedMenuCharacter;

    public static event Action OnPartyRosterChanged;
    public static event Action<Character> OnSelectedMenuCharacterChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
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
                    // Usar el método AddCharacterToParty para asegurar que la lógica sea consistente
                    // y que se aplique DontDestroyOnLoad.
                    AddCharacterToParty(characterComponent);
                }
            }
        }
        Debug.Log("PartyManager: Party inicializada con " + _currentPartyMembers.Count + " miembros.");

        if (_currentPartyMembers.Count > 0)
        {
            SetSelectedMenuCharacter(_currentPartyMembers[0]);
        }
    }

    /// <summary>
    /// Añade un nuevo personaje a la party desde un Prefab. Ideal para reclutamiento.
    /// </summary>
    public bool AddCharacterToPartyFromPrefab(GameObject characterPrefab)
    {
        if (characterPrefab == null)
        {
            Debug.LogError("PartyManager: Se intentó añadir un personaje desde un Prefab nulo.");
            return false;
        }
        GameObject newMemberInstance = Instantiate(characterPrefab);
        Character newMemberCharacter = newMemberInstance.GetComponent<Character>();

        if (newMemberCharacter == null)
        {
            Debug.LogError($"PartyManager: El prefab '{characterPrefab.name}' no tiene un componente Character.cs.", characterPrefab);
            Destroy(newMemberInstance);
            return false;
        }
        return AddCharacterToParty(newMemberCharacter);
    }

    /// <summary>
    /// Añade un nuevo personaje a la party si aún no está presente y lo hace persistente.
    /// </summary>
    public bool AddCharacterToParty(Character newMember)
    {
        if (newMember == null) return false;
        if (_currentPartyMembers.Contains(newMember)) return false;

        _currentPartyMembers.Add(newMember);

        // --- LÓGICA DE PERSISTENCIA CENTRALIZADA ---
        // 1. Desvincular al personaje de cualquier padre para que sea un objeto raíz.
        newMember.transform.SetParent(null);
        // 2. Ahora marcarlo como persistente.
        DontDestroyOnLoad(newMember.gameObject);
        // --- FIN LÓGICA DE PERSISTENCIA ---

        Debug.Log($"PartyManager: {newMember.characterName} añadido a la party y marcado como persistente. Miembros totales: {_currentPartyMembers.Count}");
        OnPartyRosterChanged?.Invoke();

        if (_selectedMenuCharacter == null && _currentPartyMembers.Count == 1)
        {
            SetSelectedMenuCharacter(newMember);
        }
        return true;
    }

    public void SetSelectedMenuCharacter(Character character)
    {
        if (character == null) return;
        if (_currentPartyMembers.Contains(character))
        {
            if (_selectedMenuCharacter != character)
            {
                _selectedMenuCharacter = character;
                OnSelectedMenuCharacterChanged?.Invoke(_selectedMenuCharacter);
            }
        }
    }

    public Character GetFirstPartyMember()
    {
        if (_currentPartyMembers.Count > 0)
        {
            return _currentPartyMembers[0];
        }
        return null;
    }
}
