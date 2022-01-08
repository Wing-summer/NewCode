/*本工具的用法，命令不区分大小写：
 * 
 * NewCode -t cpp -p C:\test -k
 * NewCode -st c
 * NewCode -t cpp -p C:\test -f "hello" " world"
 * NewCode -q
 * 
 * 参数含义：
 * -t | --type：类型，配置 json 代码块时的 id
 * -p | --path：路径，创建文件的路径，如果省略后缀，则以 json 配置为准添加
 * -k | --keepalive | --keep | --alive：执行该程序后不会退出，保持运行状态，可以执行其他的命令
 * -f | --fill | --param：填充可变参数，这对于模板十分有用，如何编写合适请见示例
 * -st | --settype：程序运行后，如果没特地设置，默认调用配置文件的第一个
 * -q：退出程序，仅在 keepalive 环境中有效
 * -add [type] [path] {ext}：添加以 type 的内容为 id ，然后用 path 作为路径，注意路径建议为相对路径，如果有 ext 参数则以它为扩展名。
 * -del [type]：删除以 type == id 的内容
 * -cls：清理所有的配置
 * 
 * 说明：
 * 如果在 keepalive 状态，如果就不需输入 NewCode 。如果上面的参数没有，该程序按照 cmdline 命令运行
 * 
 */


using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("NewCode，为简化写代码而生，By. Wing Summer！！！");
Console.ForegroundColor = ConsoleColor.White;
var currentdir = Environment.CurrentDirectory;
var processdir = AppDomain.CurrentDomain.BaseDirectory;
var ConfigPath = Path.Combine(processdir, "NewCode.json");

Dictionary<string, CodeObject>? codeObj;

// param 负责获取配置参数，arguments 负责获取当含有格式化时的参数
CmdLineParam param = new();
CodeNewEnvironment CodeNewEnvironment = new();
List<string> arguments = new();
char[] trimchar = { '\\', '\"', '\'', '/' };

if (File.Exists(ConfigPath))
{
    codeObj = JsonConvert.DeserializeObject(File.ReadAllText(ConfigPath, System.Text.Encoding.UTF8),
        typeof(Dictionary<string, CodeObject>)) as Dictionary<string, CodeObject>;
    if (codeObj == null)
    {
        codeObj = new Dictionary<string, CodeObject>();
    }
    CodeNewEnvironment.CurrentType=codeObj.Keys.First();
}
else
{
    File.WriteAllText(ConfigPath, string.Empty, System.Text.Encoding.UTF8);
    codeObj = new Dictionary<string, CodeObject>();
}

