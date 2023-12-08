using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Practica2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Button, Action<int, int>> algorithms;
        public MainWindow()
        {
            InitializeComponent();
            algorithms = new Dictionary<Button, Action<int, int>>()
            {
                { B1, Algoritm.AlgDataDecomposition },
                { B2, Algoritm.AlgPrimesDecomposition },
                { B3, Algoritm.AlgThreadPool },
                { B4, Algoritm.AlgSequentialSearch },
            };
            foreach (var item in algorithms.Keys)
            {
                item.Click += BasicLaunch;
            }
        }

        private void BasicLaunch(object sender, RoutedEventArgs e)
        {
            Load.Visibility = Visibility.Visible;
            Content.Opacity = 0.3;
            Content.IsEnabled = false;

            if (Int32.TryParse(Threads.Text, out int threads) && Int32.TryParse(MaxValue.Text, out int maxValue))
                Task.Run(() => Algoritm.TimeToAlg(algorithms[(Button)sender], threads, maxValue)).ContinueWith(t =>
                {
                    Load.Visibility = Visibility.Hidden;
                    Content.Opacity = 1;
                    Content.IsEnabled = true;
                    MessageBox.Show(t.Result.ToString());
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public static class Algoritm
    {
        private static List<Number> buffer = [];
        private static List<int> listprimes = [2, 3, 5, 7];

        private static void AddPrimes()
        {
            listprimes.AddRange(buffer
                .Where((x) => x.Check == true)
                .Select((x) => x.Value)
                );
        }

        private static void BasicSequentialAlg(IEnumerable<Number> numbers, params int[] primes)
        {
            foreach(var item in numbers)
            {
                if (item.Check && primes.FirstOrDefault((x) => item.Value % x == 0, 0) != 0)
                    item.Check = false;
            }
        }

        private static void Clear()
        {
            buffer.Clear();
            listprimes = [2, 3, 5, 7];
        }

        private static IEnumerable<IEnumerable<T>> Split<T>(IEnumerable<T> source, int count)
        {
            return source
              .Select((x, y) => new { Index = y, Value = x })
              .GroupBy(x => x.Index % count)
              .Select(x => x.Select(y => y.Value).ToList())
              .ToList();
        }

        public static long TimeToAlg(Action<int, int> f, int threads, int maxValue)
        {
            Stopwatch sw = Stopwatch.StartNew();
            f.Invoke(threads, maxValue);
            sw.Stop();
            //foreach(var item in listprimes)
            //    Debug.WriteLine(item);
            Clear();
            return sw.ElapsedMilliseconds;
        }

        public static void AlgDataDecomposition(int threads, int maxValue)
        {
            int i;
            do
            {
                i = (int)(Math.Pow(listprimes.Last(), 2) < maxValue ? Math.Pow(listprimes.Last(), 2) : maxValue);
                buffer = new List<Number>(Number.GetNumbers(listprimes.Last(), i));
                var list = Split(buffer, threads);
                int validThreads = list.ToArray().Length > threads ? threads : list.ToArray().Length;
                Parallel.For(0, validThreads, (x) => BasicSequentialAlg(
                    list.ToArray()[x], 
                    [.. listprimes]));
                AddPrimes();
            } while (i < maxValue);
        }

        public static void AlgPrimesDecomposition(int threads, int maxValue)
        {
            int validThreads;
            int i;
            do
            {
                validThreads = listprimes.Count > threads ? threads : listprimes.Count;
                i = (int)(Math.Pow(listprimes.Last(), 2) < maxValue ? Math.Pow(listprimes.Last(), 2) : maxValue);
                buffer = new List<Number>(Number.GetNumbers(listprimes.Last(), i));
                var list = Split(listprimes.ToArray(), validThreads);
                Parallel.For(0, validThreads, x => BasicSequentialAlg(
                    buffer,
                    list.ToArray()[x].ToArray()));
                AddPrimes();
            } while (i < maxValue);
        }

        public static void AlgThreadPool(int threads, int maxValue)
        {
            int i;
            object tasksLock = new();
            int tasks = 0;
            do
            {
                i = (int)(Math.Pow(listprimes.Last(), 2) < maxValue ? Math.Pow(listprimes.Last(), 2) : maxValue);
                buffer = new List<Number>(Number.GetNumbers(listprimes.Last(), i));
                foreach(int prime in listprimes)
                {
                    while (tasks >= threads) { }
                    lock (tasksLock)
                    {
                        tasks++;
                    }
                    Task.Run(() => BasicSequentialAlg(buffer, prime)).ContinueWith((t) =>
                    {
                        lock(tasksLock)
                        {
                            tasks--;
                        }
                    });
                }
                while (tasks > 0) { }
                AddPrimes();
            } while (i < maxValue);
        }

        public static void AlgSequentialSearch(int threads, int maxValue)
        {
            int i;
            object primeLock = new();
            int indexPrime = 0;
            do
            {
                i = (int)(Math.Pow(listprimes.Last(), 2) < maxValue ? Math.Pow(listprimes.Last(), 2) : maxValue);
                buffer = new List<Number>(Number.GetNumbers(listprimes.Last(), i));
                Parallel.For(0, threads, x =>
                {
                    int thisPrime;
                    while(indexPrime < listprimes.Count)
                    {
                        lock (primeLock)
                        {
                            thisPrime = indexPrime++;
                        }
                        BasicSequentialAlg(buffer, listprimes[thisPrime]);
                    }
                });
                AddPrimes();
                indexPrime = 0;
            } while (i < maxValue);
        }
    }

    public class Number
    {
        public int Value { get; }
        public bool Check { get; set; } = true;

        private Number(int value)
        {
            Value = value;
        }

        public static IEnumerable<Number> GetNumbers(int start, int end)
        {
            return new Number[end - start].Select((t, i) => t = new Number(i + start));
        }
    }
}