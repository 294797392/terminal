﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using XTerminalDevice;
using XTerminalParser;

namespace XTerminal.WPFRenderer
{
    public static class TerminalUtils
    {
        public static Brush VTForeground2Brush(VTForeground foreground)
        {
            switch (foreground)
            {
                case VTForeground.BrightBlack: return Brushes.Black;
                case VTForeground.BrightBlue: return Brushes.Blue;
                case VTForeground.BrightCyan: return Brushes.Cyan;
                case VTForeground.BrightGreen: return Brushes.Green;
                case VTForeground.BrightMagenta: return Brushes.Magenta;
                case VTForeground.BrightRed: return Brushes.Red;
                case VTForeground.BrightWhite: return Brushes.White;
                case VTForeground.BrightYellow: return Brushes.Yellow;

                case VTForeground.DarkBlack: return Brushes.Black;
                case VTForeground.DarkBlue: return Brushes.DarkBlue;
                case VTForeground.DarkCyan: return Brushes.DarkCyan;
                case VTForeground.DarkGreen: return Brushes.DarkGreen;
                case VTForeground.DarkMagenta: return Brushes.DarkMagenta;
                case VTForeground.DarkRed: return Brushes.DarkRed;
                case VTForeground.DarkWhite: return Brushes.White;
                case VTForeground.DarkYellow: return Brushes.Yellow;

                default:
                    throw new NotImplementedException();
            }
        }

        public static FormattedText CreateFormattedText(VTextBlock textBlock, Typeface typeface, double pixelsPerDip)
        {
            Brush foreground = TerminalUtils.VTForeground2Brush(textBlock.Foreground);
            FormattedText formattedText = new FormattedText(textBlock.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, textBlock.Size, foreground, pixelsPerDip);
            return formattedText;
        }

        public static void UpdateTextMetrics(VTextBlock textBlock, FormattedText formattedText)
        {
            textBlock.Metrics.Width = formattedText.Width;
            textBlock.Metrics.WidthIncludingWhitespace = formattedText.WidthIncludingTrailingWhitespace;
            textBlock.Metrics.Height = formattedText.Height;
        }

        public static VTKeys ConvertToVTKey(Key key)
        {
            return (VTKeys)key;
        }
    }
}