if (args.Length < 1)
{
    ShowHelp();
    Pause();
}
else
{
    var cmdlines = args;

    do
    {
        //解析命令行
        try
        {
            param.Operation = OperationType.None;
            for (int i = 0; i < cmdlines.Length; i++)
            {
                var cmd = TrimInvaildChars(cmdlines[i], trimchar);

                if (isTheSame(cmd, "-t") || isTheSame(cmd, "--type"))
                {
                    CodeNewEnvironment.CurrentType = param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.NewCode;
                }
                else if (isTheSame(cmd, "-p") || isTheSame(cmd, "--path"))
                {
                    param.FilePath = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.NewCode;
                }
                else if (isTheSame(cmd, "-f") || isTheSame(cmd, "--fill"))
                {
                    while (i < cmdlines.Length - 1)
                    {
                        var item = cmdlines[++i];
                        if (!item.StartsWith('-'))
                        {
                            arguments.Add(item);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else if (isTheSame(cmd, "-k") || isTheSame(cmd, "--alive") || isTheSame(cmd, "--keep") || isTheSame(cmd, "--keepalive"))
                {
                    if (param.IsKeepingAlive)
                    {
                        ShowWarning("当前状态已是 keepalive 模式。");
                    }
                    param.IsKeepingAlive = true;
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-st") || isTheSame(cmd, "--settype"))
                {
                    CodeNewEnvironment.CurrentType = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.SetType;
                }
                else if (isTheSame(cmd, "-q") || isTheSame(cmd, "--quit"))
                {
                    return;
                }
                else if (isTheSame(cmd, "-curType"))
                {
                    param.Operation |= OperationType.CurType;
                }
                else if (isTheSame(cmd, "-add"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.FilePath = TrimInvaildChars(cmdlines[++i], trimchar);
                    if (cmdlines.Length == 4)
                        param.Ext = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.AddNew;

                }
                else if (isTheSame(cmd, "-del"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.DelNew;
                }else if(isTheSame(cmd,"-cls"))
                {
                    codeObj.Clear();
                    
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-showAllType"))
                {
                    param.Operation |= OperationType.Show;
                }
            }
        }
        catch (IndexOutOfRangeException)
        {
            ShowError("输入参数个数缺失！！！");
            continue;
        }

        //解析完毕，进行处理

        var flags = param.Operation;
        var hasAddNew = flags.HasFlag(OperationType.AddNew);
        var hasDelNew = flags.HasFlag(OperationType.DelNew);
        var hasCurType = flags.HasFlag(OperationType.CurType);

        if (hasCurType && hasAddNew && hasDelNew)
        {
            ShowWarning("由于使用获取环境信息命令，其他所有操作被屏蔽。");
        }
        else
        {
            if (hasAddNew && hasDelNew)
            {
                ShowError("出现操作冲突，故终止。");
                continue;
            }

            if ((hasAddNew || hasDelNew) && flags.HasFlag(OperationType.NewCode))
            {
                ShowWarning("由于执行增删操作，故通过模板新建文件失败！");
            }
        }


        switch (param.Operation)
        {
            case OperationType.None:
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cmdlines[0]}&exit",
                        CreateNoWindow = false
                    };
                    var p = Process.Start(startInfo);
                    if (p != null)
                    {
                        p.WaitForExit();
                    }
                }
                break;
            case OperationType.NewCode:
                {
                    var curtype = CodeNewEnvironment.CurrentType;
                    if (!codeObj.ContainsKey(curtype))
                    {
                        ShowError("键值不存在，故无法继续！");
                        continue;
                    }

                    var co = codeObj[curtype];
                    var p = Path.Combine(processdir, co.CodePath);

                    if (!File.Exists(p))
                    {
                        ShowError($" {p}  未找到，故无法继续！");
                        continue;
                    }

                    StreamWriter? sw = null;
                    var op = TrimInvaildChars(param.FilePath, trimchar);
                    string buffer;

                    if (!Path.IsPathRooted(op))
                    {
                        op = Path.Combine(currentdir, op);
                    }

                    if (!Path.HasExtension(op))
                    {
                        op = Path.ChangeExtension(op, co.Ext);
                    }
                    buffer = File.ReadAllText(p);

                    try
                    {
                        using (sw = new(op, false, Encoding.UTF8))
                        {
                            sw.WriteLine(string.Format(ProcessCode(buffer), arguments.ToArray()));
                        }
                        ShowInfo("创建完毕，请查看");
                    }
                    catch (FormatException e)
                    {
                        if (arguments.Count == 0)
                        {
                            Regex regex = new(@"\|\[\s*\d\s*\]\|");
                            buffer = regex.Replace(buffer, string.Empty);
                            File.WriteAllText(op, buffer, Encoding.UTF8);
                            ShowWarning("无格式化参数，已用空格代替进行创建。");
                        }
                        else
                        {
                            ShowError(e.Message);
                        }
                    }
                    catch (Exception e)
                    {
                        ShowError(e.Message);
                    }
                    finally
                    {
                        arguments.Clear();
                        sw?.Close();
                    }
                }
                break;
            case OperationType.AddNew:
                {
                    try
                    {
                        if (param.Type.Length > 0 && param.FilePath != null && File.Exists(Path.Combine(processdir, param.FilePath)))
                        {
                            if (codeObj.ContainsKey(param.Type))
                            {
                                ShowError("该项目已存在，请删除再操作！");
                            }
                            else
                            {
                                codeObj.Add(param.Type, new CodeObject { Type = param.Type, CodePath = param.FilePath, Ext = param.Type });
                                SaveConfig(ConfigPath, codeObj);
                                ShowInfo("添加完毕，可用 进行查看！");
                            }
                        }
                        else
                        {
                            ShowError("未知错误，添加失败！");
                        }
                    }
                    catch (Exception e)
                    {
                        ShowError(e.Message);
                    }
                }
                break;
            case OperationType.DelNew:
                {
                    try
                    {
                        if (codeObj.ContainsKey(param.Type))
                        {
                            codeObj.Remove(param.Type);
                            SaveConfig(ConfigPath, codeObj);
                        }
                        else
                        {
                            ShowError($" {param.Type} 不存在，删除失败！");
                        }
                    }
                    catch (Exception e)
                    {
                        ShowError(e.Message);
                    }
                }
                break;
            case OperationType.Show:
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(">> 如下是存储的 Type：");
                    Console.ForegroundColor = ConsoleColor.Green;
                    foreach (var item in codeObj.Keys)
                    {
                        Console.WriteLine(item);
                    }
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;
                }
                break;
            case OperationType.SetType:
                break;
            case OperationType.Processed:
                break;
            case OperationType.CurType:
                ShowInfo(CodeNewEnvironment.CurrentType);
                break;
        }

        if (param.IsKeepingAlive)
        {
            string ncmd;
            //等待下一次输入命令
            do
            {
                Console.Write("<< ");
                ncmd = TrimInvaildChars(Console.ReadLine(), trimchar);
            } while (ncmd.Length == 0);
            cmdlines = ncmd.Split(' ');
        }

        //保存当前 KeepingAlive 值，防止丢失
        var k = param.IsKeepingAlive;
        param = default;
        param.IsKeepingAlive = k;

    } while (param.IsKeepingAlive);

}

static void Pause()
{
    Console.WriteLine("按任意键继续……");
    Console.ReadKey(true);
}

static void ShowHelp()
{

}

static string TrimInvaildChars(string? cmd, char[] trimchar)
{
    if (cmd == null) return string.Empty;
    return cmd.Trim(trimchar);
}

static bool isTheSame(string? s1, string? s2)
{
    return string.Compare(s1, s2, true) == 0;
}

static void ShowWarning(string message)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($">>【警告】{message}");
    Console.ForegroundColor = ConsoleColor.White;
}

static void ShowError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($">>【错误】{message}");
    Console.ForegroundColor = ConsoleColor.White;
}

static void ShowInfo(string message)
{
    Console.ForegroundColor= ConsoleColor.Blue;
    Console.WriteLine($">>【信息】{message}");
    Console.ForegroundColor=ConsoleColor.White;
}

static void SaveConfig(string configPath, Dictionary<string, CodeObject>? codeObj) 
    => File.WriteAllText(configPath, JsonConvert.SerializeObject(codeObj, Formatting.Indented), Encoding.UTF8);


static string ProcessCode(string code)
{
    StringBuilder stringBuilder = new(code);
    stringBuilder.Replace("{", "{{");
    stringBuilder.Replace("}", "}}");
    stringBuilder.Replace("|[", "{");
    stringBuilder.Replace("]|", "}");
    return stringBuilder.ToString();    
}

class CodeObject
{
    public string Type { get; set; } = string.Empty;

    public string CodePath { get; set; } = string.Empty;

    public string Ext { get; set; } = string.Empty;
}

struct CmdLineParam
{
    public OperationType Operation { get; set; }
    public bool IsKeepingAlive { get; set; }
    public string Type { get; set; }
    public string FilePath { get; set; }
    public string Ext { get; set; }
}

struct CodeNewEnvironment
{
    public string CurrentType { get; set; }

}

[Flags]
enum OperationType
{
    None = 0,
    Processed = 2 << 0,
    NewCode = 2 << 1 | Processed,
    AddNew = 2 << 2 | Processed,
    DelNew = 2 << 3 | Processed,
    Show = 2 << 4 | Processed,
    SetType = 2 << 5 | Processed,
    CurType = 2 << 6 | Processed
}
