﻿using ModengTerm.Base.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XTerminal.Base;
using XTerminal.Base.DataModels;
using XTerminal.Base.Enumerations;

namespace XTerminal.Session
{
    public class SessionTransport
    {
        #region 类变量

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger("SessionTransport");

        #endregion

        #region 公开事件

        /// <summary>
        /// 当Session状态发生改变的时候触发
        /// </summary>
        public event Action<object, SessionStatusEnum> StatusChanged;

        /// <summary>
        /// 当收到数据流的时候触发
        /// </summary>
        public event Action<SessionTransport, byte[], int> DataReceived;

        #endregion

        #region 实例变量

        private SessionDriver driver;

        private Task backgroundTask;
        private bool isRunning;

        private byte[] readBuffer;

        private int row;
        private int col;

        #endregion

        #region 属性

        /// <summary>
        /// 获取当前Session的连接状态
        /// </summary>
        public SessionStatusEnum Status { get; private set; }

        #endregion

        #region 公开接口

        public int Initialize(XTermSession session)
        {
            int bufferSize = session.GetOption<int>(OptionKeyEnum.READ_BUFFER_SIZE);
            this.row = session.GetOption<int>(OptionKeyEnum.SSH_TERM_ROW);
            this.col = session.GetOption<int>(OptionKeyEnum.SSH_TERM_COL);

            this.readBuffer = new byte[bufferSize];
            this.driver = SessionFactory.Create(session);
            if (this.driver is SshNetSession)
            {
                SshNetSession sshNetSession = this.driver as SshNetSession;
                sshNetSession.DataReceived = this.Stream_DataReceived;
                sshNetSession.ErrorOccurred = this.Stream_ErrorOccurred;
            }

            return ResponseCode.SUCCESS;
        }

        public void Release()
        {
            if (this.driver == null)
            {
                return;
            }

            if (this.driver is SshNetSession)
            {
                SshNetSession sshNetSession = this.driver as SshNetSession;
                sshNetSession.DataReceived = null;
                sshNetSession.ErrorOccurred = null;
            }

            this.driver = null;
        }

        /// <summary>
        /// 异步打开与远程主机的连接
        /// </summary>
        /// <returns></returns>
        public int OpenAsync()
        {
            this.isRunning = true;
            this.backgroundTask = Task.Factory.StartNew(this.BackgroundTaskProc);
            return ResponseCode.SUCCESS;
        }

        public void Close()
        {
            this.isRunning = false;

            if (!this.CheckStatus())
            {
                return;
            }

            this.backgroundTask.Wait();
            this.CloseDriver();
        }

        public int Write(byte[] bytes)
        {
            if (!this.CheckStatus())
            {
                return ResponseCode.SUCCESS;
            }

            try
            {
                this.driver.Write(bytes);
                return ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                this.Close();
                this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);
                logger.Error("发送数据异常", ex);
                return ResponseCode.FAILED;
            }
        }

        /// <summary>
        /// 重置终端大小
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public int Resize(int row, int col)
        {
            if (!this.CheckStatus())
            {
                return ResponseCode.FAILED;
            }

            if (this.row == row && this.col == col)
            {
                return ResponseCode.SUCCESS;
            }

            try
            {
                this.driver.Resize(row, col);
            }
            catch (Exception ex)
            {
                this.Close();
                this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);
                logger.Error("Resize异常", ex);
                return ResponseCode.FAILED;
            }

            this.row = row;
            this.col = col;

            return ResponseCode.SUCCESS;
        }

        #endregion

        #region 实例方法

        private void CloseDriver()
        {
            this.driver.Close();
            this.isRunning = false;
        }

        /// <summary>
        /// 检查当前连接状态
        /// </summary>
        /// <returns>
        /// true：已连接，可以执行其他操作
        /// false：未连接，不能执行其他操作
        /// </returns>
        private bool CheckStatus()
        {
            if (this.driver == null)
            {
                return false;
            }

            if (this.Status != SessionStatusEnum.Connected)
            {
                return false;
            }

            return true;
        }

        private void NotifyDataReceived(byte[] bytes, int size)
        {
            if (this.DataReceived != null)
            {
                this.DataReceived(this, bytes, size);
            }
        }

        private void NotifyStatusChanged(SessionStatusEnum status)
        {
            if (this.Status == status)
            {
                return;
            }

            this.Status = status;
            if (this.StatusChanged != null)
            {
                this.StatusChanged(this, status);
            }
        }

        private void SessionLoop()
        {
            // 没读取道数据的次数
            // 如果超过3次，那么就判断为已经断开连接
            int zeros = 0;
            int n = 0;

            while (this.isRunning)
            {
                try
                {
                    n = this.driver.Read(this.readBuffer);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error("读取数据异常", ex);
                    break;
                }
                catch (TimeoutException ex)
                {
                    logger.Error("读取数据异常", ex);
                    break;
                }
                catch (Exception ex)
                {
                    logger.Error("读取数据异常", ex);
                    break;
                }

                if (n == -1)
                {
                    // 读取失败，直接断线处理
                    break;
                }
                else if (n == 0)
                {
                    // 0的话继续读取
                    zeros++;
                    if (zeros > 3)
                    {
                        // 大于3次直接退出
                        break;
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    // 重置zero计数器
                    if (zeros != 0)
                    {
                        zeros = 0;
                    }

                    // 读取到了数据
                    this.NotifyDataReceived(this.readBuffer, n);
                }
            }
        }

        #endregion

        #region 事件处理器

        private void BackgroundTaskProc()
        {
            logger.InfoFormat("Session线程启动成功");

            int code = ResponseCode.SUCCESS;

            // 先连接远程主机
            this.NotifyStatusChanged(SessionStatusEnum.Connecting);

            try
            {
                if ((code = this.driver.Open()) != ResponseCode.SUCCESS)
                {
                    this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("连接失败", ex);
                this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);
                return;
            }

            this.NotifyStatusChanged(SessionStatusEnum.Connected);

            // SshNet使用回调获取数据，所以这里直接返回
            if (this.driver is SshNetSession)
            {
                SshNetSession sshNetSession = this.driver as SshNetSession;
                sshNetSession.DataReceived = Stream_DataReceived;
                sshNetSession.ErrorOccurred = Stream_ErrorOccurred;
                logger.InfoFormat("退出Session线程");
                return;
            }

            // 连接成功之后开始从远程主机读取数据
            // 读取失败会返回
            this.SessionLoop();

            // 此时连接失败
            this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);

            this.driver.Close();
            this.isRunning = false;

            logger.InfoFormat("退出Session线程");
        }

        private void Stream_ErrorOccurred(object sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {
            logger.Error("SshNet Stream_ErrorOccurred", e.Exception);
            this.CloseDriver();
            this.NotifyStatusChanged(SessionStatusEnum.ConnectionError);
        }

        private void Stream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            this.NotifyDataReceived(e.Data, e.Data.Length);
        }

        #endregion
    }
}
