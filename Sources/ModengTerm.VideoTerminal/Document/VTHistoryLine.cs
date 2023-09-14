﻿using DotNEToolkit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base;

namespace XTerminal.Document
{
    /// <summary>
    /// 用来存储历史的行记录
    /// </summary>
    public class VTHistoryLine
    {
        /// <summary>
        /// 行索引，从0开始
        /// </summary>
        public int PhysicsRow { get; private set; }

        /// <summary>
        /// 上一行
        /// </summary>
        public VTHistoryLine PreviousLine { get; internal set; }

        /// <summary>
        /// 下一行
        /// </summary>
        public VTHistoryLine NextLine { get; internal set; }

        /// <summary>
        /// 该行的所有字符
        /// 显示历史行的时候用到
        /// </summary>
        public List<VTCharacter> Characters { get; private set; }

        private VTHistoryLine()
        {
            this.Characters = new List<VTCharacter>();
        }

        /// <summary>
        /// 设置该历史行的数据
        /// </summary>
        /// <param name="textLine">要设置的行</param>
        public void SetVTextLine(VTextLine textLine)
        {
            this.PhysicsRow = textLine.PhysicsRow;
            // 复制一份字符列表
            this.Characters.Clear();
            this.Characters.AddRange(textLine.Characters);
        }

        /// <summary>
        /// 从VTextLine创建一个VTHistoryLine
        /// </summary>
        /// <param name="fromLine"></param>
        /// <returns></returns>
        public static VTHistoryLine Create(int row, VTHistoryLine previousLine, VTextLine sourceLine)
        {
            VTHistoryLine historyLine = new VTHistoryLine()
            {
                PhysicsRow = row,
                PreviousLine = previousLine
            };

            if (previousLine != null)
            {
                previousLine.NextLine = historyLine;
            }

            return historyLine;
        }
    }
}