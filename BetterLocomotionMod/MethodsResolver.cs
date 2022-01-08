using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;

/*
 * Code by SDraw
 */

namespace BetterLocomotion
{
    internal static class MethodsResolver
    {
        private static MethodInfo _msPrepareForCalibration;

        public static void ResolveMethods()
        {
            var methods = typeof(VRCTrackingManager).GetMethods().Where(m =>
                m.Name.StartsWith("Method_Public_Static_Void_") && !m.GetParameters().Any() &&
                XrefScanner.UsedBy(m).Any(x => x.Type == XrefType.Method && x.TryResolve()?.DeclaringType == typeof(VRCFbbIkController))).ToArray();

            // void VRCTrackingManager.PrepareForCalibration()
            if (_msPrepareForCalibration == null)
            {
                var lMethods = methods.Where(m => XrefScanner.XrefScan(m).Any(x =>
                    x.Type == XrefType.Global && x.ReadAsObject().ToString().Contains("trying to calibrate"))).ToArray();

                if (lMethods.Length != 0)
                    _msPrepareForCalibration = lMethods[0];
            }

            // void VRCTracking.RestoreTrackingAfterCalibration()
            if (RestoreTrackingAfterCalibration == null)
            {
                var lMethods = methods.Where(m => m.Name != _msPrepareForCalibration?.Name).ToArray();

                if (lMethods.Length != 0)
                {
                    RestoreTrackingAfterCalibration = lMethods[0];
                    Main.Logger.Msg("VRCTrackingManager.RestoreTrackingAfterCalibration -> VRCTrackingManager." + RestoreTrackingAfterCalibration.Name);
                }
                else
                    Main.Logger.Warning("Can't resolve VRCTrackingManager.RestoreTrackingAfterCalibration");
            }

            // Task IKTweaks.CalibrationManager.ApplyStoredCalibration(GameObject avatarRoot, string avatarId)
            if (IKTweaksApplyStoredCalibration != null) return;
            foreach (var lMod in MelonHandler.Mods)
            {
                if (lMod.Info.Name != "IKTweaks") continue;
                Type lCbType = null;
                lMod.Assembly.GetTypes().DoIf(t => t.Name == "CalibrationManager", t => lCbType = t);
                if (lCbType != null)
                {
                    IKTweaksApplyStoredCalibration = lCbType.GetMethod("ApplyStoredCalibration", BindingFlags.NonPublic | BindingFlags.Static);
                    Main.Logger.Msg("IKTweaks.CalibrationManager.ApplyStoredCalibration " + (IKTweaksApplyStoredCalibration != null ? "found" : "not found"));
                }
                break;
            }
        }

        public static MethodInfo RestoreTrackingAfterCalibration { get; private set; }
        public static MethodInfo IKTweaksApplyStoredCalibration { get; private set; }
    }
}