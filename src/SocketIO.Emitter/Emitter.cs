using log4net;
using MsgPack.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public class Emitter : IEmitter
    {
        ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static SerializationContext ctx;
        private static MessagePackSerializer asyncSerializer;

        private ConnectionMultiplexer _redisClient;
        private readonly IStreamReader _streamReader;

        private readonly List<string> _rooms;
        private readonly Dictionary<string, object> _flags;

        private readonly string _prefix;
        private const int EVENT = 2;
        private const int BINARY_EVENT = 5;

        private string _uid = "emitter";
        private string _nsp = "/";
        private EmitterOptions.EVersion _version;

        private Emitter(ConnectionMultiplexer redisClient, EmitterOptions options, IStreamReader streamReader)
        {
            if (ctx == null)
            {
                ctx = new SerializationContext() { SerializationMethod = SerializationMethod.Map };
                ctx.Serializers.RegisterOverride(new IsoDateTimeSerializer(ctx));
                ctx.Serializers.RegisterOverride(new IsoNullableDateTimeSerializer(ctx));

                asyncSerializer = MessagePackSerializer.Get<object[]>(ctx);
            }

            if (redisClient == null) { InitClient(options); } else { _redisClient = redisClient; }

            _prefix = (!string.IsNullOrWhiteSpace(options.Key) ? options.Key : "socket.io");

            _version = options.Version;

            if (_version == EmitterOptions.EVersion.V0_9_9)
                _prefix += "#emitter";


            _rooms = Enumerable.Empty<string>().ToList();
            _flags = new Dictionary<string, object>();
            _streamReader = streamReader;
        }

        public Emitter(ConnectionMultiplexer redisClient, EmitterOptions options)
            : this(redisClient, options, new StreamReader())
        {

        }

        public Emitter(EmitterOptions options)
            : this(null, options, new StreamReader())
        {

        }

        /// <summary>
        /// Create a redis client from a `host:port` uri string.
        /// </summary>
        /// <param name="options">Emitter options</param>
        private void InitClient(EmitterOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Host)) throw new Exception("Missing redis 'host'");
            if (options.Port == default(int)) throw new Exception("Missing redis 'port'");

            _redisClient = ConnectionMultiplexer.Connect(string.Format("{0}:{1}", options.Host, options.Port));
        }

        /// <summary>
        /// Alias for in
        /// </summary>
        /// <param name="room">room</param>
        /// <returns></returns>
        public IEmitter To(string room)
        {
            _rooms.Clear();

            if (!_rooms.Contains(room))
                _rooms.Add(room);

            return this;
        }

        public IEmitter To(params string[] rooms)
        {
            _rooms.Clear();

            foreach (var room in rooms)
            {
                if (!_rooms.Contains(room))
                    _rooms.Add(room);
            }

            return this;
        }

        public IEmitter Of(string nsp)
        {
            _nsp = nsp;
            return this;
        }

        public IEmitter Emit(params object[] args)
        {
            return Emit((string)args[0], args[1]);
        }

        public IEmitter Emit<T>(string eventName, T arg)
        {
            PacketObject<T> packet = new PacketObject<T>();
            Dictionary<string, object> opts = new Dictionary<string, object>();
            packet.type = HasBin(arg) ? BINARY_EVENT : EVENT;
            packet.data = Tuple.Create(eventName, arg);

            packet.nsp = _nsp;

            opts["rooms"] = _rooms.Any() ? (object)_rooms : string.Empty;
            opts["flags"] = _flags.Any() ? (object)_flags : string.Empty;

            if (_version == EmitterOptions.EVersion.V0_9_9)
            {
                byte[] pack = GetPackedMessage(packet, opts);

                _redisClient.GetSubscriber().Publish(_prefix, pack);
            }
            else
            {
                string chn = _prefix + '#' + packet.nsp + '#';
                byte[] msg = GetPackedMessage(packet, opts, _uid);

                if (_rooms.Any())
                {
                    foreach (string room in _rooms)
                    {
                        var chnRoom = chn + room + '#';

                        log.Debug($"Emitting signal [{eventName}:{chnRoom}]");
                        _redisClient.GetSubscriber().Publish(chnRoom, msg);
                    }
                }
                else
                {
                    log.Debug($"Emitting signal [{eventName}:{chn}]");
                    _redisClient.GetSubscriber().Publish(chn, msg);
                }
            }
            _rooms.Clear();
            _flags.Clear();

            return this;
        }

        private bool HasBin<T>(T arg)
        {
            return arg != null && arg.GetType() == typeof(byte[]);
        }

        public async Task<IEmitter> EmitAsync(params object[] args)
        {
            return await EmitAsync((string)args[0], args[1]);
        }

        public async Task<IEmitter> EmitAsync<T>(string eventName, T arg)
        {
            PacketObject<T> packet = new PacketObject<T>();
            Dictionary<string, object> opts = new Dictionary<string, object>();
            packet.type = HasBin(arg) ? BINARY_EVENT : EVENT;
            packet.data = Tuple.Create(eventName, arg);

            packet.nsp = _nsp;

            opts["rooms"] = _rooms.Any() ? (object)_rooms : string.Empty;
            opts["flags"] = _flags.Any() ? (object)_flags : string.Empty;

            if (_version == EmitterOptions.EVersion.V0_9_9)
            {
                byte[] pack = await GetPackedMessageAsync(packet, opts);
                await _redisClient.GetSubscriber().PublishAsync(_prefix, pack);
            }
            else
            {
                string chn = _prefix + '#' + packet.nsp + '#';
                byte[] msg = await GetPackedMessageAsync(packet, opts, _uid);

                if (_rooms.Any())
                {
                    foreach (string room in _rooms)
                    {
                        var chnRoom = chn + room + '#';

                        log.Debug($"Emitting signal [{eventName}:{chnRoom}]");
                        await _redisClient.GetSubscriber().PublishAsync(chnRoom, msg);
                    }
                }
                else
                {
                    log.Debug($"Emitting signal [{eventName}:{chn}]");
                    await _redisClient.GetSubscriber().PublishAsync(chn, msg);
                }
            }
            _rooms.Clear();
            _flags.Clear();

            return this;
        }

        private byte[] GetPackedMessage<T>(PacketObject<T> packet, Dictionary<string, object> data, string uid = null)
        {
            using (Stream stream = new MemoryStream())
            {
                if (uid == null)
                {
                    var serializer = MessagePackSerializer.Get<object[]>(ctx);
                    serializer.Pack(stream, new object[] { packet, data });
                }
                else
                {
                    var serializer = MessagePackSerializer.Get<object[]>(ctx);
                    serializer.Pack(stream, new object[] { uid, packet, data });
                }

                return _streamReader.ReadToEnd(stream);
            }
        }

        private async Task<byte[]> GetPackedMessageAsync<T>(PacketObject<T> packet, Dictionary<string, object> data, string uid = null)
        {
            using (Stream stream = new MemoryStream())
            {
                if (uid == null)
                {
                    var serializer = MessagePackSerializer.Get<object[]>(ctx);
                    await serializer.PackAsync(stream, new object[] { packet, data });
                }
                else
                {
                    var serializer = MessagePackSerializer.Get<object[]>(ctx);
                    await serializer.PackAsync(stream, new object[] { uid, packet, data });
                }

                return _streamReader.ReadToEnd(stream);
            }
        }

    }
}
