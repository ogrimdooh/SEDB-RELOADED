using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Extensions;
using System;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
    [PatchingClass]
    public class MyDamageSystemPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public MyDamageSystemPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        private static bool NeedToNullDamage(long attackerPlayerId, bool isAttackerPlayer, long ownerId, MyDamageInformationExtensions.DamageType damageType)
        {
            if (attackerPlayerId != 0)
            {
                var ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                var attackerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(attackerPlayerId);
                if (ownerId == attackerPlayerId || ownerFaction?.FactionId == attackerFaction?.FactionId)
                {
                    if (damageType != MyDamageInformationExtensions.DamageType.Tool)
                    {
                        return true;
                    }
                }
                else
                {
                    if (isAttackerPlayer)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (damageType != MyDamageInformationExtensions.DamageType.Fall)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckGridCanBeDamage(IMySlimBlock cubeBlock, long ownerId, long attackerPlayerId,
            MyDamageInformationExtensions.DamageType damageType, MyDamageInformationExtensions.AttackerType attackerType,
            VRage.ModAPI.IMyEntity attackerEntity, ref MyDamageInformation damage)
        {
            try
            {
                // NoSelfOwnerDamage
                if (damageType != MyDamageInformationExtensions.DamageType.Tool &&
                    ownerId == attackerPlayerId)
                {
                    damage.Amount = 0;
                    damage.IsDeformation = false;
                }
                else
                {
                    // NoGrindFunctionalGrids and NoGridSelfDamage
                    var gridInfo = GridObserverController.GetGridExtraData(cubeBlock.CubeGrid.EntityId);
                    if (gridInfo != null)
                    {
                        if ((gridInfo.Grid as IMyCubeGrid).ResourceDistributor.ResourceState != VRage.MyResourceStateEnum.NoPower)
                        {
                            if (gridInfo.AnyWeaponIsFunctional)
                            {
                                if (damage.AttackerId != 0)
                                {
                                    if (MyAPIGateway.Players.TryGetSteamId(attackerPlayerId) != 0)
                                    {
                                        // NoGrindFunctionalGrids
                                        if (damageType == MyDamageInformationExtensions.DamageType.Tool)
                                        {
                                            var ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                                            var attackerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(attackerPlayerId);
                                            if (ownerId != attackerPlayerId && ownerFaction?.FactionId != attackerFaction?.FactionId)
                                            {
                                                if (cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_Door) &&
                                                    cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_AirtightSlideDoor) &&
                                                    cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_AirtightHangarDoor))
                                                {
                                                    damage.Amount = 0;
                                                    damage.IsDeformation = false;
                                                }
                                            }
                                        }
                                    }
                                    // NoGridSelfDamage
                                    if (damage.Amount > 0 &&
                                        attackerType == MyDamageInformationExtensions.AttackerType.CubeBlock)
                                    {
                                        var attackerBlock = attackerEntity as IMyCubeBlock;
                                        if (attackerBlock != null && attackerBlock.CubeGrid.EntityId == cubeBlock.CubeGrid.EntityId)
                                        {
                                            damage.Amount = 0;
                                            damage.IsDeformation = false;
                                        }
                                    }
                                }
                                else
                                {
                                    // NoGrindFunctionalGrids
                                    /* During a battle a many damages info is lack of attacker id, so better avoid tool damage */
                                    if (damageType == MyDamageInformationExtensions.DamageType.Tool)
                                    {
                                        if (cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_Door) &&
                                            cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_AirtightSlideDoor) &&
                                            cubeBlock.BlockDefinition.Id.TypeId != typeof(MyObjectBuilder_AirtightHangarDoor))
                                        {
                                            damage.Amount = 0;
                                            damage.IsDeformation = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(MyDamageSystemPatch), ex);
            }
        }

        [PrefixMethod]
        [TargetMethod(Type = typeof(MyDamageSystem), Method = "RaiseBeforeDamageApplied")]
        public static void Trigger(object target, ref MyDamageInformation info)
        {
            try
            {
                if (target != null && info.Amount > 0 && !info.IsDeformation())
                {
                    var cubeBlock = target as IMySlimBlock;
                    if (cubeBlock != null)
                    {
                        var ownerId = cubeBlock.OwnerId != 0 ? cubeBlock.OwnerId : cubeBlock.CubeGrid.BigOwners.FirstOrDefault();
                        var isPlayer = MyAPIGateway.Players.TryGetSteamId(ownerId) != 0;
                        long attackerPlayerId = 0;
                        MyDamageInformationExtensions.DamageType damageType;
                        MyDamageInformationExtensions.AttackerType attackerType = MyDamageInformationExtensions.AttackerType.None;
                        VRage.ModAPI.IMyEntity attackerEntity = null;
                        if (info.AttackerId != 0)
                            attackerEntity = info.GetAttacker(out attackerPlayerId, out damageType, out attackerType);
                        else
                            damageType = MyDamageInformationExtensions.GetDamageType(info.Type);
                        var isAttackerPlayer = MyAPIGateway.Players.TryGetSteamId(attackerPlayerId) != 0;
                        if (info.Amount > 0)
                        {
                            CheckGridCanBeDamage(cubeBlock, ownerId, attackerPlayerId, damageType, attackerType, attackerEntity, ref info);
                        }
                        if (info.Amount > 0 && ArkGroundBaseController.Instance?.ARKGRID != null)
                        {
                            if (isPlayer)
                            {
                                /* Player GRID */
                                var pos = cubeBlock.CubeGrid.GetPosition();
                                float naturalGravityInterference;
                                Vector3 naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(pos, out naturalGravityInterference);
                                if (naturalGravityInterference > 0)
                                {
                                    var distance = Vector3.Distance(pos, ArkGroundBaseController.Instance.ARKGRID.PositionComp.GetPosition());
                                    if (distance < 40000)
                                    {
                                        if (info.AttackerId != 0)
                                        {
                                            if (NeedToNullDamage(attackerPlayerId, isAttackerPlayer, ownerId, damageType))
                                            {
                                                info.Amount = 0;
                                                info.IsDeformation = false;
                                            }
                                        }
                                        else
                                        {
                                            info.Amount = 0;
                                            info.IsDeformation = false;
                                        }
                                    }
                                }
                            }
                        }
                        Logging.Instance.LogInfo(typeof(MyDamageSystemPatch), $"DamageSystem: Damage info: OwnerId={ownerId}, AttackerId={attackerPlayerId}, DamageType={damageType}, AttackerType={attackerType}, Amount={info.Amount}, IsDeformation={info.IsDeformation}");
                        /*
                        if (ExtendedSurvivalSettings.Instance.Combat.LogAllPvPDamage && isPlayer && isAttackerPlayer &&
                            attackerPlayerId != ownerId && info.Amount > 0 && ExtendedSurvivalEntityManager.Instance != null)
                        {
                            ExtendedSurvivalEntityManager.Instance.DamageToLog.Enqueue(new ExtendedSurvivalCoreDamageLogging.DamageToLogInfo()
                            {
                                attackerId = attackerPlayerId,
                                ownerId = ownerId,
                                damageType = damageType,
                                gridName = cubeBlock.CubeGrid.CustomName,
                                position = cubeBlock.CubeGrid.GetPosition(),
                                amount = info.Amount,
                                time = DateTime.Now
                            });
                        }*/
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(MyDamageSystemPatch), ex);
            }
        }

    }

}
