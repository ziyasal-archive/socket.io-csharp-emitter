using NUnit.Framework;

namespace SocketIO.Emitter.Tests
{
    public class EmitterTests : TestBase
    {
        private IEmitter _emitter;
        protected override void FinalizeSetUp()
        {
            _emitter = new Emitter(null, new EmitterOptions
            {
                Host = "localhost",
                Port = 6379
            });
        }

        [Test]
        public void Should_be_able_to_emit_messages_to_client()
        {
            _emitter.Emit("broadcast event", "Hello from emitter");
        }
    }
}
