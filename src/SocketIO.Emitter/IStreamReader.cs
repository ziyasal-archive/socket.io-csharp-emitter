using System.IO;

namespace SocketIO.Emitter
{
    internal interface IStreamReader
    {
        byte[] ReadToEnd(Stream stream);
    }
}