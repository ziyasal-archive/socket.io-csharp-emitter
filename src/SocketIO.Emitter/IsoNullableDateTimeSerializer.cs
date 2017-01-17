using MsgPack;
using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public class IsoNullableDateTimeSerializer : MessagePackSerializer<DateTime?>
    {
        public IsoNullableDateTimeSerializer(SerializationContext ownerContext) : base(ownerContext) { }

        protected override void PackToCore(Packer packer, DateTime? objectTree)
        {
            if (objectTree.HasValue)
            {
                packer.Pack(objectTree.Value.ToString("o"));
            }
        }

        protected override DateTime? UnpackFromCore(Unpacker unpacker)
        {
            if(unpacker.LastReadData.IsNil)
            {
                return null;
            }
            return DateTime.Parse(unpacker.LastReadData.AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
    }
}
