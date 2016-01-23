namespace SocketIO.Emitter
{
    public class EmitterOptions
    {
        public int Port { get; set; }
        public string Host { get; set; }
        public string Key { get; set; }
		public EVersion Version = EVersion.V0_9_9;
		
		public enum  EVersion { V0_9_9 , V1_4_4 };
	}
}