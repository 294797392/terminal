﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Document
{
    public enum VTCharOptions
    {
        /// <summary>
        /// 单字节字符
        /// </summary>
        SingleBytesChar,

        /// <summary>
        /// 多字节字符
        /// </summary>
        MulitBytesChar
    }

    public enum VTextSources
    {
        StringTextSource,
        CharactersTextSource
    }

    public static class VTextSourceFactory
    {
        public static VTextSource Create(VTextSources type)
        {
            switch (type)
            {
                case VTextSources.CharactersTextSource: return new VTCharactersTextSource();
                //case VTextSources.StringTextSource: return new VTStringTextSource();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public abstract class VTextSource
    {
        protected static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(VTextSource));

        #region 实例变量

        #endregion

        #region 属性

        /// <summary>
        /// 获取该行字符所占用的总列数
        /// 如果是宽字符的话（比如中文），那么一个字符占用两列
        /// </summary>
        public abstract int Columns { get; protected set; }

        #endregion

        public VTextSource()
        {
        }

        #region 公开接口

        /// <summary>
        /// 通过在右侧填充指定的字符来达到指定的长度
        /// </summary>
        /// <param name="column"></param>
        /// <param name="padChar"></param>
        public void PadRight(int column, char padChar)
        {
            if (this.Columns >= column)
            {
                return;
            }

            int count = column - this.Columns;

            for (int i = 0; i < count; i++)
            {
                this.AddCharacter(padChar);
            }
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 设置该文本数据源的列数
        /// </summary>
        /// <param name="add">增加的列数</param>
        /// <param name="changedColumns">改变的列数</param>
        protected virtual void ResizeColumn(bool add, int changedColumns)
        { }

        /// <summary>
        /// 删除整行字符
        /// </summary>
        public abstract void DeleteAll();

        /// <summary>
        /// 获取文本数据
        /// </summary>
        /// <returns></returns>
        public abstract string GetText();

        public abstract void Insert(int column, char ch);
        public abstract void Remove(int column);
        public abstract void Remove(int column, int count);
        public abstract void SetCharacter(int column, char setChar);

        /// <summary>
        /// 把文本设置到该文本数据源里
        /// 从第一列开始
        /// </summary>
        /// <param name="text">要设置的文本</param>
        public abstract void SetText(string text);

        /// <summary>
        /// 在末尾Append一个字符
        /// </summary>
        /// <param name="addChar"></param>
        public abstract void AddCharacter(char addChar);

        #endregion
    }

    //public class VTStringTextSource : VTextSource
    //{
    //    private string text = string.Empty;

    //    public VTStringTextSource() 
    //    {
    //    }

    //    public override int Columns => this.text.Length;

    //    public override void DeleteAll()
    //    {
    //        this.text = string.Empty;
    //    }

    //    public override string GetText()
    //    {
    //        return this.text;
    //    }

    //    public override void Insert(int column, char ch)
    //    {
    //        this.text = this.text.Insert(column, char.ToString(ch));
    //    }

    //    public override void AddCharacter(char addChar)
    //    {
    //        this.text = (this.text += char.ToString(addChar));
    //    }

    //    public override void Remove(int column)
    //    {
    //        this.text = this.text.Remove(column);
    //    }

    //    public override void Remove(int column, int count)
    //    {
    //        this.text = this.text.Remove(count, count);
    //    }

    //    public override void SetCharacter(int column, char setChar)
    //    {
    //        this.text = this.text.Remove(column, 1).Insert(column, char.ToString(setChar));
    //    }

    //    public override void SetText(string text)
    //    {
    //        this.text = text;
    //    }
    //}

    public class VTCharactersTextSource : VTextSource
    {
        #region 实例变量

        private List<VTCharacter> characters;

        #endregion

        #region 属性

        public override int Columns { get; protected set; }

        #endregion

        #region 构造方法

        public VTCharactersTextSource() 
        {
            this.characters = new List<VTCharacter>();
        }

        #endregion

        #region 实例方法

        private VTCharacter CreateCharacter(char ch)
        {
            return new VTCharacter(ch);
        }

        /// <summary>
        /// 更新文本源所占用的列数
        /// </summary>
        private void UpdateColumns()
        {
            this.Columns = this.characters.Sum(v => v.Columns);
        }

        #endregion

        #region 重写方法

        public override void DeleteAll()
        {
            this.characters.Clear();
            this.Columns = 0;
        }

        public override string GetText()
        {
            string text = string.Empty;

            foreach (VTCharacter character in this.characters)
            {
                text += character.Character;
            }

            return text;
        }

        public override void Insert(int column, char ch)
        {
            this.characters.Insert(column, this.CreateCharacter(ch));
        }

        public override void Remove(int column)
        {
            this.characters.RemoveRange(column, this.characters.Count - column);
        }

        public override void Remove(int column, int count)
        {
            this.characters.RemoveRange(column, count);
        }

        public override void AddCharacter(char addChar)
        {
            this.characters.Add(this.CreateCharacter(addChar));
        }

        public override void SetCharacter(int column, char setChar)
        {
            this.characters[column].Character = setChar;
        }

        public override void SetText(string text)
        {
            this.characters.Clear();

            foreach (char ch in text)
            {
                this.characters.Add(new VTCharacter(ch));
            }
        }

        #endregion
    }
}
