﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModengTerm.Base
{
    public class ResponseCode
    {
        public const int FAILED = -1;
        public const int SUCCESS = 0;
        public const int TIMEOUT = 1;

        public static string GetMessage(int code)
        {
            switch (code)
            {
                case ResponseCode.FAILED: return "失败";
                case ResponseCode.SUCCESS: return "成功";
                case ResponseCode.TIMEOUT: return "超时";

                default:
                    return "未知错误";
            }
        }
    }
}