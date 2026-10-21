#if UNITY_EDITOR
namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Which serialized fields to show when editing a shared profile asset in Genesis Studio.
    /// </summary>
    public enum DMStudioProfileSectionFilter
    {
        None = 0,
        ClimbOnly = 1,
        DashOnly = 2,
        SurvivalOnly = 3,
        LocomotionOnly = 4,
        FootstepsAudioOnly = 5,
        CombatAmmoOnly = 6,
        LandingHeightsOnly = 7,
        BuildingPreviewOnly = 8,
        BuildingSnapOnly = 9,
        BuildingPlacementOnly = 10,
        BuildingDoorOnly = 11,
        BuildingBuiltTintsOnly = 12,
        CombatCoreOnly = 13,
        LootChestTimersOnly = 14,
        LootChestPresentationOnly = 15,
        EnemyBrainChaseWallOnly = 16,
        ScopeOverlayOnly = 17,
        CombatThrowablesOnly = 18
    }

    internal static class DMStudioProfileSections
    {
        private static readonly string[] ScopeOverlayFields =
        {
            "scopeCrosshairSprite",
            "scopeOverlayUseSpriteCrosshair",
            "rifleScopeMaskRadius",
            "rifleScopeMaskScale",
            "rifleScopeCrosshairScale",
            "pistolScopeMaskRadius",
            "pistolScopeReticleScale",
            "useNewScopeArt",
            "retireUguiOptics",
            "reticleColor",
            "ringColor",
            "reticleLineWidth",
            "reticleCenterGap",
            "milDotsPerArm",
            "rifleScopeLabel",
            "pistolScopeLabel",
            "scopeHint",
            "rifleReticleColor",
            "rifleRingColor",
            "rifleOuterCrosshairColor",
            "rifleOuterCrosshairScale",
            "rifleOuterCrosshairEmission",
            "rifleOuterCrosshairEmissionColor",
            "rifleRingOuterDiameterPx",
            "pistolReticleColor",
            "pistolRingColor",
            "pistolOuterCrosshairColor",
            "pistolOuterCrosshairScale",
            "pistolOuterCrosshairEmission",
            "pistolOuterCrosshairEmissionColor",
            "pistolRingOuterDiameterPx"
        };

        private static readonly string[] FootstepsAudioFields =
        {
            "defaultFootsteps",
            "surfaceFootsteps",
            "terrainLayerFootsteps"
        };

        private static readonly string[] LocomotionFields =
        {
            "slowWalkSpeedMultiplier",
            "jogSpeedMultiplier",
            "sprintBurstSpeedMultiplier",
            "shiftDoubleTapWindow",
            "standingJumpHeight",
            "walkJumpHeightMultiplier",
            "jogJumpHeightMultiplier",
            "sprintJumpHeightMultiplier",
            "jumpHeightSpeedExponent",
            "leanOverrideEnabled",
            "leanWalkStrength",
            "leanRunStrength",
            "leanSmoothTime",
            "leanMaxValue",
            "leanSpineRollDegrees",
            "leanSpineRollWhileStrafing"
        };

        private static readonly string[] LandingHeightFields =
        {
            "heroDropMeters",
            "lethalDropMeters",
            "fallDamageStartMeters",
            "fallDamageLethalPercent",
            "fallDamageHealthFraction",
            "jetpackLethalDelay"
        };

        private static readonly string[] BuildingPreviewFields =
        {
            "validGhostMaterial",
            "ghostColor",
            "ghostAlpha",
            "blockedGhostMaterial",
            "blockedGhostColor",
            "blockedGhostAlpha"
        };

        private static readonly string[] BuildingBuiltTintFields =
        {
            "builtMaterial",
            "finishedColor",
            "glassColor",
            "glassAlpha"
        };

        private static readonly string[] BuildingSnapFields =
        {
            "largeModuleMeters",
            "smallModuleMeters",
            "yawStepDegrees",
            "heightStepMeters",
            "maxHeightOffsetMeters",
            "edgeSnapRangeMeters",
            "topSnapRangeMeters",
            "buildLookUpDegrees",
            "buildModeCameraDistanceMultiplier",
            "buildModeCameraExtraMeters",
            "doorFrameRangeMeters",
            "edgeFacingDot"
        };

        private static readonly string[] BuildingPlacementFields =
        {
            "buildSeconds",
            "destroyHoldSeconds",
            "aimDistanceMeters",
            "doorSeatDropMeters",
            "overlapPaddingMeters",
            "newBaseClearanceMeters",
            "basePowerSquareMeters",
            "generatorFuelRangeMeters",
            "generatorTankUnits",
            "generatorMinutesPerUnit",
            "powerLoadPeriodMinutes",
            "poweredItemUnitsPerPeriod",
            "forceFieldUnitsPerPeriod",
            "forceFieldsNeedPower",
            "lightsNeedPower",
            "requireBuildHub",
            "buildHubZoneMeters",
            "buildHubZoneHeightMeters",
            "buildHubZoneStepMeters",
            "buildHubZoneHeightStepMeters",
            "buildHubMaxLevel",
            "buildHubZoneMustBeClear",
            "terrainBootstrapDeployRangeMeters",
            "terrainBootstrapHubPieceId",
            "blockPatrolPaths",
            "patrolPathClearanceMeters",
            "patrolPathHeightToleranceMeters",
            "turretInteractRangeMeters",
            "turretPlayerCameraBackMeters",
            "turretPlayerCameraUpMeters",
            "turretPlayerCameraFov"
        };

        private static readonly string[] BuildingDoorFields =
        {
            "doorSwingDegrees",
            "doorSwingSeconds",
            "doorInteractRangeMeters",
            "gateSwingDegrees",
            "gateSwingSeconds"
        };

        private static readonly string[] LootChestTimerFields =
        {
            "relootWindowSeconds",
            "timerStart",
            "emptiedDissolveDelay",
            "postExitDestroySeconds",
            "fullInventoryHoldSeconds"
        };

        private static readonly string[] SurvivalFields =
        {
            "maxHealth",
            "maxEnergy",
            "maxStamina",
            "maxOxygen",
            "energyDrainPerSecond",
            "toolEnergyDrainPerSecond",
            "handCraftEnergyPerItem",
            "oxygenDrainPerSecond",
            "lowOxygenWarningPercent",
            "lowOxygenFlashPerSecond",
            "lowOxygenWarningText",
            "lowOxygenWarningEnabled",
            "healthDrainPerSecond",
            "lowStatThresholdPercent",
            "oxygenDepletedHealthDrainMultiplier",
            "enableHealthRegen",
            "healthRegenPerSecond",
            "healthRegenDelayAfterDamage",
            "maxThermalStress",
            "maxRadiation",
            "maxSulfur",
            "maxVolcano",
            "exposureRecoveryPerSecond",
            "thermalRecoveryPerSecond"
        };

        private static readonly string[] CombatThrowablesFields =
        {
            "enableGrenades",
            "grenadeCookSecondsBase",
            "grenadeCookSecondsMax",
            "grenadeLandingRingUsesSplashSkills",
            "grenadeThrowArcLineColor",
            "grenadeLandingRingColor",
            "grenadeLandingRingSizeMultiplier",
            "grenadeLandingRingIndicatorTexture",
            "grenadeCookOutSelfDamage",
            "grenadeEnemyStaggerSeconds",
            "grenadeHotbarLocalIndex",
            "hotCrossGrenadeLoadHoldSeconds",
            "grenadeExplosionRadius",
            "grenadeMinDamage",
            "grenadeMaxDamage",
            "grenadeInnerRadiusFullDamage",
            "grenadeEdgeDamageFalloff",
            "grenadeLandFuseSeconds",
            "grenadeExplosionForce",
            "grenadeExplosionUpwardsModifier",
            "grenadeExplosionVfxPrefab",
            "grenadeCameraShakeAmplitude",
            "grenadeCameraShakeDurationSeconds",
            "grenadeSplashDamagesPlayer",
            "grenadeSplashDamagesCompanions",
            "grenadePioneerSplashLayerMask",
            "grenadeVExplosiveDamageLayerMask",
            "grenadeVExplosiveForceLayerMask"
        };

        private static readonly string[] CombatCoreFields =
        {
            "playerSelfCollisionGuard", "playerSelfCollisionGuardInterval", "playerSelfCollisionForceRootTriggers", "playerSelfCollisionGuardLog",
            "playerCameraFixedStepInterpolation", "playerRotateViaRigidbody",
            "playerAnimatorPerFrameUpdate", "turretPooledLightRescanLegacy", "playerSnapProbeLog", "playerSnapProbeLogEveryCorrection", "terrainRescueSkipWhenGrounded",
            "trainingDummyPoiseMax",
            "humanoidPoiseMax",
            "humanoidPoiseStaggerSeconds",
            "blockStaggerSeconds",
            "parryStaggerSeconds",
            "blockParryStaggerImpulseScale",
            "parryStaggerImpulseBonus",
            "blockStaggerHitstopSeconds",
            "parryStaggerHitstopSeconds",
            "parryClashVfxPrefab",
            "parryClashVfxScale",
            "parryClashVfxLifetimeSeconds",
            "chargedHitShakeAmplitude",
            "chargedHitShakeDurationSeconds",
            "parryShakeAmplitude",
            "parryShakeDurationSeconds",
            "blockShakeAmplitude",
            "blockShakeDurationSeconds",
            "survivalHotbarLocalIndex",
            "poiseDamageFromHealthMultiplier",
            "maxPoiseDamageFractionPerHit",
            "poiseRegenPerSecond",
            "poiseRegenDelayAfterHit",
            "trainingDummyPositionSpring",
            "trainingDummyPositionDamping",
            "trainingDummyRotationSpring",
            "trainingDummyRotationDamping",
            "trainingDummyHitImpulse",
            "trainingDummyHitTorque",
            "trainingDummyMaxPositionOffset",
            "trainingDummyMaxRotationOffset",
            "trainingDummyPoiseStaggerImpulseScale",
            "dodgeIFrameSeconds",
            "dashIFrameSeconds",
            "statusMaxStacks",
            "statusImmunityWindowSeconds",
            "statusVfxNormalOffset",
            "statusBossMultiplier",
            "hitstopLightFrames",
            "enableMeleeHitstop",
            "hitstopCritBonusFrames",
            "hitstopHoldSpeed",
            "hitstopFreezesAttacker",
            "lightHitShakeAmplitude",
            "lightHitShakeDurationSeconds",
            "heavyHitShakeAmplitude",
            "heavyHitShakeDurationSeconds",
            "useDamageNumberTiers",
            "damageNumberNormalColor",
            "damageNumberCritColor",
            "damageNumberHeavyColor",
            "damageNumberCritScale",
            "damageNumberHeavyScale",
            "enableHitReactions",
            "hitRecoverySeconds",
            "hitRecoveryOnPoiseBreak",
            "hitRecoveryOnHeavyHit",
            "hitRecoveryOnChainStumble",
            "directionalFlinchEnabled",
            "swingSideLightA",
            "swingSideLightB",
            "swingSideLightC",
            "swingSideCharged",
            "swingTwistDegrees",
            "directionalFlinchDegrees",
            "directionalFlinchSeconds",
            "swingLateralBias",
            "lightBackstepMeters",
            "lightBackstepSeconds",
            "poiseBreakBackstepMeters",
            "chainReactionsEnabled",
            "hitChainWindowSeconds",
            "chainStrongHitCount",
            "chainStumbleHitCount",
            "chainStrongFlinchMultiplier",
            "chainStrongBackstepMeters",
            "chainStumbleSeconds",
            "chainStumbleBackstepMeters",
            "chainStumbleLeanDegrees",
            "bigReactionImmunitySeconds",
            "heavyReactionEnabled",
            "heavyBigReactionSeconds",
            "heavySlideMeters",
            "heavySlideSeconds",
            "heavyWaistBendDegrees",
            "heavyWaistBendSeconds",
            "heavyRespectsCommitment",
            "heavyFallbackScale",
            "hitSlideMaxLedgeDrop",
            "hitSlideMaxTotalMeters",
            "enableDirectionalReactionsV2",
            "swingDirectionSource",
            "bladeVelocityMinSpeed",
            "bladeVelocityWindowSeconds",
            "strikeLeanSideShare",
            "lightLeanDegrees",
            "lightTwistDegrees",
            "lightLeanAttackSeconds",
            "lightLeanSettleSeconds",
            "leanSpringFrequency",
            "leanSpringDamping",
            "leanSpineWeight",
            "leanChestWeight",
            "leanHeadWeight",
            "headWhipDelaySeconds",
            "headWhipMultiplier",
            "critLeanMultiplier",
            "leanHoldPeakThroughHitstop",
            "reactionDamageReference",
            "reactionDamageExponent",
            "reactionDamageScaleMin",
            "reactionDamageScaleMax",
            "heavyAttackWeightMultiplier",
            "hitstopWeightBonusFrames",
            "leanAngleJitterDegrees",
            "reactionIntensityRandomMin",
            "reactionIntensityRandomMax",
            "twistRandomDegrees",
            "twistSignFlipChance",
            "clipSpeedRandomMin",
            "clipSpeedRandomMax",
            "clipWeightRandomMin",
            "clipWeightRandomMax",
            "clipStartOffsetRandomMax",
            "avoidRepeatVariation",
            "heavyStumbleStepChance",
            "heavyStumbleStepMeters",
            "mediumUsesClip",
            "mediumClipWeight",
            "mediumClipSpeed",
            "mediumLeanScale",
            "mediumAltHeavyClipChance",
            "mediumAltHeavyClipWeight",
            "heavyUsesClip",
            "heavyClipWeight",
            "heavyClipSpeed",
            "heavyLeanDegrees",
            "heavyLeanSettleSeconds",
            "reactionBlendInSeconds",
            "reactionBlendOutSeconds",
            "knockdownEnabled",
            "knockdownChancePercent",
            "knockdownLowPoiseBonusPercent",
            "knockdownCritBonusPercent",
            "knockdownOnPoiseBreakPercent",
            "knockdownMaxChancePercent",
            "knockdownPushMeters",
            "knockdownPushSeconds",
            "knockdownFallSpeed",
            "knockdownFallExitFront",
            "knockdownFallExitBack",
            "knockdownFallExitLeft",
            "knockdownFallExitRight",
            "knockdownDownSeconds",
            "knockdownDownRandomSeconds",
            "getUpPlaybackSpeed",
            "getUpExitNormalized",
            "knockdownAimFadeInSeconds",
            "getUpInvulnerableSeconds",
            "knockdownImmunitySeconds",
            "knockdownReleasesAttackSlot",
            "knockdownMaxSlopeDegrees",
            "knockdownLedgeCheckMeters",
            "enemyAnimStallFailsafe",
            "enemyAnimStallSeconds",
            "enemyAnimStallMinSpeed",
            "enemyAnimStallLog",
            "downedComboEnabled",
            "downedComboReach",
            "downedComboConeDegrees",
            "downedComboDamageMultiplier",
            "downedComboBaseDamage",
            "downedComboStates",
            "downedComboWeaponSets",
            "downedComboBlendSeconds",
            "downedComboFallbackPitchDegrees",
            "downedComboPitchBlendSeconds",
            "downedComboLowStrike",
            "downedComboStrikeWindow",
            "downedComboFaceTarget",
            "downedComboFaceMaxDegrees",
            "downedComboLog",
            "aimRatingEnabled",
            "aimRatingRequireHit",
            "aimRatingHitWindowSeconds",
            "aimRatingScopedOnly",
            "aimRatingPerfect",
            "aimRatingGood",
            "aimRatingOk",
            "aimRatingDuration",
            "aimRatingScale",
            "aimRatingPopScale",
            "aimRatingOffset",
            "aimRatingFollowReticle",
            "aimRatingPerfectColor",
            "aimRatingGoodColor",
            "aimRatingOkColor",
            "aimRatingFontSize",
            "retireCritRagdollKnockdown",
            "logHitReactionsInPlay",
            "meleeComboTargetLock",
            "meleeTargetAcquireDistance",
            "meleeTargetReleaseDistance",
            "meleeTargetIntentConeHalfAngle",
            "meleeTargetAngleWeight",
            "meleeComboLockGraceSeconds",
            "meleeRetargetStickAngle",
            "meleeRetargetMinSeconds",
            "meleeRetargetStickMagnitude",
            "meleeAssistTurnSpeed",
            "meleeAssistSmoothTime",
            "meleeAssistMaxPerSwingDegrees",
            "meleeAssistWindupFraction",
            "meleeAssistSoftFadeDegrees",
            "meleeTargetIgnoreDowned",
            "lightComboForcesStrafe",
            "logMeleeFacing",
            "enablePlayerHitReactions",
            "playerLeanDegreesMin",
            "playerLeanDegreesMax",
            "playerLeanAttackSeconds",
            "playerLeanSettleSeconds",
            "playerHeadWhipScale",
            "playerLeanJitterDegrees",
            "playerIntensityJitter",
            "playerReactionIntensityMax",
            "playerClipSpeedMin",
            "playerClipSpeedMax",
            "playerClipWeightMin",
            "playerClipWeightMax",
            "playerHeavyDamageFraction",
            "playerPushMeters",
            "playerPushSeconds",
            "playerStumbleChance",
            "playerLightLockSeconds",
            "playerHeavyLockSeconds",
            "playerHeavyMoveScale",
            "playerKnockdownChance",
            "playerKnockdownBonusChance",
            "playerKnockdownChanceCap",
            "playerKnockdownCooldownSeconds",
            "playerDownHoldSecondsMin",
            "playerDownHoldSecondsMax",
            "playerDownedDamageScale",
            "playerGetUpIFrameSeconds",
            "playerRollCancel",
            "playerRollCancelAfterSeconds",
            "playerReactionsUnderGodMode",
            "playerAdsLeanScale",
            "logPlayerHitReactions",
            "meleeBlockTargetLock",
            "meleeBlockAssistTurnSpeed",
            "meleeBlockAssistSmoothTime",
            "meleeBlockLockGraceSeconds",
            "meleeBlockFacingToleranceDegrees",
            "enableChargedSlowMo",
            "chargedSlowMoChance",
            "chargedSlowMoCooldownSeconds",
            "chargedSlowMoSeconds",
            "chargedSlowMoTimeScale",
            "chargedSlowMoEaseInSeconds",
            "chargedSlowMoEaseOutSeconds",
            "chargedSlowMoReach",
            "chargedSlowMoConeHalfAngle",
            "enableParrySlowMo",
            "parrySlowMoChance",
            "parrySlowMoTotalSeconds",
            "parrySlowMoTimeScale",
            "parrySlowMoEaseInSeconds",
            "parrySlowMoEaseOutSeconds",
            "parrySlowMoThreatRadius",
            "parrySlowMoCooldownSeconds",
            "slowMoAllowInAudit",
            "logSlowMo",
            "playerHitSoundUnderGodMode",
            "hitstopHeavyFrames",
            "parryWindowSeconds",
            "playerCorpseStayInPlace",
            "playerCorpseSettleSeconds",
            "playerCorpseMaxSlideSpeed",
            "playerRespawnSettleSeconds",
            "playerRespawnPostStandUpLockSeconds",
            "playerRespawnPostStandUpSettleSeconds",
            "playerMeleeHitboxUseAuthoredSize",
            "enemyMeleeHitboxUseAuthoredSize",
            "meleeHitboxWidthScale",
            "meleeHitboxReachScale",
            "meleeBlockDefenseHalfAngle",
            "meleeAttackAutoFaceHalfAngle",
            "meleeBlockAutoFaceHalfAngle",
            "meleeBlockAutoFaceMaxDistance",
            "meleeBlockAutoFaceTurnSpeed",
            "enableCombatCameraSnap",
            "combatCameraSnapSeconds",
            "enableGlobalCombatSeparation",
            "playerCombatBufferRadius",
            "enemyMeleeHitboxWidthScale",
            "enemyMeleeHitboxReachScale",
            "enemyMeleeAttackRangeMultiplier",
            "enemyMeleeStandoffFraction",
            "enemyMeleeMaxOrbitFactor",
            "enemyIntentionalMeleeMissChance",
            "enemyMeleeMissStepSeconds",
            "enemyMeleeMissStepStopFactor",
            "enemyMeleeMissStepSpeedMultiplier",
            "lightComboAnimSpeedA",
            "lightComboAnimSpeedB",
            "lightComboAnimSpeedC",
            "enableLightComboAttackMove",
            "lightComboStrafeSpeedMultiplier",
            "lightComboForwardDriftMultiplier",
            "lockMovementDuringLightCombo",
            "lightRandomAnimSpeedA",
            "lightRandomAnimSpeedB",
            "lightRandomAnimSpeedC",
            "strongMeleeAnimSpeedA",
            "strongMeleeAnimSpeedC",
            "strongMeleeChargeSeconds",
            "strongMeleeDamageMultiplier",
            "strongMeleeDamageStartNormalized",
            "strongMeleeDamageEndNormalized",
            "strongMeleeAnimSpeedMultiplier",
            "strongReleaseWeightA",
            "strongReleaseWeightB",
            "strongReleaseWeightC",
            "interactHoldComboAnimSpeed",
            "chargedHitsIgnoreEnemyWeapons",
            "enableAttackInterrupts",
            "interruptBaseChanceLight",
            "interruptBaseChanceFollowUp",
            "interruptBaseChanceCharged",
            "interruptEarlyWindowNormalized",
            "interruptEarlyWindowGuaranteed",
            "interruptEarlyBonus",
            "interruptCritBonus",
            "interruptHeavyHitBonus",
            "interruptChainWindowSeconds",
            "interruptChainBonusPerHit",
            "interruptChainBonusMax",
            "interruptLowPoiseThreshold",
            "interruptLowPoiseBonus",
            "interruptLowHealthThreshold",
            "interruptLowHealthBonus",
            "interruptMaxChance",
            "interruptGuaranteedOnPoiseBreak",
            "interruptGuaranteedOnHeavyVsLight",
            "interruptCommitStartLight",
            "interruptCommitStartCharged",
            "interruptImmunitySeconds",
            "interruptRecoverySeconds",
            "interruptReactionSeconds",
            "logInterruptsInPlay",
            "logCombatEventsInPlay",
            "logEngagedHudFill"
        };

        private static readonly string[] CombatAmmoFields =
        {
            "itemName",
            "ammoType",
            "tooltipDescription",
            "rangedDamage",
            "rangedDamageRandomRange",
            "rangedRange",
            "projectileSpeed",
            "projectileSpreadDegrees",
            "weaponAccuracy",
            "closeRangeFullAccuracyDistance",
            "closeRangeSpreadScale",
            "fireRate",
            "rifleScopedFireInterval",
            "pistolScopedFireInterval",
            "rifleScopedFireIntervalPercentModifier",
            "pistolScopedFireIntervalPercentModifier",
            "shotsPerBurst",
            "burstFireRate",
            "magazineSize",
            "reloadTimeSeconds",
            "recoilVertical",
            "recoilHorizontal",
            "recoilFireRateScale",
            "ammoRecoilProfile",
            "isHitscanBeam",
            "isContinuousLaser",
            "projectileGravityScale",
            "splashRadius",
            "splashDamageFalloff",
            "statusEffectOverride",
            "statusEffectDamagePerTick",
            "statusEffectTickInterval",
            "statusEffectDuration",
            "statusEffectVfxPrefab",
            "projectilePrefab",
            "muzzleFlashPrefab",
            "tracerPrefab",
            "impactVfxPrefab",
            "beamVfxPrefab",
            "fireSound",
            "projectileTravelSound",
            "continuousLoopSound",
            "continuousStartSound",
            "continuousStopSound",
            "spawnLaserBurn",
            "useHitMarks",
            "defaultDecals",
            "defaultHitEffects",
            "surfaces",
            "fallBackToCatalog",
            "catalog",
            "surfaceDamage",
            "fireTrauma",
            "fireTraumaDurationSeconds",
            "impactTrauma",
            "impactTraumaDurationSeconds",
            "impactTraumaRadius",
            "impactTraumaMinDistance"
        };

        private static readonly string[] EnemyBrainChaseWallFields =
        {
            "enableChaseWallSlide",
            "chaseWallSlideRadius",
            "chaseWallSlideHeight",
            "chaseWallSlideSkin",
            "chaseWallStandoffMeters",
            "chaseWallBufferStuckSeconds",
            "chaseWallRetreatStepMeters",
            "chaseWallRetreatSidestepMeters",
            "chaseWallRetreatSidestepSeconds",
            "chaseWallSlidePasses",
            "chaseWallMinProgress",
            "chaseWallBlockedFramesBeforeFollow",
            "chaseWallProbeDistance",
            "chaseWallFollowFaceSlide",
            "chaseWallBlockedTurnRate",
            "chaseWallFollowMinSideSeconds",
            "chaseWallFollowSideHysteresis",
            "chaseWallFollowFlipAfterFails",
            "enableChaseWallPredictiveFan",
            "chaseWallFanRayCount",
            "chaseWallFanHalfAngleDegrees",
            "chaseWallFanLookAheadScale",
            "chaseWallFanLatchSeconds",
            "chaseWallFanDirectionHysteresis",
            "chaseWallFanMinRetargetSeconds",
            "chaseWallRingLockSeconds",
            "enableChaseWallLongRangeFan",
            "chaseWallLongRangeFanDistance",
            "chaseWallLongRangeFanRayCount",
            "chaseWallLongRangeFanBlend",
            "chaseWallStandoffPushMinDeficit",
            "chaseWallFollowRequiresHeadOnBlock",
            "chaseWallStuckFramesEarlyInvestigate",
            "chaseUnreachableMinVerticalDelta",
            "chaseUnreachableMaxHorizontal"
        };

        private static readonly string[] DashFields =
        {
            "dashStaminaCost",
            "dashStaminaTickExtraPercent",
            "dashStaminaRegenPerSecond",
            "dashRecoveryStaminaRegenPerSecond",
            "dashStaminaRecoverySeconds",
            "dashDoubleTapWindow",
            "dashDistance",
            "dashSpeed",
            "dashDuration",
            "dashCooldown",
            "dashAllowAirDash",
            "dashAnimationSpeed",
            "dashCollisionSkin",
            "dashDetachesFromClimb",
            "dashAllowedWhileClimbing",
            "dashHologramColor",
            "dashHologramEmission",
            "dashHologramMaterial",
            "dashStreakColor",
            "dashStreakCount",
            "dashStreakLifetime",
            "dashStreakSize",
            "dashStreakStretch",
            "dashStreakRadius",
            "dashStreakMaterial",
            "dashStreakPrefab",
            "dashSmokeColor",
            "dashSmokeCount",
            "dashSmokeLifetime",
            "dashSmokeSize",
            "dashSmokeMaterial",
            "dashSmokePrefab"
        };

        public static bool IncludesField(string propertyPath, DMStudioProfileSectionFilter filter)
        {
            if (filter == DMStudioProfileSectionFilter.None || string.IsNullOrEmpty(propertyPath))
                return true;

            if (propertyPath == "m_Script")
                return true;

            return filter switch
            {
                DMStudioProfileSectionFilter.SurvivalOnly => IsSurvivalField(propertyPath),
                DMStudioProfileSectionFilter.DashOnly => IsDashField(propertyPath),
                DMStudioProfileSectionFilter.LocomotionOnly => IsLocomotionField(propertyPath),
                DMStudioProfileSectionFilter.ClimbOnly =>
                    !IsSurvivalField(propertyPath)
                    && !IsDashField(propertyPath)
                    && !IsLocomotionField(propertyPath)
                    && !IsLandingHeightField(propertyPath),
                DMStudioProfileSectionFilter.FootstepsAudioOnly => IsFootstepsAudioField(propertyPath),
                DMStudioProfileSectionFilter.CombatAmmoOnly => IsCombatAmmoField(propertyPath),
                DMStudioProfileSectionFilter.CombatCoreOnly => IsCombatCoreField(propertyPath),
                DMStudioProfileSectionFilter.CombatThrowablesOnly => IsCombatThrowablesField(propertyPath),
                DMStudioProfileSectionFilter.LandingHeightsOnly => IsLandingHeightField(propertyPath),
                DMStudioProfileSectionFilter.BuildingPreviewOnly => IsBuildingPreviewField(propertyPath),
                DMStudioProfileSectionFilter.BuildingBuiltTintsOnly => IsBuildingBuiltTintField(propertyPath),
                DMStudioProfileSectionFilter.BuildingSnapOnly => IsBuildingSnapField(propertyPath),
                DMStudioProfileSectionFilter.BuildingPlacementOnly => IsBuildingPlacementField(propertyPath),
                DMStudioProfileSectionFilter.BuildingDoorOnly => IsBuildingDoorField(propertyPath),
                DMStudioProfileSectionFilter.LootChestTimersOnly => IsLootChestTimerField(propertyPath),
                DMStudioProfileSectionFilter.LootChestPresentationOnly => !IsLootChestTimerField(propertyPath),
                DMStudioProfileSectionFilter.EnemyBrainChaseWallOnly => IsEnemyBrainChaseWallField(propertyPath),
                DMStudioProfileSectionFilter.ScopeOverlayOnly => IsScopeOverlayField(propertyPath),
                _ => true
            };
        }

        public static string GetSectionNote(DMStudioProfileSectionFilter filter)
        {
            return filter switch
            {
                DMStudioProfileSectionFilter.SurvivalOnly =>
                    "Survival fields on DM_ClimbDashProfile — same asset as Climb and Dash tabs; Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.DashOnly =>
                    "Dash motion, stamina cost, hologram, streaks, and smoke on DM_ClimbDashProfile — climb tuning lives under Climb.",
                DMStudioProfileSectionFilter.ClimbOnly =>
                    "Wall attach, mantle, climb stamina, sprint stamina, steps & slopes (on-foot step height and walkable slope), and surface probes on DM_ClimbDashProfile — dash, survival, and locomotion have their own tabs.",
                DMStudioProfileSectionFilter.LocomotionOnly =>
                    "On-foot gaits on DM_ClimbDashProfile — slow walk, Shift jog, double-tap Shift sprint burst, and turn lean strength. Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.FootstepsAudioOnly =>
                    "Default fallback, Unity-tag, and terrain-layer 0-10 clip libraries on GameAudioProfile.",
                DMStudioProfileSectionFilter.CombatAmmoOnly =>
                    "Live ammo combat fields (Play Mode edits push to the drawn weapon each tick). Fire Rate / burst / reload / mag on the loaded ammo profile win when greater than zero; else the weapon ItemData is used. Scoped cadence: DM_ScopeProfile rifle/pistol fire interval wins; ammo Rifle/Pistol Scoped Fire Interval is fallback only when profile interval is unset (≤0). Ammo Rifle/Pistol Scoped Fire Interval Percent Modifier applies ±10% to that base (profile or fallback seconds), then Rifles scope skills, then min interval. Recoil Vertical/Horizontal are aim kick; fireTrauma / impactTrauma feed the CameraShake hub (0 = off). Rifle column on Ammo Recoil Profile still overrides two-hand weapons. Invector weapon recoilUp does nothing.",
                DMStudioProfileSectionFilter.CombatCoreOnly =>
                    "DM_CombatCoreProfile combat fields (Resources/Combat) except grenades — Combat Studio → Core / Melee groups Player vs Enemy; throwables live under Throwables.",
                DMStudioProfileSectionFilter.CombatThrowablesOnly =>
                    "Grenade / throwable tuning on DM_CombatCoreProfile — splash radius/damage, layer masks (0 = prefab), VFX/force/shake, cook fuse, landing ring. Live in Play.",
                DMStudioProfileSectionFilter.LandingHeightsOnly =>
                    "Jump/Landing 3-tier height band on DM_ClimbDashProfile (bounce / hero / hero+damage + jetpack grace) - same asset as Climb/Dash; Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.BuildingPreviewOnly =>
                    "Valid snap/build and blocked preview holograms on DM_BuildingGhostProfile — optional materials plus color and alpha.",
                DMStudioProfileSectionFilter.BuildingBuiltTintsOnly =>
                    "Finished mesh and window glass tints on DM_BuildingGhostProfile. Finishes (Right Alt) live per style in Building > Library — use Building Studio.",
                DMStudioProfileSectionFilter.BuildingSnapOnly =>
                    "Module grid, yaw/height steps, edge/top snap, and build look-up on DM_BuildingGhostProfile.",
                DMStudioProfileSectionFilter.BuildingPlacementOnly =>
                    "Hold to build/destroy, aim distance, overlap padding, and door frame seat on DM_BuildingGhostProfile.",
                DMStudioProfileSectionFilter.BuildingDoorOnly =>
                    "Built stone door swing and interact range on DM_BuildingGhostProfile.",
                DMStudioProfileSectionFilter.LootChestTimersOnly =>
                    "World Reloot window, emptied dissolve delay, Single Loot removal, and the 30-minute full-inventory hold on DM_LootChestProfile (Resources/Loot). Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.LootChestPresentationOnly =>
                    "Lid timing, loot window delay, interact and auto-close range, storage defaults, dissolve look, and the enemy drop box prefab on DM_LootChestProfile. Timers have their own tab.",
                DMStudioProfileSectionFilter.EnemyBrainChaseWallOnly =>
                    "NavMesh-free chase wall slide: standoff buffer, retreat, predictive fan, hysteresis, and stuck → investigate on DM_EnemyBrainProfile. Full brain tuning lives under AI & awareness.",
                DMStudioProfileSectionFilter.ScopeOverlayOnly =>
                    "UITK scope overlay cosmetics on DM_ScopeProfile — reticle / ring / outer mil-dot colors, emission, outer viewport mask (dims outside ring outer edge), ring diameter. Gameplay zoom, cadence, sway, accuracy, and damage still use profile bases + Rifles scope skills (skill_scope_*).",
                _ => string.Empty
            };
        }

        private static bool IsScopeOverlayField(string propertyPath) => MatchesField(propertyPath, ScopeOverlayFields);

        private static bool IsEnemyBrainChaseWallField(string propertyPath)
        {
            for (int i = 0; i < EnemyBrainChaseWallFields.Length; i++)
            {
                if (propertyPath == EnemyBrainChaseWallFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsFootstepsAudioField(string propertyPath)
        {
            for (int i = 0; i < FootstepsAudioFields.Length; i++)
            {
                if (propertyPath == FootstepsAudioFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsLandingHeightField(string propertyPath)
        {
            for (int i = 0; i < LandingHeightFields.Length; i++)
            {
                if (propertyPath == LandingHeightFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsLocomotionField(string propertyPath)
        {
            if (propertyPath.StartsWith("strafeLoco", System.StringComparison.Ordinal) || propertyPath.StartsWith("bodyWeight", System.StringComparison.Ordinal)) // 2026-10-20c strafe director knobs; 2026-10-20n body weight
                return true;

            for (int i = 0; i < LocomotionFields.Length; i++)
            {
                if (propertyPath == LocomotionFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsSurvivalField(string propertyPath)
        {
            for (int i = 0; i < SurvivalFields.Length; i++)
            {
                if (propertyPath == SurvivalFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsCombatAmmoField(string propertyPath)
        {
            return MatchesField(propertyPath, CombatAmmoFields);
        }

        private static bool IsCombatCoreField(string propertyPath)
        {
            return MatchesField(propertyPath, CombatCoreFields);
        }

        private static bool IsCombatThrowablesField(string propertyPath)
        {
            return MatchesField(propertyPath, CombatThrowablesFields);
        }

        private static bool MatchesField(string propertyPath, string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                if (propertyPath == field || propertyPath.StartsWith(field + ".", System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsDashField(string propertyPath)
        {
            for (int i = 0; i < DashFields.Length; i++)
            {
                if (propertyPath == DashFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsBuildingPreviewField(string propertyPath) => MatchesField(propertyPath, BuildingPreviewFields);

        private static bool IsBuildingBuiltTintField(string propertyPath) => MatchesField(propertyPath, BuildingBuiltTintFields);

        private static bool IsBuildingSnapField(string propertyPath) => MatchesField(propertyPath, BuildingSnapFields);

        private static bool IsBuildingPlacementField(string propertyPath) => MatchesField(propertyPath, BuildingPlacementFields);

        private static bool IsBuildingDoorField(string propertyPath) => MatchesField(propertyPath, BuildingDoorFields);

        private static bool IsLootChestTimerField(string propertyPath) => MatchesField(propertyPath, LootChestTimerFields);
    }
}
#endif
