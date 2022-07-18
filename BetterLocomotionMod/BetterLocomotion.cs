using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using MelonLoader;
using VRC.Animation;
using VRC.SDKBase;
using BuildInfo = BetterLocomotion.BuildInfo;
using Main = BetterLocomotion.Main;
using DecaSDK;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;

/*
 * A lot of code was taken from the BetterDirections mod
 * Special thanks to Davi
 * https://github.com/d-magit/VRC-Mods 
 */

[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: MelonInfo(typeof(Main), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.Magenta)]
[assembly: MelonOptionalDependencies("UIExpansionKit")]

namespace BetterLocomotion
{
    public static class BuildInfo
    {
        public const string Name = "BetterLocomotion";
        public const string Author = "Gay fox boy, Davi & AxisAngle";
        public const string Version = "1.3.1";
    }

    internal static class UIXManager { public static void OnApplicationStart() => ExpansionKitApi.OnUiManagerInit += Main.VRChat_OnUiManagerInit; }

    public class Main : MelonMod
    {
        private enum Locomotion { Head, Hip, Chest, Deca }
        internal static MelonLogger.Instance Logger;
        private static HarmonyLib.Harmony _hInstance;
        private static DecaMoveBehaviour deca;

        // Wait for Ui Init so XRDevice.isPresent is defined
        public override void OnApplicationStart()
        {
            Logger = LoggerInstance;
            _hInstance = HarmonyInstance;

            WaitForUiInit();
            InitializeSettings();
            OnPreferencesSaved();

            // Patches
            MethodsResolver.ResolveMethods(Logger);
            if (MethodsResolver.PrepareForCalibration != null)
                HarmonyInstance.Patch(MethodsResolver.PrepareForCalibration, null,
                    new HarmonyMethod(typeof(Main), nameof(VRCTrackingManager_StartCalibration)));
            if (MethodsResolver.RestoreTrackingAfterCalibration != null)
                HarmonyInstance.Patch(MethodsResolver.RestoreTrackingAfterCalibration, null,
                    new HarmonyMethod(typeof(Main), nameof(VRCTrackingManager_FinishCalibration)));

            Logger.Msg("Successfully loaded!");
        }

        public override void OnApplicationQuit()
        {
            if (deca != null) deca.OnDestroy();
        }

        private static MelonPreferences_Entry<Locomotion> _locomotionMode;
        private static MelonPreferences_Entry<float> _joystickThreshold;
        private static MelonPreferences_Entry<bool> _lolimotion;
        private static MelonPreferences_Entry<float> _lolimotionMinimum;
        private static MelonPreferences_Entry<float> _lolimotionMaximum;
        private static MelonPreferences_Entry<bool> _decaButton;

        private static void InitializeSettings()
        {
            MelonPreferences.CreateCategory("BetterLocomotion", "BetterLocomotion");

            _locomotionMode = MelonPreferences.CreateEntry("BetterLocomotion", "LocomotionMode", Locomotion.Head, "Locomotion mode");
            _joystickThreshold = MelonPreferences.CreateEntry("BetterLocomotion", "JoystickThreshold", 0f, "Joystick threshold (0-1)");
            _lolimotion = MelonPreferences.CreateEntry("BetterLocomotion", "Lolimotion", false, "Lolimotion (scale speed to height)");
            _lolimotionMinimum = MelonPreferences.CreateEntry("BetterLocomotion", "LolimotionMinimum", 0.5f, "Lolimotion: minimum height");
            _lolimotionMaximum = MelonPreferences.CreateEntry("BetterLocomotion", "LolimotionMaximum", 1.1f, "Lolimotion: maximum height");
            _decaButton = MelonPreferences.CreateEntry("BetterLocomotion", "DecaButton", false, "Show deca QM button");
        }

        public static void DecaCalibrate()
        {
            if (deca != null) deca.Calibrate();
        }

        private static IMenuButton decaButton;

