﻿using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;

namespace Pixie
{
    /// <summary>
    /// Generates empty grid pattern bitmap
    /// </summary>
    internal class PatternGenerator
    {
        private PixelSettings _settings;
        private Color _delimeterColor;
        private Color _backGroundColor;
        private Dictionary<int, Color> Colors = new Dictionary<int, Color>();

        public PatternGenerator(PixelSettings settings)
        {
            _settings = settings;
            _delimeterColor = ColorTranslator.FromHtml(_settings.DelimeterColor);
            _backGroundColor = ColorTranslator.FromHtml(_settings.BackgroundColor);
            foreach (var i in settings.ColorMapping)
            {
                Colors.Add(i.Value, ColorTranslator.FromHtml(i.Key));
            }
        }

        /// <summary>
        /// Fills bitmap with solid color and draws a grid on it
        /// </summary>
        /// <param name="patternWidthCount">width of grid in symbols (cells)</param>
        /// <param name="patternHeightCount">height of grid in symbols (cells)</param>
        /// <returns>production ready bitmap</returns>
        public Bitmap GeneratePattern(int patternWidthCount, int patternHeightCount,
            EnumerationStyle enumerationStyle, byte[] sampleData = null)
        {
            if (enumerationStyle != EnumerationStyle.None)
            {
                patternHeightCount++;
                patternWidthCount++;
            }

            var pattentWidth = patternWidthCount * _settings.SymbolWidth +
                               (patternWidthCount - 1) * _settings.DelimeterWidth;
            var patternHeight = patternHeightCount * _settings.SymbolHeight +
                                (patternHeightCount - 1) * _settings.DelimeterHeight;
            var pattern = new Bitmap(pattentWidth, patternHeight);

            ConsoleLogger.WriteMessage($"Generating pattern\nBitmap size {pattentWidth} x {patternHeight} px", MessageType.Info);

            FillBackground(pattern);
            DrawVerticalLines(pattern);
            DrawHorizontalLines(pattern);
            Enumerate(pattern, enumerationStyle);
            if (sampleData != null)
                FillSampleData(pattern, sampleData);
            return pattern;
        }

        /// <summary>
        /// Fills background with a solid color
        /// </summary>
        /// <param name="pattern">bitmap to fill</param>
        private void FillBackground(Bitmap pattern)
        {
            var g = Graphics.FromImage(pattern);
            g.FillRectangle(new SolidBrush(_backGroundColor), new Rectangle(0, 0, pattern.Width, pattern.Height));
        }

        /// <summary>
        /// Draws horizontal lines, wich will devide grid cells
        /// </summary>
        /// <param name="pattern">bitmap to draw in</param>
        private void DrawHorizontalLines(Bitmap pattern)
        {
            var g = Graphics.FromImage(pattern);
            for (var i = _settings.SymbolHeight; i < pattern.Height; i += _settings.SymbolHeight + _settings.DelimeterHeight)
            {
                g.DrawLine(new Pen(_delimeterColor, _settings.DelimeterHeight), 0, i, pattern.Width, i);
            }

        }

        /// <summary>
        /// Draws vertical lines, wich will devide grid cells
        /// </summary>
        /// <param name="pattern">bitmap to draw in</param>
        private void DrawVerticalLines(Bitmap pattern)
        {
            var g = Graphics.FromImage(pattern);
            for (var i = _settings.SymbolWidth; i < pattern.Width; i += _settings.SymbolWidth + _settings.DelimeterWidth)
            {
                g.DrawLine(new Pen(_delimeterColor, _settings.DelimeterWidth), i, 0, i, pattern.Height);
            }
        }

        /// <summary>
        /// Draws line and column numbers
        /// </summary>
        /// <param name="pattern">bitmap to draw in</param>
        /// <param name="enumerationStyle">style of digits</param>
        private void Enumerate(Bitmap pattern, EnumerationStyle enumerationStyle)
        {
            // 72 pixels in one pt
            const int PixelsPerPoint = 72;
            if (enumerationStyle == EnumerationStyle.None)
                return;

            var graphics = Graphics.FromImage(pattern);

            // rows and column numbers will 2 times smaller 
            var fontSize = (float)(_settings.SymbolHeight * 0.5 * PixelsPerPoint / pattern.VerticalResolution);
            // align vertically in center
            int topPadding = _settings.SymbolHeight / 4 - 1;

            var font = new Font(FontFamily.GenericMonospace, fontSize);
            var brush = new SolidBrush(_delimeterColor);

            // select string format specifier based on enumeration style
            var numbersStyle = enumerationStyle == EnumerationStyle.Hex ? "X" : "D2";

            // make numbers more readable on low resolutions
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Enumerate rows
            for (int rowHeight = _settings.SymbolHeight + _settings.DelimeterHeight, rowNumber = 0, i = rowHeight;
                i < pattern.Height;
                i += rowHeight)
            {
                graphics.DrawString(rowNumber++.ToString(numbersStyle), font, brush, 0, i + topPadding);
            }

            // Enumerate columns
            for (int columnWidth = _settings.SymbolWidth + _settings.DelimeterWidth, columnNumber = 0, i = columnWidth;
                i < pattern.Width;
                i += columnWidth)
            {
                graphics.DrawString(columnNumber++.ToString(numbersStyle), font, brush, i, topPadding);
            }
        }

        private void FillSampleData(Bitmap pattern, byte[] sampleData)
        {
            // todo: add arrat length check
            var bitArray = new BitArray(sampleData);
            int columnWidth = _settings.SymbolWidth + _settings.DelimeterWidth;
            int rowHeight = _settings.SymbolHeight + _settings.DelimeterHeight;
            int rowCount = pattern.Width / columnWidth;
            int bitArrayIndex = 0;

            for (int j = 0; j < pattern.Height; j += _settings.SymbolHeight + _settings.DelimeterHeight)
            {
                for (int i = 0; i < pattern.Width; i += _settings.SymbolWidth + _settings.DelimeterWidth)
                {
                    FillSymbol(i, j, pattern, bitArray, ref bitArrayIndex);
                }
            }


        }

        private void FillSymbol(int i, int j, Bitmap pattern, BitArray sampleData, ref int index)
        {
            for (int y = j; y < j + _settings.SymbolHeight; y++)
            {
                for (int x = i; x < _settings.SymbolWidth + i; x++, index += _settings.BitsPerPixel)
                {
                    var pixelValue = sampleData.ToInt32(index, _settings.BitsPerPixel);
                    pattern.SetPixel(x, y, Colors[pixelValue]);
                }
            }
        }
    }
}