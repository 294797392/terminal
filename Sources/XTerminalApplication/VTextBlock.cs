﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminalParser;

namespace XTerminalDevice
{
    public enum DeleteCharacterFrom
    {
        /// <summary>
        /// 从前往后删除字符
        /// </summary>
        FrontToBack,

        /// <summary>
        /// 从后往前删除字符
        /// </summary>
        BackToFront
    }

    public class VTextBlock
    {
        /// <summary>
        /// TextBlock的索引号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 文本的颜色
        /// </summary>
        public VTForeground Foreground { get; set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        /// 要显示的文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 该文本块左上角的X坐标
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 该文本块左上角的Y坐标
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 字符个数
        /// </summary>
        public int Characters { get { return this.Text.Length; } }

        /// <summary>
        /// 文本的测量信息
        /// Draw完之后就有了测量信息
        /// </summary>
        public VTextBlockMetrics Metrics { get; private set; }

        /// <summary>
        /// 获取该文本块的宽度
        /// </summary>
        public double Width { get { return this.Metrics.WidthIncludingWhitespace; } }

        /// <summary>
        /// 获取该文本块的高度
        /// </summary>
        public double Height { get { return this.Metrics.Height; } }

        /// <summary>
        /// 获取该文本的矩形框
        /// </summary>
        public VTRect Boundary { get { return new VTRect(this.X, this.Y, this.Width, this.Height); } }

        /// <summary>
        /// 该文本块所在行数，从0开始
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// 该文本块第一个字符所在列数，从0开始
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// 获取该文本块所占据的列数
        /// </summary>
        public int Columns { get { return this.Text.Length; } }

        /// <summary>
        /// 所属行
        /// </summary>
        public VTextLine OwnerLine { get; internal set; }

        public VTextBlock()
        {
            this.Metrics = new VTextBlockMetrics();
        }

        /// <summary>
        /// 向文本块里插入字符
        /// </summary>
        /// <param name="text"></param>
        public void InsertCharacter(char text)
        {
            this.Text += text;
        }

        public void InsertText(string text)
        {
            this.Text += text;
        }

        /// <summary>
        /// 删除字符
        /// </summary>
        /// <param name="startIndex">要删除的字符的起始位置</param>
        /// <param name="count">要删除的字符的个数</param>
        public void DeleteCharacter(int startIndex, int count)
        {
            if (this.Text.Length == 0)
            {
                return;
            }

            this.Text = this.Text.Remove(startIndex, count);
        }

        /// <summary>
        /// 删除字符
        /// </summary>
        /// <param name="from">指定删除字符的位置</param>
        /// <param name="count">指定要删除的字符个数</param>
        public void DeleteCharacter(DeleteCharacterFrom from, int count)
        {
            switch (from)
            {
                case DeleteCharacterFrom.FrontToBack:
                    {
                        this.DeleteCharacter(0, count);
                        break;
                    }

                case DeleteCharacterFrom.BackToFront:
                    {
                        int startIndex = this.Text.Length - count;
                        this.DeleteCharacter(startIndex, count);
                        break;
                    }

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
