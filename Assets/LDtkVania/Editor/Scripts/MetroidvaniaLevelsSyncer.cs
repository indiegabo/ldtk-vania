using UnityEditor;
using UnityEngine;

namespace LDtkVania.Editor
{
    class MetroidvaniaLevelsSyncer : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // if (!MV_Project.Instance.SyncLevelsAtCompile) return;
            // MV_Project.Instance.SyncLevels();
        }
    }
}