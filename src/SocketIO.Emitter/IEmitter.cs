using System.Threading.Tasks;

namespace SocketIO.Emitter
{
    public interface IEmitter
    {
        IEmitter In(string room);
        IEmitter To(string room);
        IEmitter Of(string nsp);

        IEmitter Emit(params object[] args);
        IEmitter Emit<T>(string eventName, T arg);

        Task<IEmitter> EmitAsync(params object[] args);
        Task<IEmitter> EmitAsync<T>(string eventName, T arg);
    }
}