using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTerminal.Document
{
    /// <summary>
    /// ��ʾ�ı�Ԫ�����һ��λ��
    /// </summary>
    public class VTextPointer
    {
        /// <summary>
        /// ָ�����е�����
        /// </summary>
        public int Row { get { return this.Line.Row; } }

        /// <summary>
        /// ���е��ַ��Ĳ�����Ϣ
        /// </summary>
        public VTRect CharacterBounds { get; set; }

        /// <summary>
        /// �Ƿ��������ַ�
        /// ���û�����ַ�����ô����굱ǰλ��Ϊ��������һ���հ��ַ���CharacterBounds
        /// </summary>
        public bool IsCharacterHit { get; set; }

        /// <summary>
        /// ���е��ַ�������
        /// </summary>
        public int CharacterIndex { get; set; }

        /// <summary>
        /// ��������е���
        /// </summary>
        public VTHistoryLine Line { get; set; }
    }
}