# BetterLocomotion
A mod to enhance locomotion for VRChat using MelonLoader.  
Successor of [BetterDirections](https://github.com/d-magit/VRC-Mods).

## Features
- **Fixes the inconvenience that happens because of Euler angles when moving while looking up**, usually while laying down or cuddling, for example.
- **Allows you to set a threshold for movement to compensate for joystick drift** while keeping that smooth acceleration effect.
- **Choose between head, hip or chest locomotion.** This allows hip or chest locomotion, allowing you move towards your hip or chest instead of your head. *Just like Decamove but without the VRChat "head bias".*
- **Lolimotion**: option to slow down your movement speed according to the height of your avatar.
- Compatible with [IKTweaks](https://github.com/knah/VRCMods#iktweaks) and IK 2.0.
- Hip and chest locomotion works in 4-point tracking, 6-point tracking or more.
- Improve Decamove support. On top of moving towards your hip, you will also go faster towards your hip instead of your head when using decamove. (Thanks [ballfun](https://github.com/ballfn))

## Settings descriptions
- **Locomotion mode**: which reference should be used for locomotion (head, hip, chest or decamove)
- **Joystick threshold (0-1)**: prevents you from moving if your joystick's inclination is below that threshold. 0 being no threshold and 1 requiring you to tilt your joystick all the way to move.
- **Lolimotion (scale speed to height)**: toggles Lolimotion. Lolimotion is able to slow you down according to the height of your avatar.
- **Lolimotion: minimum height**: value at which Lolimotion will stop slowing down your avatar. Default: 0.5
- **Lolimotion: maximum height**: height at which Lolimotion will start slowing you down. Also is used to scale slowing. Default: 1.1  
   Feel free to experiment with values for Lolimotion. Speed with Lolimotion = (Avatar height clamped according to minimum and maximum) / maximum. Everything in meters.  
   Example: (0.3 clamped from 0.5 and 1.1 = 0.5) / 1.1 = 45.5%
- **Show deca QM button**: toggle the Decamove calibration button on the quick menu.

## Dependency
- [UIExpansionKit](https://github.com/knah/VRCMods#ui-expansion-kit) to change settings.

## Credit/Special thanks
- Davi's [BetterDirection](https://github.com/d-magit/VRC-Mods) mod (original mod)
- [AxisAngle](https://twitter.com/DonaldFReynolds) (for the logic with angles/movement)
- SDraw's [Calibration Lines Visualizer](https://github.com/SDraw/ml_mods) mod (code to find SteamVR trackers)
- Patchuuri for naming Lolimotion :3
- [ballfun](https://github.com/ballfn) for improved Decamove support.

## Installation
**Before installing: Modding is against VRChat's ToS. Be careful while using this as it modifies player movement! We are not responsible for any punishments.**  

Install [MelonLoader](https://melonwiki.xyz/#/) and drop [BetterLocomotion.dll](https://github.com/Louka3000/BetterLocomotion/releases/latest/download/BetterLocomotion.dll) in your mods folder.  
You will also want to install [UIExpansionKit](https://github.com/knah/VRCMods/releases/latest/download/UIExpansionKit.dll).  

**OR** you can use [VRCMelonAssistant](https://github.com/knah/VRCMelonAssistant/releases/latest/download/VRCMelonAssistant.exe) to automatically take care of the install process.
