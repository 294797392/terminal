﻿using DotNEToolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Parser;

namespace ModengTerm.Terminal
{
    public enum VTDebugCategoryEnum
    {
        /// <summary>
        /// ModengTerm和SSH主机交互的日志
        /// </summary>
        Interactive,

        /// <summary>
        /// 记录从SSH主机收到的原始数据
        /// </summary>
        RawRead,
    }

    public enum VTSendTypeEnum
    {
        /// <summary>
        /// 按键盘输入
        /// </summary>
        UserInput,

        /// <summary>
        /// 响应DSR事件
        /// </summary>
        DSR_DeviceStatusReport,

        /// <summary>
        /// 响应DA_DeviceAttributes事件
        /// </summary>
        DA_DeviceAttributes
    }

    /// <summary>
    /// 提供一些记录日志的工具函数
    /// </summary>
    public class VTDebug : SingletonObject<VTDebug>
    {
        /// <summary>
        /// 存储日志分类的上下文信息
        /// </summary>
        public class LogCategory
        {
            private bool enabled;

            public string Name { get; set; }

            /// <summary>
            /// 日志分类
            /// </summary>
            public VTDebugCategoryEnum Category { get; private set; }

            /// <summary>
            /// 日志文件的完整路径
            /// </summary>
            public string FilePath { get; set; }

            /// <summary>
            /// 是否记录该类型的日志
            /// </summary>
            public bool Enabled
            {
                get { return this.enabled; }
                set
                {
                    if (this.enabled != value)
                    {
                        this.enabled = value;

                        if (value)
                        {
                            this.OnStart();
                        }
                        else
                        {
                            this.OnStop();
                        }
                    }
                }
            }

            public LogCategory(VTDebugCategoryEnum category)
            {
                this.Category = category;
                this.Name = this.Category.ToString();
            }

            public void OnStart()
            {
                if (File.Exists(this.FilePath))
                {
                    try
                    {
                        File.Delete(this.FilePath);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("删除日志文件异常", ex);
                    }
                }
            }

            public void OnStop()
            { }
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger("VTDebug");

        private LogCategory interactiveCategory;
        private LogCategory rawReadCategory;
        private Dictionary<VTDebugCategoryEnum, LogCategory> categoryMap;
        public List<LogCategory> Categories { get; private set; }

        public VTDebug()
        {
            this.Categories = new List<LogCategory>();
            this.categoryMap = new Dictionary<VTDebugCategoryEnum, LogCategory>();

            this.interactiveCategory = this.CreateCategory(VTDebugCategoryEnum.Interactive);
            this.rawReadCategory = this.CreateCategory(VTDebugCategoryEnum.RawRead);
        }

        private LogCategory CreateCategory(VTDebugCategoryEnum categoryEnum)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("log_{0}.txt", categoryEnum.ToString()));

            LogCategory category = new LogCategory(categoryEnum)
            {
                FilePath = filePath
            };

            this.categoryMap[categoryEnum] = category;
            this.Categories.Add(category);

            return category;
        }

        private bool CanWrite(LogCategory logCategory)
        {
            if (!logCategory.Enabled)
            {
                return false;
            }

            return true;
        }

        public void WriteInteractive(VTActions action, string format, params object[] param)
        {
            if (!this.CanWrite(this.interactiveCategory))
            {
                return;
            }

            string message = string.Format(format, param);
            string log = string.Format("<- [{0},{1}]", action, message);
            File.AppendAllText(this.interactiveCategory.FilePath, log);
            File.AppendAllText(this.interactiveCategory.FilePath, "\r\n");
        }

        public void WriteInteractive(VTSendTypeEnum type, byte[] bytes)
        {
            if (!this.CanWrite(this.interactiveCategory))
            {
                return;
            }

            string message = type == VTSendTypeEnum.UserInput ?
                bytes.Select(v => ((char)v).ToString()).Join(",") :
                bytes.Select(v => ((int)v).ToString()).Join(",");

            string log = string.Format("-> [{0},{1}]", type, message);
            File.AppendAllText(this.interactiveCategory.FilePath, log);
            File.AppendAllText(this.interactiveCategory.FilePath, "\r\n");
        }

        public void WriteInteractive(VTSendTypeEnum type, StatusType statusType, byte[] bytes)
        {
            if (!this.CanWrite(this.interactiveCategory))
            {
                return;
            }

            string message = bytes.Select(v => ((int)v).ToString()).Join(",");
            string log = string.Format("-> [{0},{1},{2}]", type, statusType, message);
            File.AppendAllText(this.interactiveCategory.FilePath, log);
            File.AppendAllText(this.interactiveCategory.FilePath, "\r\n");
        }


        public void WriteRawRead(byte[] bytes, int size)
        {
            if (!this.CanWrite(this.rawReadCategory))
            {
                return;
            }

            string message = bytes.Take(size).Select(v => ((int)v).ToString()).Join(",");
            File.AppendAllText(this.rawReadCategory.FilePath, message + ",");
        }
    }
}