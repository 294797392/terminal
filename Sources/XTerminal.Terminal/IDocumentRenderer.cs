﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Document;

namespace XTerminal.Terminal
{
    public enum ScrollOrientation
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public interface IDocumentRenderer
    {
        /// <summary>
        /// 测量某个文本块的属性
        /// </summary>
        /// <param name="text">要测量的文本</param>
        /// <param name="style">文本的样式</param>
        /// <returns></returns>
        VTextMetrics MeasureText(string text, VTextStyle style);

        /// <summary>
        /// 重新绘制一行文本
        /// </summary>
        /// <param name="textLine"></param>
        void RenderLine(VTextLine textLine);

        /// <summary>
        /// 根据VTextLine里的测量信息进行布局（移动文本行的位置）
        /// </summary>
        /// <param name="textLine"></param>
        void ArrangeLine(VTextLine textLine);

        /// <summary>
        /// 清除现实的内容并把状态还原
        /// </summary>
        void Reset();

        /// <summary>
        /// 重新调整终端大小
        /// </summary>
        /// <param name="width">终端的宽度</param>
        /// <param name="height">终端高度</param>
        void Resize(double width, double height);

        /// <summary>
        /// 滚动到某个方向的最底部
        /// </summary>
        void ScrollToEnd(ScrollOrientation direction);
    }
}
