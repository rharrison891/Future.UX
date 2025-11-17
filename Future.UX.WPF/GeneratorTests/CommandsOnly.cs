using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Future.UX.WPF.GeneratorTests
{
    public partial class CommandsOnly
    {
        private Task __Stop()
        {
            Console.WriteLine($"Running execution code with parameter [NONE]");
            return Task.CompletedTask;
        }

        private async Task __Play(string? something)
        {
            await Task.Delay(1); // simulate async
            Console.WriteLine($"Play command executed with {something}");
        }

        private void __Pause(int duration)
        {
            Console.WriteLine($"Pausing for {duration} seconds");
        }

        private async Task __Record(string filename)
        {
            await Task.Delay(1000);
            Console.WriteLine($"Recording to file {filename}");
        }
    }
}
