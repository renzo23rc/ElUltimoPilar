using System;
using System.Collections.Generic;

/// <summary>
/// Unity-free assignment order and display names of the GDD defender roles.
/// </summary>
public static class DefenderRoleCatalog
{
    private static readonly DefenderRole[] AssignmentOrder =
    {
        DefenderRole.Veteran,
        DefenderRole.Technician,
        DefenderRole.Recruit,
        DefenderRole.Deserter,
        DefenderRole.Scavenger,
        DefenderRole.FieldEngineer
    };

    /// <summary>Returns the first role, in assignment order, that no current player holds.</summary>
    /// <param name="takenRoles">The roles already held by registered players.</param>
    /// <returns>The first free role, or the first role when every role is taken.</returns>
    public static DefenderRole FirstAvailable(IEnumerable<DefenderRole> takenRoles)
    {
        var taken = new HashSet<DefenderRole>(takenRoles ?? Array.Empty<DefenderRole>());
        foreach (DefenderRole role in AssignmentOrder)
        {
            if (!taken.Contains(role))
            {
                return role;
            }
        }

        return AssignmentOrder[0];
    }

    /// <summary>Gets the Spanish display name used by the HUD.</summary>
    /// <param name="role">The defender role.</param>
    /// <returns>The display name.</returns>
    public static string GetDisplayName(DefenderRole role)
    {
        return role switch
        {
            DefenderRole.Veteran => "Veterano",
            DefenderRole.Technician => "Técnica",
            DefenderRole.Recruit => "Reclutado",
            DefenderRole.Deserter => "Desertor",
            DefenderRole.Scavenger => "Chatarrero",
            DefenderRole.FieldEngineer => "Ingeniera de campo",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported defender role.")
        };
    }
}
