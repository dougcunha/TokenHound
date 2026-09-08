using System;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Enforces a maximum dispatch budget within a single billing refresh pass.
/// </summary>
public sealed class CopilotPassDispatchBudget
{
    private readonly int _maxDispatches;
    private int _dispatchesUsed;

    /// <summary>
    /// Initializes a dispatch budget with the specified maximum dispatch count.
    /// </summary>
    /// <param name="maxDispatches">The maximum allowed network dispatches per pass.</param>
    public CopilotPassDispatchBudget(int maxDispatches = 4)
    {

        if (maxDispatches <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDispatches));

        _maxDispatches = maxDispatches;
    }

    /// <summary>
    /// Gets the number of dispatches consumed in this pass.
    /// </summary>
    public int DispatchesUsed
        => _dispatchesUsed;

    /// <summary>
    /// Gets the remaining number of allowed dispatches.
    /// </summary>
    public int RemainingDispatches
        => Math.Max(0, _maxDispatches - _dispatchesUsed);

    /// <summary>
    /// Gets a value indicating whether another dispatch may be made.
    /// </summary>
    public bool CanDispatch
        => _dispatchesUsed < _maxDispatches;

    /// <summary>
    /// Attempts to acquire a single dispatch token from the pass budget.
    /// </summary>
    /// <returns>True if a dispatch was acquired; false if the budget is exhausted.</returns>
    public bool TryAcquire()
    {

        if (_dispatchesUsed >= _maxDispatches)
            return false;

        _dispatchesUsed++;

        return true;
    }
}
