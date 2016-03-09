using System;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.SpriteStudio.Runtime
{
    //TODO this whole renderer is not optimized at all! batching is wrong and depth calculation should be done differently
    public class SpriteStudioRenderFeature : RootRenderFeature
    {
        private EffectInstance selectedSpriteEffect;
        private EffectInstance pickingSpriteEffect;

        private Sprite3DBatch sprite3DBatch;

        public BlendStateDescription MultBlendState;
        public BlendStateDescription SubBlendState;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            sprite3DBatch = new Sprite3DBatch(Context.GraphicsDevice);

            var blendDesc = new BlendStateDescription(Blend.SourceAlpha, Blend.One)
            {
                RenderTarget0 =
                {
                    BlendEnable = true,
                    ColorBlendFunction = BlendFunction.ReverseSubtract,
                    AlphaBlendFunction = BlendFunction.ReverseSubtract
                }
            };
            SubBlendState = blendDesc;

            blendDesc = new BlendStateDescription(Blend.DestinationColor, Blend.InverseSourceAlpha)
            {
                RenderTarget0 =
                {
                    BlendEnable = true,
                    ColorBlendFunction = BlendFunction.Add,
                    AlphaSourceBlend = Blend.Zero,
                    AlphaBlendFunction = BlendFunction.Add
                }
            };
            MultBlendState = blendDesc;
        }

        protected override void Destroy()
        {
            base.Destroy();

            sprite3DBatch.Dispose();
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            base.Draw(context, renderView, renderViewStage, startIndex, endIndex);

            BlendStateDescription? previousBlendState = null;
            DepthStencilStateDescription? previousDepthStencilState = null;
            EffectInstance previousEffect = null;

            //TODO string comparison ...?
            var isPicking = renderViewStage.RenderStage.Name == "Picking";

            var device = RenderSystem.GraphicsDevice;

            var hasBegin = false;
            for (var index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);

                var spriteState = (RenderSpriteStudio)renderNode.RenderObject;

                var transfoComp = spriteState.TransformComponent;
                var depthStencilState = DepthStencilStates.DepthRead;

                foreach (var node in spriteState.SpriteStudioComponent.SortedNodes)
                {
                    if (node.Sprite?.Texture == null || node.Sprite.Region.Width <= 0 || node.Sprite.Region.Height <= 0f || node.Hide != 0) continue;

                    // Update the sprite batch

                    BlendStateDescription spriteBlending;
                    switch (node.BaseNode.AlphaBlending)
                    {
                        case SpriteStudioBlending.Mix:
                            spriteBlending = BlendStates.AlphaBlend;
                            break;
                        case SpriteStudioBlending.Multiplication:
                            spriteBlending = MultBlendState;
                            break;
                        case SpriteStudioBlending.Addition:
                            spriteBlending = BlendStates.Additive;
                            break;
                        case SpriteStudioBlending.Subtraction:
                            spriteBlending = SubBlendState;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // TODO: this should probably be moved to Prepare()
                    // Project the position
                    // TODO: This could be done in a SIMD batch, but we need to figure-out how to plugin in with RenderMesh object
                    var worldPosition = new Vector4(transfoComp.WorldMatrix.TranslationVector, 1.0f);

                    Vector4 projectedPosition;
                    Vector4.Transform(ref worldPosition, ref renderView.ViewProjection, out projectedPosition);
                    var projectedZ = projectedPosition.Z / projectedPosition.W;

                    var blendState = isPicking ? BlendStates.Default : spriteBlending;
                    var currentEffect = isPicking ? GetOrCreatePickingSpriteEffect() : ShadowObject.IsObjectSelected(spriteState.SpriteStudioComponent) ? GetOrCreateSelectedSpriteEffect() : null;
                    // TODO remove this code when material are available
                    if (previousEffect != currentEffect || blendState != previousBlendState || depthStencilState != previousDepthStencilState)
                    {
                        if (hasBegin)
                        {
                            sprite3DBatch.End();
                        }
                        sprite3DBatch.Begin(context.GraphicsContext, renderView.ViewProjection, SpriteSortMode.Deferred, blendState, null, depthStencilState, RasterizerStates.CullNone, currentEffect);
                        hasBegin = true;
                    }

                    previousEffect = currentEffect;
                    previousBlendState = blendState;
                    previousDepthStencilState = depthStencilState;

                    var sourceRegion = node.Sprite.Region;
                    var texture = node.Sprite.Texture;

                    // skip the sprite if no texture is set.
                    if (texture == null)
                        continue;

                    var color4 = Color4.White;
                    if (isPicking)
                    {
                        // TODO move this code corresponding to picking out of the runtime code.
                        color4 = new Color4(RuntimeIdHelper.ToRuntimeId(spriteState.SpriteStudioComponent));
                    }
                    else
                    {
                        if (node.BlendFactor > 0.0f)
                        {
                            switch (node.BlendType) //todo this should be done in a shader
                            {
                                case SpriteStudioBlending.Mix:
                                    color4 = Color4.Lerp(color4, node.BlendColor, node.BlendFactor) * node.FinalTransparency;
                                    break;
                                case SpriteStudioBlending.Multiplication:
                                    color4 = Color4.Lerp(color4, node.BlendColor, node.BlendFactor) * node.FinalTransparency;
                                    break;
                                case SpriteStudioBlending.Addition:
                                    color4 = Color4.Lerp(color4, node.BlendColor, node.BlendFactor) * node.FinalTransparency;
                                    break;
                                case SpriteStudioBlending.Subtraction:
                                    color4 = Color4.Lerp(color4, node.BlendColor, node.BlendFactor) * node.FinalTransparency;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            color4 *= node.FinalTransparency;
                        }
                    }

                    var worldMatrix = node.ModelTransform*transfoComp.WorldMatrix;

                    // calculate normalized position of the center of the sprite (takes into account the possible rotation of the image)
                    var normalizedCenter = new Vector2(node.Sprite.Center.X/sourceRegion.Width - 0.5f, 0.5f - node.Sprite.Center.Y/sourceRegion.Height);
                    if (node.Sprite.Orientation == ImageOrientation.Rotated90)
                    {
                        var oldCenterX = normalizedCenter.X;
                        normalizedCenter.X = -normalizedCenter.Y;
                        normalizedCenter.Y = oldCenterX;
                    }
                    // apply the offset due to the center of the sprite
                    var size = node.Sprite.Size;
                    var centerOffset = Vector2.Modulate(normalizedCenter, size);
                    worldMatrix.M41 -= centerOffset.X*worldMatrix.M11 + centerOffset.Y*worldMatrix.M21;
                    worldMatrix.M42 -= centerOffset.X*worldMatrix.M12 + centerOffset.Y*worldMatrix.M22;

                    // draw the sprite
                    sprite3DBatch.Draw(texture, ref worldMatrix, ref sourceRegion, ref size, ref color4, node.Sprite.Orientation, SwizzleMode.None, projectedZ);
                }
            }

            if(hasBegin) sprite3DBatch.End();
        }

        private EffectInstance GetOrCreateSelectedSpriteEffect()
        {
            return selectedSpriteEffect ?? (selectedSpriteEffect = new EffectInstance(RenderSystem.EffectSystem.LoadEffect("SelectedSprite").WaitForResult()));
        }

        private EffectInstance GetOrCreatePickingSpriteEffect()
        {
            return pickingSpriteEffect ?? (pickingSpriteEffect = new EffectInstance(RenderSystem.EffectSystem.LoadEffect("SpritePicking").WaitForResult()));
        }

        public override Type SupportedRenderObjectType => typeof(RenderSpriteStudio);
    }
}