using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MsgPack.Serialization;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public class Emitter : IEmitter
    {
        private ConnectionMultiplexer _redisClient;
        private readonly IStreamReader _streamReader;

        private readonly List<string> _rooms;
        private readonly Dictionary<string, object> _flags;

        private readonly string _prefix;
		private const int EVENT = 2;
        private const int BINARY_EVENT = 5;

		private string _uid = "emitter";
		private EmitterOptions.EVersion _version;

		private Emitter(ConnectionMultiplexer redisClient, EmitterOptions options, IStreamReader streamReader)
        {
            if (redisClient == null) { InitClient(options); } else { _redisClient = redisClient; }

			_prefix = (!string.IsNullOrWhiteSpace(options.Key) ? options.Key : "socket.io");

			_version = options.Version;

			if (_version == EmitterOptions.EVersion.V0_9_9)
				_prefix += "#emitter";


			_rooms = Enumerable.Empty<string>().ToList();
            _flags = new Dictionary<string, object>();
            _streamReader = streamReader;
        }

        public Emitter(ConnectionMultiplexer redisClient,EmitterOptions options)
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
            Dictionary<string, object> opts = new Dictionary<string, object>();
            packet["type"] = HasBin(args) ? BINARY_EVENT : EVENT;
            packet["data"] = args;

            // set namespace to packet
            if (_flags.ContainsKey("nsp"))
            {
                packet["nsp"] = _flags["nsp"];
                _flags.Remove("nsp");
            }

			// default emit to namespace /
			if(!packet.ContainsKey("nsp"))
			{
				packet["nsp"] = "/";
			}

			opts["rooms"] = _rooms.Any() ? (object)_rooms : string.Empty;
			opts["flags"] = _flags.Any() ? (object)_flags : string.Empty;

			if(_version == EmitterOptions.EVersion.V0_9_9)
			{
				byte[] pack = GetPackedMessage(packet, opts);
				_redisClient.GetSubscriber().Publish(_prefix, pack);
			}
			else
			{ 
				string chn = _prefix + '#' + packet["nsp"] + '#';
				byte[] msg = GetPackedMessage(packet, opts, _uid); 

				if (_rooms.Any())
				{
					foreach(string room in _rooms)
					{
						var chnRoom = chn + room + '#';
						_redisClient.GetSubscriber().Publish(chnRoom, msg);
					}
				}
				else
				{
					_redisClient.GetSubscriber().Publish(chn, msg);
				}
			}
			_rooms.Clear();
            _flags.Clear();

            return this;
        }

        public async Task<IEmitter> EmitAsync(params object[] args)
        {
            Dictionary<string, object> packet = new Dictionary<string, object>();
            Dictionary<string, object> opts = new Dictionary<string, object>();
            packet["type"] = HasBin(args) ? BINARY_EVENT : EVENT;
            packet["data"] = args;

            // set namespace to packet
            if (_flags.ContainsKey("nsp"))
            {
                packet["nsp"] = _flags["nsp"];
                _flags.Remove("nsp");
            }

            // default emit to namespace /
            if (!packet.ContainsKey("nsp"))
            {
                packet["nsp"] = "/";
            }

            opts["rooms"] = _rooms.Any() ? (object)_rooms : string.Empty;
            opts["flags"] = _flags.Any() ? (object)_flags : string.Empty;

            if (_version == EmitterOptions.EVersion.V0_9_9)
            {
                byte[] pack = GetPackedMessage(packet, opts);
                await _redisClient.GetSubscriber().PublishAsync(_prefix, pack);
            }
            else
            {
                string chn = _prefix + '#' + packet["nsp"] + '#';
                byte[] msg = GetPackedMessage(packet, opts, _uid);

                if (_rooms.Any())
                {
                    foreach (string room in _rooms)
                    {
                        var chnRoom = chn + room + '#';
                        await _redisClient.GetSubscriber().PublishAsync(chnRoom, msg);
                    }
                }
                else
                {
                    await _redisClient.GetSubscriber().PublishAsync(chn, msg);
                }
            }
            _rooms.Clear();
            _flags.Clear();

            return this;
        }



        private byte[] GetPackedMessage(Dictionary<string, object> packet, Dictionary<string, object> data, string uid = null)
        {
            var serializer = MessagePackSerializer.Get<object[]>();

            using (Stream stream = new MemoryStream())
            {
				object[] obj;
				if(uid == null)
					obj = new object[] { packet, data };
				else
					obj = new object[] { uid, packet, data };
				

				serializer.Pack(stream, obj);
                return _streamReader.ReadToEnd(stream);
            }
        }

        private bool HasBin(IEnumerable<object> args)
        {
            return args.Any(arg => arg.GetType() == typeof(byte[]));
        }
    }
}
