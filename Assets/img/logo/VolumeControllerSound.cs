using UnityEngine;
using UnityEngine.UI; // Necesario para Image y Slider

public class NewBehaviourScript : MonoBehaviour
{
    public Image volumeMeterImage; // Asigna tu objeto Image del medidor aquí
    public Sprite[] volumeSprites; // Arreglo de tus Sprites de volumen (05_0, 05_1, etc.)
    public Slider volumeSlider;   // Asigna tu Slider aquí (opcional)

    // Asumiendo que 0 es silencio y el máximo es volumeSprites.Length - 1
    // Si tu slider va de 0 a 100, habrá que mapear ese rango a los índices de los sprites.

    void Start()
    {
        if (volumeSlider != null)
        {
            // Asegúrate de que el método OnVolumeChanged se llame cuando el slider cambie de valor
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            // Establece el valor inicial del slider para que el medidor se actualice al inicio
            OnVolumeChanged(volumeSlider.value);
        }
        else
        {
            // Si no hay slider, puedes establecer un volumen inicial fijo o controlarlo de otra manera
            UpdateVolumeMeter(50); // Ejemplo: volumen inicial al 50%
        }
    }

    // Método que se llama cuando el valor del Slider cambia
    public void OnVolumeChanged(float sliderValue)
    {
        // Mapea el valor del slider (ej. 0-100) al índice del arreglo de sprites
        // Clamp asegura que el índice esté dentro de los límites del arreglo
        int spriteIndex = Mathf.FloorToInt(Mathf.InverseLerp(volumeSlider.minValue, volumeSlider.maxValue, sliderValue) * (volumeSprites.Length - 1));

        // Asegúrate de que el índice no exceda los límites
        spriteIndex = Mathf.Clamp(spriteIndex, 0, volumeSprites.Length - 1);

        UpdateVolumeMeter(spriteIndex);
    }

    // Método para actualizar el sprite del medidor de volumen
    void UpdateVolumeMeter(int spriteIndex)
    {
        if (volumeMeterImage != null && volumeSprites != null && spriteIndex >= 0 && spriteIndex < volumeSprites.Length)
        {
            volumeMeterImage.sprite = volumeSprites[spriteIndex];
        }
        else
        {
            Debug.LogError("Configuración incorrecta del VolumeController. Asegúrate de asignar la imagen y los sprites.");
        }
    }

    // Puedes agregar métodos públicos para cambiar el volumen desde otros scripts
    public void SetVolume(float newVolume)
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = newVolume; // Esto llamará a OnVolumeChanged automáticamente
        }
        else
        {
            // Si no hay slider, mapea newVolume (ej. 0-100) directamente al índice del sprite
            int spriteIndex = Mathf.FloorToInt(Mathf.InverseLerp(0, 100, newVolume) * (volumeSprites.Length - 1));
            spriteIndex = Mathf.Clamp(spriteIndex, 0, volumeSprites.Length - 1);
            UpdateVolumeMeter(spriteIndex);
        }
    }
}