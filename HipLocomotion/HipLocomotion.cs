using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using MelonLoader;
using VRC.Animation;
using BuildInfo = HipLocomotion.BuildInfo;
using Main = HipLocomotion.Main;
using VRC.SDKBase;

/*A lot of code was taken from the BetterDirections mod
//Special thanks to d-magit
//https://github.com/d-magit/VRC-Mods 
*/

[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: MelonInfo(typeof(Main), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]

namespace HipLocomotion
{
    public static class BuildInfo
    {
        public const string Name = "HipLocomotion";
        public const string Author = "Erimel";
        public const string Version = "1.0";
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += Main.VRChat_OnUiManagerInit; }

    public class Main : MelonMod
    {
        private static MelonMod Instance;
        private static HarmonyLib.Harmony HInstance => Instance.HarmonyInstance;

        private static MelonPreferences_Entry<bool> EnableHipLocomotion;
        private static MelonPreferences_Entry<bool> HeadLocomotionIfProne;

        // Wait for Ui Init so XRDevice.isPresent is defined
        public override void OnApplicationStart()
        {
            Instance = this;

            WaitForUiInit();

            MelonLogger.Msg("Successfully loaded!");

            var category = MelonPreferences.CreateCategory("HipLocomotion", "HipLocomotion");
            EnableHipLocomotion = category.CreateEntry("Enabled", true, "Enable hip locomotion");
            HeadLocomotionIfProne = category.CreateEntry("HeadLocomotionProne", true, "Use head locomotion when prone");
            OnPreferencesSaved();
        }

        private static void WaitForUiInit()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
                typeof(UIXManager).GetMethod("OnApplicationStart").Invoke(null, null);
            else
            {
                MelonLogger.Warning("UiExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
                static IEnumerator OnUiManagerInit()
                {
                    while (VRCUiManager.prop_VRCUiManager_0 == null)
                        yield return null;
                    VRChat_OnUiManagerInit();
                }
                MelonCoroutines.Start(OnUiManagerInit());
            }
        }

        // Apply the patch
        public static void VRChat_OnUiManagerInit()
        {
            if (XRDevice.isPresent)
            {
                MelonLogger.Msg("XRDevice detected. Initializing...");
                try
                {
                    foreach (var info in typeof(VRCMotionState).GetMethods().Where(method =>
                        method.Name.Contains("Method_Public_Void_Vector3_Single_") && !method.Name.Contains("PDM")))
                        HInstance.Patch(info, new HarmonyMethod(typeof(Main).GetMethod(nameof(Prefix))));
                    MelonLogger.Msg("Successfully loaded!");
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Failed to initialize mod!");
                    MelonLogger.Error(e);
                }
            }
            else
                MelonLogger.Warning("Mod is VR-Only.");
        }

        // Substitute the direction from the original method with our own
        public static void Prefix(ref Vector3 __0) { __0 = CalculateDirection(__0); }
        // Gets the hip tracker
        private static Transform hipTransform;
        private static Transform HipTransform 
        {
            get
            {
                if (hipTransform == null)
                    hipTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips);
                return hipTransform;
            }
        }
        // Gets the camera
        private static GameObject cameraObj;
        private static GameObject CameraObj
        {
            get
            {
                if (cameraObj == null)
                    cameraObj = Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0].transform.Find(Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : "Camera (eye)").gameObject;
                return cameraObj;
            }
        }
        //Gets the VRCMotionState to know if the player is crouching or prone.
        private static VRCMotionState playerMotionState;
        private static VRCMotionState PlayerMotionState
        {
            get
            {
                if (playerMotionState == null)
                    playerMotionState = Utils.GetLocalPlayer().gameObject.GetComponent<VRCMotionState>();
                return playerMotionState;
            }
        }
        //Gets the player API to get player speed
        private static VRCPlayerApi playerApi;
        private static VRCPlayerApi PlayerApi
        {
            get
            {
                if (playerApi == null)
                    playerApi = Utils.GetLocalPlayer().field_Private_VRCPlayerApi_0;
                return playerApi;
            }
        }

        // Fixes the game's original direction to match the preferred one
        private static Vector3 CalculateDirection(Vector3 headVelo)
        {
            if (EnableHipLocomotion.Value && HipTransform != null && (HeadLocomotionIfProne.Value == false || PlayerMotionState.field_Private_Single_0 > 0.4f)) //hip Locomotion.
            {
                var badRot = Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HipTransform.forward), Vector3.up));
                Vector3 hipVelo = Vector3.ProjectOnPlane(HipTransform.right, Vector3.up).normalized * Input.GetAxis("Horizontal") * PlayerApi.GetStrafeSpeed() + Vector3.ProjectOnPlane(HipTransform.forward, Vector3.up).normalized * Input.GetAxis("Vertical") * PlayerApi.GetRunSpeed();
                if (PlayerMotionState.field_Private_Single_0 < 0.65f && PlayerMotionState.field_Private_Single_0 >= 0.4f) hipVelo *= 0.5f; //player crouching at 65% of height: half speed
                if (PlayerMotionState.field_Private_Single_0 < 0.4f) hipVelo *= 0.1f; //player prone at 40% of height: tenth speed
                var inputDirection = Quaternion.Inverse(badRot) * hipVelo;
                return Quaternion.FromToRotation(HipTransform.up, Vector3.up) * HipTransform.rotation * inputDirection;
            }
            else //head locomotion (from BetterDirection)
            {
                var badRot = Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, CameraObj.transform.forward),Vector3.up));
                var inputDirection = Quaternion.Inverse(badRot) * headVelo;
                return Quaternion.FromToRotation(CameraObj.transform.up, Vector3.up) * CameraObj.transform.rotation * inputDirection;
            }
        }
    }
    class Utils
    {
        public static VRCPlayer GetLocalPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        public static VRCTrackingManager GetVRCTrackingManager() => VRCTrackingManager.field_Private_Static_VRCTrackingManager_0;
        public static VRCTrackingSteam GetVRCTrackingSteam() => GetVRCTrackingManager().field_Private_List_1_VRCTracking_0[0].TryCast<VRCTrackingSteam>();
    }
}