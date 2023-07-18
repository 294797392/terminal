
# terminal

一个多功能的终端模拟器，可以连接SSH服务器，串口...etc  
类似于XShell，putty和微软开源的terminal，代码参考了xterm和terminal的源码  
整体的设计文档请参考__Documents/设计文档.pptx__  

## 支持连接的客户端类型
1. SSH服务器
2. 串口 - 正在实现

## 开发环境
使用VS2019，WPF开发

## 项目结构

## 实现原理
参考：  
* https://github.com/microsoft/terminal.git
* https://devblogs.microsoft.com/commandline/windows-command-line-introducing-the-windows-pseudo-console-conpty/
* Control Functions for Coded Character Sets Ecma-048.pdf
* 终端字符解析：
https://learn.microsoft.com/zh-cn/windows/console/console-virtual-terminal-sequences
https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
https://invisible-island.net/xterm/
https://vt100.net/emu/dec_ansi_parser
* 虚拟终端/控制台/Shell介绍：https://cloud.tencent.com/developer/news/304629


# 虚拟终端支持启用或禁用以下模式
| 模式 | 描述 | 启用/禁用方法 |
| :--- | ---------------|---|
| DECAWM | 是否自动换行 |tput smam/tput rmam|


# 渲染方式对比
| 功能 | FlowDocument |
| :--- | ---------------|
| 显示光标        | &#10004; |
| 修改光标样式    | &#10008; |
| 文本选中    | &#10004; |
| 自定义右键菜单    | &#10004; |

# 不同程序终端的输出对比
| 程序 | 动作 | 输出 |
| :--- | ----|------|
| vim  | 打开|      |
| vim  | PageUp/PageDown ||
| vim  | 输入3个中文字符 |Print:你, cursorRow = 0, cursorCol = 4<br/>Print:你, cursorRow = 0, cursorCol = 6<br/>Print:你, cursorRow = 0, cursorCol = 8|
| vim  | 按二次Backspace删除中文字符 |CUP_CursorPosition, row = 0, col = 8<br/>EL_EraseLine, eraseType = ToEnd, cursorRow = 0, cursorCol = 8<br/>CUP_CursorPosition, row = 0, col = 6,EL_EraseLine, eraseType = ToEnd, cursorRow = 0, cursorCol = 6|
| shell  | 输入3个中文字符 |Print:你, cursorRow = 23, cursorCol = 31<br/>Print:你, cursorRow = 23, cursorCol = 32<br/>Print:你, cursorRow = 23, cursorCol = 33|
| shell  | 按一次Backspace删除中文字符 |CursorBackward, cursorRow = 23, cursorCol = 33<br/>CursorBackward, cursorRow = 23, cursorCol = 32<br/>EL_EraseLine, eraseType = ToEnd, cursorRow = 23, cursorCol = 32|
| man | 打开| |
| man | PageUp/PageDown ||

























