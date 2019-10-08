using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snowflake.Library;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

namespace Snowflake.UnitTest
{
    [TestClass]
    public class SnowflakeIdGeneratorTest
    {
        private static List<KeyValuePair<string, long>> idGeneratorList = new List<KeyValuePair<string, long>>();
        private static readonly object lockObj = new object();

        [TestMethod]
        public void SnowflakeIdGeneratorOne()
        {
            try
            {
                int generatorindex = 0;
                int generatorMax = (int)Math.Pow(10, 6);
                SnowflakeIdGenerator idWorker1 = new SnowflakeIdGenerator(1, 1);
                var idGeneratorDictionary = new Dictionary<string, long>();
                for (int i = generatorindex; i < generatorMax; i++)
                {
                    long temp = idWorker1.nextId();
                    idGeneratorDictionary.Add(string.Format("{0}->{1}", i + 1, temp), temp);
                }
                Console.WriteLine(JsonConvert.SerializeObject(idGeneratorDictionary, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        [TestMethod]
        public void SnowflakeIdGeneratorThread()
        {
            try
            {
                int workerIdMax = (int)Math.Pow(2, 5) - 1;
                Thread[] threads = new Thread[workerIdMax];
                for (int i = 0; i < threads.Count(); i++)
                {
                    threads[i] = new Thread(new ParameterizedThreadStart(SnowflakeIdGeneratorTask));
                    threads[i].Start(i + 1);
                }

                while (true)
                {
                    int threadsStop = 0;
                    for (int i = 0; i < threads.Count(); i++)
                    {

                        if (!threads[i].IsAlive)
                        {
                            threadsStop++;
                        }
                    }
                    if (threadsStop >= threads.Count())
                    {
                        break;
                    }
                    Thread.Sleep(1);
                }

                idGeneratorList.Add(new KeyValuePair<string, long>("1", 2));
                idGeneratorList.Add(new KeyValuePair<string, long>("4", 2));
                var dataRepeat = (from a in idGeneratorList
                                  group a by a.Value into g
                                  where g.Count() > 1
                                  select g.Key).ToList();

                Console.WriteLine(JsonConvert.SerializeObject(dataRepeat, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private void SnowflakeIdGeneratorTask(object workerId)
        {
            int generatorMax = (int)Math.Pow(10, 3);
            SnowflakeIdGenerator idGenerator = new SnowflakeIdGenerator((int)workerId, 1);
            var iDGeneratorDictionary = new Dictionary<string, long>();
            for (int i = 0; i < generatorMax; i++)
            {
                long temp = idGenerator.nextId();
                iDGeneratorDictionary.Add(string.Format("idGenerators-{0}-{1}->{2}", (int)workerId, i + 1, temp), temp);
            }
            lock (lockObj)
            {
                idGeneratorList.AddRange(iDGeneratorDictionary);
            }

        }
    }
}
