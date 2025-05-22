using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogicaPantallaCompleta : MonoBehaviour
{
    // Toggle para activar o desactivar pantalla completa
    public Toggle toggle;

    // Dropdown para seleccionar la resolución de la pantalla
    public TMP_Dropdown resolucionesDropDown;

    // Array que almacena las resoluciones disponibles
    Resolution[] resoluciones;

    void Start()
    {
        // Verifica si el juego está en pantalla completa y ajusta el estado del toggle
        if (Screen.fullScreen)
        {
            toggle.isOn = true;
        }
        else
        {
            toggle.isOn = false;
        }

        // Llama a la función para cargar las resoluciones disponibles
        RevisarResolucion();
    }

    void Update()
    {
        // En este caso, el Update no se usa, podrías eliminarlo si no lo necesitas
    }

    // Método para activar o desactivar la pantalla completa
    public void ActivarPantallaCompleta(bool pantallaCompleta)
    {
        Screen.fullScreen = pantallaCompleta;
    }

    // Método que obtiene las resoluciones disponibles y las asigna al dropdown
    public void RevisarResolucion()
    {
        resoluciones = Screen.resolutions; // Obtiene todas las resoluciones disponibles en el sistema
        resolucionesDropDown.ClearOptions(); // Limpia las opciones actuales del dropdown
        List<string> opciones = new List<string>(); // Lista para almacenar las opciones
        int resolucionActual = 0; // Índice de la resolución actual

        // Recorre todas las resoluciones disponibles y las agrega al dropdown
        for (int i = 0; i < resoluciones.Length; i++)
        {
            string opcion = resoluciones[i].width + " x " + resoluciones[i].height;
            opciones.Add(opcion);

            // Identifica cuál es la resolución actual
            if (Screen.fullScreen && resoluciones[i].width == Screen.currentResolution.width && resoluciones[i].height == Screen.currentResolution.height)
            {
                resolucionActual = i;
            }
        }

        // Añade las opciones al dropdown y establece la resolución actual como seleccionada
        resolucionesDropDown.AddOptions(opciones);
        resolucionesDropDown.value = resolucionActual;
        resolucionesDropDown.RefreshShownValue();
    }

    // Método para cambiar la resolución de la pantalla según la selección en el dropdown
    public void CambiarResolucion(int indiceResolucion)
    {
        Resolution resolucion = resoluciones[indiceResolucion]; // Obtiene la resolución seleccionada
        Screen.SetResolution(resolucion.width, resolucion.height, Screen.fullScreen); // Aplica la nueva resolución
    }
}