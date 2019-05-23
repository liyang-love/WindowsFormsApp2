using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        //连接数据库
        string str = ConfigurationManager.ConnectionStrings["cis"].ConnectionString;

        public Form1()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "/class/";
            string pathrule = AppDomain.CurrentDomain.BaseDirectory + "/rule/";
            //先清空文件夹
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            //先清空文件夹
            if (Directory.Exists(pathrule))
            {
                Directory.Delete(pathrule, true);
            }

            //获取所有表名
            string tablename = "select TABLE_NAME from all_tables where owner='LENOVO_CIS'";

            //获取某个表的所有字段
            string sql = "select column_name,data_type,data_precision,data_scale from user_tab_columns t where t.TABLE_NAME='{0}'";

            DataTable dtTableName = GetData(tablename);
            for (int i = 0; i < dtTableName.Rows.Count; i++)
            //for (int i = 0; i < 1; i++)
            {
                DataTable dtFiled = GetData(string.Format(sql, dtTableName.Rows[i][0].ToString()));

                //if ("HD_ADVICE_EXTEND" != dtTableName.Rows[i][0].ToString())
                //{ continue; }

                //生成类名
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(GetNameSpace());
                stringBuilder.AppendLine(GetClass("CPOE.Entities", GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")));

                //生成属性
                for (int j = 0; j < dtFiled.Rows.Count; j++)
                {
                    stringBuilder.AppendLine(GetBody(dtFiled.Rows[j][0].ToString(),
                        ConvertSqlToNetType(dtFiled.Rows[j][1].ToString(), NullToInt(dtFiled.Rows[j][3]))
                                             , GetTitleCase(dtFiled.Rows[j][0].ToString().Replace("_", " ")).Replace(" ", "")));
                }

                //生成结尾括号
                stringBuilder.AppendLine(GetFoot());


                string fileName = path + GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "") + ".cs";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                File.AppendAllText(fileName, stringBuilder.ToString());

                //生成增删查改
                StringBuilder RuleBuilder = new StringBuilder();
                RuleBuilder.AppendLine(GetNameSpaceNew());
                RuleBuilder.AppendLine(GetClassNew(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")));

                List<Tuple<string, string>> filedsList = new List<Tuple<string, string>>();
                for (int j = 0; j < dtFiled.Rows.Count; j++)
                {
                    filedsList.Add(new Tuple<string, string>(dtFiled.Rows[j][0].ToString(),
                        ConvertSqlToNetType(dtFiled.Rows[j][1].ToString(), NullToInt(dtFiled.Rows[j][3]))));
                }

                RuleBuilder.AppendLine(GetSelectFiledMethod(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")
                             , filedsList, dtTableName.Rows[i][0].ToString()));

                RuleBuilder.AppendLine(GetSelectMethod(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")
             , filedsList, dtTableName.Rows[i][0].ToString()));


                RuleBuilder.AppendLine(GetMethod(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")
                                  , filedsList, dtTableName.Rows[i][0].ToString()));

                RuleBuilder.AppendLine(GetUpdateMethod(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")
                  , filedsList, dtTableName.Rows[i][0].ToString()));


                RuleBuilder.AppendLine(GetDeleteMethod(GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "")
                  , filedsList, dtTableName.Rows[i][0].ToString()));

                //生成结尾括号
                RuleBuilder.AppendLine(GetFoot());

                string fileNameNew = pathrule + GetTitleCase(dtTableName.Rows[i][0].ToString().Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "") + "Rule.cs";

                if (!Directory.Exists(pathrule))
                {
                    Directory.CreateDirectory(pathrule);
                }

                File.AppendAllText(fileNameNew, RuleBuilder.ToString());
            }

            MessageBox.Show("生成完成!");

        }

        //空值转换
        public int NullToInt(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            if (string.IsNullOrWhiteSpace(obj.ToString()))
            {
                return 0;
            }
            return Convert.ToInt32(obj.ToString());
        }

        //首字母大写转换
        public string GetTitleCase(string word)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(word.ToLower());
        }

        //首字母大写转换
        public string GetTitleLower(string word)
        {
            return word.Substring(0, 1).ToLower() + word.Substring(1);
        }

        //sql类型转c#类型
        public string ConvertSqlToNetType(string sqlType, int SqlLength)
        {
            switch (sqlType)
            {
                case "NUMBER":
                    if (SqlLength > 0) { return "decimal?"; }
                    else
                    {
                        return "int?";
                    };
                case "VARCHAR2": return "string";
                case "DATE": return "DateTime";
                default:
                    return "string";
            }
        }

        //查询数据
        public DataTable GetData(string sql)
        {
            using (OracleConnection conn = new OracleConnection(str))
            {
                conn.Open();
                OracleDataAdapter sda = new OracleDataAdapter(sql, conn);
                DataSet ds = new DataSet();
                sda.Fill(ds);
                conn.Close();
                return ds.Tables[0];
            }
        }

        //拼接名称空间
        public string GetNameSpace()
        {
            StringBuilder strClass = new StringBuilder();
            strClass.AppendLine("using Lenovo.HIS.Common;");
            strClass.AppendLine("using System;");
            strClass.AppendLine("using System.Collections.Generic;");
            strClass.AppendLine("using System.Linq;");
            strClass.AppendLine("using System.Runtime.Serialization;");
            strClass.AppendLine("using System.Text;");
            strClass.AppendLine("      ");
            return strClass.ToString();
        }

        //拼接类名
        public string GetClass(string namespac, string className)
        {
            StringBuilder strClass = new StringBuilder();
            strClass.AppendLine("namespace Lenovo.CIS." + namespac);
            strClass.AppendLine("{");
            strClass.AppendLine(" ");
            strClass.AppendLine("    [DataContract]");
            strClass.AppendLine("    public class " + className);
            strClass.AppendLine("    {");
            return strClass.ToString();
        }

        //拼接属性
        public string GetBody(string DataField, string type, string filedName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        [DataMember]");
            strb.AppendLine("        [DataField(\"" + DataField + "\")]");
            strb.AppendLine("        public " + type + " " + filedName + " { get; set; }");
            strb.AppendLine("        ");
            return strb.ToString();
        }

        //----------------------------------------
        //拼接名称空间
        public string GetNameSpaceNew()
        {
            StringBuilder strClass = new StringBuilder();
            strClass.AppendLine("using Lenovo.HIS.Entities;");
            strClass.AppendLine("using System;");
            strClass.AppendLine("using System.Collections.Generic;");
            strClass.AppendLine("using System.Linq;");
            strClass.AppendLine("using System.Runtime.Serialization;");
            strClass.AppendLine("using System.Text;");
            strClass.AppendLine("      ");
            return strClass.ToString();
        }

        //拼接类名
        public string GetClassNew(string className)
        {
            StringBuilder strClass = new StringBuilder();
            strClass.AppendLine("namespace Lenovo.CIS.CPOE.sDBRule");
            strClass.AppendLine("{");
            strClass.AppendLine(" ");
            strClass.AppendLine("    public class " + className + "ConcreteRule : " + className + "Rule");
            strClass.AppendLine("    {");
            strClass.AppendLine("        public " + className + "ConcreteRule(EnumDBType _EnumDBType)");
            strClass.AppendLine("            : base(_EnumDBType)");
            strClass.AppendLine("        {");
            strClass.AppendLine("        }");
            strClass.AppendLine("    }");
            strClass.AppendLine(" ");
            strClass.AppendLine("    public class " + className + "Rule");
            strClass.AppendLine("    {");
            strClass.AppendLine("        ");
            strClass.AppendLine("        public static EnumDBType m_DBType = EnumDBType.Oracle;");
            strClass.AppendLine("        ");
            strClass.AppendLine("        public " + className + "Rule(EnumDBType dbType)");
            strClass.AppendLine("        {");
            strClass.AppendLine("            m_DBType = dbType;");
            strClass.AppendLine("        }");
            strClass.AppendLine("  ");
            return strClass.ToString();
        }

        public string GetSelectFiledMethod(string ClassName, List<Tuple<string, string>> filedName, string tableName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        const string m_" + ClassName + "Filed =@\"");

            List<string> filedList = new List<string>();
            string filed = string.Empty;
            for (int i = 0; i < filedName.Count; i++)
            {
                if (filed.Length >= 60 && i != filedName.Count - 1)
                {
                    if (i == filedName.Count - 1)
                    {
                        filed = filed.Trim(',') + "\";    ";
                    }
                    strb.AppendLine("            " + filed);
                    filed = string.Empty;
                }
                else if (i == filedName.Count - 1)
                {
                    filed += filedName[i].Item1;
                    strb.AppendLine("            " + filed + " \";");
                    filed = string.Empty;
                }
                filed += filedName[i].Item1 + ",";
            }
            strb.AppendLine("        ");
            return strb.ToString();
        }


        public string GetSelectMethod(string ClassName, List<Tuple<string, string>> filedName, string tableName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        public virtual string Get" + ClassName + "List(List<" + ClassName + ">  " + GetTitleLower(ClassName) + "s)");
            strb.AppendLine("        {");
            strb.AppendLine("            return string.Format($\"SELECT {m_" + ClassName + "Filed} FROM " + tableName + " WHERE " + filedName[0].Item1 + " =" + GetFileds(GetTitleCase(filedName[0].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""), filedName[0].Item2, true, GetTitleLower(ClassName) + "s.") + "\");");
            strb.AppendLine("        }");

            strb.AppendLine("        ");
            return strb.ToString();
        }

        //生成增删除查改方法
        public string GetMethod(string ClassName, List<Tuple<string, string>> filedName, string tableName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        public virtual  List<string> Insert" + ClassName + "(List<" + ClassName + ">  " + GetTitleLower(ClassName) + "s)");
            strb.AppendLine("        {");
            strb.AppendLine("            List<string> _SqlList = new List<string>();");
            strb.AppendLine("            foreach (var item in "+ GetTitleLower(ClassName) + "s)");
            strb.AppendLine("            {");
            strb.AppendLine("                _SqlList.Add($@\"INSERT INTO " + tableName + "(");


            List<string> filedList = new List<string>();
            string filed = string.Empty;
            for (int i = 0; i < filedName.Count; i++)
            {
                if (filed.Length >= 60 && i != filedName.Count - 1)
                {
                    if (i == filedName.Count - 1)
                    {
                        filed = filed.Trim(',');
                    }
                    strb.AppendLine("            " + filed);
                    filed = string.Empty;
                }
                else if (i == filedName.Count - 1)
                {
                    filed += filedName[i].Item1;
                    strb.AppendLine("            " + filed);
                    filed = string.Empty;
                }
                filed += filedName[i].Item1 + ",";
            }

            strb.AppendLine("            ) VALUES (");
            string filedNew = string.Empty;
            for (int i = 0; i < filedName.Count; i++)
            {
                if (filedNew.Trim().Length >= 120)
                {
                    if (i == filedName.Count - 1)
                    {
                        if (filedName[i].Item2 == "DateTime")
                        {
                            if (filedNew != "")
                                filedNew = filedNew.Substring(0, filedNew.Length - 3) + ",";
                        }
                        //filedNew = filedNew.Trim(',');
                        filedNew += GetFiled("item." + GetTitleCase(filedName[i].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""),
                                filedName[i].Item2, (i == filedName.Count - 1), i == 0);
                        strb.AppendLine("            " + filedNew.Substring(0, filedNew.Length - 3));
                    }
                    else
                    {
                        if (filedName[i].Item2 == "DateTime")
                        {
                            if (filedNew != "")
                                filedNew = filedNew.Substring(0, filedNew.Length - 3) + ",";
                        }
                        strb.AppendLine("            " + filedNew);
                    }
                    filedNew = string.Empty;
                }
                else if (i == filedName.Count - 1)
                {
                    //filedNew = filedNew.Substring(0, filedNew.Length-3);
                    if (filedName[i].Item2 == "DateTime")
                    {
                        if (filedNew != "")
                            filedNew = filedNew.Substring(0, filedNew.Length - 3) + ",";
                    }
                    filedNew += GetFiled("item." + GetTitleCase(filedName[i].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""),
                                                    filedName[i].Item2, (i == filedName.Count - 1), i == 0);
                    strb.AppendLine("            " + filedNew.Substring(0, filedNew.Length - 3));
                    filedNew = string.Empty;
                }
                if (filedName[i].Item2 == "DateTime")
                {
                    if (filedNew != "")
                        filedNew = filedNew.Substring(0, filedNew.Length - 3) + ",";
                }
                filedNew += GetFiled("item." + GetTitleCase(filedName[i].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""),
                                                filedName[i].Item2, (i == filedName.Count - 1), i == 0);
            }
            strb.AppendLine("            ) \");");
            strb.AppendLine("            }");
            strb.AppendLine("            return _SqlList;");
            strb.AppendLine("        }");


            //strb.AppendLine("        [DataMember]");
            //strb.AppendLine("        [DataField(\"" + DataField + "\")]");
            //strb.AppendLine("        public " + type + " " + filedName + " { get; set; }");
            strb.AppendLine("        ");
            return strb.ToString();
        }

        public string GetUpdateMethod(string ClassName, List<Tuple<string, string>> filedName, string tableName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        public virtual  List<string> Update" + ClassName + "(List<" + ClassName + ">  " + GetTitleLower(ClassName) + "s)");
            strb.AppendLine("        {");
            strb.AppendLine("            List<string> _SqlList = new List<string>();");
            strb.AppendLine("            foreach (var item in "+GetTitleLower(ClassName) + "s)");
            strb.AppendLine("            {");
            strb.AppendLine("                StringBuilder builder = new StringBuilder();");
            strb.AppendLine("                builder.Append(\"UPDATE " + tableName + " SET  \");");


            List<string> filedList = new List<string>();
            string filed = string.Empty;
            for (int i = 0; i < filedName.Count; i++)
            {
                strb.AppendLine("                builder.Append($\" " + filedName[i].Item1 + " = '{item." + GetTitleCase(filedName[i].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", "") + "}'" + GetDouHao((i == filedName.Count - 1)) + "\");");
                filed += filedName[i].Item1 + ",";
            }
            strb.AppendLine("                builder.Append($\" WHERE " + filedName[0].Item1 + " =" + GetFileds(GetTitleCase(filedName[0].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""), filedName[0].Item2, true) + "\");");
            strb.AppendLine("                _SqlList.Add(builder.ToString());");
            strb.AppendLine("            }");
            strb.AppendLine("            return _SqlList;");
            strb.AppendLine("        }");

            strb.AppendLine("        ");
            return strb.ToString();
        }

        public string GetDeleteMethod(string ClassName, List<Tuple<string, string>> filedName, string tableName)
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("        public virtual  List<string> Remove" + ClassName + "(List<" + ClassName + ">  " + GetTitleLower(ClassName) + "s)");
            strb.AppendLine("        {");
            strb.AppendLine("            List<string> _SqlList = new List<string>();");
            strb.AppendLine("            foreach (var item in "+GetTitleLower(ClassName) + "s)");
            strb.AppendLine("            {");
            strb.AppendLine("                _SqlList.Add($\"DELETE FROM  " + tableName + "  WHERE " + filedName[0].Item1 + " =" + GetFileds(GetTitleCase(filedName[0].Item1.Replace("HD_", "").Replace("V_CPOE_", "").Replace("_", " ")).Replace(" ", ""), filedName[0].Item2, true) + "\");");
            strb.AppendLine("            }");
            strb.AppendLine("            return _SqlList;");
            strb.AppendLine("        }");

            strb.AppendLine("        ");
            return strb.ToString();
        }

        public string GetDouHao(bool IsLast)
        {
            if (IsLast) { return string.Empty; }
            return ",";
        }


        public string GetFiled(string filedName, string type, bool IsLast, bool isFirst)
        {
            string filed = string.Empty;
            if (isFirst)
            {
                filed += "'{";
            }
            if (type == "string")
            {
                filed += filedName + "}','{";
            }
            else if (type == "DateTime")
            {
                filed = "to_date('{" + filedName + "}','yyyy-mm-dd hh24:mi:ss') ,'{";
            }
            else
            {
                filed += filedName + "}','{";
            }
            if (IsLast)
            {
                return filed;
            }
            return filed;
        }

        public string GetFileds(string filedName, string type, bool IsLast, string para = "item.")
        {
            string filed = string.Empty;
            if (type == "string")
            {
                filed = "'{" + para + filedName + "}',";
            }
            else if (type == "DateTime")
            {
                filed = "to_date('{" + para + filedName + "}','yyyy-mm-dd hh24:mi:ss'),";
            }
            else
            {
                filed = "{" + para + filedName + "},";
            }
            if (IsLast)
            {
                return filed.Trim(',');
            }
            return filed;
        }

        //拼接尾部括号
        public string GetFoot()
        {
            StringBuilder strb = new StringBuilder();
            strb.AppendLine("    }");
            strb.AppendLine("}");
            return strb.ToString();
        }
    }
}
