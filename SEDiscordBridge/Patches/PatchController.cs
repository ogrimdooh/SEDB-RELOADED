using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using VRage.Utils;
using HarmonyLib;

namespace SEDiscordBridge
{

    public static class PatchController
    {

        public static dynamic CastedClas = null;
        public class TargetMethod : Attribute
        {
            public Type Type { get; set; }
            public string Method { get; set; }
        }

        public class PrefixMethod : Attribute
        {

        }

        public class PostFixMethod : Attribute
        {

        }

        public class PatchingClass : Attribute
        {

        }

        public static void PatchMethods()
        {
            Logging.Instance.LogInfo(typeof(PatchController), "Patching methods...");
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var PatchingClass in GetPatchingClassesAndInitalize(assembly))
            {

                foreach (var method in PatchingClass.GetMethods().Where(x => x.GetCustomAttributes(typeof(PrefixMethod), false).FirstOrDefault() != null))
                {
                    Patch(method, typeof(PrefixMethod));
                }

                foreach (var method in PatchingClass.GetMethods().Where(x => x.GetCustomAttributes(typeof(PostFixMethod), false).FirstOrDefault() != null))
                {
                    Patch(method, typeof(PostFixMethod));
                }
            }
        }

        public static void Patch(MethodInfo newMethod, Type typeOfPatch)
        {
            var harmony = new Harmony("SEDB-LITE");
            TargetMethod TargetMethodData = (TargetMethod)newMethod.GetCustomAttribute(typeof(TargetMethod));
            Logging.Instance.LogInfo(typeof(PatchController), $"Patching {TargetMethodData.Method} with {newMethod.Name} (Prefix)");

            if (typeOfPatch == typeof(PrefixMethod))
            {
                harmony.Patch(TargetMethodData.Type.GetMethod(TargetMethodData.Method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public), new HarmonyMethod(newMethod));

            }
            else
            {
                harmony.Patch(TargetMethodData.Type.GetMethod(TargetMethodData.Method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public), null, new HarmonyMethod(newMethod));

            }
        }

        public static IEnumerable<Type> GetPatchingClassesAndInitalize(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(PatchingClass), true).Length > 0)
                {

                    Activator.CreateInstance(type, SEDiscordBridgePlugin.Static);
                    if (SEDiscordBridgePlugin.DEBUG)
                    {
                        Logging.Instance.LogDebug(typeof(PatchController), $"Found patching class: {type.Name}");
                    }
                    yield return type;
                }
            }
        }
    }

}
