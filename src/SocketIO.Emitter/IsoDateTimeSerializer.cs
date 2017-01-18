using MsgPack;
using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public class IsoDateTimeSerializer : MessagePackSerializer<DateTime>
    {
        public IsoDateTimeSerializer(SerializationContext ownerContext) : base(ownerContext) { }

        protected override void PackToCore(Packer packer, DateTime objectTree)
        {
            var date = objectTree.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(objectTree, DateTimeKind.Utc) : objectTree;
            
            packer.Pack(date.ToString("o"));
        }

        protected override DateTime UnpackFromCore(Unpacker unpacker)
        {
            return DateTime.Parse(unpacker.LastReadData.AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
    }
}