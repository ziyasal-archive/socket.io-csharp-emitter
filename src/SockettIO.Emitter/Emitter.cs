using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace SockettIO.Emitter
{
    public class Emitter : IEmitter
    {
        private ConnectionMultiplexer _redisClient;

        private readonly List<string> _rooms;
        private readonly Dictionary<string, object> _flags;

        private readonly string _key;

        private const int EVENT = 2;
        private const int BINARY_EVENT = 5;

        public Emitter(ConnectionMultiplexer redisClient, EmitterOptions options)
        {
            if (redisClient == null)
            {
                InitClient(options);
            }
            else
            {
                _redisClient = redisClient;
            }

            _key = (!string.IsNullOrWhiteSpace(options.Key) ? options.Key : "socket.io") + "#emitter";

            _rooms = Enumerable.Empty<string>().ToList();
            _flags = new Dictionary<string, object>();

            InitFlags();
        }

        /// <summary>
        /// Create a redis client from a `host:port` uri string.
        /// </summary>
        /// <param name="options">Emitter options</param>
        private void InitClient(EmitterOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Socket) && string.IsNullOrWhiteSpace(options.Host)) throw new Exception("'Missing redis 'host'");
            if (string.IsNullOrWhiteSpace(options.Socket) && options.Port == default(int)) throw new Exception("'Missing redis 'port'");

            _redisClient = ConnectionMultiplexer.Connect(string.Format("{0}:{1}", options.Host, options.Port));
        }

        /// <summary>
        ///  Limit emission to a certain `room`.
        /// </summary>
        /// <param name="room">room</param>
        /// <returns></returns>
        public IEmitter In(string room)
        {
            if (!_rooms.Contains(room))
            {
                _rooms.Add(room);
            }
            return this;
        }

        /// <summary>
        /// Alias for in
        /// </summary>
        /// <param name="room">room</param>
        /// <returns></returns>
        public IEmitter To(string room)
        {
            return In(room);
        }

        public IEmitter Of(string nsp)
        {
            _flags["nsp"] = nsp;
            return this;
        }

        public IEmitter Emit(params object[] args)
        {
            Dictionary<string, object> packet = new Dictionary<string, object>();
            packet["type"] = HasBin(args) ? BINARY_EVENT : EVENT;
            packet["data"] = args;

            // set namespace to packet
            if (_flags["nsp"] != null)
            {
                packet["nsp"] = _flags["nsp"];
                _flags.Remove("nsp");
            }
            else
            {
                packet["nsp"] = '/';
            }


            byte[] bytes = new MsgPack.CompiledPacker().Pack(new object[] { packet, new { rooms = _rooms, flags = _flags } });

            _redisClient.GetSubscriber().Publish(_key, bytes);
            _rooms.Clear();
            _flags.Clear();

            return this;
        }

        private bool HasBin(IEnumerable<object> args)
        {
            return args.Any(arg => arg.GetType() == typeof(byte[]));
        }

        private void InitFlags()
        {
            _flags[Flags.JSON] = true;
            _flags[Flags.Broadcast] = true;
            _flags[Flags.Volatile] = true;
        }
    }
}
