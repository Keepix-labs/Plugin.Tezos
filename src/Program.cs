
using Keepix.PluginSystem;
using Plugin.Tezos.Commands;
using Plugin.Tezos.src.Commands;
using Plugin.Tezos.src.Services;
using System.Reflection;

namespace Plugin.Tezos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // todo : 
            new Task(async () =>
            {
                await Setup.OnInstall();
            }).Start();

            Console.ReadLine();
        }
    }
}