using EmptyKeys.UserInterface.Generated.EditFactionIconView_Bindings;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Registry;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Weapons;
using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SEDiscordBridge.Controllers.Grids
{
    public class MyCubeGridExtraData
    {

        public long EntityId { get { return _grid.EntityId; } }

        private float _threatLevel = 0;
        public float ThreatLevel
        {
            get
            {
                if (ThreatLevelIsDirty)
                    DoCalcThreatLevel();
                return _threatLevel;
            }
        }
        public bool ThreatLevelIsDirty { get; private set; }

        public IEnumerable<MyCubeBlock> Antennas
        {
            get
            {
                return _grid.GetFatBlocks<MyRadioAntenna>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyLaserAntenna>().Cast<MyCubeBlock>()
                );
            }
        }

        public IEnumerable<MyCubeBlock> Beacons
        {
            get
            {
                return _grid.GetFatBlocks<MyBeacon>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Containers
        {
            get
            {
                return _grid.GetFatBlocks<MyCargoContainer>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Controllers
        {
            get
            {
                return _grid.GetFatBlocks<MyCockpit>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyRemoteControl>().Cast<MyCubeBlock>()
                );
            }
        }

        public IEnumerable<MyCubeBlock> Gravity
        {
            get
            {
                return _grid.GetFatBlocks<MyGravityGenerator>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Guns
        {
            get
            {
                return _grid.GetFatBlocks<MySmallGatlingGun>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MySmallMissileLauncher>().Cast<MyCubeBlock>().Concat(
                        _grid.GetFatBlocks<MySmallMissileLauncherReload>().Cast<MyCubeBlock>()
                    )
                );
            }
        }

        public IEnumerable<MyCubeBlock> Gyros
        {
            get
            {
                return _grid.GetFatBlocks<MyGyro>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> JumpDrives
        {
            get
            {
                return _grid.GetFatBlocks<MyJumpDrive>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Medical
        {
            get
            {
                return _grid.GetFatBlocks<MyMedicalRoom>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MySurvivalKit>().Cast<MyCubeBlock>()
                );
            }
        }

        public IEnumerable<MyCubeBlock> Parachutes
        {
            get
            {
                return _grid.GetFatBlocks<MyParachute>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Production
        {
            get
            {
                return _grid.GetFatBlocks<MyAssembler>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyGasGenerator>().Cast<MyCubeBlock>()
                );
            }
        }

        public IEnumerable<MyCubeBlock> Projectors
        {
            get
            {
                return _grid.GetFatBlocks<MyProjectorBase>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Power
        {
            get
            {
                return _grid.GetFatBlocks<MyReactor>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyBatteryBlock>().Cast<MyCubeBlock>().Concat(
                        _grid.GetFatBlocks<MyHydrogenEngine>().Cast<MyCubeBlock>().Concat(
                            _grid.GetFatBlocks<MySolarPanel>().Cast<MyCubeBlock>().Concat(
                                _grid.GetFatBlocks<MyWindTurbine>().Cast<MyCubeBlock>()
                            )
                        )
                    )
                );
            }
        }

        public IEnumerable<MyCubeBlock> Seats
        {
            get
            {
                return _grid.GetFatBlocks<MyCockpit>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Stores
        {
            get
            {
                return _grid.GetFatBlocks<MyVendingMachine>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Thrusters
        {
            get
            {
                return _grid.GetFatBlocks<MyThrust>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Tools
        {
            get
            {
                return _grid.GetFatBlocks<MyShipDrill>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyShipWelder>().Cast<MyCubeBlock>().Concat(
                        _grid.GetFatBlocks<MyShipGrinder>().Cast<MyCubeBlock>()
                    )
                );
            }
        }

        public IEnumerable<MyCubeBlock> Turrets
        {
            get
            {
                return _grid.GetFatBlocks<MyLargeGatlingTurret>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyLargeMissileTurret>().Cast<MyCubeBlock>().Concat(
                        _grid.GetFatBlocks<MyLargeInteriorTurret>().Cast<MyCubeBlock>()
                    )
                );
            }
        }

        public IEnumerable<MyCubeBlock> TurretControllers
        {
            get
            {
                return _grid.GetFatBlocks<MyTurretControlBlock>().Cast<MyCubeBlock>();
            }
        }

        public IEnumerable<MyCubeBlock> Mechanical
        {
            get
            {
                return _grid.GetFatBlocks<MyMotorAdvancedRotor>().Cast<MyCubeBlock>().Concat(
                    _grid.GetFatBlocks<MyMotorRotor>().Cast<MyCubeBlock>().Concat(
                        _grid.GetFatBlocks<MyPistonBase>().Cast<MyCubeBlock>().Concat(
                            _grid.GetFatBlocks<MyMotorAdvancedStator>().Cast<MyCubeBlock>()
                        )
                    )
                );
            }
        }

        public ulong OwnerSteamId
        {
            get
            {
                var ownerId = _grid.BigOwners.FirstOrDefault();
                if (ownerId == 0)
                    return 0;
                if (MySession.Static.Players.TryGetPlayerId(ownerId, out var playerId))
                    return playerId.SteamId;
                return 0;
            }
        }

        public bool IsFromPlayer
        {
            get
            {
                return OwnerSteamId != 0;
            }
        }

        public bool IsOwnerRegistred
        {
            get
            {
                return RegistryStorage.Instance.IsSteamUserRegistered(OwnerSteamId);
            }
        }

        public bool HasAnyGun
        {
            get
            {
                return Guns.Any();
            }
        }

        public bool HasAnyTurret
        {
            get
            {
                return Turrets.Any();
            }
        }

        public bool HasWeapon
        {
            get
            {
                return HasAnyGun || HasAnyTurret;
            }
        }

        public bool AnyWeaponIsFunctional
        {
            get
            {
                return HasWeapon && AllWeapons.Any(x => x.IsFunctional);
            }
        }

        public IEnumerable<MyCubeBlock> AllWeapons
        {
            get
            {
                return Guns.Concat(Turrets);
            }
        }

        private MyCubeGrid _grid;
        public MyCubeGrid Grid
        {
            get
            {
                return _grid;
            }
        }

        public MyCubeGridExtraData(MyCubeGrid grid)
        {
            _grid = grid;
            _grid.OnFatBlockAdded += _grid_OnFatBlockAdded;
            _grid.OnFatBlockRemoved += _grid_OnFatBlockRemoved;
            DoCalcThreatLevel();
        }

        public Vector2 GridPowerOutput()
        {

            var result = Vector2.Zero;

            if (_grid.Closed)
                return result;

            foreach (var block in _grid.GetFatBlocks())
            {

                if (block == null || block.Closed || !block.IsWorking || !block.IsFunctional)
                    continue;

                var powerBlock = block as IMyPowerProducer;

                if (powerBlock == null)
                    continue;

                result.X += powerBlock.CurrentOutput;
                result.Y += powerBlock.MaxOutput;

            }

            return result;

        }

        private static ConcurrentDictionary<Tuple<MyDefinitionId, MyDefinitionId, float>, float> AMMO_DPS = new ConcurrentDictionary<Tuple<MyDefinitionId, MyDefinitionId, float>, float>();
        private static float GetAmmoDps(MyWeaponDefinition WeaponInfo, MyAmmoDefinition ammoInfo, float maxRangeMeters)
        {
            var key = new Tuple<MyDefinitionId, MyDefinitionId, float>(WeaponInfo.Id, ammoInfo.Id, maxRangeMeters);
            if (AMMO_DPS.TryGetValue(key, out var result))
                return result;
            result = 0f;
            int WrateOfFire = 0;
            int WshotsInBurst = 0;
            if (WeaponInfo.WeaponAmmoDatas.Length >= 2)
            {
                if (WeaponInfo.WeaponAmmoDatas[0] != null)
                {
                    WrateOfFire = WeaponInfo.WeaponAmmoDatas[0].RateOfFire;
                    WshotsInBurst = WeaponInfo.WeaponAmmoDatas[0].ShotsInBurst;
                }
                if (WeaponInfo.WeaponAmmoDatas[1] != null)
                {
                    WrateOfFire = WeaponInfo.WeaponAmmoDatas[1].RateOfFire;
                    WshotsInBurst = WeaponInfo.WeaponAmmoDatas[1].ShotsInBurst;
                }
            }
            if (ammoInfo is MyMissileAmmoDefinition missile)
            {
                var rateOfFire = Math.Max(Math.Min(WrateOfFire, 500) * 10, Math.Max(WeaponInfo.ReloadTime, Math.Max(WeaponInfo.ShotDelay, WeaponInfo.ReleaseTimeAfterFire)));
                result =((missile.MissileInitialSpeed + missile.DesiredSpeed) / 2) *
                    (missile.MaxTrajectory * WeaponInfo.RangeMultiplier / 1000f) *
                    missile.MissileMass *
                    (1 + (Math.Max(missile.MissileExplosionRadius, 1) * Math.Max(missile.MissileExplosionDamage, 1) * missile.ExplosiveDamageMultiplier / 1000)) /
                    (1 + WeaponInfo.DeviateShotAngle) *
                    WeaponInfo.DamageMultiplier /
                    (rateOfFire / 1000f) *
                    (1 + (WshotsInBurst / 10)) *
                    (1 + (missile.MissileRicochetDamage / 10000)) *
                    (1 + (maxRangeMeters / 100000));
            }
            else if (ammoInfo is MyProjectileAmmoDefinition info)
            {
                var delayFire = Math.Max(Math.Max(WeaponInfo.ReloadTime, Math.Max(WeaponInfo.ShotDelay, WeaponInfo.ReleaseTimeAfterFire * 10)), 1);
                result = (((float)WrateOfFire) / 1000f) *
                    (info.DesiredSpeed / 100f) *
                    (info.MaxTrajectory * WeaponInfo.RangeMultiplier / 1000f) *
                    info.ProjectileMassDamage /
                    (1 + WeaponInfo.DeviateShotAngle) *
                    WeaponInfo.DamageMultiplier /
                    (1 + (delayFire / 10000f)) *
                    (1 + (WshotsInBurst / 1000)) *
                    (1 + (maxRangeMeters / 100000));
            }
            AMMO_DPS[key] = result;
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"W={WeaponInfo.Id} A={ammoInfo.Id} R={maxRangeMeters} DPS : {result}");
            return result;
        }

        public static float GetTargetValueFromBlockList(IEnumerable<MyCubeBlock> blockList, float threatValue, bool scanDamage = false, bool scanInventory = false)
        {

            float result = 0;

            foreach (var block in blockList)
            {

                if (block == null || block.Closed || !block.IsFunctional)
                    continue;

                var value = threatValue;

                if (scanInventory)
                {

                    if (block.HasInventory && block.GetInventory().MaxVolume > 0)
                    {

                        var inventoryModifier = ((float)block.GetInventory().CurrentVolume / (float)block.GetInventory().MaxVolume) + 1;

                        if (inventoryModifier != float.NaN)
                            value *= inventoryModifier;

                    }

                }

                if (scanDamage)
                {
                    if (block is IMyGunObject<MyGunBase> gunBlock)
                    {
                        var maxRange = 0f;
                        if (block is MyLargeTurretBase turret)
                        {
                            maxRange = turret.BlockDefinition.MaxRangeMeters;
                        }
                        float dps = GetAmmoDps(
                            gunBlock.GunBase.WeaponProperties.WeaponDefinition,
                            gunBlock.GunBase.WeaponProperties.AmmoDefinition,
                            maxRange
                        );
                        value *= 1f + (dps / 10000);
                    }
                }

                result += value;

            }

            return result;

        }

        public const float ANTENNA_THREAT_VALUE = 16f;
        public const float BEACON_THREAT_VALUE = 8f;
        public const float CONTAINER_THREAT_VALUE = 4f;
        public const float CONTROLLER_THREAT_VALUE = 16f;
        public const float GYRO_THREAT_VALUE = 8f;
        public const float GRAVITY_THREAT_VALUE = 8f;
        public const float GUN_THREAT_VALUE = 32f;
        public const float JUMPDRIVE_THREAT_VALUE = 48f;
        public const float MECHANICAL_THREAT_VALUE = 4f;
        public const float MEDICAL_THREAT_VALUE = 8f;
        public const float PARACHUTE_THREAT_VALUE = 4f;
        public const float PRODUCTION_THREAT_VALUE = 4f;
        public const float PROJECTOR_THREAT_VALUE = 2f;
        public const float POWER_THREAT_VALUE = 4f;
        public const float TOOL_THREAT_VALUE = 4f;
        public const float THRUSTER_THREAT_VALUE = 4f;
        public const float TURRET_THREAT_VALUE = 64f;

        private void DoCalcThreatLevel()
        {
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Starting ThreatLevel Calculation for Grid={_grid.DisplayName} | EntityId={_grid.EntityId}");
            _threatLevel = 0;
            var antennas = Antennas.ToList();
            _threatLevel += GetTargetValueFromBlockList(antennas, ANTENNA_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Antennas={antennas.Count} | ThreatLevel={_threatLevel}");
            var beacons = Beacons.ToList();
            _threatLevel += GetTargetValueFromBlockList(beacons, BEACON_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Beacons={beacons.Count} | ThreatLevel={_threatLevel}");
            var containers = Containers.ToList();
            _threatLevel += GetTargetValueFromBlockList(containers, CONTAINER_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Containers={containers.Count} | ThreatLevel={_threatLevel}");
            var controllers = Controllers.ToList();
            _threatLevel += GetTargetValueFromBlockList(controllers, CONTROLLER_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Controllers={controllers.Count} | ThreatLevel={_threatLevel}");
            var gyros = Gyros.ToList();
            _threatLevel += GetTargetValueFromBlockList(gyros, GYRO_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Gyros={gyros.Count} | ThreatLevel={_threatLevel}");
            var gravity = Gravity.ToList();
            _threatLevel += GetTargetValueFromBlockList(gravity, GRAVITY_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Gravity={gravity.Count} | ThreatLevel={_threatLevel}");
            var guns = Guns.ToList();
            _threatLevel += GetTargetValueFromBlockList(guns, GUN_THREAT_VALUE, scanDamage: true, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Guns={guns.Count} | ThreatLevel={_threatLevel}");
            var jumpDrives = JumpDrives.ToList();
            _threatLevel += GetTargetValueFromBlockList(jumpDrives, JUMPDRIVE_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: JumpDrives={jumpDrives.Count} | ThreatLevel={_threatLevel}");
            var mechanical = Mechanical.ToList();
            _threatLevel += GetTargetValueFromBlockList(mechanical, MECHANICAL_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Mechanical={mechanical.Count} | ThreatLevel={_threatLevel}");
            var medical = Medical.ToList();
            _threatLevel += GetTargetValueFromBlockList(medical, MEDICAL_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Medical={medical.Count} | ThreatLevel={_threatLevel}");
            var parachutes = Parachutes.ToList();
            _threatLevel += GetTargetValueFromBlockList(parachutes, PARACHUTE_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Parachutes={parachutes.Count} | ThreatLevel={_threatLevel}");
            var production = Production.ToList();
            _threatLevel += GetTargetValueFromBlockList(production, PRODUCTION_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Production={production.Count} | ThreatLevel={_threatLevel}");
            var projectors = Projectors.ToList();
            _threatLevel += GetTargetValueFromBlockList(projectors, PROJECTOR_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Projectors={projectors.Count} | ThreatLevel={_threatLevel}");
            var power = Power.ToList();
            _threatLevel += GetTargetValueFromBlockList(power, POWER_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Power={power.Count} | ThreatLevel={_threatLevel}");
            var tools = Tools.ToList();
            _threatLevel += GetTargetValueFromBlockList(tools, TOOL_THREAT_VALUE, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Tools={tools.Count} | ThreatLevel={_threatLevel}");
            var thrusters = Thrusters.ToList();
            _threatLevel += GetTargetValueFromBlockList(thrusters, THRUSTER_THREAT_VALUE);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Thrusters={thrusters.Count} | ThreatLevel={_threatLevel}");
            var turrets = Turrets.ToList();
            _threatLevel += GetTargetValueFromBlockList(turrets, TURRET_THREAT_VALUE, scanDamage: true, scanInventory: true);
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: Turrets={turrets.Count} | ThreatLevel={_threatLevel}");

            //Factor Power
            var powerOutput = GridPowerOutput();
            _threatLevel += powerOutput.X > 0 ? powerOutput.Y / 10 : 0;
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: PowerOutput={powerOutput} | ThreatLevel={_threatLevel}");

            //Factor Total Block Count
            _threatLevel += _grid.BlocksCount / 100;
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: BlocksCount={_grid.BlocksCount} | ThreatLevel={_threatLevel}");

            //Factor Grid Box Size
            var distance = Vector3D.Distance((_grid as IMyEntity).WorldAABB.Min, (_grid as IMyEntity).WorldAABB.Max);
            _threatLevel += (float)distance / 4;
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: GridBoxSize={distance} | ThreatLevel={_threatLevel}");

            //Factor Static/Dynamic
            if (_grid.IsStatic)
            {
                _threatLevel *= 0.75f;
                Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: GridIsStatic | ThreatLevel={_threatLevel}");
            }

            //Factor Cube Size
            if (_grid.GridSizeEnum == MyCubeSize.Large)
                _threatLevel *= 2.5f;
            else
                _threatLevel *= 0.5f;
            Logging.Instance.LogInfo(typeof(MyCubeGridExtraData), $"DoCalcThreatLevel: GridSize={_grid.GridSizeEnum} | ThreatLevel={_threatLevel}");

            ThreatLevelIsDirty = false;
        }

        private void _grid_OnFatBlockRemoved(MyCubeBlock obj)
        {
            ThreatLevelIsDirty = true;
        }

        private void _grid_OnFatBlockAdded(MyCubeBlock obj)
        {
            ThreatLevelIsDirty = true;
        }

    }

}
