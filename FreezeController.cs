using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace CosmicSpell {
    public class FreezeController {
        public static FreezeController Instance {
            get {
                return instance;
            }
        }

        private FreezeController() {
        }

        public bool IsTimeFrozen {
            get {
                return isTimeFrozen;
            }
            set {
                if (IsTimeFrozen == false) {
                    FreezeTime();
                } else {
                    UnFreezeTime();
                }
            }
        }

        public void FreezeTime() {
            if (!IsTimeFrozen)
                isTimeFrozen = true;
        }

        public void UnFreezeTime() {
            if (isTimeFrozen)
                isTimeFrozen = false;
        }

        /// <summary>
        /// These are pretty self explanitory
        /// </summary>

        public void FreezeCreature(Creature creature) {
            if (creature != Player.currentCreature) {
                creature.FreezeData().IsCreatureFrozen = true;
                creature.brain.Stop();
                if (creature.animator != null) {
                    creature.animator.speed = 0;
                }
                if (creature.locomotion != null) {
                    creature.locomotion.MoveStop();
                    creature.brain.StopTurn();
                }
                if (creature.brain.navMeshAgent != null) {
                    creature.brain.navMeshAgent.isStopped = true;
                }
                if (!creature.ragdoll.isGrabbed) {
                    foreach (RagdollPart ragdollPart in creature.ragdoll.parts) {
                        FreezeRigidbody(ragdollPart.rb);
                    }
                }
            }
        }

        public void UnFreezeCreature(Creature creature) {
            if (creature != Player.currentCreature) {
                creature.FreezeData().IsCreatureFrozen = false;
                if (creature.currentHealth > 0) {
                    creature.brain.instance.Start();
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);

                    if (creature.brain.navMeshAgent != null)
                        creature.brain.navMeshAgent.isStopped = false;
                }
                creature.animator.speed = 1f;
                foreach (RagdollPart ragdollPart in creature.ragdoll.parts) {
                    UnFreezeRigidbody(ragdollPart.rb);
                }
            }
        }

        public void FreezeRigidbody(Rigidbody rigidbody) {
            StoredPhysicsData storedPhysicsData = rigidbody.gameObject.GetComponent<StoredPhysicsData>();
            if (storedPhysicsData == null) {
                storedPhysicsData = rigidbody.gameObject.AddComponent<StoredPhysicsData>();
            }
            if (storedPhysicsData != null) {
                storedPhysicsData.StoreDataFromRigidBody(rigidbody);
            }
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            rigidbody.useGravity = false;
        }

        public void UnFreezeRigidbody(Rigidbody rigidbody) {
            if (rigidbody.constraints != RigidbodyConstraints.None) {
                rigidbody.constraints = RigidbodyConstraints.None;
                rigidbody.useGravity = true;
                rigidbody.ResetInertiaTensor();
                StoredPhysicsData storedPhysicsData = rigidbody.gameObject.GetComponent<StoredPhysicsData>();
                if (storedPhysicsData == null) {
                    storedPhysicsData = rigidbody.gameObject.AddComponent<StoredPhysicsData>();
                }
                if (storedPhysicsData != null) {
                    storedPhysicsData.SetRigidbodyFromStoredData(rigidbody);
                }
            }
        }

        private static FreezeController instance = new FreezeController();

        private bool isTimeFrozen;

        /// <summary>
        /// Add freeze data on spawn
        /// </summary>

        [HarmonyPatch(typeof(Creature))]
        [HarmonyPatch("Awake")]
        private static class NewCreatureDataPatch {
            [HarmonyPostfix]
            private static void Postfix(Creature __instance) {
                __instance.gameObject.AddComponent<CreatureFreezeData>();
            }
        }

        /// <summary>
        /// Grabbing and un-grabbing frozen creatures
        /// </summary>

        [HarmonyPatch(typeof(HandleRagdoll))]
        [HarmonyPatch("OnGrab")]
        private static class GrabbedRagdollUnFreezePatch {
            [HarmonyPostfix]
            private static void Postfix(HandleRagdoll __instance, RagdollHand ragdollHand, float axisPosition, HandleOrientation orientation, bool teleportToHand = false) {
                if (__instance.ragdollPart.ragdoll.creature.FreezeData().IsCreatureFrozen) {
                    Instance.UnFreezeCreature(__instance.ragdollPart.ragdoll.creature);
                }
            }
        }

        [HarmonyPatch(typeof(HandleRagdoll))]
        [HarmonyPatch("OnUnGrab")]
        private static class UnGrabbedRagdollFreezePatch {
            [HarmonyPostfix]
            private static void Postfix(HandleRagdoll __instance, RagdollHand ragdollHand, bool throwing) {
                if (__instance.ragdollPart.ragdoll.creature.FreezeData().IsCreatureFrozen && !__instance.ragdollPart.ragdoll.isGrabbed && !__instance.ragdollPart.ragdoll.isTkGrabbed) {
                    Instance.FreezeCreature(__instance.ragdollPart.ragdoll.creature);
                }
            }
        }

        /*[HarmonyPatch(typeof(HandleRagdoll))]
        [HarmonyPatch("OnTelekinesisGrab")]
        private static class TKGrabPatch {
            [HarmonyPostfix]
            private static void Postfix(HandleRagdoll __instance, SpellTelekinesis spellTelekinesis) {
                if (__instance.ragdollPart.ragdoll.creature.FreezeData().IsCreatureFrozen) {
                    Instance.UnFreezeCreature(__instance.ragdollPart.ragdoll.creature);
                }
            }
        }

        [HarmonyPatch(typeof(HandleRagdoll))]
        [HarmonyPatch("OnTelekinesisRelease")]
        private static class TKUnGrabPatch {
            [HarmonyPostfix]
            private static void Postfix(HandleRagdoll __instance, SpellTelekinesis spellTelekinesis, bool tryThrow, out bool throwing) {
                if (__instance.ragdollPart.ragdoll.creature.FreezeData().IsCreatureFrozen && !__instance.ragdollPart.ragdoll.isGrabbed && !__instance.ragdollPart.ragdoll.isTkGrabbed) {
                    Instance.FreezeCreature(__instance.ragdollPart.ragdoll.creature);
                }
            }
        }*/

        /// <summary>
        /// Brain handlers
        /// </summary>

        [HarmonyPatch(typeof(BrainData))]
        [HarmonyPatch("Update")]
        private static class CreatureBrainFreezePatch {
            [HarmonyPrefix]
            private static bool Prefix(BrainData __instance) {
                if(__instance != null) {
                    return !__instance.targetCreature.FreezeData().IsCreatureFrozen;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Brain))]
        [HarmonyPatch("UpdateActionCycle")]
        private static class CreatureActionFreezePatch {
            [HarmonyPrefix]
            private static bool Prefix(Brain __instance) {
                return !__instance.creature.FreezeData().IsCreatureFrozen;
            }
        }

        [HarmonyPatch(typeof(Brain))]
        [HarmonyPatch("TryAction")]
        private static class CreatureNewActionFreezePatch {
            [HarmonyPrefix]
            private static bool Prefix(Brain __instance) {
                return !__instance.creature.FreezeData().IsCreatureFrozen;
            }

        }
    }
}
