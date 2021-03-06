# NewCode
一个用命令行工具，使用模板创建文件：比如代码文件，含有固定格式的文件等等，支持格式化参数。

注意涉及管理员高权限操作请注意把控，调试编写的时候请注意备份。本人编码的部分都经过测试，保证正常操作的合法性可以得到保证，因此导致的问题本人概不负责。

# 开发目的

是不是写代码或者写博客的时候重复使用某代码块，疲于复制粘贴想要更快捷的方式？那就试一试该软件吧，它可以急速方便你的编写，只需要写好文件并做好配置，就可以完美使用。

# 配置注意事项

所有的文档请用`UTF-8`进行保存，否则容易出现乱码的状况，如果你的代码基于此修改请根据自身情况。

# 使用方法

命令不区分大小写，如下为示例：

```c
NewCode -t cpp -p C:\test -k
NewCode -st c
NewCode -t cpp -p C:\test -f "hello" " world"
NewCode -q
```

参数含义：
```c
-t | -type：类型，配置 json 代码块时的 id
-p | -path：路径，创建文件的路径，如果省略后缀，则以 json 配置为准添加
-k | -keepalive | -keep | -alive：执行该程序后不会退出，保持运行状态，可以执行其他的命令
-f | -fill | -param：填充可变参数，这对于模板十分有用，如何编写合适请见示例
-st | -settype：程序运行后，如果没特地设置，默认调用配置文件的第一个
-q：退出程序，仅在 keepalive 环境中有效
-add [type] [path] {ext}：添加以 type 的内容为 id ，然后用 path 作为路径，注意路径建议为相对路径，如果有 ext 参数则以它为扩展名。
-mod [type] p={path} ext={ext}：修改 type 中的配置
-del [type]：删除以 type == id 的内容
-cls：清理所有的配置
-showAll：显示所有配置键值
-showInfo [type]：显示该类型的所有信息
-showtype [type]：作用同 -showInfo
-r | -restart：重启程序，如果后面带有 # 作为参数说明以管理员权限重启，重启默认带有 -k 参数。
-curdir | -pwd：显示当前工作目录
-prodir：显示该程序所在目录
```

说明：

如果在 keepalive 状态，就不需输入 NewCode 。如果上面的参数没有，该程序会调用 cmd 命令运行。

如果被视为 cmd 命令运行，开头不得带有 - 字符，否则视为内部命令。