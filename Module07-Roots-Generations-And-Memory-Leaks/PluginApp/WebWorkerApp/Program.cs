using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using BasePlugin;

namespace WebWorkerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var job = new SampleJob();

            foreach (var command in LoadPluginCommands(@"plugins\test\net5.0\TestPlugin.dll"))
            {
                var result = command.Execute();
                Console.WriteLine(result);
            }

            foreach (var command in LoadPluginCommands(@"plugins\test\net5.0\TestPlugin.dll"))
            {
                var result = command.Execute();
                Console.WriteLine(result);
            }

            Console.WriteLine("Processing...");
            Console.ReadKey();
        }

        static IEnumerable<ICommand> LoadPluginCommands(string path)
        {
            var programPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var absolutePath = Path.Combine(programPath, path);
            var pluginContext = new PluginAssemblyLoadContext(absolutePath);

            var assembly = pluginContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(absolutePath));
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    var result = Activator.CreateInstance(type) as ICommand;
                    yield return result;
                }
            }
        }
    }

    public class SampleJob
    {
        public SampleJob()
        {
            Timer timer = new Timer(HandleTick);
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void HandleTick(object state)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Doing...");
        }
    }

    class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginAssemblyLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
    }
}
