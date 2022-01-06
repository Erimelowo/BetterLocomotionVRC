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
        public const string Version = "1.2.1";
    }

    internal static class UIXManager { public static void OnApplicationStart() => UIExpansionKit.API.ExpansionKitApi.OnUiManagerInit += Main.VRChat_OnUiManagerInit; }

    public class Main : MelonMod
    {
        private static MelonMod Instance;

        private static Main m_Instance;
        private static HarmonyLib.Harmony HInstance => Instance.HarmonyInstance;

        internal static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<Locomotion> LocomotionMode;

        // Wait for Ui Init so XRDevice.isPresent is defined
        public override void OnApplicationStart()
        {
            Instance = this;
            m_Instance = this;

            WaitForUiInit();

            MelonLogger.Msg("Successfully loaded!");

            Settings();

            OnPreferencesSaved();

            MethodsResolver.ResolveMethods();

            if (MethodsResolver.RestoreTrackingAfterCalibration != null)
                HarmonyInstance.Patch(MethodsResolver.RestoreTrackingAfterCalibration, null, new HarmonyLib.HarmonyMethod(typeof(Main), nameof(VRCTrackingManager_RestoreTrackingAfterCalibration)));

            if (MethodsResolver.IKTweaks_ApplyStoredCalibration != null)
                HarmonyInstance.Patch(MethodsResolver.IKTweaks_ApplyStoredCalibration, new HarmonyLib.HarmonyMethod(typeof(Main), nameof(VRCTrackingManager_RestoreTrackingAfterCalibration)), null);
        }
        private static void Settings()
        {
            var category = Category = MelonPreferences.CreateCategory("HipLocomotion", "HipLocomotion");
            LocomotionMode = category.CreateEntry("LocomotionMode", Locomotion.Hip, "Locomotion Mode");
        }
        private static void WaitForUiInit()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
                typeof(UIXManager).GetMethod("OnApplicationStart").Invoke(null, null);
            else
            {
                MelonLogger.Warning("UIExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
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
        public static bool CheckIfInFBT() => GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.SixPoint || GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 == IkController.IkType.FourPoint;

        public static VRCPlayer GetLocalPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        public static SteamVR_ControllerManager GetSteamVRControllerManager()
        {
            SteamVR_ControllerManager l_result = null;
            if (VRCInputManager.field_Private_Static_Dictionary_2_InputMethod_VRCInputProcessor_0?.Count > 0)
            {
                VRCInputProcessor l_input = null;
                l_input = VRCInputManager.field_Private_Static_Dictionary_2_InputMethod_VRCInputProcessor_0[VRCInputManager.InputMethod.Vive];
                if (l_input != null)
                {
                    VRCInputProcessorVive l_viveInput = l_input.TryCast<VRCInputProcessorVive>();
                    if (l_viveInput != null)
                        l_result = l_viveInput.field_Private_SteamVR_ControllerManager_0;
                }
            }
            return l_result;
        }
        static public void VRCTrackingManager_RestoreTrackingAfterCalibration() => m_Instance?.OnCalibrationEnd();
        void OnCalibrationEnd()
        {
            IsInFBT = true;
            if (GetTracker(HumanBodyBones.Hips) != null)
            {
                HipTransform = GetTracker(HumanBodyBones.Hips);
            }
            else
            {
                HipTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips);
            }
            if (GetTracker(HumanBodyBones.Chest) != null && GetTracker(HumanBodyBones.Chest) != HipTransform)
            {
                ChestTransform = GetTracker(HumanBodyBones.Chest);
            }
            else
            {
                ChestTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Chest);
            }
            OffsetHip = new();
            OffsetHip.transform.parent = HipTransform;
            OffsetHip.transform.rotation = Quaternion.Euler(0, headTransform.rotation.eulerAngles.y, 0);
            OffsetChest = new();
            OffsetChest.transform.parent = ChestTransform;
            OffsetChest.transform.rotation = Quaternion.Euler(0, headTransform.rotation.eulerAngles.y, 0);
        }
        static Transform GetTracker(HumanBodyBones bodyPart)
        {
            var puckArray = GetSteamVRControllerManager().field_Public_ArrayOf_GameObject_0;
            for (int i = 0; i < puckArray.Length - 2; i++)
            {
                if (FindAssignedBone(puckArray[i + 2].transform) == bodyPart)
                    return puckArray[i + 2].transform;
            }
            return HeadTransform;
        }
        public static readonly HumanBodyBones[] linkedBones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Chest
        };
        static HumanBodyBones FindAssignedBone(Transform trackerTransform)
        {
            HumanBodyBones result = HumanBodyBones.LastBone;
            // Find nearest bone
            float distance = float.MaxValue;
            foreach (HumanBodyBones bone in linkedBones)
            {
                Transform l_boneTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(bone);
                if (l_boneTransform != null)
                {
                    float l_distanceToPuck = Vector3.Distance(l_boneTransform.position, trackerTransform.position);
                    if (l_distanceToPuck < distance)
                    {
                        distance = l_distanceToPuck;
                        result = bone;
                    }
                }
            }
            return result;
        }

        private static int isInFBTTimer;

        private static bool IsInFBT;

        private static Transform HipTransform, ChestTransform;

        private static GameObject OffsetHip, OffsetChest;

        //Gets the head transform
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

        // Substitute the direction from the original method with our own
        public static void Prefix(ref Vector3 __0) { __0 = CalculateDirection(__0); }

        // Fixes the game's original direction to match the preferred one
        private static Vector3 CalculateDirection(Vector3 headVelo)
        {
            if (headVelo.magnitude > 0)
            {
                isInFBTTimer++;
                if(isInFBTTimer > 100) //Checks if player is no longer in FBT each 2 seconds
                {
                    isInFBTTimer = 0;
                    IsInFBT = CheckIfInFBT();
                }
                return LocomotionMode.Value switch
                {
                    Locomotion.Hip when IsInFBT && HipTransform != null => TrackerLoco(OffsetHip.transform),
                    Locomotion.Chest when IsInFBT && ChestTransform != null => TrackerLoco(OffsetChest.transform),
                    _ => HeadLoco(headVelo),
                };
            }
            else return Vector3.zero;
        }
        static Vector3 TrackerLoco(Transform trackerTransform)
        {
            Vector3 forward = trackerTransform.forward;
            forward.y = 0;
            Vector3 right = trackerTransform.right;
            right.y = 0;

            Vector3 trackerVelo = right.normalized * Input.GetAxis("Horizontal") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetStrafeSpeed() 
                    + forward.normalized * Input.GetAxis("Vertical") * GetLocalPlayer().field_Private_VRCPlayerApi_0.GetRunSpeed();
            if (PlayerMotionState.field_Private_Single_0 < 0.4f) trackerVelo *= 0.1f; //player prone at 40% of height: tenth speed
            else if (PlayerMotionState.field_Private_Single_0 < 0.65f) trackerVelo *= 0.5f; //player crouching at 65% of height: half speed
            return trackerVelo;
        }
        static Vector3 HeadLoco(Vector3 headVelo)
        {
            return Quaternion.FromToRotation(HeadTransform.up, Vector3.up) * HeadTransform.rotation * Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(Vector3.up, HeadTransform.forward), Vector3.up))) * headVelo;
        }
    }
    public enum Locomotion
    {
        Head,
        Hip,
        Chest
    }
}