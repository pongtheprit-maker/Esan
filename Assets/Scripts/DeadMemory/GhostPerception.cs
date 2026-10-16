using UnityEngine;

/// <summary>Shared sight query for enemy AI. The current scene has no ghost chase AI attached.</summary>
public static class GhostPerception
{
    public static bool CanSeePlayer(Transform observer, FirstPersonController player, float range = 15, float fieldOfView = 100)
    {
        if (observer == null || player == null) return false;
        var hiding = player.GetComponent<PlayerHiding>(); if (hiding != null && hiding.IsConcealed) return false;
        Vector3 delta = player.playerCamera.transform.position - observer.position;
        if (delta.magnitude > range || Vector3.Angle(observer.forward, delta) > fieldOfView * .5f) return false;
        float nearest = float.PositiveInfinity; Collider first = null;
        foreach (var hit in Physics.RaycastAll(observer.position, delta.normalized, delta.magnitude + .05f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(observer.root) || hit.distance >= nearest) continue;
            nearest = hit.distance; first = hit.collider;
        }
        return first == null || first.transform.IsChildOf(player.transform);
    }
}
