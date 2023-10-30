﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.Rendering
{
    public class HashBlockPixel : BlockPixel<int>
    {
        public HashBlockPixel(string pixel)
        {
            if (pixel is null)
                throw new ArgumentNullException(nameof(pixel));

            _blockConverter = new();
            Pixel = _blockConverter[pixel];
        }

        public HashBlockPixel(int pixel)
        {
            _blockConverter = new();
            Pixel = pixel;
        }

        private readonly HashBlockConverter _blockConverter;

        public override IBlockConverter<int> BlockConverter => _blockConverter;

        public override int Pixel { get; }

        public override BlockPixel Clone()
        {
            return new HashBlockPixel(Pixel);
        }
    }
}
