using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace NetworksHW {
    class ErrorVector {

        // Текущий вектор ошибки
        private int[] vector;
        // Для проверки существования вектора данной кратности в классе
        private bool initialized;
        // Текущее значение кратности
        private int errorRate;
        // Храним индексы единиц
        private List<int> ones;
        // Файл, в который будем записывать всю информацию (в консоль не помещается :) )
        private FileStream infoFile;
        // Файл, в который будем записывать результат
        private FileStream resultFile;

        /// <summary>
        /// Текущий вектор ошибки
        /// </summary>
        public int[] Vector { get { return this.vector; } }
        /// <summary>
        /// Текущее значение кратности ошибки
        /// </summary>
        public int CurrentErrorRate { get { return this.errorRate; } }
        public ErrorVector(FileStream infoFile, FileStream resultFile) {
            vector = new int[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            initialized = false;
            errorRate = 0;
            ones = new List<int>();
            this.infoFile = infoFile;
            this.resultFile = resultFile;
        }

        /// <summary>
        /// Итератор, перебирающий все комбинации векторов всех кратностей ошибок от 1 до 15 и выбрасывающий все эти комбинации.
        /// </summary>
        /// <returns>Текущий вектор</returns>
        public IEnumerable GetVectors() {

            while (true) {
                // Если мы ещё не создали вектор для текущей кратности
                if (!initialized) {
                    // Проверяем границы диапазона значений кратности
                    if (errorRate == 15) {
                        // Отправляем сигнал об окончании перебора векторов данной кратности и завершаем работу итератора
                        yield return new int[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        yield break;
                    } else {
                        // Переходим к новой кратности
                        CreateFirstVector();
                        // Оповещаем о том, что произошёл переход на новую кратность
                        yield return new int[15] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    }
                }
                // Итерируемся по текущему значению кратности, пока не дойдём до последнего
                // (если дошли до последнего, то функция проверки снимает флаг initialized)
                yield return vector;
                if (!CheckTheLatter()) {
                    MoveToNextVector();
                }
            }

        }

        /// <summary>
        /// Проверяем, является ли текущий вектор ошибки последним из возможных для данной кратности
        /// </summary>
        /// <returns>Является ли?</returns>
        private bool CheckTheLatter() {
            /* Последним вектором считается тот, 
             * который имеет в n старших разрядах вектора единицы, 
             * а во всех остальных - нули (n - кратность ошибки).*/

            int currentIndex = vector.Length - 1;
            while (currentIndex >= vector.Length - errorRate) {
                if (vector[currentIndex] == 0) {
                    return false;
                }
                currentIndex = currentIndex - 1;
            }
            initialized = false;
            ones.Clear();
            return true;
        }

        /// <summary>
        /// Создаём начальный вектор для нового значения кратности
        /// </summary>
        private void CreateFirstVector() {
            /* Первым вектором считается тот,
             * который имеет в n младших разрядах единицы, 
             * а во всех остальных - нули (n - кратность ошибки)
             */

            errorRate = errorRate + 1;
            initialized = true;
            // Заносим начальный вектор
            for (int i = 0; i < vector.Length; i = i + 1) {
                if (i < errorRate) {
                    ones.Add(i);
                    vector[i] = 1;
                } else {
                    vector[i] = 0;
                }
            }
        }

        /// <summary>
        /// Переходим к следующему вектору данной кратности
        /// </summary>
        private void MoveToNextVector() {
            if (ones[errorRate - 1] != vector.Length - 1) {
                // Если доступна, то просто сдвигаем её
                MoveOneToNext(errorRate - 1);
            } else {
                // Если же недоступна, то необходимо перегруппировать единицы в векторе
                // Определяем первый доступный для сдвига уровень
                int currentLevel = errorRate - 1;
                while (currentLevel > 0) {
                    currentLevel = currentLevel - 1;
                    if (vector[ones[currentLevel] + 1] == 0 && vector[ones[currentLevel]] == 1) { // Второе условие добавлено исключительно для читаемости кода
                        break;
                    }
                }

                MoveOnesToNextLayout(currentLevel);
            }

        }

        /// <summary>
        /// Сдвиг одной единицы на одну позицию влева
        /// (с изменением индекса внутри массива единиц)
        /// </summary>
        /// <param name="oneLevel">Текущее положение сдвигаемой единички</param>
        private void MoveOneToNext(int oneLevel) {
            vector[ones[oneLevel]] = 0;
            vector[ones[oneLevel] + 1] = 1;
            ones[oneLevel] = ones[oneLevel] + 1;
        }

        /// <summary>
        /// Установка всех упёршихся в край массива единиц в новое расположение
        /// (с изменениями индексов внутри массива единиц)
        /// </summary>
        /// <param name="oneLevel">
        /// Положение первой единицы, доступной для сдвига на 1
        /// (остальные будут передвинуты в соседние для этой единицы позиции)
        /// </param>
        private void MoveOnesToNextLayout(int oneLevel) {
            // Сдвинем предыдущую единицу вперёд
            vector[ones[oneLevel]] = 0;
            vector[ones[oneLevel] + 1] = 1;
            ones[oneLevel] = ones[oneLevel] + 1;

            // После сдвига пред. единицы начинаем возвращать по цепочке все упёршиеся
            int currentIndex = ones[oneLevel] + 1,
                currentLevel = oneLevel + 1;
            while (currentLevel < errorRate) {
                vector[ones[currentLevel]] = 0;
                vector[currentIndex] = 1;
                ones[currentLevel] = currentIndex;
                currentLevel = currentLevel + 1;
                currentIndex = currentIndex + 1;
            }
        }

    }
}
