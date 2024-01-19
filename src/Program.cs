
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
            string arg = args.Count() > 0 ? args[0] : "";
            Task task = KeepixPlugin.Run(arg, Assembly.GetExecutingAssembly().GetTypes());
            task.Wait();
        }
    }
}