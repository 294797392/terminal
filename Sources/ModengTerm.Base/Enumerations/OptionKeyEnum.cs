﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Base.Enumerations
{
    public enum OptionKeyEnum
    {
        #region SSH 0 - 500

        // Terminal 0 - 99
        SSH_TERM_ROW = 0,
        SSH_TERM_COL = 1,
        SSH_TERM_TYPE = 2,
        SSH_TERM_SIZE_MODE = 3,

        // Theme 100 - 200
        SSH_THEME_FONT_FAMILY = 100,
        SSH_THEME_FONT_SIZE = 101,
        SSH_THEME_FORE_COLOR = 102,
        /// <summary>
        /// 光标闪烁速度
        /// </summary>
        SSH_THEME_CURSOR_SPEED = 103,
        /// <summary>
        /// 光标样式
        /// </summary>
        SSH_THEME_CURSOR_STYLE = 104,
        SSH_THEME_CURSOR_COLOR = 106,
        /// <summary>
        /// 选择的主题ID
        /// </summary>
        SSH_THEME_ID = 107,
        SSH_TEHEM_COLOR_TABLE = 108,

        SSH_THEME_BACKGROUND_TYPE = 110,
        SSH_THEME_BACKGROUND_URI = 111,
        SSH_THEME_BACK_COLOR = 112,
        SSH_THEME_BACKGROUND_EFFECT = 113,

        /// <summary>
        /// 文档边距
        /// </summary>
        SSH_THEME_CONTENT_MARGIN = 114,

        /// <summary>
        /// 是否显示书签
        /// </summary>
        SSH_BOOKMARK_VISIBLE = 115,
        SSH_BOOKMARK_COLOR = 116,

        // Server 201 - 300
        SSH_SERVER_ADDR = 201,
        SSH_SERVER_PORT,
        SSH_SERVER_USER_NAME,
        SSH_SERVER_PASSWORD,
        SSH_SERVER_PRIVATE_KEY_FILE,
        SSH_SERVER_Passphrase,
        SSH_SERVER_AUTH_TYPE,

        #endregion

        #region 串口 501 - 1000

        SERIAL_PORT_NAME = 501,
        SERIAL_PORT_BAUD_RATE = 502,
        SERIAL_PORT_DATA_BITS = 503,
        SERIAL_PORT_STOP_BITS = 504,
        SERIAL_PORT_PARITY = 505,
        SERIAL_PORT_HANDSHAKE = 506,

        #endregion

        #region 终端 1001 - 1500

        /// <summary>
        /// 最多可以回滚的行数
        /// </summary>
        TERM_MAX_SCROLLBACK = 1001,
        /// <summary>
        /// 最多可以保存的剪贴板历史记录数据
        /// </summary>
        TERM_MAX_CLIPBOARD_HISTORY = 1002,

        TERM_SELECTION_COLOR = 1003,

        MOUSE_SCROLL_DELTA = 1004,

        #endregion

        #region 通用 1501 - 5000

        /// <summary>
        /// 输出编码格式
        /// </summary>
        WRITE_ENCODING = 1501,
        WRITE_BUFFER_SIZE = 1502,
        READ_BUFFER_SIZE = 1503,

        #endregion

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
