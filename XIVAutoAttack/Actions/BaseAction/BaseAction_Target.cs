﻿using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVAutoAttack.Data;
using XIVAutoAttack.Helpers;
using XIVAutoAttack.Updaters;

namespace XIVAutoAttack.Actions.BaseAction
{
    internal partial class BaseAction
    {
        internal bool IsTargetDying
        {
            get
            {
                if (Target == null) return false;
                return Target.IsDying();
            }
        }

        internal bool IsTargetBoss
        {
            get
            {
                if (Target == null) return false;
                return Target.IsBoss();
            }
        }

        internal BattleChara Target { get; set; } = Service.ClientState.LocalPlayer;
        private Vector3 _position = default;

        private Func<IEnumerable<BattleChara>, BattleChara> _choiceTarget = null;
        internal Func<IEnumerable<BattleChara>, BattleChara> ChoiceTarget
        {
            private get
            {
                if (_choiceTarget != null) return _choiceTarget;
                return _isFriendly ? TargetFilter.DefaultChooseFriend : TargetFilter.DefaultFindHostile;
            }
            set => _choiceTarget = value;
        }

        internal Func<IEnumerable<BattleChara>, IEnumerable<BattleChara>> FilterForTarget { private get; set; } = null;

        private IEnumerable<BattleChara> TargetFilterFunc(IEnumerable<BattleChara> tars, bool mustUse)
        {
            if (FilterForTarget != null) return FilterForTarget(tars);
            if (TargetStatus == null ||　!_isEot) return tars;

            var canDot = TargetFilter.GetTargetCanDot(tars);
            var DontHave = canDot.Where(t => t.WillStatusEndGCD((uint)Service.Configuration.AddDotGCDCount, 0, true, TargetStatus));

            if (mustUse)
            {
                if (DontHave.Any()) return DontHave;
                if (canDot.Any()) return canDot;
                return tars;
            }
            else
            {
                return DontHave;
            }
        }

        /// <summary>
        /// 给敌人造成的Debuff,如果有这些Debuff，那么不会执行，这个status是玩家赋予的。
        /// </summary>
        internal StatusID[] TargetStatus { get; set; } = null;

        internal static bool TankDefenseSelf(BattleChara chara)
        {
            return TargetUpdater.TarOnMeTargets.Any();
        }
        internal static bool TankBreakOtherCheck(ClassJobID id, BattleChara chara)
        {
            var tankHealth = Service.Configuration.HealthForDyingTanks.TryGetValue(id, out var value) ? value : 0.15f;

            return TargetUpdater.HaveHostilesInRange
                && Service.ClientState.LocalPlayer.GetHealthRatio() < tankHealth
                && TargetUpdater.PartyMembersAverHP > tankHealth + 0.1f;
        }

