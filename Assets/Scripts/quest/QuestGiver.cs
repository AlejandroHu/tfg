using UnityEngine;
using Ink.Runtime; // Necesario para interactuar con la historia de Ink

// Asegúrate de que los scripts QuestData y NPCDialogue estén accesibles
// using TopDown; // Si están en este namespace

[RequireComponent(typeof(NPCDialogue))] // Asegura que este GameObject también tenga NPCDialogue.cs
public class QuestGiver : MonoBehaviour
{
    [Header("Misión")]
    [Tooltip("La misión que este NPC ofrece. Arrastra aquí el asset QuestData correspondiente.")]
    [SerializeField] private QuestData questToGive;

    [Header("Diálogos de Ink")]
    [Tooltip("Diálogo que se muestra cuando la misión está disponible para ser aceptada.")]
    [SerializeField] private TextAsset dialogueQuestAvailable;
    [Tooltip("Diálogo que se muestra si el jugador habla con el NPC mientras la misión está activa.")]
    [SerializeField] private TextAsset dialogueQuestActive;
    [Tooltip("Diálogo que se muestra cuando el jugador ha cumplido los objetivos y vuelve a hablar con el NPC.")]
    [SerializeField] private TextAsset dialogueQuestCompleted;
    [Tooltip("Diálogo que se muestra después de que la misión ha sido entregada y las recompensas reclamadas.")]
    [SerializeField] private TextAsset dialogueQuestClaimed;

    // Referencia al gestor de diálogo en el mismo GameObject
    private NPCDialogue npcDialogue;

    void Awake()
    {
        // Obtener la referencia al componente NPCDialogue
        npcDialogue = GetComponent<NPCDialogue>();
        if (npcDialogue == null)
        {
            Debug.LogError($"QuestGiver en '{gameObject.name}' necesita un componente NPCDialogue en el mismo GameObject para funcionar.", this);
            this.enabled = false; // Deshabilitar si falta el componente de diálogo
        }
    }

    /// <summary>
    /// Este es el método principal que se llamará cuando el jugador interactúe con este NPC.
    /// Decide qué diálogo mostrar basándose en el estado actual de la misión.
    /// </summary>
    public void StartQuestDialogue()
    {
        if (questToGive == null || npcDialogue == null)
        {
            Debug.LogWarning($"QuestGiver en '{gameObject.name}' no tiene una misión asignada o falta NPCDialogue.");
            // Si no hay misión, podría iniciar un diálogo por defecto
            // npcDialogue.StartDefaultDialogue(); // Si tuvieras un método así en NPCDialogue
            return;
        }

        // Obtener el estado actual de la misión desde el QuestManager
        QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(questToGive);

        // Mostrar el diálogo apropiado
        switch (currentStatus)
        {
            case QuestStatus.Inactive:
                npcDialogue.StartDialogue(dialogueQuestAvailable);
                // Después de que este diálogo termine, necesitaremos aceptar la misión
                // Esto se hará llamando a AcceptThisQuest() desde una función externa de Ink.
                break;
            case QuestStatus.Active:
                npcDialogue.StartDialogue(dialogueQuestActive);
                break;
            case QuestStatus.Completed:
                npcDialogue.StartDialogue(dialogueQuestCompleted);
                // Después de que este diálogo termine, necesitaremos reclamar las recompensas
                // llamando a ClaimRewardsForThisQuest() desde Ink.
                break;
            case QuestStatus.Claimed:
                npcDialogue.StartDialogue(dialogueQuestClaimed);
                break;
        }
    }

    /// <summary>
    /// Método público que será llamado por Ink para aceptar la misión.
    /// </summary>
    public void AcceptThisQuest()
    {
        if (QuestManager.Instance != null && questToGive != null)
        {
            QuestManager.Instance.AcceptQuest(questToGive);
        }
    }

    /// <summary>
    /// Método público que será llamado por Ink para reclamar las recompensas de la misión.
    /// </summary>
    public void ClaimRewardsForThisQuest()
    {
        // Asumimos que el primer miembro de la party es quien recibe las recompensas
        // Necesitarás una referencia a tu PartyManager o una forma de obtener el personaje jugador.
        if (QuestManager.Instance != null && questToGive != null && PartyManager.Instance != null)
        {
            Character playerCharacter = PartyManager.Instance.GetFirstPartyMember();
            if (playerCharacter != null)
            {
                QuestManager.Instance.ClaimQuestRewards(questToGive, playerCharacter);
            }
            else
            {
                Debug.LogError("No se pudo encontrar un personaje jugador para darle las recompensas de la misión.");
            }
        }
    }
}
