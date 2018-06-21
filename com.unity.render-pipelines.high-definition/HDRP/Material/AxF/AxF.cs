﻿using System;
using UnityEngine;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class AxF : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1500)]
        public struct SurfaceData {

            [SurfaceDataAttributes(new string[]{"Normal", "Normal View Space"}, true)]
            public Vector3  normalWS;

            [SurfaceDataAttributes("Tangent", true)]
            public Vector3  tangentWS;

            [SurfaceDataAttributes("BiTangent", true)]
            public Vector3  biTangentWS;


            //////////////////////////////////////////////////////////////////////////
            // SVBRDF Variables
            //
            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3  diffuseColor;

            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3  specularColor;

            [SurfaceDataAttributes("Fresnel F0", false, true)]
            public Vector3  fresnelF0;

            [SurfaceDataAttributes("Specular Lobe", false, true)]
            public Vector2  specularLobe;

            [SurfaceDataAttributes("Height")]
            public float    height_mm;

            [SurfaceDataAttributes("Anisotropic Angle")]
            public float    anisotropyAngle;


            //////////////////////////////////////////////////////////////////////////
            // Car Paint Variables
            //
            [SurfaceDataAttributes("Flakes UV")]
            public Vector2	flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;
            

            //////////////////////////////////////////////////////////////////////////
            // BTF Variables
            //


            //////////////////////////////////////////////////////////////////////////
            // Clear Coat
            [SurfaceDataAttributes("Clear Coat Color")]
            public Vector3  clearCoatColor;

            [SurfaceDataAttributes("Clear Coat Normal", true, true)]
            public Vector3  clearCoatNormalWS;

            [SurfaceDataAttributes("Clear Coat IOR")]
            public float    clearCoatIOR;

        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1600)]
        public struct BSDFData {

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3	normalWS;

            [SurfaceDataAttributes("", true)]
            public Vector3  tangentWS;

            [SurfaceDataAttributes("", true)]
            public Vector3  biTangentWS;


            //////////////////////////////////////////////////////////////////////////
            // SVBRDF Variables
            //
            [SurfaceDataAttributes("", false, true)]
            public Vector3	diffuseColor;
     
            [SurfaceDataAttributes("", false, true)]
            public Vector3	specularColor;

            [SurfaceDataAttributes("", false, true)]
            public Vector3  fresnelF0;

            [SurfaceDataAttributes("", false, true)]
            public Vector2	roughness;
  
            [SurfaceDataAttributes("", false, true)]
            public float	height_mm;

            [SurfaceDataAttributes("", false, true)]
            public float	anisotropyAngle;


            //////////////////////////////////////////////////////////////////////////
            // Car Paint Variables
            //
            [SurfaceDataAttributes("")]
            public Vector2	flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;


            //////////////////////////////////////////////////////////////////////////
            // BTF Variables
            //


            //////////////////////////////////////////////////////////////////////////
            // Clear Coat
            //
            [SurfaceDataAttributes("", false, true)]
            public Vector3  clearCoatColor;

            [SurfaceDataAttributes("", true)]
            public Vector3  clearCoatNormalWS;
  
            [SurfaceDataAttributes("", false, true)]
            public float	clearCoatIOR;
        };

