Sophie's Armature Cleanup
=========================

Have you added a bunch of assets to your avatar, and now you have multiple
copies of each bone in the Armature?

This tool can clean all of that mess up!

[![Generic badge](https://img.shields.io/badge/Unity-2019.4.31f1-informational.svg)](https://unity3d.com/unity/whats-new/2019.4.31)
[![Generic badge](https://img.shields.io/badge/SDK-AvatarSDK3-informational.svg)](https://vrchat.com/home/download)

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
