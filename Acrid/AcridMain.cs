using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using System;
using EntityStates;
using R2API;
using RoR2.Skills;
using RoR2.Projectile;

namespace ROR1AltSkills.Acrid
{
    public class AcridMain : SurvivorMain
    {
        public override string CharacterName => "Croco";

        #region passive
        //public static DamageAPI.ModdedDamageType OriginalPoisonOnHit;
        //public static float OriginalPoisonDamageCoefficient = 0.24f;

        //public static DotController.DotIndex OriginalPoisonDot;
        //public static CustomBuff OriginalPoisonBuff;
        #endregion
        #region primary
        public static float FesteringWoundsDamageCoefficient = 1.8f;
        public static float FesteringWoundsDPSCoefficient = 0.9f;
        #endregion
        #region utility
        public static GameObject acidPool;
        public static GameObject acidPoolDrop;

        internal static float acidPoolScale = 1f;
        private static readonly float buffWard_to_acidPoolScale_ratio = 5f; //shouldn't be changed
        internal static float CausticSludgeBuffDuration = 3f;

        internal static float CausticSludgeActualScale = acidPoolScale * buffWard_to_acidPoolScale_ratio;

        public static float CausticSludgeLifetime = 12f;
        public static float CausticSludgeDuration = 2f;
        public static float CausticSludgeSlowDuration = 3f;
        public static float CausticSludgeDamageCoefficient = 0.5f;

        public static float CausticSludgeLeapLandDamageCoefficient = 2f;

        #endregion
        public override string ConfigCategory => "Acrid";

        public override void Init(ConfigFile config)
        {
            base.Init(config);
            SetupProjectiles();
        }

        private static void SetupProjectiles()
        {
            SetupAcidPool();
            SetupAcidPoolDrop();
            void SetupAcidPool()
            {
                acidPool = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/CrocoLeapAcid"), "CrocoSpeedAcid");
                acidPool.transform.localScale *= acidPoolScale;
                var buffWard = acidPool.AddComponent<BuffWard>();
                buffWard.buffDef = RoR2Content.Buffs.CloakSpeed;
                buffWard.buffDuration = CausticSludgeBuffDuration;
                buffWard.expires = false;
                buffWard.floorWard = true;
                buffWard.radius = CausticSludgeActualScale;
                buffWard.requireGrounded = true;
                buffWard.interval *= 0.5f;

                var enemyBuffWard = acidPool.AddComponent<BuffWard>();
                enemyBuffWard.buffDef = RoR2Content.Buffs.Slow60;
                enemyBuffWard.buffDuration = CausticSludgeBuffDuration;
                enemyBuffWard.expires = false;
                enemyBuffWard.floorWard = true;
                enemyBuffWard.radius = CausticSludgeActualScale;
                enemyBuffWard.requireGrounded = true;
                enemyBuffWard.invertTeamFilter = true;

                ProjectileDotZone projectileDotZone = acidPool.GetComponent<ProjectileDotZone>();
                projectileDotZone.damageCoefficient = CausticSludgeDamageCoefficient;
                projectileDotZone.lifetime = CausticSludgeLifetime;
                projectileDotZone.overlapProcCoefficient = 0f;

                //PoisonSplatController poisonSplatController = acidPool.AddComponent<PoisonSplatController>();
                //poisonSplatController.destroyOnTimer = acidPool.GetComponent<DestroyOnTimer>();
                //poisonSplatController.projectileDotZone = projectileDotZone;
                //poisonSplatController.projectileController = acidPool.GetComponent<ProjectileController>();

                ProjectileAPI.Add(acidPool);

                LeapDropAcid.groundedAcid = acidPool;
            }

            void SetupAcidPoolDrop()
            {
                Debug.Log("Setting up AcidPoolDrop");
                acidPoolDrop = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/SporeGrenadeProjectile"), "CrocoSpeedAcidDrop");

                acidPoolDrop.GetComponent<ProjectileSimple>().desiredForwardSpeed = 0;

                var atos = acidPoolDrop.AddComponent<ApplyTorqueOnStart>();
                atos.localTorque = Vector3.down * 3f;
                atos.randomize = false;

                var projectileImpactExplosion = acidPoolDrop.GetComponent<ProjectileImpactExplosion>();
                projectileImpactExplosion.blastDamageCoefficient = 0f;
                projectileImpactExplosion.childrenProjectilePrefab = acidPool;
                projectileImpactExplosion.impactEffect = null;
                projectileImpactExplosion.destroyOnEnemy = false;

                //acidPoolDrop.AddComponent<PoisonFallController>();

                ProjectileAPI.Add(acidPoolDrop);
            }
            LeapDropAcid.projectilePrefab = acidPoolDrop;
        }

