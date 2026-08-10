using HarmonyLib;
using UnityEngine;
using static BulletSim;
namespace QolComboFix
{
    [HarmonyPatch(typeof(Bullet), "TrajectoryTrace")]
    public static class Patch_Bullet_TrajectoryTrace
    {
        static bool Prefix(ref float deltaTime)
        {
            if (QolComboFixPlugin.Cfg_GunControl_Enable?.Value != true)
                return true;
            if (QolComboFixPlugin.Cfg_GunControl_FixTrajectoryTrace?.Value != true)
                return true;
            deltaTime = Time.fixedDeltaTime;
            return true;
        }
    }
    [HarmonyPatch(typeof(Kinematics), "TrajectorySim")]
    public static class Patch_TrajectorySim
    {
        static bool Prefix(
            bool debug,
            WeaponInfo weaponInfo,
            Vector3 initialVelocity,
            GlobalPosition initialPosition,
            GlobalPosition targetPos,
            Vector3 targetVel,
            Vector3 targetAccel,
            float timeStep,
            out Vector3 missVector,
            out float timeToTarget)
        {
            if (QolComboFixPlugin.Cfg_GunControl_Enable?.Value != true ||
                QolComboFixPlugin.Cfg_GunControl_FixTrajectorySim?.Value != true)
            {
                missVector   = default;
                timeToTarget = default;
                return true;
            }
            const int maxSteps = 2000;
            float dt = Time.fixedDeltaTime;
            GlobalPosition bulletPos    = initialPosition;
            Vector3        bulletVel    = initialVelocity;
            GlobalPosition simTargetPos = targetPos;
            Vector3        simTargetVel = targetVel;
            float t        = 0f;
            float bestDist = float.MaxValue;
            Vector3 bestBulletPos = bulletPos.AsVector3();
            Vector3 bestTargetPos = simTargetPos.AsVector3();
            float bestTime       = 0f;
            for (int i = 0; i < maxSteps; i++)
            {
                simTargetVel += targetAccel * dt;
                simTargetPos += simTargetVel * dt;
                bulletVel.y -= 9.81f * dt * weaponInfo.gravMult;
                bulletVel   -= bulletVel.sqrMagnitude
                               * weaponInfo.dragCoef
                               * dt
                               * bulletVel.normalized
                               / weaponInfo.muzzleVelocity;
                bulletPos   += bulletVel * dt;
                Vector3 diff = bulletPos - simTargetPos;
                float dist   = diff.sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist      = dist;
                    bestBulletPos = bulletPos.AsVector3();
                    bestTargetPos = simTargetPos.AsVector3();
                    bestTime      = t;
                }
                t += dt;
                if (i > 10 && dist > bestDist * 2f)
                    break;
            }
            Vector3 finalDiff = bestBulletPos - bestTargetPos;
            missVector   = Vector3.ProjectOnPlane(finalDiff, bulletVel);
            timeToTarget = bestTime;
            return false;
        }
    }
}