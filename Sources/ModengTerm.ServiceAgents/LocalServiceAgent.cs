﻿using DotNEToolkit;
using ModengTerm.Base;
using ModengTerm.Base.DataModels;
using ModengTerm.Terminal.DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base;
using XTerminal.Base.DataModels;

namespace ModengTerm.ServiceAgents
{
    /// <summary>
    /// 离线服务Agent
    /// </summary>
    public class LocalServiceAgent : ServiceAgent
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger("LocalServiceAgent");

        public override MTermManifest GetManifest()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.json");

            try
            {
                return JSONHelper.ParseFile<MTermManifest>(path);
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("加载AppManifest异常, {0}, {1}", path, ex);
                return default(MTermManifest);
            }
        }

        #region Session管理

        public override int AddSession(XTermSession session)
        {
            try
            {
                JSONDatabase.Insert<XTermSession>(session);

                return ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                logger.Error("AddSession异常", ex);
                return ResponseCode.FAILED;
            }
        }

        public override int DeleteSession(string sessionID)
        {
            try
            {
                JSONDatabase.Delete<XTermSession>(v => v.ID == sessionID);

                return ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                logger.Error("DeleteSession异常", ex);
                return ResponseCode.FAILED;
            }
        }

        public override List<XTermSession> GetSessions()
        {
            try
            {
                return JSONDatabase.SelectAll<XTermSession>();
            }
            catch (Exception ex)
            {
                logger.Error("GetSessions异常", ex);
                return new List<XTermSession>();
            }
        }

        public override int UpdateSession(XTermSession session)
        {
            try
            {
                return JSONDatabase.Update<XTermSession>(v => v.ID == session.ID, session);
            }
            catch (Exception ex)
            {
                logger.Error("UpdateSession异常", ex);
                return ResponseCode.FAILED;
            }
        }

        #endregion

        public override List<Favorites> GetFavorites(string sessionId)
        {
            try
            {
                return JSONDatabase.SelectAll<Favorites>(this.GetFilePath<Favorites>(sessionId));
            }
            catch (Exception ex)
            {
                logger.Error("GetFavorites异常", ex);
                return new List<Favorites>();
            }
        }

        public override int AddFavorites(Favorites favorites)
        {
            try
            {
                JSONDatabase.Insert<Favorites>(this.GetFilePath<Favorites>(favorites.SessionID), favorites);

                return ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                logger.Error("AddFavorites异常", ex);
                return ResponseCode.FAILED;
            }

        }

        public override int DeleteFavorites(Favorites favorites)
        {
            try
            {
                JSONDatabase.Delete<Favorites>(this.GetFilePath<Favorites>(favorites.SessionID), v => v.ID == favorites.ID);

                return ResponseCode.SUCCESS;
            }
            catch (Exception ex)
            {
                logger.Error("DeleteFavorites异常", ex);
                return ResponseCode.FAILED;
            }
        }

        #region 实例方法

        private string GetFilePath<T>(string sessionId)
        {
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataDirectory, string.Format("{0}_{1}", sessionId, typeof(T).ToString()));
        }

        #endregion
    }
}
