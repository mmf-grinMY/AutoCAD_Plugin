using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

internal class Program
{
    private static readonly string readPath = @"C:\Users\grinm\Desktop\Test.txt";
    private static readonly string writePath = @"C:\Users\grinm\Desktop\Write.txt";
    public async static Task Main(string[] args)
    {
        var sr = new StreamReader(readPath);
        var sw = new StreamWriter(writePath);

        RunConveyor<string>(sr.ReadLine, sw.WriteLine);

        sr.Close();
        sw.Close();

        await Console.Out.WriteLineAsync("Конец записи файла!");

        Console.Read();
    }
    private static void RunConveyor<T>(Func<T> readAction, Action<T> writeAction, int bufferSize = 50)
    {
        Thread read = null;
        Thread write = null;
        List<T> list = new List<T>();
        object locker = new object();
        int maxLength = bufferSize;

        read = new Thread(() =>
        {
            while (read.IsAlive)
            {
                if (list.Count < maxLength)
                {
                    lock (locker)
                    {
                        T line = readAction();
                        if (line != null)
                        {
                            list.Add(line);
                        }
                        else
                        {
                            read.Abort();
                        }
                    }
                }
            }
        });

        write = new Thread(() =>
        {
            while (write.IsAlive)
            {
                lock (locker)
                {
                    if (list.Count > 0)
                    {
                        T item = list[0];
                        list.RemoveAt(0);
                        writeAction(item);
                    }
                    else if (!read.IsAlive)
                    {
                        write.Abort();
                    }
                }
            }
        });

        read.Start();
        write.Start();
        read.Join();
        write.Join();
    }
}