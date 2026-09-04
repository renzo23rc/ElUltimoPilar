using System;
using System.Collections.Generic;

namespace UltimoPilar.Arena;

/// <summary>Tracks irreversible arena phase progression and pending transitions.</summary>
public sealed class ArenaPhaseState
{
    private const int FirstPhase = 1;

    private readonly Queue<int> pendingPhases = new();
    private readonly HashSet<int> activatedPhases = new();
    private int currentPhase = FirstPhase;

    /// <summary>Gets the highest phase accepted as activated.</summary>
    public int CurrentPhase => currentPhase;

    /// <summary>Gets the phases waiting to be activated in order.</summary>
    public IReadOnlyCollection<int> PendingPhases => pendingPhases;

    /// <summary>Gets the phases that have already been activated.</summary>
    public IReadOnlyCollection<int> ActivatedPhases => activatedPhases;

    /// <summary>Queues each missing phase between the current and target phases.</summary>
    /// <param name="currentPhase">The phase currently reported by the caller.</param>
    /// <param name="targetPhase">The forward phase requested by the caller.</param>
    public void EnqueueMissing(int currentPhase, int targetPhase)
    {
        if (!IsValidPhase(currentPhase) || !IsValidPhase(targetPhase))
        {
            return;
        }

        if (targetPhase <= currentPhase || targetPhase <= this.currentPhase)
        {
            return;
        }

        int firstMissingPhase = Math.Max(currentPhase + 1, this.currentPhase + 1);
        for (int phase = firstMissingPhase; phase <= targetPhase; phase++)
        {
            if (activatedPhases.Contains(phase) || pendingPhases.Contains(phase))
            {
                continue;
            }

            pendingPhases.Enqueue(phase);
        }
    }

    /// <summary>Removes and returns the next pending phase.</summary>
    /// <param name="phase">The next pending phase, or zero when the queue is empty.</param>
    /// <returns><see langword="true"/> when a pending phase was returned.</returns>
    public bool TryDequeue(out int phase)
    {
        if (pendingPhases.Count == 0)
        {
            phase = default;
            return false;
        }

        phase = pendingPhases.Dequeue();
        return true;
    }

    /// <summary>Marks a valid forward phase as activated without allowing regression.</summary>
    /// <param name="phase">The phase that completed activation.</param>
    public void MarkActivated(int phase)
    {
        if (!IsValidPhase(phase) || phase <= currentPhase)
        {
            return;
        }

        activatedPhases.Add(phase);
        currentPhase = phase;
    }

    /// <summary>Restores the initial phase and clears all pending and activated phases.</summary>
    public void Reset()
    {
        pendingPhases.Clear();
        activatedPhases.Clear();
        currentPhase = FirstPhase;
    }

    private static bool IsValidPhase(int phase)
    {
        return phase >= FirstPhase;
    }
}
