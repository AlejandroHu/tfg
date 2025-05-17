using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryTestAdder : MonoBehaviour
{
    [Tooltip("Arrastra aquí tu asset de ItemData 'PocionSimple' desde la ventana de Proyecto.")]
    public ItemData itemDePrueba;
    public int cantidadAAñadir = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8)) // Presiona '8' para añadir
        {
            if (itemDePrueba != null && PlayerInventory.Instance != null)
            {
                bool anadido = PlayerInventory.Instance.AddItem(itemDePrueba, cantidadAAñadir);
                if (anadido)
                {
                    Debug.Log($"Añadido {cantidadAAñadir} de {itemDePrueba.itemName} al inventario. Contenido actual:");
                    PrintInventory();
                }
                else
                {
                    Debug.Log($"No se pudo añadir {itemDePrueba.itemName}. ¿Inventario lleno?");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha9)) // Presiona '9' para quitar
        {
            if (itemDePrueba != null && PlayerInventory.Instance != null)
            {
                bool quitado = PlayerInventory.Instance.RemoveItem(itemDePrueba, cantidadAAñadir);
                if (quitado)
                {
                    Debug.Log($"Quitados {cantidadAAñadir} de {itemDePrueba.itemName} del inventario. Contenido actual:");
                    PrintInventory();
                }
                else
                {
                    Debug.Log($"No se pudo quitar {itemDePrueba.itemName}. ¿No había suficientes o no se encontró?");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) // Presiona '0' para ver el inventario en consola
        {
            PrintInventory();
        }
    }

    void PrintInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            Debug.Log("--- Contenido del Inventario ---");
            if (PlayerInventory.Instance.inventorySlots.Count == 0)
            {
                Debug.Log("Inventario Vacío.");
            }
            foreach (var slot in PlayerInventory.Instance.inventorySlots)
            {
                if (slot.item != null)
                {
                    Debug.Log($"- {slot.item.itemName} x {slot.quantity}");
                }
            }
            Debug.Log("-----------------------------");
        }
    }
}
