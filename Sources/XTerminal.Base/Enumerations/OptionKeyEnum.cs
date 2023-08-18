﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Base.Enumerations
{
    public enum OptionKeyEnum
    {
        #region SSH 0 - 500

        SSH_TERM_ROW = 0,
        SSH_TERM_COL = 1,
        SSH_TERM_TYPE = 2,
        SSH_TERM_SIZE_MODE = 3,

        SSH_THEME_FONT_FAMILY = 6,
        SSH_THEME_FONT_SIZE = 7,
        SSH_THEME_FONT_COLOR = 8,
        /// <summary>
        /// 光标闪烁速度
        /// </summary>
        SSH_THEME_CURSOR_SPEED,
        /// <summary>
        /// 光标样式
        /// </summary>
        SSH_THEME_CURSOR_STYLE,

        SSH_SERVER_ADDR,
        SSH_SERVER_PORT,
        SSH_SERVER_USER_NAME,
        SSH_SERVER_PASSWORD,
        SSH_SERVER_PRIVATE_KEY_FILE,
        SSH_SERVER_Passphrase,
        SSH_AUTH_TYPE,

        #endregion

        #region 串口 501 - 1000

        SERIAL_PORT_NAME,
        SERIAL_PORT_BAUD_RATE,

        #endregion

        /// <summary>
        /// 输出编码格式
        /// </summary>
        WRITE_ENCODING,
        WRITE_BUFFER_SIZE,
        READ_BUFFER_SIZE,

        MOUSE_SCROLL_DELTA,

        #region SFTP 10000 - 11000

        SFTP_SERVER_ADDRESS = 10000,
        SFTP_SERVER_PORT = 10001,
        SFTP_USER_NAME = 10002,
        SFTP_USER_PASSWORD = 10003,
        SFTP_AUTH_TYPE = 10004,
        SFTP_SERVER_INITIAL_DIRECTORY = 10005,
        SFTP_CLIENT_INITIAL_DIRECTORY = 10006,

        #endregion
    }
}
