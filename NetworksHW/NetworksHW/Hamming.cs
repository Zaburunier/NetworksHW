using System;
using System.IO;
using System.Text;

namespace NetworksHW {
    class Hamming {
        private int[] codeVector;
        private FileStream infoFile;
        private FileStream resultFile;
        public Hamming(FileStream infoFile, FileStream resultFile) {
            int[] infCodeVector = new int[11];
            Console.Write("Введите информационный вектор: ");
            for (int i = infCodeVector.Length - 1; i >= 0; i = i - 1) {
                infCodeVector[i] = Convert.ToInt32(Console.Read() - 48);
            }
            codeVector = new int[15];
            this.infoFile = infoFile;
            this.resultFile = resultFile;
            WriteIntoFile("Получен информационный вектор " + this.ToString(infCodeVector), resultFile);
            Executing(infCodeVector);
        }

        /// <summary>
        /// Основной метод для проведения операций с кодом Хэмминга
        /// </summary>
        /// <param name="data"></param>
        private void Executing(int[] data) {
            // Преобразуем в кодовый вектор
            DataToEncoded(data);
            WriteIntoFile("ПОЛУЧЕН КОДОВЫЙ ВЕКТОР" + this.ToString(codeVector), infoFile);
            WriteIntoFile("\nПолучен кодовый вектор " + this.ToString(codeVector), resultFile);
            ErrorVector error = new ErrorVector(infoFile, resultFile);

            // Кол-во итераций на текущей кратности
            int iterationCounter = 0;
            // Обнаружено на текущей кратности
            int detectionCounter = 0;
            // Кол-во итераций всего
            int totalIterCounter = 0;
            // Обнаружено всего
            int totalDetCounter = 0;

            // Перебираем все вектора ошибки 
            foreach (int[] err in error.GetVectors()) {
                totalIterCounter = totalIterCounter + 1;
                // Итератор выбрасывает нулевой вектор при переходе на новую кратность в качестве сигнала
                if (IsNewRate(err)) {
                    // Записываем в файлы промежуточные итоги и обновляем счётчики
                    if (error.CurrentErrorRate != 1) {
                        WriteIntoFile("\n\tОБНАРУЖЕНО " + detectionCounter.ToString() + " ОШИБОК.", infoFile);
                        double detectionRate = (double)detectionCounter / iterationCounter;
                        WriteIntoFile("\n\tВсего " + iterationCounter + " комбинаций" + "\n\tНайдено " + detectionCounter + " ошибок" + "\n\tОбнаруживающая способность = " + System.Math.Round(detectionRate, 3), resultFile);
                    }
                    WriteIntoFile("\nДля кратности " + error.CurrentErrorRate, resultFile);
                    WriteIntoFile("\nПРОИЗВОДИМ РАСЧЁТ ДЛЯ КРАТНОСТИ " + error.CurrentErrorRate, infoFile);
                    totalDetCounter = totalDetCounter + detectionCounter;
                    iterationCounter = 0;
                    detectionCounter = 0;
                } else {
                    iterationCounter = iterationCounter + 1;

                    // Моделируем канал связи и записываем в файл итоги работы
                    WriteIntoFile("\n\tДля вектора ошибки " + this.ToString(err) + ":", infoFile);
                    // Получаемый искажённый вектор
                    int[] distortedCodeVector = ApplyError(err);
                    WriteIntoFile("\n\t\tВектор с наложенной ошибкой: " + this.ToString(distortedCodeVector), infoFile);
                    // Работаем с искажённым вектором
                    int syndromeBit;
                    ObtainedToDecoded(ref distortedCodeVector, out syndromeBit);

                    if (syndromeBit != 0) {
                        WriteIntoFile("\n\t\tОбнаружена ошибка в " + syndromeBit.ToString() + " бите." + "\n\t\tИсправленный вектор: " + this.ToString(distortedCodeVector), infoFile);
                        detectionCounter = detectionCounter + 1;
                    } else {
                        WriteIntoFile("\n\t\tОшибка не обнаружена", infoFile);
                    }
                }

               
            }
            WriteIntoFile("\nВСЕГО ОБНАРУЖЕНО " + totalDetCounter.ToString() + " ОШИБОК.", infoFile);
        }

        /// <summary>
        /// Преобразуем полученную информацию в кодовый вектор
        /// </summary>
        /// <param name="data">Исходный вектор</param>
        private void DataToEncoded(int[] data) {
            // Получив информационный вектор, необходимо заполнить все поля кодового

            // Информационные поля...
            codeVector[2] = data[0];
            codeVector[4] = data[1];
            codeVector[5] = data[2];
            codeVector[6] = data[3];
            codeVector[8] = data[4];
            codeVector[9] = data[5];
            codeVector[10] = data[6];
            codeVector[11] = data[7];
            codeVector[12] = data[8];
            codeVector[13] = data[9];
            codeVector[14] = data[10];

            // Проверочные поля...
            int[] checkingBits = PartialSums(codeVector, false);
            codeVector[0] = checkingBits[0];
            codeVector[1] = checkingBits[1];
            codeVector[3] = checkingBits[2];
            codeVector[7] = checkingBits[3];
        }

