using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MenuOpcionesAudio : MonoBehaviour
{
    // Referencia al AudioMixer
    [SerializeField] private AudioMixer audioMixer;

    void Start()
    {
        // Obtiene el volumen guardado en PlayerPrefs (por defecto 0.5)
        float volumenGuardado = PlayerPrefs.GetFloat("Volumen", 0.5f);
        audioMixer.SetFloat("Volumen", volumenGuardado);
    }

    public void CambiarVolumen(float volumen)
    {
        // Ajusta el volumen en el AudioMixer
        audioMixer.SetFloat("Volumen", volumen);

        // Guarda el nuevo valor en PlayerPrefs para futuras sesiones
        PlayerPrefs.SetFloat("Volumen", volumen);
    }
}