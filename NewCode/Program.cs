using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Text;
using System.Security.Principal;
using NewCode;
using System.ComponentModel;

Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.UTF8;
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(Resource.MyInfo);
Console.ForegroundColor = ConsoleColor.White;
var processdir = AppDomain.CurrentDomain.BaseDirectory;
var ConfigPath = Path.Combine(processdir, "NewCode.json");
Data.IsAdmin = IsAdmin();
string ncmd;

Dictionary<string, CodeObject>? codeObj;

// param 负责获取配置参数，arguments 负责获取当含有格式化时的参数
CmdLineParam param = new();
CodeNewEnvironment CodeNewEnvironment = new();
List<string> arguments = new();
char[] trimchar = { '\\', '\"', '\'', '/' };
Regex CmdRegex= new Regex("(\"[^\"]+\"|[^\\s\"]+)");
List<string> cmdlines;

if (File.Exists(ConfigPath))
{
    codeObj = JsonConvert.DeserializeObject(File.ReadAllText(ConfigPath, System.Text.Encoding.UTF8),
        typeof(Dictionary<string, CodeObject>)) as Dictionary<string, CodeObject>;
    if (codeObj == null)
    {
        codeObj = new Dictionary<string, CodeObject>();
    }
    else
    {
        var invaild = false;
        List<string> unv = new();
        foreach (var item in codeObj.Keys)
        {
            var value = codeObj[item];
            if (isTheSame(value.Type, "NULL"))
            {
                invaild = true;
                unv.Add(item);
                continue;
            }
            if (!File.Exists(Path.Combine(processdir, value.CodePath)))
            {
                invaild = true;
                unv.Add(item);
                continue;
            }
        }

        if (invaild)
        {
            foreach (var item in unv)
            {
                codeObj.Remove(item);
            }
            ShowWarning("配置选项含有非法或无效内容，已清除。");
        }
    }

    var keys = codeObj.Keys;
    if (keys.Count > 0)
    {
        CodeNewEnvironment.CurrentType = keys.First();
    }
}
else
{
    File.WriteAllText(ConfigPath, string.Empty, System.Text.Encoding.UTF8);
    codeObj = new Dictionary<string, CodeObject>();
}

