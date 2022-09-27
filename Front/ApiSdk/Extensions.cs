using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetMQ;

namespace ApiSdk
{
    public static class Extensions
    {
        public static IServiceCollection RegisterApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiConfiguration>(configuration.GetSection("ApiClient"));
            services.AddSingleton<SimApiClient>();
            services.AddSingleton<SimAuthenticationManager>();

            return services;
        }

        public static bool StartsWith(this int num, int starts) => num.ToString().StartsWith(starts.ToString());

        public static ValueTask<NetMQMessage> ReceiveMultipartMessageAsync(this IThreadSafeSocket socket, int expectedFrameCount = 4)
        {
            return new ValueTask<NetMQMessage>(Task.Factory.StartNew(() =>
            {
                var msg = new Msg();
                msg.InitEmpty();

                var message = new NetMQMessage(expectedFrameCount);

                do
                {
                    socket.Receive(ref msg);
                    message.Append(msg.CloneData());
                } while (msg.HasMore);

                msg.Close();
                return message;
            }, TaskCreationOptions.LongRunning));
        }

        private static void SendMessage(NetMQMessage message, IThreadSafeSocket socket)
        {
            if (message.FrameCount == 0)
                throw new ArgumentException("message is empty", nameof(message));

            for (var i = 0; i < message.FrameCount - 1; i++)
            {
                var msg = new Msg();
                PopulateMessage(ref msg, message[i].Buffer, message[i].MessageSize, true);
                socket.Send(ref msg);
                msg.Close();
            }

            var lastMsg = new Msg();
            PopulateMessage(ref lastMsg, message.Last.Buffer, message.Last.MessageSize);
            socket.Send(ref lastMsg);
            lastMsg.Close();
        }

        private static void PopulateMessage(ref Msg msg, byte[] data, int length, bool more = false)
        {
            msg.InitPool(length);
            data.AsSpan().Slice(0, length).CopyTo(msg);
            if (more)
            {
                msg.SetFlags(MsgFlags.More);
            }
        }

        /// <summary>
        /// Transmit a string over this socket asynchronously.
        /// </summary>
        /// <param name="socket">the socket to transmit on</param>
        /// <param name="message">the string to send</param>
        public static ValueTask SendAsync(this IThreadSafeOutSocket socket, NetMQMessage message)
        {
            return new ValueTask(Task.Factory.StartNew(() => SendMessage(message, socket), TaskCreationOptions.LongRunning));
        }
    }
}