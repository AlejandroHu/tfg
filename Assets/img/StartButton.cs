using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    // Método que se llamará al pulsar el botón Start
    public void LoadVillageScene()
    {
        Debug.Log("Cargando la escena 'Village'...");
        SceneManager.LoadScene("Village");
    }
}
