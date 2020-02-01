# MateAnimator

Animator++

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use DOTween: http://dotween.demigiant.com/index.php

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Mega Morph has been removed.

Import/Export, Code Generator are currently disabled. 

## Dependencies
* DOTween v1.2.283


## Updates
### 1-30-20
* Seems like using SerializeReference is screwing with object references with prefabs (I suspect that Unity doesn't seem to know how to properly update the object references in SerializeReference when instantiating a prefab). Serialization is reverted back to the old way.
### 1-28-20
* Updated serialization to use SerializeReference with Unity 2019.3. Tracks and keys are now directly serialized in Takes. The animator will convert the old serialization if it exists (I plan to move this to M8AnimatorUpgrade, and remove the old serialization). Hopefully this will resolve some of the strange issues that occur with undo/redo/prefab.
### Previous
* Changed EventTrack, you can now select functions from any Component if target is a GameObject.
* No longer need to setup DOTween Preferences.
* loop parameter now works for Play functions.
* You can play/goto takes during runtime, useful for debugging.

## Known Issues
* Event Tracker - if you can't find the function for a component, make sure the target is the said component. Simply drag the component you want to the target. I will revamp this feature sometime in the near feature.
* CameraSwitcher Track is currently not working. This needs to be completely overhauled.

## Installation
* Make sure to get DOTween and install to your project: http://dotween.demigiant.com/download.php
  * If you are using the Hyper Compatible version, make sure to add DOTWEEN_HYPER_COMPATIBLE in the player settings
  * Go to Edit->Project Settings->Player
  * Add **DOTWEEN_HYPER_COMPATIBLE** in Scripting Define Symbols
* Clone this project to your Assets folder.  If your project is already setup for Git, then clone this project as a submodule.
* Open your project, you should be able to see "M8/Animator" on the menu, or M8/Animate in the 'Add Component' droplist.

## Upgrade From Previous
Grab [MateAnimatorUpgrade](https://github.com/ddionisio/MateAnimatorUpgrade) and check its README.md for further instructions.

## TODO
* Package installation via Unity 2019.3's Package Manager through Github.
* Track duplicate interface.
* Unify pathing of position track to all other tracks.
* Alternate Meta by using ID component (easier, less human error, but with a little bit more overhead)
* Allow typing of function for SendMessage event key.
* Import/Export - AnimateMeta sort of does this already, but adding a JSON format wouldn't hurt.
* Add a way to make this tool extensive - will help with implementing tk2d, etc.
* Work with MechAnim. - allow change state, etc. and preview.
* Work with Particles - allow play/pause/stop and preview.
* Scene preview for Material property track.
* Camera transition fixes (too glitchy, maybe do something like UE4's director)

## DEBUG
You can check the serialized data by setting the Inspector mode to Debug:
* Select 'Debug' via the dropdown menu on the upper right header of the Inspector window.
* You can see all the tracks/keys in "Serialize Data".

## License
MateAnimator is licensed under a Creative Commons Attribution-NonCommercial 3.0 Unported License
Note: You may use the extension in commercial games and other Unity projects. This license applies to the source code of the extension only.
  - http://creativecommons.org/licenses/by/3.0/
