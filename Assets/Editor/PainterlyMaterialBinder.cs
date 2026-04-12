using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Auto-binds Toon_*.mat presets to FBX material slots based on slot name.
    /// Convention: name your Blender material slots "Stone", "Wood", "Metal",
    /// "Foliage", "Skin", "Cloth", "Banner", "Terrain", or "Grass_Top" and the
    /// matching preset under Assets/Materials/Painterly/ is bound on import.
    /// Only runs for FBXs imported under Assets/Models/ so existing assets are safe.
    /// Slots whose names don't match a preset fall through to Unity defaults.
    /// </summary>
    public class PainterlyMaterialBinder : AssetPostprocessor
    {
        private static readonly Dictionary<string, string> SlotMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Stone",     "Assets/Materials/Painterly/Toon_Stone.mat"   },
            { "Wood",      "Assets/Materials/Painterly/Toon_Wood.mat"    },
            { "Metal",     "Assets/Materials/Painterly/Toon_Metal.mat"   },
            { "Foliage",   "Assets/Materials/Painterly/Toon_Foliage.mat" },
            { "Skin",      "Assets/Materials/Painterly/Toon_Skin.mat"    },
            { "Cloth",     "Assets/Materials/Painterly/Toon_Cloth.mat"   },
            { "Banner",    "Assets/Materials/Painterly/Toon_Banner.mat"  },
            { "Terrain",   "Assets/Materials/Painterly/Toon_Terrain.mat" },
            { "Water",     "Assets/Materials/Painterly/Toon_Water.mat"  },
            { "Grass_Top", "Assets/Materials/GeometryGrass.mat"          },
        };

        private static bool ShouldHandle(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath)
                && assetPath.StartsWith("Assets/Models/", StringComparison.OrdinalIgnoreCase);
        }

        public Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!ShouldHandle(assetPath)) return null;
            if (material == null || string.IsNullOrEmpty(material.name)) return null;

            if (SlotMap.TryGetValue(material.name, out string matPath))
            {
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing != null) return existing;
            }
            return null;
        }
    }
}
