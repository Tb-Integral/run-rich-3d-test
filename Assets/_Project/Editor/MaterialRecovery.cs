using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RunRich.Editor
{
    public static class MaterialRecovery
    {
        [Serializable]
        private sealed class Report
        {
            public string[] converted;
            public string[] deleted;
            public string[] retainedWithReferences;
        }

        public static void Run()
        {
            var converted = new List<string>();
            var upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
            // Использован тот же реестр преобразований, что и в Render Pipeline Converter.
            foreach (var material in MaterialUpgrader.FetchAllUpgradableMaterialsForPipeline(typeof(UniversalRenderPipelineAsset)))
            {
                string path = AssetDatabase.GetAssetPath(material);
                if (!path.StartsWith("Assets/Visual/") || !path.EndsWith(".mat"))
                    continue;
                string message = string.Empty;
                if (MaterialUpgrader.Upgrade(material, upgraders, (MaterialUpgrader.UpgradeFlags)0, ref message))
                {
                    EditorUtility.SetDirty(material);
                    converted.Add(path);
                }
            }
            AssetDatabase.SaveAssets();

            var broken = new HashSet<string>(Directory.GetFiles("Assets/Visual", "*.mat", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/')).Where(IsBroken));
            var retained = new HashSet<string>();
            var assets = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(path)).ToArray();
            foreach (string asset in assets.Where(path => !broken.Contains(path)))
                foreach (string dependency in AssetDatabase.GetDependencies(asset, false))
                    if (broken.Contains(dependency))
                        retained.Add(dependency);

            // Сохранены также зависимости материалов, которые пока нельзя удалить.
            var pending = new Queue<string>(retained);
            while (pending.Count > 0)
                foreach (string dependency in AssetDatabase.GetDependencies(pending.Dequeue(), false))
                    if (broken.Contains(dependency) && retained.Add(dependency))
                        pending.Enqueue(dependency);

            var deleted = new List<string>();
            string allowedRoot = Path.GetFullPath("Assets/Visual") + Path.DirectorySeparatorChar;
            foreach (string path in broken.Except(retained).OrderBy(path => path))
            {
                if (!Path.GetFullPath(path).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Материал находится вне разрешённой папки.");
                if (!AssetDatabase.DeleteAsset(path))
                    throw new IOException("Не удалось удалить материал: " + path);
                deleted.Add(path);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/material-recovery.json", JsonUtility.ToJson(new Report
            {
                converted = converted.ToArray(), deleted = deleted.ToArray(), retainedWithReferences = retained.ToArray()
            }, true));
            AssetDatabase.SaveAssets();
            ProjectValidation.Run();
        }

        private static bool IsBroken(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                return true;
            string shaderPath = AssetDatabase.GetAssetPath(material.shader);
            if (shaderPath.StartsWith("Assets/Visual/") && File.ReadAllText(shaderPath).Contains("#pragma surface"))
                return true;
            foreach (Match reference in Regex.Matches(File.ReadAllText(path), @"m_Texture: \{fileID: -?\d+, guid: ([a-f0-9]{32}), type: \d+\}"))
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value)))
                    return true;
            return false;
        }
    }
}
