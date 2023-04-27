Sophie's Armature Cleanup
=========================

Have you added a bunch of assets to your avatar, and now you have multiple
copies of each bone in the Armature?

This tool can clean all of that mess up!

[![Generic badge](https://img.shields.io/badge/Unity-2019.4.31f1-informational.svg)](https://unity3d.com/unity/whats-new/2019.4.31)
[![Generic badge](https://img.shields.io/badge/SDK-AvatarSDK3-informational.svg)](https://vrchat.com/home/download)

The process:

* Find all bones which are named the same as their parent - these "extra" bones
  will be "merged" into their parent
* Find all components of the following categories which exist on the "extra"
  bones and copy them to their parent bone:
	* AimConstraint
	* Animation
	* Animator
	* AudioSource
	* Camera
	* Cloth
	* Collider
	* FlareLayer
	* CharacterJoint
	* ConfigurableJoint
	* HingeJoint
	* FixedJoint
	* SpringJoint
	* Light
	* LineRenderer
	* LookAtConstraint
	* MeshFilter
	* MeshRenderer
	* ParentConstraint
	* ParticleSystem
	* ParticleSystemRenderer
	* PositionConstraint
	* Rigidbody
	* RotationConstraint
	* ScaleConstraint
	* TrailRenderer
	* VRCContactReceiver
	* VRCContactSender
	* VRCPhysBone
	* VRCPhysBoneCollider
	* VRCStation
	* VRCSpatialAudioSource
* Find all skinned mesh renderers which use "extra" bones, and re-assign the
  mesh to use the main avatar bones
* Update any existing or copied components of the following types, switching
  their use of the "extra" bones to the appropriate parent bone:
	* VRCContactSender
    * VRCContactReceiver
	* VRCPhysBone
	* VRCPhysBoneCollider
    * AimConstraint
	* LookAtConstraint
	* ParentConstraint
	* PositionConstraint
	* RotationConstraint
	* ScaleConstraint
	* Station
* Destroy the "extra" bones

### Caveats

This process currently only works if the bones of your "add-on" armature are
named *exactly* the same as the bones you parented them to.  Often you'll be
adding a totally different armature to your avatar.  So, you'll need to rename
all the bones to match the avatar's armature before attempting a merge like
this.

Yes, I'm considering how to actually do that :)

## Usage

Click on `Tools` > `SophieBlue` > `ArmatureCleanup` in the menu bar.  In the
window that opens, drag your avatar into the appropriate slot and click `Clean
up!`

I cannot stress this enough:

**MAKE A BACKUP BEFORE YOU RUN THIS TOOL**

This tool makes assumptions and guesses and may not work at all, or even may
cause permanent changes to your project.  I offer no guarantee and no support,
use this as your own risk.

## Installation

There are two methods, pick **only one**:

### UnityPackage

Install the unitypackage the usual way, from the menu bar in Unity, going
to `Assets` then `Import Package` then `Custom Package...` and selecting the
file.

### VPM

You can also use [VRChat's VPM tool](https://vcc.docs.vrchat.com/vpm/cli/)!
First add my [VPM Repository](https://github.com/SophieBlueVR/vpm-repos), and
then you can simply go to your project directory and type:

```
vpm add package io.github.sophiebluevr.armaturecleanup
```

## License

ArmatureCleanup is available as-is under MIT. For more information see
[LICENSE](/LICENSE.txt).
