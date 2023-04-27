﻿using DotNEToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base;
using XTerminal.Channels;
using XTerminal.Session.Property;

namespace XTerminal.Session
{
    public class libvtsshSession : SessionBase
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("libvtsshChannel");

        #endregion

        #region 实例变量

        private IntPtr ssh;
        private IntPtr vtssh_options_ptr;
        private libvt.vtssh_status_changed_callback statusChangedDlg;
        private libvt.vtssh_data_received_callback dataReceivedDlg;

        #endregion

        #region 构造方法

        public libvtsshSession(VTInitialOptions options) :
            base(options)
        {
            this.statusChangedDlg = new libvt.vtssh_status_changed_callback(this.StatusChangedCallback);
            this.dataReceivedDlg = new libvt.vtssh_data_received_callback(this.DataReceivedCallback);

            SSHSessionProperties channelAuthorition = this.options.SessionProperties as SSHSessionProperties;
            TerminalProperties terminalProperties = this.options.TerminalProperties;

            libvt.vtssh_options vtssh_Options = new libvt.vtssh_options()
            {
                serverip = Encoding.ASCII.GetBytes(channelAuthorition.ServerAddress),
                serverport = channelAuthorition.ServerPort,
                username = Encoding.ASCII.GetBytes(channelAuthorition.UserName),
                password = Encoding.ASCII.GetBytes(channelAuthorition.Password),
                term = Encoding.ASCII.GetBytes(terminalProperties.GetTerminalName()),
                term_columns = terminalProperties.Columns,
                term_rows = terminalProperties.Rows,
                auth = libvt.vtssh_auth_enum.VTSSH_AUTH_PASSWORD,
                on_data_received = this.dataReceivedDlg,
                on_status_changed = this.statusChangedDlg
            };
            this.vtssh_options_ptr = MarshalUtils.CreateStructurePointer(vtssh_Options);
        }

        #endregion

        #region VTChannel

        protected override int OnInitialize()
        {
            int code = libvt.vtssh_create(out this.ssh, this.vtssh_options_ptr);
            if (code != libvt.VTSSH_ERR.VTSSH_ERR_OK)
            {
                logger.ErrorFormat("创建vtssh实例失败, code = {0}", code);
                return ResponseCode.FAILED;
            }

            return ResponseCode.SUCCESS;
        }

        protected override void OnRelease()
        {
            libvt.vtssh_delete(this.ssh);
            this.ssh = IntPtr.Zero;
        }

        public override int Connect()
        {
            int code = libvt.vtssh_connect(this.ssh);
            if (code != libvt.VTSSH_ERR.VTSSH_ERR_OK)
            {
                logger.ErrorFormat("vtssh_connect失败, code = {0}", code);
                return ResponseCode.FAILED;
            }

            return ResponseCode.SUCCESS;
        }

        public override void Disconnect()
        {
            libvt.vtssh_disconnect(this.ssh);
        }

        public override int Write(byte[] data)
        {
            IntPtr bytesPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            int code = libvt.vtssh_send(this.ssh, bytesPtr, data.Length);
            if (code != libvt.VTSSH_ERR.VTSSH_ERR_OK)
            {
                logger.ErrorFormat("vtssh_send失败, code = {0}", code);
                return ResponseCode.FAILED;
            }

            return ResponseCode.SUCCESS;
        }

        #endregion

        #region 实例方法

        private SessionStatusEnum GetVTChannelState(libvt.vtssh_status_enum status)
        {
            switch (status)
            {
                case libvt.vtssh_status_enum.VTSSH_STATUS_CONNECTED: return SessionStatusEnum.Connected;
                case libvt.vtssh_status_enum.VTSSH_STATUS_CONNECTING: return SessionStatusEnum.Connecting;
                case libvt.vtssh_status_enum.VTSSH_STATUS_DISCONNECTED: return SessionStatusEnum.Disconnected;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region 事件处理器

        private void StatusChangedCallback(IntPtr ssh, libvt.vtssh_status_enum status)
        {
            this.NotifyStatusChanged(this.GetVTChannelState(status));
        }

        private void DataReceivedCallback(IntPtr ssh, IntPtr data, int datasize)
        {
            byte[] bytes = new byte[datasize];
            Marshal.Copy(data, bytes, 0, bytes.Length);
            this.NotifyDataReceived(bytes);
        }

        #endregion
    }
}
