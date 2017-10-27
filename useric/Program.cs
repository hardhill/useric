using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


namespace useric
{
    class Program
    {
        static void Main(string[] args)
        {
            using (MySqlConnection connect = new MySqlConnection())
            {
                connect.ConnectionString = String.Format("Database = infocenter; Data Source = localhost; User Id = root;");
                try
                {
                    Console.WriteLine("Connecting...");
                    connect.Open();
                    Console.WriteLine("Connected!");
                    var sql = "SELECT * FROM operators";
                    MySqlCommand command = new MySqlCommand(sql, connect);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var login = reader.GetString(0);
                                var pass = reader.GetString(1);
                                var sqlins = String.Format("INSERT INTO LOGINS (ID_LOGIN,PASS)VALUES('{0}','{1}')", login, pass);
                                MySqlCommand inscommand = new MySqlCommand(sqlins, new MySqlConnection(connect.ConnectionString));
                                inscommand.Connection.Open();
                                int i = inscommand.ExecuteNonQuery();
                                inscommand.Connection.Close();
                            }
                        }
                    }
                    //=======================================================================
                    Console.WriteLine("2 stage");
                    command.Connection = connect;
                    command.CommandText = "SELECT * FROM logins";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var login = reader.GetString(0);
                                using (MySqlConnection conn = new MySqlConnection())
                                {
                                    conn.ConnectionString = connect.ConnectionString;
                                    conn.Open();
                                    var sqlSelect = "SELECT FA,IM,OT FROM operators WHERE login ='" + login + "'";
                                    MySqlCommand comSelectOperators = new MySqlCommand(sqlSelect, conn);
                                    try
                                    {
                                        MySqlDataReader readOperator = comSelectOperators.ExecuteReader();
                                        if (readOperator.HasRows)
                                        {
                                            while (readOperator.Read())
                                            {
                                                var FIO = readOperator.GetString(0) + " " + readOperator.GetString(1) + " " + readOperator.GetString(2);
                                                InsertFIO_to_User(new MySqlConnection(conn.ConnectionString), FIO);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        conn.Close();
                                    }
                                }

                            }
                        }
                    }
                    //================================================================================================================
                    Console.WriteLine("3 stage");
                    command.Connection = connect;
                    command.CommandText = "SELECT ID_TABEL, FIO FROM USERS";
                    using (MySqlDataReader readUsers = command.ExecuteReader())
                    {
                        if (readUsers.HasRows)
                        {
                            while (readUsers.Read())
                            {
                                var id_tabel = readUsers.GetString(0);
                                var fio = readUsers.GetString(1);
                                List<string> logins = GetLogins(new MySqlConnection(connect.ConnectionString), fio);
                                foreach(var log in logins)
                                {
                                    UpdateLoginTable(new MySqlConnection(connect.ConnectionString), (String)log, (String)id_tabel);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    connect.Close();
                }


            }
        }

        private static void UpdateLoginTable(MySqlConnection con,string log, string id_tabel)
        {
            con.Open();
            string sql = String.Format("UPDATE LOGINS SET id_tabel = '{0}' where ID_LOGIN='{1}'",id_tabel,log);
            MySqlCommand comUpdateLogin = new MySqlCommand(sql, con);
            try
            {
                comUpdateLogin.ExecuteNonQuery();
            }
            finally
            {
                con.Close();
            }
        }

        private static List<string> GetLogins(MySqlConnection mySqlConnection, string fio)
        {
            List<string> list = new List<string>();
            mySqlConnection.Open();
            try
            {
              string[] arrFio = fio.Split(' ');
                if (arrFio.Length == 3)
                {
                    MySqlCommand comSelectOperator = new MySqlCommand("SELECT LOGIN FROM operators WHERE (FA='" + arrFio[0] + "')and(IM='" + arrFio[1] + "')and(OT='" + arrFio[2] + "')", mySqlConnection);
                    MySqlDataReader readOperators = comSelectOperator.ExecuteReader();
                    while (readOperators.Read())
                    {
                        list.Add(readOperators.GetString(0));
                    }
                }
                
            }
            finally
            {

                mySqlConnection.Close();
            }
            return list;
        }

        private static void InsertFIO_to_User(MySqlConnection mySqlConnection, string FIO)
        {
            mySqlConnection.Open();
            try
            {
                if (!FIOINUSERS(new MySqlConnection(mySqlConnection.ConnectionString), FIO))
                {
                    string tabel = GenerateTabel();
                    MySqlCommand comInsertUser = new MySqlCommand("INSERT INTO USERS (ID_TABEL, FIO) VALUES('" + tabel + "','" + FIO + "')", mySqlConnection);
                    comInsertUser.ExecuteNonQuery();
                }
            }
            finally
            {
                mySqlConnection.Close();
            }
        }

        private static string GenerateTabel()
        {
            string tabel = "";
            DateTime dt = new DateTime();
            dt = DateTime.Now;
            int seed = dt.Millisecond + dt.Second + dt.Minute;
            Random rnd = new Random(seed);
            for (int i = 0; i < 6; i++)
            {
                tabel += rnd.Next(0, 9).ToString();
            }
            return tabel;
        }

        private static bool FIOINUSERS(MySqlConnection conn, string fio)
        {
            bool present = false;
            conn.Open();
            MySqlCommand comSelectFIO = new MySqlCommand("SELECT * FROM USERS Where FIO = '" + fio + "'", conn);
            try
            {
                MySqlDataReader readFIO = comSelectFIO.ExecuteReader();
                present = (readFIO.HasRows) ? true : false;

            }
            finally
            {
                conn.Close();
            }
            return present;
        }
    }
}
