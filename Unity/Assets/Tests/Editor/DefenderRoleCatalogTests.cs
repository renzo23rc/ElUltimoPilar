using System;
using NUnit.Framework;

public class DefenderRoleCatalogTests
{
    [Test]
    public void FirstPlayerGetsTheVeteran()
    {
        Assert.That(DefenderRoleCatalog.FirstAvailable(Array.Empty<DefenderRole>()), Is.EqualTo(DefenderRole.Veteran));
    }

    [Test]
    public void RolesAreAssignedInOrderForUpToFourPlayers()
    {
        Assert.That(DefenderRoleCatalog.FirstAvailable(new[] { DefenderRole.Veteran }), Is.EqualTo(DefenderRole.Technician));
        Assert.That(DefenderRoleCatalog.FirstAvailable(new[] { DefenderRole.Veteran, DefenderRole.Technician }), Is.EqualTo(DefenderRole.Recruit));
        Assert.That(
            DefenderRoleCatalog.FirstAvailable(new[] { DefenderRole.Veteran, DefenderRole.Technician, DefenderRole.Recruit }),
            Is.EqualTo(DefenderRole.Deserter));
    }

    [Test]
    public void RoleFreedByALeavingPlayerIsReused()
    {
        DefenderRole role = DefenderRoleCatalog.FirstAvailable(new[] { DefenderRole.Veteran, DefenderRole.Recruit });

        Assert.That(role, Is.EqualTo(DefenderRole.Technician));
    }

    [Test]
    public void NullTakenRolesIsTreatedAsEmpty()
    {
        Assert.That(DefenderRoleCatalog.FirstAvailable(null), Is.EqualTo(DefenderRole.Veteran));
    }

    [TestCase(DefenderRole.Veteran, "Veterano")]
    [TestCase(DefenderRole.Technician, "Técnica")]
    [TestCase(DefenderRole.Recruit, "Reclutado")]
    [TestCase(DefenderRole.Deserter, "Desertor")]
    [TestCase(DefenderRole.Scavenger, "Chatarrero")]
    [TestCase(DefenderRole.FieldEngineer, "Ingeniera de campo")]
    public void DisplayNamesMatchTheGdd(DefenderRole role, string expected)
    {
        Assert.That(DefenderRoleCatalog.GetDisplayName(role), Is.EqualTo(expected));
    }
}
