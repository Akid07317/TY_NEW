using UnityEngine;

namespace CampusRPG.Combat
{
    public interface IDamageable
    {
        void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source);
    }
}