        /// <summary>
        /// Наложение на кодовый вектор текущего значения вектора ошибки
        /// </summary>
        /// <param name="errorVector">Вектор ошибки</param>
        /// <returns>Искажённый вектор</returns>
        private int[] ApplyError(int[] errorVector) {
            int[] distorted = new int[15];
            // Не изменяем исходный вектор, чтобы потом проделать ту же операцию на следующих итерациях
            codeVector.CopyTo(distorted, 0);

            for (int i = 0; i < errorVector.Length; i = i + 1) {
                // Если текущий бит ошибки равен 1, то инвертируем бит кода
                if (errorVector[i] == 1) {
                    if (distorted[i] == 1) {
                        distorted[i] = 0;
                    } else {
                        distorted[i] = 1;
                    }
                }
            }

            return distorted;
        }

        /// <summary>
        /// Вычисление синдрома ошибки и инвертирование бита по найденному порядковому номеру
        /// </summary>
        /// <param name="vector">Искажённый вектор</param>
        /// <param name="bitNumber">Бит, на который указывает синдром ошибки</param>
        private void ObtainedToDecoded(ref int[] vector, out int bitNumber) {
            // Определяем синдром ошибки
            int[] errorSyndrome = PartialSums(vector, true);
            // Получаем бит синдрома ошибки
            string syndromeStr = "";
            foreach (int bit in errorSyndrome) {
                syndromeStr = bit.ToString() + syndromeStr;
            }
            bitNumber = Convert.ToInt32(syndromeStr, 2);

            if (bitNumber != 0) {
                if (vector[bitNumber - 1] == 1) {
                    vector[bitNumber - 1] = 0;
                } else {
                    vector[bitNumber - 1] = 1;
                }
            }
        }

        /// <summary>
        /// Получение проверочных битов с помощью сложения по модулю 2
        /// </summary>
        /// <param name="vector">Кодовый вектор, из которого подсчитываем контрольные суммы</param>
        /// <param name="isDecoding">Данный параметр определяет, будут ли для подсчёта контрольных сумм использоваться проверочные биты</param>
        /// <returns>Массив проверочных битов</returns>
        private int[] PartialSums(int[] vector, bool isDecoding) {
            int[] values = new int[4] { 0, 0, 0, 0 };
            // Получаем сумму из информационных битов
            values[0] = vector[2] + vector[4] + vector[6] + vector[8] 
                      + vector[10] + vector[12] + vector[14];
            values[1] = vector[2] + vector[5] + vector[6] + vector[9]
                      + vector[10] + vector[13] + vector[14];
            values[2] = vector[4] + vector[5] + vector[6] + vector[11]
                      + vector[12] + vector[13] + vector[14];
            values[3] = vector[8] + vector[9] + vector[10] + vector[11]
                      + vector[12] + vector[13] + vector[14];

            // При подсчёте синдрома ошибки так же добавляем проверочные биты
            if (isDecoding) {
                values[0] = values[0] + vector[0];
                values[1] = values[1] + vector[1];
                values[2] = values[2] + vector[3];
                values[3] = values[3] + vector[7];
            }

            // Отрезаем сумму по модулю 2
            for (int i = 0; i < values.Length; i = i + 1) {
                values[i] = values[i] % 2;
            }

            return values;
        }

        /// <summary>
        /// Запись текстовой информации в файл
        /// </summary>
        /// <param name="file">Файл, в который заносим строку</param>
        /// <param name="strToWrite">Строка, которую заносим в файл</param>
        private void WriteIntoFile(string strToWrite, FileStream file) {
            byte[] written = new UTF8Encoding().GetBytes(strToWrite);
            file.Write(written, 0, written.Length);
        }

        /// <summary>
        /// Метод определяет, исчерпал ли итератор варианты вектора ошибок данной кратности. 
        /// Это позволяется из-за того, что в качестве сигнала перед переходом на следующую кратность итератор выбрасывает нулевой вектор
        /// </summary>
        /// <param name="errorVector">Полученное значение вектора ошибки</param>
        /// <returns>Произошёл ли переход на новый показатель кратности ошибки?</returns>
        private bool IsNewRate(int[] errorVector) {
            for (int i = 0; i < errorVector.Length; i = i + 1) {
                if (errorVector[i] != 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Текстовое представление битового массива для вывода в файл
        /// </summary>
        /// <param name="intArray">Массив нулей и единиц</param>
        /// <returns>Строка, соответствующая полученному массиву</returns>
        public string ToString(int[] intArray) {
            string s = "";
            for (int i = intArray.Length - 1; i >= 0; i = i - 1) {
                s = s + intArray[i].ToString();
            }
            return s;
        }

    }
}
