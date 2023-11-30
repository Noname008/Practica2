using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Practica2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Button, Func<long>> algorithms;
        public MainWindow()
        {
            InitializeComponent();
            algorithms = new Dictionary<Button, Func<long>>()
            {
                { B1, Algoritm.SequentialAlg }
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

            Task.Run(algorithms[(Button)sender].Invoke).ContinueWith(t =>
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
        private static List<Number> buffer = new List<Number>();
        private static List<int> listprimes = new List<int>
        {
            2, 3, 5, 7
        };

        private static long BasicSequentialAlg(List<Number> numbers, params int[] primes)
        {
            foreach(var item in numbers)
            {
                if (primes.FirstOrDefault((x) =>
                    {
                        if (item.Value % x == 0)
                            return true;
                        return false;
                    }, 0)
                    != 0)
                    item.Check = false;
            }


            return 1;
        }

        public static long SequentialAlg()
        //test version for prime numbers 2,3,5,7 for the range of numbers from 2 to 100
        {
            buffer.AddRange(Number.GetNumbers(25, 200));
            BasicSequentialAlg(buffer, listprimes.ToArray());
            int i = 0;
            Debug.WriteLine(buffer[i].Value.ToString() + buffer[i].Check);
            return 1;
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
            return new Number[end - start].Select((t, i) =>
            {
                t = new Number(i + start);
                return t;
            });
        }
    }
}