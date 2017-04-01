using System;
using System.Collections.Generic;

namespace Santol
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblyLoader loader = new AssemblyLoader();
            IList<ClassDefinition> classes = loader.Load("TestOS.exe");

            foreach (ClassDefinition classDefinition in classes)
            {
                CodeGenerator generator = new CodeGenerator();
                generator.GenerateClass(classDefinition);
            }

            Console.ReadLine();
        }
    }
}