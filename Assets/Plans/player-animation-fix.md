# Project Overview
- Game Title: Survival Pioneer
- High-Level Concept: An immersive 3D survival game with advanced character movement, resource harvesting, and combat.
- Players: Single-player.
- Target Platform: PC (StandaloneWindows64).
- Render Pipeline: URP (PC_RPAsset).

# Game Mechanics
## Core Gameplay Loop
Players explore the environment, gather resources (wood, stone), craft equipment, engage in melee and ranged combat, and manage survival stats (health, hunger, stamina).
## Controls and Input Methods
Keyboard & Mouse (WASD movement, mouse look, left-click for attack, right-click for block, hotbar switches) integrated via Unity's New Input System and ECM2 (Easy Character Movement 2).

# UI
Standard survival HUD with action bars, resource counters, health status, and interaction prompts.

# Key Asset & Context
- **Animator Controller**: `Assets/_Project/Animations/ProjectGKCCharacterController.controller`
- **Animator Driver**: `Assets/_Project/Scripts/Player/PlayerGkcAnimatorDriver.cs`
- **Player Controller**: `Assets/_Project/Scripts/Player/PlayerController.cs`
- **Avatar Masks**:
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Arms/Human Arm Left Mask.mask`
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Arms/Human Arm Right Mask.mask`
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Hands/Human Hand Left Mask.mask`
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Hands/Human Hand Right Mask.mask`
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Body Upper Mask.mask`
  - `Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Head Mask.mask`
- **Animation Clips**:
  - Male Strafe Walk/Run FBX assets in `Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/`
  - Unarmed Idle clip: `Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/HumanoidIdle.fbx`
  - Locomotion / Turn / Sprint clips in `Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/` and `Assets/Animations/Human Animations Melee/Human Animations/`
- **Editor Fixer Script**: `Assets/_Project/Editor/PlayerAnimatorFixer.cs` (to be created for automated fixes)

# Implementation Steps

### Step 1: Create the Automated PlayerAnimatorFixer Editor Script
- **Description**: Create an Editor script at `Assets/_Project/Editor/PlayerAnimatorFixer.cs` containing a menu item `Tools / Fix Player Animator`. This script will programmatically open the animator controller, load and assign the appropriate avatar masks to layers 3-7 and 9-10, and populate all missing/null clips in the locomotion and unarmed strafe/airborne blend trees with standard humanoid assets.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Assign Avatar Masks to Animator Layers (Automated)
- **Description**: Programmatically assign:
  - Layer 3 `[Right Arm]` -> `Human Arm Right Mask`
  - Layer 4 `[Left Arm]` -> `Human Arm Left Mask`
  - Layer 5 `[Right Hand]` -> `Human Hand Right Mask`
  - Layer 6 `[Left Hand]` -> `Human Hand Left Mask`
  - Layer 7 `[Upper Body]` -> `Human Body Upper Mask`
  - Layer 9 `[Head]` -> `Human Head Mask`
  - Layer 10 `[Upper Body With Movement]` -> `Human Body Upper Mask`
  - This stops override layers with empty/default states (weight = 1.0) from overriding/freezing the lower body and base locomotion animations.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 3: Populate Missing Blend Tree Animations (Automated)
- **Description**: Programmatically load standard humanoid movement assets and map them:
  - `Idle Type` (unarmed idle) -> Index 0 -> `HumanoidIdle`
  - `Normal` (sprint forward) -> Index 15 `{0, 2}` -> `HumanoidRun`
  - `Walk Strafe / Run Strafe` (Unarmed Weapon ID 0 in all strafe views) -> Populate 8-way directional slots with:
    - Forward `{0, 1}` -> `HumanM@Walk01_Forward` / `HumanM@Run01_Forward`
    - Backward `{0, -1}` -> `HumanM@Walk01_Backward` / `HumanM@Run01_Backward` (or backward run)
    - Left `{-1, 0}` -> `HumanM@StrafeWalk01_Left` / `HumanM@StrafeRun01_Left`
    - Right `{1, 0}` -> `HumanM@StrafeWalk01_Right` / `HumanM@StrafeRun01_Right`
    - Forward-Left `{-0.7, 0.7}` -> `HumanM@StrafeWalk01_ForwardLeft` / `HumanM@StrafeRun01_ForwardLeft`
    - Forward-Right `{0.7, 0.7}` -> `HumanM@StrafeWalk01_ForwardRight` / `HumanM@StrafeRun01_ForwardRight`
    - Backward-Left `{-0.7, -0.7}` -> `HumanM@StrafeWalk01_BackwardLeft` / `HumanM@StrafeRun01_BackwardLeft`
    - Backward-Right `{0.7, -0.7}` -> `HumanM@StrafeWalk01_BackwardRight` / `HumanM@StrafeRun01_BackwardRight`
  - `Airbone` (Airborne blend tree inside Airborne state) -> Programmatically populate empty slots by copying from the fully functional `Regular Air` sibling blend tree.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 4: Implement Running Banking/Leaning in PlayerGkcAnimatorDriver.cs
- **Description**: Add fields `_prevBodyYaw`, `_prevBodyYawInitialized`, and `_currentLeanTurn` to `PlayerGkcAnimatorDriver.cs`. In `UpdateLocomotion()`, when character is moving forward (`!driveStrafeTurnAxis` and `isMoving`), calculate body yaw rate and camera look yaw velocity delta to compute a dynamic `turnAmount` lean/bank. Also apply `forwardLeanForwardReduction` from the script settings. Smooth and pass this `turnAmount` into the Animator's `Turn` parameter.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

# Verification & Testing
- **Execution**: Run the `Tools / Fix Player Animator` command in the Unity menu and check the logs to verify all avatar masks and blend trees have been perfectly assigned.
- **Locomotion**: Enter Play Mode and test unarmed movement in all directions (WASD) to ensure strafe walking/running blends seamlessly without leg freezing or sliding.
- **Banking / Turning**: Test sprinting forward and making sharp turns (and looking left/right with the camera) to verify proper banking/leaning which dynamically blends standard run animations.
- **Weapon Attacks**: Perform weapon attacks (one-hand, two-hand, axe) and blocking to check that the upper body performs swings/blocks while the legs continue locomotion naturally.
- **Airborne**: Jump and fall off ledges to verify smooth transitions into jump/airborne animations.
