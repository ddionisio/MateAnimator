MateAnimator
============

Animator++

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use DOTween: http://dotween.demigiant.com/index.php

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Mega Morph has been removed.

Import/Export, Code Generator are currently disabled. 

### Dependencies ###
* DOTween v1.1.710


Updates
=======
* Refactored to use custom serialization, all tracks and keys are no longer MonoBehaviours. This improved stability significantly, and reduces overhead.
* Changed how Meta works, improving its stability.
* Added ScaleTrack
* Changed EventTrack to allow targets to be any Object. EventKey no longer stores Component. This allows for invoking functions from other objects such as: GameObject and ScriptableObjects.
* Removed TriggerTrack, use EventTrack instead. You can use TriggerSignal to emulate TriggerTrack.
* You can now paste keys to multiple tracks of the same type.
* Various fixes.
* A bunch of clean ups.

Known Issues
============
* CameraSwitcher Track is currently not working. This needs to be completely overhauled.

Installation
============
* Make sure to get DOTween and install to your project: http://dotween.demigiant.com/download.php
  * If you are using the Hyper Compatible version, make sure to add DOTWEEN_HYPER_COMPATIBLE in the player settings
  * Go to Edit->Project Settings->Player
  * Add **DOTWEEN_HYPER_COMPATIBLE** in Scripting Define Symbols
* Clone this project to your Assets folder (or Plugins if you are scripting in javascript).  If your project is already setup for Git, then clone this project as a submodule.
* Open your project, you should be able to see "M8/Animator" on the menu, or M8/Animate in the 'Add Component' droplist.

TODO
====
* Track duplicate interface.
* Unify pathing of position track to all other tracks.
* Alternate Meta by using ID component (easier, less human error, but with a little bit more overhead)
* Allow typing of function for SendMessage event key.
* Import/Export - AnimateMeta sort of does this already, but adding a JSON format wouldn't hurt.
* Add a way to make this tool extensive - will help with implementing tk2d, etc.
* Work with MechAnim. - allow change state, etc. and preview.
* Work with Particles - allow play/pause/stop and preview.
* Scene preview for Material property track.
* Any other glitches with mouse events.
* Camera transition fixes (too glitchy, maybe do something like UE4's director)

DEBUG
=====
Add "MATE\_DEBUG_ANIMATOR" (without quotes) in Player Settings -> Scripting Define Symbols.

For now it only shows a trace message whenever the GameObject "_animdata" is destroyed.

License
=======
* MateAnimator is licensed under a Creative Commons Attribution-NonCommercial 3.0 Unported License
Note: You may use the extension in commercial games and other Unity projects. This license applies to the source code of the extension only.
  - http://creativecommons.org/licenses/by/3.0/
