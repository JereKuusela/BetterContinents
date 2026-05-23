// 0.0.2c
using System;
using UnityEngine;
using HarmonyLib;

namespace BetterContinents;

/**
 * Loads the custom square-map shader from an embedded AssetBundle and swaps it
 * onto the Minimap materials when DisableMapEdgeDropoff is true; restores the
 * cached vanilla shader on toggle back.
 *
 * Bundle lives at assets/BCAssets/squaremap_assets in the source tree and is
 * embedded via an EmbeddedResource entry in the csproj. The existing
 * GameUtils.GetAssetBundleFromResources helper does the embedded-resource-to-
 * AssetBundle dance, matching by suffix so we just pass the short name.
 *
 * Vanilla shaders are captured on the first Apply call so toggling back off
 * restores bit-identical vanilla rendering.
 */
[HarmonyPatch]
internal static class MinimapShaderSwap
{
    private const string BundleSuffix = "squaremap_assets";
    private const string ShaderAssetName = "squaremapshader";

    private static AssetBundle? _bundle;
    private static Shader? _squareShader;
    private static Shader? _vanillaLargeShader;
    private static Shader? _vanillaSmallShader;
    private static bool _vanillaCached;

    public static void Apply()
    {
        if (Minimap.instance == null)
        {
            return;
        }

        if (Minimap.instance.m_mapImageLarge == null || Minimap.instance.m_mapImageSmall == null)
        {
            return;
        }

        Material largeMat = Minimap.instance.m_mapImageLarge.material;
        Material smallMat = Minimap.instance.m_mapImageSmall.material;

        if (largeMat == null || smallMat == null)
        {
            return;
        }

        if (!_vanillaCached)
        {
            _vanillaLargeShader = largeMat.shader;
            _vanillaSmallShader = smallMat.shader;
            _vanillaCached = true;
            BetterContinents.Log($"[MinimapShaderSwap] Cached vanilla shaders: large={_vanillaLargeShader.name} small={_vanillaSmallShader.name}");
        }

        if (BetterContinents.Settings.DisableMapEdgeDropoff)
        {
            Shader? sq = LoadSquareShader();
            if (sq == null)
            {
                return;
            }
            if (largeMat.shader != sq)
            {
                largeMat.shader = sq;
            }
            if (smallMat.shader != sq)
            {
                smallMat.shader = sq;
            }

            largeMat.SetFloat("_SquareMap", 1f);
            smallMat.SetFloat("_SquareMap", 1f);

            BetterContinents.Log("[MinimapShaderSwap] Applied square-map shader");
        }
        else
        {
            /**
             * Restore only if we've actually cached vanilla earlier — this
             * protects against a no-op early-startup call before any swap.
             */
            if (_vanillaLargeShader != null && largeMat.shader != _vanillaLargeShader)
            {
                largeMat.shader = _vanillaLargeShader;
            }
            if (_vanillaSmallShader != null && smallMat.shader != _vanillaSmallShader)
            {
                smallMat.shader = _vanillaSmallShader;
            }

            largeMat.SetFloat("_SquareMap", 0f);
            smallMat.SetFloat("_SquareMap", 0f);

            BetterContinents.Log("[MinimapShaderSwap] Restored vanilla shaders");
        }
    }

    private static Shader? LoadSquareShader()
    {
        if (_squareShader != null) return _squareShader;
        if (_bundle == null)
        {
            _bundle = GameUtils.GetAssetBundleFromResources(BundleSuffix);
            if (_bundle == null)
            {
                BetterContinents.LogError($"[MinimapShaderSwap] Bundle '{BundleSuffix}' not loaded");
                return null;
            }
        }
        _squareShader = _bundle.LoadAsset<Shader>(ShaderAssetName);
        if (_squareShader == null)
        {
            BetterContinents.LogError($"[MinimapShaderSwap] Shader '{ShaderAssetName}' not found. Available: {string.Join(", ", _bundle.GetAllAssetNames())}");
            return null;
        }
        BetterContinents.Log($"[MinimapShaderSwap] Loaded shader: {_squareShader.name}");
        return _squareShader;
    }

    /**
     * Postfix Minimap.Start so the first time the minimap comes up it has the
     * right shader applied. Patcher.PatchWorldSize also calls Apply() so
     * toggle changes that go through DynamicPatch propagate to the shader.
     */
    [HarmonyPostfix, HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    private static void OnMinimapStart()
    {
        Apply();
    }
}