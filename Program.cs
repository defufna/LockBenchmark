using System.Diagnostics;
using VeloxDB.Common;

internal class Program
{
	static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

	static ParallelResizeCounter counter = new ParallelResizeCounter(int.MaxValue);
	static readonly double time = 30;

	static object totalLock = new object();

	private static long total;
	static int threadNum = NativeProcessorInfo.LogicalCoreCount;

	private static void TakeReadLockMS()
	{
		long count = 0;
		Stopwatch sw = Stopwatch.StartNew();

		long timeMS = (long)(time * 1000);
		while(sw.ElapsedMilliseconds < timeMS)
		{
			for (int i = 0; i < 100; i++)
			{
				rwLock.EnterReadLock();
				rwLock.ExitReadLock();

				count++;
			}
		}

		lock(totalLock)
		{
			total += count;
		}
	}

	private static void TakeReadLockVLX()
	{
		long count = 0;
		Stopwatch sw = Stopwatch.StartNew();

		long timeMS = (long)(time * 1000);
		while(sw.ElapsedMilliseconds < timeMS)
		{
			for (int i = 0; i < 100; i++)
			{
				int handle = counter.EnterReadLock();
				counter.ExitReadLock(handle);

				count++;
			}
		}

		lock(totalLock)
		{
			total += count;
		}
	}

	private static void TakeWriteLockMS()
	{
		long count = 0;
		Stopwatch sw = Stopwatch.StartNew();

		long timeMS = (long)(time * 1000);
		while(sw.ElapsedMilliseconds < timeMS)
		{
			for (int i = 0; i < 100; i++)
			{
				rwLock.EnterWriteLock();
				rwLock.ExitWriteLock();

				count++;
			}
		}

		lock(totalLock)
		{
			total += count;
		}
	}

	private static void TakeWriteLockVLX()
	{
		long count = 0;
		Stopwatch sw = Stopwatch.StartNew();

		long timeMS = (long)(time * 1000);
		while(sw.ElapsedMilliseconds < timeMS)
		{
			for (int i = 0; i < 100; i++)
			{
				counter.EnterWriteLock();
				counter.ExitWriteLock();

				count++;
			}
		}

		lock(totalLock)
		{
			total += count;
		}
	}

	private static void Main(string[] args)
	{
		ThreadStart[] methods = new ThreadStart[] {
			TakeReadLockMS, TakeReadLockVLX, TakeWriteLockMS, TakeWriteLockVLX
		};

		int[] threads = new int[] { 1, 2, 4, 8, 12, 16, 24, 32 };


		foreach (var method in methods)
		{
			foreach (int threadNum in threads)
			{
				Program.threadNum = threadNum;
				// Warmup
				Benchmark(method);

				List<long> results = new List<long>();

				for (int i = 0; i < 4; i++)
				{
					results.Add(Benchmark(method));
				}

				Console.WriteLine($"{method.Method.Name}[{threadNum}]: {string.Join(", ", results.Select((s) => s.ToString()))}");
			}
		}
	}

	private static long Benchmark(ThreadStart method)
	{
		total = 0;
		Thread[] threads = new Thread[threadNum];

		for (int i = 0; i < threads.Length; i++)
		{
			threads[i] = new Thread(method);
			threads[i].Start();
		}

		for (int i = 0; i < threads.Length; i++)
		{
			threads[i].Join();
		}

		return total;
	}
}