using System;

namespace BasePlugin
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }

        string Execute();
    }
}
