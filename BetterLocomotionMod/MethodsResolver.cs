using System.Reflection;
using System.Linq;
using UnhollowerRuntimeLib.XrefScans;
using MelonLoader;

/*
 * Code by SDraw
 */

namespace BetterLocomotion
{
    internal static class MethodsResolver
    {
        internal static MelonLogger.Instance Logger;

        private static MethodInfo ms_prepareForCalibration;
        private static MethodInfo ms_restoreTrackingAfterCalibration;

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
        }

        public static MethodInfo PrepareForCalibration
        {
            get => ms_prepareForCalibration;
        }
        public static MethodInfo RestoreTrackingAfterCalibration
        {
            get => ms_restoreTrackingAfterCalibration;
        }
    }
}