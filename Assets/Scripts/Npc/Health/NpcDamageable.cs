using UnityEngine;

public class NpcDamageable : MonoBehaviour, IDamageabale
{
    public HitOutcome ApplyHit(in HitInfo hitInfo) 
    {
        return new HitOutcome 
        {
            result = HitResult.Normal,
            damageApplied = hitInfo.baseDamage,
            impactPoint = hitInfo.point,
            hitbox = hitInfo.hitbox
        };
        
    }
}
