using NUnit.Framework;

public class PlayerCommandTests
{
    [Test]
    public void DefaultCommandHasDeterministicEmptyValues()
    {
        var command = default(PlayerCommand);

        Assert.That(command.MoveX, Is.EqualTo(0f));
        Assert.That(command.MoveY, Is.EqualTo(0f));
        Assert.That(command.LookX, Is.EqualTo(0f));
        Assert.That(command.LookY, Is.EqualTo(0f));
        Assert.That(command.Jump, Is.False);
        Assert.That(command.Fire, Is.False);
        Assert.That(command.Interact, Is.False);
        Assert.That(command.Heal, Is.False);
        Assert.That(command.Ability, Is.False);
        Assert.That(command.PreviousWeapon, Is.False);
        Assert.That(command.NextWeapon, Is.False);
        Assert.That(command.WeaponSlot, Is.Null);
    }

    [Test]
    public void AnalogValuesArePreserved()
    {
        var command = new PlayerCommand(0.375f, -0.625f, 0.125f, -0.875f);

        Assert.That(command.MoveX, Is.EqualTo(0.375f));
        Assert.That(command.MoveY, Is.EqualTo(-0.625f));
        Assert.That(command.LookX, Is.EqualTo(0.125f));
        Assert.That(command.LookY, Is.EqualTo(-0.875f));
    }

    [Test]
    public void ButtonsRemainIndependent()
    {
        var command = new PlayerCommand(
            moveX: 0f,
            moveY: 0f,
            lookX: 0f,
            lookY: 0f,
            jump: true,
            fire: false,
            interact: true,
            heal: false,
            ability: true,
            previousWeapon: false,
            nextWeapon: true);

        Assert.That(command.Jump, Is.True);
        Assert.That(command.Fire, Is.False);
        Assert.That(command.Interact, Is.True);
        Assert.That(command.Heal, Is.False);
        Assert.That(command.Ability, Is.True);
        Assert.That(command.PreviousWeapon, Is.False);
        Assert.That(command.NextWeapon, Is.True);
    }

    [Test]
    public void WeaponSlotIsOptional()
    {
        var empty = new PlayerCommand(0f, 0f, 0f, 0f);
        var selected = new PlayerCommand(0f, 0f, 0f, 0f, weaponSlot: 3);

        Assert.That(empty.WeaponSlot, Is.Null);
        Assert.That(selected.WeaponSlot, Is.EqualTo(3));
    }
}
