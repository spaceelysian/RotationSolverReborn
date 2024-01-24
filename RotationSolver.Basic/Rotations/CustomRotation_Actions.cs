﻿using ECommons.ExcelServices;
using RotationSolver.Basic.Traits;

namespace RotationSolver.Basic.Rotations;

partial class CustomRotation
{
    public static void LoadActionConfigAndSetting(ref IBaseAction action)
    {
        //TODO: better target type check. (NoNeed?)
        //TODO: better friendly check.
        //TODO: load the config from the configuration.
    }

    #region Role Actions
    static partial void ModifyAddlePvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Addle];
        setting.TargetStatusFromSelf = false;
    }

    static partial void ModifySwiftcastPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.SwiftcastStatus;
    }

    static partial void ModifyEsunaPvE(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifyLucidDreamingPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.CurrentMp < 6000 && InCombat;
    }

    static partial void ModifySecondWindPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.GetHealthRatio() < Service.Config.GetValue(Configuration.JobConfigFloat.HealthSingleAbility) && InCombat;
    }

    static partial void ModifyRampartPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.RampartStatus;
    }

    static partial void ModifyBloodbathPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player?.GetHealthRatio() < Service.Config.GetValue(Configuration.JobConfigFloat.HealthSingleAbility) && InCombat && HasHostilesInRange;
    }

    static partial void ModifyFeintPvE(ref ActionSetting setting)
    {
        setting.TargetStatusFromSelf = false;
        setting.TargetStatusProvide = [StatusID.Feint];
    }

    static partial void ModifyLowBlowPvE(ref ActionSetting setting)
    {
        setting.CanTarget = o =>
        {
            if (o is not BattleChara b) return false;

            if (b.IsBossFromIcon() || IsMoving || b.CastActionId == 0) return false;

            if (!b.IsCastInterruptible || ActionID.InterjectPvE.IsCoolingDown()) return true;
            return false;
        };
    }

    static partial void ModifyPelotonPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () =>
        {
            if (!NotInCombatDelay) return false;
            var players = PartyMembers.GetObjectInRadius(20);
            if (players.Any(ObjectHelper.InCombat)) return false;
            return players.Any(p => p.WillStatusEnd(3, false, StatusID.Peloton));
        };
    }

    static partial void ModifyIsleSprintPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Dualcast];
    }
    #endregion

    #region PvP

    static partial void ModifyStandardissueElixirPvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !HasHostilesInMaxRange
            && (Player.CurrentMp <= Player.MaxMp / 3 || Player.CurrentHp <= Player.MaxHp / 3)
            && !IsLastAction(ActionID.StandardissueElixirPvP);
    }

    static partial void ModifyRecuperatePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => Player.MaxHp - Player.CurrentHp > 15000;
    }

    static partial void ModifyPurifyPvP(ref ActionSetting setting)
    {
        setting.TargetType = TargetType.Dispel;
    }

    static partial void ModifySprintPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Sprint_1342];
    }

    #endregion
    private protected virtual IBaseAction? Raise => null;
    private protected virtual IBaseAction? TankStance => null;

    IBaseAction[] _allBaseActions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public virtual IBaseAction[] AllBaseActions => _allBaseActions ??= GetBaseActions(GetType()).ToArray();

    IAction[] _allActions;
    public virtual IAction[] AllActions => _allActions ??= Array.Empty<IAction>().Union(GetBaseItems(GetType())).Union(AllBaseActions).ToArray();

    IBaseTrait[] _allTraits;
    public virtual IBaseTrait[] AllTraits => _allTraits ??= GetIEnoughLevel<IBaseTrait>(GetType()).ToArray();

    PropertyInfo[] _allBools;
    public PropertyInfo[] AllBools => _allBools ??= GetType().GetStaticProperties<bool>();

    PropertyInfo[] _allBytes;
    public PropertyInfo[] AllBytesOrInt => _allBytes ??= GetType().GetStaticProperties<byte>().Union(GetType().GetStaticProperties<int>()).ToArray();

    PropertyInfo[] _allFloats;
    public PropertyInfo[] AllFloats => _allFloats ??= GetType().GetStaticProperties<float>();

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private IEnumerable<IBaseAction> GetBaseActions(Type type)
    {
        return GetIEnoughLevel<IBaseAction>(type);
    }

    private IEnumerable<IBaseItem> GetBaseItems(Type type)
    {
        return GetIEnoughLevel<IBaseItem>(type).Where(a => a is not MedicineItem medicine || medicine.InType(this)).Reverse();
    }

    private IEnumerable<T> GetIEnoughLevel<T>(Type? type) where T : IEnoughLevel
    {
        if (type == null) return Array.Empty<T>();

        var acts = from prop in type.GetProperties()
                   where typeof(T).IsAssignableFrom(prop.PropertyType) && !(prop.GetMethod?.IsPrivate ?? true)
                   select (T)prop.GetValue(this)! into act
                   where act != null
                   orderby act.Level
                   select act;

        return acts.Union(GetIEnoughLevel<T>(type.BaseType));
    }
}
