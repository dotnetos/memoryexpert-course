using System;
using BasePlugin;

namespace TestPlugin
{
    public class TestCommand : ICommand
    {
        private static InternalIdentifier s_identifier;
        static TestCommand()
        {
            s_identifier = new InternalIdentifier()
            {
                Created = DateTime.Now
            };
        }

        public string Name => "test";
        public string Description => "Dummy command for testing purpose.";
        public string Execute()
        {
            Console.WriteLine($"Hello from plugin {s_identifier}!");
            return "Hello world!";
        }
    }

    internal class InternalIdentifier
    {
        public DateTime Created { get; set; }
        public override string ToString()
            => $"id_TestCommand_{Created:O}";
    }
}
