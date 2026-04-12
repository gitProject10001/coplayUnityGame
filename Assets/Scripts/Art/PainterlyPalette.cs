using UnityEngine;

namespace ArtStyle
{
    /// <summary>
    /// Shared color palette for the painterly isometric ARPG style.
    /// Every Toon_*.mat preset draws its _BaseColor from one of these arrays.
    /// Editing this asset re-tints the whole game's art at once.
    /// </summary>
    [CreateAssetMenu(fileName = "PainterlyPalette", menuName = "Art Style/Painterly Palette")]
    public class PainterlyPalette : ScriptableObject
    {
        [Header("Earth Neutrals (terrain, rock, wood, dirt)")]
        public Color[] earth;

        [Header("Cool Neutrals (stone props, metal)")]
        public Color[] cool;

        [Header("Foliage Greens")]
        public Color[] foliage;

        [Header("Sky / Water Cools")]
        public Color[] sky;

        [Header("Saturated Accents (use one per scene)")]
        public Color[] accents;
    }
}
