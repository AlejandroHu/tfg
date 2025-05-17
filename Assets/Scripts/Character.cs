using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Información Básica del Personaje")]
    public string characterName = "Personaje";
    public int level = 1;

    // Más adelante añadiremos aquí:
    // - Stats (HP, MP, Ataque, Defensa, etc.)
    // - Referencias a su equipamiento (Dictionary<EquipmentSlot, ItemData> equippedItems)
    // - Lista de habilidades
    // - Métodos para recibir daño, curar, usar habilidades, etc.

    // Ejemplo de un método que ItemData podría llamar (actualmente solo un Debug.Log)
    public void Heal(int amount)
    {
        Debug.Log(characterName + " se curó por " + amount + " HP.");
        // Lógica real de curación iría aquí
    }

    public void RestoreMana(int amount)
    {
        Debug.Log(characterName + " restauró " + amount + " MP.");
        // Lógica real de restauración de maná iría aquí
    }

    // Puedes añadir más métodos placeholder si ItemData los necesita
}
