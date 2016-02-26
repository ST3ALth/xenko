﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Lights;
using SiliconStudio.Xenko.Rendering.Materials;
using SiliconStudio.Xenko.Rendering.Shadows;

namespace SiliconStudio.Xenko.Rendering
{
    public class MeshPipelinePlugin : IPipelinePlugin
    {
        public void SetupPipeline(RenderContext context, NextGenRenderSystem renderSystem)
        {
            // Mandatory render stages
            var mainRenderStage = EntityComponentRendererBase.GetOrCreateRenderStage(renderSystem, "Main", "Main", new RenderOutputDescription(context.GraphicsDevice.Presenter.BackBuffer.ViewFormat, context.GraphicsDevice.Presenter.DepthStencilBuffer.ViewFormat));
            var transparentRenderStage = EntityComponentRendererBase.GetOrCreateRenderStage(renderSystem, "Transparent", "Main", new RenderOutputDescription(context.GraphicsDevice.Presenter.BackBuffer.ViewFormat, context.GraphicsDevice.Presenter.DepthStencilBuffer.ViewFormat));

            var meshRenderFeature = renderSystem.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault();
            if (meshRenderFeature == null)
            {
                meshRenderFeature = new MeshRenderFeature
                {
                    RenderFeatures =
                    {
                        new TransformRenderFeature(),
                        //new SkinningRenderFeature(),
                        new MaterialRenderFeature(),
                        (renderSystem.forwardLightingRenderFeature = new ForwardLightingRenderFeature()),
                    },
                };

                // Set default stage selector
                meshRenderFeature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
                {
                    EffectName = "TestEffect",
                    MainRenderStage = mainRenderStage,
                    TransparentRenderStage = transparentRenderStage,
                });

                // Register renderer
                renderSystem.RenderFeatures.Add(meshRenderFeature);
            }
        }
    }

    public class PickingMeshPipelinePlugin : IPipelinePlugin
    {
        public void SetupPipeline(RenderContext context, NextGenRenderSystem renderSystem)
        {
            var meshRenderFeature = renderSystem.RenderFeatures.OfType<MeshRenderFeature>().First();
            var pickingRenderStage = EntityComponentRendererBase.GetRenderStage(renderSystem, "Picking");

            meshRenderFeature.RenderFeatures.Add(new PickingRenderFeature());
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = "TestEffect.Picking",
                RenderStage = pickingRenderStage,
            });
        }
    }

    public class WireFrameMeshPipelinePlugin : IPipelinePlugin
    {
        public void SetupPipeline(RenderContext context, NextGenRenderSystem renderSystem)
        {
            var meshRenderFeature = renderSystem.RenderFeatures.OfType<MeshRenderFeature>().First();
            var wireFrameRenderStage = EntityComponentRendererBase.GetRenderStage(renderSystem, "WireFrame");

            meshRenderFeature.RenderFeatures.Add(new WireFrameRenderFeature());

            meshRenderFeature.PostProcessPipelineState += (RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState) =>
            {
                if (renderNode.RenderStage == wireFrameRenderStage)
                {
                    pipelineState.BlendState = context.GraphicsDevice.BlendStates.AlphaBlend;
                    pipelineState.RasterizerState = context.GraphicsDevice.RasterizerStates.WireFrame;
                }
            };

            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = "TestEffect.WireFrame",
                RenderStage = wireFrameRenderStage,
            });
        }
    }

    public class HighlightMeshPipelinePlugin : IPipelinePlugin
    {
        public void SetupPipeline(RenderContext context, NextGenRenderSystem renderSystem)
        {
            var meshRenderFeature = renderSystem.RenderFeatures.OfType<MeshRenderFeature>().First();
            var highlightRenderFeature = EntityComponentRendererBase.GetRenderStage(renderSystem, "Highlight");

            meshRenderFeature.RenderFeatures.Add(new HighlightRenderFeature());

            meshRenderFeature.PostProcessPipelineState += (RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState) =>
            {
                if (renderNode.RenderStage == highlightRenderFeature)
                {
                    pipelineState.BlendState = context.GraphicsDevice.BlendStates.AlphaBlend;
                    pipelineState.DepthStencilState = context.GraphicsDevice.DepthStencilStates.DepthRead;
                }
            };

            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = "TestEffect.Highlight",
                RenderStage = highlightRenderFeature,
            });
        }
    }

    public class ShadowMeshPipelinePlugin : IPipelinePlugin
    {
        public void SetupPipeline(RenderContext context, NextGenRenderSystem renderSystem)
        {
            var meshRenderFeature = renderSystem.RenderFeatures.OfType<MeshRenderFeature>().First();
            var shadowMapRenderStage = EntityComponentRendererBase.GetRenderStage(renderSystem, "ShadowMapCaster");

            var forwardLightingRenderFeature = meshRenderFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().First();
            forwardLightingRenderFeature.ShadowMapRenderStage = shadowMapRenderStage;

            meshRenderFeature.PostProcessPipelineState += (RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState) =>
            {
                if (renderNode.RenderStage == shadowMapRenderStage)
                {
                    pipelineState.RasterizerState = new RasterizerStateDescription(CullMode.None) { DepthClipEnable = false };
                }
            };

            meshRenderFeature.RenderStageSelectors.Add(new ShadowMapRenderStageSelector
            {
                EffectName = "TestEffect.ShadowMapCaster",
                ShadowMapRenderStage = shadowMapRenderStage,
            });
        }
    }
}