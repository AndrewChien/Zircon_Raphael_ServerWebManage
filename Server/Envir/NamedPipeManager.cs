using MirDB;
using System.Buffers;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Server.Envir
{
    /// <summary>
    /// lyo 2025-11-6 08:40:49
    /// 跨平台AOT双工NamedPipe通讯
    /// </summary>
    public sealed class DuplexNamedPipeManager<T> : IDisposable where T : class
    {
        private readonly string _pipeName;
        private readonly JsonSerializerContext _jsonContext;
        private NamedPipeServerStream? _serverStream;
        private NamedPipeClientStream? _clientStream;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed = false;

        private readonly Channel<T> _sendChannel = Channel.CreateUnbounded<T>();
        private readonly Channel<T> _receiveChannel = Channel.CreateUnbounded<T>();

        public event Action<T>? MessageReceived;
        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<Exception>? ErrorOccurred;

        public bool IsConnected => _serverStream?.IsConnected == true || _clientStream?.IsConnected == true;

        public DuplexNamedPipeManager(string pipeName, JsonSerializerContext jsonContext)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _jsonContext = jsonContext ?? throw new ArgumentNullException(nameof(jsonContext));
        }

        public async Task StartServerAsync(CancellationToken cancellationToken = default)
        {
            if (_serverStream != null)
                throw new InvalidOperationException("Server is already running");

            _serverStream = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            _ = Task.Run(() => ProcessOutgoingMessagesAsync(_serverStream, cancellationToken));

            await _serverStream.WaitForConnectionAsync(cancellationToken);
            Connected?.Invoke();

            _ = Task.Run(() => ProcessIncomingMessagesAsync(_serverStream, cancellationToken));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_clientStream != null)
                throw new InvalidOperationException("Already connected or connecting");

            _clientStream = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            await _clientStream.ConnectAsync(cancellationToken);
            Connected?.Invoke();

            _ = Task.Run(() => ProcessOutgoingMessagesAsync(_clientStream, cancellationToken));
            _ = Task.Run(() => ProcessIncomingMessagesAsync(_clientStream, cancellationToken));
        }

        public async Task SendMessageAsync(T message, CancellationToken cancellationToken = default)
        {
            await _sendChannel.Writer.WriteAsync(message, cancellationToken);
        }

        public async Task<T?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            return await _receiveChannel.Reader.ReadAsync(cancellationToken);
        }

        private async Task ProcessOutgoingMessagesAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (pipeStream.IsConnected)
                    {
                        await WriteMessageAsync(pipeStream, message, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }

        private async Task ProcessIncomingMessagesAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            var buffer = new byte[4]; // 用于读取长度
            var lengthBuffer = new byte[4];

            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
                {
                    // 读取消息长度
                    int bytesRead = await pipeStream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    if (bytesRead == 0)
                    {
                        // 连接已关闭
                        Disconnected?.Invoke();
                        break;
                    }

                    if (bytesRead == 4)
                    {
                        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                        if (messageLength > 0 && messageLength <= 10 * 1024 * 1024) // 10MB限制
                        {
                            // 读取消息内容
                            byte[] messageBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
                            try
                            {
                                int totalRead = 0;
                                while (totalRead < messageLength)
                                {
                                    bytesRead = await pipeStream.ReadAsync(
                                        messageBuffer, totalRead, messageLength - totalRead, cancellationToken);

                                    if (bytesRead == 0) break;
                                    totalRead += bytesRead;
                                }

                                if (totalRead == messageLength)
                                {
                                    var message = JsonSerializer.Deserialize<T>(
                                        messageBuffer.AsSpan(0, messageLength), _jsonContext.Options);

                                    if (message != null)
                                    {
                                        MessageReceived?.Invoke(message);
                                        await _receiveChannel.Writer.WriteAsync(message, cancellationToken);
                                    }
                                }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(messageBuffer);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                Disconnected?.Invoke();
            }
        }

        private async Task WriteMessageAsync(PipeStream pipeStream, T message, CancellationToken cancellationToken)
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, typeof(T), _jsonContext);

            byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            await pipeStream.WriteAsync(lengthBytes, 0, 4, cancellationToken);

            await pipeStream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken);
            await pipeStream.FlushAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
                _serverStream?.Dispose();
                _clientStream?.Dispose();
                _sendChannel.Writer.Complete();
                _receiveChannel.Writer.Complete();
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }
        }
    }

    [JsonSerializable(typeof(Config))]
    [JsonSerializable(typeof(Session))]
    [JsonSerializable(typeof(ManaMessage))]
    [JsonSerializable(typeof(CommandMessage))]
    [JsonSerializable(typeof(DataMessage))]
    public partial class DuplexJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// lyo 2025-11-6 08:56:20
    /// 通讯包装器
    /// </summary>
    public class ManaMessage
    {
        public ActType Act { get; set; }
        public string? ModelType { get; set; }
        public string? ModelData { get; set; }
    }

    public enum ActType
    {
        Get = 0,
        Set = 1,
        Response = 2
    }

    public class CommandMessage
    {
        public string? Command { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
    }

    public class DataMessage
    {
        public string? DataType { get; set; }
        public byte[]? Payload { get; set; }
    }

    /// <summary>
    /// lyo 2025-11-6 08:41:07
    /// </summary>
    public static class DuplexNamedPipeExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public class RequestResponse<TRequest, TResponse>
        {
            public string RequestId { get; set; } = Guid.NewGuid().ToString();
            public TRequest? Request { get; set; }
            public TResponse? Response { get; set; }
        }

        public static async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(
            this DuplexNamedPipeManager<RequestResponse<TRequest, TResponse>> pipe,
            TRequest request,
            TimeSpan timeout = default)
            where TRequest : class
            where TResponse : class
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            var requestMessage = new RequestResponse<TRequest, TResponse>
            {
                RequestId = Guid.NewGuid().ToString(),
                Request = request
            };

            var completionSource = new TaskCompletionSource<TResponse?>();
            var cts = new CancellationTokenSource(timeout);

            void ResponseHandler(RequestResponse<TRequest, TResponse> response)
            {
                if (response.RequestId == requestMessage.RequestId)
                {
                    pipe.MessageReceived -= ResponseHandler;
                    completionSource.TrySetResult(response.Response);
                }
            }

            pipe.MessageReceived += ResponseHandler;
            cts.Token.Register(() => completionSource.TrySetCanceled());

            await pipe.SendMessageAsync(requestMessage);
            return await completionSource.Task;
        }
    }

    /// <summary>
    /// lyo 2025-11-6 08:41:19
    /// </summary>
    public static class PropertyReflectionHelper
    {
        public static object GetPropertyValueFromDescription(
            string typeDescription,
            string propertyDescription,
            object targetInstance = null)
        {
            if (string.IsNullOrEmpty(typeDescription))
                throw new ArgumentException("类型描述不能为空", nameof(typeDescription));

            if (string.IsNullOrEmpty(propertyDescription))
                throw new ArgumentException("属性描述不能为空", nameof(propertyDescription));

            // 获取类型
            Type targetType = GetTypeFromDescription(typeDescription);
            if (targetType == null)
                throw new ArgumentException($"未找到类型: {typeDescription}");

            // 获取或创建实例
            object instance = targetInstance ?? CreateInstance(targetType);

            // 获取属性值
            return GetPropertyValue(instance, propertyDescription);
        }

        private static Type GetTypeFromDescription(string typeDescription)
        {
            // 首先尝试直接获取类型
            Type type = Type.GetType(typeDescription);
            if (type != null) return type;

            // 在当前域的所有程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeDescription);
                if (type != null) return type;
            }

            // 尝试部分匹配（不包含程序集版本信息）
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes()
                    .FirstOrDefault(t => t.FullName == typeDescription ||
                                        t.Name == typeDescription);
                if (type != null) return type;
            }

            return null;
        }

        private static object CreateInstance(Type type)
        {
            try
            {
                // 尝试使用无参构造函数
                return Activator.CreateInstance(type);
            }
            catch
            {
                // 如果无参构造函数不可用，返回类型的默认值
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
        }

        private static object GetPropertyValue(object obj, string propertyPath)
        {
            if (obj == null) return null;

            object current = obj;
            string[] properties = propertyPath.Split('.');

            foreach (string propertyName in properties)
            {
                if (current == null) return null;

                Type currentType = current.GetType();
                PropertyInfo property = currentType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property == null)
                    throw new ArgumentException($"在类型 {currentType.Name} 中未找到属性: {propertyName}");

                if (!property.CanRead)
                    throw new ArgumentException($"属性 {propertyName} 不可读");

                current = property.GetValue(current);
            }

            return current;
        }

        public static TProperty GetPropertyValueFromDescription<TProperty>(
            string typeDescription,
            string propertyDescription,
            object targetInstance = null)
        {
            object value = GetPropertyValueFromDescription(typeDescription, propertyDescription, targetInstance);

            if (value == null)
                return default(TProperty);

            try
            {
                return (TProperty)Convert.ChangeType(value, typeof(TProperty));
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"无法将属性值从 {value.GetType().Name} 转换为 {typeof(TProperty).Name}");
            }
        }

        public static Dictionary<string, object> GetMultiplePropertyValues(
            string typeDescription,
            string[] propertyDescriptions,
            object targetInstance = null)
        {
            var results = new Dictionary<string, object>();

            foreach (string propertyDesc in propertyDescriptions)
            {
                try
                {
                    object value = GetPropertyValueFromDescription(typeDescription, propertyDesc, targetInstance);
                    results[propertyDesc] = value;
                }
                catch (Exception ex)
                {
                    results[propertyDesc] = $"错误: {ex.Message}";
                }
            }

            return results;
        }

        public static Dictionary<string, object> GetAllPropertyValues(
            string typeDescription,
            object targetInstance = null)
        {
            Type targetType = GetTypeFromDescription(typeDescription);
            if (targetType == null)
                throw new ArgumentException($"未找到类型: {typeDescription}");

            object instance = targetInstance ?? CreateInstance(targetType);

            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var results = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    try
                    {
                        object value = property.GetValue(instance);
                        results[property.Name] = value;
                    }
                    catch (Exception ex)
                    {
                        results[property.Name] = $"获取失败: {ex.Message}";
                    }
                }
            }

            return results;
        }
    }
}
