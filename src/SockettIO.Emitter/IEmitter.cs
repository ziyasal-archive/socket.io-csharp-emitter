namespace SockettIO.Emitter
{
    public interface IEmitter
    {
        IEmitter In(string room);
        IEmitter To(string room);
        IEmitter Of(string nsp);

        IEmitter Emit(params object[] args);
    }
}