using System.Collections.Generic;
using Project.Data;

namespace Project.Player
{
    /// <summary>
    /// Maps extracted GKC transition rows to gameplay combat actions.
    /// Action IDs verified against ProjectGKCCharacterController Base Layer Any State transitions.
    /// </summary>
    public static class GkcActionCatalogClassifier
    {
        public static GkcCombatAction Classify(string stateName, int actionId)
        {
            if (string.IsNullOrWhiteSpace(stateName))
                return GkcCombatAction.None;

            string normalized = stateName.Trim();

            if (normalized.StartsWith("Punch "))
                return ClassifyPunch(normalized);

            if (normalized.Contains("Sword Attack 1 Hand"))
                return ClassifySword1H(normalized);

            if (normalized.Contains("Axe Attack 1 Hand"))
                return ClassifyAxe1H(normalized);

            if (normalized.Contains("Sword Attack 2 Hands"))
                return ClassifySword2H(normalized);

            if (normalized.Contains("Block With Sword") || normalized == "Block With Axe")
                return GkcCombatAction.Block;
            if (normalized == "Keep Sword 1 Hand")
                return GkcCombatAction.Charge1H;
            if (normalized == "Keep Sword 2 Hands")
                return GkcCombatAction.Charge2H;
            if (normalized == "Keep Axe")
                return GkcCombatAction.ChargeAxe;
            if (normalized == "Hit Damage Reflection")
                return GkcCombatAction.HitReactionUnarmed;
            if (normalized == "Melee Weapon Hit Reaction")
                return GkcCombatAction.HitReactionArmed;
            if (normalized.StartsWith("Dead"))
                return GkcCombatAction.Death;
            if (normalized == "Get Up From Back")
                return GkcCombatAction.GetUp;

            return ClassifyByKnownActionId(actionId);
        }

        public static GkcWeaponKind ResolveWeaponFilter(GkcCombatAction action)
        {
            return action switch
            {
                GkcCombatAction.Punch1 or GkcCombatAction.Punch2 or GkcCombatAction.Punch3
                    or GkcCombatAction.Punch4 or GkcCombatAction.Punch5
                    or GkcCombatAction.HitReactionUnarmed => GkcWeaponKind.Unarmed,
                GkcCombatAction.Sword1HCombo1 or GkcCombatAction.Sword1HCombo2 or GkcCombatAction.Sword1HCombo3
                    or GkcCombatAction.Sword1HCombo4 or GkcCombatAction.Sword1HPower or GkcCombatAction.Charge1H
                    => GkcWeaponKind.OneHandSword,
                GkcCombatAction.Axe1HCombo1 or GkcCombatAction.Axe1HCombo2 or GkcCombatAction.Axe1HCombo3
                    or GkcCombatAction.Axe1HCombo4 or GkcCombatAction.Axe1HPower or GkcCombatAction.ChargeAxe
                    => GkcWeaponKind.OneHandAxe,
                GkcCombatAction.Sword2HCombo1 or GkcCombatAction.Sword2HCombo2 or GkcCombatAction.Sword2HCombo3
                    or GkcCombatAction.Sword2HCombo4 or GkcCombatAction.Sword2HPower or GkcCombatAction.Charge2H
                    => GkcWeaponKind.TwoHand,
                _ => GkcWeaponKind.Infer
            };
        }

