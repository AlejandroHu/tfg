using UnityEngine;
public class AutoDestroyVFX : MonoBehaviour
{
    public float lifetime = 1f;
    void Start() { Destroy(gameObject, lifetime); }
}
