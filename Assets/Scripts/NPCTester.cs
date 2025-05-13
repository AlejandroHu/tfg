using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTester : MonoBehaviour
{
    // Arrastra aquí desde el Inspector el NPC que tiene NPCMovement.cs
    [SerializeField] private NPCMovement npcToTest;

    // Opcional: Arrastra aquí al jugador u otro objeto para que el NPC lo mire
    [SerializeField] private Transform targetToFace;

    void Update()
    {
        if (npcToTest == null) return;

        // Probar Pausar Movimiento
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Tester: Pausando movimiento del NPC.");
            npcToTest.SetMovementPaused(true);
        }

        // Probar Reanudar Movimiento
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Tester: Reanudando movimiento del NPC.");
            npcToTest.SetMovementPaused(false);
        }

        // Probar Hacer que Mire a un Objetivo (y se ponga en Idle)
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (targetToFace != null)
            {
                Debug.Log("Tester: Haciendo que NPC mire a " + targetToFace.name);
                // Es importante que si se va a girar para hablar, primero se pause el movimiento general
                npcToTest.SetMovementPaused(true); // Aseguramos que esté pausado
                npcToTest.SetIdleAndFaceTarget(targetToFace);
            }
            else
            {
                Debug.LogWarning("Tester: No se asignó un targetToFace para la prueba de mirar.");
                // Podrías hacer que mire en una dirección fija para probar
                // npcToTest.SetIdleAndFaceTarget(null); // O una versión que tome un Vector2
            }
        }
    }
}
