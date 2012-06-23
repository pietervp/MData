using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using MData.Core;

namespace MData.SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            TypeInfo.Instance.RegisterAssembly();
            TypeInfo.SaveAssemblies();

            var concreteTest = TypeInfo.Resolve<ITest>();

            concreteTest.Test();
            concreteTest.MethodThree(4);
            concreteTest.Gener("", 1);
            concreteTest.Gener("", "");
            concreteTest.Data = "Hello World";
            concreteTest.Test("hello", "world", "this", "is", "MData");

            Console.WriteLine(concreteTest.Data);
            Console.ReadLine();
        }
    }

    [MDataData("Test")]
    public interface ITest : IId
    {
        string Data { get; set; }

        void Test(params string[] parameters);
    }

    [MDataData("No")]
    public interface IId
    {
        int Id { get; set; }
        int MethodThree(int data);
        void Gener<T>(T parameter, string test);
        void Gener<T>(T parameter, int index);
    }
}
