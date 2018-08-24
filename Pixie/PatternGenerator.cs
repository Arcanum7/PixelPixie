﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
                FillSampleData(pattern, sampleData, enumerationStyle);
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

        private void FillSampleData(Bitmap pattern, byte[] sampleData, EnumerationStyle enumerationStyle)
        {
            var bitArray = sampleData.ToBitArray();
            int bitArrayIndex = 0;
            var cellWidth = _settings.SymbolWidth + _settings.DelimeterWidth;
            var cellHeight = _settings.SymbolHeight + _settings.DelimeterHeight;

            var directionSelector = LookUpDirectionSelector.GetSelector(LookupDirection.RowWise);

            // Skip first row and columnt if they were used for rows/columns numbers
            directionSelector.RowStart = enumerationStyle == EnumerationStyle.None
                ? 0
                : cellWidth;
            directionSelector.ColumntStart = enumerationStyle == EnumerationStyle.None
                ? 0
                : cellHeight;

            directionSelector.RowEnd = pattern.Width;
            directionSelector.ColumnEnd = pattern.Height;

            directionSelector.RowDelta = cellWidth;
            directionSelector.ColumntDelta = cellHeight;

            // Process cells
            directionSelector.ProcessSymbol((x, y) => FillSymbol(x, y, pattern, bitArray, ref bitArrayIndex));
        }

        private void FillSymbol(int i, int j, Bitmap pattern, BitArray sampleData, ref int index)
        {
            var directionSelector = LookUpDirectionSelector.GetSelector(_settings.LookupDirection);
            directionSelector.RowStart = i;
            directionSelector.ColumntStart = j;
            directionSelector.RowEnd = _settings.SymbolWidth + i;
            directionSelector.ColumnEnd = _settings.SymbolHeight + j;
            directionSelector.RowDelta = 1;
            directionSelector.ColumntDelta = 1;

            // lamba cannot use ref value
            var indexClosure = index;
            try
            {
                directionSelector.ProcessSymbol((x, y) =>
                {
                    // If there was not enough data, just skip the symbol (leaves cell empty)
                    if (indexClosure >= sampleData.Length)
                        return;
                    var pixelValue = sampleData.ToByte(indexClosure, _settings.BitsPerPixel);
                    pattern.SetPixel(x, y, Colors[pixelValue]);
                    indexClosure += _settings.BitsPerPixel;
                });
            }
            finally
            {
                // restore value so the ref make sense
                index = indexClosure;
            }
        }
    }
}