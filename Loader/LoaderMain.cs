using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROR1AltSkills.Loader
{
    public class LoaderMain : SurvivorMain
    {
        public override string CharacterName => "Loader";

        public static SteppedSkillDef KnuckleBoomSkillDef;
        public static SkillDef DebrisShieldSkillDef;

        public static ConfigEntry<bool> DebrisShieldAffectsDrones;
        public static ConfigEntry<DebrisShieldMode> DebrisShieldSelectedMode;
        public static ConfigEntry<float> DebrisShieldDuration;
        public static ConfigEntry<float> DebrisShieldCooldown;

        public static CustomBuff DebrisShieldBarrierBuff;
        public static CustomBuff PylonPoweredBuff;

        private static readonly int pylonPowerMaxBounces = 3;
        private static readonly float pylonPowerRange = 20;
        private static readonly float pylonPowerDamageCoefficient = 0.3f;

        public enum DebrisShieldMode
        {
            Immunity,
            Shield,
            Barrier
        }

        public override void SetupConfig(ConfigFile config)
        {
            DebrisShieldAffectsDrones = config.Bind(ConfigCategory, "Debris Shield Affects Your Drones", true, "If true, drones owned by the player will be given the buff too.");
            DebrisShieldSelectedMode = config.Bind(ConfigCategory, "Debris Shield Type", DebrisShieldMode.Shield, "Sets the type of shielding provided by the skill." +
                "\nImmunity - Actually what the original skill provided in ROR1" +
                "\nShield - Provides 100% of your health as shield. Dissipates on skill end." +
                "\nBarrier - Provides 100% of your health as barrier. Dissipates on skill end.");
            DebrisShieldDuration = config.Bind(ConfigCategory, "Debris Shield Duration", 3f, "The duration in seconds of how long the buff lasts for.");
            DebrisShieldCooldown = config.Bind(ConfigCategory, "Debris Shield Cooldown", 5f, "The duration in seconds for the cooldown.");
        }

        public override void SetupUtility()
        {
            LanguageAPI.Add("DC_LOADER_SECONDARY_SHIELD_NAME", "Debris Shield");
            string desc = (DebrisShieldAffectsDrones.Value ? "You and your drones gain" : "Gain");
            desc += " <style=cIsHealing>100% health</style> as ";
            switch (DebrisShieldSelectedMode.Value)
            {
                case DebrisShieldMode.Immunity:
                    desc += $"<style=cIsDamage>damage immunity";
                    break;

                case DebrisShieldMode.Barrier:
                    desc += $"<style=cIsDamage>barrier";
                    break;

                case DebrisShieldMode.Shield:
                    desc += $"<style=cIsUtility>shield";
                    break;
            }
            desc += $"</style> and become <style=cIsUtility>electrified</style>, causing your attacks to <style=cIsUtility>zap up to {pylonPowerMaxBounces} times</style> within <style=cIsDamage>{pylonPowerRange}m</style> for <style=cIsDamage>{pylonPowerDamageCoefficient * 100f}% damage</style>.";
            LanguageAPI.Add("DC_LOADER_SECONDARY_SHIELD_DESCRIPTION", desc);

            DebrisShieldSkillDef = ScriptableObject.CreateInstance<SkillDef>();
            DebrisShieldSkillDef.activationState = new SerializableEntityStateType(typeof(ActivateShield));
            DebrisShieldSkillDef.activationStateMachineName = "DebrisShield";
            DebrisShieldSkillDef.baseMaxStock = 1;
            DebrisShieldSkillDef.baseRechargeInterval = DebrisShieldCooldown.Value;
            DebrisShieldSkillDef.cancelSprintingOnActivation = false;
            DebrisShieldSkillDef.beginSkillCooldownOnSkillEnd = false;
            DebrisShieldSkillDef.canceledFromSprinting = false;
            DebrisShieldSkillDef.fullRestockOnAssign = true;
            DebrisShieldSkillDef.interruptPriority = InterruptPriority.Any;
            DebrisShieldSkillDef.isCombatSkill = false;
            DebrisShieldSkillDef.mustKeyPress = false;
            DebrisShieldSkillDef.rechargeStock = 1;
            DebrisShieldSkillDef.requiredStock = 1;
            DebrisShieldSkillDef.stockToConsume = 1;
            DebrisShieldSkillDef.icon = RoR2Content.Items.PersonalShield.pickupIconSprite;
            DebrisShieldSkillDef.skillDescriptionToken = "DC_LOADER_SECONDARY_SHIELD_DESCRIPTION";
            DebrisShieldSkillDef.skillName = "DC_LOADER_SECONDARY_SHIELD_NAME";
            DebrisShieldSkillDef.skillNameToken = DebrisShieldSkillDef.skillName;
            DebrisShieldSkillDef.keywordTokens = new string[]
            {
                OriginalSkillsPlugin.modkeyword,
            };

            LoadoutAPI.AddSkillDef(DebrisShieldSkillDef);

            var skillFamily = SurvivorSkillLocator.secondary.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = DebrisShieldSkillDef,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(DebrisShieldSkillDef.skillNameToken, false, null)
            };

            //not a typo
            //this is to give the option of keeping the respective skill slot
            skillFamily = SurvivorSkillLocator.utility.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = DebrisShieldSkillDef,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(DebrisShieldSkillDef.skillNameToken, false, null)
            };
        }

        public override void SetupBuffs()
        {
            if (DebrisShieldSelectedMode.Value == DebrisShieldMode.Barrier)
            {
                DebrisShieldBarrierBuff = new CustomBuff("Debris Shield (Barrier)",
                    RoR2Content.Buffs.EngiShield.iconSprite,
                    Color.yellow,
                    false,
                    false);
                BuffAPI.Add(DebrisShieldBarrierBuff);
            }

            PylonPoweredBuff = new CustomBuff("Pylon Powered (Debris Shield)",
                RoR2Content.Buffs.FullCrit.iconSprite,
                Color.yellow,
                false,
                false);
            BuffAPI.Add(PylonPoweredBuff);
        }

        public override void Hooks()
        {
            base.Hooks();
            if (LoaderMain.DebrisShieldSelectedMode.Value == DebrisShieldMode.Barrier)
            {
                On.RoR2.HealthComponent.AddBarrier += HealthComponent_AddBarrier;
                On.RoR2.HealthComponent.Awake += HealthComponent_Awake;
                On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;
                On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
            }

            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
        }

        private static void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>())
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody.HasBuff(PylonPoweredBuff.BuffDef) && !damageInfo.procChainMask.HasProc(ProcType.LoaderLightning))
                {
                    float damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, pylonPowerDamageCoefficient);
                    LightningOrb lightningOrb = new LightningOrb();
                    lightningOrb.origin = damageInfo.position;
                    lightningOrb.damageValue = damageValue;
                    lightningOrb.isCrit = damageInfo.crit;
                    lightningOrb.bouncesRemaining = pylonPowerMaxBounces;
                    lightningOrb.teamIndex = attackerBody.teamComponent ? attackerBody.teamComponent.teamIndex : TeamIndex.None;
                    lightningOrb.attacker = damageInfo.attacker;
                    lightningOrb.bouncedObjects = new List<HealthComponent>
                            {
                                victim.GetComponent<HealthComponent>()
                            };
                    lightningOrb.procChainMask = damageInfo.procChainMask;
                    lightningOrb.procChainMask.AddProc(ProcType.LoaderLightning);
                    lightningOrb.procCoefficient = 0f;
                    lightningOrb.lightningType = LightningOrb.LightningType.Loader;
                    lightningOrb.damageColorIndex = DamageColorIndex.Item;
                    lightningOrb.range = pylonPowerRange;
                    HurtBox hurtBox = lightningOrb.PickNextTarget(damageInfo.position);
                    if (hurtBox)
                    {
                        lightningOrb.target = hurtBox;
                        OrbManager.instance.AddOrb(lightningOrb);
                    }
                }
            }
        }

        #region DebrisShield Barrier Type

        private static void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
        {
            orig(self, buffDef, duration);
            if (buffDef == DebrisShieldBarrierBuff.BuffDef)
            {
                var comp = self.GetComponent<TrackDebrisShield>();
                if (!comp)
                    comp = self.gameObject.AddComponent<TrackDebrisShield>();
                comp.OnBuffApplied();
            }
        }

        private static void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef == DebrisShieldBarrierBuff.BuffDef)
            {
                self.GetComponent<TrackDebrisShield>()?.OnBuffLost();
            }
        }

        private static void HealthComponent_Awake(On.RoR2.HealthComponent.orig_Awake orig, HealthComponent self)
        {
            orig(self);
            if (!self.GetComponent<TrackDebrisShield>())
                self.gameObject.AddComponent<TrackDebrisShield>().healthComponent = self;
        }

        private static void HealthComponent_AddBarrier(On.RoR2.HealthComponent.orig_AddBarrier orig, HealthComponent self, float value)
        {
            orig(self, value);
            if (value > 0)
                self.GetComponent<TrackDebrisShield>()?.OnAddBarrier(value);
        }

        #endregion DebrisShield Barrier Type

        public class TrackDebrisShield : MonoBehaviour
        {
            public float barrierToRemove = 0f;
            public HealthComponent healthComponent;

            private bool acceptContributions = true;

            public void OnAddBarrier(float amount)
            {
                if (acceptContributions)
                {
                    //Chat.AddMessage($"OnAddBarrier: {barrierToRemove} - {amount} = {barrierToRemove-amount}");
                    barrierToRemove -= amount;
                }
            }

            public void OnBuffLost()
            {
                if (barrierToRemove > 0)
                {
                    //Chat.AddMessage($"OnBuffLost: Expectation of resulting barrier: {healthComponent.Networkbarrier - barrierToRemove}");
                    healthComponent.Networkbarrier = Mathf.Max(healthComponent.Networkbarrier - barrierToRemove, 0f);
                    barrierToRemove = 0f;
                }
            }

            public void OnBuffApplied()
            {
                var barrierLostPerSecond = healthComponent.body.barrierDecayRate;
                var barrierLostAfterXSeconds = barrierLostPerSecond * DebrisShieldDuration.Value;

                var barrierToGive = healthComponent.fullBarrier;// * DebrisShieldPercentage.Value;

                barrierToRemove = barrierToGive - barrierLostAfterXSeconds;
                //Chat.AddMessage("Expected Barrier to Remove: " + barrierToRemove);
                acceptContributions = false;
                healthComponent.AddBarrier(barrierToGive);
                acceptContributions = true;
                //healthComponent.Networkbarrier = healthComponent.fullBarrier;
            }
        }
    }
}