using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;

namespace HipLocomotion
{
    static class MethodsResolver
    {
        static MethodInfo ms_prepareForCalibration = null;
        static MethodInfo ms_restoreTrackingAfterCalibration = null;
        static MethodInfo ms_calibrate = null; // IKTweaks
        static MethodInfo ms_applyStoredCalibration = null; // IKTweaks

        public static void ResolveMethods()
        {
            // void VRCTracking.RestoreTrackingAfterCalibration()
            if (ms_restoreTrackingAfterCalibration == null)
            {
                var l_methods = typeof(VRCTrackingManager).GetMethods().Where(m =>
                    m.Name.StartsWith("Method_Public_Static_Void_") && (m.Name != ms_prepareForCalibration?.Name) && (m.ReturnType == typeof(void)) && !m.GetParameters().Any() &&
                    XrefScanner.UsedBy(m).Where(x => (x.Type == XrefType.Method) && (x.TryResolve()?.DeclaringType == typeof(VRCFbbIkController))).Any()
                );

                if (l_methods.Any())
                {
                    ms_restoreTrackingAfterCalibration = l_methods.First();
                    MelonLogger.Msg("VRCTrackingManager.RestoreTrackingAfterCalibration -> VRCTrackingManager." + ms_restoreTrackingAfterCalibration.Name);
                }
                else
                    MelonLogger.Warning("Can't resolve VRCTrackingManager.RestoreTrackingAfterCalibration");
            }

            // Task IKTweaks.CalibrationManager.ApplyStoredCalibration(GameObject avatarRoot, string avatarId)
            if (ms_applyStoredCalibration == null)
            {
                foreach (MelonLoader.MelonMod l_mod in MelonLoader.MelonHandler.Mods)
                {
                    if (l_mod.Info.Name == "IKTweaks")
                    {
                        Type l_cbType = null;
                        l_mod.Assembly.GetTypes().DoIf(t => t.Name == "CalibrationManager", t => l_cbType = t);
                        if (l_cbType != null)
                        {
                            ms_applyStoredCalibration = l_cbType.GetMethod("ApplyStoredCalibration", BindingFlags.NonPublic | BindingFlags.Static);
                            MelonLogger.Msg("IKTweaks.CalibrationManager.ApplyStoredCalibration " + ((ms_applyStoredCalibration != null) ? "found" : "not found"));
                        }
                        break;
                    }
                }
            }
        }
        public static MethodInfo RestoreTrackingAfterCalibration
        {
            get => ms_restoreTrackingAfterCalibration;
        }
        public static MethodInfo IKTweaks_ApplyStoredCalibration
        {
            get => ms_applyStoredCalibration;
        }
    }
}