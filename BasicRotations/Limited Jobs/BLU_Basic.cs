namespace DefaultRotations.Magical;

[Rotation("Basic BLU", CombatType.PvE, GameVersion = "7.11")]
[SourceCode(Path = "main/BasicRotations/Limited Jobs/BLU_Basic.cs")]
[Api(4)]
public sealed class Blue_Basic : BlueMageRotation
{

    [RotationConfig(CombatType.PvE, Name = "Single Target Spell")]
    public BluDPSSpell SingleTargetDPSSpell { get; set; } = BluDPSSpell.SonicBoom;

    [RotationConfig(CombatType.PvE, Name = "AoE Spell")]
    public BluAOESpell AoeSpell { get; set; } = BluAOESpell.MindBlast;

    [RotationConfig(CombatType.PvE, Name = "Healing Spell")]
    public BluHealSpell HealSpell { get; set; } = BluHealSpell.WhiteWind;

    [RotationConfig(CombatType.PvE, Name = "Use Basic Instinct")]
    public bool UseBasicInstinct { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Mighty Guard")]
    public bool UseMightyGuard { get; set; } = true;

    #region Countdown logic
    // Defines logic for actions to take during the countdown before combat starts.
    protected override IAction? CountDownAction(float remainTime)
    {

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region Emergency Logic
    // Determines emergency actions to take based on the next planned GCD action.
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        return base.EmergencyAbility(nextGCD, out act);
    }
    #endregion

    #region Move oGCD Logic
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.MoveForwardAbility(nextGCD, out act);
    }

    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.MoveBackAbility(nextGCD, out act);
    }

    protected override bool SpeedAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.SpeedAbility(nextGCD, out act);
    }
    #endregion

    #region Heal/Defense oGCD Logic
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.HealSingleAbility(nextGCD, out act);
    }

    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.DefenseAreaAbility(nextGCD, out act);
    }

    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;


        return base.DefenseSingleAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (Player.CurrentMp < 6000 && InCombat && LucidDreamingPvE.CanUse(out act)) return true;
        //if (AethericMimicryPvE_19239.CanUse(out act)) return true;
        return base.GeneralAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool EmergencyGCD(out IAction? act)
    {
        return base.EmergencyGCD(out act);
    }

    protected override bool MyInterruptGCD(out IAction? act)
    {
        if (FlyingSardinePvE.CanUse(out act)) return true;
        return base.MyInterruptGCD(out act);
    }

    protected override bool DefenseAreaGCD(out IAction? act)
    {
        //if (ColdFogPvE.CanUse(out act)) return true;
        return base.DefenseAreaGCD(out act);
    }

    protected override bool DefenseSingleGCD(out IAction? act)
    {
        return base.DefenseSingleGCD(out act);
    }

    protected override bool HealAreaGCD(out IAction? act)
    {
        if (BluHealSpellActions[HealSpell].CanUse(out act)) return true;
        return base.HealAreaGCD(out act);
    }

    protected override bool HealSingleGCD(out IAction? act)
    {
        if (BluHealSpellActions[HealSpell].CanUse(out act)) return true;
        return base.HealSingleGCD(out act);
    }

    protected override bool MoveForwardGCD(out IAction? act)
    {
        return base.MoveForwardGCD(out act);
    }

    protected override bool DispelGCD(out IAction? act)
    {
        return base.DispelGCD(out act);
    }

    protected override bool RaiseGCD(out IAction? act)
    {
        //if (AngelWhisperPvE.CanUse(out act)) return true;
        return base.RaiseGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (UseMightyGuard && MightyGuardPvE.CanUse(out act)) return true;
        if (UseBasicInstinct && BasicInstinctPvE.CanUse(out act)) return true;
        if (BluAOESpellActions[AoeSpell].CanUse(out act)) return true;
        if (BluDPSSpellActions[SingleTargetDPSSpell].CanUse(out act)) return true;
        if (FlyingSardinePvE.CanUse(out act)) return true;
        return base.GeneralGCD(out act);
    }
    #endregion
}