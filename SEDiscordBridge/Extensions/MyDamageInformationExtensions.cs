using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace SEDiscordBridge.Patches
{
    public static class MyDamageInformationExtensions
    {

        [Flags]
        public enum DamageType
        {

            None = 0,
            Creature = 1 << 1,
            Bullet = 1 << 2,
            Explosion = 1 << 3,
            Radioactivity = 1 << 4,
            Fire = 1 << 5,
            Toxicity = 1 << 6,
            Fall = 1 << 7,
            Tool = 1 << 8,
            Environment = 1 << 9,
            Suicide = 1 << 10,
            Asphyxia = 1 << 11,
            Other = 1 << 12

        }

        public enum AttackerType
        {

            None = 0,
            Character = 1,
            CubeBlock = 2

        }

        public static readonly Dictionary<DamageType, MyStringHash[]> DAMAGE_TYPES_EQUIVALENCE = new Dictionary<DamageType, MyStringHash[]>()
        {
            { DamageType.None, new MyStringHash[] { MyDamageType.Unknown } },
            { DamageType.Creature, new MyStringHash[] { MyDamageType.Wolf, MyDamageType.Spider } },
            { DamageType.Bullet, new MyStringHash[] { MyDamageType.Bolt, MyDamageType.Bullet, MyDamageType.Weapon } },
            { DamageType.Explosion, new MyStringHash[] { MyDamageType.Explosion, MyDamageType.Mine, MyDamageType.Rocket } },
            { DamageType.Radioactivity, new MyStringHash[] { MyDamageType.Radioactivity } },
            { DamageType.Fire, new MyStringHash[] { MyDamageType.Fire, MyDamageType.Thruster } },
            { DamageType.Toxicity, new MyStringHash[] { MyDamageType.Debug } },
            { DamageType.Fall, new MyStringHash[] { MyDamageType.Environment, MyDamageType.Deformation, MyDamageType.Fall, MyDamageType.Squeez } },
            { DamageType.Tool, new MyStringHash[] { MyDamageType.Weld, MyDamageType.Grind, MyDamageType.Drill } },
            { DamageType.Environment, new MyStringHash[] { MyDamageType.Temperature } },
            { DamageType.Suicide, new MyStringHash[] { MyDamageType.Suicide } },
            { DamageType.Asphyxia, new MyStringHash[] { MyDamageType.Asphyxia, MyDamageType.LowPressure } },
            { DamageType.Other, new MyStringHash[] { MyDamageType.Destruction, MyDamageType.OutOfBounds } }
        };

        public static DamageType GetDamageType(MyStringHash source)
        {
            var query = DAMAGE_TYPES_EQUIVALENCE.Where(x => x.Value.Contains(source));
            if (query.Any())
                return query.FirstOrDefault().Key;
            return DamageType.None;
        }

        public static IMyEntity GetAttacker(this MyDamageInformation damage, out long playerId, out DamageType damageType, out AttackerType attackerType)
        {
            playerId = 0;
            attackerType = AttackerType.None;
            damageType = GetDamageType(damage.Type);
            if (damageType == DamageType.Creature)
            {
                var creature = MyAPIGateway.Entities.GetEntityById(damage.AttackerId) as IMyCharacter;
                if (creature != null)
                {
                    playerId = creature.GetPlayerId();
                    attackerType = AttackerType.Character;
                    return creature;
                }
            }
            else if (damageType == DamageType.Tool)
            {
                var attackerTool = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
                if (attackerTool != null)
                {
                    var handTool = attackerTool as Sandbox.ModAPI.Weapons.IMyEngineerToolBase;
                    if (handTool != null)
                    {
                        var character = MyAPIGateway.Entities.GetEntityById(handTool.OwnerId) as IMyCharacter;
                        if (character != null)
                        {
                            playerId = character.GetPlayerId();
                            attackerType = AttackerType.Character;
                            return character;
                        }
                    }
                    else
                    {
                        var handDrill = attackerTool as Sandbox.ModAPI.Weapons.IMyHandDrill;
                        if (handDrill != null)
                        {
                            var character = MyAPIGateway.Entities.GetEntityById((handDrill as IMyGunBaseUser).OwnerId) as IMyCharacter;
                            if (character != null)
                            {
                                playerId = character.GetPlayerId();
                                attackerType = AttackerType.Character;
                                return character;
                            }
                        }
                        else
                        {
                            var toolBlock = attackerTool as IMyCubeBlock;
                            if (toolBlock != null)
                            {
                                playerId = toolBlock.OwnerId;
                                attackerType = AttackerType.CubeBlock;
                                return toolBlock;
                            }
                        }
                    }
                }
            }
            else if (damageType == DamageType.Bullet || damageType == DamageType.Explosion)
            {
                var attackerGun = MyAPIGateway.Entities.GetEntityById(damage.AttackerId);
                if (attackerGun != null)
                {
                    var weaponBlock = attackerGun as IMyCubeBlock;
                    if (weaponBlock != null)
                    {
                        playerId = weaponBlock.OwnerId;
                        attackerType = AttackerType.CubeBlock;
                        return weaponBlock;
                    }
                    else
                    {
                        var handGun = attackerGun as IMyGunBaseUser;
                        if (handGun != null)
                        {
                            var character = handGun.Owner as IMyCharacter;
                            if (character != null)
                            {
                                playerId = character.GetPlayerId();
                                attackerType = AttackerType.Character;
                                return character;
                            }
                        }
                    }
                }
            }
            return null;
        }

    }

}