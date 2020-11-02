using System;
using System.Threading;

namespace LogBufferTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var logBuffer = new LogBuffer();
            do
            {
                Console.WriteLine("--------------------------------------------------------------" +
                                  Environment.NewLine +
                                  "Выберите дейтвие:" + Environment.NewLine +
                                  "1) Записать в буфер текст несколько раз" + Environment.NewLine +
                                  "2) Записать асинхронно в буфер текст  несколько раз" + Environment.NewLine +
                                  "3) Остановить/Запустить автоматическую запись на диск" + Environment.NewLine +
                                  "4) Изменить задержку между записями на диск" + Environment.NewLine +
                                  "5) Произвести принудительную запись на диск" + Environment.NewLine +
                                  "6) Произвести асинхронно принудительную запись на диск" + Environment.NewLine +
                                  "7) Изменить максимальное количество сообщений до записи на диск" +
                                  Environment.NewLine +
                                  "8) Отобразить все записанные на диск сообщения" + Environment.NewLine +
                                  "9) Отобразить все сообщение в очереди на запись" + Environment.NewLine +
                                  "10) Отобразить все сообщения" + Environment.NewLine +
                                  "0) Выход" + Environment.NewLine +
                                  "---------------------------------------------------------------");
                Console.Write("Ваш выбор: ");
                var answer = ParseAnswer(Console.ReadLine());
                int value;
                switch (answer)
                {
                    case 1:
                        Console.Write("Сколько сообщений записать?: ");
                        do
                        {
                            value = ParseAnswer(Console.ReadLine());
                            if (value <= 0)
                            {
                                Console.Write("Некорректный ввод, повторите: ");
                            }
                            else
                            {
                                Console.WriteLine("Вводите сообщения: ");
                                for (var i = 0; i < value; i++)
                                {
                                    Console.Write($"[{i + 1}]: ");
                                    logBuffer.Add(Console.ReadLine());
                                }
                            }
                        } while (value <= 0);

                        break;
                    case 2:
                        Console.Write("Сколько сообщений записать?: ");
                        do
                        {
                            value = ParseAnswer(Console.ReadLine());
                            if (value <= 0)
                            {
                                Console.Write("Некорректный ввод, повторите: ");
                            }
                            else
                            {
                                Console.WriteLine("Вводите сообщения: ");
                                for (var i = 0; i < value; i++)
                                {
                                    Console.Write($"[{i + 1}]: ");
                                    logBuffer.AddAsync(Console.ReadLine());
                                }
                            }
                        } while (value <= 0);

                        break;
                    case 3:
                        logBuffer.IsLogging = !logBuffer.IsLogging;
                        Console.WriteLine("Теперь автоматическая запись {0}",
                            logBuffer.IsLogging ? "включена" : "выключена");
                        break;
                    case 4:
                        Console.Write("Введите новую задержку в мс: ");
                        do
                        {
                            value = ParseAnswer(Console.ReadLine());
                            if (value < 0)
                            {
                                Console.Write("Некорректный ввод, повторите: ");
                            }
                            else
                            {
                                logBuffer.Delay = value;
                            }
                        } while (value <= 0);

                        break;
                    case 5:
                        logBuffer.Flush();
                        Console.WriteLine("Запись на диск была произведена");
                        break;
                    case 6:
                        logBuffer.FlushAsync();
                        Console.WriteLine("Запись на диск была произведена");
                        break;
                    case 7:
                        Console.Write("Изменить максимальное количество сообщений до записи на диск на: ");
                        do
                        {
                            value = ParseAnswer(Console.ReadLine());
                            if (value < 0)
                            {
                                Console.WriteLine("Некорректный ввод, повторите:");
                            }
                            else
                            {
                                logBuffer.MessageLimit = value;
                            }
                        } while (value <= 0);

                        break;
                    case 8:
                        Console.WriteLine(logBuffer.DisplayJournal());
                        break;
                    case 9:
                        Console.WriteLine(logBuffer.DisplayQueue());
                        break;
                    case 10:
                        Console.WriteLine(logBuffer.DisplayLog());
                        break;
                    case 0:
                        logBuffer.Dispose();
                        return;
                }
            } while (true);
        }

        private static int ParseAnswer(string text)
        {
            try
            {
                return int.Parse(text.Trim());
            }
            catch
            {
                return -1;
            }
        }
    }
}