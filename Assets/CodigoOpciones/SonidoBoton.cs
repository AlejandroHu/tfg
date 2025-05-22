using UnityEngine;
using UnityEngine.UI;

public class SonidoBoton : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        // Obtén el componente Button del GameObject
        Button button = GetComponent<Button>();

        if (button != null)
        {
            // Añade un listener al evento onClick del botón
            button.onClick.AddListener(PlaySound);
        }
    }

    void PlaySound()
    {
        // Reproduce el sonido
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}