if (args.Length < 1)
{
    ShowHelp();
    Console.ForegroundColor = ConsoleColor.White;
    Pause();
}
else
{
    cmdlines = args.ToList();
    ncmd = string.Join(' ', args);

    do
    {
        //解析命令行代码区
        try
        {
            param.Operation = OperationType.None;
            param.Type = string.Empty;
            param.FilePath = string.Empty;
            param.Ext = string.Empty;

            for (int i = 0; i < cmdlines.Count; i++)
            {
                var cmd = TrimInvaildChars(cmdlines[i], trimchar);

                if (isTheSame(cmd, "-t") || isTheSame(cmd, "-type"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.NewCode;
                }
                else if (isTheSame(cmd, "-p") || isTheSame(cmd, "-path"))
                {
                    param.FilePath = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.NewCode;
                }
                else if (isTheSame(cmd, "-f") || isTheSame(cmd, "-fill") || isTheSame(cmd, "-param"))
                {
                    while (i < cmdlines.Count - 1)
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
                else if (isTheSame(cmd, "-k") || isTheSame(cmd, "-alive") || isTheSame(cmd, "-keep") || isTheSame(cmd, "-keepalive"))
                {
                    if (param.IsKeepingAlive)
                    {
                        ShowWarning("当前状态已是 keepalive 模式。");
                    }
                    param.IsKeepingAlive = true;
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-st") || isTheSame(cmd, "-settype"))
                {
                    CodeNewEnvironment.CurrentType = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.SetType;
                }
                else if (isTheSame(cmd, "-q") || isTheSame(cmd, "-quit"))
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
                    if (cmdlines.Count == 4)
                        param.Ext = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.AddNew;
                    if (isTheSame(param.Type, "NULL"))
                    {
                        param.Operation = OperationType.Processed;
                        ShowError("无效数据！！！");
                        goto LblParse;
                    }
                }
                else if (isTheSame(cmd, "-mod"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    if (!codeObj.ContainsKey(param.Type))
                    {
                        ShowError("键值不存在，故无法修改！");
                    }
                    string tmp;
                    if (cmdlines.Count == 3)
                    {
                        tmp = cmdlines[++i];
                        if (string.Compare(tmp, 0, "p=", 0, 2, true) == 0)
                        {
                            param.FilePath = tmp[2..];
                        }
                        else if (string.Compare(tmp, 0, "ext=", 0, 4, true) == 0)
                        {
                            param.Ext = tmp[4..];
                        }

                    }
                    else if (cmdlines.Count == 4)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            tmp = cmdlines[++i];

                            if (string.Compare(tmp, 0, "p=", 0, 2, true) == 0)
                            {
                                param.FilePath = tmp;
                            }
                            else if (string.Compare(tmp, 0, "ext=", 0, 4, true) == 0)
                            {
                                param.Ext = tmp;
                            }
                        }
                    }
                    param.Operation |= OperationType.ModNew;
                }
                else if (isTheSame(cmd, "-del"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.DelNew;
                }
                else if (isTheSame(cmd, "-cls"))
                {
                    codeObj.Clear();
                    CodeNewEnvironment.CurrentType = null;
                    SaveConfig(ConfigPath, codeObj);
                    ShowInfo("删除成功！！！");
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-showAll"))
                {
                    param.Operation |= OperationType.ShowAll;
                }
                else if (isTheSame(cmd, "-showInfo") || isTheSame(cmd, "-showtype"))
                {
                    param.Type = TrimInvaildChars(cmdlines[++i], trimchar);
                    param.Operation |= OperationType.ShowInfo;
                }
                else if (isTheSame(cmd, "-h") || isTheSame(cmd, "-help"))
                {
                    ShowHelp();
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-setenv"))
                {
                    param.Operation |= OperationType.SetEnv;
                }
                else if (isTheSame(cmd, "-delenv"))
                {
                    param.Operation |= OperationType.DelEnv;
                }
                else if (isTheSame(cmd, "-r") || isTheSame(cmd, "-restart"))
                {
                    string id = string.Empty;
                    if (cmdlines.Count == 2)
                    {
                        id = cmdlines[1];
                    }

                    try
                    {
                        ProcessStartInfo info = new()
                        {
                            FileName = Process.GetCurrentProcess().MainModule?.FileName,
                            Arguments = "-k",
                            UseShellExecute = true //创建新的程序
                        };

                        if (id == "#")
                        {
                            if (info == null)
                            {
                                ShowError("管理员重启失败！");
                                goto LblParse;
                            }
                            info.Verb = "runas";
                        }
                        Process.Start(info);
                        return;
                    }
                    catch (Win32Exception)
                    {
                        ShowError("请求管理员权限未授权，故重启失败。");
                    }
                }
                else if (isTheSame(cmd, "-pwd") || isTheSame(cmd, "-curdir"))
                {
                    ShowInfo(Environment.CurrentDirectory);
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-prodir"))
                {
                    ShowInfo(processdir);
                    param.Operation |= OperationType.Processed;
                }
                else if (isTheSame(cmd, "-cd"))
                {
                    try
                    {
                        if (Environment.CurrentDirectory == null)
                        {
                            Environment.CurrentDirectory = processdir;
                        }
                        var path = GetFilePath(cmdlines[++i], Environment.CurrentDirectory);
                        if (!File.Exists(path))
                        {
                            throw new IOException($"{path} 不存在！");
                        }
                        Environment.CurrentDirectory = path;
                    }
                    catch (Exception e)
                    {
                        ShowError(e.Message);
                    }
                    param.Operation |= OperationType.Processed;
                }
            }
        }
        catch (Exception e)
        {
            if (e is ArgumentOutOfRangeException || e is IndexOutOfRangeException)
                ShowError("输入参数个数缺失！！！");
            else
                ShowError(e.Message);
            param.Operation = OperationType.Processed;
        }

        //解析完毕，进行处理

        var flags = param.Operation;
        var hasAddNew = flags.HasFlag(OperationType.AddNew);
        var hasDelNew = flags.HasFlag(OperationType.DelNew);
        var hasCurType = flags.HasFlag(OperationType.CurType);
        var hasSetEnv = flags.HasFlag(OperationType.SetEnv);
        var hasDelEnv = flags.HasFlag(OperationType.DelEnv);

        if (hasCurType && (hasAddNew || hasDelNew || hasDelEnv || hasSetEnv))
        {
            ShowWarning("由于使用获取环境信息命令，其他所有操作被屏蔽。");
            param.Operation = OperationType.CurType;
        }
        else
        {
            if ((hasAddNew && hasDelNew) || (hasSetEnv && hasDelEnv))
            {
                ShowError("出现操作冲突，故终止。");
                goto LblParse;
            }

            if ((hasDelEnv || hasSetEnv) && (hasDelNew || hasAddNew))
            {
                ShowWarning("涉及修改环境变量信息的命令，故屏蔽其他命令。");
                param.Operation = hasSetEnv ? OperationType.SetEnv : OperationType.DelEnv;
            }
            else
            {
                if ((hasAddNew || hasDelNew) && flags.HasFlag(OperationType.NewCode))
                {
                    ShowWarning("由于执行增删操作，故通过模板新建文件失败！");
                }

            }
        }

        switch (param.Operation)
        {
            case OperationType.None:
                {
                    if (cmdlines[0].StartsWith('-'))
                    {
                        ShowError("非法调用外部命令，请查看是命令是否输入错误。");
                        break;
                    }

                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {ncmd}&exit",
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

                    if (curtype == null || curtype == string.Empty)
                    {
                        ShowError("当前的创建代码模板类型为 NULL ，故无法创建代码！");
                        break;
                    }

                    if (!codeObj.ContainsKey(curtype))
                    {
                        ShowError("键值不存在，故无法继续！");
                        break;
                    }

                    var co = codeObj[curtype];
                    var p = GetFilePath(co.CodePath, processdir);

                    if (!File.Exists(p))
                    {
                        ShowError($" {p}  未找到，故无法继续！");
                        break;
                    }

                    StreamWriter? sw = null;
                    var op = TrimInvaildChars(param.FilePath, trimchar);
                    string buffer;

                    op = GetFilePath(op, Environment.CurrentDirectory);

                    if (!Path.HasExtension(op))
                    {
                        op = Path.ChangeExtension(op, co.Ext);
                    }

                    buffer = File.ReadAllText(p);
                    try
                    {

                        if (File.Exists(op))
                        {
                            ShowWarning($" {op} 已存在，请输入 y 确认！");
                            Console.Write("按下 y 确认覆盖，按下 n 取消：");
                            ConsoleKeyInfo kd;
                            do
                            {
                                kd = Console.ReadKey(true);
                            } while (kd.Key != ConsoleKey.Y && kd.Key != ConsoleKey.N);
                            if (kd.Key == ConsoleKey.N)
                            {
                                Console.WriteLine("N");
                                throw new IOException("文件覆盖已被用户取消！");
                            }
                            else
                            {
                                Console.WriteLine("Y");
                            }
                        }

                        using (sw = new(op, false, Encoding.UTF8))
                        {
                            sw.Write(string.Format(ProcessCode(buffer), arguments.ToArray()));
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
                        if (param.Type.Length > 0 && File.Exists(Path.Combine(processdir, param.FilePath)))
                        {
                            if (codeObj.ContainsKey(param.Type))
                            {
                                ShowError("该项目已存在，请删除再操作！");
                            }
                            else
                            {
                                codeObj.Add(param.Type, new CodeObject { Type = param.Type, CodePath = param.FilePath, Ext = param.Ext.Length > 0 ? param.Ext : param.Type });
                                SaveConfig(ConfigPath, codeObj);
                                ShowInfo("添加完毕，可用查询函数进行查看！");
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

                            if (param.Type == CodeNewEnvironment.CurrentType)
                            {
                                var keys = codeObj.Keys;
                                CodeNewEnvironment.CurrentType = keys.Count > 0 ? keys.First() : string.Empty;
                            }
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
            case OperationType.ShowAll:
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (codeObj.Count > 0)
                    {
                        Console.WriteLine(">> 如下是存储的 Type：");
                        Console.ForegroundColor = ConsoleColor.Green;
                        foreach (var item in codeObj.Keys)
                        {
                            Console.WriteLine(item);
                        }
                    }
                    else
                    {
                        Console.WriteLine(">> 没有存储的 Type。");
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                }
                break;
            case OperationType.SetType:
                ShowInfo("设置完毕！！！");
                break;
            case OperationType.Processed:
                break;
            case OperationType.CurType:
                if (CodeNewEnvironment.CurrentType == null)
                {
                    ShowInfo("NULL");
                }
                else
                {
                    ShowInfo(CodeNewEnvironment.CurrentType);
                }
                break;
            case OperationType.ShowInfo:
                {
                    if (!codeObj.ContainsKey(param.Type))
                    {
                        ShowError("键值不存在，请修改进行查询！");
                        break;
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"如下是 {param.Type} 的信息：");
                    var item = codeObj[param.Type];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"代码的类型：{item.Type}\n代码扩展名：{item.Ext}\n代码路径：{item.CodePath}\n代码绝对路径：{GetFilePath(item.CodePath, processdir)}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                break;
            case OperationType.SetEnv:
                try
                {
                    if (!Data.IsAdmin)
                    {
                        ShowError("非管理员权限无法使用此操作！");
                        break;
                    }
                    RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true);
                    if (key == null)
                    {
                        ShowError("打开注册表失败！！！");
                        break;
                    }
                    var value = key.GetValue("Path");
                    if (value == null)
                    {
                        ShowError("打开环境变量路径键值失败！！！");
                        break;
                    }
                    var paths = value.ToString();
                    if (paths == null)
                    {
                        ShowError("解析环境变量路径键值失败！");
                        break;
                    }

                    key.SetValue("Path", string.Join(';', value, processdir));
                    key.Close();
                    ShowInfo("修改完毕！");

                }
                catch (Exception e)
                {
                    ShowError(e.Message);
                }
                break;
            case OperationType.DelEnv:
                try
                {
                    if (!Data.IsAdmin)
                    {
                        ShowError("非管理员权限无法使用此操作！");
                        break;
                    }
                    RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true);
                    if (key == null)
                    {
                        ShowError("打开注册表失败！！！");
                        break;
                    }
                    var value = key.GetValue("Path");
                    if (value == null)
                    {
                        ShowError("打开环境变量路径键值失败！！！");
                        break;
                    }
                    var paths = value.ToString();
                    if (paths == null)
                    {
                        ShowError("解析环境变量路径键值失败！");
                        break;
                    }

                    var mpaths = paths.Split(";");
                    List<string> lpath = new();
                    foreach (var mp in mpaths)
                    {
                        DirectoryInfo info = new(mp);
                        if (!isTheSame(info.FullName, processdir))
                        {
                            lpath.Add(mp);
                        }
                    }
                    key.SetValue("Path", string.Join(';', lpath));
                    key.Close();
                    lpath.Clear();
                    ShowInfo("修改完毕！");
                }
                catch (Exception e)
                {
                    ShowError(e.Message);
                }
                break;
            case OperationType.ModNew:
                {
                    if (param.FilePath.Length > 0)
                    {
                        if (File.Exists(GetFilePath(param.FilePath, processdir)))
                        {
                            codeObj[param.Type].CodePath = param.FilePath;
                        }
                        else
                        {
                            ShowWarning("代码文件不存在，故忽略");
                        }
                    }
                    if (param.Ext.Length > 0)
                    {
                        codeObj[param.Type].Ext = param.Ext;
                    }
                    SaveConfig(ConfigPath, codeObj);
                    ShowInfo("修改成功！！！");
                }
                break;
        }

    LblParse:

        if (param.IsKeepingAlive)
        {
            //等待下一次输入命令
            do
            {
                ConsoleIn();
                ncmd = ConsoleReadLine();
                Console.ForegroundColor = ConsoleColor.White;
            } while (ncmd.Length == 0);

            cmdlines.Clear();   //清理陈旧的命令解析

            //处理命令行
            var matches = CmdRegex.Matches(ncmd);
            foreach (Match item in matches)
            {
                cmdlines.Add(TrimInvaildChars(item.Value, trimchar));
            }
        }

        //保存当前 KeepingAlive 值，防止丢失
        var k = param.IsKeepingAlive;
        param = default;
        param.IsKeepingAlive = k;

    } while (param.IsKeepingAlive);

}

static void Pause()
{
    Console.Write("按任意键继续……");
    Console.ReadKey(true);
}

static void ShowHelp()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(">> 如下是本程序的帮助：\n");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(Resource.HelpFile);
    Console.WriteLine();
}

static string GetFilePath(string path, string dfolder)
{
    if (!Path.IsPathRooted(path))
    {
        return Path.Combine(dfolder, path);
    }
    return path;
}

static void ConsoleIn()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Black;
    Console.BackgroundColor = Data.IsAdmin ? ConsoleColor.Red : ConsoleColor.Green;
    Console.Write($"{(Data.IsAdmin ? '#' : '$')} <<");
    Console.BackgroundColor = ConsoleColor.Black;
    Console.Write(' ');
    Console.ForegroundColor = ConsoleColor.DarkYellow;
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

static void ShowWarning(string? message)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($">>【警告】{message}");
    Console.ForegroundColor = ConsoleColor.White;
}

static void ShowError(string? message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($">>【错误】{message}");
    Console.ForegroundColor = ConsoleColor.White;
}

static void ShowInfo(string? message)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($">>【信息】{message}");
    Console.ForegroundColor = ConsoleColor.White;
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

static bool IsAdmin()
{
    WindowsIdentity identity = WindowsIdentity.GetCurrent();
    WindowsPrincipal principal = new(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}

static string ConsoleReadLine()
{
    var buffer = Console.ReadLine();
    if (buffer == null)
        return string.Empty;
    return buffer;
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
    public string? CurrentType { get; set; }

}

[Flags]
enum OperationType : ulong
{
    None = 0,
    Processed = 2 << 0,
    NewCode = 2 << 1 | Processed,
    AddNew = 2 << 2 | Processed,
    DelNew = 2 << 3 | Processed,
    ShowAll = 2 << 4 | Processed,
    SetType = 2 << 5 | Processed,
    CurType = 2 << 6 | Processed,
    ShowInfo = 2 << 7 | Processed,
    SetEnv = 2 << 8 | Processed,
    DelEnv = 2 << 9 | Processed,
    ModNew = 2 << 10 | Processed
}

static class Data
{
    public static bool IsAdmin { get; set; }
}