﻿using DotNEToolkit;
using ModengTerm.Base.DataModels;
using ModengTerm.Base.Definitions;
using ModengTerm.Terminal.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XTerminal.Base.Definitions;

namespace ModengTerm.ServiceAgents
{
    public class MTermManifest : AppManifest
    {
        /// <summary>
        /// 支持的会话列表
        /// </summary>
        [JsonProperty("sessions")]
        public List<SessionDefinition> SessionList { get; private set; }

        /// <summary>
        /// XTerminal支持的文字大小列表
        /// </summary>
        [JsonProperty("fontSize")]
        public List<FontSizeDefinition> FontSizeList { get; private set; }

        /// <summary>
        /// 支持的文字样式列表
        /// </summary>
        [JsonProperty("fontFamily")]
        public List<FontFamilyDefinition> FontFamilyList { get; private set; }

        /// <summary>
        /// 和主题相关的数据清单
        /// </summary>
        [JsonProperty("themeManifest")]
        public ThemeManifest ThemeManifest { get; private set; }

        public MTermManifest()
        {
            this.SessionList = new List<SessionDefinition>();
            this.FontSizeList = new List<FontSizeDefinition>();
            this.FontFamilyList = new List<FontFamilyDefinition>();
        }
    }
}
