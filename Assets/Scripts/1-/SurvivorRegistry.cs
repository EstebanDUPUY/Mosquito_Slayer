using System;
using System.Collections.Generic;
using UnityEngine;

public static class SurvivorRegistry
{
    static readonly HashSet<PlayerSucc> _alive = new HashSet<PlayerSucc>();
    static bool _roundLocked = false;

    public static event Action<PlayerSucc> OnLastSurvivor; // broadcast quand il ne reste qu’1

    public static void Reset()
    {
        _alive.Clear();
        _roundLocked = false;
    }

    public static void Register(PlayerSucc p)
    {
        if (p == null) return;
        _alive.Add(p);
    }

    public static void Died(PlayerSucc p)
    {
        if (p == null || _roundLocked) return;

        _alive.Remove(p);

        if (_alive.Count == 1)
        {
            _roundLocked = true;
            // il reste exactement 1 vivant
            foreach (var last in _alive)
            {
                OnLastSurvivor?.Invoke(last);
                break;
            }
        }
        else if (_alive.Count == 0)
        {
            // personne en vie → on peut aussi invoquer null si tu veux gérer une égalité
            OnLastSurvivor?.Invoke(null);
        }
    }
}
