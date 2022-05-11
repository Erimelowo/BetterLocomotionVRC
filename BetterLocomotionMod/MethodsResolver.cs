using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;

/*
 * Code by SDraw
 */

namespace BetterLocomotionDE
{
    internal static class MethodsResolver
    {
        internal static MelonLogger.Instance Logger;

        private static MethodInfo ms_prepareForCalibration;
        private static MethodInfo ms_restoreTrackingAfterCalibration;
        private static MethodInfo ms_calibrate; // IKTweaks
        private static MethodInfo ms_applyStoredCalibration; // IKTweaks

        public static void ResolveMethods(MelonLogger.Instance loggerInstance)
        {
            Logger = loggerInstance;
            // void VRCTrackingManager.PrepareForCalibration()
            if (ms_prepareForCalibration == null)
            {
                var l_methods = typeof(VRCTrackingManager).GetMethods().Where(m =>
                    m.Name.StartsWith("Method_Public_Static_Void_") && (m.ReturnType == typeof(void)) && !m.GetParameters().Any() &&
                    XrefScanner.XrefScan(m).Where(x => (x.Type == XrefType.Global) && x.ReadAsObject().ToString().Contains("trying to calibrate")).Any() &&
                    XrefScanner.UsedBy(m).Where(x => (x.Type == XrefType.Method) && (x.TryResolve()?.DeclaringType == typeof(VRCFbbIkController))).Any()
                );

                if (l_methods.Any())
                {
                    ms_prepareForCalibration = l_methods.First();
                    Logger.Msg("VRCTrackingManager.PrepareForCalibration -> VRCTrackingManager." + ms_prepareForCalibration.Name);
                }
                else
                    Logger.Warning("Can't resolve VRCTrackingManager.PrepareForCalibration");
            }

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
                    Logger.Msg("VRCTrackingManager.RestoreTrackingAfterCalibration -> VRCTrackingManager." + ms_restoreTrackingAfterCalibration.Name);
                }
                else
                    Logger.Warning("Can't resolve VRCTrackingManager.RestoreTrackingAfterCalibration");
            }

            // void IKTweaks.CalibrationManager.Calibrate(GameObject avatarRoot)
            if (ms_calibrate == null)
            {
                foreach (MelonMod l_mod in MelonHandler.Mods)
                {
                    if (l_mod.Info.Name == "IKTweaks")
                    {
                        Type l_cbType = null;
                        l_mod.Assembly.GetTypes().DoIf(t => t.Name == "CalibrationManager", t => l_cbType = t);
                        if (l_cbType != null)
                        {
                            ms_calibrate = l_cbType.GetMethod("Calibrate");
                            Logger.Msg("IKTweaks.CalibrationManager.Calibrate " + ((ms_calibrate != null) ? "found" : "not found"));
                        }
                        break;
                    }
                }
            }

            // Task IKTweaks.CalibrationManager.ApplyStoredCalibration(GameObject avatarRoot, string avatarId)
            if (ms_applyStoredCalibration == null)
            {
                foreach (MelonMod l_mod in MelonHandler.Mods)
                {
                    if (l_mod.Info.Name == "IKTweaks")
                    {
                        Type l_cbType = null;
                        l_mod.Assembly.GetTypes().DoIf(t => t.Name == "CalibrationManager", t => l_cbType = t);
                        if (l_cbType != null)
                        {
                            ms_applyStoredCalibration = l_cbType.GetMethod("ApplyStoredCalibration", BindingFlags.NonPublic | BindingFlags.Static);
                            Logger.Msg("IKTweaks.CalibrationManager.ApplyStoredCalibration " + ((ms_applyStoredCalibration != null) ? "found" : "not found"));
                        }
                        break;
                    }
                }
            }
        }

        public static MethodInfo PrepareForCalibration
        {
            get => ms_prepareForCalibration;
        }
        public static MethodInfo RestoreTrackingAfterCalibration
        {
            get => ms_restoreTrackingAfterCalibration;
        }
        public static MethodInfo IKTweaks_Calibrate
        {
            get => ms_calibrate;
        }
        public static MethodInfo IKTweaks_ApplyStoredCalibration
        {
            get => ms_applyStoredCalibration;
        }
    }
}