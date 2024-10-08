﻿using ModengTerm.Document;
using ModengTerm.Terminal.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ModengTerm.Document.Rendering
{
    /// <summary>
    /// 用来画光标选中的文本的Drawable
    /// </summary>
    public class DrawingSelection : DrawingObject, IDrawingSelection
    {
        private static readonly Brush DefaultSelectionBrush = Application.Current.FindResource("BrushSelectionBackground") as Brush;

        private StreamGeometry selectionGeometry;
        private Pen pen;
        private Brush brush;

        public List<VTRect> Geometry { get; set; }

        public DrawingSelection()
        {
        }

        protected override void OnInitialize()
        {
            selectionGeometry = new StreamGeometry();
            pen = new Pen(Brushes.Transparent, 1);
            brush = DefaultSelectionBrush;
        }

        protected override void OnRelease()
        {
        }

        protected override void OnDraw(DrawingContext dc)
        {
            StreamGeometryContext sgc = selectionGeometry.Open();

            foreach (VTRect bounds in Geometry)
            {
                sgc.BeginFigure(new Point(bounds.LeftTop.X, bounds.LeftTop.Y), true, true);
                sgc.LineTo(new Point(bounds.RightTop.X, bounds.RightTop.Y), true, true);
                sgc.LineTo(new Point(bounds.RightBottom.X, bounds.RightBottom.Y), true, true);
                sgc.LineTo(new Point(bounds.LeftBottom.X, bounds.LeftBottom.Y), true, true);
            }

            sgc.Close();

            dc.DrawGeometry(brush, pen, selectionGeometry);
        }
    }
}