using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicIdleTester : MonoBehaviour
{
    [Tooltip("Arrastra aquí desde el Inspector el NPC que tiene DynamicIdleBehavior.cs")]
    [SerializeField] private DynamicIdleBehavior npcToTest;

    [Tooltip("Opcional: Arrastra aquí al jugador u otro objeto para que el NPC lo mire")]
    [SerializeField] private Transform targetToFace;

    void Update()
    {
        if (npcToTest == null)
        {
            if (Input.anyKeyDown) // Solo para que no spamee el log si no hay NPC asignado
            {
                Debug.LogWarning("DynamicIdleTester: No se ha asignado un NPC para probar en el Inspector.");
            }
            return;
        }

        // Probar hacer que el NPC se enfoque en un objetivo (simula inicio de diálogo)
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Tecla '1'
        {
            if (targetToFace != null)
            {
                Debug.Log("Tester: Haciendo que NPC se enfoque en " + targetToFace.name);
                npcToTest.FocusOnTarget(targetToFace);
            }
            else
            {
                Debug.LogWarning("Tester: No se asignó un targetToFace para la prueba de FocusOnTarget. El NPC se quedará en su pose actual pero detendrá el ciclo dinámico.");
                npcToTest.FocusOnTarget(null); // Probar cómo reacciona sin un objetivo específico
            }
        }

        // Probar reanudar el comportamiento de idle dinámico (simula fin de diálogo)
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Tecla '2'
        {
            Debug.Log("Tester: Reanudando el idle dinámico del NPC.");
            npcToTest.ResumeDynamicIdle();
        }
    }
}
