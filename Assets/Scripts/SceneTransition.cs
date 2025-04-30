using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [SerializeField] private Image FadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutIn(sceneName));
    }

    private IEnumerator FadeOutIn(string sceneName)
    {
        yield return StartCoroutine(Fade(1)); // Fade out
        SceneManager.LoadScene(sceneName);
        yield return new WaitForSeconds(0.1f); // Esperar a que se cargue la escena
        yield return StartCoroutine(Fade(0)); // Fade in
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = FadeImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            FadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }
}
