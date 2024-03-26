# Pico Experiments

This is a skeleton for experiments using the Pico 4 Enterprise HMD.

It contains following demos:
- Monty Hall Game
- AI sales avatar for 3D printers
- Car showroom
- Cybersickness questionnaire
- Supermarket

## Setup
- To check out the `.fbx` files run `git lfs pull`
- Open Unity and select Android Platform to build the APK

## Face Tracking

![ft-manager-settings.png](ft-manager-settings.png)

To enable Face Tracking, you need to pick a Face Tracking Mode on PXR_Manager.

Hybrid: Enable face tracking and lipsync. Uses all 52 blend shapes and 20 visemes.

Face Only: Enable face tracking only. Uses all 52 blend shapes.

Lipsync Only: Enable lipsync only. Uses all 20 visemes.


## Inverse Kinematics

### Howto Animated

- Arms: https://www.youtube.com/watch?v=tBYl-aSxUe0
- Legs: https://youtu.be/Wk2_MtYSPaM
- Walk: https://youtu.be/8REDoRu7Tsw

- New Version: https://www.youtube.com/watch?v=v47lmqfrQ9s&t=200s

### Howto Sinoid

- Part 1: https://www.youtube.com/watch?v=MYOjQICbd8I
- Part 2: https://www.youtube.com/watch?v=1Xr3jB8ik1g

# Readyplayerme hints
- Right morph targets and quality: https://models.readyplayer.me/649716ff38ad7f783a122407.glb?quality=low&textureAtlas=none&morphTargets=ARKit,Oculus%20Visemes,mouthSmile
- Comparison with VR upper-half avatars as possible extension: https://vr.readyplayer.me/

Failed API-Calls:
- No morph targets: https://models.readyplayer.me/649716ff38ad7f783a122407.glb?quality=high?morphTargets=ARKit,Oculus%20Visemes
- Wrong morph targets: https://models.readyplayer.me/649716ff38ad7f783a122407.glb?quality=low&textureAtlas=none&morphTargets=ARKit,Oculus%20Visemes
