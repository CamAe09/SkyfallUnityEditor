========================================
FORTNITE CHAPTER 3 STYLE INTRO SYSTEM
========================================

WHAT'S INCLUDED:
----------------
✓ Fade from black opening
✓ "Press P To Start New Decade" prompt
✓ Fake player with animation clips (laying down → wake up)
✓ Two-track music system with crossfades
✓ Player controller for walking to NPC
✓ Hold P to talk interaction
✓ Multiple camera transitions
✓ Map flyby with waypoints
✓ Battle Royale plane integration

FILES:
------
/Assets/Scripts/LiveEvent/SimpleEraIntroController.cs
- Main intro system controller

/Assets/Scripts/Cinematics/CinematicPlayerController.cs  
- WASD + Mouse movement during intro

/Assets/Scripts/Cinematics/LocomotionBlender.cs
- Handles animation blending between idle and walk

RECENT FIXES:
-------------
✓ Fixed WASD controls (W now goes forward, not backward)
✓ Fixed mouse controls (no longer inverted)
✓ Added gravity and ground snapping (player falls if floating, rises if underground)
✓ Fixed locomotion animations (proper idle/walk blending)
✓ Fixed flyby camera staying active (not skipping to loading screen)
✓ Fixed music timing - plane music now plays AFTER second voiceline
✓ Fixed premature loading screen during NPC dialogue (removed duplicate Battle Royale start)

QUICK SETUP:
-----------
1. Create empty GameObject "EraIntroController"
2. Add SimpleEraIntroController component
3. Assign in Inspector:
   - Player Prefab (with Animator + humanoid Avatar)
   - Player Spawn Point (empty GameObject)
   - 4 Animation Clips (laying, wake, idle, walk)
   - NPC GameObject with Animator
   - 4 Cinemachine Virtual Cameras
   - Flyby Waypoints (optional, can use airplane)
   - 2 Music Tracks (wake-up music, plane music)
   - 2 Voiceline Clips (NPC dialogue, "There's your stop!")

SEQUENCE:
---------
1. Fade from black
2. Player laying down - "Press P To Start New Decade"
3. Press P → wake up animation plays
4. Music Track 1 fades in (wake-up music)
5. Player can walk with WASD + Mouse
6. Walk to NPC, hold P to talk
7. NPC dialogue plays
8. Music Track 1 fades out
9. Battle Royale starts, plane spawns
10. Camera follows plane (flyby sequence)
11. "There's your stop!" voiceline plays (2 seconds in)
12. Music Track 2 fades in (after voiceline starts)
13. Player jumps, full game begins

MUSIC & VOICELINE TIMING:
------------------------
Track 1 (Wake-Up Music):
- Plays when player wakes up
- Continues during walk to NPC
- Fades out after NPC dialogue
- Calm, atmospheric recommended

Track 2 (Plane Music):
- Plays 2 seconds into airplane flyby
- Starts AFTER "There's your stop!" voiceline
- Continues during plane ride and into gameplay
- Upbeat, action music recommended

Voiceline Timing:
- First voiceline: During NPC dialogue camera
- Second voiceline: 2 seconds into flyby sequence
- Plane music starts 0.5 seconds after second voiceline begins

TESTING:
--------
Right-click SimpleEraIntroController component
→ "Force Trigger Intro (Test)"

This replays the entire sequence for testing.

PLAYER CONTROLS:
---------------
Phase 1 - Laying Down:
  P = Wake up

Phase 2 - Walking:
  W = Move Forward (FIXED!)
  A = Strafe Left
  S = Move Backward  
  D = Strafe Right
  Mouse = Look/Rotate (FIXED!)
  
Phase 3 - NPC:
  Hold P = Talk (2 seconds)
  
Phase 4 - Plane:
  Space = Jump
  Full game controls

PHYSICS & GRAVITY:
-----------------
✓ Player automatically falls if spawned in air
✓ Player rises if stuck underground
✓ CharacterController with proper ground snapping
✓ Smooth gravity application (20 m/s²)

CAMERA SETUP:
-------------
You need 4 Cinemachine Virtual Cameras:

1. Intro Cam
   - Wide shot of player laying down
   - Priority: 0 (will be 100 when active)

2. Player Follow Cam
   - Third-person follow
   - Add CinemachineTransposer + CinemachineComposer
   - Follow Offset: (0, 0.3, -3.5)

3. Dialogue Cam  
   - Close-up of NPC
   - Shows conversation

4. Flyby Camera
   - Follows airplane OR moves through waypoints
   - Shows map overview
   - Now stays active properly during flyby!

REQUIRED ANIMATIONS:
-------------------
1. Laying Down Idle - Character on ground (looping)
2. Wake Up - Transition from laying to standing
3. Idle - Standing idle for locomotion
4. Walk - Walking animation

All must be humanoid-compatible!

CUSTOMIZABLE SETTINGS:
---------------------
In Inspector:

Timing:
- Fade Duration (default: 2s)
- Music Fade Duration (default: 2s)  
- Hold Duration (default: 2s)
- Flyby Duration (default: 10s)

Audio:
- Wake Up Music Volume (default: 0.4)
- Plane Music Volume (default: 0.6)

Interaction:
- Interaction Distance (default: 3 units)

TROUBLESHOOTING:
---------------
Player T-posing?
→ Check Avatar is assigned to Animator
→ Ensure animations are humanoid-compatible

Intro not starting?
→ Use "Force Trigger Intro (Test)"
→ Check Console for [SimpleEraIntroController] logs

No movement or inverted controls?
→ FIXED! W goes forward, mouse rotation correct
→ Check ground has collision for CharacterController

Player floating or stuck in ground?
→ FIXED! Auto gravity and ground snapping enabled
→ Player falls/rises to correct ground position

Animations not playing (stuck in T-pose)?
→ FIXED! Locomotion system now uses playable graph blending
→ Idle and Walk animations blend automatically based on movement
→ Check that Idle and Walk clips are assigned in Inspector

Loading screen appears during NPC dialogue?
→ FIXED! Removed duplicate Battle Royale start call
→ Battle Royale now starts only after NPC dialogue completes
→ Sequence: Dialogue → Music fade → Start BR → Flyby → Gameplay

Music not playing?
→ Verify AudioClips assigned
→ Check Audio Sources not muted

Camera not switching?
→ Main Camera needs CinemachineBrain
→ All 4 cameras must be assigned

Flyby skipping to loading screen?
→ FIXED! Camera now stays active properly
→ Make sure Flyby Camera is assigned

Plane music plays too early?
→ FIXED! Now plays after second voiceline

TIPS FOR BEST RESULTS:
---------------------
Animation:
- Use smooth, natural transitions
- Test wake-up animation thoroughly
- Ensure standing pose at end of wake-up

Camera Work:
- Position waypoints to showcase map
- Vary heights and angles
- Tell visual story of the map
- Flyby camera will stay active for full duration

Music:
- Track 1: Wonder, discovery, mystery
- Track 2: Action, battle-ready, exciting
- Perfect timing: voiceline → music fade-in

Pacing:
- Let moments breathe
- Time dialogue to music
- Flyby should match map size
- Second voiceline plays 2 seconds into flyby

See /Pages/Fortnite Style Intro Setup Guide for complete details!

========================================
