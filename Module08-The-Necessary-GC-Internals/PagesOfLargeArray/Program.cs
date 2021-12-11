using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PagesOfLargeArray
{
    class Program
    {
        static void Main(string[] args)
        {
            // Assume Background Workstation GC where LOH segment is 128MB
            Console.WriteLine($"ServerGC: {GCSettings.IsServerGC}, LatencyMode: {GCSettings.LatencyMode}");
            Continue();

            // Create two 32MB arrays.
            // With respect to what was said during lessons, they will be allocated in LOH and
            // **pre-zeroed**, because this is guaranteed from the runtime. It means that this memory
            // needs to be **committed** and **touched** (we write 0 for every array element).
            // Thus, we should expect private bytes AND working set to grow by those 64MB. 
            var array0 = new byte[32 * 1024 * 1024];
            var array1 = new byte[32 * 1024 * 1024];
            Continue();
            // Answer: as they are created in a LOH segment by commiting more memory it is zero-initialized
            // by operating system (Virtual Memory Manager) and this zeroing is "on the house":
            // - private bytes will grow by 64MB - those pages are really committed
            // - working set will not grow at all - we don't touch physical memory while "zeroing" because
            //     it is "zeroing| from the OS. As Pavel Yosifovich, one of our mentors says: "it's using
            //     the VirtualAlloc function behind the scenes, which guarantees you get zero pages when
            //      they're accessed, because the zero-page thread in the kernel prepares zero pages upfront.
            //      So, only when you make the access there is "demand zero" page fault handler that gives
            //      you the zero-pages and thus increases the working set."
            // In other words, and it turns out to be pretty surprising, although we really allocate and zero
            // those 64MB, thanks to the operating system support this memory is not physically consumed 
            // until we start to access them.

            // Get rid of array0 only, as array1 is rooted below
            GC.KeepAlive(array0);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Continue();
            // Answer: This does not decommit anything because there is a gap in LOH now. So both private
            // bytes and working set stays the same level.

            // Create a new 32MB array. It should fit into the gap left by array0. What's the change in
            // private bytes and working set? What do you think, why?!
            var array = new byte[32 * 1024 * 1024];
            Continue();
            // Answer: now, while allocating in LOH, GC finds a gap and to prepare it, clears it after
            // previous object by zero-initializing it (memclr in gc.cpp). This is different than taking
            // some pages from the OS at the end of the segment (like previously). Now it really touches
            // this memory region (a gap) physically so we see immediate increase of working set by 32MB.
            // Committed stays the same.

            // Start to touch array page by page. What's memory usage change?
            var offset = 0;
            var sum = 0L;
            while (offset < 32 * 1024 * 1024)
            {
                sum += array[offset];
                offset += 4096;
                Console.Write('.');
            }
            Continue();
            // Answer: This does not change both working set and committed  because we already touched all
            // region physically while zeroing.

            // Start to touch array1 page by page. What's memory usage change?
            var offset2 = 0;
            var sum2 = 0L;
            while (offset2 < 32 * 1024 * 1024)
            {
                sum2 += array1[offset2];
                offset2 += 4096;
                Console.Write('.');
            }
            Continue();
            // Answer: This does change working set because we touch "zero-pages".

            Console.WriteLine(sum + sum2);
        }

        static void Continue()
        {
            Console.WriteLine("Press any key to run...");
            Console.ReadKey();
        }
    }
}
