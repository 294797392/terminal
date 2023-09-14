﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base;
using XTerminal.Base.DataModels;
using XTerminal.Base.Enumerations;

namespace XTerminal.Session
{
    public class SerialPortSession : SessionDriver
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("SerialPortSession");

        #endregion

        #region 实例变量

        private SerialPort serialPort;

        #endregion

        #region 构造方法

        public SerialPortSession(XTermSession options) :
            base(options)
        {
        }

        #endregion

        #region SessionDriver

        public override int Open()
        {
            string portName = this.session.GetOption<string>(OptionKeyEnum.SERIAL_PORT_NAME);
            int baudRate = this.session.GetOption<int>(OptionKeyEnum.SERIAL_PORT_BAUD_RATE);

            try
            {
                this.serialPort = new SerialPort(portName);
                this.serialPort.BaudRate = baudRate;
                this.serialPort.Open();
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("串口打开失败, {0}, {1}", portName, ex);
                return ResponseCode.FAILED;
            }

            this.NotifyStatusChanged(SessionStatusEnum.Connected);

            return ResponseCode.SUCCESS;
        }

        public override void Close()
        {
            this.serialPort.Close();
            this.serialPort.Dispose();
        }

        public override int Write(byte[] bytes)
        {
            try
            {
                this.serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                this.NotifyStatusChanged(SessionStatusEnum.Disconnected);
                logger.Error("写入串口异常", ex);
                return ResponseCode.FAILED;
            }

            return ResponseCode.SUCCESS;
        }

        internal override int Read(byte[] buffer)
        {
            return this.serialPort.Read(buffer, 0, buffer.Length);
        }

        public override void Resize(int row, int col)
        {
        }

        #endregion

        #region 事件处理器

        #endregion
    }
}