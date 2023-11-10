using Assets.Scripts.Data.MapSets;
using Assets.Scripts.Models.Map;
using Assets.Scripts.Unity.Map;
using Assets.Scripts.Unity.UI_New;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Utils;
using Harmony;
using MelonLoader;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheCorner {

    public class TheCornerMod : MelonMod {
        public static AssetBundle SceneBundle = null;
        public static AssetBundle AssetBundle = null;

        public override void OnApplicationStart() {
            SceneBundle = AssetBundle.LoadFromMemory(Properties.Resources.TheCornerScene);
            AssetBundle = AssetBundle.LoadFromMemory(Properties.Resources.TheCornerAssets);
        }
    }

    [HarmonyPatch(typeof(UI), nameof(UI.Awake))] // this and UI.Start are not entry points for tower, bloon, etc. mods, not set yet
    public class Awake_Patch {
        [HarmonyPostfix]
        public static void Postfix() =>
            UI.instance.mapSet.Maps.items = UI.instance.mapSet.Maps.items.Add(TheCornerMap.Details).ToArray();
    }

    [HarmonyPatch(typeof(MapLoader), nameof(MapLoader.Load))]
    public class MapLoad_Patch {
        [HarmonyPrefix]
        public static bool Prefix(MapLoader __instance, string map, Il2CppSystem.Action<MapModel> loadedCallback) {
            if (map.Equals(TheCornerMap.Name)) {
                __instance.currentMapName = map;
                
                loadedCallback.Invoke(TheCornerMap.Model);
                
                SceneManager.LoadScene(map, new LoadSceneParameters {
                    loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.None
                });

                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(MapLoader), nameof(MapLoader.ResetMap))]
        public class MapRestart_Patch {
            [HarmonyPrefix]
            public static bool Prefix(MapLoader __instance) {
                if(__instance.currentMapName.Equals(TheCornerMap.Name)) {
                    InGame.instance.Restart(false);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UI), nameof(UI.DestroyAndUnloadMapScene))]
        public class MapClear_Patch {
            [HarmonyPrefix]
            public static bool Prefix(UI __instance) {
                if(__instance.mapLoader.currentMapName.Equals(TheCornerMap.Name)) {
                    SceneManager.UnloadScene(TheCornerMap.Name);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ResourceLoader), "LoadSpriteFromSpriteReferenceAsync")]
        public class ResourceLoader_Patch {
            [HarmonyPrefix]
            public static bool Prefix(SpriteReference reference, Image image) {
                if (reference != null && reference.guidRef == TheCornerMap.Name) {
                    Texture2D texture = TheCornerMod.AssetBundle.LoadAsset(TheCornerMap.Name).Cast<Texture2D>();
                    image.canvasRenderer.SetTexture(texture);
                    image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2());
                    return false;
                }
                return true;
            }
        }
    }
}