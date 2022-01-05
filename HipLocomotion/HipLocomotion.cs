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
[assembly: MelonColor(ConsoleColor.Magenta)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]

namespace HipLocomotion
{
    public static class BuildInfo
    {
        public const string Name = "HipLocomotion";
        public const string Author = "Erimel";
        public const string Version = "1.1.1";
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += Main.VRChat_OnUiManagerInit; }

    public class Main : MelonMod
    {
        private static MelonMod Instance;
        private static HarmonyLib.Harmony HInstance => Instance.HarmonyInstance;

        internal static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<Locomotion> LocomotionMode;
        private static MelonPreferences_Entry<bool> HeadLocomotionIfProne;

        // Wait for Ui Init so XRDevice.isPresent is defined
        public override void OnApplicationStart()
        {
            Instance = this;

            WaitForUiInit();

            MelonLogger.Msg("Successfully loaded!");

            Settings();

            OnPreferencesSaved();
        }
        private static void Settings()
        {
            var category = Category = MelonPreferences.CreateCategory("HipLocomotion", "HipLocomotion");
            LocomotionMode = category.CreateEntry("LocomotionMode", Locomotion.Hip, "Locomotion Mode");
            HeadLocomotionIfProne = category.CreateEntry("HeadLocomotionProne", true, "Use head locomotion when prone");
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
        public static VRCPlayer GetLocalPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;

        // Gets the hip transform
        private static Transform hipTransform;
        private static Transform HipTransform
        {
            get
            {
                if (hipTransform == null)
                    hipTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips);
                return hipTransform;
            }
        }
        // Gets the chest transform
        private static Transform chestTransform;
        private static Transform ChestTransform
        {
            get
            {
                if (chestTransform == null)
                    chestTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Chest);
                return chestTransform;
            }
        }
        // Gets the head transform
        private static Transform headTransform;
        private static Transform HeadTransform
        {
            get
            {
                if (headTransform == null)
                    headTransform = Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0].transform.Find(Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : "Camera (eye)").gameObject.transform;
                return headTransform;
            }
        }
        //Gets the VRCMotionState to know if the player is crouching or prone.
        private static VRCMotionState playerMotionState;
        private static VRCMotionState PlayerMotionState
        {
            get
            {
                if (playerMotionState == null)
                    playerMotionState = GetLocalPlayer().gameObject.GetComponent<VRCMotionState>();
                return playerMotionState;
            }
        }
        // Fixes the game's original direction to match the preferred one
        private static Vector3 CalculateDirection(Vector3 headVelo)
        {
            if (headVelo.magnitude > 0)
            {
                switch (LocomotionMode.Value)
                {
                    case Locomotion.Hip:
                        if ((HeadLocomotionIfProne.Value == false || PlayerMotionState.field_Private_Single_0 > 0.4f) && (GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.SixPoint || (GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.FourPoint && PlayerMotionState.field_Private_Single_0 > 0.65f)))
                        { //Hip locomotion
                            Vector3 hipVelo = Vector3.ProjectOnPlane(HipTransform.right, Vector3.up).normalized * Input.GetAxis("Horizontal") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetStrafeSpeed() + Vector3.ProjectOnPlane(HipTransform.forward, Vector3.up).normalized * Input.GetAxis("Vertical") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetRunSpeed();
                            if (PlayerMotionState.field_Private_Single_0 < 0.4f) hipVelo *= 0.1f; //player prone at 40% of height: tenth speed
                            else if (PlayerMotionState.field_Private_Single_0 < 0.65f) hipVelo *= 0.5f; //player crouching at 65% of height: half speed
                            return Quaternion.FromToRotation(HipTransform.up, Vector3.up) * HipTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HipTransform.forward), Vector3.up))) * hipVelo;
                        }
                        else //Head locomotion
                        {
                            return Quaternion.FromToRotation(HeadTransform.up, Vector3.up) * HeadTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HeadTransform.forward), Vector3.up))) * headVelo;
                        }
                    case Locomotion.Chest:
                        if ((HeadLocomotionIfProne.Value == false || PlayerMotionState.field_Private_Single_0 > 0.4f) && (GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.SixPoint || (GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.FourPoint && PlayerMotionState.field_Private_Single_0 > 0.65f)))
                        { //Chest locomotion
                            Vector3 chestVelo = Vector3.ProjectOnPlane(ChestTransform.right, Vector3.up).normalized * Input.GetAxis("Horizontal") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetStrafeSpeed() + Vector3.ProjectOnPlane(ChestTransform.forward, Vector3.up).normalized * Input.GetAxis("Vertical") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetRunSpeed();
                            if (PlayerMotionState.field_Private_Single_0 < 0.4f) chestVelo *= 0.1f; //player prone at 40% of height: tenth speed
                            else if (PlayerMotionState.field_Private_Single_0 < 0.65f) chestVelo *= 0.5f; //player crouching at 65% of height: half speed
                            return Quaternion.FromToRotation(ChestTransform.up, Vector3.up) * ChestTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, ChestTransform.forward), Vector3.up))) * chestVelo;
                        }
                        else //Head locomotion
                        {
                            return Quaternion.FromToRotation(HeadTransform.up, Vector3.up) * HeadTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HeadTransform.forward), Vector3.up))) * headVelo;
                        }
                    case Locomotion.Head:
                    default: //Head locomotion
                        return Quaternion.FromToRotation(HeadTransform.up, Vector3.up) * HeadTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HeadTransform.forward), Vector3.up))) * headVelo;
                }
            }
            else
            {
                return headVelo;
            }
        }
    }
    public enum Locomotion
    {
        Head,
        Hip,
        Chest
    }
}