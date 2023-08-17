﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Document.Rendering
{
    /// <summary>
    /// 定义文本测量接口
    /// </summary>
    public abstract class VTextMeter
    {
        /// <summary>
        /// 测量某个文本行的大小
        /// 测量后的结果存储在VTextLine.Metrics属性里
        /// </summary>
        /// <param name="textLine">要测量的数据模型</param>
        /// <returns></returns>
        public abstract void MeasureLine(VTextLine textLine);

        /// <summary>
        /// 测量指定文本里的子文本的矩形框
        /// </summary>
        /// <param name="textLine">要测量的数据模型</param>
        /// <param name="startIndex">要测量的起始字符索引</param>
        /// <param name="count">要测量的最大字符数，0为全部测量</param>
        /// <returns></returns>
        public abstract VTRect MeasureLine(VTextLine textLine, int startIndex, int count);

        /// <summary>
        /// 测量一行里某个字符的测量信息
        /// 注意该接口只能测量出来X偏移量，Y偏移量需要外部根据高度自己计算
        /// </summary>
        /// <param name="textLine">要测量的文本行</param>
        /// <param name="characterIndex">要测量的字符</param>
        /// <returns>文本坐标，X=文本左边的X偏移量，Y永远是0，因为边界框是相对于该行的</returns>
        public abstract VTRect MeasureCharacter(VTextLine textLine, int characterIndex);

        public abstract VTextMetrics MeasureText(string text, double fontSize, string fontFamily);
    }
}