        private static void WaitForUiInit()
        {
            if (MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit")))
            {
                decaButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Calibrate Deca", DecaCalibrate);
                decaButton.SetVisible(false);
                typeof(UIXManager).GetMethod("OnApplicationStart")!.Invoke(null, null);
            }
            else
            {
                Logger.Warning("UIExpansionKit (UIX) was not detected. Using coroutine to wait for UiInit. Please consider installing UIX.");
                static IEnumerator OnUiManagerInit()
                {
                    while (VRCUiManager.prop_VRCUiManager_0 == null)
                        yield return null;
                    VRChat_OnUiManagerInit();
                }
                MelonCoroutines.Start(OnUiManagerInit());
            }
        }

        private static bool _xrPresent;
        // Apply the patch
        public static void VRChat_OnUiManagerInit()
        {
            _xrPresent = XRDevice.isPresent;
            if (_xrPresent)
            {
                Logger.Msg("XRDevice detected. Initializing...");
                try
                {
                    foreach (MethodInfo info in typeof(VRCMotionState).GetMethods().Where(method =>
                        method.Name.Contains("Method_Public_Void_Vector3_Single_") && !method.Name.Contains("PDM")))
                        _hInstance.Patch(info, new HarmonyMethod(typeof(Main).GetMethod(nameof(Prefix))));

                    Logger.Msg("Successfully loaded!");
                }
                catch (Exception e)
                {
                    Logger.Warning("Failed to initialize mod!");
                    Logger.Error(e);
                }
            }
            else Logger.Warning("Mod is VR-Only.");
        }

        private static VRCPlayer GetLocalPlayer() => VRCPlayer.field_Internal_Static_VRCPlayer_0;

        private static SteamVR_ControllerManager GetSteamVRControllerManager()
        {
            var inputProcessor = VRCInputManager.field_Private_Static_Dictionary_2_InputMethod_VRCInputProcessor_0;
            if (!(inputProcessor?.Count > 0)) return null;
            VRCInputProcessor lInput = inputProcessor[VRCInputManager.InputMethod.Vive];
            if (lInput == null) return null;
            VRCInputProcessorVive lViveInput = lInput.TryCast<VRCInputProcessorVive>();

            SteamVR_ControllerManager lResult = null;
            if (lViveInput != null) lResult = lViveInput.field_Private_SteamVR_ControllerManager_0;
            return lResult;
        }

        private static bool CheckIfInFbt() => GetLocalPlayer().field_Private_VRC_AnimationController_0.field_Private_IkController_0.field_Private_IkType_0 is IkController.IkType.SixPoint or IkController.IkType.FourPoint;
        private static float GetAvatarScaledSpeed()
        {
            float minimum = Mathf.Clamp(_lolimotionMinimum.Value, 0.05f, 2f);
            float maximum = Mathf.Clamp(_lolimotionMaximum.Value, minimum, 5f);
            return Mathf.Clamp(VRCTrackingManager.field_Private_Static_Vector3_0.y, minimum, maximum) / maximum;
        }
        public override void OnUpdate()
        {
            if (_xrPresent)
            {
                if (_isCalibrating)
                {
                    getTrackerHip = GetTracker(HumanBodyBones.Hips) ?? GetTracker(HumanBodyBones.Chest);
                    getTrackerChest = GetTracker(HumanBodyBones.Chest);
                    _CalibrationSavingSaverTimer++;
                }

                if (_locomotionMode.Value == Locomotion.Deca && deca != null)
                {
                    deca.Update();
                }
            }
        }

        private static bool _decaDllLoaded;
        public override void OnPreferencesSaved()
        {
            if (_locomotionMode.Value == Locomotion.Deca)
            {
                if (!_decaDllLoaded)
                {
                    string dllName = "deca_sdk.dll";

                    try
                    {
                        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Main), dllName);
                        using var fileStream = File.Open("VRChat_Data/Plugins/" + dllName, FileMode.Create, FileAccess.Write);
                        resourceStream.CopyTo(fileStream);
                    }
                    catch (IOException ex)
                    {
                        MelonLogger.Warning("Failed to write native dll; will attempt loading it anyway. This is normal if you're running multiple instances of VRChat");
                        MelonDebug.Msg(ex.ToString());
                    }
                    _decaDllLoaded = true;
                }
                if (deca == null)
                {
                    deca = new DecaMoveBehaviour
                    {
                        Logger = Logger

                    };
                    Logger.Msg("Deca Created");
                }
                if (decaButton != null) decaButton.SetVisible(_decaButton.Value);
            }
            else
            {
                if (decaButton != null) decaButton.SetVisible(false);
            }
        }

