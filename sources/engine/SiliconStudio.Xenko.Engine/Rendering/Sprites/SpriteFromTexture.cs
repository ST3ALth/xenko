﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Rendering.Sprites
{
    /// <summary>
    /// A <see cref="Sprite"/> provider from a <see cref="Texture"/>.
    /// </summary>
    [DataContract("SpriteFromTexture")]
    [Display("Texture")]
    public class SpriteFromTexture : ISpriteProvider
    {
        private float pixelsPerUnit;
        private Vector2 center;
        private Texture texture;
        private bool isTransparent;
        private bool centerFromMiddle;

        private bool isSpriteDirty = true;
        private readonly Sprite sprite = new Sprite();
        
        /// <summary>
        /// Creates a new instance of <see cref="SpriteFromTexture"/>.
        /// </summary>
        public SpriteFromTexture()
        {
            PixelsPerUnit = 100;
            CenterFromMiddle = true;
            IsTransparent = true;
        }

        private SpriteFromTexture(Sprite source)
            : this()
        {
            sprite = source;
            isSpriteDirty = false;

            center = sprite.Center;
            centerFromMiddle = false;
            isTransparent = sprite.IsTransparent;
            // FIXME: should we use the Max, Min, average of X and/or Y?
            pixelsPerUnit = sprite.PixelsPerUnit.X;
            texture = sprite.Texture;
        }

        /// <summary>
        /// Gets or sets the texture of representing the sprite
        /// </summary>
        /// <userdoc>Specify the texture to use as sprite</userdoc>
        [DataMember(5)]
        [InlineProperty]
        public Texture Texture
        {
            get { return texture; }
            set
            {
                texture = value;
                isSpriteDirty = true;
                UpdateSprite();
            }
        }

        /// <summary>
        /// Gets or sets the value specifying the size of one pixel in scene units
        /// </summary>
        /// <userdoc>
        /// Specify the size in pixels of one unit in the scene.
        /// </userdoc>
        [DataMember(8)]
        [DefaultValue(100)]
        public float PixelsPerUnit
        {
            get { return pixelsPerUnit; }
            set
            {
                pixelsPerUnit = value;
                isSpriteDirty = true;
                UpdateSprite();
            }
        }

        /// <summary>
        /// The position of the center of the image in pixels.
        /// </summary>
        /// <userdoc>
        /// The position of the center of the sprite in pixels. 
        /// Depending on the value of 'CenterFromMiddle', it is the offset from the top/left corner or the middle of the image.
        /// </userdoc>
        [DataMember(10)]
        public Vector2 Center
        {
            get { return center; }
            set
            {
                center = value;
                isSpriteDirty = true;
                UpdateSprite();
            }
        }

        /// <summary>
        /// Gets or sets the value indicating position provided to <see cref="Center"/> is from the middle of the sprite region or from the left/top corner.
        /// </summary>
        /// <userdoc>
        /// If checked, the value in 'Center' represents the offset of the sprite center from the middle of the image.
        /// </userdoc>
        [DataMember(15)]
        [DefaultValue(true)]
        public bool CenterFromMiddle
        {
            get { return centerFromMiddle; }
            set
            {
                centerFromMiddle = value;
                isSpriteDirty = true;
                UpdateSprite();
            }
        }

        /// <summary>
        /// Gets or sets the transparency value of the sprite.
        /// </summary>
        /// <userdoc>
        /// If checked, the sprite is considered as having transparent colors.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(true)]
        public bool IsTransparent
        {
            get { return isTransparent; }
            set
            {
                isTransparent = value;
                isSpriteDirty = true;
                UpdateSprite();
            }
        }

        /// <inheritdoc/>
        public int SpritesCount => sprite == null ? 0 : 1;

        /// <inheritdoc/>
        public Sprite GetSprite()
        {
            if (isSpriteDirty)
            {
                UpdateSprite();
                isSpriteDirty = false;
            }
            // Note: This "isDirty" system is needed because the texture size is not valid 
            // when the texture is set for the first time by the serializer (texture are loaded in two times)

            // Workaround for deserialized texures. Keep dirty while the texture is not fully deserialized.
            if (texture?.Size == Size3.Zero)
                isSpriteDirty = true;

            return sprite;
        }

        private void UpdateSprite()
        {
            sprite.Texture = texture;
            sprite.IsTransparent = isTransparent;
            sprite.PixelsPerUnit = new Vector2(PixelsPerUnit);
            if (texture != null)
            {
                sprite.Center = center + (centerFromMiddle ? new Vector2(texture.Width, texture.Height) / 2 : Vector2.Zero);
                sprite.Region = new RectangleF(0, 0, texture.Width, texture.Height);
            }
        }

        public static explicit operator SpriteFromTexture(Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            return new SpriteFromTexture(sprite);
        }
    }
}
