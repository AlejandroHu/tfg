using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSonido : MonoBehaviour, IPointerEnterHandler
{
    public AudioSource audioSource;
    public AudioClip hoverClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioSource != null && hoverClip != null) { 
            audioSource.PlayOneShot(hoverClip);
        } 
    }
}
