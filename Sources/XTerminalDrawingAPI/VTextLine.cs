﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Drawing
{
    /// <summary>
    /// 1. 对文本行进行排版，分块
    /// 2. 维护行的测量信息
    /// </summary>
    public class VTextLine
    {
        #region 实例变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("VTextLine");

        #endregion

        #region 属性

        /// <summary>
        /// 该行的索引
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// 终端行的最大列数
        /// 规定终端一行里的字符数不能超过列数
        /// 超过列数要按照手册里定义的标准来执行动作
        /// 在linux里使用stty size获取
        /// </summary>
        public int Columns { get; set; }

        /// <summary>
        /// 该行高度，有可能终端里的一行等于屏幕上的N行
        /// 当一行的字符超过终端的列数的时候，DECAWM指令指定了超出的字符要如何处理
        /// DECAWM SET：超出后要在新的一行上从头开始显示字符
        /// DECAWM RESET：超出后在该行的第一个字符处开始显示字符
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 该行所有的文本块
        /// </summary>
        public List<VTextBlock> TextBlocks { get; set; }

        /// <summary>
        /// 第一个文本块
        /// </summary>
        public VTextBlock First { get; private set; }

        /// <summary>
        /// 最后一个文本块
        /// </summary>
        public VTextBlock Last { get; private set; }

        /// <summary>
        /// 该行的Y偏移量
        /// </summary>
        public double OffsetY { get; set; }

        /// <summary>
        /// 该行文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 所属的Canvas
        /// </summary>
        public IDrawingCanvas OwnerCanvas { get; set; }

        /// <summary>
        /// 画图对象
        /// </summary>
        public object DrawingObject { get; set; }

        #endregion

        #region 构造方法

        public VTextLine()
        {
            this.Text = string.Empty;
            this.TextBlocks = new List<VTextBlock>();
        }

        #endregion

        #region 实例方法

        /// <summary>
        /// 删除空的文本，然后使文本左对齐
        /// 并更新所有TextBlock的测量信息
        /// </summary>
        private void LeftAlignment()
        {
            if (this.First == null)
            {
                return;
            }

            string blockText = string.Empty;

            #region 先对第一个元素排版

            this.First.OffsetX = 0;
            this.First.Column = 0;
            blockText = this.Text.Substring(this.First.Column, this.First.Columns);
            this.First.Metrics = this.OwnerCanvas.MeasureText(blockText, this.First.Style);

            #endregion

            // 从第二个元素开始遍历
            VTextBlock current = this.First.Next;

            while (current != null)
            {
                if (current.IsEmpty())
                {
                    // 如果这个文本块是空的，那么删除掉，并且更新current指针
                    this.DeleteTextWithoutAlignment(current);

                    // 移动current指针指向下一个元素
                    current = current.Next;

                    // 这里要continue，因为下一个元素也可能是空的
                    continue;
                }

                VTextBlock previous = current.Previous;
                if (previous != null)
                {
                    current.OffsetX = previous.OffsetX + previous.Width;
                    current.Column = previous.Column + previous.Columns + 1;
                }

                // 更新测量信息
                blockText = this.Text.Substring(current.Column, current.Columns);
                current.Metrics = this.OwnerCanvas.MeasureText(blockText, current.Style);
            }
        }

        /// <summary>
        /// 从缓存和链表里删除一个TextBlock
        /// 并更新链表指针
        /// </summary>
        /// <param name="textBlock">要删除的TextBlock</param>
        private void DeleteTextWithoutAlignment(VTextBlock textBlock)
        {
            if (this.TextBlocks.Count == 1 && this.TextBlocks[0] == textBlock)
            {
                // 此时该行只有一个TextBlock了，不能删光，要留一个备用
                return;
            }

            VTextBlock previous = textBlock.Previous;
            if (previous != null)
            {
                previous.Next = textBlock.Next;
            }

            VTextBlock next = textBlock.Next;
            if (next != null)
            {
                next.Previous = previous;
            }

            if (textBlock == this.First)
            {
                this.First = textBlock.Next;
            }

            if (textBlock == this.Last)
            {
                this.Last = textBlock.Previous;
            }

            this.TextBlocks.Remove(textBlock);
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 设置指定位置处的字符，然后进行排版
        /// 该操作会更新VTextBlock的测量信息和其他的文本块信息
        /// 
        /// 如果该位置有字符了，那么把该位置的字符替换
        /// </summary>
        /// <param name="ch">要插入的字符</param>
        /// <param name="position">索引位置，在此处插入字符串</param>
        public void SetCharacter(char ch, int position)
        {
            logger.InfoFormat("SetCharacter, position = {0}", position);
            VTextBlock textBlock = this.HitTestText(position);
            if (textBlock == null)
            {
                // 应该不会发生
                logger.ErrorFormat("InsertText失败, textBlock不存在, position = {0}", position);
                return;
            }

            // 更新文本
            if (position > this.Text.Length - 1)
            {
                // 说明是追加字符串操作
                this.Text = this.Text.Insert(position, char.ToString(ch));

                // 更新TextBlock的列数
                textBlock.Columns += 1;
            }
            else
            {
                this.Text = this.Text.Remove(position, 1).Insert(position, char.ToString(ch));
            }

            // 对齐
            this.LeftAlignment();
        }

        /// <summary>
        /// 在指定位置插入n个字符，要插入的字符由ch指定
        /// </summary>
        /// <param name="ch">要插入的字符</param>
        /// <param name="position">插入位置</param>
        /// <param name="n">要插入的字符数量</param>
        public void InsertCharacter(char ch, int position, int n)
        {
        }

        /// <summary>
        /// 从指定位置开始删除字符串，然后进行排版
        /// 该操作会更新VTextBlock的测量信息和其他的文本块信息
        /// 
        /// 删除后会对该行数据进行左对齐
        /// </summary>
        /// <param name="position">从此处开始删除字符</param>
        /// <param name="count">要删除的字符个数</param>
        public void DeleteText(int position)
        {
            this.DeleteText(position, this.Text.Length - position);
        }

        /// <summary>
        /// 删除指定位置处的字符串，然后进行排版
        /// 该操作会更新VTextBlock的测量信息和其他的文本块信息
        /// 
        /// 删除后会对该行数据进行左对齐
        /// </summary>
        /// <param name="position">从此处开始删除字符</param>
        /// <param name="count">要删除的字符个数</param>
        public void DeleteText(int position, int count)
        {
            VTextBlock startTextBlock = this.HitTestText(position);
            if (startTextBlock == null)
            {
                logger.ErrorFormat("DeleteText失败, startTextBlock不存在, position = {0}", position);
                return;
            }

            VTextBlock endTextBlock = this.HitTestText(position + count);
            if (endTextBlock == null)
            {
                logger.ErrorFormat("DeleteText失败, endTextBlock不存在, position = {0}", position + count);
                return;
            }

            this.Text = this.Text.Remove(position, count);

            if (startTextBlock == endTextBlock)
            {
                // 此时说明一个TextBlock就可以删完整个字符了
                startTextBlock.Columns -= count;
                if (startTextBlock.IsEmpty())
                {
                    this.DeleteTextWithoutAlignment(startTextBlock);
                }
            }
            else
            {
                // 先计算startTextBlock能删除多少字符
                int startDelete = startTextBlock.Columns - (position - startTextBlock.Column + 1);

                // 跨TextBlock删除, 把startTextBlock和endTextBlock中间的所有TextBlock删除, 并且计算一共删了多少个字符
                int deletes = 0;
                VTextBlock current = startTextBlock.Next;

                while (current != null && current != endTextBlock)
                {
                    this.DeleteTextWithoutAlignment(current);

                    deletes += current.Columns;

                    current = current.Next;
                }

                // 最后计算endTextBlock能删除多少字符
                int endDelete = count - startDelete - deletes;
                endTextBlock.Columns -= endDelete;
            }

            // 左对齐排版
            this.LeftAlignment();
        }

        /// <summary>
        /// 删除整行，然后进行排版
        /// 该操作会更新VTextBlock的测量信息和其他的文本块信息
        /// </summary>
        /// <param name="position">从此处开始删除字符</param>
        /// <param name="count">要删除的字符个数</param>
        public void DeleteAll()
        {
            // 删除整行要留一个TextBlock备用
            this.First.Column = 0;
            this.First.Columns = 0;
            this.First.Metrics = new VTextMetrics();

            // 如果链表的首尾相同，那么Next和Previous都设置成空指针
            this.First.Next = null;
            this.First.Previous = null;

            this.Last = this.First;

            this.Text = string.Empty;
            this.TextBlocks.Clear();
            this.TextBlocks.Add(this.First);
        }



        public void AddTextBlock(VTextBlock textBlock)
        {
            if (this.First == null)
            {
                this.First = textBlock;
                this.Last = textBlock;
            }
            else
            {
                this.Last.Next = textBlock;
                textBlock.Previous = this.Last;
                this.Last = textBlock;
            }
            this.TextBlocks.Add(textBlock);

            textBlock.OwnerLine = this;
        }

        /// <summary>
        /// 删除文本
        /// </summary>
        /// <param name="textBlock"></param>
        public void DeleteTextBlock(VTextBlock textBlock)
        {
            this.DeleteTextWithoutAlignment(textBlock);
        }

        /// <summary>
        /// 查询大于等于column列所有的文本块
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public List<VTextBlock> GetTextBlockAfter(int column)
        {
            return this.TextBlocks.Where(v => v.Column >= column || column >= v.Column && column <= v.Column + v.Columns).ToList();
        }

        /// <summary>
        /// 查询小于等于column列所有的文本块
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public List<VTextBlock> GetTextBlockBefore(int column)
        {
            return this.TextBlocks.Where(v => v.Column <= column || column >= v.Column && column <= v.Column + v.Columns).ToList();
        }

        /// <summary>
        /// 返回某个列所属的TextBlock
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public VTextBlock HitTestText(int column)
        {
            foreach (VTextBlock textBlock in this.TextBlocks)
            {
                int startCol = textBlock.Column;
                int endCol = textBlock.Column + textBlock.Columns;

                if (column >= startCol && column <= endCol)
                {
                    return textBlock;
                }
            }

            return null;
        }

        #endregion
    }
}