        public override void SetupConfig(ConfigFile config)
        {
            //OriginalPoisonDamageCoefficient = config.Bind(ConfigCategory, "Original Poison Damage Coefficient", 0.24f, "The damage coefficient of the Original Poison.").Value;
            FesteringWoundsDamageCoefficient = config.Bind(ConfigCategory, "Festering Wounds Damage Coefficient", 1.8f, "").Value;
            FesteringWoundsDPSCoefficient = config.Bind(ConfigCategory, "Festering Wounds DPS Coefficient", 0.9f, "").Value;
            CausticSludgeBuffDuration = config.Bind(ConfigCategory, "Caustic Sludge Buff Duration", 3f, "").Value;
            CausticSludgeLifetime = config.Bind(ConfigCategory, "Caustic Sludge Lifetime", 12f, "").Value;
            CausticSludgeDuration = config.Bind(ConfigCategory, "Caustic Sludge Duration", 2f, "").Value;
            CausticSludgeSlowDuration = config.Bind(ConfigCategory, "Caustic Sludge Slow Duration", 3f, "").Value;
            CausticSludgeDamageCoefficient = config.Bind(ConfigCategory, "Caustic Sludge Damage Coefficient", 0.5f, "").Value;
            CausticSludgeLeapLandDamageCoefficient = config.Bind(ConfigCategory, "Caustic Sludge Leap Land Damage Coefficient", 2f, "").Value;
        }

        public override void SetupLanguage()
        {
        }

        public override void SetupPrimary()
        {
            return;
            /*LanguageAPI.Add("DC_CROCO_PRIMARY_FESTERINGWOUNDS_NAME", "Festering Wounds");
            LanguageAPI.Add("DC_CROCO_PRIMARY_FESTERINGWOUNDS_DESCRIPTION", $"Maul an enemy for <style=cIsDamage>{FesteringWoundsDamageCoefficient * 100f}% damage</style>." +
                $" The target is poisoned for <style=cIsDamage>{FesteringWoundsDPSCoefficient * 100f}% damage per second</style>.");

            var mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(PoisonBite));
            mySkillDef.activationStateMachineName = "Mouth";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Resources.Load<Sprite>("textures/bufficons/texBuffLunarShellIcon");
            mySkillDef.skillDescriptionToken = "DC_CROCO_PRIMARY_FESTERINGWOUNDS_DESCRIPTION";
            mySkillDef.skillName = "DC_CROCO_PRIMARY_FESTERINGWOUNDS_NAME";
            mySkillDef.skillNameToken = mySkillDef.skillName;
            mySkillDef.keywordTokens = new string[]
            {
                OriginalSkillsPlugin.modkeyword,
                "KEYWORD_POISON",
                "KEYWORD_SLAYER",
                "KEYWORD_RAPID_REGEN",
            };

            LoadoutAPI.AddSkillDef(mySkillDef);


            var skillFamily = SurvivorSkillLocator.primary.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };*/
        }

        public override void SetupSecondary()
        {
        }

        public override void SetupUtility()
        {
            LanguageAPI.Add("DC_CROCO_UTILITY_CAUSTICSLUDGE_NAME", "Caustic Sludge");
            LanguageAPI.Add("DC_CROCO_UTILITY_CAUSTICSLUDGE_DESCRIPTION", $"<style=cIsUtility>Leap in the air</style>, and secrete <style=cIsDamage>poisonous sludge</style> for {CausticSludgeDuration} seconds." +
                $" <style=cIsUtility>Speeds up allies,</style> while <style=cIsDamage>slowing and hurting enemies</style> for <style=cIsDamage>{CausticSludgeDamageCoefficient * 100f}% damage</style>." +
                $" If you are leaping, the impact deals <style=cIsDamage>{CausticSludgeLeapLandDamageCoefficient * 100}% damage</style>.");

            var mySkillDefUtil = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDefUtil.activationState = new SerializableEntityStateType(typeof(Acrid.LeapDropAcid));
            mySkillDefUtil.activationStateMachineName = "Weapon";
            mySkillDefUtil.baseMaxStock = 1;
            mySkillDefUtil.baseRechargeInterval = CausticSludgeLifetime;
            mySkillDefUtil.beginSkillCooldownOnSkillEnd = true;
            mySkillDefUtil.canceledFromSprinting = false;
            mySkillDefUtil.fullRestockOnAssign = true;
            mySkillDefUtil.interruptPriority = InterruptPriority.Frozen;
            mySkillDefUtil.isCombatSkill = false;
            mySkillDefUtil.mustKeyPress = true;
            mySkillDefUtil.rechargeStock = 1;
            mySkillDefUtil.requiredStock = 1;
            mySkillDefUtil.stockToConsume = 1;
            mySkillDefUtil.icon = Resources.Load<Sprite>("textures/bufficons/texBuffLunarShellIcon");
            mySkillDefUtil.skillDescriptionToken = "DC_CROCO_UTILITY_CAUSTICSLUDGE_DESCRIPTION";
            mySkillDefUtil.skillName = "DC_CROCO_UTILITY_CAUSTICSLUDGE_NAME";
            mySkillDefUtil.skillNameToken = mySkillDefUtil.skillName;
            mySkillDefUtil.keywordTokens = new string[]
            {
                OriginalSkillsPlugin.modkeyword,
                "KEYWORD_POISON",
            };

            LoadoutAPI.AddSkillDef(mySkillDefUtil);

            var skillFamily = SurvivorSkillLocator.utility.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDefUtil,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(mySkillDefUtil.skillNameToken, false, null)
            };
        }

        public override void SetupSpecial()
        {
        }
    }
}
