using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Utils;

namespace SEDiscordBridge.Extensions
{
    public static class FlagsUtils
    {
        public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
        {
            foreach (T flag in Enum.GetValues(typeof(T)).Cast<T>())
            {
                if (value.IsFlagSet(flag))
                    yield return flag;
            }
        }

        public static bool IsFlagSet<T>(this T value, T flag) where T : struct
        {
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        public static int GetMaxSetFlagValue<T>(T flags) where T : struct
        {
            int value = (int)Convert.ChangeType(flags, typeof(int));
            IEnumerable<int> setValues = Enum.GetValues(flags.GetType()).Cast<int>().Where(f => (f & value) == f);
            return setValues.Any() ? setValues.Max() : 0;
        }

    }

}