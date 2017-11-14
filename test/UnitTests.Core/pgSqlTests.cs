using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zonkey.UnitTests
{
    [TestClass, Ignore]
    public class pgSqlTests
    {
#if (PG_SQL)
        [TestMethod]
        public void Insert_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var obj = new TestObject1(true)
                          {
                              column_two = Guid.NewGuid(),
                              column_three = true,
                              column_four = (decimal) Math.PI,
                              column_five = DateTime.Now.ToString("G"),
                              column_six =
                                  "<xml><now>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</now></xml>",
                              column_seven = DateTime.Now,
                              column_eight = DateTime.Today
                          };

            var da = new DataClassAdapter<TestObject1>(cxn);
            da.BeforeExecuteCommand += da_BeforeExecuteCommand;
            da.Save(obj).Wait();

            Console.WriteLine(obj.column_one);
            Assert.IsTrue(obj.column_one > 0);
        }

        [TestMethod]
        public void Fill_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;                        
            var da = new DataClassAdapter<TestObject1>(cxn);
            var col = new List<TestObject1>();

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            //da.SetProperty(AdapterProperty.TableName, "vw1");
            var r = da.Fill(col, t => t.column_one < 8).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);
        }

        [TestMethod]
        public void GetSingleItem_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);            

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            //da.SetProperty(AdapterProperty.TableName, "vw1");
            var r = da.GetOne(t => t.column_four > 1m).Result;

            Assert.IsNotNull(r);            
        }

        [TestMethod]
        public void FillRange_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);
            var col = new List<TestObject1>();

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            da.OrderBy = "column_one";
            var r = da.FillRange(col, 2, 3, t => t.column_one < 8).Result;

            Assert.AreEqual(r, 3);
            Assert.AreEqual(col[0].column_one, 4);
            Assert.AreEqual(col[1].column_one, 5);
            Assert.AreEqual(col[2].column_one, 6);
        }

        [TestMethod]
        public void Update_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            var obj = da.GetOne(t => 1 == 1).Result;

            obj.column_three = (!obj.column_three);
            obj.column_four = (decimal)Math.Pow((double)obj.column_four, 1.12d);
            obj.column_five = DateTime.Now.ToString("G");
            obj.column_six = "<xml><now>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</now></xml>";
            obj.column_seven = (obj.column_seven.HasValue && ((obj.column_seven.Value.Second%2) == 0)) ? null : (DateTime?)DateTime.Now;
            obj.column_eight = DateTime.Today;

            Assert.IsTrue(da.Save(obj).Result);
        }

        [TestMethod]
        public void FillWithSP_Test_1()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);
            var col = new List<TestObject1>();

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            using (var trx = cxn.BeginTransaction())
            {
                var r = da.FillWithSP(col, "fn_test1", 
                        new GenericParameter("low_index", DbType.Int32, 5)
                        ).Result;

                trx.Rollback();

                Assert.IsTrue(r > 0);
                Assert.IsTrue(col.Count > 0);

            }
        }

        [TestMethod]
        public void FillWithSP_Test_2()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);
            var col = new List<TestObject1>();

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            using (var trx = cxn.BeginTransaction())
            {
                var r = da.FillWithSP(col, "fn_test2").Result;
                trx.Rollback();

                Assert.IsTrue(r > 0);
                Assert.IsTrue(col.Count > 0);
            }
        }

        [TestMethod]
        public void Delete_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("pgSQL_Test").Result;
            var da = new DataClassAdapter<TestObject1>(cxn);

            da.BeforeExecuteCommand += da_BeforeExecuteCommand;

            var ret = da.Delete(t => t.column_one == 18).Result;
            
            Assert.AreEqual(1, ret);
        }

        private static void da_BeforeExecuteCommand(object sender, CommandExecuteEventArgs e)
        {
            Console.WriteLine(e.Command.CommandText);
        }


        [TestMethod]
        public void Copy_Test()
        {
            // Enable logging.
            NpgsqlEventLog.Level = LogLevel.Debug;
            NpgsqlEventLog.LogName = "C:\\Temp\\NpgsqlTests.Log";
            NpgsqlEventLog.EchoMessages = true;

            using (var cxn = (NpgsqlConnection)DbConnectionFactory.OpenConnection("pgSQL_Test"))
            {
                var cin = new NpgsqlCopyIn("COPY table1 (column_two, column_three, column_four, column_five, column_six, column_seven, column_eight) FROM STDIN", cxn);

                try
                {
                    cin.Start();
                    using (var writer = new StreamWriter(cin.CopyStream, new UTF8Encoding()))
                    {
                        var n = 1000;
                        while ((--n) > 0)
                        {
                            Thread.Sleep(4);

                            var obj = new TestObject1(true)
                                          {
                                              column_two = Guid.NewGuid(),
                                              column_three = true,
                                              column_four = (decimal) Math.PI,
                                              column_five = DateTime.Now.ToString("G"),
                                              column_six =
                                                  "<xml><now>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") +
                                                  "</now></xml>",
                                              column_seven = DateTime.Now,
                                              column_eight = DateTime.Today
                                          };

                            writer.Write(obj.column_two);

                            writer.Write('\t');
                            writer.Write(obj.column_three);

                            writer.Write('\t');
                            writer.Write(obj.column_four);

                            writer.Write('\t');
                            writer.Write(obj.column_five);

                            writer.Write('\t');
                            writer.Write(obj.column_six);

                            writer.Write('\t');
                            writer.Write(obj.column_seven);

                            writer.Write('\t');
                            writer.Write(obj.column_eight);

                            writer.Write('\n');
                        }

                        writer.Write("\\.\n");
                        writer.Flush();
                    }

                    cin.End();
                }
                catch (Exception e)
                {
                    try
                    {
                        cin.Cancel("Undo copy"); // Sends CopyFail to server
                    }
                    catch (Exception e2)
                    {
                        // we should get an error in response to our cancel request:
                        if (!("" + e2).Contains("Undo copy"))
                        {
                            throw new Exception("Failed to cancel copy: " + e2 + " upon failure: " + e);
                        }
                    }

                    throw;
                }

            }
        }

        [TestMethod]
        public void Copy_Test_2()
        {
            // Enable logging.
            NpgsqlEventLog.Level = LogLevel.Debug;
            NpgsqlEventLog.LogName = "C:\\Temp\\NpgsqlTests.Log";
            NpgsqlEventLog.EchoMessages = true;

            for (int cc = 0; cc < 50; cc++)
            {
                // cache to ram
                var ms = new MemoryStream();
                var writer = new StreamWriter(ms, new UTF8Encoding());

                var n = 500;
                while ((--n) > 0)
                {
                    Thread.Sleep(4);

                    var obj = new TestObject1(true)
                    {
                        column_two = Guid.NewGuid(),
                        column_three = true,
                        column_four = (decimal)Math.PI,
                        column_five = DateTime.Now.ToString("G"),
                        column_six =
                            "<xml><now>" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "</now></xml>",
                        column_seven = DateTime.Now,
                        column_eight = DateTime.Today
                    };

                    writer.Write(obj.column_two);

                    writer.Write('\t');
                    writer.Write(obj.column_three);

                    writer.Write('\t');
                    writer.Write(obj.column_four);

                    writer.Write('\t');
                    writer.Write(obj.column_five);

                    writer.Write('\t');
                    writer.Write(obj.column_six);

                    writer.Write('\t');
                    writer.Write(obj.column_seven);

                    writer.Write('\t');
                    writer.Write(obj.column_eight);

                    writer.Write('\n');
                }

                writer.Write("\\.\n");
                writer.Flush();

                // reset ms
                ms.Position = 0;

                using (var cxn = (NpgsqlConnection)DbConnectionFactory.OpenConnection("pgSQL_Test"))
                {
                    var cmd =
                        new NpgsqlCommand(
                            "COPY table1 (column_two, column_three, column_four, column_five, column_six, column_seven, column_eight) FROM STDIN",
                            cxn);

                    var cin = new NpgsqlCopyIn(cmd, cxn, ms);

                    try
                    {
                        cin.Start();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            cin.Cancel("Undo copy"); // Sends CopyFail to server
                        }
                        catch (Exception e2)
                        {
                            // we should get an error in response to our cancel request:
                            if (!("" + e2).Contains("Undo copy"))
                            {
                                throw new Exception("Failed to cancel copy: " + e2 + " upon failure: " + e);
                            }
                        }

                        throw;
                    }
                }
            }
        }
#endif
    }
}
