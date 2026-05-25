using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(AmmoManager.AmmoManagerMod), AmmoManager.BuildInfo.Name, AmmoManager.BuildInfo.Version, AmmoManager.BuildInfo.Author)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace AmmoManager
{
    public static class BuildInfo
    {
        public const string Name    = "Ammo Manager";
        public const string Author  = "YourName";
        public const string Version = "1.0.0";
    }

    public class AmmoManagerMod : MelonMod
    {
        // ── MelonPreferences ────────────────────────────────────────────────
        private static MelonPreferences_Category _prefs;

        public static MelonPreferences_Entry<bool>  RemoveShotgunAmmo;
        public static MelonPreferences_Entry<float> AmmoScale;
        public static MelonPreferences_Entry<float> AmmoOffsetX;
        public static MelonPreferences_Entry<float> AmmoOffsetY;
        public static MelonPreferences_Entry<float> AmmoOffsetZ;
        public static MelonPreferences_Entry<float> ScanDelay;

        // ── Shotgun ammo name keywords (lower-case) ─────────────────────────
        // BONELAB uses "heavy" ammo for shotguns; add any extra keywords here.
        private static readonly HashSet<string> ShotgunKeywords = new HashSet<string>
        {
            "shotgun", "shell", "buckshot", "slug", "heavyammo", "heavy ammo", "heavy_ammo"
        };

        // ── All ammo keywords (lower-case) ──────────────────────────────────
        private static readonly HashSet<string> AmmoKeywords = new HashSet<string>
        {
            "ammo", "cartridge", "shell", "bullet", "mag", "magazine",
            "lightammo",  "light ammo",  "light_ammo",
            "mediumammo", "medium ammo", "medium_ammo",
            "heavyammo",  "heavy ammo",  "heavy_ammo",
            "shotgun", "buckshot", "slug"
        };

        // ────────────────────────────────────────────────────────────────────
        public override void OnInitializeMelon()
        {
            // Preferences
            _prefs = MelonPreferences.CreateCategory("AmmoManager");
            RemoveShotgunAmmo = _prefs.CreateEntry("RemoveShotgunAmmo", true,
                display_name: "Remove Shotgun Ammo");
            AmmoScale  = _prefs.CreateEntry("AmmoScale",   1.5f,  display_name: "Ammo Scale");
            AmmoOffsetX = _prefs.CreateEntry("AmmoOffsetX", 0f,   display_name: "Position Offset X");
            AmmoOffsetY = _prefs.CreateEntry("AmmoOffsetY", 0.15f,display_name: "Position Offset Y");
            AmmoOffsetZ = _prefs.CreateEntry("AmmoOffsetZ", 0f,   display_name: "Position Offset Z");
            ScanDelay  = _prefs.CreateEntry("ScanDelay",   1.5f,  display_name: "Scene Scan Delay (s)");

            // BoneLib level hook
            Hooking.OnLevelInitialized += OnLevelInitialized;

            // BoneMenu (in-game settings panel)
            SetupBoneMenu();

            LoggerInstance.Msg($"{BuildInfo.Name} v{BuildInfo.Version} loaded.");
        }

        // ── BoneMenu ────────────────────────────────────────────────────────
        private void SetupBoneMenu()
        {
            var page = Page.Root.CreatePage("Ammo Manager", Color.yellow);

            page.CreateBool("Remove Shotgun Ammo",
                Color.red,
                RemoveShotgunAmmo.Value,
                v => { RemoveShotgunAmmo.Value = v; _prefs.SaveToFile(); });

            page.CreateFloat("Ammo Scale",
                Color.cyan,
                AmmoScale.Value, 0.1f, 0.1f, 5f,
                v => { AmmoScale.Value = v; _prefs.SaveToFile(); });

            page.CreateFloat("Offset Y",
                Color.green,
                AmmoOffsetY.Value, 0.05f, -2f, 2f,
                v => { AmmoOffsetY.Value = v; _prefs.SaveToFile(); });

            page.CreateFloat("Offset X",
                Color.white,
                AmmoOffsetX.Value, 0.05f, -2f, 2f,
                v => { AmmoOffsetX.Value = v; _prefs.SaveToFile(); });

            page.CreateFloat("Offset Z",
                Color.white,
                AmmoOffsetZ.Value, 0.05f, -2f, 2f,
                v => { AmmoOffsetZ.Value = v; _prefs.SaveToFile(); });

            page.CreateFunction("Apply Now (current scene)",
                Color.magenta,
                () => MelonCoroutines.Start(ProcessAmmoPickups()));
        }

        // ── Level hook ───────────────────────────────────────────────────────
        private void OnLevelInitialized(LevelInfo levelInfo)
        {
            LoggerInstance.Msg($"Level loaded: {levelInfo.title} — scanning ammo...");
            MelonCoroutines.Start(ProcessAmmoPickups());
        }

        // ── Main scan coroutine ──────────────────────────────────────────────
        private IEnumerator ProcessAmmoPickups()
        {
            // Give the scene time to finish spawning pooled objects
            yield return new WaitForSeconds(ScanDelay.Value);

            var offset = new Vector3(AmmoOffsetX.Value, AmmoOffsetY.Value, AmmoOffsetZ.Value);
            float scale = AmmoScale.Value;

            // Scan every active GameObject for ammo components / names
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            int removed = 0, modified = 0;

            foreach (var go in allObjects)
            {
                if (go == null) continue;

                string nameLower = go.name.ToLowerInvariant();

                if (!ContainsAnyKeyword(nameLower, AmmoKeywords)) continue;

                if (RemoveShotgunAmmo.Value && ContainsAnyKeyword(nameLower, ShotgunKeywords))
                {
                    LoggerInstance.Msg($"[REMOVE] {go.name}");
                    GameObject.Destroy(go);
                    removed++;
                }
                else
                {
                    // Reposition + rescale
                    go.transform.position += offset;
                    go.transform.localScale = Vector3.one * scale;
                    LoggerInstance.Msg($"[MODIFY] {go.name} → scale:{scale}  offset:{offset}");
                    modified++;
                }
            }

            LoggerInstance.Msg($"Ammo scan complete. Removed: {removed}  Modified: {modified}");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static bool ContainsAnyKeyword(string target, HashSet<string> keywords)
        {
            foreach (var kw in keywords)
                if (target.Contains(kw)) return true;
            return false;
        }
    }
}
