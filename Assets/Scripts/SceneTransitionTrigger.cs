using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Forest"; // o el nombre de tu otra escena

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Aquí llamaremos al fade y cambio de escena
            SceneTransition.Instance.FadeToScene(sceneToLoad);
        }
    }
}
