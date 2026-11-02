# Nuxe

FromSoftware 游戏的游戏数据解包器与松散加载修补器；[UXM](https://github.com/JKAnderson/UXM) 的精神续作。完整使用说明与更新日志请见每次发行附带的 [Readme](./dist/Readme.txt)。

> 本分支提供简体中文本地化（默认中文，可随时切换英文），由 [Hoshino486](https://github.com/Hoshino486/Nuxe) 维护。

界面默认简体中文，可在主界面右上角“语言”下拉框中随时切换到 English，选择结果会被记住（写入程序目录下的 `Nuxe.language`，设置存储可用时也会写入用户配置）；也可用环境变量 `NUXE_LANG=zh` 或 `NUXE_LANG=en` 强制指定启动语言。

Nuxe 支持所有使用大型 BinderLight 归档的 FromSoftware 游戏：

- 装甲核心 V（PS3、X360）
- 装甲核心 审判日（PS3、X360）
- 装甲核心 VI 境界天火（PC）
- 黑暗之魂（PC、PS3、X360）
- 黑暗之魂 重制版（NS）
- 黑暗之魂 II（PC）
- 黑暗之魂 II 原罪学者（PC）
- 黑暗之魂 III（PC）
- 艾尔登法环（PC）
- 艾尔登法环 黑夜君临（PC）
- 只狼：影逝二度 + 数字艺术集与迷你原声带（PC）
- 铁骑 重装甲（X360）

所有游戏均可解包；可执行文件修补目前仅支持 PC 游戏。

如果你觉得这个工具好用，可以在 [Ko-fi](https://ko-fi.com/tkgp) 或 [Patreon](https://www.patreon.com/c/TKGP) 上支持作者。

## 依赖库

[**BinderKeys**](https://github.com/JKAnderson/BinderKeys)（[许可证](https://github.com/JKAnderson/BinderKeys?tab=Apache-2.0-1-ov-file)）  
[**Coremats**](https://github.com/JKAnderson/Coremats)（[许可证](https://github.com/JKAnderson/Coremats?tab=GPL-3.0-1-ov-file)）
