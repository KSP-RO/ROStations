using System;
using System.Reflection;
using UnityEngine;
using ROLib;

namespace ROStations
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KerbalismInterface : MonoBehaviour
    {
        private static bool kerbalismChecked;
        public static bool isEnabled;

        public static Func<double, string> HumanReadableVolume;
        public static Func<double, string> HumanReadableSurface;
        public static Func<object, double> GetHabVolume;
        public static Func<object, double> GetHabSurface;
        public static Action<object, double> SetHabVolume;
        public static Action<object, double> SetHabSurface;
        public static Action<object, string> SetHabVolumeStr;
        public static Action<object, string> SetHabSurfaceStr;
        public static Action<object, float> SetHabShieldingCost;
        public static Func<object, string> GetComfortBonus;
        public static Action<object, bool> SetComfortEnabled;

        void Start()
        {
            if (kerbalismChecked)
            {
                Destroy(gameObject);
                return;
            }

            kerbalismChecked = true;
            Assembly kerbalismAssembly = null;

            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                if (a.assembly.GetName().Name == "Kerbalism") kerbalismAssembly = a.assembly;
            }

            isEnabled = kerbalismAssembly != null;
            ROLLog.debug($"kerbalismAssembly.isEnabled{isEnabled}");

            if (!isEnabled)
            {
                Destroy(gameObject);
                return;
            }

            try
            {
                Type t_Lib = kerbalismAssembly.GetType("KERBALISM.Lib");
                MethodInfo m_HumanReadableVolume = t_Lib.GetMethod("HumanReadableVolume", BindingFlags.Public | BindingFlags.Static);
                MethodInfo m_HumanReadableSurface = t_Lib.GetMethod("HumanReadableSurface", BindingFlags.Public | BindingFlags.Static);
                HumanReadableVolume = (Func<double, string>)Delegate.CreateDelegate(typeof(Func<double, string>), m_HumanReadableVolume);
                HumanReadableSurface = (Func<double, string>)Delegate.CreateDelegate(typeof(Func<double, string>), m_HumanReadableSurface);

                Type t_Habitat = kerbalismAssembly.GetType("KERBALISM.Habitat");
                Type t_Comfort = kerbalismAssembly.GetType("KERBALISM.Comfort");
                GetHabVolume = ReflectionHelpers.CreateFieldGetter<double>(t_Habitat.GetField("volume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                GetHabSurface = ReflectionHelpers.CreateFieldGetter<double>(t_Habitat.GetField("surface", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                GetComfortBonus = ReflectionHelpers.CreateFieldGetter<string>(t_Comfort.GetField("bonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetHabVolume = ReflectionHelpers.CreateFieldSetter<double>(t_Habitat.GetField("volume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetHabSurface = ReflectionHelpers.CreateFieldSetter<double>(t_Habitat.GetField("surface", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetHabVolumeStr = ReflectionHelpers.CreateFieldSetter<string>(t_Habitat.GetField("Volume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetHabSurfaceStr = ReflectionHelpers.CreateFieldSetter<string>(t_Habitat.GetField("Surface", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetHabShieldingCost = ReflectionHelpers.CreateFieldSetter<float>(t_Habitat.GetField("shieldingCost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                SetComfortEnabled = ReflectionHelpers.CreateFieldSetter<bool>(t_Comfort.GetField("isEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            }
            catch (Exception e)
            {
                ROLLog.debug($"Failed to initialize Kerbalism support\n{e}");
                isEnabled = false;
            }

            Destroy(gameObject);
        }
    }
}
