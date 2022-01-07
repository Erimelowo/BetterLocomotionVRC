# BetterLocomotion
A mod to enhance locomotion for VRChat using MelonLoader.  
Continuation of [BetterDirections](https://github.com/d-magit/VRC-Mods)

## Features
- **Fixes the inconvenience that happens because of Euler angles when moving while looking up**, usually while laying down or cuddling, for example.
- **Choose between head, hip or chest locomotion.** This allows hip or chest locomotion, allowing you move towards your hip or chest instead of your head. *Just like Decamove but without the VRChat "head bias".*
- Compatible with [IKTweaks](https://github.com/knah/VRCMods#iktweaks).
- Hip and chest locomotion is compatible with 4-point tracking, 6-point tracking or more (using IKTweaks).

## Settings
- **Locomotion mode**: which reference should be used for locomotion (head, hip or chest)
- **Use bones instead of trackers (not recommended)**: uses the rotation of your avatar's hip or chest when using hip/chest locomotion instead of the SteamVR tracker. Will not work on avatars with locomotion animations or inverted hip so only use if hip/chest tracker doesn't get detected properly.

## Dependency
- [UIExpansionKit](https://github.com/knah/VRCMods#ui-expansion-kit) to change settings.

## Credit/Special thanks
- Davi's [BetterDirection](https://github.com/d-magit/VRC-Mods) mod (original mod)
- [AxisAngle](https://twitter.com/DonaldFReynolds) (for the logic with angles/movement)
- SDraw's [Calibration Lines Visualizer](https://github.com/SDraw/ml_mods) mod (code to find SteamVR trackers)

## Installation
**Before installing: Modding is against VRChat's ToS. Be careful while using this as it modifies player movement! We are not responsible for any punishments.**  

Install [MelonLoader](https://melonwiki.xyz/#/) and drop [BetterLocomotion.dll](https://github.com/Louka3000/BetterLocomotion/releases/latest/download/BetterLocomotion.dll) in your mods folder.  
You will also want to install [UIExpansionKit](https://github.com/knah/VRCMods/releases/latest/download/UIExpansionKit.dll).  

**OR** you can use [VRCMelonAssistant](https://github.com/knah/VRCMelonAssistant/releases/latest/download/VRCMelonAssistant.exe) to automatically take care of the install process.
