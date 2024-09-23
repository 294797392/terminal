﻿using ModengTerm.Base.DataModels;
using ModengTerm.Base.Enumerations;
using ModengTerm.Controls;
using ModengTerm.Terminal.UserControls;
using System;

namespace ModengTerm
{
    public static class SessionContentFactory
    {
        public static ISessionContent Create(XTermSession session)
        {
            switch ((SessionTypeEnum)session.Type)
            {
                case SessionTypeEnum.SerialPort:
                case SessionTypeEnum.CommandLine:
                case SessionTypeEnum.SSH:
                    {
                        return new TerminalContentUserControl();
                    }

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