        public static List<GkcActionCatalogEntry> BuildManualSeedEntries()
        {
            return new List<GkcActionCatalogEntry>
            {
                // 1H sword — Action ID only (no Action Active on Base Layer Any State)
                MeleeEntry(GkcCombatAction.Sword1HCombo1, 4100, "Sword Attack 1 Hand Full Body 1", GkcWeaponKind.OneHandSword),
                MeleeEntry(GkcCombatAction.Sword1HCombo2, 4101, "Sword Attack 1 Hand 2", GkcWeaponKind.OneHandSword),
                MeleeEntry(GkcCombatAction.Sword1HCombo3, 4102, "Sword Attack 1 Hand 3", GkcWeaponKind.OneHandSword),
                MeleeEntry(GkcCombatAction.Sword1HCombo4, 4104, "Sword Attack 1 Hand 4", GkcWeaponKind.OneHandSword),
                MeleeEntry(GkcCombatAction.Sword1HPower, 4108, "Sword Attack 1 Hand Special 1", GkcWeaponKind.OneHandSword, GkcAnimatorConstants.DefaultPowerActionDuration),

                // 2H sword
                MeleeEntry(GkcCombatAction.Sword2HCombo1, 4200, "Sword Attack 2 Hands Body 1", GkcWeaponKind.TwoHand),
                MeleeEntry(GkcCombatAction.Sword2HCombo2, 4201, "Sword Attack 2 Hands Body 2", GkcWeaponKind.TwoHand),
                MeleeEntry(GkcCombatAction.Sword2HCombo3, 4202, "Sword Attack 2 Hands Body 3", GkcWeaponKind.TwoHand),
                MeleeEntry(GkcCombatAction.Sword2HCombo4, 4203, "Sword Attack 2 Hands Body 4", GkcWeaponKind.TwoHand),
                MeleeEntry(GkcCombatAction.Sword2HPower, 4204, "Sword Attack 2 Hands Special 1", GkcWeaponKind.TwoHand, GkcAnimatorConstants.DefaultPowerActionDuration),

                // 1H axe
                MeleeEntry(GkcCombatAction.Axe1HCombo1, 700, "Axe Attack 1 Hand 1", GkcWeaponKind.OneHandAxe),
                MeleeEntry(GkcCombatAction.Axe1HCombo2, 710, "Axe Attack 1 Hand 2", GkcWeaponKind.OneHandAxe),
                MeleeEntry(GkcCombatAction.Axe1HCombo3, 720, "Axe Attack 1 Hand 3", GkcWeaponKind.OneHandAxe),
                MeleeEntry(GkcCombatAction.Axe1HCombo4, 770, "Axe Attack 1 Hand 4", GkcWeaponKind.OneHandAxe),
                MeleeEntry(GkcCombatAction.Axe1HPower, 780, "Axe Attack 1 Hand Special 1", GkcWeaponKind.OneHandAxe, GkcAnimatorConstants.DefaultPowerActionDuration),

                // Block — Action ID routes on Upper Body With Movement Any State (no direct CrossFade)
                BlockEntry(GkcWeaponKind.OneHandSword, 1000, "Block With Sword 2 Hands", strafe: false),
                BlockEntry(GkcWeaponKind.TwoHand, 2100, "Block With Axe", strafe: false),
                BlockEntry(GkcWeaponKind.OneHandAxe, 2100, "Block With Axe", strafe: false),

                // Charge poses — no Action ID route; CrossFade on upper body
                CrossFadeEntry(GkcCombatAction.Charge1H, "Keep Sword 1 Hand", GkcAnimatorConstants.UpperBodyChargeLayer),
                CrossFadeEntry(GkcCombatAction.Charge2H, "Keep Sword 2 Hands", GkcAnimatorConstants.UpperBodyChargeLayer),
                CrossFadeEntry(GkcCombatAction.ChargeAxe, "Keep Axe", GkcAnimatorConstants.UpperBodyChargeLayer, GkcWeaponKind.OneHandAxe),

                // Hit reactions live under Base Layer > Hit Reaction > {sub-SM} > {state}
                HitReactionEntry(
                    GkcCombatAction.HitReactionUnarmed,
                    "Hit Reaction.Hit Damage Reflection.Hit Damage Reflection",
                    GkcWeaponKind.Unarmed),
                HitReactionEntry(
                    GkcCombatAction.HitReactionArmed,
                    "Hit Reaction.Regular Hit Reaction 1.Regular Hit Reaction 1",
                    GkcWeaponKind.Infer),
                Entry(GkcCombatAction.Death, 1535, "Dead 2", 4f),

                // Unarmed punches share Action ID range with sword — weapon filter disambiguates in TryGet
                MeleeEntry(GkcCombatAction.Punch1, 4100, "Punch 1", GkcWeaponKind.Unarmed),
                MeleeEntry(GkcCombatAction.Punch2, 4101, "Punch 2", GkcWeaponKind.Unarmed),
                MeleeEntry(GkcCombatAction.Punch3, 4102, "Punch 3", GkcWeaponKind.Unarmed),
                MeleeEntry(GkcCombatAction.Punch4, 4104, "Punch 4", GkcWeaponKind.Unarmed),
                MeleeEntry(GkcCombatAction.Punch5, 4105, "Punch 5", GkcWeaponKind.Unarmed, GkcAnimatorConstants.DefaultPowerActionDuration),
            };
        }

