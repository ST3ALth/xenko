﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Paradox.Effects;

namespace SiliconStudio.Paradox.Engine.Graphics
{
    /// <summary>
    /// A default implementation for a <see cref="IEntityComponentRenderer"/>.
    /// </summary>
    public abstract class EntityComponentRendererBase : EntityComponentRendererCoreBase, IEntityComponentRenderer
    {
        public void Prepare(RenderContext context, RenderItemCollection opaqueList, RenderItemCollection transparentList)
        {
            PrepareCore(context, opaqueList, transparentList);
        }

        public void Draw(RenderContext context, RenderItemCollection renderItems, int fromIndex, int toIndex)
        {
            PreDrawCoreInternal(context);
            DrawCore(context, renderItems, fromIndex, toIndex);
            PostDrawCoreInternal(context);
        }

        protected abstract void PrepareCore(RenderContext context, RenderItemCollection opaqueList, RenderItemCollection transparentList);

        protected abstract void DrawCore(RenderContext context, RenderItemCollection renderItems, int fromIndex, int toIndex);
    }
}