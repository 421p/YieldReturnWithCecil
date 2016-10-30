using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Should;
using TestProj;
using static Mono.Cecil.Cil.OpCodes;

namespace Program
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var definition = AssemblyDefinition.ReadAssembly("TestProj.dll");

            var testClassType = definition.MainModule.GetType(typeof (TestClass).FullName);

            // давайте проверим, есть ли у нашего класса вложенные типы
            testClassType.NestedTypes.Count.ShouldNotEqual(0);

            // и они есть, хоть мы их и не объявляли. очевидно что CLR нам что-то нагенерила,
            // посмотрим сколько их

            Console.WriteLine($"Нагенерено типов: {testClassType.NestedTypes.Count}"); //1
            Console.WriteLine();

            // посмотрим что за тип
            var nested = testClassType.NestedTypes.First();

            Console.WriteLine("Название типа: " + nested.Name);
            Console.WriteLine("Тип имплементит интерфейсы: ");
            nested.Interfaces.ToList().ForEach(x => Console.WriteLine(x.Name));
            Console.WriteLine();

            /* Видимо это и есть наш генератор-итератор-енумератор
             * который нам сгенерировала CLR на месте yield return
             * попробуем немножко с ним пообщаться.
             * Как мы знаем у енумератора есть Current property, а значит есть
             * и сгенерированный generic-метод get_Current
             * попробуем найти его
             */

            var getCurrentMethod = nested.Methods.First(x => x.Name.Contains("<System.String>.get_Current"));

            Console.WriteLine("Полное имя метода get_Current:");
            Console.WriteLine(getCurrentMethod.FullName);
            Console.WriteLine();

            /* Метод get_Current возвращает текущий элемент енумератора
             * который в нашем случае является строкой длинной в 1 символ
             * немного изменим его, что-бы он возвращал строку
             * состоящую из текущего символа и пробела.
             * Для этого используем метод String.Concat.
             * Создадим ссылку на метод String.Concat
             */
            var concatRef = definition.MainModule.Import(
                typeof (string).GetMethod("Concat", new[] {typeof (string), typeof (string)})
                );

            // Получим список инструкций, из которых состоит метод get_Current
            var instructions = getCurrentMethod.Body.Instructions;

            /* Так как инструкций Ret (return) является последней инструкцией метода
             * нам нужно на время извлечь её из списка инструкций
             */
            var retInst = instructions.Last();
            instructions.Remove(retInst);

            // Посмотрим какие инструкции остались:

            Console.WriteLine("Инструкции: ");
            instructions.ToList().ForEach(x => Console.WriteLine(x.OpCode));
            Console.WriteLine();

            /* Каждому .NET-разработчику известно, что ldfld закидывает значение поля класса в стек
             * в нашем случае это строка содержащая текущий символ енумератора
             * добавим в стек пробел при помощи ldstr
             */

            instructions.Add(Instruction.Create(Ldstr, " "));

            // добавим вызов метода String.Concat, который достанет из стека 2 строки и вернет туда результат
            instructions.Add(Instruction.Create(Call, concatRef));

            // вернем обратно инструкцию возврата
            instructions.Add(retInst);

            // посмотрим на список инструкций который у нас получился:

            Console.WriteLine("Новый список инструкций: ");
            instructions.ToList().ForEach(x => Console.WriteLine($"code: {x.OpCode} operand: {x.Operand}"));
            Console.WriteLine();

            // запишем нашу модифицированную сборку в файл и загрузим её в рантайм
            definition.MainModule.Write("new_asm.dll");
            var loadedAssembly = Assembly.LoadFrom("new_asm.dll");

            // создадим экземпляр класса TestClass
            var newTestClassType = loadedAssembly.GetType("TestProj.TestClass");
            var testClass = Activator.CreateInstance(newTestClassType);

            // вызовем метод Counter класса TestClass
            var sequence =
                (IEnumerable<string>) newTestClassType.GetMethod("Counter").Invoke(testClass, null);

            // Проверим есть ли пробел после каждого символа: 
            foreach (var part in sequence)
            {
                // при каждом шаге перебора IEnumerable будут вызываться методы енумератора
                // MoveNext и get_Current
                // один из которых мы модифицировали
                Console.Write(part);
            }

            Console.ReadLine();
        }
    }
}