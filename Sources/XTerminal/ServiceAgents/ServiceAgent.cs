﻿using DotNEToolkit.Modular;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base.DataModels;
using XTerminal.Session.Definitions;

namespace XTerminal.ServiceAgents
{
    /// <summary>
    /// 远程服务器代理
    /// 远程服务器的作用：
    /// 1. 管理分组和Session信息
    /// 2. 用户数据管理，用户登录
    /// </summary>
    public abstract class ServiceAgent : ModuleBase
    {
        public List<SessionDefinition> GetSessionDefinitions()
        {
            return XTermApp.Context.Manifest.SessionList;
        }

        /// <summary>
        /// 获取所有的会话列表
        /// </summary>
        /// <returns></returns>
        public abstract List<SessionDM> GetSessions();

        /// <summary>
        /// 增加一个会话
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract int AddSession(SessionDM session);

        /// <summary>
        /// 删除Session
        /// </summary>
        /// <param name="sessionID">要删除的SessionID</param>
        /// <returns></returns>
        public abstract int DeleteSession(string sessionID);

        /// <summary>
        /// 更新一个会话的信息
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract int UpdateSession(SessionDM session);
    }
}