        private static void VRCTrackingManager_StartCalibration()
        {
            _CalibrationSavingSaverTimer = 0;
            _isCalibrating = true;
        }

        // Gets the trackers or bones and creates the offset GameObjects
        private static void VRCTrackingManager_FinishCalibration()
        {
            _isInFbt = true;
            _isCalibrating = false;
            _avatarScaledSpeed = GetAvatarScaledSpeed();

            if (_CalibrationSavingSaverTimer > 6 || _offsetHip == null) // 6 frames for saved calibration (IKTweaks' universal calibration for example)
            {
                _hipTransform = getTrackerHip ?? GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips);
                _chestTransform = getTrackerChest ?? GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Chest);

                Quaternion rotation = Quaternion.FromToRotation(_headTransform.up, Vector3.up) * _headTransform.rotation;

                _offsetHip = new GameObject
                {
                    transform =
                {
                    parent = _hipTransform,
                    rotation = rotation
                }
                };
                _offsetChest = new GameObject
                {
                    transform =
                {
                    parent = _chestTransform,
                    rotation = rotation
                }
                };
            }
        }

        private static readonly HumanBodyBones[] LinkedBones = {
            HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.Chest
        };

        // Gets the SteamVR tracker for a certain bone
        private static Transform GetTracker(HumanBodyBones bodyPart)
        {
            var puckArray = GetSteamVRControllerManager().field_Public_ArrayOf_GameObject_0;
            for (int i = 0; i < puckArray.Length - 2; i++)
            {
                if (puckArray[i + 2].transform != null)
                {
                    if (FindAssignedBone(puckArray[i + 2].transform) == bodyPart)
                    {
                        return puckArray[i + 2].transform;
                    }
                }
            }
            return null;
        }
        private static HumanBodyBones FindAssignedBone(Transform trackerTransform) // Finds the nearest bone to the transform of a SteamVR tracker
        {
            HumanBodyBones result = HumanBodyBones.LastBone;
            float distance = float.MaxValue;
            foreach (HumanBodyBones bone in LinkedBones)
            {
                Transform lBoneTransform = GetLocalPlayer().field_Internal_Animator_0.GetBoneTransform(bone);
                if (lBoneTransform == null) continue;
                float lDistanceToPuck = Vector3.Distance(lBoneTransform.position, trackerTransform.position);
                if (!(lDistanceToPuck < distance)) continue;
                distance = lDistanceToPuck;
                result = bone;
            }
            return result;
        }

        private static bool _isInFbt, _isCalibrating;
        private static int _checkStuffTimer, _CalibrationSavingSaverTimer;
        private static float _avatarScaledSpeed = 1;
        private static GameObject _offsetHip, _offsetChest;
        private static Transform _headTransform, _hipTransform, _chestTransform, getTrackerHip, getTrackerChest;
        private static GamelikeInputController inputController = null;
        private static Transform HeadTransform => // Gets the head transform
            _headTransform ??= Resources.FindObjectsOfTypeAll<NeckMouseRotator>()[0].transform.Find(Environment.CurrentDirectory.Contains("vrchat-vrchat") ? "CenterEyeAnchor" : "Camera (eye)");

        // Substitute the direction from the original method with our own
        public static void Prefix(ref Vector3 __0) { __0 = CalculateDirection(); }

        // Fixes the game's original direction to match the preferred one
        private static Vector3 CalculateDirection()
        {
            if (_locomotionMode.Value == Locomotion.Deca && deca != null) deca.HeadTransform = HeadTransform;

            Vector3 @return = _locomotionMode.Value switch
            {
                Locomotion.Hip when _isInFbt && !_isCalibrating && _hipTransform != null => CalculateLocomotion(_offsetHip.transform),
                Locomotion.Chest when _isInFbt && !_isCalibrating && _chestTransform != null => CalculateLocomotion(_offsetChest.transform),
                Locomotion.Deca when deca != null && (deca.state == Move.State.Streaming) && deca.OutObject => CalculateLocomotion(deca.OutTransform),
                _ => CalculateLocomotion(HeadTransform),
            };

            _checkStuffTimer++;
            if (_checkStuffTimer <= 50) return @return; // doing this is ~60 times less heavy on performance than calling CheckIfInFbt() and GetAvatarScaledSpeed().

            _checkStuffTimer = 0;
            _isInFbt = CheckIfInFbt();
            _avatarScaledSpeed = GetAvatarScaledSpeed();
            return @return;
        }

        private static Vector3 CalculateLocomotion(Transform trackerTransform) // Thanks AxisAngle for the code!
        {
            // Get joystick inputs
            if (inputController == null) inputController = GetLocalPlayer().GetComponent<GamelikeInputController>();
            float inputX = inputController.field_Protected_VRCInput_0.field_Public_Single_0;
            float inputY = inputController.field_Protected_VRCInput_1.field_Public_Single_0;
            float inputMag = Mathf.Sqrt(inputX * inputX + inputY * inputY);

            // Early escape to avoid division by 0
            if (inputMag == 0.0f) return Vector3.zero;

            // Now we modulate the input magnitude to observe a deadzone
            float in0 = Mathf.Clamp(_joystickThreshold.Value, 0, 0.96f), in1 = 1.0f;
            float out0 = 0.0f, out1 = 1.0f;
            float inputMod = Mathf.Clamp(LinearMap(in0, in1, out0, out1, inputMag), out0, out1);

            // Check if inputMod is 0 to avoid weird bugs...
            if (inputMod == 0) return Vector3.zero;

            // Get player speeds
            float speedMod;
            VRCMotionState playerMotionState = GetLocalPlayer().gameObject.GetComponent<VRCMotionState>();
            if (playerMotionState.field_Private_Single_0 < 0.4f) speedMod = 0.1f;
            else if (playerMotionState.field_Private_Single_0 < 0.65f) speedMod = 0.5f;
            else speedMod = 1.0f;
            if (_lolimotion.Value == true) speedMod *= _avatarScaledSpeed;

            VRCPlayerApi playerApi = GetLocalPlayer().field_Private_VRCPlayerApi_0;
            float ovalWidth = inputMod * speedMod * playerApi.GetStrafeSpeed();
            float ovalHeight = inputMod * speedMod * playerApi.GetRunSpeed();

            // Compute the multiplier which moves the input onto the oval
            float t = TimeToOval(ovalWidth, ovalHeight, inputX, inputY);

            // Apply t to get a point on the oval and compute input direction with given inputs
            Vector3 inputDirection = t * (inputX * Vector3.right + inputY * Vector3.forward);
            return Quaternion.FromToRotation(trackerTransform.transform.up, Vector3.up) * trackerTransform.transform.rotation * inputDirection;
        }

        // Linear mapping
        private static float LinearMap(float x0, float x1, float y0, float y1, float x)
        {
            return ((x1 - x) * y0 + (x - x0) * y1) / (x1 - x0);
        }

        // Raycast from the center against an oval
        private static float TimeToOval(float w, float h, float dx, float dy)
        {
            // compute time of intersection time between ray d and the oval
            return 1.0f / Mathf.Sqrt(dx * dx / (w * w) + dy * dy / (h * h));
        }
    }
}
