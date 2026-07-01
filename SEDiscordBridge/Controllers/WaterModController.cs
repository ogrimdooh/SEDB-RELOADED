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
using VRageMath;

namespace SEDiscordBridge.Controllers
{

    public static class WaterModController
    {

        private const ulong MOD_ID = 2200451495;

        private static object _waterApiSessionComponent;
        private static Type _waterApiSessionComponentType;
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
                                    _waterApiSessionComponentMethods = _waterApiSessionComponentType.GetMethods().GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.FirstOrDefault());
                                    Logging.Instance.LogInfo(typeof(WaterModController), $"WaterModController: Found WaterAPIComponent with {_waterApiSessionComponentMethods.Count} methods [{string.Join(", ", _waterApiSessionComponentMethods.Values.Select(m => m.Name))}].");
                                    break;
                                }
                            }
                        }
                    }
                }
                return _waterApiSessionComponent != null;
            }
            return false;
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

    }

}