//*
        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------
        //
        Material        m_preIntegratedFGDMaterial_Ward = null;
        Material        m_preIntegratedFGDMaterial_CookTorrance = null;
        RenderTexture   m_preIntegratedFGD_Ward = null;
        RenderTexture   m_preIntegratedFGD_CookTorrance = null;
        bool            m_preIntegratedTableAvailable = false;

        public AxF() {}

        public override void Build( HDRenderPipelineAsset hdAsset ) {
            if ( m_preIntegratedFGDMaterial_Ward != null )
                return; // Already initialized

            string[]  shaderWardGUIDs = UnityEditor.AssetDatabase.FindAssets( "PreIntegratedFGD_WardLambert" );
            if ( shaderWardGUIDs == null || shaderWardGUIDs.Length == 0 )
                throw new Exception( "Shader for Ward BRDF pre-integration not found!" );
            string[]  shaderCookTorranceGUIDs = UnityEditor.AssetDatabase.FindAssets( "PreIntegratedFGD_CookTorranceLambert" );
            if ( shaderCookTorranceGUIDs == null || shaderCookTorranceGUIDs.Length == 0 )
                throw new Exception( "Shader for Cook-Torrance BRDF pre-integration not found!" );

// Debug.Log( "Found " + shaderWardGUIDs.Length + " Ward DFG shaders!" );
// Debug.Log( "Found " + shaderCookTorranceGUIDs.Length + " Cook-Torrance DFG shaders!" );

            // Create Materials
            Shader  preIntegratedFGD_Ward = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>( UnityEditor.AssetDatabase.GUIDToAssetPath( shaderWardGUIDs[0] ) );
            Shader  preIntegratedFGD_CookTorrance = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>( UnityEditor.AssetDatabase.GUIDToAssetPath( shaderCookTorranceGUIDs[0] ) );
            m_preIntegratedFGDMaterial_Ward = CoreUtils.CreateEngineMaterial( preIntegratedFGD_Ward );
            m_preIntegratedFGDMaterial_CookTorrance = CoreUtils.CreateEngineMaterial( preIntegratedFGD_CookTorrance );

            // Create render textures where we will render the FGD tables
            m_preIntegratedFGD_Ward = new RenderTexture( 128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear );
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_Ward.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_Ward.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_Ward.name = CoreUtils.GetRenderTargetAutoName( 128, 128, 1, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD_Ward" );
            m_preIntegratedFGD_Ward.Create();

            m_preIntegratedFGD_CookTorrance = new RenderTexture( 128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear );
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_CookTorrance.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_CookTorrance.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_CookTorrance.name = CoreUtils.GetRenderTargetAutoName( 128, 128, 1, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD_CookTorrance" );
            m_preIntegratedFGD_CookTorrance.Create();
        }

        public override void Cleanup() {

// Debug.Log( "Destroying Ward/CookieResolution-Torrance DFG shaders!" );

            CoreUtils.Destroy( m_preIntegratedFGD_CookTorrance );
            CoreUtils.Destroy( m_preIntegratedFGD_Ward );
            CoreUtils.Destroy( m_preIntegratedFGDMaterial_CookTorrance );
            CoreUtils.Destroy( m_preIntegratedFGDMaterial_Ward );
            m_preIntegratedFGD_CookTorrance = null;
            m_preIntegratedFGD_Ward = null;
            m_preIntegratedFGDMaterial_Ward = null;
            m_preIntegratedFGDMaterial_CookTorrance = null;
            m_preIntegratedTableAvailable = false;
        }

        public override void RenderInit(CommandBuffer cmd) {
            if (    m_preIntegratedFGDMaterial_Ward == null
                ||  m_preIntegratedFGDMaterial_CookTorrance == null ) {
                return;
            }
// Disable cache while developing shader
            if ( m_preIntegratedTableAvailable ) {
//Debug.Log( "****Ward DFG table already computed****" );
                return;
            }
Debug.Log( "Rendering Ward/Cook-Torrance DFG table!" );

            using ( new ProfilingSample( cmd, "PreIntegratedFGD Material Generation for Ward & Cook-Torrance BRDF" ) ) {
                CoreUtils.DrawFullScreen( cmd, m_preIntegratedFGDMaterial_Ward, new RenderTargetIdentifier( m_preIntegratedFGD_Ward ) );
                CoreUtils.DrawFullScreen( cmd, m_preIntegratedFGDMaterial_CookTorrance, new RenderTargetIdentifier( m_preIntegratedFGD_CookTorrance ) );
                m_preIntegratedTableAvailable = true;
Debug.Log( "*FINISHED RENDERING* Ward/Cook-Torrance DFG table!" );
            }
        }

        public override void Bind() {
            if (    m_preIntegratedFGD_Ward == null
                ||  m_preIntegratedFGD_CookTorrance == null
                ||  !m_preIntegratedTableAvailable ) {
                throw new Exception( "Ward & Cook-Torrance BRDF pre-integration table not available!" );
            }

//Debug.Log( "Binding Ward DFG table!" );

            Shader.SetGlobalTexture( "_PreIntegratedFGD_WardLambert", m_preIntegratedFGD_Ward );
            Shader.SetGlobalTexture( "_PreIntegratedFGD_CookTorranceLambert", m_preIntegratedFGD_CookTorrance );
        }
//*/
    }
}
