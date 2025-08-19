VRCFT for multi-devices(ver 1.0)

新增实现的功能基于：https://github.com/benaclejames/VRCFaceTracking 这是原项目 伟大无需多言

新增的功能包括：

增加了一个新的配置方案功能，利好双眼追设备的vrc玩家，比如vive focus vision 和 bigscreen beyond 两台设备 对应两套组件的支持

这样的更新避免了原先 vive streaming 组件会被率先加载出来，其他的组件就会被其占用的尴尬情况，也同时避免了htc 小方块的VIVE SRanipal 软件，当你使用其他的组件，他也要让你加载(并且使用管理员权限 你还要点一下)

双持玩家不必每次进入CustomLibs去修改文件夹的名称，每次vrcft遇到bug也不必关闭重启 只需要切换配置 就可以重新加载所有组件

与此同时我们允许用户新建配置，能够灵活高效的管理组建的加载

然后一下是todo-list，以后可能会考虑增加

增加对于varjo aero 或者 vpe 等等设备的 眼追画面 预览

增加 多语言支持 繁体中文 以及 简体中文

改善原软件 点击uninstall删不掉组件的逻辑错误

以及更好的组件管理逻辑 等等 

有问题 请直接联系en2209260768@gmail.com
