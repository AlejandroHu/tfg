using UnityEngine;
using System.Collections.Generic; // Para List si una habilidad lanza múltiples proyectiles a la vez

// Asegúrate de que CombatManager, Combatant, AbilityData, ItemData, Projectile sean accesibles
// (Pueden estar en el mismo namespace o necesitarás 'using')
// Asumiendo que TopDown incluye Character, AbilityData, Projectile, etc. o que están en el namespace global.
// using TopDown; 

public class CombatSpriteEventHandler : MonoBehaviour
{
    private Combatant _ownerCombatant;
    private List<Combatant> _skillTargets; // Para habilidades que pueden tener múltiples objetivos
    private Combatant _singleDirectTarget; // Para ataques básicos o habilidades de un solo objetivo

    private AbilityData _currentAbilityToLaunch;
    // private ItemData _weaponUsedForBasicAttack; // Ya no es necesario si el daño viene precalculado
    private int _basicAttackPreCalculatedDamage;
    private GameObject _projectilePrefabToLaunch;


    /// <summary>
    /// CombatManager llama a esto ANTES de reproducir la animación de una HABILIDAD con proyectil.
    /// </summary>
    public void SetupForSkillProjectileLaunch(Combatant owner, List<Combatant> targets, AbilityData ability, GameObject projectilePrefab)
    {
        _ownerCombatant = owner;
        _skillTargets = targets;
        _singleDirectTarget = (targets != null && targets.Count == 1) ? targets[0] : null;
        _currentAbilityToLaunch = ability;
        _projectilePrefabToLaunch = projectilePrefab;

        // _weaponUsedForBasicAttack = null; 
        _basicAttackPreCalculatedDamage = 0;
        // Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetupForSkillProjectileLaunch: Habilidad '{ability?.abilityName}', Proyectil '{projectilePrefab?.name}', {targets?.Count} objetivos.");
    }

    /// <summary>
    /// CombatManager llama a esto ANTES de reproducir la animación de un ATAQUE BÁSICO con proyectil.
    /// </summary>
    public void SetupForBasicAttackProjectileLaunch(Combatant owner, Combatant target, GameObject projectilePrefab, int precalculatedDamage)
    {
        _ownerCombatant = owner;
        _skillTargets = null;
        _singleDirectTarget = target;
        _currentAbilityToLaunch = null;
        _projectilePrefabToLaunch = projectilePrefab;
        _basicAttackPreCalculatedDamage = precalculatedDamage;

        // _originatingWeapon = weapon; 
        // Debug.Log($"[{Time.frameCount}] {gameObject.name} - SetupForBasicAttackProjectileLaunch: Proyectil '{projectilePrefab?.name}', Objetivo '{target?.GetName()}', Daño: {precalculatedDamage}");
    }


    /// <summary>
    /// Este método será llamado por un Evento de Animación
    /// en el frame específico donde el proyectil debe ser lanzado.
    /// </summary>
    public void AnimationEvent_LaunchProjectile()
    {
        if (_ownerCombatant == null || _projectilePrefabToLaunch == null || CombatManager.Instance == null)
        {
            Debug.LogError($"AnimationEvent_LaunchProjectile en {gameObject.name}: Faltan datos para lanzar (owner, projectilePrefab, o CombatManager.Instance).");
            return;
        }
        if (_projectilePrefabToLaunch.GetComponent<Projectile>() == null)
        {
            Debug.LogError($"AnimationEvent_LaunchProjectile: El prefab '{_projectilePrefabToLaunch.name}' no tiene el script Projectile.cs.");
            return;
        }

        List<Combatant> targetsToLaunchAt = new List<Combatant>();
        if (_currentAbilityToLaunch != null && _skillTargets != null)
        {
            targetsToLaunchAt.AddRange(_skillTargets);
        }
        else if (_singleDirectTarget != null)
        {
            targetsToLaunchAt.Add(_singleDirectTarget);
        }

        if (targetsToLaunchAt.Count == 0)
        {
            Debug.LogWarning($"AnimationEvent_LaunchProjectile: No hay objetivos válidos para el proyectil de {_ownerCombatant.GetName()}.");
            return;
        }

        Debug.Log($"AnimationEvent_LaunchProjectile: {_ownerCombatant.GetName()} está lanzando '{_projectilePrefabToLaunch.name}'.");

        foreach (Combatant target in targetsToLaunchAt)
        {
            if (target == null || target.isDefeated || target.combatSpriteGO == null) continue;

            // --- MODIFICADO: Lógica de Spawn Simplificada ---
            Vector3 spawnPos;
            Renderer attackerRenderer = _ownerCombatant.combatSpriteGO.GetComponent<Renderer>();
            if (attackerRenderer != null)
            {
                // Origen en el centro del sprite del atacante, ligeramente elevado
                // Puedes ajustar el multiplicador de bounds.extents.y o usar un offset fijo en CombatManager
                // Por ahora, usamos un offset pequeño para que no salga exactamente del centro.
                spawnPos = attackerRenderer.bounds.center + new Vector3(0, attackerRenderer.bounds.extents.y * 0.3f, 0);
            }
            else
            {
                // Fallback si no hay renderer (aunque el combatSpriteGO debería tener uno)
                // Usar la posición del combatSpriteGO con un offset fijo.
                spawnPos = _ownerCombatant.combatSpriteGO.transform.position + new Vector3(0, 0.3f, 0); // Ajusta este offset Y
            }
            // --- FIN MODIFICACIÓN ---


            Quaternion projectileRotation = Quaternion.identity;
            if (target.combatSpriteGO != null)
            {
                Vector3 directionToTarget = (target.combatSpriteGO.transform.position - spawnPos).normalized;
                if (directionToTarget != Vector3.zero)
                {
                    float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                    projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
            }

            GameObject projectileGO = Instantiate(_projectilePrefabToLaunch, spawnPos, projectileRotation);
            Projectile projectileScript = projectileGO.GetComponent<Projectile>();

            if (projectileScript != null)
            {
                if (_currentAbilityToLaunch != null) // Si fue lanzado por una habilidad
                {
                    projectileScript.InitializeSkillProjectile(target, _ownerCombatant, _currentAbilityToLaunch);
                }
                else // Si fue lanzado por un ataque básico
                {
                    // Pasamos null para ItemData weapon, ya que el daño viene precalculado
                    projectileScript.InitializeBasicAttackProjectile(target, _ownerCombatant, null, _basicAttackPreCalculatedDamage);
                }
            }
            else
            {
                Destroy(projectileGO);
            }

            if (_currentAbilityToLaunch != null && (_currentAbilityToLaunch.targetType == AbilityTargetType.SingleEnemy || _currentAbilityToLaunch.targetType == AbilityTargetType.SingleAlly)) break;
            if (_currentAbilityToLaunch == null) break;
        }

        _currentAbilityToLaunch = null;
        _projectilePrefabToLaunch = null;
        _skillTargets = null;
        _singleDirectTarget = null;
    }
}
