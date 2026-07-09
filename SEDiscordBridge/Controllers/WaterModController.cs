using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace SEDiscordBridge.Controllers
{

    public static class WaterModController
    {

        private const ulong MOD_ID = 2200451495;

        private static object _waterApiSessionComponent;
        private static Type _waterApiSessionComponentType;
        private static Type _waterDataType;
        private static Dictionary<string, MethodInfo> _waterApiSessionComponentMethods;
        public static bool IsRegistered()
        {
            var hasMod = MOD_ID != 0 && MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == MOD_ID);
            if (hasMod)
            {
                if (_waterApiSessionComponent == null)
                {
                    var mainSession = MySession.Static.GetComponentByTypeName("Jakaria.Session");
                    if (mainSession != null)
                    {
                        Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Jakaria.Session component.");
                        var componentsCollection = mainSession.GetType().GetField("_sessionComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(mainSession) as IDictionary;
                        if (componentsCollection != null)
                        {
                            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found _sessionComponents collection with {componentsCollection.Count} components.");
                            foreach (var component in componentsCollection.Values)
                            {
                                if (component.GetType().Name == "WaterAPIComponent")
                                {
                                    _waterApiSessionComponent = component;
                                    _waterApiSessionComponentType = component.GetType();
                                    _waterApiSessionComponentMethods = _waterApiSessionComponentType.GetMethods(
                                        BindingFlags.Public |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Instance |
                                        BindingFlags.Static |
                                        BindingFlags.DeclaredOnly
                                    ).GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.FirstOrDefault());
                                    Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found WaterAPIComponent with {_waterApiSessionComponentMethods.Count} methods [{string.Join(", ", _waterApiSessionComponentMethods.Values.Select(m => m.Name))}].");
                                    break;
                                }
                            }
                        }
                        _waterDataType = mainSession.GetType().Assembly.GetTypes().FirstOrDefault(x => x.Name == "WaterData");
                        if (_waterDataType != null)
                        {
                            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found WaterData type.");
                        }
                    }
                }
                return _waterApiSessionComponent != null;
            }
            return false;
        }

        public static float Entity_PercentUnderwater(MyEntity entity)
        {
            if (!IsRegistered() || !_waterApiSessionComponentMethods.ContainsKey(nameof(Entity_PercentUnderwater)))
            {
                return 0f;
            }
            var method = _waterApiSessionComponentMethods[nameof(Entity_PercentUnderwater)];
            if (method == null)
                return 0f;
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Invoking Entity_PercentUnderwater for entity {entity?.EntityId ?? 0}.");
            return (float)method.Invoke(_waterApiSessionComponent, new object[] { entity });
        }

        public static Vector3D GetClosestSurfacePoint(Vector3D pos, MyPlanet planet = null)
        {
            if (!IsRegistered() || !_waterApiSessionComponentMethods.ContainsKey(nameof(GetClosestSurfacePoint)))
            {
                return pos;
            }
            var method = _waterApiSessionComponentMethods[nameof(GetClosestSurfacePoint)];
            if (method == null) 
                return pos;
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Invoking GetClosestSurfacePoint for position {pos} on planet {planet?.EntityId ?? 0}.");
            return (Vector3D)method.Invoke(_waterApiSessionComponent, new object[] { pos, planet });
        }

        public static bool IsUnderwater(Vector3D pos, MyPlanet planet = null)
        {
            if (!IsRegistered() || !_waterApiSessionComponentMethods.ContainsKey(nameof(IsUnderwater)))
            {
                return false;
            }
            var method = _waterApiSessionComponentMethods[nameof(IsUnderwater)];
            if (method == null)
                return false;
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Invoking IsUnderwater for position {pos} on planet {planet?.EntityId ?? 0}.");
            return (bool)method.Invoke(_waterApiSessionComponent, new object[] { pos, planet });
        }

        public static bool HasWater(MyPlanet planet)
        {
            if (!IsRegistered() || !_waterApiSessionComponentMethods.ContainsKey(nameof(HasWater)))
            {
                return false;
            }
            var method = _waterApiSessionComponentMethods[nameof(HasWater)];
            if (method == null)
                return false;
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Invoking HasWater for planet {planet?.EntityId ?? 0}.");
            return (bool)method.Invoke(_waterApiSessionComponent, new object[] { planet });
        }

        public static bool SetWaterDensity(MyPlanet planet, float density)
        {
            if (!IsRegistered())
            {
                return false;
            }
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Invoking SetWaterDensity for planet {planet?.EntityId ?? 0}.");
            var waterComponentType = planet.Components.GetComponentTypes().FirstOrDefault(x => x.Name == "WaterComponent");
            if (waterComponentType != null)
            {
                Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found WaterComponent for planet {planet?.EntityId ?? 0}.");
                var settingsField = waterComponentType.GetField("Settings");
                if (settingsField != null)
                {
                    Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Settings field for planet {planet?.EntityId ?? 0}.");
                    if (planet.Components.TryGet(waterComponentType, out var waterComponent))
                    {
                        Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found WaterComponent for planet {planet?.EntityId ?? 0}.");
                        var settings = settingsField.GetValue(waterComponent);
                        if (settings != null)
                        {
                            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Settings for planet {planet?.EntityId ?? 0}.");
                            var materialField = settings.GetType().GetField("Material");
                            if (materialField != null)
                            {
                                Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Material field for planet {planet?.EntityId ?? 0}.");
                                var material = materialField.GetValue(settings);
                                if (material != null)
                                {
                                    Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Material for planet {planet?.EntityId ?? 0}.");
                                    var densityField = material.GetType().GetField("Density");
                                    if (densityField != null)
                                    {
                                        Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found Density field for planet {planet?.EntityId ?? 0}.");
                                        densityField.SetValue(material, density);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static void SetWaterDensityToAllPlanets(float density)
        {
            foreach (var planet in GameWatcherController.Planets.Values)
            {
                SetWaterDensity(planet, density);
            }
        }

        public static bool SetPlayerMaximumPressure(float maxPressure)
        {
            if (!IsRegistered())
            {
                return false;
            }
            Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: SetPlayerMaximumPressure called to {maxPressure}");
            if (_waterDataType != null)
            {
                Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: _waterDataType is not null!");
                var characterConfigsField = _waterDataType.GetField("CharacterConfigs", BindingFlags.Static | BindingFlags.Public);
                if (characterConfigsField != null)
                {
                    Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: characterConfigsField is not null!");
                    var characterConfigs = characterConfigsField.GetValue(null) as IDictionary;
                    if (characterConfigs != null)
                    {
                        Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: characterConfigs is not null!");
                        foreach (var item in characterConfigs.Values)
                        {
                            var itemType = item.GetType();
                            var maximumPressureType = itemType.GetField("MaximumPressure");
                            if (maximumPressureType != null)
                            {
                                Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: maximumPressureType is not null!");
                                maximumPressureType.SetValue(item, maxPressure);
                            }
                        }
                    }
                }
            }
            return false;
        }

    }

}
