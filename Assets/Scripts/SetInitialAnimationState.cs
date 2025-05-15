using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetInitialAnimationState : MonoBehaviour
{
    [Tooltip("El componente Animator del NPC (o de su hijo 'CharacterSprite').")]
    [SerializeField] private Animator anim;

    [Tooltip("El nombre exacto del estado de animación que se reproducirá al inicio (ej: 'IdleUp', 'IdleFacingWindow').")]
    [SerializeField] private string initialAnimationStateName = "IdleUp";

    void Start()
    {
        // Intentar obtener el Animator si no está asignado en el Inspector
        if (anim == null)
        {
            // Asumiendo tu estructura donde el Animator está en un hijo llamado "CharacterSprite"
            Transform characterSprite = transform.Find("CharacterSprite");
            if (characterSprite != null)
            {
                anim = characterSprite.GetComponent<Animator>();
            }
            else
            {
                // Fallback: buscar el Animator en el mismo GameObject
                anim = GetComponent<Animator>();
            }
        }

        // Si se encontró un Animator y se especificó un nombre de estado
        if (anim != null && !string.IsNullOrEmpty(initialAnimationStateName))
        {
            // Reproducir el estado de animación especificado
            anim.Play(initialAnimationStateName);
            Debug.Log(gameObject.name + " - Estado inicial fijado a: " + initialAnimationStateName);
        }
        else if (anim == null)
        {
            Debug.LogError("SetInitialAnimationState: Animator no encontrado en " + gameObject.name + " o su hijo 'CharacterSprite'. No se pudo fijar el estado inicial.", this);
        }
        else if (string.IsNullOrEmpty(initialAnimationStateName))
        {
            Debug.LogWarning("SetInitialAnimationState: No se especificó un 'Initial Animation State Name' para " + gameObject.name, this);
        }
    }
}
