﻿using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;

namespace XTerminal.Channels
{
    public class SSHChannel : VTChannel
    {
        #region 类变量

        private static log4net.ILog logger = log4net.LogManager.GetLogger("SSHSocket");

        #endregion

        #region 实例变量

        private SshClient sshClient;
        private ShellStream stream;
        private SSHChannelAuthorition authorition;

        #endregion

        #region 属性

        public override VTChannelTypes Type { get { return VTChannelTypes.SSH; } }

        #endregion

        #region 构造方法

        public SSHChannel(SSHChannelAuthorition authorition) : 
            base(authorition)
        {
        }

        #endregion

        #region 实例方法

        #endregion

        #region SocketBase

        public override bool Connect()
        {
            this.NotifyStatusChanged(VTChannelState.Connecting);

            this.authorition = this.Authorition as SSHChannelAuthorition;
            var authentications = new List<AuthenticationMethod>();
            if (!string.IsNullOrEmpty(this.authorition.KeyFilePath))
            {
                var privateKeyFile = new PrivateKeyFile(this.authorition.KeyFilePath, this.authorition.KeyFilePassphrase);
                authentications.Add(new PrivateKeyAuthenticationMethod(this.authorition.UserName, privateKeyFile));
            }
            authentications.Add(new PasswordAuthenticationMethod(this.authorition.UserName, this.authorition.Password));
            ConnectionInfo connectionInfo = new ConnectionInfo(this.authorition.ServerAddress, this.authorition.ServerPort, this.authorition.UserName, authentications.ToArray());

            this.sshClient = new SshClient(connectionInfo);
            this.sshClient.Connect();
            this.sshClient.KeepAliveInterval = TimeSpan.FromSeconds(20);

            this.stream = this.sshClient.CreateShellStream("xterm", 80, 24, 9999, 9999, 4096);
            this.stream.DataReceived += this.Stream_DataReceived;

            this.NotifyStatusChanged(VTChannelState.Connected);

            return true;
        }

        public override bool Disconnect()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] bytes)
        {
            long left = bytes.Length;
            int readed = 0;

            while (left > 0)
            {
                int readLen = left > DefaultReadBufferSize ? DefaultReadBufferSize : (int)left;
                int size = this.stream.Read(bytes, readed, readLen);
                if (size == 0)
                {
                    return readed;
                }
                readed += size;
                left -= readed;
            }

            return readed;
        }

        public override byte Read()
        {
            throw new NotImplementedException();
        }

        public override bool Write(byte data)
        {
            this.stream.WriteByte(data);
            this.stream.Flush();
            return true;
        }

        public override bool Write(byte[] data)
        {
            this.stream.Write(data, 0, data.Length);
            this.stream.Flush();
            return true;
        }

        #endregion

        #region 实例方法

        private void Stream_DataReceived(object sender, ShellDataEventArgs e)
        {
            base.NotifyDataReceived(e.Data);
        }

        #endregion
    }
}