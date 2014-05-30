using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SocketIO.Emitter.Tests
{
    public class EmitterTests : TestBase
    {
        private IEmitter _emitter;
        private Process _redisCli;
        private Process _redisServer;

        const string REDIS_SERVER_PATH = @"packages/Redis-64.2.8.4/redis-server.exe";
        const string REDIS_REDIS_CLI_EXE_PATH = @"packages/Redis-64.2.8.4/redis-cli.exe";

        private string _srcFolderPath;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _srcFolderPath = baseDirectory.Replace(@"\SocketIO.Emitter.Tests\bin\Debug", "");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_srcFolderPath, REDIS_SERVER_PATH),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            _redisServer = Process.Start(startInfo);
        }

        protected override void FinalizeSetUp()
        {
            _emitter = new Emitter(new EmitterOptions
            {
                Host = "localhost",
                Port = 6379
            });

            var processStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_srcFolderPath, REDIS_REDIS_CLI_EXE_PATH),
                Arguments = "MONITOR",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            _redisCli = Process.Start(processStartInfo);
        }

        [Test]
        public void Should_be_able_to_emit_messages_to_client()
        {
            _emitter.Emit("broadcast event", "Hello from socket.io-emitter");
            Task<string> readToEnd = _redisCli.StandardOutput.ReadToEndAsync();
            _redisCli.Kill();
            string log = readToEnd.Result;
            log.Contains("PUBLISH").Should().BeTrue();
        }

        [Test]
        public void Test_Publish_Contains_Expected_Attributes()
        {
            _emitter.Emit("hello", "Hello Socket.io");
            Task<string> readToEnd = _redisCli.StandardOutput.ReadToEndAsync();

            readToEnd.Wait(1000);

            _redisCli.Kill();

            string log = readToEnd.Result;
            log.Contains("PUBLISH").Should().BeTrue();
            log.Contains("hello").Should().BeTrue();
            log.Contains("Hello Socket.io").Should().BeTrue();
            log.Contains("rooms").Should().BeTrue();
            log.Contains("flags").Should().BeTrue();

            log.Contains("broadcast").Should().BeFalse();
            log.Contains("/").Should().BeFalse();
        }

        [Test]
        public void Test_Publish_Contains_Expected_Data_When_Emitting_Binary()
        {
            //TODO:
        }

        [Test]
        public void Test_Publish_Contains_Expected_Data_When_Emitting_Binary_With_Wrapper()
        {
            //TODO:
        }

        [Test]
        public void Test_Publish_Contains_Namespace_When_Emitting_With_Namespace_Set()
        {
            _emitter.Of("/nsp").Emit("broadcast event", "Hello from socket.io-emitter");
            Task<string> readToEnd = _redisCli.StandardOutput.ReadToEndAsync();

            readToEnd.Wait(1000);

            _redisCli.Kill();

            string log = readToEnd.Result;
            log.Contains("/nsp").Should().BeTrue();
        }

        [Test]
        public void Test_Publish_Contains_Room_Name_When_Emitting_With_Room_Name_Set()
        {
            _emitter.In("hello-room").Emit("broadcast event", "Hello from socket.io-emitter");
            Task<string> readToEnd = _redisCli.StandardOutput.ReadToEndAsync();

            readToEnd.Wait(1000);

            _redisCli.Kill();

            string log = readToEnd.Result;
            log.Contains("hello-room").Should().BeTrue();
        }

        protected override void FinalizeTearDown()
        {
            if (!_redisCli.HasExited)
                _redisCli.Kill();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (!_redisServer.HasExited)
                _redisServer.Kill();
        }
    }
}
