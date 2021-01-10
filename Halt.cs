using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using ThunderRoad;
using UnityEngine;

namespace CosmicSpell {
    public class HaltIntegration : LevelModule {

        /// <summary>
        /// Starts freeze controller
        /// </summary>

        private Harmony harmony;
        public override IEnumerator OnLoadCoroutine(Level level) {
            try {
                harmony = new Harmony("Halt");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("Halt successfully loaded!");
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
            return base.OnLoadCoroutine(level);
        }
    }
}
