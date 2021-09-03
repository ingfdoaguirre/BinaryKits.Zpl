﻿using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.ElementDrawers;
using SkiaSharp;
using System;
using System.Linq;

namespace BinaryKits.Zpl.Viewer
{
    public class ZplElementDrawer
    {
        private readonly IPrinterStorage _printerStorage;
        private readonly IElementDrawer[] _elementDrawers;

        public ZplElementDrawer(IPrinterStorage printerStorage)
        {
            this._printerStorage = printerStorage;
            this._elementDrawers = new IElementDrawer[]
            {
                new Barcode128ElementDrawer(),
                new Barcode39ElementDrawer(),
                new FieldBlockElementDrawer(),
                new GraphicBoxElementDrawer(),
                new GraphicCircleElementDrawer(),
                new GraphicFieldElementDrawer(),
                new Interleaved2of5BarcodeDrawer(),
                new ImageMoveElementDrawer(),
                new QrCodeElementDrawer(),
                new RecallGraphicElementDrawer(),
                new TextFieldElementDrawer()
            };
        }

        /// <summary>
        /// Draw the label
        /// </summary>
        /// <param name="elements">Zpl elements</param>
        /// <param name="labelWidth">Label width in millimeter</param>
        /// <param name="labelHeight">Label height in millimeter</param>
        /// <param name="printDensityDpmm">Dots per millimeter</param>
        /// <returns></returns>
        public byte[] Draw(
            ZplElementBase[] elements,
            double labelWidth = 102,
            double labelHeight = 152,
            int printDensityDpmm = 8)
        {
            var labelImageWidth = Convert.ToInt32(labelWidth * printDensityDpmm);
            var labelImageHeight = Convert.ToInt32(labelHeight * printDensityDpmm);

            using var skBitmap = new SKBitmap(labelImageWidth, labelImageHeight);
            using var skCanvas = new SKCanvas(skBitmap);
            skCanvas.Clear(SKColors.White);

            foreach (var element in elements)
            {
                var drawer = this._elementDrawers.SingleOrDefault(o => o.CanDraw(element));
                if (drawer != null)
                {
                    if (drawer.IsReverseDraw(element))
                    {
                        using var skBitmapInvert = new SKBitmap(skBitmap.Width, skBitmap.Height);
                        using var skCanvasInvert = new SKCanvas(skBitmapInvert);

                        drawer.Prepare(this._printerStorage, skCanvasInvert);

                        drawer.Draw(element);

                        this.InvertDraw(skBitmap, skBitmapInvert);
                        continue;
                    }

                    drawer.Prepare(this._printerStorage, skCanvas);
                    drawer.Draw(element);

                    continue;
                }
            }

            using var data = skBitmap.Encode(SKEncodedImageFormat.Png, 80);
            return data.ToArray();
        }

        private void InvertDraw(SKBitmap skBitmap, SKBitmap skBitmapInvert)
        {
            byte invertBlue, originalBlue;
            var originalBytes = new Span<byte>(skBitmap.Bytes);
            var invertBytes = new Span<byte>(skBitmapInvert.Bytes);

            int total = skBitmapInvert.Pixels.Length;
            for (int i = 0; i < total; i++)
            {
                // RGBA8888
                int alphaByte = i * 4 + 3;
                if (invertBytes[alphaByte] == 0)
                {
                    continue;
                }
                
                var targetColor = SKColors.White;
                invertBlue = invertBytes[alphaByte - 1];
                originalBlue = originalBytes[alphaByte - 1];

                if (invertBlue == originalBlue)
                {
                    if (invertBlue == 255)
                    {
                        targetColor = SKColors.Black;
                    }
                }
                else
                {
                    if (invertBlue == 0)
                    { 
                        targetColor = SKColors.Black;
                    }
                }

                skBitmap.SetPixel(i % skBitmapInvert.Width, i / skBitmapInvert.Width, targetColor);
            }
        }
    }
}
