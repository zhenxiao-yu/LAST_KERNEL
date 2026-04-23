using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Markyu.FortStack
{
    public class CustomPostProcessFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material effectMaterial;
            public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        CustomPass customPass;

        public override void Create()
        {
            customPass = new CustomPass();
            customPass.renderPassEvent = settings.renderEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.effectMaterial == null)
                return;

            customPass.Setup(settings.effectMaterial, settings.renderEvent);
            renderer.EnqueuePass(customPass);
        }

        class CustomPass : ScriptableRenderPass
        {
            private const string PassName = "FortStack Custom Post Process";

            private Material material;

            public void Setup(Material effectMaterial, RenderPassEvent passEvent)
            {
                material = effectMaterial;
                renderPassEvent = passEvent;

                // The effect samples the current camera color, so URP must provide an intermediate texture.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                    return;

                TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
                destinationDesc.name = "_FortStackCustomPostProcessColor";
                destinationDesc.clearBuffer = false;

                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
                RenderGraphUtils.BlitMaterialParameters blitParameters = new(source, destination, material, 0);

                renderGraph.AddBlitPass(blitParameters, PassName);
                resourceData.cameraColor = destination;
            }
        }
    }
}

