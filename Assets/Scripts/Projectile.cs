using UnityEngine;
using System.Collections; // Necesario para Coroutines si esperamos que termine una animación

public class Projectile : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    [Tooltip("Velocidad a la que se mueve el proyectil.")]
    [SerializeField] private float speed = 10f;
    [Tooltip("Prefab del efecto de impacto a instanciar al colisionar (opcional).")]
    [SerializeField] private GameObject impactVFXPrefab;
    [Tooltip("Tiempo de vida del proyectil en segundos antes de autodestruirse si no golpea nada.")]
    [SerializeField] private float lifetime = 5f;
    [Tooltip("Duración de la animación de impacto antes de destruir el proyectil (si la hay).")]
    [SerializeField] private float impactAnimationDuration = 0.5f; // Ajusta esto a la duración de tu animación de impacto

    private Transform _targetTransform;
    private Combatant _attacker;
    private Combatant _targetCombatant;
    private AbilityData _originatingAbility;
    private Animator _animator; // Referencia al Animator del proyectil
    private bool _hitOccurred = false; // Para evitar múltiples impactos o lógica de impacto

    void Awake()
    {
        _animator = GetComponent<Animator>(); // Obtener el Animator del mismo GameObject
        if (_animator == null)
        {
            // Podrías buscarlo en hijos si la estructura es diferente
            // _animator = GetComponentInChildren<Animator>(); 
            Debug.LogWarning("Projectile: No se encontró el componente Animator en " + gameObject.name, this);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
        // Si tienes una animación de "Volar" que debe reproducirse, asegúrate de que sea el estado
        // por defecto en el Animator Controller del proyectil, o actívala aquí:
        // if (_animator != null) _animator.Play("FlyAnimationName"); // Reemplaza con el nombre de tu animación de volar
    }

    public void Initialize(Combatant target, Combatant attackerCombatant, AbilityData ability)
    {
        if (target != null && target.combatSpriteGO != null)
        {
            _targetTransform = target.combatSpriteGO.transform;
        }
        _targetCombatant = target;
        _attacker = attackerCombatant;
        _originatingAbility = ability;

        if (_targetTransform == null)
        {
            Debug.LogWarning("Projectile: El objetivo (Combatant o su sprite) es nulo. El proyectil se autodestruirá pronto.");
            Destroy(gameObject, 0.1f);
        }
    }

    void Update()
    {
        if (_hitOccurred) return; // Si ya golpeó, no hacer nada más (se está destruyendo o animando impacto)

        if (isTargetDefeatedOrNull())
        {
            Destroy(gameObject);
            return;
        }

        if (_targetTransform != null)
        {
            Vector3 direction = (_targetTransform.position - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            // Opcional: Rotar el proyectil para que mire hacia el objetivo
            if (direction != Vector3.zero) // Evitar error si está en la misma posición
            {
                // Si tu sprite de proyectil apunta hacia la derecha por defecto:
                // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                // Si tu sprite apunta hacia arriba por defecto:
                // float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                // transform.rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
            }
        }
    }

    private bool isTargetDefeatedOrNull()
    {
        if (_targetCombatant != null && _targetCombatant.isDefeated) return true;
        if (_targetTransform == null && _targetCombatant == null) return true;
        if (_targetTransform == null && _targetCombatant != null && !_targetCombatant.isDefeated) return true;
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitOccurred || isTargetDefeatedOrNull()) return;

        if (_targetCombatant != null && _targetCombatant.combatSpriteGO == other.gameObject)
        {
            _hitOccurred = true; // Marcar que el impacto ocurrió para detener el Update y múltiples colisiones
            Debug.Log($"Projectile: ¡Impacto en el objetivo {_targetCombatant.GetName()}!");

            // Detener el movimiento del proyectil
            speed = 0;

            // Aplicar el efecto de la habilidad al objetivo
            if (_originatingAbility != null && _attacker != null && CombatManager.Instance != null)
            {
                if (_originatingAbility.effectType == AbilityEffectType.Damage)
                {
                    int calculatedDamage = Mathf.Max(1, (int)_originatingAbility.power + (_attacker.GetAttack() / 2) - _targetCombatant.GetDefense());
                    _targetCombatant.TakeDamage(calculatedDamage);
                }
                // (Añadir lógica para otros tipos de efectos de habilidad)
            }

            // Iniciar corrutina para manejar el impacto (VFX, animación de impacto del proyectil, y destrucción)
            StartCoroutine(HandleImpactSequence());
        }
        // (Opcional: Lógica si golpea un muro u otro personaje no objetivo)
    }

    private IEnumerator HandleImpactSequence()
    {
        // 1. Instanciar VFX de impacto (si existe)
        if (impactVFXPrefab != null)
        {
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        }

        // 2. Reproducir animación de impacto del proyectil (si existe)
        if (_animator != null)
        {
            // Asume que tienes un estado/trigger "Impact" en el Animator Controller del proyectil
            _animator.SetTrigger("ImpactTrigger");
            // Ocultar el sprite principal si la animación de impacto es un efecto separado
            // SpriteRenderer sr = GetComponent<SpriteRenderer>();
            // if (sr != null) sr.enabled = false;

            yield return new WaitForSeconds(impactAnimationDuration); // Esperar que termine la animación de impacto
        }
        else
        {
            // Si no hay animación de impacto, esperar un poco para el VFX
            if (impactVFXPrefab != null) yield return new WaitForSeconds(0.1f);
        }

        // 3. Destruir el proyectil
        Destroy(gameObject);
    }
}
