using Library.SystemModels;
using MirDB;
using Server.DBModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Envir
{
    /// <summary>
    /// lyo 2025-11-6 08:42:35
    /// 通讯服务端类
    /// </summary>
    public class NamedPipeServer
    {
        #region Server

        /// <summary>
        /// lyo 2025-11-6 08:57:52
        /// 主管道服务端
        /// </summary>
        static DuplexNamedPipeManager<ManaMessage>? NPServer;
        public static void InitNPServer()
        {
            try
            {
                lock(pullUpObj)
                {
                    if (NPServer == null)
                    {
                        NPServer = new DuplexNamedPipeManager<ManaMessage>("zircon_pipe", new DuplexJsonContext());
                        NPServer.MessageReceived += NPServer_MessageReceived;
                        NPServer.Disconnected += () =>
                        {
                            NPServer.MessageReceived -= NPServer_MessageReceived;
                            NPServer?.Dispose();
                            NPServer = null;
                        };
                        NPServer.StartServerAsync();
                        InitSysLogServer();
                        InitChatLogServer();
                    }
                    if (!pullUpFlag)
                    {
                        pullUpFlag = true;
                        Task.Run(PullUp);
                    }
                }
            }
            catch { }
        }

        private static bool pullUpFlag = false;
        private static object pullUpObj = new object();
        private static void PullUp()
        {
            while (true)
            {
                Thread.Sleep(500);
                if (NPServer == null)
                {
                    InitNPServer();
                }
            }
        }

        public static void StopNPServer()
        {
            try
            {
                if (NPServer != null)
                {
                    NPServer.MessageReceived -= NPServer_MessageReceived;
                    NPServer?.Dispose();
                    NPServer = null;
                }
            }
            catch { }
        }

        private static void NPServer_MessageReceived(ManaMessage msg)
        {
            if (msg == null)
                return;
            switch (msg.Act)
            {
                case ActType.Get:
                    GetServerData1(msg.ModelType);
                    break;
                case ActType.Set:
                    SetServerData1(msg.ModelType, msg.ModelData);
                    break;
                case ActType.Response:
                    OpenPushNPService(msg.ModelType);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// 自动实现
        /// </summary>
        /// <param name="datatype"></param>
        private async static Task GetServerData(string? datatype)
        {
            if (NPServer == null || string.IsNullOrEmpty(datatype)) return;
            if (Session == null)
            {
                Session = new Session(SessionMode.System)
                {
                    BackUpDelay = 60
                };
                Session.Initialize(
                    Assembly.GetAssembly(typeof(ItemInfo)), // returns assembly LibraryCore
                    Assembly.GetAssembly(typeof(AccountInfo)) // returns assembly ServerLibrary
                );
            }
            string dtype = datatype.Substring(0, datatype.LastIndexOf('.'));
            string dprop = datatype.Substring(datatype.LastIndexOf('.') + 1);
            await NPServer.SendMessageAsync(new ManaMessage
            {
                Act = ActType.Response,
                ModelData = JsonSerializer.Serialize(PropertyReflectionHelper.GetPropertyValueFromDescription(dtype, dprop)),
                ModelType = datatype
            });
        }

        public static Session? Session = null;
        /// <summary>
        /// 手动实现
        /// </summary>
        /// <param name="datatype"></param>
        /// <returns></returns>
        private async static Task GetServerData1(string? datatype)
        {
            if (NPServer == null) return;
            if (Session == null)
            {
                Session = new Session(SessionMode.System)
                {
                    BackUpDelay = 60
                };
                Session.Initialize(
                    Assembly.GetAssembly(typeof(ItemInfo)), // returns assembly LibraryCore
                    Assembly.GetAssembly(typeof(AccountInfo)) // returns assembly ServerLibrary
                );
            }
            switch (datatype)
            {
                case "Config":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Config.Instance),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<BaseStat>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<BaseStat>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "SEnvir.AccountInfoList.Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(SEnvir.AccountInfoList?.Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<CastleInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<CastleInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<CompanionInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<CompanionInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<CompanionLevelInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<CompanionLevelInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<CompanionSkillInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<CompanionSkillInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<ItemInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<ItemInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<MonsterInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<MonsterInfo>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<MapRegion>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<MapRegion>().Binding),
                        ModelType = datatype
                    });
                    break;
                case "Session.GetCollection<MapInfo>().Binding":
                    await NPServer.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = JsonSerializer.Serialize(Session.GetCollection<MapInfo>().Binding),
                        ModelType = datatype
                    });
                    break;

                //case ...


                default: break;
            }
        }

        /// <summary>
        /// 手动实现
        /// </summary>
        /// <param name="datatype"></param>
        /// <param name="data"></param>
        private static void SetServerData1(string? datatype, string? data)
        {
            if (NPServer == null || string.IsNullOrEmpty(data)) return;
            if (Session == null)
            {
                Session = new Session(SessionMode.System)
                {
                    BackUpDelay = 60
                };
                Session.Initialize(
                    Assembly.GetAssembly(typeof(ItemInfo)), // returns assembly LibraryCore
                    Assembly.GetAssembly(typeof(AccountInfo)) // returns assembly ServerLibrary
                );
            }
            switch (datatype)
            {
                case "Session.Save(true)":
                    var session = JsonSerializer.Deserialize<Session>(data);
                    if (session != null)
                    {
                        Session = session;
                        Session.Save(true);
                    }
                    break;

                //case todo:

                default: break;
            }
        }

        /// <summary>
        /// 管道控制器
        /// </summary>
        /// <param name="datatype"></param>
        private static void OpenPushNPService(string? datatype)
        {
            switch (datatype)
            {
                case "OpenSysLog":
                    transSysLogLock = true;
                    break;
                case "CloseSysLog":
                    transSysLogLock = false;
                    break;
                case "OpenChatLog":
                    transChatLogLock = true;
                    if(chatLogThread == null)
                    {
                        chatLogThread = new Thread(TransChatLog);
                        chatLogThread.IsBackground = true;
                    }
                    chatLogThread.Start();
                    break;
                case "CloseChatLog":
                    transChatLogLock = false;
                    break;
                default: break;
            }
        }

        #region SysLogServer

        /// <summary>
        /// lyo 2025-11-6 08:58:16
        /// 系统日志管道服务端
        /// </summary>
        static DuplexNamedPipeManager<ManaMessage>? SysLogServer = null;
        public static void InitSysLogServer()
        {
            try
            {
                if (SysLogServer == null)
                {
                    SysLogServer = new DuplexNamedPipeManager<ManaMessage>("zircon_sys_pipe", new DuplexJsonContext());
                    SysLogServer.Connected += () => 
                    { 
                        ManagerDelegate.SysLogMsg += TransSysLog;
                    };
                    SysLogServer.Disconnected += () =>
                    {
                        ManagerDelegate.SysLogMsg -= TransSysLog;
                        SysLogServer?.Dispose();
                        SysLogServer = null;
                    };
                    SysLogServer.StartServerAsync();
                }
            }
            catch { }
        }

        static bool transSysLogLock = false;
        private static void TransSysLog(string msg)
        {
            try
            {
                if (transSysLogLock)
                {
                    _ = SysLogServer?.SendMessageAsync(new ManaMessage
                    {
                        Act = ActType.Response,
                        ModelData = msg,
                        ModelType = "SysLog"
                    });
                }
            }
            catch { }
        }

        #endregion

        #region ChatLogServer

        /// <summary>
        /// lyo 2025-11-6 08:58:45
        /// 聊天日志管道服务端
        /// </summary>
        static DuplexNamedPipeManager<ManaMessage>? ChatLogServer = null;
        public static void InitChatLogServer()
        {
            try
            {
                if (ChatLogServer == null)
                {
                    ChatLogServer = new DuplexNamedPipeManager<ManaMessage>("zircon_chat_pipe", new DuplexJsonContext());
                    ChatLogServer.Disconnected += () =>
                    {
                        ChatLogServer?.Dispose();
                        ChatLogServer = null;
                    };
                    ChatLogServer.StartServerAsync();
                }
            }
            catch { }
        }

        static bool transChatLogLock = false;
        static Thread? chatLogThread = null;
        private static void TransChatLog()
        {
            while(transChatLogLock)
            {
                while (!SEnvir.ChatLogs.IsEmpty && ChatLogServer != null)
                {
                    try
                    {
                        string? log;
                        if (!SEnvir.DisplayChatLogs.TryDequeue(out log))
                            continue;

                        _ = ChatLogServer?.SendMessageAsync(new ManaMessage
                        {
                            Act = ActType.Response,
                            ModelData = log,
                            ModelType = "ChatLog"
                        });
                    }
                    catch { }
                }
                Thread.Sleep(100);
            }
        }

        #endregion

        #endregion

        private async void TestServer()
        {
            using var server = new DuplexNamedPipeManager<ManaMessage>("zircon_pipe", new DuplexJsonContext());

            server.MessageReceived += message =>
            {
                Console.WriteLine($"[{message.Act}] {message.ModelType}: {message.ModelData}");

                if (message.ModelData?.Contains("?") == true)
                {
                    var reply = new ManaMessage
                    {
                        Act = ActType.Get,
                        ModelData = "I received your message!",
                        ModelType = "SEnvir"
                    };
                    _ = server.SendMessageAsync(reply);
                }
            };

            server.Connected += () => Console.WriteLine("Client connected!");
            server.Disconnected += () => Console.WriteLine("Client disconnected.");
            server.ErrorOccurred += ex => Console.WriteLine($"Error: {ex.Message}");

            Console.WriteLine("Starting server...");
            await server.StartServerAsync();

            await server.SendMessageAsync(new ManaMessage
            {
                Act = ActType.Get,
                ModelData = "Welcome to the chat!",
                ModelType = "SEnvir"
            });

            Console.WriteLine("Server running. Press 'q' to quit.");
            while (Console.ReadKey().Key != ConsoleKey.Q)
            {
                //todo...
            }
        }
    }

    /// <summary>
    /// lyo 2025-11-6 11:12:29
    /// 通讯客户端类
    /// </summary>
    public class NamedPipeClient
    {

        #region Client

        /// <summary>
        /// lyo 2025-11-6 08:59:21
        /// 主管道客户端
        /// </summary>
        public static DuplexNamedPipeManager<ManaMessage>? NPClient;
        public static async Task InitNPClient()
        {
            try
            {
                if (NPClient == null)
                {
                    NPClient = new DuplexNamedPipeManager<ManaMessage>("zircon_pipe", new DuplexJsonContext());
                    //NPClient.MessageReceived += action;//页面负责订阅释放
                    NPClient.Disconnected += () =>
                    {
                        NPClient?.Dispose();
                        NPClient = null;
                    };
                    await NPClient.ConnectAsync();
                    InitSysLogClient();
                    InitChatLogClient();
                }
            }
            catch { }
        }

        public static void StopNPClient()
        {
            try
            {
                if (NPClient != null)
                {
                    //NPClient.MessageReceived -= action;
                    NPClient?.Dispose();
                }
            }
            catch { }
        }

        public static async Task<T2?> GetFromNPServer<T1, T2>(T1 modeldata)
        {
            if (NPClient != null)
            {
                var message = new ManaMessage
                {
                    Act = ActType.Get,
                    ModelData = JsonSerializer.Serialize(modeldata),
                    ModelType = typeof(T1).ToString()
                };
                await NPClient.SendMessageAsync(message);
                var res = await NPClient.ReceiveMessageAsync();
                if (res == null || res.ModelData == null) return default;
                return JsonSerializer.Deserialize<T2>(res.ModelData);
            }
            return default;
        }

        public static async Task<T?> GetFromNPServer<T>(string modeltype)
        {
            if (NPClient != null)
            {
                var message = new ManaMessage
                {
                    Act = ActType.Get,
                    ModelData = "",
                    ModelType = modeltype
                };
                await NPClient.SendMessageAsync(message);
                var res = await NPClient.ReceiveMessageAsync(); 
                if (res == null || res.ModelData == null) return default;
                return JsonSerializer.Deserialize<T>(res.ModelData);
            }
            return default;
        }

        public static async Task SetToNPServer<T>(T modeldata)
        {
            if (NPClient != null)
            {
                var message = new ManaMessage
                {
                    Act = ActType.Set,
                    ModelData = JsonSerializer.Serialize(modeldata),
                    ModelType = typeof(T).ToString()
                };
                await NPClient.SendMessageAsync(message);
            }
        }

        public static async Task SetToNPServer(string modeltype, string modeldata)
        {
            if (NPClient != null)
            {
                var message = new ManaMessage
                {
                    Act = ActType.Set,
                    ModelData = modeldata,
                    ModelType = modeltype
                };
                await NPClient.SendMessageAsync(message);
            }
        }

        #region SysLogClient

        /// <summary>
        /// lyo 2025-11-6 08:59:47
        /// 系统日志管道客户端
        /// </summary>
        public static DuplexNamedPipeManager<ManaMessage>? SysLogClient = null;
        public static async Task InitSysLogClient()
        {
            try
            {
                if (SysLogClient == null)
                {
                    SysLogClient = new DuplexNamedPipeManager<ManaMessage>("zircon_sys_pipe", new DuplexJsonContext());
                    //SysLogClient.MessageReceived += action;//页面负责订阅释放
                    SysLogClient.Disconnected += () =>
                    {
                        SysLogClient?.Dispose();
                        SysLogClient = null;
                    };
                    await SysLogClient.ConnectAsync();
                }
            }
            catch { }
        }

        public static async Task OpenSysLog()
        {
            try
            {
                if (NPClient == null)
                    await InitNPClient();
                //if (SysLogClient == null)
                //    await InitSysLogClient();
                var message = new ManaMessage
                {
                    Act = ActType.Response,
                    ModelData = "",
                    ModelType = "OpenSysLog"
                };
                await NPClient.SendMessageAsync(message);
            }
            catch { }
        }

        public static async Task CloseSysLog()
        {
            try
            {
                if (NPClient == null)
                    await InitNPClient();
                //if (SysLogClient == null)
                //    await InitSysLogClient();
                var message = new ManaMessage
                {
                    Act = ActType.Response,
                    ModelData = "",
                    ModelType = "CloseSysLog"
                };
                await NPClient.SendMessageAsync(message);
            }
            catch { }
        }

        #endregion

        #region ChatLogClient

        /// <summary>
        /// lyo 2025-11-6 08:59:47
        /// 聊天日志管道客户端
        /// </summary>
        public static DuplexNamedPipeManager<ManaMessage>? ChatLogClient = null;
        public static async Task InitChatLogClient()
        {
            try
            {
                if (ChatLogClient == null)
                {
                    ChatLogClient = new DuplexNamedPipeManager<ManaMessage>("zircon_chat_pipe", new DuplexJsonContext());
                    //ChatLogClient.MessageReceived += action;//页面负责订阅释放
                    ChatLogClient.Disconnected += () =>
                    {
                        ChatLogClient?.Dispose();
                        ChatLogClient = null;
                    };
                    await ChatLogClient.ConnectAsync();
                }
            }
            catch { }
        }

        public static async Task OpenChatLog()
        {
            try
            {
                if (NPClient == null)
                    await InitNPClient();
                //if (ChatLogClient == null)
                //    await InitChatLogClient();
                var message = new ManaMessage
                {
                    Act = ActType.Response,
                    ModelData = "",
                    ModelType = "OpenChatLog"
                };
                await NPClient.SendMessageAsync(message);
            }
            catch { }
        }

        public static async Task CloseChatLog()
        {
            try
            {
                if (NPClient == null)
                    await InitNPClient();
                //if (ChatLogClient == null)
                //    await InitChatLogClient();
                var message = new ManaMessage
                {
                    Act = ActType.Response,
                    ModelData = "",
                    ModelType = "CloseChatLog"
                };
                await NPClient.SendMessageAsync(message);
            }
            catch { }
        }

        #endregion

        #endregion


        private async void TestClient()
        {
            using var client = new DuplexNamedPipeManager<ManaMessage>("zircon_pipe", new DuplexJsonContext());

            client.MessageReceived += message =>
            {
                Console.WriteLine($"[{message.Act}] {message.ModelType}: {message.ModelData}");
            };

            client.Connected += () => Console.WriteLine("Connected to server!");
            client.Disconnected += () => Console.WriteLine("Disconnected from server.");
            client.ErrorOccurred += ex => Console.WriteLine($"Error: {ex.Message}");

            Console.WriteLine("Connecting to server...");
            await client.ConnectAsync();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var message = await client.ReceiveMessageAsync();
                    if (message != null)
                    {
                        Console.WriteLine($"Received via channel: {message.ModelData}");
                    }
                }
            });

            Console.WriteLine("Type messages to send (or 'quit' to exit):");
            while (true)
            {
                var input = Console.ReadLine();
                if (input?.ToLower() == "quit") break;

                var message = new ManaMessage
                {
                    Act = ActType.Get,
                    ModelData = input,
                    ModelType = "SEnvir"
                };

                await client.SendMessageAsync(message);
            }
        }
    }
}

/*
 * SEnvir.AccountInfoList?.Binding
 * Session.GetCollection<BaseStat>().Binding
 * Session.GetCollection<BundleInfo>().Binding
 * Session.GetCollection<CastleInfo>().Binding
 * Session.GetCollection<CompanionInfo>().Binding;
 * Session.GetCollection<CompanionLevelInfo>().Binding;
 * Session.GetCollection<CompanionSkillInfo>().Binding;
 * Session.GetCollection<ItemInfo>().Binding
 * Session.GetCollection<MonsterInfo>().Binding
 * Session.GetCollection<MapRegion>().Binding
 * Session.GetCollection<MapInfo>().Binding
 * Session.SystemPath
 * Session.Save(true);
 * SEnvir.CharacterInfoList?.Binding
 * SEnvir.ChatLogs
 * SEnvir.DisplayChatLogs
 * Config
 * Config.LoadVersion();
 * SEnvir.Broadcast()
 * 
 * 
 */
