﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Document.Rendering
{
    /// <summary>
    /// 表示一个用来显示文档的画板
    /// </summary>
    public interface IDocumentCanvas
    {
        /// <summary>
        /// 初始化渲染器
        /// </summary>
        /// <param name="options"></param>
        void Initialize(DocumentCanvasOptions options);

        /// <summary>
        /// 请求创建一个新的渲染对象
        /// </summary>
        /// <param name="type">渲染对象的类型</param>
        /// <param name="num">请求的数量</param>
        /// <returns></returns>
        List<IDocumentDrawable> RequestDrawable(Drawables type, int num);

        /// <summary>
        /// 测量某个渲染模型的大小
        /// TODO：如果测量的是字体，要考虑到对字体应用样式后的测量信息
        /// </summary>
        /// <param name="textLine">要测量的数据模型</param>
        /// <param name="maxCharacters">要测量的最大字符数，0为全部测量</param>
        /// <returns></returns>
        VTElementMetrics MeasureLine(VTextLineBase textLine, int maxCharacters);

        /// <summary>
        /// 测量一行里某个字符的测量信息
        /// 注意该接口只能测量出来X偏移量，Y偏移量需要外部根据高度自己计算
        /// </summary>
        /// <param name="textLine">要测量的文本行</param>
        /// <param name="characterIndex">要测量的字符</param>
        /// <returns>文本坐标，X=文本左边的X偏移量，Y=文本高度</returns>
        VTRect MeasureCharacter(VTextLineBase textLine, int characterIndex);

        /// <summary>
        /// 画
        /// 如果是文本元素，将对文本进行重新排版并渲染
        /// 排版是比较耗时的操作
        /// </summary>
        /// <param name="drawable"></param>
        void DrawDrawable(IDocumentDrawable drawable);

        /// <summary>
        /// 更新元素的位置信息
        /// 而不用重新画，速度要比DrawDrawable快
        /// 画文本的速度还是比较慢的，因为需要对文本进行排版，耗时都花在排版上面了
        /// 所以能不排版就最好不排版
        /// </summary>
        /// <param name="drawable"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        void UpdatePosition(IDocumentDrawable drawable, double offsetX, double offsetY);

        /// <summary>
        /// 设置元素的透明度
        /// </summary>
        /// <param name="drawable"></param>
        /// <param name="opacity"></param>
        void SetOpacity(IDocumentDrawable drawable, double opacity);
    }
}