        private static GkcCombatAction ClassifyPunch(string normalized)
        {
            if (normalized.Contains("5")) return GkcCombatAction.Punch5;
            if (normalized.Contains("4")) return GkcCombatAction.Punch4;
            if (normalized.Contains("3")) return GkcCombatAction.Punch3;
            if (normalized.Contains("2")) return GkcCombatAction.Punch2;
            return GkcCombatAction.Punch1;
        }

        private static GkcCombatAction ClassifySword1H(string normalized)
        {
            if (normalized.Contains("Special") || normalized.Contains("5"))
                return GkcCombatAction.Sword1HPower;
            if (normalized.Contains("Full Body 1") || normalized == "Sword Attack 1 Hand 1")
                return GkcCombatAction.Sword1HCombo1;
            if (normalized.Contains("Full Body 2") || normalized == "Sword Attack 1 Hand 2")
                return GkcCombatAction.Sword1HCombo2;
            if (normalized.Contains("Full Body 3") || normalized == "Sword Attack 1 Hand 3")
                return GkcCombatAction.Sword1HCombo3;
            if (normalized.Contains("4"))
                return GkcCombatAction.Sword1HCombo4;
            return GkcCombatAction.None;
        }

        private static GkcCombatAction ClassifyAxe1H(string normalized)
        {
            if (normalized.Contains("Special"))
                return GkcCombatAction.Axe1HPower;
            if (normalized.Contains("Full Body 1") || normalized == "Axe Attack 1 Hand 1")
                return GkcCombatAction.Axe1HCombo1;
            if (normalized.Contains("Full Body 2") || normalized == "Axe Attack 1 Hand 2")
                return GkcCombatAction.Axe1HCombo2;
            if (normalized.Contains("Full Body 3") || normalized == "Axe Attack 1 Hand 3")
                return GkcCombatAction.Axe1HCombo3;
            if (normalized.Contains("4"))
                return GkcCombatAction.Axe1HCombo4;
            return GkcCombatAction.None;
        }

        private static GkcCombatAction ClassifySword2H(string normalized)
        {
            if (normalized.Contains("Special"))
                return GkcCombatAction.Sword2HPower;
            if (normalized.Contains("Body 1"))
                return GkcCombatAction.Sword2HCombo1;
            if (normalized.Contains("Body 2"))
                return GkcCombatAction.Sword2HCombo2;
            if (normalized.Contains("Body 3"))
                return GkcCombatAction.Sword2HCombo3;
            if (normalized.Contains("Body 4"))
                return GkcCombatAction.Sword2HCombo4;
            return GkcCombatAction.None;
        }

