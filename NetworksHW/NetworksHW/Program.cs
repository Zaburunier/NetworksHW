using System;
using System.IO;

namespace NetworksHW {
    class Program {
        static void Main(string[] args) {
            
            Console.WriteLine("Забурунов Леонид, РТ5-51Б.\nДомашняя работа #2 по курсу \"Сети и телекоммуникации\"" + 
                "\nИтоги работы программы представлены в файле \"HammingInfo.txt\" в корневой папке проекта.");

            HammingProcessing();

            Console.ReadKey();
        }

        static void HammingProcessing() {
            FileStream information = CreatingInfoFile("HammingInfo.txt");
            FileStream result = CreatingInfoFile("HammingResult.txt");

            Hamming ham = new Hamming(information, result);
        }

        /// <summary>
        /// Создаём файл для записи в корне проекта
        /// </summary>
        /// <param name="filename">Имя файла</param>
        /// <returns>Объект для потокового ввода-вывода</returns>
        static FileStream CreatingInfoFile(string filename) {
            // Начальный путь (в каталоге с приложением); имя файла
            string path = filename;
            // Получаем абсолютный путь 
            path = Path.GetFullPath(path);
            // Поднимаеся в корень проекта
            for (int i = 0; i < 4; i = i + 1) {
                path = Path.GetDirectoryName(path);
            }
            // В итоге получили место и имя для создаваемого файла
            return File.Create(Path.Combine(path, filename));
        }
    }    
}