        private bool FindTarget(bool mustUse)
        {
            int aoeCount = mustUse ? 1 : AOECount;

            _position = Service.ClientState.LocalPlayer.Position;
            var player = Service.ClientState.LocalPlayer;

            float range = Range;

            //如果都没有距离，这个还需要选对象嘛？选自己啊！
            if (range == 0 && _action.EffectRange == 0)
            {
                Target = player;
                return true;
            }

            if (_action.TargetArea)
            {
                //移动
                if (_action.EffectRange == 1 && range >= 15)
                {
                    var availableCharas = Service.ObjectTable.Where(b => b.ObjectId != Service.ClientState.LocalPlayer.ObjectId && b is BattleChara)
                        .Select(b => (BattleChara)b);

                    Target = TargetFilter.FindTargetForMoving(TargetFilter.GetObjectInRadius(availableCharas, 20));
                    _position = Target.Position;
                }
                //其他友方
                else if (_isFriendly)
                {
                    //如果用户不想使用自动友方地面放置功能
                    if (!Service.Configuration.UseAreaAbilityFriendly) return false;

                    //如果当前目标是Boss且有身位，放他身上。
                    if (Service.TargetManager.Target is BattleChara b && b.IsBoss() && b.HasLocationSide())
                    {
                        Target = b;
                        _position = Target.Position;
                    }
                    //计算玩家和被打的Ｔ之间的关系。
                    else
                    {
                        var attackT = TargetFilter.FindAttackedTarget(TargetFilter.GetObjectInRadius(TargetUpdater.PartyTanks,
                            range + _action.EffectRange));

                        Target = Service.ClientState.LocalPlayer;

                        if (attackT == null)
                        {
                            _position = Target.Position;
                        }
                        else
                        {
                            var disToTankRound = Vector3.Distance(Target.Position, attackT.Position) + attackT.HitboxRadius;

                            if (disToTankRound < _action.EffectRange
                                || disToTankRound > 2 * _action.EffectRange - Target.HitboxRadius
                                || disToTankRound > range)
                            {
                                _position = Target.Position;
                            }
                            else
                            {
                                Vector3 directionToTank = attackT.Position - Target.Position;
                                var MoveDirection = directionToTank / directionToTank.Length() * (disToTankRound - _action.EffectRange);
                                _position = Target.Position + MoveDirection;
                            }
                        }
                    }
                }
                //敌方
                else
                {
                    Target = TargetFilter.GetMostObjectInRadius(TargetUpdater.HostileTargets, range, _action.EffectRange, true, aoeCount)
                        .OrderByDescending(p => p.GetHealthRatio()).FirstOrDefault();
                    if (Target == null)
                    {
                        Target = Service.ClientState.LocalPlayer;
                        return false;
                    }
                    _position = Target.Position;
                }
                return true;
            }
            //如果能对友方和敌方都能选中
            else if (_action.CanTargetParty && _action.CanTargetHostile)
            {
                var availableCharas = TargetUpdater.PartyMembers.Union(TargetUpdater.HostileTargets).Where(b => b.ObjectId != Service.ClientState.LocalPlayer.ObjectId);
                availableCharas = TargetFilter.GetObjectInRadius(availableCharas, range);

                //特殊选队友的方法。
                Target = ChoiceTarget(availableCharas);
                if (Target == null) return false;
                return true;

            }
            //首先看看是不是能对小队成员进行操作的。
            else if (_action.CanTargetParty)
            {
                //还消耗2400的蓝，那肯定是复活的。
                if (_action.PrimaryCostType == 3 && _action.PrimaryCostValue == 24 || (ActionID)ID == ActionID.AngelWhisper)
                {
                    Target = TargetFilter.GetDeathPeople(TargetUpdater.DeathPeopleAll, TargetUpdater.DeathPeopleParty);
                    if (Target == null) return false;
                    return true;
                }

                //找到没死的队友们。
                var availableCharas = TargetUpdater.PartyMembers.Where(player => player.CurrentHp != 0);

                if ((ActionID)ID == ActionID.AetherialMimicry)
                {
                    availableCharas = availableCharas.Union(TargetUpdater.AllianceMembers);
                }
                if (!_action.CanTargetSelf)
                {
                    availableCharas = availableCharas.Where(p => p.ObjectId != Service.ClientState.LocalPlayer.ObjectId);
                }
                if (!availableCharas.Any()) return false;

                //判断是否是范围。
                if (_action.CastType > 1 && (ActionID)ID != ActionID.DeploymentTactics)
                {
                    //找到能覆盖最多的位置，并且选血最少的来。
                    Target = TargetFilter.GetMostObjectInRadius(availableCharas, range, _action.EffectRange, true, aoeCount)
                        .OrderBy(p => p.GetHealthRatio()).FirstOrDefault();
                    if (Target == null) return false;

                    return true;
                }
                else
                {

                    availableCharas = TargetFilter.GetObjectInRadius(availableCharas, range);
                    //特殊选队友的方法。
                    Target = ChoiceTarget(availableCharas);
                    if (Target == null) return false;
                    return true;
                }
            }
            //再看看是否可以选中敌对的。
            else if (_action.CanTargetHostile)
            {
                //如果不用自动找目标，那就直接返回。
                if (!CommandController.AutoTarget)
                {
                    if (Service.TargetManager.Target is BattleChara b && b.CanAttack() && b.DistanceToPlayer() <= range)
                    {
                        if (_action.CastType == 1)
                        {
                            //目标已有充足的Debuff
                            if (!mustUse && TargetStatus != null)
                            {
                                var tar = Target ?? Service.ClientState.LocalPlayer;

                                if (!tar.WillStatusEndGCD((uint)Service.Configuration.AddDotGCDCount, 0, true, TargetStatus)) return false;
                            }

                            Target = b;
                            return true;
                        }
                        else if (Service.Configuration.AttackSafeMode)
                        {
                            return false;
                        }

                        if (Service.Configuration.UseAOEWhenManual || mustUse)
                        {
                            switch (_action.CastType)
                            {
                                case 10: //环形范围攻击也就这么判断吧，我烦了。
                                case 2: // 圆形范围攻击。找到能覆盖最多的位置，并且选血最多的来。
                                    if (TargetFilter.GetMostObjectInRadius(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), range, _action.EffectRange, false, aoeCount)
                                        .Contains(b))
                                    {
                                        Target = b;
                                        return true;
                                    }
                                    break;
                                case 3: // 扇形范围攻击。找到能覆盖最多的位置，并且选最远的来。
                                    if (TargetFilter.GetMostObjectInArc(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), _action.EffectRange, false, aoeCount)
                                        .Contains(b))
                                    {
                                        Target = b;
                                        return true;
                                    }
                                    break;
                                case 4: //直线范围攻击。找到能覆盖最多的位置，并且选最远的来。
                                    if (TargetFilter.GetMostObjectInLine(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), range, false, aoeCount)
                                        .Contains(b))
                                    {
                                        Target = b;
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }

                    Target = null;
                    return false;
                }

                //判断一下AOE攻击的时候如果有攻击目标标记目标
                if (_action.CastType > 1 && (NoAOEForAttackMark || Service.Configuration.AttackSafeMode))
                {
                    return false;
                }

                switch (_action.CastType)
                {
                    case 1:
                    default:
                        var canReachTars = TargetFilterFunc(TargetFilter.GetObjectInRadius(TargetUpdater.HostileTargets, range), mustUse);

                        Target = ChoiceTarget(canReachTars);
                        if (Target == null) return false;
                        return true;
                    case 10: //环形范围攻击也就这么判断吧，我烦了。
                    case 2: // 圆形范围攻击。找到能覆盖最多的位置，并且选血最多的来。
                        Target = ChoiceTarget(TargetFilter.GetMostObjectInRadius(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), range, _action.EffectRange, true, aoeCount));
                        if (Target == null) return false;
                        return true;
                    case 3: // 扇形范围攻击。找到能覆盖最多的位置，并且选最远的来。
                        Target = ChoiceTarget(TargetFilter.GetMostObjectInArc(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), _action.EffectRange, true, aoeCount));
                        if (Target == null) return false;
                        return true;
                    case 4: //直线范围攻击。找到能覆盖最多的位置，并且选最远的来。
                        Target = ChoiceTarget(TargetFilter.GetMostObjectInLine(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), range, true, aoeCount));
                        if (Target == null) return false;
                        return true;
                }
            }
            //如果只能选自己，那就选自己吧。
            else if (_action.CanTargetSelf)
            {
                Target = player;

                if (_action.EffectRange > 0 && !_isFriendly)
                {
                    if (NoAOEForAttackMark || Service.Configuration.AttackSafeMode)
                    {
                        return false;
                    }

                    //如果不用自动找目标，那就不打AOE
                    if (!CommandController.AutoTarget)
                    {
                        if (!Service.Configuration.UseAOEWhenManual && !mustUse)
                            return false;
                    }
                    var count = TargetFilter.GetObjectInRadius(TargetFilterFunc(TargetUpdater.HostileTargets, mustUse), _action.EffectRange).Count();
                    if (count < aoeCount) return false;
                }
                return true;
            }

            Target = Service.TargetManager.Target is BattleChara battle ? battle : Service.ClientState.LocalPlayer;
            return true;
        }
        /// <summary>
        /// 开启攻击标记且有攻击标记目标且不开AOE。
        /// </summary>
        private static bool NoAOEForAttackMark =>
            Service.Configuration.ChooseAttackMark && !Service.Configuration.AttackMarkAOE
            && MarkingController.HaveAttackChara(TargetUpdater.HostileTargets);
    }
}
