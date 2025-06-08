using UnityEngine;

// Este script será el único responsable de abrir y cerrar los paneles de la UI.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Referencias a Prefabs de Paneles")]
    [Tooltip("Arrastra aquí el PREFAB de tu panel de inventario completo.")]
    [SerializeField] private GameObject inventoryUIPrefab;
    [Tooltip("Arrastra aquí el PREFAB de tu panel de diario de misiones.")]
    [SerializeField] private GameObject questLogUIPrefab;
    [Tooltip("Arrastra aquí el PREFAB de tu panel de equipo.")]
    [SerializeField] private GameObject equipmentUIPrefab;
    // Añade más prefabs aquí para otros paneles que quieras gestionar

    // Referencias a las instancias actuales de los paneles para poder cerrarlos
    private GameObject currentInventoryUIInstance;
    private GameObject currentQuestLogUIInstance;
    private GameObject currentEquipmentUIInstance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Escuchar las teclas para abrir/cerrar los paneles
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryUI();
        }

        if (Input.GetKeyDown(KeyCode.U)) // Asumiendo que 'U' es para el diario de misiones
        {
            ToggleQuestLogUI();
        }

        // Si tienes una tecla para el equipo, por ejemplo 'C'
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleEquipmentUI();
        }
    }

    // Método para alternar la visibilidad del inventario
    public void ToggleInventoryUI()
    {
        // Si el inventario ya está abierto, lo cerramos
        if (currentInventoryUIInstance != null)
        {
            Destroy(currentInventoryUIInstance);
            currentInventoryUIInstance = null;
        }
        else // Si no, buscamos el Canvas y lo creamos
        {
            Transform mainCanvas = FindMainCanvasTransform();
            if (mainCanvas != null)
            {
                currentInventoryUIInstance = Instantiate(inventoryUIPrefab, mainCanvas);
            }
        }
    }

    // Método para alternar la visibilidad del diario de misiones
    public void ToggleQuestLogUI()
    {
        if (currentQuestLogUIInstance != null)
        {
            Destroy(currentQuestLogUIInstance);
            currentQuestLogUIInstance = null;
        }
        else
        {
            Transform mainCanvas = FindMainCanvasTransform();
            if (mainCanvas != null)
            {
                currentQuestLogUIInstance = Instantiate(questLogUIPrefab, mainCanvas);
            }
        }
    }

    // Método para alternar la visibilidad del panel de equipo
    public void ToggleEquipmentUI()
    {
        if (currentEquipmentUIInstance != null)
        {
            Destroy(currentEquipmentUIInstance);
            currentEquipmentUIInstance = null;
        }
        else
        {
            Transform mainCanvas = FindMainCanvasTransform();
            if (mainCanvas != null)
            {
                currentEquipmentUIInstance = Instantiate(equipmentUIPrefab, mainCanvas);
            }
        }
    }


    // Busca el Canvas en la escena actual para poder instanciar la UI como hija
    private Transform FindMainCanvasTransform()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            return canvas.transform;
        }
        else
        {
            Debug.LogError("UIManager: ¡No se encontró ningún Canvas en la escena! No se puede mostrar la UI.");
            return null;
        }
    }
}