        private static GkcCombatAction ClassifyByKnownActionId(int actionId) =>
            actionId switch
            {
                4100 => GkcCombatAction.Sword1HCombo1,
                4101 => GkcCombatAction.Sword1HCombo2,
                4102 => GkcCombatAction.Sword1HCombo3,
                4104 => GkcCombatAction.Sword1HCombo4,
                4108 => GkcCombatAction.Sword1HPower,
                4200 => GkcCombatAction.Sword2HCombo1,
                4201 => GkcCombatAction.Sword2HCombo2,
                4202 => GkcCombatAction.Sword2HCombo3,
                4203 => GkcCombatAction.Sword2HCombo4,
                4204 => GkcCombatAction.Sword2HPower,
                700 => GkcCombatAction.Axe1HCombo1,
                710 => GkcCombatAction.Axe1HCombo2,
                720 => GkcCombatAction.Axe1HCombo3,
                770 => GkcCombatAction.Axe1HCombo4,
                780 => GkcCombatAction.Axe1HPower,
                1000 => GkcCombatAction.Block,
                1535 => GkcCombatAction.Death,
                80 => GkcCombatAction.HitReactionUnarmed,
                _ => GkcCombatAction.None
            };

        private static GkcActionCatalogEntry MeleeEntry(
            GkcCombatAction action,
            int actionId,
            string stateName,
            GkcWeaponKind weapon,
            float duration = GkcAnimatorConstants.DefaultActionDuration) =>
            new()
            {
                combatAction = action,
                actionId = actionId,
                stateName = stateName,
                layerName = "Base Layer",
                requiresActionActive = true,
                clearActionIdAfterTrigger = true,
                weaponFilter = weapon,
                defaultDuration = duration
            };

        private static GkcActionCatalogEntry HitReactionEntry(
            GkcCombatAction action,
            string nestedStatePath,
            GkcWeaponKind weapon) =>
            new()
            {
                combatAction = action,
                stateName = nestedStatePath,
                layerName = "Base Layer",
                useDirectCrossFade = true,
                requiresActionActive = false,
                clearActionIdAfterTrigger = false,
                weaponFilter = weapon,
                defaultDuration = GkcAnimatorConstants.DefaultHitReactionDuration,
                crossFadeDuration = 0.08f
            };

        private static GkcActionCatalogEntry BlockEntry(
            GkcWeaponKind weapon,
            int actionId,
            string stateName,
            bool strafe,
            float duration = GkcAnimatorConstants.DefaultBlockActionDuration) =>
            new()
            {
                combatAction = GkcCombatAction.Block,
                actionId = actionId,
                stateName = stateName,
                layerName = GkcAnimatorConstants.UpperBodyCombatLayer,
                useDirectCrossFade = false,
                requiresActionActive = true,
                useActionActiveUpperBody = true,
                requiresStrafeMode = strafe,
                clearActionIdAfterTrigger = false,
                weaponFilter = weapon,
                defaultDuration = duration,
                crossFadeDuration = 0.08f
            };

        private static GkcActionCatalogEntry CrossFadeEntry(
            GkcCombatAction action,
            string stateName,
            string layer,
            GkcWeaponKind weapon = GkcWeaponKind.Infer) =>
            new()
            {
                combatAction = action,
                stateName = stateName,
                layerName = layer,
                useDirectCrossFade = true,
                clearActionIdAfterTrigger = false,
                useActionActiveUpperBody = true,
                weaponFilter = weapon,
                defaultDuration = GkcAnimatorConstants.DefaultPowerActionDuration,
                crossFadeDuration = 0.12f
            };

        private static GkcActionCatalogEntry Entry(
            GkcCombatAction action,
            int actionId,
            string stateName,
            float duration = GkcAnimatorConstants.DefaultActionDuration,
            bool strafe = false,
            bool upperBody = false,
            bool actionActive = false,
            string layer = GkcAnimatorConstants.UpperBodyCombatLayer) =>
            new()
            {
                combatAction = action,
                actionId = actionId,
                stateName = stateName,
                layerName = layer,
                requiresActionActive = actionActive,
                useActionActiveUpperBody = upperBody,
                requiresStrafeMode = strafe,
                clearActionIdAfterTrigger = actionId > 0,
                weaponFilter = GkcWeaponKind.Infer,
                defaultDuration = duration
            };
    }
}
